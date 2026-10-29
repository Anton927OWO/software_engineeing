# 《尋跡宜大》三關交接

Unity 6000.3.19f1｜更新：2026-10-29

## Scene、操作與通關

| Scene（相對專案根目錄） | 操作與條件 |
|---|---|
| `Assets/Levels/Prologue/L_Prologue.unity` | 點紅書，依紅、藍、黃順序拖入書箱；錯放返回。收齊三本後直接通關；暫停可選用，不是通關條件。 |
| `Assets/Levels/Dig/L_Dig.unity` | 滑鼠／單指輕刷土層；60秒內三件文物本體露出70%成功；超時或損傷3次失敗。 |
| `Assets/Levels/Strata/L_Strata.unity` | 第一階段三件文物隨機排列、自由歸層，答對生土層觀察題後解鎖下一階段；第二階段三題紋飾及選項隨機排列。兩階段共用60秒；成功後依序閱讀三頁知識卡，再進入結算。 |

保留既有圖片、UI、配對規則、隨機、難度、教學、知識卡與音效。

## 交付檔案與必要依賴

交付完整的`Assets/Levels/Prologue/`、`Dig/`、`Strata/`及其全部`.meta`，並保留本文件；不另建交接README。

| 類別 | 不可遺漏的檔案／依賴 |
|---|---|
| 序章 | `Scripts/PrologueManager.cs`、`BookDrag.cs`、`PauseMenu.cs`、`PrologueAudio.cs`、`PrologueUIStyle.cs`及資料夾內其他必要腳本、Art、Audio。 |
| 考古小隊 | `Scripts/SoilBrush.cs`、`DigManager.cs`、`ArtifactSpawner.cs`、`BrushCursorController.cs`、`DigLevelFlow.cs`、`DigLevelUI.cs`、`DigLevelAudio.cs`及其餘支援腳本；`DigArtifactCatalog.asset`。 |
| 地層解碼 | `Scripts/StrataStageOneManager.cs`、`StrataStageTwoManager.cs`、拖曳／Drop腳本、`StrataLevelFlow.cs`、`StrataLevelUI.cs`、`StrataLevelAudio.cs`、`StrataChallengeRules.cs`、Catalog／QuestionSet資料結構及其餘支援腳本。 |
| 第三關資料 | `Assets/Levels/Strata/StrataPatternQuestionSet.asset`；`Assets/Levels/Strata/Resources/StrataKnowledgeCatalog.asset`及各自Meta。 |
| 關卡外依賴 | `Assets/Common/Fonts/`、實際使用的`Assets/Common/audio/`及License；`Assets/TextMesh Pro/`內引用的Shader、資源及fallback字型。 |
| Unity環境 | `Packages/manifest.json`與`packages-lock.json`；URP 17.3.0、UGUI 2.0.0、Input System 1.19.0。保留團隊現有URP／輸入設定；三關使用New Input System，Active Input Handling維持Input System Package (New)（activeInputHandler: 1）。第二關刷土游標使用Mouse.current與Touchscreen.current；按住刷土、放開停止仍由InputSystemUIInputModule的EventSystem事件處理，不需啟用舊版Input Manager。 |

建議保留完整專案（Assets、Packages、ProjectSettings）作可重現基準；供既有主專案匯入時使用含依賴的UnityPackage，另外核對程式引用、Resources及授權文件。只交Assets/Levels會漏掉共用依賴。不要直接覆蓋主專案ProjectSettings。

### 第三關知識卡載入

`StrataLevelUI.EnsureKnowledgeData()`在建立與顯示時讀取`StrataKnowledgeCatalog`；優先使用Scene序列化引用，缺少引用時以`Resources.Load<StrataKnowledgeCatalog>("StrataKnowledgeCatalog")`載入。必須保留Resources路徑及Meta，不要產生同名資源。

`HasCompletePages`要求三頁各有唯一ID、名稱、圖片、正文及來源。頁序為磚瓦殘片、玻璃珠、印紋陶片；三頁共用左圖右文版型，最後一頁「完成關卡」才呼叫`ContinueFromKnowledge()`顯示成功結算並回報一次結果。Scene仍保留相容資料陣列，不應以清空診斷文字掩蓋Catalog缺失。失敗不顯示知識卡。

