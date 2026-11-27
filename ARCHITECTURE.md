# 尋跡宜大 MR 版｜技術架構

> 原型範圍：Unity 6000.3.19f1、只支援 Meta Quest 3、只做 MR。不處理正式版輸出與多平台。
> 玩法設計見 `GOD.md`，分工見 `task.md`，開發規則見 `AGENTS.md`。

## 1. 設計原則

1. **沿用現有遊戲流程**：`GameManager`、`GameRun`、`LevelCatalog`、`ScoreCalculator`、`LevelSession` 不重寫。MR 版只換「畫面與輸入」這一層。
2. **玩家只有一份**：攝影機、手把、passthrough 放在一個常駐物件裡，切換場景時不銷毀。關卡場景只放「桌上的內容」。
3. **關卡彼此獨立**：每關是獨立的資料夾、Scene 和 asmdef，只依賴 `Common` 與 `Common/XR`，不互相引用。
4. **2D 版保留**：原本的 `L_<關卡>.unity` 不刪不改，MR 版另開新 Scene。MR 版還沒做好的關卡不放進抽關池。

## 2. 套件

由 Anton927OWO 統一安裝與更新，其他人不要修改 `Packages/manifest.json`。

| 套件 | 用途 |
|---|---|
| Meta XR Core SDK | 玩家物件、passthrough、手把輸入 |
| Meta XR Interaction SDK | 抓取、放置、射線點擊 UI |
| Meta MR Utility Kit（MRUK） | 偵測真實桌面（P1） |
| Meta XR Simulator | 沒有頭盔時在編輯器裡測試 |
| 既有：URP 17.3、UGUI、Input System、TextMeshPro | 沿用 |

專案設定（同樣只由 Anton927OWO 修改）：平台切到 Android、啟用 OpenXR／Meta 外掛、URP 關閉 HDR 與後製、MSAA 4x、Active Input Handling 維持 Input System（New）。

## 3. 執行時結構

```
Boot.unity
 └─ 生成 MRCore（DontDestroyOnLoad）
      ├─ 玩家物件：攝影機、左右手把／手、射線
      ├─ Passthrough
      ├─ TableAnchor：桌面位置、方向、可用尺寸
      ├─ XR EventSystem：讓空間面板可以用射線點擊
      └─ PauseInput：手把選單鍵 → 暫停
 └─ GameManager（DontDestroyOnLoad，既有）

GameManager.EnterLevel(id)
 └─ 用 Single 模式載入 L_<Id>_XR.unity（沒有 MR 版的關卡不會被抽到）
      └─ XRLevelRoot（場景根物件）
           載入後 MRCore 把它移到 TableAnchor 上
           ├─ 關卡物件（土堆、書、卡片……）
           ├─ 空間面板（介紹、HUD、結算）
           └─ 關卡控制腳本 → LevelSession.Report() / Exit()
```

- 既有 `GameManager` 用 `LoadSceneMode.Single` 切換場景，MRCore 放在 `DontDestroyOnLoad` 就能跨場景保留，**不需要改成 Additive 載入**。
- 關卡場景裡**不能**有 Camera、AudioListener、EventSystem、玩家物件。

## 4. 資料夾與擁有者

```
Assets/
├─ Common/
│   ├─ Scripts/            既有遊戲流程            Anton927OWO
│   └─ XR/                 MR 共用元件（新）        Anton927OWO
│       ├─ Runtime/        TrackingNIU.XR.asmdef
│       ├─ Prefabs/        MRCore、WorldPanel、互動元件
│       └─ Editor/
├─ Art3D/                  共用 3D 素材（新）       Chun
│   ├─ Artifacts/          6 件文物
│   ├─ Props/              書、刷子、柵欄、卡片……
│   ├─ Environment/        校園模型、桌面裝飾
│   └─ 素材來源與授權.csv
├─ Levels/
│   ├─ Shared/XR/          關卡共用面板（新）       Teatarossa
│   ├─ Prologue/XR/        L_Prologue_XR.unity      Anton927OWO
│   ├─ Dig/XR/             L_Dig_XR.unity           Juelang0305
│   ├─ Strata/XR/          L_Strata_XR.unity        Juelang0305
│   ├─ Timeline/XR/        L_Timeline_XR.unity      Teatarossa
│   ├─ Train/XR/           L_Train_XR.unity         Teatarossa
│   ├─ Finale/XR/          L_Finale_XR.unity        Teatarossa
│   └─ Junting/XR/         L_Deer_XR.unity          Chun
└─ Scenes/
    ├─ Boot.unity          既有，加入 MRCore        Anton927OWO
    └─ Map_XR.unity        桌上地圖（新）           Anton927OWO
```

每個 `<關卡>/XR/` 底下固定為：

```
<關卡>/XR/
├─ L_<關卡>_XR.unity
├─ Runtime/   XunJiYiDa.Levels.<關卡>.XR.asmdef
├─ Prefabs/
└─ Tests/     （純規則邏輯的 EditMode 測試）
```

asmdef 只能引用 `TrackingNIU.Common`、`TrackingNIU.XR`、`XunJiYiDa.Levels.Shared.XR`、該關原本的 Runtime asmdef 與 Meta SDK。

> 序章、考古小隊、地層解碼的 2D 版腳本目前沒有 asmdef（在 Assembly-CSharp 裡），asmdef 無法引用它們。Juelang0305 可以選擇把純規則邏輯（不依賴 UI 的部分）抽到新的 asmdef 共用，或在 XR 版重寫。不要為此修改其他人的檔案。

