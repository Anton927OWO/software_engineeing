# 尋跡宜大：共用基礎

適用 Unity **6000.3.19f1**。這一階段只有共用系統與驗證場景，沒有 Timeline、Train、Finale 的玩法、題庫或正式關卡場景。

## 開啟與操作

1. 開啟 `Assets/Levels/Shared/Scenes/SharedFoundation.unity`，按 Play。
2. 介紹期間不倒數；按「開始探索」後開始 120 秒倒數。
3. 使用中央按鈕檢查三種內容 UI、記錄失誤、成功或失敗。這些按鈕只存在驗證 Scene，沒有放進共用 Prefab。
4. 右上「暫停」或 Escape 可切換手動暫停。知識卡與介紹會暫停；小遊戲容器預設繼續倒數。
5. 結果面板的「再試一次」會重置本次狀態並重新顯示介紹。

編輯器選單：`尋跡宜大 > Shared > 建立缺少的共用資產與驗證場景`。重複執行會保留既有設定、Prefab 和 Scene。`開啟共用基礎驗證場景` 選單會先詢問是否儲存目前 Scene；取消即可保留目前畫面。

## 資產與程式結構

| 位置 | 用途 |
| --- | --- |
| `Core` | 無 Unity 相依的 LevelSession、狀態、暫停原因、不可變結果與接收介面 |
| `Runtime` | 關卡控制器、結果轉接、UI、音效、安全區與驗證 Scene 操作腳本 |
| `Editor` | 資產建立、UI Prefab 工廠、安全 Scene 產生工具 |
| `Data/SharedFoundation.asset` | 驗證用關卡 ID、名稱、120 秒限制、失誤上限與介紹 |
| `Data/SharedAudioLibrary.asset` | 五種共用 UI 音效，可替換或留空 |
| `Fonts/NotoSansTC-Regular SDF.asset` | 動態、多 atlas、預熱常用繁體中文字形的 TMP 字型 |
| `Prefabs/SharedLevelRoot.prefab` | 一個場景一個共用根物件，含 HUD、面板、結果轉接與音效 |
| `Scenes/SharedFoundation.unity` | 僅用於測試共用基礎；含 Camera、AudioListener、InputSystem EventSystem |
| `Sprites` | 六張 UI 素材副本；只調整副本的 Sprite／九宮格匯入設定 |
| `TextMeshPro` | 來自已安裝 uGUI 套件的 TMP Essential Resources，整組保留原本相互引用與授權 |
| `Tests` | EditMode 與 PlayMode 回歸測試 |

## 接入之後的關卡

將 `SharedLevelRoot.prefab` 放進關卡 Scene；Scene 需有 **一個** Camera／AudioListener 和 **一個**使用 `InputSystemUIInputModule` 的 EventSystem。Prefab 本身不附加 EventSystem，避免與宿主重複。世界物件可放在空的 `GameplayRoot`。

複製 `SharedFoundation.asset` 作為該關設定，在 Prefab 實例的 `SharedLevelController.definition` 指定它。`mistakeLimit = 0` 表示不因失誤次數失敗；時間歸零仍會失敗。ID 與倒數必須有效；執行期間修改 definition 不會重新建立已開始的 Session。

```csharp
using XunJiYiDa.Levels.Shared;

// 在關卡 MonoBehaviour 中用 Inspector 指定 controller / ui。
if (!controller.CanInteract) return; // 所有玩法輸入、拖放、判題都應先檢查。
controller.RecordMistake();
controller.CollectArtifact("artifact-01"); // 忽略空值與重複 ID。
controller.Succeed();                    // 或 controller.Fail();

ui.ShowKnowledgeCard("標題", "繁體中文知識內容", optionalSprite);
ui.ShowMinigame("標題", "說明", pauseCountdown: false);
// 自訂小遊戲 UI 可掛在 ui.MinigameContentRoot；此基礎不實作玩法。
```

