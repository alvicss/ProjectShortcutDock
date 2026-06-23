param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

$artifacts = Join-Path $root "artifacts"
$buildOut = Join-Path $root "bin\Release\net10.0-windows"
$package = Join-Path $artifacts "package"
$app = Join-Path $package "app"
$zipPath = Join-Path $artifacts "ProjectShortcutDock-$Version-win-x64.zip"

Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifacts, $app, (Join-Path $app "image") | Out-Null

& $dotnet build (Join-Path $root "ProjectShortcutDock.csproj") -c Release

$requiredFiles = @(
    "ProjectShortcutDock.exe",
    "ProjectShortcutDock.dll",
    "ProjectShortcutDock.deps.json",
    "ProjectShortcutDock.runtimeconfig.json"
)

foreach ($file in $requiredFiles) {
    Copy-Item -LiteralPath (Join-Path $buildOut $file) -Destination $app -Force
}

Copy-Item -LiteralPath `
    (Join-Path $root "image\project-shortcut-dock.ico"), `
    (Join-Path $root "image\codex.ico"), `
    (Join-Path $root "image\claude.ico"), `
    (Join-Path $root "image\agy.ico") `
    -Destination (Join-Path $app "image") -Force

$installScript = @'
$ErrorActionPreference = "Stop"

$installDir = Join-Path $env:LOCALAPPDATA "ProjectShortcutDock"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Project Shortcut Dock"
$sourceDir = $PSScriptRoot
$appSource = Join-Path $sourceDir "app"

New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -Path (Join-Path $appSource "*") -Destination $installDir -Recurse -Force

New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
$shortcutPath = Join-Path $startMenuDir "Project Shortcut Dock.lnk"
$targetPath = Join-Path $installDir "ProjectShortcutDock.exe"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetPath
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$targetPath,0"
$shortcut.Save()

Start-Process -FilePath $targetPath
'@

Set-Content -LiteralPath (Join-Path $package "install.ps1") -Value $installScript -Encoding UTF8
Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zipPath -Force

Get-Item -LiteralPath $zipPath | Select-Object FullName, Length, LastWriteTime
