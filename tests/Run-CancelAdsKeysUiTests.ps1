# 用途：在 Windows PowerShell 的 .NET Framework 中验证实际按键编辑窗口。
param([string]$MagazineCheckPreviewPath)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
$sourcePaths = @(Get-ChildItem -LiteralPath (Join-Path $repoPath 'TarkovHipAimer\src') -Filter '*.cs' |
    ForEach-Object { $_.FullName })
$sourcePaths += Join-Path $PSScriptRoot 'CancelAdsKeysFormTests.cs'
$sourcePaths += Join-Path $PSScriptRoot 'MagazineCheckFormTests.cs'
# 编译到内存，不调用程序入口，不创建测试 EXE 或修改玩家配置。
Add-Type -Path $sourcePaths -ReferencedAssemblies @(
    'System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll',
    'System.Runtime.Serialization.dll', 'System.Xml.dll')
$checks = [CancelAdsKeysFormTests]::Run()
'PASS：取消开镜键编辑窗口 {0} 项' -f $checks
$magazineChecks = [MagazineCheckFormTests]::Run($MagazineCheckPreviewPath)
'PASS：检查弹匣设置窗口 {0} 项' -f $magazineChecks