## 外部整合接口

`PrologueManager`、`DigLevelFlow`、`StrataLevelFlow` 實作 `NIUAdventure.Integration.ILevelHandoffSource`。定義在 `Assets/Levels/Prologue/Scripts/LevelHandoff.cs`，交接三關須包含這唯一一份共同檔案；不可重複複製定義。這是薄轉接，不是全組GameManager或永久收藏格式。

| 接口 | 用法 |
|---|---|
| `IsReady` | 等待啟用、初始化完成。 |
| `ConfigureIntegration(string levelId, string rewardArtifactId)` | 介紹／未開始時設定，檢查bool結果；序章獎勵只能null或空白。 |
| `BeginChallenge()` | 外部開始；序章原有`StartChallenge()`保留。勿直接啟用DigManager跳過Flow。 |
| `PauseGame()` / `ResumeGame()` | 使用關卡暫停入口。 |
| `ResetForNewChallenge()` | bool；完整重置回介紹，不自動開始、不切換Scene。 |
| `ResultReady` | `Action<LevelHandoffResult>`，每回合正式結果一次。 |
| `LastHandoffResult` | 已發布的結果，重置後清除。 |

`LevelHandoffResult`：`LevelId`、每回合去重用`AttemptId`、`Success`、`float? RemainingSeconds`、`int? MistakeCount`、單一`RewardArtifactId`、`IsRewardEligible`、`RequiresRewardConfiguration`。

- 序章沒有計時及失誤統計，兩欄為null，且不發收藏獎勵。
- 第二關MistakeCount為損傷次數；第三關為兩階段錯配加L4錯答總數。
- 第二、三關成功各提供**一件關卡獎勵資格**；失敗不發獎。關卡內三件玩法文物不是三件收藏獎勵。
- 正式LevelId與獎勵ID待主遊戲系統確認（見「待整合事項」）；Scene獎勵欄刻意留空。未設定仍可遊玩，但`RequiresRewardConfiguration`為true，不得以空ID加入收藏。未設定LevelId暫用Scene名稱。
- 開始前設定、訂閱ResultReady；卸載／銷毀外部持有者時解除訂閱。計分、地圖、背包、永久收藏及跨關去重由主流程處理。

### 保留事件與發布時機

| 事件 | 型別／時機 |
|---|---|
| 序章`PrologueCompleted`、`ResultReady` | `Action<LevelHandoffResult>`；紅、藍、黃三書依序收齊後直接觸發，一回合一次；不要求暫停教學。 |
| 序章`ContinueRequested` | `Action`；完成按鈕呼叫`RequestContinue()`，一次通知外部繼續，不自己跳關。 |
| `DigManager.ChallengeEnded` | `Action<DigLevelResult>`；玩法成功／失敗立即。 |
| `DigLevelFlow.LevelCompleted` | `Action<DigLevelResult>`；成功後看完知識卡、按完成關卡時。 |
| 第二關`ResultReady` | 失敗立即；成功於最後完成關卡按鈕。 |
| `StageOneCompleted`、`SecondStageRequested`、`StageTwoCompleted` | 原有`Action`；分別為三件及L4完成、下一階段請求、三題紋飾完成。 |
| `StrataLevelFlow.ChallengeEnded` | `Action<StrataLevelResult>`；失敗立即，成功在第三頁知識卡按「完成關卡」後，經ContinueFromKnowledge()發布。 |
| `StrataLevelFlow.LevelCompleted` | `Action<StrataLevelResult>`；成功結算發布點。 |
| 第三關`ResultReady` | 與原結算發布點一致，一回合一次。 |

考古小隊成功時立即停止玩法並播放成功音效，約0.5秒後顯示完成面板；不等待提示佇列。之後「查看文物知識 → 三頁知識卡 → 完成關卡」才正式提交成功結果。第三關則先完成三頁知識卡，再顯示成功結算。兩關ChallengeEnded的成功發布時機不同，整合時不要混用。

