# BerryShare 安装包 (WiX v4)

目录包含 WiX v4 安装包定义。核心文件：
- `Product.wxs`  主安装包定义
- `AppFiles.wxs`  由 heat 自动生成（不提交或可提交，视你需要）
- `build-installer.ps1` 自动化脚本

## 基本流程（API 自包含复制到 Shell/assert 后再打包）
1. 发布 API（自包含）：脚本会输出到 `publish/api` 并把需要的 `Ledon.BerryShare.Api.*` 复制进 `src/Ledon.Berry.Shell/assert`。
2. 发布 WPF Shell（自包含）到 `publish/shell`。
3. (可选) 下载 WebView2 引导器放入发布目录。
4. heat 把 `publish/shell` 目录 harvest 成 `AppFiles.wxs`。
5. wix build 生成 `publish/BerryShare.msi`。

## build-installer.ps1 用法
默认执行（发布 API + Shell，复制 API 到 assert，生成 MSI）：
```powershell
pwsh -File .\installer\build-installer.ps1 -DownloadWebView2
```
参数：
- `-SkipApiPublish` 跳过 API 发布（仅重新打包现有文件）。
- `-SingleFileApi` 让 API 以单文件模式发布（复制更少文件）。
- `-Configuration Release` / `Debug` 指定编译配置。
- `-Runtime win-x64` 可改为 `win-arm64` 等。
示例（单文件 API，不下载 WebView2）：
```powershell
pwsh -File .\installer\build-installer.ps1 -SingleFileApi
```

## 自定义
- 修改 `Product.wxs` 中 `<Package Version>` 同步版本；保持 `UpgradeCode` 不变以支持升级。
- 替换 WebView2 检测中的 GUID：在已安装 WebView2 的电脑注册表 `HKLM\SOFTWARE\Microsoft\EdgeUpdate\Clients` 下找到实际子键 GUID。
- 图标：把 `app.ico` 放入发布目录并保持文件名或更改 `<Icon>` 引用。

## 体积优化
- 当前 shell 发布是自包含；若后续出现重复运行时（多项目），建议改为单自包含或框架依赖 + Bundle 链接 .NET Runtime。
- `<MediaTemplate CompressionLevel="high"/>` 已开启最大压缩；对二进制 DLL 压缩余地有限。

## 生成 Bundle (可选)
后续可添加 `Bundle.wxs` 生成单 exe，引导安装 .NET Runtime / WebView2 等。

## 签名
```powershell
signtool sign /fd SHA256 /a /f .\certs\codesign.pfx /p <pwd> ..\publish\BerryShare.msi
```
