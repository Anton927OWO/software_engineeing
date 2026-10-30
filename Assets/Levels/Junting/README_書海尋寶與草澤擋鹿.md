# 書海尋寶、草澤擋鹿（朱峻廷）

由 `codex/team-junting` 的獨立 Unity 專案移植進主專案。玩法、題目與介面沿用原版，改由主系統決定關卡、限時、文物與計分。

## Scene

| Scene | LevelId | 文物（依 `LevelCatalog`） |
|---|---|---|
| `Assets/Levels/Junting/L_Bookshelf.unity` | `Bookshelf` | 木鏟 |
| `Assets/Levels/Junting/L_Deer.unity` | `Deer` | 金鯉魚 |

直接打開 Scene 按 Play 即可單獨測試（`LevelTestLauncher` 已設定）；從 GameManager 進入時由主流程接手。

## 與原版的差異

- 每個 Scene 只跑一關；首頁、隨機順序、文物圖鑑交給 GameManager 與地圖，已移除。
- 限時、失誤扣分、文物來自 `LevelSession.Current`；結算用 `LevelSession.Report`，分數由 `ScoreCalculator` 計算。結尾問答答錯可重試，不算失誤。
- 失敗時可「再試一次」（`LevelSession.Retry`）或「繼續旅程」（照常回報並給文物，與團隊規則一致）。
- 文物知識改讀終章的 `Finale/Data/ArtifactFacts.json`，文物插圖用 `unity其中素材/Finale/Sprites/Artifact_0N.png`，與終章一致。
- EventSystem 改用 Input System 的 `InputSystemUIInputModule`；字型改用 `Shared/Fonts/NotoSansTC-Regular SDF`。
- 未移植：WebGL 橋接（`WebBridge`、`NIU.jslib`、WebGL 範本）、原專案的編輯器建置工具。

## 待補素材

原分支的圖片與音效存在 Git LFS，但 LFS 物件沒有上傳到 GitHub（下載回 404）。缺檔時程式會自動改用替代素材：

| 原檔（放到 `Resources/Junting/`） | 目前替代 |
|---|---|
| `library.jpg`、`corridor.jpg` | 介紹頁顯示「實景照片待補」 |
| `deer.png` | 棕色圓形「鹿」字 |
| `rounded.png` | `Shared/Sprites/Panel_Cream.png` |
| `Audio/*.wav`（16 個音效與兩首 BGM） | Scene 上 `fallbackClips` 指定的共用 UI 音效與 `Common/audio` 的 BGM |

原作者在原機器執行 `git lfs push --all se codex/team-junting` 後，把檔案用原檔名放回上表位置即可，程式不用改。授權見 `素材來源與授權.csv`。

## 測試

`Tests/Editor/JuntingRulesTests.cs`：排架、索書號比較、擋鹿路徑、計時暫停與關卡資料對應。Test Runner → EditMode 執行。
