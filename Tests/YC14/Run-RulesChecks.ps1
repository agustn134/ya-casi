param([string]$UnityData = 'Z:\Dev\Unity\Editors\6000.3.25f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtimeRoot = Join-Path $UnityData 'NetCoreRuntime'
$runtime = Get-ChildItem (Join-Path $runtimeRoot 'shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$dotnet = Join-Path $runtimeRoot 'dotnet.exe'
$compiler = Join-Path $UnityData 'DotNetSdkRoslyn/csc.dll'
$testOutput = Join-Path ([System.IO.Path]::GetTempPath()) 'ya-casi-yc14-rules'
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null
$assembly = Join-Path $testOutput 'RulesChecks.dll'
$sources = @('Assets/Scripts/ProjectInventory.cs', 'Assets/Scripts/ProjectDeliverySession.cs', 'Tests/YC14/DeliveryRulesChecks.cs') | ForEach-Object { Join-Path $projectRoot $_ }
$references = @('System.Private.CoreLib.dll', 'System.Runtime.dll', 'System.Console.dll', 'System.Collections.dll') | ForEach-Object { '-r:' + (Join-Path $runtime.FullName $_) }
& $dotnet exec $compiler -noconfig -nostdlib+ -target:exe "-out:$assembly" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas' }
@{runtimeOptions = @{tfm = 'net6.0'; framework = @{name = 'Microsoft.NETCore.App'; version = $runtime.Name}}} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $testOutput 'RulesChecks.runtimeconfig.json')
$results = & $dotnet $assembly
if ($LASTEXITCODE -ne 0) { throw ($results -join "`n") }
$results | Set-Content (Join-Path $PSScriptRoot 'UltimaEjecucion.txt')
$results