既有事件簽章與UnityEvent保留。主流程只選一個結果提交入口（建議ResultReady），避免原生與轉接事件各加分／發獎。序章可先保存結果，等ContinueRequested才離開。重試會產生新AttemptId，因此可能再次產生合法結果；主遊戲系統須自行決定重試的獎勵政策，並以AttemptId避免同回合重複提交。

## 暫停、重試與載入

- 序章：暫停取消拖曳、回原位並封鎖輸入；遲到EndDrag不收集。完整重置恢復三書、位置／縮放、計數、步驟與事件鎖。StartChallenge建立完整新挑戰，進行中重複呼叫忽略。仍沿用Time.timeScale；換關前應恢復或正常卸載序章。
- 第二關：失敗重試改為原Scene內重置，保留物件與外部訂閱，不依賴buildIndex、不卸載其他Additive Scene。重置泥土Alpha、隨機位置、發掘／初見／保護／危險、損傷／冷卻、時計、閃紅、提示及提交鎖；難度不變。
- 第三關：沿用原地完整重置兩階段／L4／提示／事件鎖及重新洗牌；成功先知識卡再結算。
- Additive由主流程管理各Scene roots、Canvas、EventSystem與音源啟用；只停用Flow不等於關閉整關。
- 未修改ProjectSettings、Packages或正式Scene順序。團隊目前EditorBuildSettings已啟用的Scene依序為`Assets/Scenes/Boot.unity`、`Assets/Scenes/SampleScene.unity`、`Assets/Scenes/Map_Library.unity`、`Assets/Scenes/Map_Campus.unity`；三關尚未列入，首次Player載入須由關卡整合端加入正式Build Profile／Scene List（見「待整合事項」）。

## 素材、來源與授權

以下路徑相對專案根目錄；未知授權一律待確認，不從檔名推定CC0。正式PNG與原始音訊未更動。

| 素材 | 使用／來源狀態 |
|---|---|
| `Assets/Levels/Prologue/Art/Library.png`、`Book_Red_Library_v2.png`、`Book_Blue_Library_v2.png`、`Book_Yellow_Library_v2.png`、`BookBox_Library_v2.png` | 序章背景與互動物件；OpenAI ChatGPT（AI 圖片生成），使用權依據見下方圖片紀錄；既有原圖保留。 |
| `Assets/Levels/Dig/Art/Soil.png`、`Gold.png`、`Agate.png`、`Pottery.png`、`ArchaeologyBrush.png` | 考古小隊教學示意圖；OpenAI ChatGPT（AI 圖片生成），使用權依據見下方圖片紀錄。 |
| `Assets/Levels/Strata/Art/StrataLayers.png`、`BrickFragment.png`、`GlassBead.png`、`PotteryFragment.png`、`Pattern_A.png`、`Pattern_B.png`、`Pattern_C.png` | 地層解碼教學示意圖；OpenAI ChatGPT（AI 圖片生成），非官方照片或精確復刻，使用權依據見下方圖片紀錄。 |
| `Assets/Common/Fonts/NotoSansTC-Regular.ttf`、`NIU_Adventure_NotoSansTC-Regular SDF.asset` | 三關字型；TTF內嵌name ID 13明載SIL Open Font License 1.1，name ID 14為http://scripts.sil.org/OFL；name ID 0記載Adobe 2014–2021及Reserved Font Name「Source」。SDF由該字型使用；原始下載頁與獨立完整授權文件仍待補存，不以LiberationSans授權代替。 |

### 音訊來源與授權

已於2026-10-28核對專案實際檔名與下列來源頁；音檔及Unity播放設定均未修改。

