param([string]$Filter, [string]$CoverageDirectory, [string]$PerformanceDirectory)
$ErrorActionPreference = 'Stop'
$previousEnvironment = @{}
foreach ($name in @('DOTNET_CLI_TELEMETRY_OPTOUT', 'MSSQL_SA_PASSWORD', 'HEALTHAUTHORIZATION_IDEMPOTENCY_TEST_CONNECTION', 'ConnectionStrings__HealthAuthorization', 'ASPNETCORE_ENVIRONMENT')) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
$container = 'healthauthorization-stage11d-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$started = $false

try {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:MSSQL_SA_PASSWORD = 'Aa9!' + [Guid]::NewGuid().ToString('N')
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $hostPort = $listener.LocalEndpoint.Port
    $listener.Stop()
    docker run -d --name $container -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p "127.0.0.1:${hostPort}:1433" mcr.microsoft.com/mssql/server:2022-latest | Out-Null
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $started = $true
    $ready = $false
    for ($i = 0; $i -lt 40; $i++) {
        docker exec $container sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d master -b -l 2 -Q "SELECT 1" >/dev/null 2>&1'
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (!$ready) { throw 'SQL Server no respondió.' }
    $port = (docker port $container 1433/tcp).Trim().Split(':')[-1]
    Write-Host "SQL Server aislado disponible en 127.0.0.1:$port ($container)"
    $env:HEALTHAUTHORIZATION_IDEMPOTENCY_TEST_CONNECTION = "Server=127.0.0.1,$port;Database=HealthAuthorizationIdempotencyTests;User ID=sa;Password=$env:MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
    $env:ConnectionStrings__HealthAuthorization = $env:HEALTHAUTHORIZATION_IDEMPOTENCY_TEST_CONNECTION
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet build HealthAuthorization.slnx -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet tool run dotnet-ef database update --project src/HealthAuthorization.Infrastructure --startup-project src/HealthAuthorization.API --context HealthAuthorizationDbContext --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $testArgs = @('HealthAuthorization.slnx', '-c', 'Release', '--no-build', '--no-restore')
    if ($Filter) { $testArgs += @('--filter', $Filter) }
    if ($CoverageDirectory) {
        $testArgs += @('--collect', 'XPlat Code Coverage', '--results-directory', $CoverageDirectory,
            '--', 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover')
    }
    dotnet test @testArgs
    $testExit = $LASTEXITCODE
    if ($testExit -eq 0 -and $PerformanceDirectory) {
        & "$PSScriptRoot/Measure-SqlServerApi.ps1" -Container $container -OutputDirectory $PerformanceDirectory
    }
    docker exec $container sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d HealthAuthorizationIdempotencyTests -b -Q "SET NOCOUNT ON; SELECT DB_NAME() AS TestDatabase; SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId; SELECT COUNT(*) AS Authorizations FROM Authorizations; SELECT COUNT(*) AS Details FROM AuthorizationDetails; SELECT COUNT(*) AS HistoricalDetails FROM AuthorizationDetails WHERE IsDelete = 1; SELECT COUNT(*) AS DuplicateScopes FROM (SELECT IdempotencyScopeId, IdempotencyKey FROM Authorizations WHERE IdempotencyKey IS NOT NULL GROUP BY IdempotencyScopeId, IdempotencyKey HAVING COUNT(*) > 1) AS Duplicates;"'
    if ($testExit -ne 0) { exit $testExit }
    exit $LASTEXITCODE
}
finally {
    if ($started) {
        docker stop $container | Out-Null
    }
    foreach ($name in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
}
