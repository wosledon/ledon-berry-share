param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$DownloadWebView2,
    [switch]$SkipApiPublish,
    [switch]$SingleFileApi,
    [string]$ApiProject = '..\src\Ledon.BerryShare.Api\Ledon.BerryShare.Api.csproj',
    [string]$ShellProject = '..\src\Ledon.Berry.Shell\Ledon.Berry.Shell.csproj'
)

function Run-Cmd([string]$cmd){
    Write-Host "    $cmd" -ForegroundColor DarkGray
    Invoke-Expression $cmd
    if($LASTEXITCODE -ne 0){ throw "Command failed: $cmd" }
}

$scriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
Push-Location $scriptDir
$ApiProject = (Resolve-Path $ApiProject).Path
$ShellProject = (Resolve-Path $ShellProject).Path
$rootOut = Resolve-Path (Join-Path $scriptDir '..')
$apiOutPath = Join-Path $rootOut 'publish/api'
$shellOutPath = Join-Path $rootOut 'publish/shell'
New-Item -ItemType Directory -Force -Path $apiOutPath | Out-Null
New-Item -ItemType Directory -Force -Path $shellOutPath | Out-Null

$step = 1
if(-not $SkipApiPublish){
    Write-Host "[$step/7] Publish API" -ForegroundColor Cyan; $step++
    if($SingleFileApi){ $apiSingleArgs='-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true' } else { $apiSingleArgs='-p:PublishSingleFile=false' }
    Run-Cmd "dotnet publish `"$ApiProject`" -c $Configuration -r $Runtime -p:SelfContained=true $apiSingleArgs -o `"$apiOutPath`""

    $apiPublishDir = Resolve-Path $apiOutPath
    $assertDir = Resolve-Path (Join-Path (Split-Path $ShellProject -Parent) 'assert')
    if (-not (Test-Path $assertDir)) { throw "assert folder not found: $assertDir" }

    Write-Host "[$step/7] Clean old API files (assert)" -ForegroundColor Cyan; $step++
    Get-ChildItem $assertDir -Filter 'Ledon.BerryShare.Api*' -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

    Write-Host "[$step/7] Copy new API files -> assert" -ForegroundColor Cyan; $step++
    if($SingleFileApi){
        Copy-Item (Join-Path $apiPublishDir 'Ledon.BerryShare.Api.exe') $assertDir -Force
        Get-ChildItem $apiPublishDir -Filter 'Ledon.BerryShare.Api.staticwebassets.endpoints.json' -ErrorAction SilentlyContinue | Copy-Item -Destination $assertDir -Force
    } else {
        $includePatterns = @('Ledon.BerryShare.Api.exe','Ledon.BerryShare.Api.dll','Ledon.BerryShare.Api.runtimeconfig.json','Ledon.BerryShare.Api.deps.json','*.staticwebassets.*.json','appsettings*.json','e_sqlite3.dll')
        foreach($p in $includePatterns){ Get-ChildItem $apiPublishDir -Filter $p -File -ErrorAction SilentlyContinue | Copy-Item -Destination $assertDir -Force }
    }
} else {
    Write-Host "[1-3/7] Skip API publish & copy (-SkipApiPublish)" -ForegroundColor DarkGray
    $step = 4
}

Write-Host "[$step/7] Publish WPF Shell (SelfContained=true)" -ForegroundColor Cyan; $step++
Run-Cmd "dotnet publish `"$ShellProject`" -c $Configuration -r $Runtime -p:SelfContained=true -p:PublishSingleFile=false -o `"$shellOutPath`""
$publishDir = (Resolve-Path $shellOutPath).ProviderPath

Write-Host "[$step/7] Ensure icon" -ForegroundColor Cyan; $step++
$appIcon = Join-Path $publishDir 'app.ico'
if (-not (Test-Path $appIcon)) {
    Write-Host '  No app.ico, generating placeholder...' -ForegroundColor Yellow
    $shellExe = Join-Path $publishDir 'Ledon.Berry.Shell.exe'
    $iconCreated = $false
    if (Test-Path $shellExe) {
        try {
            Add-Type -AssemblyName System.Drawing -ErrorAction Stop
            $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($shellExe)
            if ($ico) { $fs = [IO.File]::Create($appIcon); $ico.Save($fs); $fs.Close(); $ico.Dispose(); $iconCreated=$true }
        } catch { }
    }
    if (-not $iconCreated) {
        $header = 0,0,1,0,1,0,16,16,0,0,1,0,32,0,0x68,0x04,0,0,0x16,0,0,0
        $info   = 0x28,0,0,0,16,0,0,0,32,0,0,0,1,0,32,0,0,0,0,0,0x00,0x04,0,0,0xC4,0x0E,0,0,0xC4,0x0E,0,0,0,0,0,0,0,0,0,0
        $bytes = New-Object System.Collections.Generic.List[byte]
        $bytes.AddRange($header); $bytes.AddRange($info)
        for($i=0;$i -lt 256;$i++){ $bytes.AddRange(0xFF,0xFF,0xFF,0xFF) }
        for($i=0;$i -lt 64;$i++){ $bytes.Add(0) }
        [IO.File]::WriteAllBytes($appIcon,$bytes.ToArray())
    }
}

