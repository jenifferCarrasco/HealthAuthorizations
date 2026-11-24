param([string]$Container, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
if ($Container -notmatch '^healthauthorization-stage11d-[a-f0-9]{8}$') {
    throw 'Usa el contenedor temporal creado por Run-SqlServerTests.ps1.'
}
$connection = [Data.Common.DbConnectionStringBuilder]::new()
$connection.set_ConnectionString($env:ConnectionStrings__HealthAuthorization)
$sqlPort = (docker port $Container 1433/tcp).Trim().Split(':')[-1]
if ($connection.get_Item('Database') -ne 'HealthAuthorizationIdempotencyTests' -or
    $connection.get_Item('Server') -ne "127.0.0.1,$sqlPort" -or $connection.ContainsKey('AttachDBFilename')) {
    throw 'La cadena de conexión debe apuntar a la base de pruebas del contenedor aislado.'
}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path

function Sql([string]$Query, [switch]$Plan) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Query))
    $format = if ($Plan) { @('-y', '0') } else { @('-h', '-1', '-W') }
    $result = docker exec $Container sh -c 'query=$(printf %s "$1" | base64 -d); shift; SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d HealthAuthorizationIdempotencyTests -I -b "$@" -Q "$query"' sh $encoded @format
    if ($LASTEXITCODE) { throw "Falló la consulta SQL de pruebas. $($result -join ' ')" }
    return $result
}

function Start-Api([string]$Label) {
    $options = @{
        WindowStyle='Hidden'; PassThru=$true; FilePath='dotnet'
        ArgumentList='src/HealthAuthorization.API/bin/Release/net10.0/HealthAuthorization.API.dll'
        RedirectStandardOutput=(Join-Path $OutputDirectory "$Label.log")
        RedirectStandardError=(Join-Path $OutputDirectory "$Label-errors.log")
    }
    return Start-Process @options
}

function Wait-Api {
    for ($i = 0; $i -lt 30; $i++) {
        try {
            $response = Invoke-WebRequest "$base/api/authorizations?pageSize=1" -Headers $headers -TimeoutSec 3
            if ($response.StatusCode -eq 200) { return }
        }
        catch { Start-Sleep -Milliseconds 500 }
    }
    throw 'La API no respondió a la consulta autenticada.'
}