| 專案內實際路徑 | 使用關卡／用途 | 曲名或素材包／作者 | 授權與來源 |
|---|---|---|---|
| `Assets/Common/audio/forget_me_not_in_f_major_looped.ogg` | 序章BGM；0.12、Loop | Forget Me Not — Kistol | CC0；[OpenGameArt來源頁](https://opengameart.org/content/forget-me-not)（提供同名looped檔案） |
| `Assets/Common/audio/Exploration Theme.ogg` | 第二、三關BGM；各獨立音源，0.12、Loop | Exploration Theme — Cleyton Kauffman | CC0；[OpenGameArt來源頁](https://opengameart.org/content/exploration-theme) |
| `Assets/Common/audio/freesound_community-hand-brush-small-rocks-72944.mp3` | 第二關刷土Loop；0.2 | hand brush small rocks — oscaraudiogeek（Freesound）；Pixabay發布帳號freesound_community | **Pixabay Content License，非CC0**；[音效來源頁](https://pixabay.com/sound-effects/film-special-effects-hand-brush-small-rocks-72944/)、[授權摘要](https://pixabay.com/service/license-summary/) |
| `Assets/Common/audio/kenney_interface-sounds/Audio/`（實際使用檔名見下） | 三關UI／操作與回饋音效 | Interface Sounds — Kenney | CC0；[官方素材頁](https://kenney.nl/assets/interface-sounds)；本地證據：同包`License.txt` |
| `Assets/Common/audio/kenney_ui-audio/Audio/`（如`click1.ogg`、`mouseclick1.ogg`、`rollover1.ogg`、`switch1.ogg`） | 專案內既有UI音效包；三關目前選用下列Interface Sounds檔案 | UI Audio — Kenney（本地License記載Kenney Vleugels） | CC0；[官方素材頁](https://kenney.nl/assets/ui-audio)；本地證據：同包`License.txt` |

目前三關使用`kenney_interface-sounds/Audio/`內的：

- `select_001.ogg`：三關按鈕、序章點書。
- `drop_001.ogg`：序章收集、第二關發現。
- `confirmation_001.ogg`：完成／成功、第三關正確回饋。
- `error_004.ogg`：第二關損傷、第三關錯誤。
- `error_005.ogg`：第二、三關失敗；`switch_001.ogg`：第三關階段切換。

序章`Assets/Levels/Prologue/Audio/`內的`select_001.ogg`、`drop_001.ogg`、`confirmation_001.ogg`副本與Kenney同名檔雜湊一致。交接保留兩包原有`License.txt`，並保留上述作者及來源紀錄；[CC0授權頁](https://creativecommons.org/publicdomain/zero/1.0/)。

BGM沿用既有素材；暫停Pause、恢復接續，結束停止，重置重播，不建立重複音源。

### 圖片產製與文化資料限定

依專案提供者於2026-10-29確認，三關PNG圖片全部使用OpenAI ChatGPT生成，包含正式素材與保留原圖；本紀錄依該確認更新，不以PNG中繼資料是否包含生成標記作為來源待確認的理由。

- **生成工具**：OpenAI ChatGPT（AI 圖片生成）。
- **素材用途**：遊戲背景、書本、考古文物、地層、陶器紋飾及UI圖片。
- **使用性質**：AI生成之教學示意素材，非真實出土文物照片，亦非官方文物的精確復刻。
- **使用權依據**：OpenAI Terms of Use；在使用者與OpenAI之間，且於適用法律允許的範圍內，依適用條款使用者擁有生成輸出的相關權利。這是服務條款下的輸出權利安排，**不是CC0授權**。
- **權利範圍**：生成輸出可能不具唯一性，其他使用者可能取得相似輸出；本紀錄不宣稱具有排他性著作權，也不提供第三方權利保證。使用仍須符合適用法律與條款。
- **官方條款**：[OpenAI Terms of Use](https://openai.com/policies/row-terms-of-use/)（Content：Ownership of content／Similarity of content）。

適用檔案包括：

- 序章：上表五張正式圖，以及保留的`Book_Red.png`、`Book_Blue.png`、`Book_Yellow.png`、`BookBox.png`原圖。
- 考古小隊：`Soil.png`、`Gold.png`、`Agate.png`、`Pottery.png`、`ArchaeologyBrush.png`。
- 地層解碼：`StrataLayers.png`、`BrickFragment.png`、`GlassBead.png`、`PotteryFragment.png`、`Pattern_A.png`、`Pattern_B.png`、`Pattern_C.png`。紋飾用於遊戲圖案辨識，不宣稱是特定出土文物的精確紋樣復刻。

遊戲文物與紋飾圖片作教學示意，不作真實出土照片或官方精確復刻。第一階段配對是遊戲代表性規則，不代表玻璃珠、陶片僅出自單一層位。生土層資訊保留「上部文物較少、下部未發現遺物」限定，不能概括為所有生土層不可能有文物。

文化參考：[宜蘭農校遺址與考古斷面牆](https://lic.niu.edu.tw/p/404-1010-71228.php)、[出土文物](https://lic.niu.edu.tw/p/412-1010-2280.php)。來源紀錄保留於`DigArtifactCatalog.asset`、`StrataPatternQuestionSet.asset`及`Resources/StrataKnowledgeCatalog.asset`。目前第三關玩家知識卡採精簡三頁文案，上述完整限定集中記錄於本文件／資料資產，不宣稱全部仍在玩家卡片上顯示。文化資料來源不等於遊戲PNG授權。

## 待整合事項

- 由主遊戲系統確認正式Level ID、每個一般關卡的單一獎勵ID及對照表；序章不發一般關卡獎勵。本次不設定任何正式ID。
- 關卡整合端確認nullable結果欄位、ResultReady接收、AttemptId去重及重試發獎政策；七個一般關卡抽三關、總分、地圖、永久收藏與全組結算由主遊戲系統處理。
- 由整合開發者統一Build Profile／Build Scene清單與載入／離開策略。團隊目前EditorBuildSettings已啟用的Scene依序為`Assets/Scenes/Boot.unity`、`Assets/Scenes/SampleScene.unity`、`Assets/Scenes/Map_Library.unity`、`Assets/Scenes/Map_Campus.unity`；三關尚未列入，須於整合時加入正式Player載入清單。本次不改設定。
- 三關輸入需求為New Input System，Active Input Handling維持Input System Package (New)，不需設為Both。第二關游標優先使用觸控，沒有進行中的觸控時才使用桌面滑鼠；無可用裝置時隱藏游標。整合後須測試滑鼠移動、按住刷土、放開停止、單指觸控、觸控與滑鼠模擬並存時不重複刷土，以及暫停／恢復與重新挑戰後的輸入。
- 協調Additive中的Canvas、EventSystem、AudioListener及音源生命週期；序章Time.timeScale會影響全域。外部卸載時解除事件訂閱，不只停用Flow。
- 若替換共用知識卡／音效版型，保留三頁資料、來源、示意限定及原本事件發布時機。

## 待驗證與素材待補

- 舊版文件記載曾進行Player／Editor條件編譯及替身測試；不代表目前最新版已實測。本次僅修改交接文字與統計標題，未啟動Unity、Play Mode或編譯。
- 在Unity重新確認Player／Editor編譯、Console、Scene／Sprite／字型引用，以及1920×1080、1280×720、960×540畫面。靜態邊界檢查不能代替實際排版與按鈕測試。
- 序章：紅→藍→黃、錯放返回、拖曳中暫停、三書直接通關、繼續按鈕與完整重置；每回合事件一次。
- 考古小隊：慢刷／快刷、初見保護、損傷與超時失敗、警告暫停接續、成功約0.5秒轉場、三頁卡片與原地連續重試。
- 地層解碼：兩階段洗牌／拖曳、觀察題錯答與重答、共用時計、超時封鎖、Resources三頁載入與翻頁、知識卡後結算及完整重試。「第一階段失誤」包括文物錯放及觀察題錯答；「紋飾配錯次數」為第二階段失誤，總數為兩者相加。
- 關卡整合端實測ResultReady、成功／失敗／重試及Additive載入，記錄AttemptId、事件次數、時間與失誤；另測滑鼠／觸控、BGM暫停接續及音效。
- 三關PNG生成工具與產製方式已確認為OpenAI ChatGPT（AI 圖片生成），使用權依下述OpenAI條款記錄，不再列為生成來源待確認。字型已由TTF內嵌聲明確認OFL 1.1，另補原始下載來源與完整授權文件。保留既有音訊來源及授權紀錄。

三關交接為Scene、腳本、素材、本文件與必要測試紀錄。實地照片已取消；整體Demo影片待主遊戲系統完成後另行製作，不列三關交接缺失。