if ($DownloadWebView2) {
    Write-Host "[$step/7] Download WebView2 bootstrapper" -ForegroundColor Cyan
    $wv2 = Join-Path $publishDir 'MicrosoftEdgeWebView2Setup.exe'
    if (-not (Test-Path $wv2)) { Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $wv2 }
} else {
    Write-Host "[$step/7] Skip WebView2 download (add -DownloadWebView2)" -ForegroundColor DarkGray
}

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host 'WiX cli not found, installing...' -ForegroundColor Yellow
    dotnet tool install --global wix | Out-Null
}
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) { Write-Error 'wix tool still unavailable'; exit 1 }
try { wix extension add -g WixToolset.Util.wixext | Out-Null } catch { }
try { wix extension add -g WixToolset.Heat.wixext | Out-Null } catch { }
$hasHeatExe = (Get-Command heat.exe -ErrorAction SilentlyContinue) -ne $null

Write-Host "[$step/7] Generate AppFiles.wxs" -ForegroundColor Cyan; $step++
Push-Location $scriptDir
if ($hasHeatExe) {
    heat.exe dir $publishDir -cg AppFiles -dr INSTALLFOLDER -var var.PublishDir -srd -sreg -scom -ag -out AppFiles.wxs
} else {
    $wxsPath = Join-Path (Get-Location) 'AppFiles.wxs'
    $allFiles = Get-ChildItem $publishDir -Recurse -File
    $root = ($publishDir.TrimEnd('\\'))
    $dirs = $allFiles | ForEach-Object { Split-Path $_.FullName -Parent } | Sort-Object -Unique
    if ($dirs -notcontains $root) { $dirs = @($root) + $dirs }
    $dirIds = @{}
    $dirIds[$root] = 'INSTALLFOLDER'
    foreach($d in ($dirs | Where-Object { $_ -ne $root })) {
        $leaf = Split-Path $d -Leaf
        $safe = ($leaf -replace '[^A-Za-z0-9_]','_'); if (-not $safe) { $safe='D' }
        $base='Dir_' + $safe; $i=1; $id=$base
        while($dirIds.Values -contains $id){ $i++; $id=$base + '_' + $i }
        $dirIds[$d]=$id
    }
    $children=@{}
    foreach($d in $dirIds.Keys){ if($d -ne $root){ $p=Split-Path $d -Parent; if(-not $children.ContainsKey($p)){ $children[$p]=@() }; $children[$p]+=$d } }
    $componentRefs = New-Object System.Collections.Generic.List[string]
    $sb=[System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    function Emit-Dir([string]$dir){
        $id = $dirIds[$dir]
        if($dir -ne $root -and $dir){ $name = Split-Path $dir -Leaf; if(-not $name){ $name = 'ROOT'; if($id -eq 'INSTALLFOLDER'){ $name='.' } } [void]$sb.AppendLine('      <Directory Id="' + $id + '" Name="' + $name + '">') }
        $filesHere = $allFiles | Where-Object { (Split-Path $_.FullName -Parent) -eq $dir }
        foreach($f in $filesHere){
            $cmpId = 'Cmp_' + ([Guid]::NewGuid().ToString('N').Substring(0,8))
            $src = $f.FullName
            [void]$sb.AppendLine('        <Component Id="' + $cmpId + '" Guid="*" Directory="' + $id + '"><File Source="' + $src + '" KeyPath="yes" /></Component>')
            $componentRefs.Add($cmpId) | Out-Null
        }
        if ($children.ContainsKey($dir)) { foreach($c in $children[$dir]){ Emit-Dir $c } }
        if($dir -ne $root){ [void]$sb.AppendLine('      </Directory>') }
    }
    Emit-Dir $root
    [void]$sb.AppendLine('    </DirectoryRef>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')
    foreach($cid in $componentRefs){ [void]$sb.AppendLine('      <ComponentRef Id="' + $cid + '" />') }
    [void]$sb.AppendLine('    </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')
    $sb.ToString() | Set-Content -Encoding UTF8 $wxsPath
    Write-Host '  (heat.exe missing) Generated recursive AppFiles.wxs (includes subdirectories).' -ForegroundColor Yellow
}
Pop-Location

if (-not (Get-Command 'wix.exe' -ErrorAction SilentlyContinue)) { Write-Error 'wix.exe not found'; exit 1 }

Write-Host "[$step/7] Build MSI" -ForegroundColor Cyan
$outputMsi = Join-Path $rootOut 'publish/BerryShare.msi'
$prodWxs = Join-Path $scriptDir 'Product.wxs'
$appFilesWxs = Join-Path $scriptDir 'AppFiles.wxs'
if (-not (Test-Path (Split-Path $outputMsi -Parent))) { New-Item -ItemType Directory -Path (Split-Path $outputMsi -Parent) | Out-Null }
$wixArgs = @('build', $prodWxs, $appFilesWxs, '-d', ('PublishDir=' + $publishDir), '-ext', 'WixToolset.Util.wixext', '-o', $outputMsi)
Write-Host ('    wix ' + ($wixArgs -join ' ')) -ForegroundColor DarkGray
& wix @wixArgs
if ($LASTEXITCODE -ne 0) { throw 'Command failed: wix build' }
Write-Host ('DONE -> ' + $outputMsi) -ForegroundColor Green

Pop-Location
