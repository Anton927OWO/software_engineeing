# 百年軌跡

Unity **6000.3.19f1**。開啟 `Assets/Levels/Timeline/L_Timeline.unity` 後按 Play。本次在原 Scene 上局部改善，保留五卡五格、年份、時間軸、Shared Prefab 實例及結果橋接，沒有重製玩法。

## 操作與規則

1. 閱讀精簡的操作說明，按「開始探索」後開始 **45 秒**倒數。正式 GameManager 的限時仍由既有 LevelContext 決定。
2. 將下方五張學制卡拖至對應年份。支援滑鼠左鍵與觸控拖曳，拖曳保留按下的位置。
3. 正確卡吸附固定，並立即顯示該年代的一句歷史變化。
4. 放錯、放到已佔用位置或放在格外會彈回；一次拖曳只增加一次失誤。每張卡有不同的簡短學制線索，不直接給答案年份。**失誤不限次數，五張全對才通關，時間歸零仍失敗。**
5. 「年代提示」沿用原知識卡，可前後瀏覽五階段、閱讀文化連結、開啟官方出處，也能重看操作說明。閱讀期間不扣時間；原時間軸提示持續保留。
6. 成功與超時結果保留原得分、重試及繼續功能，另可瀏覽五階段摘要；固定從第一階段（1926）開始。瀏覽不改分數、不重送結果，也不自動退出。
7. 右上「暫停」或 Esc 切換手動暫停。「重新開始／再試一次」原地清除進度、失誤、拖曳與瀏覽狀態，重新洗牌並顯示介紹。關閉知識卡不會解除手動或失焦暫停。

主畫面的重複介紹入口移入年代提示，原介紹功能仍可使用。五卡保留原插圖與年份裁切，另以五種色彩和幾何紋樣協助辨識；**這些圖形是遊戲設計，不是歷史建築、印記或文物復原。**

## 學習設計與來源

`Runtime/TimelineHistory.cs` 集中保存不可變的五階段資料。每個歷史變化的 `SourceUrl` 與每個文化連結的 `ConnectionSourceUrl` 均有官方網址；遊戲中的「校史出處／文化出處」按鈕依目前頁面開啟對應來源。來源查核日：**2026-10-29**。

| 階段 | 操作中學習的重點 | 官方校史 |
| --- | --- | --- |
| 1926 | 臺北州立宜蘭農林學校的農林教育起點 | [校史](https://www.niu.edu.tw/p/412-1000-1049.php) |
| 1946 | 更名為臺灣省立宜蘭農業職業學校；仍屬職業教育 | [校史](https://www.niu.edu.tw/p/412-1000-1049.php) |
| 1988 | 改制國立宜蘭農工專科學校，辨識專科階段 | [校史](https://www.niu.edu.tw/p/412-1000-1049.php) |
| 1998 | 升格國立宜蘭技術學院，辨識專科與學院的轉變 | [校史](https://www.niu.edu.tw/p/412-1000-1049.php) |
| 2003 | 更名國立宜蘭大學，辨識大學階段 | [校史](https://www.niu.edu.tw/p/412-1000-1049.php) |

西元年由官方校史的民國年加 1911 換算，校名以繁體「臺」統一。五卡是主要節點，並非完整校名史；期間還有 1967、1969 年職校更名，知識卡與摘要皆附提示。[官方校史](https://www.niu.edu.tw/p/412-1000-1049.php)

「文化連結（非當年事件）」明確區分當年校史與今日文化觀察，讓玩家在操作時讀到：農林教育回應地方需求、校史與教育制度的演變，以及宜大記憶庫保存校園生活、連結噶瑪蘭文化廊道與萬斌廳考古展示的方式。出處：[校方開展報導](https://www.niu.edu.tw/p/406-1000-56719%2Cr43.php)、[圖資館宜大記憶庫](https://lic.niu.edu.tw/p/404-1010-63587.php)、[百年校史系列專題](https://lic.niu.edu.tw/p/412-1010-3425.php)。不新增文物用途、年代或館舍規則。

## 共用相容性

- 真正序列化的 Scene，沿用 `SharedLevelRoot.prefab`、Noto Sans TC、音效、安全區與一組 Camera／Input System EventSystem。
- 本關獨立 `TimelineDefinition.asset` 保留 `levelId = Timeline`、`durationSeconds = 45`、`mistakeLimit = 0`。
- `TimelineBoard` 仍透過 Shared 的 `RecordMistake()`、`Succeed()` 與 `Retried` 管理互動，時間歸零由 Shared 處理。非 Running 不接受拖放，狀態中斷取消拖曳且不算失誤。
- `TimelineHistoryView` 只管理原知識卡與結果面板內的瀏覽；不呼叫結算、計分、切場景、寫存檔或修改時間。
- Shared 的 Completed／LevelResultRelay 本地快照與 JSON 不變。正式流程仍由既有 TrackingLevelBridge Report，只有按「繼續探索」才 Exit。未修改 Shared、Common 或 Integration。

## Editor 與測試

原選單「開啟百年軌跡場景／建立缺少的百年軌跡場景」保留，生成器不覆蓋既有 Scene。新增「套用百年軌跡品質改善（保留場景）」只更新本關既有物件與其獨立 definition，拒絕在有未儲存場景時執行；重複執行不重複新增控制項。新生成的場景亦會套用改善。不使用全域 SaveAssets，不修改 Build Settings。

Test Runner 可執行 `XunJiYiDa.Levels.Timeline.Editor.Tests` 與 `XunJiYiDa.Levels.Timeline.PlayMode.Tests`。完整回歸應包含 Common、Shared 與 Integration；本分支沒有其他關卡時，其既有測試可能略過。

命令列使用 Unity 6000.3.19f1：`-batchmode -projectPath <副本路徑> -runTests -testPlatform EditMode`（或 PlayMode），加 `-testResults <XML路徑> -logFile <log路徑>`；不要與 `-quit` 合用。`NIU_TIMELINE_SCREENSHOT_DIR` 可指定原生 1920×1080 截圖位置。

修改前基準、逐項實測結果、範圍檢查與尚未確認項目見 [QualityVerification.md](QualityVerification.md)。
