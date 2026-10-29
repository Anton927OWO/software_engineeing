# 尋跡宜大 朱峻廷關卡

這是 Unity 6.3 LTS（6000.3.25f1）專案。遊戲在 Unity 製作，WebGL 版本供網頁組員整合。

## 開始使用

1. Unity Hub → 新增 → 從硬碟新增專案，選擇本資料夾。
2. 開啟 `Assets/Junting/Scenes/JuntingMain.unity`，按上方 Play。
3. 按「開始探索」，系統隨機安排「書海尋寶」與「草澤擋鹿」的先後順序；玩家不能指定關卡。
4. 若是首次重新建立場景或字型，執行上方 `Junting → 1 Setup Project`，等匯入完成。此功能會重建本專案示範場景，不要在其他同學尚未儲存的場景執行。

## 已實作的個人工作

- 書海尋寶：完整索書號標籤、三本書歸位、拖放或點選放置、空位亮起、正確吸附、錯誤彈回與扣 20 分、60 秒限時。
- 草澤擋鹿：鏽鐵色鹿剪影、起始路線、放置／收回擋板、最多五個、碰擋板順時針轉向、三隻鹿入區、入區閃亮、出界提示、60 秒限時。
- 前置解說：3～5 行空間介紹、本關操作說明、來源按鈕。只有按「開始挑戰」才計時。
- 結尾問答：每關兩題；答錯附解說並可重試，不扣遊戲分；完成全部問答後領卡。
- 文物知識卡：插畫、知識、官方來源、外部傳入得分明細；收藏以 PlayerPrefs 保存在本機／同一瀏覽器來源。
- 暫停：凍結計時、移動與互動；音樂、音效分開控制。視窗失焦時自動暫停。
- 專屬 BGM：書海尋寶綁定 `bgm_books.wav`，草澤擋鹿綁定 `bgm_deer.wav`，兩首 72 秒原創合成音樂不同；另保留終章音樂給全組整合使用。
- 每關只綁定一件文物：書海尋寶為幾何印紋陶罐，草澤擋鹿為木鏟。通關事件的 `artifactCount` 固定為 1，失敗為 0。
- 12 種共用音效位於 `Assets/Common/Audio/Resources/Audio`。

## 隨機順序與全組接入

本次只實作朱峻廷負責的兩關。獨立版每輪以 Fisher-Yates 洗牌，可能先書海或先擋鹿，兩關各出現一次；完成後顯示本輪探索完成。隨機不保證連續兩輪必定顛倒。

未完成關卡若返回首頁、失敗後重試，仍保留同一個抽中關卡，不能以返回首頁重抽跳關。BGM 隨實際關卡資料切換，不依它在本輪排第幾關決定。

正式 7+2 共九關由主系統管理；本專案不加入其餘七關的遊戲內容。主系統抽中本人的關卡時，可用以下接點：

```csharp
game.SetExternalProgression(true);
game.StartLevel(selectedLevelId); // books 或 deer，由主系統指定
game.NextLevelRequested += () => mainGame.RequestNextAssignedLevel();
```

外部模式的「繼續探索」只送出下一關請求，不會在本人兩關中自行抽取。網頁可接收 `niu:next-level-request`；由主系統決定下一關與何時結束。文物知識卡依 `LevelData` 的單一文物欄位顯示，不一次發放多件。

## 玩法

**書海尋寶**：比較分類號，把下方的三本書拖回黃色空位。可先點一本，再點空位，方便觸控。練習題的分類號刻意互不相同，聚焦由小到大的基本排架。它們不是實際館藏或現場書架位置；正式擴充同號／特藏題目前，應由圖書館確認排架細則。

**草澤擋鹿**：先安排擋板再按「放行」。鹿向右前進，前方被擋住時順時針轉 90 度，下一步才移動；三隻鹿分批出發。放行後不能再移動擋板。範例解法：第 10 欄第 2 列放一個擋板，鹿會在第 9 欄轉向下方草澤。這是可解的路徑謎題，遊戲機制不作為歷史狩獵方式的證據。

## 接入全組計分

