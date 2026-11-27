# Common/XR｜MR 共用元件

負責人：Anton927OWO。其他人只使用、不修改；需要新功能請開 issue。
架構說明見根目錄 `ARCHITECTURE.md`。

## 第一次設定（Anton927OWO 執行一次）

> 這些程式是在沒有 Unity 的環境寫的，**還沒編譯過**。第一次執行時如果 Console 有錯誤，照錯誤訊息修正。

前置：Unity 6000.3.19f1 已安裝 **Android Build Support**（含 OpenJDK、Android SDK & NDK）。

1. **尋跡宜大 → MR 原型 → 1. 安裝 XR 套件**
   安裝 XR Interaction Toolkit、OpenXR、Meta OpenXR、AR Foundation、XR Plug-in Management。等編譯結束。
2. **尋跡宜大 → MR 原型 → 2. 匯入範例並設定專案**
   匯入 XR Interaction Toolkit 的 Starter Assets 與模擬器範例；設定 OpenXR（Meta Quest、Quest 手把、Passthrough、平面偵測）；URP 關閉 HDR、MSAA 4x；切到 Android。
   完成後打開 **Project Settings → XR Plug-in Management → Project Validation**，按 **Fix All**。
3. **尋跡宜大 → MR 原型 → 3. 建立 MRCore 與 MR 場景**
   產生 `Resources/TrackingNIU/MRCore.prefab`、`WorldPanel.prefab`、範本場景、元件示範場景、`Assets/Scenes/Map_XR.unity`，並在 Boot 的 GameManager 勾選 Mixed Reality。
4. **尋跡宜大 → MR 原型 → 關卡 → 序章（L_Prologue_XR）**
5. **Window → General → Test Runner → EditMode** 全部執行。
6. 把產生的檔案（含 `.meta`）commit。

### 確認方式

| 打開 | 按 Play 應該看到 |
|---|---|
| `Common/XR/Samples/XR_Components_Demo` | 面前出現木色桌面；模擬器可以抓方塊放進綠色插槽，紅色方塊會彈回 |
| `Levels/Prologue/XR/L_Prologue_XR` | 介紹面板 → 開始 → 依紅藍黃把書放進書箱 → 通關面板 → 繼續後 Console 印出測試結果 |
| `Scenes/Boot` | 自動進入 Map_XR 標題 → 開始遊戲 → 序章 → 地圖 → 還沒有 MR 版的關卡顯示「模擬通關」 |

模擬器操作：參考畫面左下角的說明（XR Interaction Toolkit 模擬器）。鍵盤 **Esc** = 選單鍵，按住 **R** = 重新擺放桌面。

### 在 Quest 3 上測試

- 頭盔開啟開發者模式，USB 連接電腦，**File → Build Profiles → Android → Run Device** 選頭盔，按 **Build And Run**。
- 想在編輯器直接用頭盔（Quest Link）：XR Plug-in Management 的 **Windows** 分頁勾選 OpenXR，並加入 Oculus Touch 手把設定。不勾的話編輯器會使用模擬器。

## 各關怎麼使用

**座標約定**：關卡內容放在 `XRLevelRoot` 底下，原點是桌面中心、`y = 0` 是桌面表面；玩家站在 **-Z** 那一側，面向 **+Z**。預設桌面範圍 1.0 × 0.6 公尺。

### 建立關卡場景

參考 `Assets/Levels/Prologue/XR/`：

```
<關卡>/XR/
├─ Runtime/   複製序章的 asmdef 改名（保留 defineConstraints、versionDefines）
├─ Editor/    場景建置腳本，使用 XRSceneBuildUtility
└─ Tests/Editor/
```

建置腳本常用的函式（`TrackingNIU.XR.Editor.XRSceneBuildUtility`）：

```csharp
if (!TryNewScene(out var scene)) return;
var root = CreateLevelRoot(new Vector2(1f, 0.6f));      // XRLevelRoot
AddTestLauncher(root.transform, LevelId.Dig);           // 單獨測試用
var mat = GetOrCreateMaterial(".../Soil.mat", Hex("#7A5A3A"));
var box = CreateBox("Soil", root.transform, pos, size, mat);
var item = MakeGrabbable(box, "artifact-01");          // 可以抓
var socket = MakeSocket(go, 0.07f, new[] { "artifact-01" }, lockOnAccept: true, singleUse: true);
SaveScene(scene, "Assets/Levels/Dig/XR/L_Dig_XR.unity"); // 存檔＋加入 Build Settings
```

### 執行時元件

```csharp
// 插槽：放對／放錯
socket.Accepted += (s, item) => Haptics.Play(item.LastHolder, HapticPreset.Correct);
socket.Rejected += (s, item) => { mistakes++; Haptics.Play(item.LastHolder, HapticPreset.Wrong); };
socket.Validator = item => rule.IsNext(item.ItemId);   // 需要自訂規則時

// 可抓物件
item.SetGrabEnabled(false);   // 介紹面板顯示時禁止抓
item.ResetToHome();           // 重試

// 刷子：在被刷的物件上實作 IBrushSurface
public void OnBrush(in BrushContact c) { /* c.Point、c.Speed、c.Depth */ }

// 面板（共用面板完成前可以先用）
var panel = WorldPanel.Create(transform, new Vector3(0f, 0.02f, 0.22f), "標題", "內文");
panel.AddButton("開始", BeginPlay);

// 選單鍵（暫停）
MRCore.MenuPressed += OnMenu;
```

結果回報照舊使用 `LevelSession.Report()`／`Exit()`／`Retry()`。

## 檔案

| 路徑 | 內容 |
|---|---|
| `Setup/Editor/` | 選單 1：安裝套件（不依賴 XR 套件，任何時候都能編譯） |
| `Runtime/` | `TrackingNIU.XR`：MRCore、TableAnchor、XRLevelRoot、XRGrabbable、XRSnapSocket、BrushTool、XRPokeTarget、Haptics、WorldPanel、MapXRController |
| `Runtime/Samples/` | 示範場景用的腳本 |
| `Editor/` | 選單 2、3 與 `XRSceneBuildUtility` |
| `Tests/Editor/` | EditMode 測試 |
| `Resources/TrackingNIU/` | 選單 3 產生的 prefab |
