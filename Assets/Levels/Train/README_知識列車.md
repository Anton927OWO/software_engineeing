# 知識列車

Unity **6000.3.19f1**。開啟 `Assets/Levels/Train/L_Train.unity` 按 Play，閱讀空間介紹後按「開始探索」。也可使用選單「尋跡宜大 > Train > 開啟知識列車場景」。Scene 已由原生 Editor API 建立並隨檔交付，不需要先跑生成器。

## 操作與規則

- 滑鼠點選 A／B／C 選項。在 60 秒內答對五題，每題接上一節車廂；第五節完成連接後，整列火車開出畫面才成功。
- 答錯顯示正確答案一秒，失誤增加一，不扣車廂。這一秒不能重複作答。
- 一輪以 Fisher–Yates 洗牌；有效題目 ID 不重複抽取。時間到或有效題目用完且車廂未達標則失敗。
- 接車 0.7 秒、出站 2.2 秒，兩種動畫都計入 60 秒；即使已有五節，出站前逾時仍失敗。
- 「空間介紹」重看規則；「知識卡／來源」閱讀上一題的正解、解說、官方網址及查核日期。第一題未作答時只顯示本題來源，不提前揭答。
- 介紹、知識卡、右上「暫停」與 Escape 均沿用 Shared 暫停；動畫也一起停止。暫停面板的「重新開始」或結果面板的「再試一次」會原地清空車廂、時間、失誤、題序及結果，重新顯示介紹。

## 編輯題庫

在 Inspector 選取 `Data/TrainQuestions.asset`，編輯 questions。預設 12 題，不需要改程式。

| 欄位 | 用法 |
| --- | --- |
| id | 非空且唯一；同 ID 的重複資料只採第一筆 |
| prompt | 題目 |
| options | 恰好三個不同且非空選項 |
| correctIndex | 正解索引：0=A、1=B、2=C |
| explanation | 答題後知識卡顯示的解說 |
| sourceTitle / sourceUrl | 官方資料名稱與完整 HTTPS 網址 |
| verified / verifiedOn | 查核完成才勾選並填日期。新增／修改尚未查核內容請取消勾選，畫面會標示「待驗證」 |

格式錯誤的題目會略過；沒有有效題目時開始後安全失敗。正式題庫請維持至少十題。`TrainQuestions.seed.json` 僅用於首次生成缺少的題庫，不會覆蓋 Inspector 已編輯的 asset。

預設題目於 **2026-10-26（Asia/Taipei）**逐題對照以下官方資料：

| 題目 ID | 官方來源與支持內容 |
| --- | --- |
| history-1926、history-name、history-1946、history-1967、history-1969、history-1988、history-1998、history-2003 | [國立宜蘭大學｜校史](https://www.niu.edu.tw/p/412-1000-1049.php)：創校及歷次改制／校名；西元年份由民國年加 1911 換算 |
| motto | [國立宜蘭大學｜校訓](https://www.niu.edu.tw/p/412-1000-1310.php)：篤學、力行、敬業、樂群 |
| location、station | [國立宜蘭大學｜交通位置](https://www.niu.edu.tw/p/412-1000-1052.php)：校本部城市及台鐵到站指引 |
| archaeology | [宜大圖資館｜校史緣起](https://lic.niu.edu.tw/p/412-1010-2275.php)：校園遺址名稱 |

「校史車站」為關卡情境，未宣稱校內存在實際鐵道或車站。題目與解說是依來源重新撰寫。

## 接入與結果

Scene 保留真正的 `SharedLevelRoot.prefab` 實例，僅以 Scene instance overrides 指定 TrainLevel 設定、Train 專用字型與內容文字區寬度。Train 不修改 Shared 原始資產。唯一 Camera／AudioListener 及 InputSystemUIInputModule 已配置。

`SharedLevelController.Completed`、`LevelResultRelay` 與 `LastResultJson` 都直接沿用 Shared。`levelId` 為 `Train`；每答對一題收集 `train-carriage-01` 至 `train-carriage-05`，artifactIds 代表本次已取得的車廂，失敗也保留當次已取得的 ID。結果的 success、remainingTime、mistakeCount 由同一 Shared Session 提供，每次嘗試只送一次。

不修改 Build Settings、不自動切換其他關卡、不寫跨關存檔。其他流程可訂閱 Completed 或接到既有 relay receiver。

## 檔案

- `L_Train.unity`：可直接開啟的原生 Scene。
- `Runtime/TrainRound.cs`：題序、判題、動畫階段及終局。
- `Runtime/TrainLevelController.cs`：玩法與已序列化 UI／車廂的連接。
- `Runtime/TrainQuestion.cs`、`TrainQuestionBank.cs`：可編輯題庫格式。
- `Editor/TrainSceneBuilder.cs`：建立缺少資產，保留既有 Scene／題庫／Build Settings，不儲存不相關 dirty 資產；未命名且有內容的 Scene 需先另存。
- `Data`、`Fonts`：Train 專用設定、題庫與 TMP 字型副本，避免動態補字修改 Shared。
- `Tests/Editor`、`Tests/PlayMode`：原生 Unity 測試。
- `Verification`、`驗證報告.md`：原生測試 XML、實際渲染畫面與驗證結果。

## 重跑測試

Unity Test Runner 執行 `XunJiYiDa.Levels.Train.Editor.Tests` 及 `XunJiYiDa.Levels.Train.PlayMode.Tests`，亦可一起選取 Shared 測試。

批次測試：`Unity -batchmode -projectPath <專案路徑> -runTests -testPlatform EditMode -testResults <輸出.xml> -logFile <輸出.log>`；PlayMode 改為 `-testPlatform PlayMode`。專案已被其他 Editor 開啟時，請使用關閉後的原專案或獨立副本，避免第二個 Editor 開啟同一專案。

設定環境變數 `NIU_TRAIN_SCREENSHOT_DIR` 並使用具有圖形裝置的 PlayMode 執行，可擷取 1920×1080 原生 Metal 畫面。測試不依賴此環境變數。
