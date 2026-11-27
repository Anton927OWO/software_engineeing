# AGENTS.md｜開發規則

給在這個 repo 工作的**所有人與 AI 程式助理**。開始改任何東西之前請先讀完。

## 專案是什麼

《尋跡宜大》：以宜蘭大學為主題的教育解謎遊戲。目前正把 PC 2D 版轉成 **Meta Quest 3 MR 原型**。

- 玩法設計：`GOD.md`
- 技術架構：`ARCHITECTURE.md`
- 分工與進度：`task.md`
- 既有關卡接口：`Docs/關卡介面說明.md`

**時程**：2026/11/30 開工，2026/12/16 完成，12/14 起功能凍結（見 `task.md`）。

**原型範圍**：只做 Quest 3、只做 MR、只做手把輸入。不處理上架、正式版輸出、多平台、多人、存檔。遇到這類需求時先停下來問，不要自行加入。

## 環境

- Unity **6000.3.19f1**（不要升級或降級）
- URP 17.3、UGUI、TextMeshPro、Input System（New）
- XR Interaction Toolkit 3.x、OpenXR、Meta OpenXR、AR Foundation 6.x（安裝方式見 `Assets/Common/XR/README.md`）

## 擁有者規則（最重要）

一個資料夾只有一個擁有者。**只改自己負責的資料夾。** 要改別人的檔案時，開 issue 或直接找擁有者。

| 範圍 | 擁有者 |
|---|---|
| `Assets/Common/`（含 `Common/XR/`）、`Assets/Scenes/`、`Assets/Levels/Prologue/XR/`、`ProjectSettings/`、`Packages/`、`LevelCatalog.cs`、Build Settings | Anton927OWO |
| `Assets/Levels/Shared/`、`Timeline/`、`Train/`、`Finale/`、`Integration/` | Teatarossa |
| `Assets/Levels/Prologue/`（`XR/` 除外）、`Dig/`、`Strata/`、`Assets/Common/audio/` | Juelang0305 |
| `Assets/Levels/Junting/`、`Assets/Art3D/` | Chun |
| 根目錄文件（`AGENTS.md`、`GOD.md`、`ARCHITECTURE.md`、`task.md`） | 各自更新自己負責的段落；結構調整找 Anton927OWO |

> `Assets/Common/audio/` 由 Juelang0305 管理，但不在 `Common/` 底下開發程式碼。

## 不可以做的事

- 修改 `ProjectSettings/`、`Packages/manifest.json`、`packages-lock.json`（只有 Anton927OWO 可以）。
- 修改或刪除 2D 版的 `L_<關卡>.unity` 與其腳本（MR 版另開 `XR/` 資料夾）。
- 在關卡場景裡放 Camera、AudioListener、EventSystem 或玩家物件。
- 關卡之間互相引用程式碼或 prefab。
- 使用舊版 Input Manager（`Input.GetKey`、`Input.mousePosition`）。
- 不經過 `LevelSession` 自己計分、自己切換場景。
- 放進來源或授權不明的素材。
- 把個人的本機路徑、帳號、金鑰寫進檔案或 commit。

## 新增 MR 關卡的固定做法

1. 在 `Assets/Levels/<關卡>/XR/` 建立 `L_<關卡>_XR.unity`、`Runtime/`、`Prefabs/`、`Tests/`。
2. `Runtime/` 建 asmdef `XunJiYiDa.Levels.<關卡>.XR`，只引用 `TrackingNIU.Common`、`TrackingNIU.XR`、`XunJiYiDa.Levels.Shared.XR`、該關原本的 Runtime asmdef、`Unity.XR.Interaction.Toolkit`。**複製序章的 asmdef 來改**，保留它的 `defineConstraints` 與 `versionDefines`。
3. 場景根物件掛 `XRLevelRoot`，所有關卡物件放在它底下；另放一個既有的 `LevelTestLauncher`。建議用 Editor 腳本產生場景（參考 `Assets/Levels/Prologue/XR/Editor/PrologueXRSceneBuilder.cs`，選單放在「尋跡宜大 → MR 原型 → 關卡」底下）。
4. 互動一律使用 `Common/XR` 的元件（`XRGrabbable`、`XRSnapSocket`、`BrushTool`……），缺功能就開 issue，不要自己複製一份。
5. 介紹、HUD、暫停、結算使用 `Levels/Shared/XR` 的共用面板。
6. 遊戲結束呼叫 `LevelSession.Report()`，按繼續時呼叫 `LevelSession.Exit()`，重試呼叫 `LevelSession.Retry()`。
7. 純規則邏輯（不依賴 MonoBehaviour 與 UI）寫成獨立類別並加 EditMode 測試。

## 程式風格

- 沿用現有風格：namespace `TrackingNIU`（共用）、`TrackingNIU.XR`（MR 共用）、`XunJiYiDa.Levels.<關卡>.XR`（關卡）。
- 註解與 UI 文字用繁體中文。
- 公開欄位用 `[SerializeField] private`，不要用 `public` 欄位。
- 不用 `FindObjectOfType` 找其他關卡或共用系統的物件；用 Inspector 連結，或使用元件提供的靜態入口（例如 `TableAnchor.Current`）。
- 一個檔案一個主要類別，檔名與類別同名。

## Git 流程

- `main` 只透過 PR 更新，PR 由 Anton927OWO 審查合併。
- 分支命名：`feature/xr-<範圍>`，例如 `feature/xr-dig`、`feature/xr-core`、`feature/art3d-artifacts`。
- 開工前先 `git pull` main 到自己的分支；PR 前再合併一次 main，解決衝突後才送。
- 一個 `.unity`、`.prefab` 只有一個人改。萬一場景檔衝突，**不要手動合併 YAML**，保留擁有者的版本，另一方重做自己的改動。
- `.meta` 檔一定要跟著資產一起 commit；不要刪掉別人的 `.meta`。
- 3D 模型、貼圖、音檔走 Git LFS（`.gitattributes` 已設定）。
- Commit 訊息：`<範圍>: <做了什麼>`，例如 `dig-xr: 加入刷土覆蓋率計算`。

## PR 檢查清單

- [ ] 只改了自己負責的資料夾
- [ ] Unity Console 沒有新的錯誤或警告
- [ ] 自己的 EditMode 測試全部通過
- [ ] 打開自己的 `L_<關卡>_XR.unity` 用模擬器可以從開始玩到結算
- [ ] 新素材已登記來源與授權
- [ ] 有影響其他人的改動（共用元件 API、共用面板）已在 PR 說明並通知相關的人
- [ ] 更新 `task.md` 對應項目的狀態

## 給 AI 程式助理的補充

- 先確認任務屬於哪個擁有者的範圍；超出範圍時停下來說明，不要順手改。
- 不要自行安裝套件、修改專案設定或升級 Unity。
- 修改 `.unity`、`.prefab` YAML 前先確認沒有其他人正在改同一個檔案；能用 Editor 腳本產生的，優先寫 Editor 腳本。
- 無法在 Unity 或頭盔上驗證的改動，要在回報中明確說明「未在 Unity／實機驗證」。
