param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$DownloadWebView2,
    [switch]$SkipApiPublish,
    [switch]$SingleFileApi,
    [string]$ApiProject = "..\src\Ledon.BerryShare.Api\Ledon.BerryShare.Api.csproj",
    [string]$ShellProject = "..\src\Ledon.Berry.Shell\Ledon.Berry.Shell.csproj"
)

function Run-Cmd($cmd) {
    Write-Host "    $cmd" -ForegroundColor DarkGray
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) { throw "Command failed: $cmd" }
}

$ErrorActionPreference = 'Stop'

# Resolve project paths to absolute to avoid msbuild treating them as switches
Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
try {
    if (Test-Path $ApiProject) { $ApiProject = (Resolve-Path $ApiProject).Path } else { throw "Api project not found: $ApiProject" }
    if (Test-Path $ShellProject) { $ShellProject = (Resolve-Path $ShellProject).Path } else { throw "Shell project not found: $ShellProject" }
} catch {
    Write-Error $_.Exception.Message
    exit 1
}

Write-Host "[1/7] Publish API (SelfContained=true SingleFile=$($SingleFileApi.IsPresent))" -ForegroundColor Cyan
if ($SkipApiPublish) {
    Write-Host '    Skip API publish (using existing files in assert)' -ForegroundColor Yellow
} else {
    $apiOutPath = "..\publish\api"
    if ($SingleFileApi) {
        $apiSingleArgs = "-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"
    } else {
        $apiSingleArgs = "-p:PublishSingleFile=false"
    }
    Run-Cmd "dotnet publish $ApiProject -c $Configuration -r $Runtime -p:SelfContained=true $apiSingleArgs -o $apiOutPath"

    $apiPublishDir = Resolve-Path $apiOutPath
    $assertDir = Resolve-Path "..\src\Ledon.Berry.Shell\assert"
    if (-not (Test-Path $assertDir)) { throw "assert folder not found: $assertDir" }

    Write-Host '[2/7] Clean old API files (assert)' -ForegroundColor Cyan
    Get-ChildItem $assertDir -Filter "Ledon.BerryShare.Api*" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    # 若 single-file 模式, 只需要 exe + endpoints json；否则复制全部发布输出核心文件

    Write-Host '[3/7] Copy new API files -> assert' -ForegroundColor Cyan
    if ($SingleFileApi) {
        Copy-Item (Join-Path $apiPublishDir "Ledon.BerryShare.Api.exe") $assertDir -Force
        Get-ChildItem $apiPublishDir -Filter "Ledon.BerryShare.Api.staticwebassets.endpoints.json" -ErrorAction SilentlyContinue | Copy-Item -Destination $assertDir -Force
    } else {
    # Copy minimal set: exe / dll / json / webassets / (exclude pdb unless Debug)
        $includePatterns = @('Ledon.BerryShare.Api.exe','Ledon.BerryShare.Api.dll','Ledon.BerryShare.Api.runtimeconfig.json','Ledon.BerryShare.Api.deps.json','*.staticwebassets.*.json','appsettings*.json','e_sqlite3.dll')
        foreach($p in $includePatterns){
            Get-ChildItem $apiPublishDir -Filter $p -File -ErrorAction SilentlyContinue | Copy-Item -Destination $assertDir -Force
        }
    # Frontend static assets: publish embeds mappings; extend copy logic if needed
    }
}

Write-Host '[4/7] Publish WPF Shell (SelfContained=true)' -ForegroundColor Cyan
$shellPublishCmd = "dotnet publish $ShellProject -c $Configuration -r $Runtime -p:SelfContained=true -p:PublishSingleFile=false -o ..\publish\shell"
Run-Cmd $shellPublishCmd
$publishDir = Resolve-Path ..\publish\shell

# Ensure app.ico exists (needed by Product.wxs). Create early so harvest includes it.
$appIcon = Join-Path $publishDir 'app.ico'
if (-not (Test-Path $appIcon)) {
    Write-Host 'No app.ico found, generating placeholder icon...' -ForegroundColor Yellow
    $shellExe = Join-Path $publishDir 'Ledon.Berry.Shell.exe'
    $iconCreated = $false
    if (Test-Path $shellExe) {
        try {
            Add-Type -AssemblyName System.Drawing -ErrorAction Stop
            $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($shellExe)
            if ($ico) {
                $fs = [IO.File]::Create($appIcon)
                $ico.Save($fs)
                $fs.Close(); $ico.Dispose()
                $iconCreated = $true
                Write-Host 'Extracted icon from executable.' -ForegroundColor DarkGray
            }
        } catch { Write-Host 'Icon extraction failed, will synthesize.' -ForegroundColor DarkGray }
    }
    if (-not $iconCreated) {
        # Synthesize a 16x16 32bpp white opaque icon.
        $header = 0,0,1,0,1,0,16,16,0,0,1,0,32,0,0x68,0x04,0,0,0x16,0,0,0
        $info   = 0x28,0,0,0,16,0,0,0,32,0,0,0,1,0,32,0,0,0,0,0,0x00,0x04,0,0,0xC4,0x0E,0,0,0xC4,0x0E,0,0,0,0,0,0,0,0,0,0
        $bytes = New-Object System.Collections.Generic.List[byte]
        $bytes.AddRange($header)
        $bytes.AddRange($info)
        # 256 pixels (16*16) * 4 bytes each (BGRA) -> white opaque
        for($i=0;$i -lt 256;$i++){ $bytes.AddRange(0xFF,0xFF,0xFF,0xFF) }
        # AND mask 64 bytes (all zero = opaque)
        for($i=0;$i -lt 64;$i++){ $bytes.Add(0) }
        [IO.File]::WriteAllBytes($appIcon, $bytes.ToArray())
        Write-Host 'Created synthetic placeholder icon.' -ForegroundColor DarkGray
    }
}

