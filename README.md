# Project Shortcut Dock

Project Shortcut Dock 是一個 Windows 桌面小工具，用來管理常用專案資料夾捷徑。把資料夾拖進視窗後，它會變成一列捷徑；雙擊可開啟資料夾，右鍵可開啟上層資料夾、複製路徑、換圖示，或直接在指定 Shell 中開啟專案。

這個工具不會刪除、搬移或修改你的原始專案資料夾。它只保存捷徑清單與個人偏好設定。

## 功能

- 拖放資料夾建立專案捷徑。
- 雙擊捷徑開啟資料夾。
- 右鍵選單支援開啟、開啟上層資料夾、複製路徑、變更圖示、重設圖示、移除捷徑。
- 右鍵選單支援在 Shell 中開啟資料夾，並可直接執行 `codex`、`CLAUDE`、`agy`。
- Shell 可在設定中選擇，程式會自動列出本機偵測得到的 `cmd.exe`、Windows PowerShell、PowerShell 7、Git Bash、WSL。
- 支援繁體中文、英文、日文介面。
- 第一次執行會自動偵測 Windows UI 語系；不支援時預設英文。
- 第一次執行預設視窗模式為 `Topmost`，樣式為 `Normal`。
- 支援 `Desktop` 與 `Topmost` 視窗模式。
- 支援 Normal、Dark、Glass、Tech、Aero 樣式。
- 支援自訂捷徑圖示，可使用 `.ico`、圖片、`.exe`、`.dll`。
- 視窗位置、大小、語系、Shell、樣式、視窗模式都會保存。
- 支援系統匣顯示、隱藏與結束。
- 可設定是否隨 Windows 啟動。

## 安裝

建議使用 GitHub Release 下載安裝包：

```text
ProjectShortcutDock-0.1.0-win-x64.zip
```

解壓縮後執行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1
```

目前安裝包是 framework-dependent 版本，使用者電腦需要先安裝 .NET 10 Desktop Runtime。若尚未安裝，請到 Microsoft 官方網站安裝 `.NET Desktop Runtime 10`。

執行安裝檔後，程式會安裝到目前使用者資料夾：

```text
%LOCALAPPDATA%\ProjectShortcutDock
```

安裝不需要系統管理員權限。使用者設定會另外儲存在：

```text
%APPDATA%\ProjectShortcutDock\settings.json
```

## 使用方式

1. 啟動 Project Shortcut Dock。
2. 把專案資料夾拖放到視窗。
3. 雙擊捷徑開啟資料夾。
4. 右鍵捷徑使用更多操作。
5. 點齒輪調整樣式、視窗模式、語系、Shell、開機啟動。
6. 不需要顯示時可隱藏到系統匣。

## 右鍵 Shell 功能

右鍵選單提供：

- 在終端機中開啟
- 用 Codex 開啟
- 用 Claude 開啟
- 用 Antigravity 開啟

Shell 下拉清單只會顯示本機偵測得到的工具。預設是 `cmd.exe`。如果選擇 PowerShell 7，右鍵的 AI 工具命令會在 PowerShell 7 中執行。

## 設定與隱私

程式只會在目前使用者目錄保存設定：

```text
%APPDATA%\ProjectShortcutDock\settings.json
%APPDATA%\ProjectShortcutDock\last-error.log
```

`settings.json` 可能包含你的本機資料夾路徑與自訂圖示路徑，因此不應提交到 GitHub。此專案的 `.gitignore` 已排除常見建置輸出、發佈輸出、暫存檔與 log。

## 從原始碼建置

需求：

- Windows 10 或更新版本
- .NET 10 SDK

建置：

```powershell
.\.dotnet\dotnet.exe build
```

或使用系統安裝的 SDK：

```powershell
dotnet build
```

發佈 self-contained 版本：

```powershell
.\tools\build-release.ps1 -Version 0.1.0
```

產出的 GitHub Release 檔案會在：

```text
artifacts\ProjectShortcutDock-0.1.0-win-x64.zip
```

## 專案結構

```text
ProjectShortcutDock.csproj     WPF 專案設定
App.xaml / App.xaml.cs         應用程式資源與全域錯誤處理
MainWindow.xaml                主視窗 UI
MainWindow.xaml.cs             主視窗互動、右鍵選單、Shell、系統匣、設定套用
AppSettings.cs                 使用者設定讀寫與第一次執行預設
ShortcutItem.cs                捷徑資料模型
IconHelper.cs                  Shell 圖示與自訂圖示載入
ThemePalette.cs                內建樣式色票
image/                         右鍵選單 AI 工具圖示
tools/build-release.ps1        產生 GitHub Release 安裝包
docs/                          技術文件
```

## 技術文件

詳細架構請看：

[docs/TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md)

## 授權

公開到 GitHub 前，建議補上明確授權條款，例如 MIT License。
