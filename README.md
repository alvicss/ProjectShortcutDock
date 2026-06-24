# Project Shortcut Dock

把常用專案資料夾收進一個安靜、透明、隨手可開的小工具。

Project Shortcut Dock 是給 Windows 使用者的桌面捷徑工具。你可以把專案資料夾拖進去，之後雙擊就開、右鍵就進終端機、需要 AI CLI 時也能直接從資料夾位置啟動。它不會搬動你的檔案，不會偷改你的專案，也不會假裝自己是第二個檔案總管。它只做一件事：讓你不用一直瘋狂點擊找檔案。

如果你的桌面已經變成「新增資料夾」、「新增資料夾 (2)」、「真的最後版」的考古現場；又或者像我一樣，每次開舊檔案要編輯，因為資料夾分太多類別與階層需要尋找與點選，這個工具可能可以救你一點點。

## 特色

- **拖放建立捷徑**
  把資料夾拖進視窗，就會加入專案清單。

- **雙擊開啟資料夾**
  不用再從桌面、下載、CDEF 槽、某個神秘備份資料夾一路翻進去。

- **右鍵快速操作**
  開啟資料夾、開啟上層資料夾、複製路徑、變更圖示、重設圖示、移除捷徑。

- **直接在終端機中開啟**
  習慣使用 AI CLI 工作的人可選擇 cmd、Windows PowerShell、PowerShell 7、Git Bash。程式只會列出你電腦上偵測得到的 Shell。

- **AI CLI 快速入口**
  右鍵可直接在該專案資料夾執行：
  - `codex`
  - `CLAUDE`
  - `agy`

- **多語系介面**
  支援繁體中文、英文、日文。第一次執行會依 Windows UI 語系自動判斷，判斷不到就用英文。

- **桌面小工具模式**
  支援 `Desktop` 與 `Topmost`。想安靜待在桌面就 Desktop，想永遠在眼前就 Topmost。

- **可選外觀與圖示**
  內建 Normal、Dark、Glass、Tech、Aero 樣式，也可替每個捷徑換圖示。

- **系統匣常駐**
  可以顯示、隱藏、結束。它會在右下角乖乖待命。

## 安裝

到 GitHub Releases 下載：

```text
ProjectShortcutDock-Setup-0.1.1.exe
```

雙擊安裝即可。安裝器會：

1. 檢查是否已安裝 `.NET 10 Desktop Runtime`。
2. 如果沒有，詢問是否安裝內嵌的 Microsoft 官方 `.NET 10 Desktop Runtime` 離線安裝包。
3. 將程式安裝到目前使用者資料夾。
4. 建立開始功能表捷徑與解除安裝入口。
5. 安裝完成後啟動 Project Shortcut Dock。

如果 runtime 自動安裝失敗，安裝器會顯示原因，並附上 Microsoft 官方手動安裝連結。

安裝位置：

```text
%LOCALAPPDATA%\ProjectShortcutDock
```

也提供 ZIP 版本：

```text
ProjectShortcutDock-0.1.1-win-x64.zip
```

解壓縮後執行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1
```

## 使用方式

1. 啟動 Project Shortcut Dock。
2. 把專案資料夾拖進視窗。
3. 雙擊捷徑開啟資料夾。
4. 右鍵捷徑使用更多操作。
5. 點齒輪調整樣式、視窗模式、語系、Shell、是否隨 Windows 自動啟動。
6. 不想看到它時，右上方打叉隱藏到系統匣就好。

## Shell 與 AI CLI

設定裡的 `Shell` 下拉選單會自動偵測：

- `cmd.exe`
- Windows PowerShell
- PowerShell 7
- Git Bash

右鍵選單的 AI CLI 選項會依你選擇的 Shell 執行。
如果你的電腦沒有安裝對應 CLI，Shell 會顯示找不到命令。這是正常的，程式不會替你偷偷安裝那些工具。

## 設定存在哪裡

使用者設定儲存在：

```text
%APPDATA%\ProjectShortcutDock\settings.json
```

這裡會記錄：

- 捷徑清單
- 自訂圖示路徑
- 視窗位置與大小
- 樣式
- 視窗模式
- 語系
- Shell 偏好
- 是否隨 Windows 啟動

錯誤紀錄會寫在：

```text
%APPDATA%\ProjectShortcutDock\last-error.log
```

這些資料都在你自己的 Windows 使用者資料夾裡。Project Shortcut Dock 不會把設定上傳到任何地方。

## 要怎麼移除

安裝版可直接從 Windows 的「已安裝的應用程式」移除，或從開始功能表執行：

```text
Uninstall Project Shortcut Dock
```

解除安裝會移除：

- `%LOCALAPPDATA%\ProjectShortcutDock`
- 開始功能表捷徑
- Windows 的解除安裝登錄資訊
- 隨 Windows 啟動登錄值

解除安裝時會再詢問你是否連 `%APPDATA%\ProjectShortcutDock` 的設定與捷徑清單一起刪除。

## 會不會刪掉我的專案？

不會。

「移除捷徑」只會從 Project Shortcut Dock 的清單中移除那筆資料，不會刪除原始資料夾。你的專案還在原地，除非你自己去檔案總管刪它。

## 從原始碼建置

需求：

- Windows 10 或更新版本
- .NET 10 SDK

建置：

```powershell
dotnet build
```

如果使用專案本機 SDK：

```powershell
.\.dotnet\dotnet.exe build
```

產生 Release 安裝包：

```powershell
.\tools\build-release.ps1 -Version 0.1.1
```

輸出檔案：

```text
artifacts\ProjectShortcutDock-Setup-0.1.1.exe
artifacts\ProjectShortcutDock-0.1.1-win-x64.zip
```

## 技術文件

想看架構、設定格式、Shell 偵測、安裝器流程，可以看：

[docs/TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md)

## 授權

本專案提供個人、學習、測試與非商業用途免費使用。

未經作者書面同意，不得將本工具或其修改版本用於商業販售、重新包裝銷售、付費服務、商業產品整合，或移除作者資訊後作為自己的商業產品發布。

詳細條款請看 [LICENSE](LICENSE)。

## 免責聲明

本工具是個人學習、實作與經驗分享作品，部分內容可能透過 AI 協助修正與整理。

由於每位使用者的系統環境、軟體版本、權限設定、Shell 設定、.NET Runtime 狀態與操作方式不同，實際結果可能有所差異。請在操作前自行評估風險，並備份重要資料。

因使用、安裝、修改、建置或參考本工具所造成的任何系統異常、資料遺失、設定錯誤、設備故障、服務中斷或其他直接、間接損失，均由使用者自行承擔，作者不負任何相關責任。

## 更多作品

https://halfmemo.com/about/