`Retry()` 是**原地重置**，不依賴 Build Settings，也不重新載入 Scene。之後的關卡必須訂閱 `controller.Retried` 或 Inspector 的 `onRetried`，重置自己的物件、題目及互動。控制器會清空時間、失誤、文物、結果，接著重新顯示介紹。結果發送期間要求重試時，會等本次結果送完後才重置。

`LevelSession` 狀態為 Ready、Running、Paused、Succeeded、Failed。手動、內容面板、失焦三種暫停原因彼此獨立；關閉面板不會解除手動暫停。控制器以 `Time.unscaledDeltaTime` 計時，非 Running 時不扣秒；非活動狀態凍結世界時間，恢復／停用／卸載時還原原本倍率。音效另行暫停遊戲通道，UI 通道可繼續播放。此設計預期同時只有一個啟用的關卡控制器。

## 結果輸出

可選擇其中一種方式接入其他組員的流程：

- 訂閱 `controller.Completed`，取得 `LevelResult` 不可變快照。
- 在 `LevelResultRelay.onCompleted` 綁定接收 `LevelResult` 的 UnityEvent。
- 將實作 `ILevelResultReceiver.ReceiveLevelResult(LevelResult result)` 的 MonoBehaviour 放進 relay 的 `receivers` 陣列。
- 讀取 `LevelResultRelay.LastResultJson` 交給既有存檔或跨關流程；此基礎不自動寫檔、不切換其他關卡。

```json
{"levelId":"SharedFoundation","success":true,"remainingTime":83.4,"mistakeCount":1,"artifactIds":["artifact-01"]}
```

每次嘗試只結算一次，失敗同樣輸出上述欄位。沒有收集文物時輸出空陣列；文物 ID 由未來關卡提供，此階段不自行指定三關的文物規則。

## 中文、HUD 與音效

- CanvasScaler 的參考解析度為 **1920×1080**，按寬高各 0.5 的比例縮放，HUD 套用裝置 safe area；不修改玩家的螢幕設定。
- 所有共用 TMP 文字都直接指定 Noto Sans TC Regular SDF，來源是素材包內的靜態 `NotoSansTC-Regular.ttf`，來源字型資料保留供動態補字。
- 字型使用 2048×2048 SDF atlas，允許多 atlas；保留預熱字形，不依賴作業系統安裝字型。只支援來源字型本身具備的字元。
- TMP Essential Resources 已放在 Shared；請勿再次匯入同一套 Essential Resources 造成 GUID 重複。此處新建的 TMP Settings 只設定 Shared 的預設字型，不改其他組員資產。
- UI 音效使用原素材包的 Click、Correct、Wrong、Victory、Tick，均可在 audio library 換掉。`PlayClip(null)` 安全略過。最後 10 秒每秒播放一次 Tick。

## 編輯器工具擴充

`SharedPrefabFactory.Create(font, definition, library)` 可供後續關卡產生器組合共用根物件。`SharedSceneBuilder.CreateScene(path, populate)` 以 callback 加入場景內容，目前只接受 Shared 內的新 `.unity` 路徑，拒絕覆蓋現有 Scene 以及路徑跳脫。

一般情況以 additive Scene 建立並關閉輸出，保留既有開啟場景。Unity 不允許在未命名 Scene 旁新增 additive Scene：若它是全新且無內容的空場景，工具建立後會還原空場景；其他未命名 Scene 會明確要求先另存，不會替使用者儲存或丟棄。工具不修改 Build Settings，不自動生成三個關卡，也不儲存專案中無關的 dirty 資產。

## 驗證

在 Test Runner 執行 `XunJiYiDa.Levels.Shared.Editor.Tests` 和 `XunJiYiDa.Levels.Shared.PlayMode.Tests`。實際執行結果與尚未驗證項目記錄在 `docs/SharedFoundation_驗證報告.md`。

原始 `Assets/unity其中素材`、原有場景、Packages、ProjectSettings 與其他組員的 Common 均不需要修改。
