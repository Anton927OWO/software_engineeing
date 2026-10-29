# 朱峻廷：書海尋寶、草澤擋鹿

此資料夾先提交目前完成的獨立 Unity 專案，供組員檢視與後續整合。

## 開啟

在 Unity Hub 選擇「從硬碟新增專案」，指定本目錄中的 `UnityProject`，使用 Unity **6000.3.25f1** 開啟，再開啟 `Assets/Junting/Scenes/JuntingMain.unity` 按 Play。

本版只有朱峻廷負責的兩關，按「開始探索」後隨機決定先後順序，每輪不重複。兩關均含前置解說、遊戲、結尾兩題問答、通關一件文物知識卡，並各用不同 BGM。

## 本次提交範圍

- 原始碼、場景、素材、Unity 套件及專案設定、測試與素材授權。
- 未包含 Library、Temp、Logs、UserSettings；已實測 WebGL 建置以 ZIP 收錄於 Builds，透過 Git LFS 保存。
- 本目錄是獨立專案快照，尚未接入儲存庫根目錄的 `TrackingNIU` 主系統。根專案目前使用 6000.3.19f1，本提交保留原團隊設定。
- 全組文物配置與示範版不同，合併時應以主系統設定為準；本次不修改全組 `LevelCatalog`。
- 五樓中文圖書區已換成使用者提供的實景照片；修正匯入尺寸設定，保留照片比例。

操作、重建 WebGL、供主系統接入的 API 請參考 [專案說明](UnityProject/README.md)。測試結果及測試範圍請參考 [驗證紀錄](UnityProject/驗證紀錄.md)。

## 目前測試版（2026/10/29）

- Unity Play Mode 18 項流程檢查通過；Unity 內容與規則驗證 35 項通過。
- WebGL 已在桌面 Chromium 實際操作兩關，完成書本拖放、擋板與鹿群、結尾問答、各一件文物，以及本輪完成；主控台無 error。
- [下載 WebGL 實景照片測試版](Builds/Junting_WebGL_PhotoTest.zip)：GitHub 檔案頁按 Download raw file 取得完整 ZIP；解壓後透過 HTTP/HTTPS 主機開啟，不能直接以 file:// 遊玩。
- 實測畫面：[中文圖書區](Verification/WebGL_LibraryPhoto.png)、[兩關完成](Verification/WebGL_JourneyComplete.png)。
- 此版仍為獨立 Unity 專案，尚未接入團隊主流程；不改動儲存庫根目錄的團隊專案。
