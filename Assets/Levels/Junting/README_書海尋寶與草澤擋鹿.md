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

## 素材

`Resources/Junting/` 放峻廷原版的實景照片（`library.jpg`、`corridor.jpg`）、鹿剪影（`deer.png`）、圓角底圖（`rounded.png`）；`Resources/Junting/Audio/` 放 12 個原創合成音效與兩首 BGM（CC0）。來源取自 `codex/team-junting` 的 `d68aa7d`，GUID 已重新產生，避免與原分支衝突。授權見 `素材來源與授權.csv`。

Scene 上的 `fallbackClips` 只在 Resources 缺檔時使用。文物插圖沿用終章的 `Artifact_0N.png`，所以沒有搬 `pot.png`、`paddle.png`；原版未使用的 `ending.wav` 也沒有搬。

## 測試

`Tests/Editor/JuntingRulesTests.cs`：排架、索書號比較、擋鹿路徑、計時暫停與關卡資料對應。Test Runner → EditMode 執行。
