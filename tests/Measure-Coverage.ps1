param([Parameter(Mandatory)][string]$Directory)
$points = @{}
foreach ($report in Get-ChildItem -LiteralPath $Directory -Recurse -Filter coverage.opencover.xml) {
    [xml]$xml = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($module in $xml.CoverageSession.Modules.Module) {
        if ($module.ModuleName -notmatch '^HealthAuthorization\.(API|Application|Domain|Infrastructure)$') { continue }
        $files = @{}
        foreach ($file in $module.Files.File) { $files[[string]$file.uid] = [string]$file.fullPath }
        foreach ($method in $module.SelectNodes('Classes/Class/Methods/Method')) {
            $owner = $method.ParentNode.ParentNode.FullName
            foreach ($point in $method.SelectNodes('SequencePoints/SequencePoint | BranchPoints/BranchPoint')) {
                $path = $files[[string]$point.fileid]
                if (!$path) { continue }
                $isBranch = $point.Name -eq 'BranchPoint'
                $key = "$($module.ModuleName)|$path|$($point.sl)"
                if ($isBranch) { $key += "|$owner|$($method.Name)|$($point.offset)|$($point.path)" }
                if (!$points.ContainsKey($key)) {
                    $points[$key] = [pscustomobject]@{ Project=$module.ModuleName; File=$path; Class=$owner; Method=$method.Name; Line=[int]$point.sl; Branch=$isBranch; Hit=$false }
                }
                if ([int]$point.vc -gt 0) { $points[$key].Hit = $true }
            }
        }
    }
}
function Measure-Points($items, $label) {
    $lines = @($items | Where-Object { !$_.Branch })
    $branches = @($items | Where-Object Branch)
    $lineHits = @($lines | Where-Object Hit).Count
    $branchHits = @($branches | Where-Object Hit).Count
    [pscustomobject]@{
        Scope=$label
        Lines="$lineHits/$($lines.Count)"
        LinePercent=if ($lines.Count) { [math]::Round(100*$lineHits/$lines.Count,2) }
        Branches="$branchHits/$($branches.Count)"
        BranchPercent=if ($branches.Count) { [math]::Round(100*$branchHits/$branches.Count,2) }
    }
}
$all = @($points.Values)
$source = @($all | Where-Object File -NotMatch '[/\\]Migrations[/\\]')
foreach ($dataset in @(@{Name='All'; Items=$all}, @{Name='Without migrations'; Items=$source})) {
    Measure-Points $dataset.Items $dataset.Name
    foreach ($project in $dataset.Items | Group-Object Project | Sort-Object Name) {
        Measure-Points $project.Group "$($dataset.Name) / $($project.Name)"
    }
}
$points.Values | Sort-Object Project,File,Line | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $Directory 'points.csv')

