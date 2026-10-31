# 終章｜文物記憶翻牌

Unity **6000.3.19f1**。直接開啟 `Assets/Levels/Finale/L_Finale.unity` 並按 Play，不需要加入 Build Settings，也不需要 GameManager。

Game View 請使用 **16:9／Full HD（1920×1080）**，與原始素材及共用 UI 的參考解析度一致。

## 遊玩

1. 閱讀精簡的「終章空間」介紹，按「開始探索」。介紹期間不扣時間。
2. 顯示本局取得的三件文物 **2 秒**；提示期間不能翻牌，也不扣 90 秒。提示結束後，左欄仍保留三件文物名稱。獨立模式標示「測試資料」；正式來源不足時，補入項目逐件標示「測試補足」。
3. 在 **90 秒**內完成 **4 × 3** 牌桌的六組配對。每次翻兩張；第一張動畫結束後才能選第二張。
4. 相同文物保持翻開，顯示金框與有金色底的「已配對」標記；左欄立即顯示一句對應文物的知識，不暫停或新增彈窗，下一次翻牌不會清掉知識。不同文物記錄一次失誤，兩張完整翻開後保留 **0.8 秒**，顯示形狀比較線索，再一起翻回。動畫與等待時不能翻第三張。
5. 左欄常駐簡短規則，移除重複的「空間介紹」按鈕。先翻一張，再點「查看文物與來源」可閱讀該件的知識與來源 URL；尚未選牌時顯示本局第一件文物。保留程式介面 `ShowSpaceIntroduction()`。
6. 右上「暫停」或 Escape 暫停／繼續。介紹、知識卡、手動暫停及失焦都會凍結倒數與翻牌進度；手動暫停不會因關閉知識卡而解除。
7. 左下「重新開始」、暫停面板「重新開始」、結果面板「再試一次」都會原地清空牌桌、時間、失誤與結果，重新洗牌並從介紹及兩秒提示開始。
8. 六組配對完成即成功；時間歸零即失敗。失誤沒有次數上限。通關後在原結果卡內顯示六件可點擊圖鑑；切換文物可閱讀文物介紹，並點按鈕開啟來源。原有分數、再試一次與繼續探索保留。失敗與重試還原原結果卡尺寸。

## 三件文物來源

選取 Scene 中 `SharedLevelRoot/GameplayRoot/FinaleGameplay` 的 **FinaleArtifactSource**：

- `Test Artifact Ids` 可設定單獨測試時的三件文物，預設 `artifact-01`、`artifact-03`、`artifact-06`。
- `Source` 可指定 GameManager 或轉接元件，推薦實作下方介面。留空時會先尋找已載入場景中實作此介面的元件，再尋找類別名稱為 `GameManager` 的元件。
- 也支援 GameManager 公開的 `CollectedArtifactIds`／`ArtifactIds`／`collectedArtifactIds` 屬性或欄位，以及 `GetCollectedArtifactIds()` 方法，資料型別需為 `IEnumerable<string>`。不要求任何 Common 程式碼相依。
- 每次正式開始／重試後重新讀取，去掉空值、未知 ID 與重複 ID，最多保留前三件。有效來源不足三件時用測試 ID 補足；測試資料也不足時以六件標準 ID 補足。

```csharp
using System.Collections.Generic;
using UnityEngine;
using XunJiYiDa.Levels.Finale;

// 放在負責遊戲流程的組件中；本關沒有修改或建立 Common/GameManager。
public class FinaleInventoryAdapter : MonoBehaviour, IFinaleArtifactSource
{
    public IEnumerable<string> GetFinaleArtifactIds()
        => new[] { "artifact-02", "artifact-04", "artifact-05" }; // 改為本局取得資料
}
```

| ID | 文物 |
| --- | --- |
| artifact-01 | 金鯉魚 |
| artifact-02 | 金箔片 |
| artifact-03 | 幾何印紋陶罐 |
| artifact-04 | 木鏟 |
| artifact-05 | 木質圓形雕板 |
| artifact-06 | 瑪瑙珠 |

六種文物各兩張始終出現在牌桌上；開局提示與結果中的三件文物代表本局取得的文物。六組配對不會另外產生六件獎勵。

## 文物資料、來源與教育目標

正式內容的單一資料來源為 `Data/ArtifactFacts.json`，由 Scene 的 `FinaleLevelController.artifactFacts` 引用；Scene 只保留文物 ID、名稱與既有圖像，不再重複存放舊的長篇圖像介紹。

每件包含：`summary`（配對一句知識）、`knowledge`（詳細說明）、`memoryCue`（示意圖比較線索）、`sourceUrl`（文物來源）、`verificationStatus`、`verifiedOn`、`context`（校史／在地文化背景）、`contextSourceUrl`。本版六件的狀態為 `VerifiedOfficialSource`，核對日為 **2026-10-29**；驗證狀態與日期僅供資料管理，不出現在玩家介紹中；非此狀態只顯示圖卡辨識線索，不呈現未確認史實。