if ($DownloadWebView2) {
    Write-Host '[5/7] Download WebView2 bootstrapper' -ForegroundColor Cyan
    $wv2 = Join-Path $publishDir "MicrosoftEdgeWebView2Setup.exe"
    if (-not (Test-Path $wv2)) { Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $wv2 } else { Write-Host '  Exists, skip' -ForegroundColor DarkGray }
} else {
    Write-Host '[5/7] Skip WebView2 download (add -DownloadWebView2 to enable)' -ForegroundColor DarkGray
}

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host 'WiX cli not found, installing wix global dotnet tool (core build only)' -ForegroundColor Yellow
    dotnet tool install --global wix | Out-Null
}
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Error 'wix tool still unavailable (ensure %USERPROFILE%\\.dotnet\\tools is in PATH)'
    exit 1
}
try { wix extension add -g WixToolset.Util.wixext | Out-Null } catch { }
try { wix extension add -g WixToolset.Heat.wixext | Out-Null } catch { }

# heat.exe (directory harvest) is NOT shipped in dotnet global wix tool; needs full WiX Toolset install.
$heatExe = Get-Command heat.exe -ErrorAction SilentlyContinue
$hasHeatExe = $heatExe -ne $null

Write-Host '[6/7] Generate AppFiles.wxs' -ForegroundColor Cyan
Push-Location (Split-Path $MyInvocation.MyCommand.Path)
if ($hasHeatExe) {
    heat.exe dir $publishDir -cg AppFiles -dr INSTALLFOLDER -var var.PublishDir -srd -sreg -scom -ag -out AppFiles.wxs
} else {
    # Fallback: generate minimal AppFiles.wxs with root files only (no heat, no wix harvest)
    $files = Get-ChildItem $publishDir -File
    $wxsPath = Join-Path (Get-Location) 'AppFiles.wxs'
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')
    foreach($f in $files){
        $id = 'Cmp_' + ([System.Guid]::NewGuid().ToString('N').Substring(0,8))
        $fileSource = '$(var.PublishDir)\' + $f.Name
    [void]$sb.AppendLine(('      <Component Id="{0}" Guid="*" Directory="INSTALLFOLDER"><File Source="{1}" KeyPath="yes" /></Component>' -f $id,$fileSource))
    }
    [void]$sb.AppendLine('    </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER" />')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')
    $sb.ToString() | Set-Content -Encoding UTF8 $wxsPath
    Write-Host '  (heat.exe missing) Generated minimal AppFiles.wxs (root files only).' -ForegroundColor Yellow
}
Pop-Location

if (-not (Get-Command 'wix.exe' -ErrorAction SilentlyContinue)) {
    Write-Error 'wix.exe not found'
    exit 1
}

Write-Host '[7/7] Build MSI' -ForegroundColor Cyan
$scriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
# AppFiles.wxs / Product.wxs already in script directory
$outputMsi = Join-Path (Resolve-Path (Join-Path $scriptDir '..')) 'publish\BerryShare.msi'
$prodWxs = Join-Path $scriptDir 'Product.wxs'
$appFilesWxs = Join-Path $scriptDir 'AppFiles.wxs'
if (-not (Test-Path (Split-Path $outputMsi -Parent))) { New-Item -ItemType Directory -Path (Split-Path $outputMsi -Parent) | Out-Null }

$wixArgs = @('build', $prodWxs, $appFilesWxs, '-d', ('PublishDir=' + $publishDir), '-ext', 'WixToolset.Util.wixext', '-o', $outputMsi)
Write-Host ('    wix ' + ($wixArgs -join ' ')) -ForegroundColor DarkGray
& wix @wixArgs
if ($LASTEXITCODE -ne 0) { throw 'Command failed: wix build' }
Write-Host ('DONE -> ' + $outputMsi) -ForegroundColor Green

