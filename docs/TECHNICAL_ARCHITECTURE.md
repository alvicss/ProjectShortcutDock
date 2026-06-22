# 技術棧與架構說明

本文說明 Project Shortcut Dock 的技術選型、程式架構、Windows 整合方式與主要設計取捨。

## 整體概念

Project Shortcut Dock 是一個 Windows 桌面小工具，使用 WPF 與 .NET 建置。它的核心工作很單純：

1. 接收使用者拖曳進來的資料夾。
2. 將資料夾記錄成捷徑資料。
3. 在小型桌面視窗中顯示捷徑清單。
4. 透過 Windows Shell 開啟原始資料夾。
5. 將設定儲存在使用者層級的 JSON 檔案。

目前專案刻意不依賴外部 NuGet 套件，主要使用 .NET、WPF、Windows Forms interop 與少量 Win32 API。

## 技術棧

| 層級 | 技術 | 用途 |
| --- | --- | --- |
| Runtime | .NET 6, `net6.0-windows` | Windows 桌面應用程式執行環境 |
| UI Framework | WPF | 視窗、版面、樣式、資料繫結、事件處理 |
| 系統匣 | Windows Forms `NotifyIcon` | 右下角系統匣圖示與選單 |
| Shell 操作 | `ProcessStartInfo` + `UseShellExecute` | 透過 Windows Shell 開啟資料夾 |
| 圖示載入 | Win32 `SHGetFileInfo`、WPF Imaging、`System.Drawing.Icon` | 載入資料夾圖示與自訂圖示 |
| 開機啟動 | Windows Registry | 寫入目前使用者的 Run key |
| 設定儲存 | `System.Text.Json` | 將設定與捷徑資料序列化成 JSON |
| 視窗層級 | Win32 `SetWindowPos` | Desktop 模式下將視窗送到一般工作視窗後方 |

## 應用程式入口

### `App.xaml`

定義應用程式層級的 WPF 資源，例如預設筆刷與顏色資源。

### `App.xaml.cs`

負責全域錯誤處理：

- `AppDomain.CurrentDomain.UnhandledException`
- WPF `DispatcherUnhandledException`

啟動或 UI 執行期間若發生未處理例外，會寫入：

```text
%APPDATA%\ProjectShortcutDock\last-error.log
```

每次啟動時會先清除上一份錯誤紀錄，避免舊錯誤誤導除錯。

## UI 架構

### `MainWindow.xaml`

主視窗是一個無標準標題列、支援透明背景的 WPF 視窗。主要區塊包含：

- 標題列：顯示名稱、設定按鈕、隱藏按鈕。
- 設定面板：可收合，包含外觀、視窗模式、開機啟動。
- 捷徑清單：使用 `ListBox` 與自訂 item template。
- 視窗調整：使用 WPF `ResizeMode="CanResizeWithGrip"`。

捷徑清單放在 `Grid` 的 star-sized row 裡，因此會吃滿剩餘高度。這樣可以確保：

- 垂直捲軸會貼齊清單右側。
- 水平捲軸會貼齊清單底部。
- 使用者改變視窗寬高時，捲軸位置跟著正確調整。

### `MainWindow.xaml.cs`

主視窗的互動邏輯集中在這裡：

- 載入與儲存設定。
- 初始化系統匣圖示。
- 處理資料夾拖曳。
- 處理雙擊開啟資料夾。
- 處理 `Delete` 移除捷徑。
- 以程式碼建立捷徑右鍵選單。
- 套用主題色票。
- 套用 `Desktop` / `Topmost` 視窗模式。
- 儲存視窗位置與尺寸。

視窗設定為 `ShowInTaskbar="False"`，讓它更像一個系統匣常駐小工具，而不是一般工作列程式。

## 資料模型

### `ShortcutItem`

代表一筆捷徑資料：

- `Name`：顯示名稱。
- `Path`：原始資料夾路徑。
- `IconPath`：選填，自訂圖示來源。
- `IconSource`：執行期間使用的 WPF 圖片來源。

`IconSource` 使用 `JsonIgnore`，因為它不是設定資料，而是由 `Path` 或 `IconPath` 在執行期間重新產生。

### `AppSettings`

代表使用者設定：

- 外觀主題。
- 視窗模式。
- 是否開機啟動。
- 視窗位置。
- 視窗寬高。
- 捷徑清單。

