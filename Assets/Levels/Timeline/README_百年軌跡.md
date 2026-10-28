# 百年軌跡

Unity **6000.3.19f1**。開啟 `Assets/Levels/Timeline/L_Timeline.unity` 後按 Play。

## 操作

1. 閱讀「校史時光廊」空間介紹，按「開始探索」後開始 **45 秒**倒數。
2. 將下方五張學制卡拖曳至時間軸對應年份。支援滑鼠左鍵與觸控拖曳；拖曳期間保留按下的位置。
3. 放對立即吸附固定，已定位卡不可再移動。放錯年份、已佔用的格子或放到格子外會彈回原位置，一次拖曳只增加一次失誤。失誤沒有次數上限。
4. 五張卡全對即通關；時間歸零顯示共用失敗結果。
5. 「閱讀知識卡」與「空間介紹」會暫停倒數。右上「暫停」或 Esc 可手動暫停；關閉內容面板不會解除另一個手動暫停。
6. 暫停面板的「重新開始」、結果面板的「再試一次」皆原地重置 45 秒、失誤、定位狀態、拖曳、結果，重新洗牌並顯示介紹，不需要 Build Settings。

讀取中、暫停、失焦、介紹、知識卡與結算期間不接受玩法輸入。拖曳中進入上述狀態，未固定卡直接回原位，不算失誤。每局洗牌保留五張不同卡片，排除正確年代順序，也不沿用上一局順序。

## 場景與共用接口

- 真正序列化的 Unity Scene；包含 `SharedLevelRoot.prefab` 實例、單一 Camera／AudioListener、單一 Input System EventSystem。
- 保留原 Shared UI、CanvasScaler、安全區、Noto Sans TC TMP 字型、音效與 `LevelResultRelay`。
- `Data/TimelineDefinition.asset` 是 Shared 關卡設定的獨立副本：`levelId = Timeline`、`durationSeconds = 45`、`mistakeLimit = 0`。
- `TimelineBoard` 在正確／錯誤時呼叫共用音效與 `RecordMistake()`，全部定位後呼叫 `Succeed()`；逾時由 Shared 控制器處理。
- 共用結果透過 `controller.Completed` 或 `LevelResultRelay` 提供，含 `levelId`、`success`、`remainingTime`、`mistakeCount`、`artifactIds`。本關沒有額外文物規則，`artifactIds` 為空陣列。
- 訂閱 `controller.Retried` 重置玩法，沒有自行切場景、寫存檔或重複發送結果。

## Editor 工具

選單：`尋跡宜大 > Timeline > 開啟百年軌跡場景`。

建立缺少資產：`尋跡宜大 > Timeline > 建立缺少的百年軌跡場景`。

產生器使用 `EditorSceneManager.NewScene`、`PrefabUtility.InstantiatePrefab` 與 `EditorSceneManager.SaveScene` 生成實體場景。既有 Scene 不覆蓋；保留使用者開啟場景與 Build Settings。不呼叫全域 `AssetDatabase.SaveAssets()`。非空白的未命名 Scene 需先另存，避免丟失內容。

素材由 `Assets/unity其中素材/Timeline/Sprites` 複製至本關 Sprites，僅設定副本匯入規則。遊玩卡面用 `RectMask2D` 裁去圖上的年份，保留學制辨識挑戰；介紹與知識卡仍可顯示原完整插圖。沒有修改素材來源或 Shared 資產。

## 可執行測試

Unity 的 `Window > General > Test Runner`：

- EditMode：`XunJiYiDa.Levels.Timeline.Editor.Tests`。
- PlayMode：`XunJiYiDa.Levels.Timeline.PlayMode.Tests`。

關閉同一路徑的 Editor 後，也可用命令列執行（將 `<project>`、`<results>` 換成絕對路徑）：

```sh
/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath '<project>' -runTests \
  -testPlatform EditMode -testFilter XunJiYiDa.Levels.Timeline \
  -testResults '<results>/editmode.xml' -logFile '<results>/editmode.log'

/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath '<project>' -runTests \
  -testPlatform PlayMode -testFilter XunJiYiDa.Levels.Timeline \
  -testResults '<results>/playmode.xml' -logFile '<results>/playmode.log'
```

`-runTests` 不搭配 `-quit`，讓 Test Runner 完成後自行結束。可設定 `NIU_TIMELINE_SCREENSHOT_DIR` 為輸出目錄，於有圖形裝置時擷取 1920×1080 原生畫面；`-nographics` 會略過截圖。

真實滑鼠測試暫時調整記憶體中的 Input System 失焦／Editor 輸入路由設定，注入合成 MouseState 事件，經真正 InputSystemUIInputModule 和 GraphicRaycaster 拖放，最後復原設定。測試不儲存 ProjectSettings。

實際測試結果、已知驗證範圍與修改清單見 `Verification/驗證報告.md`。

## 內容依據

五個素材對應主要校史節點：1926 臺北州立宜蘭農林學校、1946 臺灣省立宜蘭農業職業學校、1988 國立宜蘭農工專科學校、1998 國立宜蘭技術學院、2003 國立宜蘭大學。

名稱採繁體「臺」字統一呈現。官方校史另有 1967、1969 年的職校更名，知識卡已補充，這五張卡不代表完整七次校名歷程。

來源：[國立宜蘭大學校史](https://www.niu.edu.tw/p/412-1000-1049.php)、[圖書資訊館百年校史系列專題](https://lic.niu.edu.tw/p/412-1010-3425.php)。查核日期：2026-10-26。