`IScoreProvider` 是計分接點。呼叫 `game.SetScoreProvider(yourProvider)` 換成游士霆的系統；知識卡只讀取 `LevelResult`，不自行重算。

示範 `DemoScoreProvider` 暫採基礎分 300、每剩餘整秒加 2 分、每次歸位錯誤扣 20 分，最低 0 分。這個完整公式是本專案示範設定，不是 Word 已指定的全組公式。

`LevelCompleted` 在挑戰及問答完成後只發出一次；`LevelFailed` 回報逾時或鹿群出界。`runId` 可供整合端避免重複計分。

## 組員重用共用版型

引用 `NIU.Junting` 命名空間，取得場景中 `JuntingGame` 後：

```csharp
game.PresentIntroduction(levelData, () => StartMyMiniGame(), () => ReturnToMap());
game.PresentKnowledgeCard(levelData, receivedScores, () => ReturnToMap());
```

`LevelData` 使用相同資料欄位：照片 Resources 路徑、圖說、介紹、規則、專屬 bgmKey、單一文物插畫、知識與來源。前置介紹需提供 3～5 行、每行不超過 14 字。外部關卡回呼時會停用本模組 Canvas，讓其他場景接手；顯示知識卡時會重新啟用。共用版型呼叫不替組員決定其通關、收藏或計分政策。

## WebGL 交接

- 由 Unity Hub 安裝對應版本 Web Build Support。
- `Junting → 3 Build WebGL` 會建置到專案旁的 `NIU_Junting_WebGL`。
- 目前採無壓縮輸出，方便交接時以一般靜態 HTTP 伺服器測試；正式主機若改為 Brotli/Gzip，應同步設定正確 HTTP 標頭。
- WebGL 內的遊戲仍由 Unity C# 執行，網頁僅負責載入 Canvas。
- 同學可直接把整份建置資料夾放到網站子目錄，或用 iframe 嵌入 `index.html`。
- 不要直接雙擊 `index.html` 以 file:// 開啟；要用 HTTP/HTTPS。
- 遊戲音訊需玩家第一次點擊後才能由瀏覽器允許播放。
- `WebBridge.cs` 與 `NIU.jslib` 提供同頁的 `niu:level-complete`／`niu:level-failed` 事件。iframe 的事件位於 iframe 自己的 window；父頁需另外經過雙方約定的 postMessage 轉送。

```js
window.addEventListener('niu:level-complete', e => {
  console.log(e.detail.levelId, e.detail.totalScore, e.detail.artifactId, e.detail.artifactCount);
});
// 完成載入後切到指定關卡的前置解說
window.unityInstance.SendMessage('JuntingGame', 'StartLevel', 'books');
```

## 素材與正式上線前的替換

- 噶瑪蘭文化廊道介紹使用宜大官方實景照片，畫面與素材紀錄均列來源。
- 5F 空間介紹目前使用官方五樓平面圖，已明確標示為平面圖。**Word 要求的五樓實景照片仍需由本人／組員提供與確認使用權後替換**，不要把其他樓層或空間的照片當作五樓中文圖書區。
- 文物插畫依官方照片外形簡化，知識內容連回官方頁面。文物配置是本模組示範獎勵，並不表示該關場域有文物出土；最終全組文物分配可在 JSON 調整。
- 字型附 Noto Sans TC 原檔及 SIL OFL 授權。音效、音樂為程式原創合成，附 CC0 宣告。未使用傳統歌謠錄音。
- 官方照片不是 CC0 宣告範圍。課堂展示與公開上線的利用方式，請依官方授權條件處理。

修改關卡文字與問答：`Assets/Junting/Resources/Junting/levels.json`。
修改遊戲規則：`Assets/Junting/Scripts/GameModels.cs`。
修改畫面：`Assets/Junting/Scripts/JuntingGame.cs`。

## 驗證

`Junting → 2 Validate Game Rules` 可重跑規則與資料驗證。
`Junting → Run Full Play Mode Smoke Test` 可重跑 Unity 端完整流程；測試會實際寫入本機收藏進度。

實際驗證結果另見同資料夾的 `驗證紀錄.md`。不把尚未測試的瀏覽器／手機型號列為已通過。
