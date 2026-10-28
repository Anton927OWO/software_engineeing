# 終章｜文物記憶翻牌

Unity **6000.3.19f1**。直接開啟 `Assets/Levels/Finale/L_Finale.unity` 並按 Play，不需要加入 Build Settings，也不需要 GameManager。

Game View 請使用 **16:9／Full HD（1920×1080）**，與原始素材及共用 UI 的參考解析度一致。

## 遊玩

1. 閱讀共用「終章空間」介紹，按「開始探索」。介紹期間不扣時間。
2. 顯示本局取得的三件文物 **2 秒**；提示期間不能翻牌，也不扣 90 秒。
3. 在 **90 秒**內完成 **4 × 3** 牌桌的六組配對。每次翻兩張；第一張動畫結束後才能選第二張。
4. 相同文物保持翻開，顯示加粗金框與「已配對」標記；不同文物記錄一次失誤，兩張完整翻開後保留 **0.8 秒**，再一起翻回。動畫與等待時不能翻第三張。
5. 點「空間介紹」重看規則；先翻一張再點「閱讀知識卡」可閱讀該文物的圖像觀察，尚未選牌時顯示本局第一件文物。
6. 右上「暫停」或 Escape 暫停／繼續。介紹、知識卡、手動暫停及失焦都會凍結倒數與翻牌進度；手動暫停不會因關閉知識卡而解除。
7. 左下「重新開始」、暫停面板「重新開始」、結果面板「再試一次」都會原地清空牌桌、時間、失誤與結果，重新洗牌並從介紹及兩秒提示開始。
8. 六組配對完成即成功；時間歸零即失敗。失誤沒有次數上限。

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

## 結果與共用系統

Scene 保留 `Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab` 的 Prefab 連結，以場景覆寫指定 `Data/Finale.asset`。使用原有 SharedLevelController、SharedLevelUI、SharedAudioPlayer、LevelResultRelay；只有一個 Camera／AudioListener 和一個 InputSystem EventSystem。

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

Editor 工具不修改 Build Settings、Shared Prefab、來源 PNG 或共用字型。場景文字使用從共用字型來源產生的 Finale 專用動態 SDF，字形新增限於本關資產。文物名稱依素材 `manifest.json`；知識卡以圖像觀察為內容，素材為示意圖。

實際驗證證據見 `Verification/驗證報告.md`。