function Base64Url([byte[]]$bytes) {
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Measure-List([string]$Name, [string]$Query, [int]$Workers, [int]$PerWorker) {
    $address = "$base/api/authorizations?$Query"
    $sample = Invoke-RestMethod $address -Headers $headers
    if (@($sample.items).Count -eq 0) { throw "Sin registros para $Name." }
    $runs = 1..$Workers | ForEach-Object -ThrottleLimit $Workers -Parallel {
        $header = $using:headers
        $url = $using:address
        $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        for ($i = 0; $i -lt $using:PerWorker; $i++) {
            $watch = [Diagnostics.Stopwatch]::StartNew()
            try { $status = (Invoke-WebRequest $url -Headers $header -WebSession $session -SkipHttpErrorCheck -TimeoutSec 20).StatusCode }
            catch { $status = 0 }
            $watch.Stop()
            [pscustomobject]@{ Ms = $watch.Elapsed.TotalMilliseconds; Status = [int]$status }
        }
    }
    $sorted = @($runs.Ms | Sort-Object)
    $count = $sorted.Count
    $ok = @($runs | Where-Object Status -EQ 200).Count
    [pscustomobject]@{
        Scenario=$Name; Matching=$sample.totalCount; Workers=$Workers; Requests=$count
        MeanMs=[math]::Round(($sorted | Measure-Object -Average).Average, 2)
        MedianMs=[math]::Round(($sorted[[math]::Floor(($count - 1)/2)] + $sorted[[math]::Floor($count/2)])/2, 2)
        P95Ms=[math]::Round($sorted[[int][math]::Ceiling($count*0.95)-1], 2)
        MinMs=[math]::Round($sorted[0], 2); MaxMs=[math]::Round($sorted[-1], 2)
        Successful=$ok; Failed=$count-$ok
    }
}

function Read-DatabaseMarker([guid]$Id) {
    return (Sql @"
SET NOCOUNT ON;
SELECT a.Id, a.IsActive, a.CreatedDate, a.CreatedBy, a.LastUpdatedDate, a.LastUpdatedBy,
a.IdempotencyScopeId, a.IdempotencyKey, a.IdempotencyRequestHash, d.Id, d.ClaimedAmount, d.ArsAmount,
d.AffiliateAmount, d.IsDelete, f.AffiliateNumber, f.IsActive, p.Name, s.Name
FROM Authorizations a JOIN AuthorizationDetails d ON a.Id=d.AuthorizationId
JOIN Affiliates f ON a.AffiliateId=f.Id JOIN Providers p ON p.Id=d.ProviderId
JOIN ServiceTypes s ON s.Id=d.ServiceTypeId WHERE a.Id='$Id' ORDER BY d.Id;
"@) -join [Environment]::NewLine
}

$settings = @{
    ASPNETCORE_ENVIRONMENT='Development'
    Authentication__Schemes__DevelopmentJwt__ValidIssuer='dotnet-user-jwts'
    Authentication__Schemes__DevelopmentJwt__SigningKeys__0__Issuer='dotnet-user-jwts'
    'Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command'='Information'
}
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$base = "http://127.0.0.1:$($listener.LocalEndpoint.Port)"
$listener.Stop()
$key = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$settings['ASPNETCORE_URLS'] = $base
$settings['Authentication__Schemes__DevelopmentJwt__ValidAudiences__0'] = $base
$settings['Authentication__Schemes__DevelopmentJwt__SigningKeys__0__Value'] = [Convert]::ToBase64String($key)
$userId = [guid]::NewGuid()
$claims = @{
    iss='dotnet-user-jwts'; aud=$base; exp=[DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
    role='Administrator'; healthauthorization_user_id=$userId.ToString()
} | ConvertTo-Json -Compress
$jwt = (Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))) + '.' + (Base64Url ([Text.Encoding]::UTF8.GetBytes($claims)))
$hmac = [Security.Cryptography.HMACSHA256]::new($key)
$headers = @{ Authorization = "Bearer $jwt.$(Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($jwt))))" }
$hmac.Dispose()
$previous = @{}
$api = $null
try {
    foreach ($name in $settings.Keys) {
        $previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')
        [Environment]::SetEnvironmentVariable($name,$settings[$name],'Process')
    }
    $fixture = Sql @'
SET NOCOUNT ON;
IF DB_NAME() <> N'HealthAuthorizationIdempotencyTests' THROW 50001, 'Base de pruebas requerida', 1;
SELECT @@VERSION;
SELECT cpu_count, physical_memory_kb FROM sys.dm_os_sys_info;
;WITH n AS (SELECT TOP (300) ROW_NUMBER() OVER (ORDER BY a.object_id,b.object_id) AS n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT Affiliates (Id,AffiliateNumber,FirstName,LastName,BirthDate,DocumentType,CreatedDate)
SELECT NEWID(),CONCAT('PERF-',n),'Prueba','Rendimiento','1990-01-01',0,'2025-01-01' FROM n;
SELECT ROW_NUMBER() OVER (ORDER BY AffiliateNumber) AS n,Id INTO #affiliates FROM Affiliates WHERE AffiliateNumber LIKE 'PERF-%';
;WITH n AS (SELECT TOP (30000) ROW_NUMBER() OVER (ORDER BY a.object_id,b.object_id) AS n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT Authorizations (Id,AffiliateId,IsActive,CreatedDate)
SELECT NEWID(),f.Id,IIF(n.n%5=0,0,1),DATEADD(day,CONVERT(int,n.n%365),CONVERT(datetimeoffset,'2025-01-01'))
FROM n JOIN #affiliates f ON f.n=1+n.n%300;
INSERT AuthorizationDetails (Id,AuthorizationId,ProviderId,ServiceTypeId,ClaimedAmount,ArsAmount,AffiliateAmount,IsDelete)
SELECT NEWID(),a.Id,d.ProviderId,d.ServiceTypeId,d.Amount,d.Amount*0.70,d.Amount*0.30,0
FROM Authorizations a JOIN #affiliates f ON f.Id=a.AffiliateId CROSS JOIN (VALUES
('00000065-0000-0000-0000-000000000000','0000000b-0000-0000-0000-000000000000',100),
('00000066-0000-0000-0000-000000000000','0000000c-0000-0000-0000-000000000000',50),
('00000067-0000-0000-0000-000000000000','0000000e-0000-0000-0000-000000000000',20)) d(ProviderId,ServiceTypeId,Amount);
SELECT CONCAT('AFFILIATE=',CONVERT(varchar(36),Id)) FROM #affiliates WHERE n=2;
SELECT CONCAT('AUTHORIZATIONS=',COUNT(*)) FROM Authorizations a JOIN #affiliates f ON f.Id=a.AffiliateId;
SELECT CONCAT('DETAILS=',COUNT(*)) FROM AuthorizationDetails d JOIN Authorizations a ON a.Id=d.AuthorizationId JOIN #affiliates f ON f.Id=a.AffiliateId;
SELECT CONCAT('INDEX=',name,': ',ISNULL(filter_definition,'(sin filtro)')) FROM sys.indexes WHERE object_id IN (OBJECT_ID('Authorizations'),OBJECT_ID('AuthorizationDetails'));
'@
    $fixture | Set-Content (Join-Path $OutputDirectory 'environment.txt')
    $affiliate = ($fixture | Where-Object { $_ -like 'AFFILIATE=*' }).Split('=')[1].Trim()
    $api = Start-Api 'api-before-restart'
    Wait-Api
    $range = 'createdFrom=2025-05-01T00:00:00Z&createdBefore=2025-09-01T00:00:00Z'
    $metrics = @(
        Measure-List 'Sin filtros tipo 10' 'pageSize=10' 1 30
        Measure-List 'Sin filtros tipo 50' 'pageSize=50' 1 30
        Measure-List 'Sin filtros tipo 100' 'pageSize=100' 1 30
        Measure-List 'Afiliado' "affiliateId=$affiliate&pageSize=20" 1 30
        Measure-List 'Fecha' "$range&pageSize=20" 1 30
        Measure-List 'Inactivas' 'isActive=false&pageSize=20' 1 30
        Measure-List 'Afiliado, estado y fecha' "affiliateId=$affiliate&isActive=true&$range&pageSize=20" 1 30
        Measure-List 'Página 1500, tamaño 20' 'pageNumber=1500&pageSize=20' 1 30
        Measure-List 'Lecturas concurrentes' 'pageSize=20' 8 10
    )
    $metrics | Export-Csv -NoTypeInformation (Join-Path $OutputDirectory 'performance.csv')
    if (($metrics.Failed | Measure-Object -Sum).Sum -gt 0) { throw 'Hubo lecturas fallidas.' }

    $log = Get-Content (Join-Path $OutputDirectory 'api-before-restart.log') -Raw
    $select = [regex]::Match($log, '(?ms)^ +SELECT \[a\]\.\[Id\].*?OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY').Value
    if (!$select) { throw 'No se encontró la consulta paginada en el registro de EF Core.' }
    foreach ($offset in @(0, 29980)) {
        $plan = Sql "SET NOCOUNT ON; DECLARE @p int=$offset, @p1 int=20; SET STATISTICS XML ON; $select; SET STATISTICS XML OFF;" -Plan
        if (($plan -join '') -notmatch '<ShowPlanXML') { throw 'SQL Server no devolvió el plan de ejecución.' }
        $plan | Set-Content (Join-Path $OutputDirectory "plan-offset-$offset.txt")
    }

    $createHeaders = $headers.Clone()
    $idempotency = [guid]::NewGuid().ToString()
    $createHeaders['Idempotency-Key'] = $idempotency
    $body = @{ affiliateId=$affiliate; services=@(
        @{ providerId='00000065-0000-0000-0000-000000000000'; serviceTypeId='0000000b-0000-0000-0000-000000000000'; claimedAmount=100 }
    ) } | ConvertTo-Json -Depth 4 -Compress
    $created = Invoke-WebRequest "$base/api/authorizations" -Method Post -Headers $createHeaders -ContentType 'application/json' -Body $body
    if ($created.StatusCode -ne 201) { throw 'No se creó la autorización testigo.' }
    $id = [guid]($created.Content | ConvertFrom-Json).id
    $url = "$base/api/authorizations/$id"
    Invoke-WebRequest "$url/status" -Method Patch -Headers $headers -ContentType 'application/json' -Body '{"isActive":false}' | Out-Null
    $before = (Invoke-WebRequest $url -Headers $headers).Content
    $databaseBefore = Read-DatabaseMarker $id
    if (!$databaseBefore.Contains($userId.ToString(), [StringComparison]::OrdinalIgnoreCase) -or
        !$databaseBefore.Contains($idempotency, [StringComparison]::OrdinalIgnoreCase)) { throw 'Falta auditoría o idempotencia en SQL.' }

    Stop-Process -Id $api.Id
    $api.WaitForExit()
    $api = Start-Api 'api-after-restart'
    Wait-Api
    if ((Invoke-WebRequest $url -Headers $headers).Content -ne $before -or (Read-DatabaseMarker $id) -ne $databaseBefore) {
        throw 'La autorización cambió tras reiniciar la API.'
    }
    docker restart $Container | Out-Null
    if ($LASTEXITCODE) { throw 'No se pudo reiniciar SQL Server.' }
    $restartedPort = (docker port $Container 1433/tcp).Trim().Split(':')[-1]
    if ($restartedPort -ne $sqlPort) { throw 'Cambió el puerto publicado de SQL Server.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try { Sql 'SELECT 1' | Out-Null; $ready = $true; break }
        catch { Start-Sleep -Seconds 1 }
    }
    if (!$ready) { throw 'SQL Server no volvió a estar disponible.' }
    Wait-Api
    if ((Invoke-WebRequest $url -Headers $headers).Content -ne $before -or (Read-DatabaseMarker $id) -ne $databaseBefore) {
        throw 'La autorización cambió tras reiniciar SQL Server.'
    }
    $replay = Invoke-WebRequest "$base/api/authorizations" -Method Post -Headers $createHeaders -ContentType 'application/json' -Body $body
    if ($replay.StatusCode -ne 201 -or [guid]($replay.Content | ConvertFrom-Json).id -ne $id) { throw 'El reintento perdió su referencia.' }
    @(
        'Reinicio de API: consulta autenticada y fila SQL idénticas.'
        'Reinicio de SQL Server: consulta autenticada y fila SQL idénticas.'
        'Reintento tras ambos reinicios: HTTP 201 con el ID original.'
        'Comparación SQL: detalle, montos, auditoría, referencias de catálogo, alcance, clave y huella.'
    ) | Set-Content (Join-Path $OutputDirectory 'recovery.txt')
}
finally {
    if ($api -and !$api.HasExited) { Stop-Process -Id $api.Id; $api.WaitForExit() }
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') }
}
