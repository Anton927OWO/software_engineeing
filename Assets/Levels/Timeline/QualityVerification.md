# 百年軌跡品質改善驗證

## 範圍與修改前基準（2026-10-29）

原工作區位於 `codex/team-finale`，有既存未提交變更且沒有 Timeline；完整保留。由 `codex/team-timeline`（1525fc2）建立隔離分支 `codex/timeline-quality`。未找到 AGENTS.md 或 CODEOWNERS；以使用者指定的本關路徑作為修改界線，禁止修改 Common、Shared、Integration、其他關卡、來源素材、Packages 與 ProjectSettings。既有 README 的 Timeline 路徑責任界線及共用接口亦保留。

已閱讀本關 Scene、三個 Runtime C#、Editor 產生器、學制資料、測試、Shared README、SharedLevelRoot Prefab、控制器及 UI、Integration 結果橋接及介面說明。本關沒有獨立卡片 Prefab，卡片序列化在 Scene 中；Shared Prefab 以場景實例沿用。

Unity 6000.3.19f1 在暫存專案副本實測，排除首次匯入對交付範圍外檔案的影響。命令使用 `-batchmode -runTests -testPlatform EditMode/PlayMode -testResults … -logFile …`；EditMode 加 `-nographics`。PlayMode 使用真正圖形裝置，並設定 `NIU_TIMELINE_SCREENSHOT_DIR` 留存原生畫面。

| 修改前套件 | 通過 | 失敗 | 略過 |
| --- | ---: | ---: | ---: |
| 全部 EditMode | 58 | 0 | 0 |
| 全部 PlayMode | 16 | 0 | 2 |

略過項目：Integration 的 `StandaloneCanRetryAfterContinue`、`StandaloneRetryClearsReportedScoreAndReportsFreshAttempt`，由既有測試因 Finale Scene 不在 Timeline 分支而略過。非 Timeline 測試失敗。原始 XML、Editor log 與 introduction/gameplay/knowledge/pause/success/timeout 截圖保留於暫存驗證資料夾，未混入遊戲資產。

原規則：45 秒、五張全對通關、超時失敗、失誤無次數上限、錯放或放空每次拖曳只記一次失誤。知識卡暫停計時，重試重置並洗牌；正式結果由既有 Shared / TrackingLevelBridge 回傳。

## 官方內容依據

查核：2026-10-29。官方校史頁的民國年換算為西元年（+1911），校名以繁體「臺」統一。五節點是挑選的學制變化，並非完整校名史。

- 校名、1926 / 1946 / 1988 / 1998 / 2003 與 1967 / 1969 補充：[國立宜蘭大學校史](https://www.niu.edu.tw/p/412-1000-1049.php)。1946 僅描述更名，不將官方頁中「光復」一詞當作該年發生的事件。
- 地方發展需求、教育制度演變、校園博物館連結：[宜大記憶庫開門 校園變身博物館](https://www.niu.edu.tw/p/406-1000-56719%2Cr43.php)。
- 保存校園地景、生活物件、師生事件與故事：[圖資館宜大記憶庫](https://lic.niu.edu.tw/p/404-1010-63587.php)。
- 校史與農業、產業、教育發展的關係：[百年校史系列專題](https://lic.niu.edu.tw/p/412-1010-3425.php)。

不新增文物用途、年代或館舍規則。卡片幾何與色彩屬遊戲識別設計，不是歷史建築／文物復原。

## 改善一：操作中學習

新增正確放置立即顯示該年代一句史實，錯放提供各卡不同的學制線索，線索不含年份或格子編號。沿用每次拖曳一次失誤、彈回與固定機制。資料集中於不可變的 TimelineHistory，每則史實及文化連結各自附官方 URL，並區分歷史事件與今日文化連結。

先行測試：新增 2 個行為測試，在舊實作中均因通用回饋文字而失敗；修改後 Timeline PlayMode 11/11 通過（0 失敗）。

## 改善二：年代識別與可讀性

保留全部 273 個原有序列化物件、原插圖裁切、卡槽與時間軸。五卡分別加入暖褐平行線、磚紅直紋、青綠方格、靛藍階梯、草綠門框等遊戲識別；不把圖形宣稱為史實。加大卡名、手勢文字與正誤回饋，調整卡底字框以免裁字，簡化介紹並將按鈕文案分成「操作說明／年代提示」。

先行場景測試因缺少年代識別而失敗；局部升級後全 EditMode 60/60 通過，Timeline PlayMode 11/11 通過。原生 1920×1080 截圖已檢查，額外發現並修正手勢文字高度不足。升級工具只修改 Timeline Scene 與其獨立 definition，不重新建立 Scene，不儲存全域 dirty 資產。
