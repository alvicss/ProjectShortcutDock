# Project Shortcut Dock

一個給 Windows 使用者，特別是每天在不同專案資料夾之間跳來跳去的開發者，用來整理「專案資料夾捷徑」的小工具。

如果你的桌面、下載資料夾、工作資料夾裡散落著一堆專案，而且你常常在想：「那個專案到底放哪？」這個小工具就是為了這個場景做的。把資料夾拖進來，它會變成一個乾淨的小捷徑列；雙擊就打開資料夾；不想要了就刪掉捷徑，不會碰你的原始專案。

它不是檔案管理器，也不是大型 Launcher。它比較像一個安靜待在桌面上的小抽屜：需要時看一眼，不需要時不要擋路。

## 特色

- 拖曳資料夾到視窗內，自動建立專案捷徑。
- 雙擊捷徑即可用 File Explorer 開啟原始資料夾。
- 選取捷徑後按 `Delete`，只會移除捷徑，不會刪除原本的資料夾。
- 右鍵捷徑可執行開啟、開啟父層資料夾、複製路徑、更換圖示、重設圖示、移除捷徑。
- 可用 `.ico`、常見圖片檔、`.exe` 或 `.dll` 作為自訂圖示來源。
- 視窗寬高可自由調整，位置與尺寸會在下次啟動時自動恢復。
- 捷徑名稱或路徑太長時，支援垂直與水平捲動。
- 內建 Normal、Dark、Glass、Tech、Aero 多種外觀風格。
- 支援兩種視窗模式：
  - `Desktop`：一般工作視窗可以蓋住它，適合當作桌面小工具。
  - `Topmost`：固定在最上層，適合需要一直看得到捷徑的情境。
- 右下角系統匣常駐，可 Show、Hide、Exit。
- 可選擇是否跟著 Windows 開機自動啟動。
- 設定只存放在目前使用者的 Windows 設定資料夾，不會偷搬你的專案。

## 使用情境

這個工具適合：

- 同時維護多個專案的人。
- 不想把一堆捷徑塞滿桌面的人。
- 常常需要開啟不同 repo、設計檔、測試資料夾的人。
- 喜歡小工具安靜待著，但不要一直擋住工作的使用者。

它不會替你管理 Git、不會幫你同步雲端，也不會假裝自己是什麼生產力神兵。它只是把「常開資料夾」整理得順眼一點、好點一點。

## 安裝與執行

目前尚未提供安裝程式。你可以直接從原始碼執行，或自行發佈成 Windows 可執行檔。

從原始碼執行：

```powershell
dotnet run
```

建置專案：

```powershell
dotnet build
```

發佈 Release 版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

發佈後的檔案會出現在類似以下的專案相對路徑：

```text
bin\Release\net6.0-windows\win-x64\publish
```

執行其中的：

```text
ProjectShortcutDock.exe
```

## 基本操作

1. 啟動 `ProjectShortcutDock.exe`。
2. 把專案資料夾拖到小工具視窗裡。
3. 雙擊捷徑列，開啟該資料夾。
4. 選取捷徑後按 `Delete`，移除捷徑。
5. 點齒輪按鈕調整外觀、視窗模式、開機啟動。
6. 從系統匣圖示顯示、隱藏或結束程式。

## 設定檔位置

設定會儲存在目前 Windows 使用者的 AppData 資料夾：

```text
%APPDATA%\ProjectShortcutDock\settings.json
```

這份設定包含：

- 捷徑清單
- 自訂圖示路徑
- 視窗位置
- 視窗寬高
- 外觀主題
- 視窗模式
- 開機啟動偏好

捷徑本身只是設定資料。刪除捷徑不會刪除原始資料夾。

## 系統需求

- Windows 10 或更新版本
- 可執行 `net6.0-windows` 的 .NET Desktop Runtime
- 若要從原始碼建置，需安裝 .NET SDK 6.0 或更新版本，並支援 Windows Desktop 開發

## 專案結構

```text
ProjectShortcutDock.csproj     WPF 專案設定
App.xaml / App.xaml.cs         應用程式資源與全域錯誤處理
MainWindow.xaml                主視窗 UI
MainWindow.xaml.cs             視窗互動、系統匣、捷徑操作、設定套用
AppSettings.cs                 JSON 設定讀寫
ShortcutItem.cs                捷徑資料模型
IconHelper.cs                  Shell 圖示與自訂圖示載入
ThemePalette.cs                外觀主題色票
docs/                          技術文件
```

## 技術文件

想知道它怎麼做到拖曳資料夾、系統匣常駐、圖示載入、Desktop / Topmost 模式與設定持久化，可以看：

[docs/TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md)

## 目前限制

- 右鍵選單是程式內建的實用選單，不是完整的 Windows Explorer 原生右鍵選單。
- `Desktop` 模式使用一般視窗 z-order 行為，讓工作視窗可以蓋住它；沒有把視窗掛進 Explorer 的桌面層，因為那種做法在某些環境下會讓互動變得不穩。
- 目前沒有安裝程式，需要直接執行或自行 publish。

## 未來可能會加

- 安裝程式或單檔發佈設定。
- 捷徑拖曳排序。
- 匯入 / 匯出設定。
- 自訂透明度。
- 自訂捷徑顯示名稱。
- 更完整的 Explorer 右鍵選單整合。

## 授權

目前尚未指定授權條款。若要公開散布，建議先補上明確的 License。
