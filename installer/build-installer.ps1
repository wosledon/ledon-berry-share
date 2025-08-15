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

    $wxsPath = Join-Path (Get-Location) 'AppFiles.wxs'
    $allFiles = Get-ChildItem $publishDir -Recurse -File
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    # ComponentGroup fragment
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')
    # We'll build directory tree separately; components will reference directory IDs.
    # Build map: directory full path -> directory Id
    $dirIdMap = @{}
    $dirIdMap[$publishDir] = 'INSTALLFOLDER'
    function New-DirId($path){
        $name = Split-Path $path -Leaf
        $safe = ($name -replace '[^A-Za-z0-9_]','_')
        if (-not $safe) { $safe = 'D' }
        $base = 'Dir_' + $safe
        $i=1
        $id=$base
        while($dirIdMap.Values -contains $id){ $i++; $id = $base + '_' + $i }
        return $id
    }
    $dirs = $allFiles | ForEach-Object { Split-Path $_.FullName -Parent } | Sort-Object -Unique
    foreach($d in $dirs){
        if ($d -eq $publishDir) { continue }
        # ascend until parent mapped
        $stack = @()
        $cur = $d
        while($cur -and -not $dirIdMap.ContainsKey($cur)) { $stack += $cur; $cur = Split-Path $cur -Parent }
        foreach($p in ($stack | Sort-Object)){
            if (-not $dirIdMap.ContainsKey($p)) {
                $dirIdMap[$p] = New-DirId $p
            }
        }
    }
    # Emit DirectoryRef fragment for tree
    [void]$sb.AppendLine('  </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    # Build child lists
    $children = @{}
    foreach($path in $dirIdMap.Keys){ if ($path -ne $publishDir){ $parent = Split-Path $path -Parent; if (-not $children.ContainsKey($parent)) { $children[$parent]=@() }; $children[$parent]+=$path } }
    function Emit-Dir($path){
        $id = $dirIdMap[$path]
        $name = Split-Path $path -Leaf
        [void]$sb.AppendLine("      <Directory Id=\"$id\" Name=\"$name\">")
        if ($children.ContainsKey($path)) { foreach($c in $children[$path]){ Emit-Dir $c } }
        # components for files directly in this directory
        $filesHere = $allFiles | Where-Object { (Split-Path $_.FullName -Parent) -eq $path }
        foreach($f in $filesHere){
            $cmpId = 'Cmp_' + ([Guid]::NewGuid().ToString('N').Substring(0,8))
            $relDirId = $id
            $srcPath = $f.FullName
            [void]$sb.AppendLine("        <Component Id=\"$cmpId\" Guid=\"*\" Directory=\"$relDirId\"><File Source=\"$srcPath\" KeyPath=\"yes\" /></Component>")
        }
        [void]$sb.AppendLine('      </Directory>')
    }
    if ($children.ContainsKey($publishDir)) { foreach($c in $children[$publishDir]){ Emit-Dir $c } }
    # root-level files (already only if directory is publishDir)
    $rootFiles = $allFiles | Where-Object { (Split-Path $_.FullName -Parent) -eq $publishDir }
    foreach($f in $rootFiles){
        $cmpId = 'Cmp_' + ([Guid]::NewGuid().ToString('N').Substring(0,8))
        [void]$sb.AppendLine("      <Component Id=\"$cmpId\" Guid=\"*\" Directory=\"INSTALLFOLDER\"><File Source=\"$($f.FullName)\" KeyPath=\"yes\" /></Component>")
    }
    [void]$sb.AppendLine('    </DirectoryRef>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')
    $sb.ToString() | Set-Content -Encoding UTF8 $wxsPath
    Write-Host '  (heat missing) Generated recursive AppFiles.wxs (includes subdirectories).' -ForegroundColor Yellow
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
    # Fallback: recursively include all files (including assert folder) building directory tree
    $wxsPath = Join-Path (Get-Location) 'AppFiles.wxs'
    $allFiles = Get-ChildItem $publishDir -Recurse -File
    $root = $publishDir.TrimEnd('\\')
    # Collect distinct directories (including root)
    $dirs = $allFiles | ForEach-Object { Split-Path $_.FullName -Parent } | Sort-Object -Unique
    if ($dirs -notcontains $root) { $dirs = @($root) + $dirs }
    # Map dir -> Id
    $dirIds = @{}
    $dirIds[$root] = 'INSTALLFOLDER'
    foreach($d in ($dirs | Where-Object { $_ -ne $root })) {
        $leaf = Split-Path $d -Leaf
        $safe = ($leaf -replace '[^A-Za-z0-9_]','_')
        if (-not $safe) { $safe = 'D' }
        $base = 'Dir_' + $safe
        $i=1; $id=$base
        while($dirIds.Values -contains $id){ $i++; $id = $base + '_' + $i }
        $dirIds[$d] = $id
    }
    # Build parent->children map
    $children = @{}
    foreach($d in $dirIds.Keys) { if ($d -ne $root) { $p = Split-Path $d -Parent; if (-not $children.ContainsKey($p)) { $children[$p]=@() }; $children[$p]+=$d } }
    $componentRefs = New-Object System.Collections.Generic.List[string]
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    # Fragment 1: DirectoryRef + Components
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    function Emit-Dir([string]$dir){
        param()
        $id = $dirIds[$dir]
        if ($dir -ne $root) {
            $name = Split-Path $dir -Leaf
            [void]$sb.AppendLine("      <Directory Id=\"$id\" Name=\"$name\">")
        }
        # components for files directly in this dir
        $filesHere = $allFiles | Where-Object { (Split-Path $_.FullName -Parent) -eq $dir }
        foreach($f in $filesHere){
            $cmpId = 'Cmp_' + ([Guid]::NewGuid().ToString('N').Substring(0,8))
            $src = $f.FullName
            [void]$sb.AppendLine("        <Component Id=\"$cmpId\" Guid=\"*\" Directory=\"$id\"><File Source=\"$src\" KeyPath=\"yes\" /></Component>")
            $componentRefs.Add($cmpId) | Out-Null
        }
        if ($children.ContainsKey($dir)) { foreach($c in $children[$dir]) { Emit-Dir $c } }
        if ($dir -ne $root) { [void]$sb.AppendLine('      </Directory>') }
    }
    Emit-Dir $root
    [void]$sb.AppendLine('    </DirectoryRef>')
    [void]$sb.AppendLine('  </Fragment>')
    # Fragment 2: ComponentGroup referencing components
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')
    foreach($cid in $componentRefs){ [void]$sb.AppendLine("      <ComponentRef Id=\"$cid\" />") }
    [void]$sb.AppendLine('    </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')
    $sb.ToString() | Set-Content -Encoding UTF8 $wxsPath
    Write-Host '  (heat.exe missing) Generated recursive AppFiles.wxs (includes subdirectories).' -ForegroundColor Yellow
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

