# 技術架構

本文說明 Project Shortcut Dock 的技術選型、主要模組、資料保存方式、Windows 整合與發佈方式。

## 目標

Project Shortcut Dock 是一個 Windows WPF 桌面工具，核心目標是讓使用者快速整理與開啟常用專案資料夾。程式只管理捷徑資料，不會刪除或修改原始專案資料夾。

## 技術棧

| 類別 | 技術 |
| --- | --- |
| Runtime | .NET 10 |
| Target Framework | `net10.0-windows` |
| UI | WPF |
| 系統匣 | Windows Forms `NotifyIcon` |
| 設定儲存 | `System.Text.Json` |
| 圖示載入 | Win32 `SHGetFileInfo`、WPF Imaging、`System.Drawing.Icon` |
| 開機啟動 | HKCU Registry Run key |
| 視窗層級 | Win32 `SetWindowPos` |
| 發佈 | framework-dependent win-x64 Setup EXE 與 ZIP |

專案目前不依賴第三方 NuGet 套件，降低建置與審查成本。

## 啟動流程

1. `App.xaml.cs` 註冊全域例外處理。
2. `MainWindow` 初始化 UI。
3. `AppSettings.Load()` 載入使用者設定。
4. 若沒有設定檔，建立第一次執行預設值。
5. 偵測可用 Shell。
6. 初始化設定下拉選單。
7. 載入捷徑清單並重新產生圖示。
8. 套用語系、樣式、視窗模式、位置與大小。
9. 建立系統匣圖示。

## 第一次執行預設

第一次執行的判斷依據是設定檔是否存在：

```text
%APPDATA%\ProjectShortcutDock\settings.json
```

若不存在，預設值如下：

- `Theme = Normal`
- `WindowMode = Topmost`
- `Language = 依 Windows UI 語系偵測`
- 偵測不到支援語系時使用英文
- `TerminalShell = cmd`

語系偵測目前支援：

- `zh*` -> `zh-TW`
- `ja*` -> `ja`
- 其他 -> `en`

## 設定資料

設定類別位於 `AppSettings.cs`。主要欄位包含：

- `Theme`
- `WindowMode`
- `Language`
- `TerminalShell`
- `StartWithWindows`
- `Left`
- `Top`
- `Width`
- `Height`
- `Shortcuts`

設定以 JSON 儲存在使用者 AppData，不會寫入 repo 或專案資料夾。

## UI 架構

`MainWindow.xaml` 是單一主視窗，包含：

- 標題列
- 設定面板
- 捷徑清單

設定面板提供：

- 樣式
- 視窗模式
- 語系
- Shell
- 隨 Windows 啟動

捷徑清單使用 `ListBox` 與 `DataTemplate`，每個 item 顯示資料夾圖示、捷徑名稱與路徑。

## 右鍵選單

右鍵選單由 `MainWindow.xaml.cs` 以程式碼建立，避免 XAML 裡放過多事件 wiring。

功能包含：

- 開啟
- 開啟上層資料夾
- 複製路徑
- 在終端機中開啟
- 用 Codex 開啟
- 用 Claude 開啟
- 用 Antigravity 開啟
- 變更圖示
- 重設圖示
- 移除捷徑

一般選單項目使用 Segoe MDL2 Assets 系統圖示。Codex、Claude、Antigravity 使用 `image/` 目錄內的 `.ico`。應用程式與系統匣圖示使用 `image/project-shortcut-dock.ico`。

## Shell 偵測與啟動

啟動時會偵測可用 Shell，並只把存在的項目放入設定下拉清單。

目前支援：

- `cmd.exe`
- Windows PowerShell
- PowerShell 7
- Git Bash
- WSL

預設是 `cmd.exe`。若使用者曾選擇的 Shell 後來不存在，會回到 `cmd.exe`。

右鍵 AI 功能會依使用者選擇的 Shell 執行：

- `codex`
- `CLAUDE`
- `agy`

程式不檢查這些 CLI 是否已安裝。若未安裝，Shell 會顯示找不到命令。

## 圖示載入

`IconHelper.cs` 負責圖示載入。

預設流程：

1. 若使用者指定自訂圖示，先嘗試載入。
2. 支援 `.ico`、圖片、`.exe`、`.dll`。
3. 自訂圖示失敗時，回退到 Windows Shell 資料夾圖示。
4. 轉為 WPF `ImageSource` 並 `Freeze()`，供 UI 綁定使用。

## 視窗模式

### Topmost

設定 WPF `Topmost = true`，視窗會保持在最上層。

### Desktop

不設為 topmost，並使用 Win32 `SetWindowPos` 搭配 `HWND_BOTTOM`，讓一般工作視窗可覆蓋小工具。

程式沒有把視窗掛到 Explorer WorkerW 桌面層，因為該方式在部分環境會造成互動不穩定。

## 系統匣

WPF 沒有內建系統匣元件，因此使用 Windows Forms `NotifyIcon`。

系統匣選單提供：

- 顯示
- 隱藏
- 結束

點視窗關閉時預設隱藏到系統匣，只有系統匣的結束會真正關閉程式。

## 開機啟動

開機啟動使用目前使用者的 Registry Run key：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

value name：

```text
ProjectShortcutDock
```

此設定不需要系統管理員權限。

## 錯誤紀錄

未處理例外會寫入：

```text
%APPDATA%\ProjectShortcutDock\last-error.log
```

錯誤紀錄可能含本機路徑，不應提交到 GitHub。

## Git 與發佈安全

`.gitignore` 應排除：

- `bin/`
- `obj/`
- `.dotnet/`
- IDE 設定
- 發佈輸出
- 安裝包輸出
- log、暫存檔、備份檔
- `.env`、`NuGet.Config`、`settings.json` 等可能含個人資訊或私有來源的檔案
- `.pdb` 符號檔，避免把本機原始碼路徑放進 release

使用者個人設定儲存在 AppData，不在 repo 內。若未來加入匯出設定功能，需避免把 `settings.json` 直接提交。

## 發佈方式

建議流程：

1. `dotnet build -c Release` 產生 framework-dependent Release 輸出。
2. 將必要執行檔、`.dll`、`.deps.json`、`.runtimeconfig.json` 與 `image/` 圖示放入安裝包暫存資料夾。
3. 產生 `install.ps1`，安裝到 `%LOCALAPPDATA%\ProjectShortcutDock` 並建立開始功能表捷徑。
4. 壓縮成 `ProjectShortcutDock-{version}-win-x64.zip`，供手動安裝。
5. 將 app 檔案壓成內嵌資源，使用 Windows .NET Framework `csc.exe` 編譯 `tools/SetupBootstrapper.cs`。
6. 產生 `ProjectShortcutDock-Setup-{version}.exe`，供一般使用者雙擊安裝。

發佈檔不建議提交到 git，應放在 GitHub Releases。

## 安裝器行為

`ProjectShortcutDock-Setup-{version}.exe` 是 bootstrapper：

- 內嵌 Project Shortcut Dock app 檔案。
- 偵測 `Microsoft.WindowsDesktop.App 10.x` 是否存在。
- 若不存在，詢問使用者是否安裝內嵌的 Microsoft 官方 `.NET 10 Desktop Runtime` 離線安裝程式。
- 安裝 app 到 `%LOCALAPPDATA%\ProjectShortcutDock`。
- 建立開始功能表捷徑。
- 安裝完成後啟動程式。
- 若 runtime 自動安裝失敗，顯示原因與 Microsoft 官方手動安裝連結。

目前內嵌的 runtime 安裝檔是 Microsoft 官方 `.NET Desktop Runtime 10.0.9 win-x64` 離線安裝程式。