設定序列化後儲存在：

```text
%APPDATA%\ProjectShortcutDock\settings.json
```

這是使用者層級設定檔，不會寫入專案資料夾，也不需要系統管理員權限。

## 設定讀寫流程

1. 程式啟動時呼叫 `AppSettings.Load()`。
2. 讀取 JSON 設定檔。
3. 過濾已不存在的資料夾捷徑。
4. 為每個有效捷徑重新載入圖示。
5. 依照設定初始化 UI 控制項。
6. 使用者變更設定或捷徑時，呼叫 `SaveSettings()` 寫回 JSON。

移除捷徑只會移除 JSON 中的 metadata，不會刪除原始資料夾。

## Windows Shell 整合

### 開啟資料夾

資料夾開啟透過 Windows Shell：

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = path,
    UseShellExecute = true
});
```

這會讓 Windows 使用標準 Explorer 行為開啟資料夾。

### 載入資料夾圖示

預設資料夾圖示透過 Win32 `SHGetFileInfo` 取得。自訂圖示載入順序如下：

1. 圖片檔：`.png`、`.jpg`、`.jpeg`、`.bmp`
2. 圖示檔：`.ico`
3. 可抽取關聯圖示的 `.exe` 或 `.dll`
4. 退回 Windows Shell 資料夾圖示

最後會轉成 WPF `ImageSource`，並使用 `Freeze()` 讓它適合資料繫結使用。

## 系統匣

WPF 本身沒有內建系統匣控制項，因此使用 Windows Forms 的 `NotifyIcon`。

系統匣選單提供：

- Show
- Hide
- Exit

一般關閉視窗時，程式會隱藏到系統匣；只有透過 Exit 才會真正結束。

## 開機啟動

開機啟動使用目前使用者的 Registry Run key：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Value name：

```text
ProjectShortcutDock
```

Value data 會指向目前執行檔位置。

這是使用者層級設定，不需要修改系統層級啟動項。

## 視窗模式

### Desktop Mode

`Desktop` 是預設模式。視窗不會設為 topmost，並透過 `SetWindowPos` 搭配 `HWND_BOTTOM` 送到一般工作視窗後方。

這種做法的目標是讓小工具在桌面上可見，但當使用者開啟其他工作視窗時，不會一直擋在最前面。

曾經測試過將視窗掛到 Explorer 的 WorkerW 桌面 host，但在部分環境會讓點擊與互動變得不穩，因此目前改用一般視窗 z-order 行為。

### Topmost Mode

`Topmost` 模式會設定 WPF `Topmost = true`。

這時小工具會固定在其他視窗上方，適合需要長時間盯著捷徑清單的使用者。

## 主題系統

主題色票定義在 `ThemePalette.cs`，以 immutable record 表示。主視窗套用主題時會替換 WPF resource：

- `WindowBrush`
- `PanelBrush`
- `TextBrush`
- `SubtleTextBrush`
- `BorderBrush`
- `HoverBrush`

目前內建主題：

- Normal
- Dark
- Glass
- Tech
- Aero

## 錯誤處理

未處理的啟動或 UI 例外會寫入：

```text
%APPDATA%\ProjectShortcutDock\last-error.log
```

這讓使用者不必面對複雜錯誤畫面，同時保留開發與除錯需要的資訊。

## 主要設計取捨

- **使用 WPF 而不是純 WinForms：** WPF 更適合做透明視窗、版面控制、資料模板與主題樣式。
- **不使用外部套件：** 降低建置成本，也讓專案容易審查。
- **使用 JSON 而不是 `.lnk`：** 捷徑是應用程式 metadata，不需要建立或刪除真實 Windows 捷徑檔。
- **使用內建右鍵選單：** 先提供最常用操作，避免 Shell COM context menu 帶來的複雜度。
- **視窗尺寸由使用者控制：** 不用固定列數，使用者拉多高就記住多高。

## 未來可改進方向

- 提供安裝程式或單檔 publish 設定。
- 支援捷徑拖曳排序。
- 支援設定匯入 / 匯出。
- 支援自訂透明度。
- 支援自訂捷徑別名。
- 整合完整 Explorer 原生右鍵選單。
- 補上 first-run 空狀態畫面。
- 補上 UI smoke test，驗證拖曳、設定儲存、系統匣與視窗模式。