## 5. 共用元件（`Common/XR`，namespace `TrackingNIU.XR`）

由 Anton927OWO 提供，其他人只使用、不修改。需要新功能時開 issue。

| 元件 | 功能 | 主要使用者 |
|---|---|---|
| `MRCore` prefab | 玩家物件、passthrough、XR EventSystem、暫停鍵 | 全部 |
| `TableAnchor` | 提供桌面 `Pose` 與可用尺寸；先做虛擬檯面＋把手調整（P0），再加自動偵測真實桌面（P1） | 全部 |
| `XRLevelRoot` | 放在關卡場景根物件，宣告需要的桌面尺寸；載入時自動對齊 TableAnchor | 全部 |
| `XRGrabbable` | 可以抓起的物件，帶有物件 ID；放開時沒落在插槽就回到原位 | 序章、百年軌跡、知識列車、擋鹿、地層 |
| `XRSnapSocket` | 放置插槽，設定接受的 ID；事件 `Accepted(id)`、`Rejected(id)`，放錯時讓物件彈回 | 序章、百年軌跡、知識列車、地層 |
| `XRTool` / `BrushTool` | 手持工具，每幀回報接觸點、移動速度、按壓深度 | 考古小隊 |
| `XRPokeTarget` | 用手指或手把碰觸觸發（翻牌、按鈕） | 終章、地圖 |
| `Haptics` | `Haptics.Play(hand, HapticPreset.Correct / Wrong / Warning)` | 全部 |
| `WorldPanel` prefab | 空間中的 UGUI 面板，固定解析度與字級，可以用射線點擊 | Shared/XR |
| `XRLevelTestLauncher` | 單獨打開關卡場景時，自動生成 MRCore 並開始測試 Session | 全部 |

## 6. 關卡共用面板（`Levels/Shared/XR`）

由 Teatarossa 提供，各關直接使用，讓每關的介紹、暫停、結算外觀一致。

| 面板 | 內容 |
|---|---|
| `XRIntroPanel` | 關卡名稱、地點、操作說明、開始按鈕 |
| `XRHud` | 倒數計時、失誤次數（浮在桌面後方） |
| `XRPausePanel` | 繼續、重試、離開 |
| `XRResultPanel` | 成功／失敗、得分明細（`ScoreBreakdown`）、知識卡、繼續 |

API 方向：`XRLevelUI.ShowIntro(context, onStart)`、`ShowResult(result, score, onContinue, onRetry)`，細節由 Teatarossa 定義並寫在 `Levels/Shared/XR/README.md`。共用面板完成前，各關可以先用 `Debug.Log` 或簡單的 WorldPanel 代替。

## 7. 關卡與主流程的接口

**不改變現有接口**，各關照 `Docs/關卡介面說明.md` 的流程：

```csharp
void Start()
{
    var ctx = LevelSession.Current;        // 限時、文物、已收集文物
    ui.ShowIntro(ctx, BeginPlay);
}

void OnFinished(bool cleared)
{
    var score = LevelSession.Report(new LevelResult {
        Cleared = cleared, RemainingTime = timer.Remaining, Mistakes = mistakes });
    ui.ShowResult(LevelSession.Result, score,
        onContinue: LevelSession.Exit,
        onRetry:    LevelSession.Retry);
}
```

主流程需要的改動（Anton927OWO 負責）：

- `LevelDefinition` 新增 MR 版場景名稱 `L_<Id>_XR`；`GameManager` 載入 MR 版。2D 版的 Screen Space Overlay 畫面在頭盔裡看不到，所以**沒有 MR 版的關卡直接不放進抽關池**，不退回 2D 版。
- `LevelCatalog` 把書海尋寶設成 `availableInMidterm: false`。

## 8. 3D 素材規格

由 Chun 維護，所有人放進 `Art3D/` 的模型都要符合：

| 項目 | 規格 |
|---|---|
| 格式 | `.fbx` 或 `.glb`，走 Git LFS |
| 單位與比例 | 1 unit = 1 m；以桌上模型的尺寸製作（例如文物 8–15 cm） |
| 原點 | 物件底部中心 |
| 面數 | 單一物件 ≤ 5k 三角面；單一關卡場景總計 ≤ 100k |
| 材質 | URP Lit／Simple Lit，每個物件最多 1–2 張材質；貼圖 ≤ 1024 |
| 碰撞 | 用簡單碰撞體（Box／Capsule），不使用 Mesh Collider |
| 授權 | 新增前先登記到 `Art3D/素材來源與授權.csv` |

## 9. 效能預算（Quest 3 原型）

- 維持 72 fps 以上。
- 不使用即時陰影，或只在主要光源開啟一張低解析度陰影。
- 不使用後製特效、不使用透明物件疊加大面積。
- 空間面板的 Canvas 數量越少越好，每關同時顯示 ≤ 3 個。

## 10. 測試方式

| 層級 | 方式 | 誰 |
|---|---|---|
| 規則邏輯 | EditMode 測試（判斷放對放錯、刷土覆蓋率、計時等純邏輯） | 各關負責人 |
| 單關操作 | 打開 `L_<關卡>_XR.unity` 按 Play，用 Meta XR Simulator 操作 | 各關負責人 |
| 實機手感 | 用開發者模式的 Quest 3 直接執行（Quest Link 或安裝測試用 APK） | 各關負責人，依排程 |
| 完整流程（12/9 起每天） | 從 Boot 玩完一局 | Anton927OWO |