| 文物 | 操作中的學習重點 |
| --- | --- |
| 金鯉魚 | 俗稱與材質不同：實為魚形銅線編物；不能由名稱推定為金製 |
| 金箔片 | 辨識閉眼人臉壓印，認識墓葬中出土的飾物 |
| 幾何印紋陶罐 | 蘭陽平原黏土與手捏塑製作，連結在地生活文化 |
| 木鏟 | 認識 97.5 公分木鏟的保存環境與製作細節 |
| 木質圓形雕板 | 同心圓折線紋與木器的工藝特色 |
| 瑪瑙珠 | 遺址出土以六角管形最多，認識當時的裝飾文化 |

核對的官方頁面：

- [國立宜蘭大學圖資館：出土文物](https://lic.niu.edu.tw/p/412-1010-6107.php)：六件文物的材質、紋飾與記錄；木鏟介紹採已記載的長度與形制。
- [校園博物館／宜大記憶庫](https://lic.niu.edu.tw/p/412-1010-6105.php)：記憶庫整合 Library、Museum、Archives。
- [發掘大事紀](https://lic.niu.edu.tw/p/412-1010-6106.php)：1926 年建校時即發現遺址的校史連結。

所有既有圖像均為示意圖；未聲稱為真實出土物的測繪或等比例影像。本關不提供未核實的館舍規則或額外年代、用途推斷。來源網頁日後可能變動，更新文字時須重新核對並記錄狀態與日期。

2026-10-31：為統一考古小隊的寫實文物風格，`unity其中素材/Finale/Sprites/` 的 `Artifact_02`／`CardFace_02`（金箔片）改用 `Levels/Dig/Art/Gold.png`，`Artifact_03`／`CardFace_03`（幾何印紋陶罐）改用 `Pottery.png`（陶片示意，非完整罐形），`Artifact_06`／`CardFace_06`（瑪瑙珠）改用 `Agate.png`；檔名與 GUID 不變，原圖備份於 `Docs/ArtifactArt_Backup/`。`Artifact_01`／`CardFace_01`（金鯉魚）、`04`（木鏟）、`05`（木質圓形雕板）改用專案外 `10_31新文化素材/` 的 `金鯉魚.png`、`木鏟.png`、`木質圓形雕板.png`。六件文物圖現已統一為寫實風格。

## 結果與共用系統

Scene 保留 `Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab` 的 Prefab 連結，以場景覆寫指定 `Data/Finale.asset`。圖鑑是 Finale 場景內的附加 UI，沒有套用變更到 Shared Prefab。使用原有 SharedLevelController、SharedLevelUI、SharedAudioPlayer、LevelResultRelay；只有一個 Camera／AudioListener 和一個 InputSystem EventSystem。

訂閱 `SharedLevelController.Completed`，或在原有 `LevelResultRelay` 指定 `ILevelResultReceiver`。可從 `LevelResultRelay.LastResult`／`LastResultJson` 讀取每次嘗試只發送一次的不可變結果：

```json
{"levelId":"Finale","success":true,"remainingTime":73.2,"mistakeCount":1,"artifactIds":["artifact-01","artifact-03","artifact-06"]}
```

失敗同樣輸出完整結果。重試會清掉 relay 舊結果；先前收到的 LevelResult 快照不會被改動。本關不自動存檔或切換其他 Scene。

## Editor 工具與測試

- `尋跡宜大 > Finale > 建立缺少的終章場景`：以 Unity Editor API 建立真正的 Scene、關卡設定與專用中文字型；既有 Scene 不會覆蓋，其他開啟 Scene 保留。未命名且有內容的 Scene 需先另存。
- `尋跡宜大 > Finale > 開啟終章場景`：開啟正式 Scene。
- `尋跡宜大 > Finale > 驗證 Finale EditMode`：本關 EditMode 測試。
- `尋跡宜大 > Finale > 驗證全部 EditMode（含 Shared）`、`驗證全部 PlayMode（含 Shared）`：執行 Finale 與 Shared 的回歸測試，XML 與摘要輸出到本關 `Verification/`。
- 也可在 Test Runner 選取 `XunJiYiDa.Levels.Finale.Editor.Tests`／`XunJiYiDa.Levels.Finale.PlayMode.Tests`。
- 關閉此專案的 Editor 後，可在專案根目錄執行 `bash Assets/Levels/Finale/Editor/run_verification.sh` 重現全部 Finale＋Shared 測試。預設使用 macOS 的 6000.3.19f1；可用 `FINALE_UNITY_EDITOR` 指定 Editor 路徑。

Editor 工具不修改 Build Settings、Shared Prefab、來源 PNG 或共用字型。`FinaleScenePolish.UpgradeInventory/UpgradeLearning/UpgradeGallery` 是本次局部升級工具，只保存 Finale Scene；不重建整個畫面。場景文字使用從共用字型來源產生的 Finale 專用動態 SDF，字形新增限於本關資產。文物名稱沿用既有 ID 對照；正式知識來自上述官方頁面，素材為示意圖。

實際驗證證據見 `Verification/驗證報告.md`；修改前基準見 `Verification/改善前測試基準.md`。

圖形模式畫面驗證可設定 `FINALE_GRAPHICS=1 FINALE_SCREENSHOT_DIR=/tmp/finale-quality-captures` 後執行 `run_verification.sh`。截圖以 1920×1080 RenderTexture 產生，不改玩家螢幕設定。

玩家圖鑑、知識卡與配對提示只呈現對應文物的介紹，不再附加通用的記憶庫功能或生活記憶段落。文化背景與來源資料保留供內容管理及來源連結使用。
