# task.md｜MR 原型分工與進度

> 期限尚未確定，以下用「第 N 週」表示，定下期限後換成日期。
> 狀態：`[ ]` 未開始、`[~]` 進行中、`[x]` 完成。完成時在 PR 裡一併更新本檔。
> 規則見 `AGENTS.md`，技術細節見 `ARCHITECTURE.md`，玩法見 `GOD.md`。

## 分工總覽

| 成員 | 負責 | 依 git 記錄的原有經驗 |
|---|---|---|
| **Anton927OWO** | MR 基礎建設、共用互動元件、主流程、桌上地圖、整合 | GameManager、LevelSession、地圖、專案設定、整合 |
| **Teatarossa** | 百年軌跡、知識列車、終章、關卡共用面板 | Train、Timeline、Finale、Shared、Integration |
| **Juelang0305** | 序章、考古小隊、地層解碼 | Prologue、Dig、Strata、共用音效 |
| **Chun** | 草澤擋鹿、共用 3D 素材總管 | 書海尋寶、草澤擋鹿 |

## 依賴關係

```
A0 套件＋MRCore＋關卡範本 ──┬─> 各關原型（B / C / D）
                            └─> A1 共用互動元件 ─> 各關正式互動
B0 共用面板 ───────────────────> 各關介紹／結算
D0 素材規格 ─> D1 文物模型 ───> 考古、地層、終章、地圖
```

**最先要完成的是 A0**。A0 合併之前，其他人先做第 0 週的設計與素材工作，不要自己裝套件。

---

## 第 0 週：準備（大家並行）

### Anton927OWO
- [ ] **A0** `feature/xr-core`：安裝 Meta XR Core SDK、Interaction SDK、MRUK、XR Simulator；切換 Android；調整 URP 給 Quest 的設定
- [ ] **A0** 建立 `Common/XR/`、`TrackingNIU.XR.asmdef`、`MRCore` prefab（玩家物件＋passthrough）
- [ ] **A0** `XRLevelRoot`、`XRLevelTestLauncher`、空白範本場景 `Common/XR/Templates/L_Template_XR.unity`
- [ ] `LevelCatalog`：書海尋寶設為 `availableInMidterm: false`，同步更新受影響的測試
- **完成條件**：任何人複製範本場景按 Play，用 XR Simulator 看得到 passthrough 背景與雙手

### Teatarossa
- [ ] 百年軌跡、知識列車、終章的 MR 玩法定稿，更新 `GOD.md` 第 6 節
- [ ] 設計共用面板的 API，寫在 `Levels/Shared/XR/README.md`

### Juelang0305
- [ ] 序章、考古小隊、地層解碼的 MR 玩法定稿，更新 `GOD.md` 第 6 節
- [ ] 決定 2D 版規則邏輯要抽成 asmdef 共用，還是在 XR 版重寫（見 `ARCHITECTURE.md` 第 4 節）

### Chun
- [ ] 草澤擋鹿的 MR 玩法定稿，更新 `GOD.md` 第 6 節
- [ ] **D0** 建立 `Art3D/` 與 `素材來源與授權.csv`，確認素材規格（`ARCHITECTURE.md` 第 8 節）
- [ ] 列出全組需要的 3D 素材清單（向每位負責人確認），標上優先順序與來源（現成／AI 生成／自製）

### 全組
- [ ] 決定木鏟改給哪一關
- [ ] 確認原型期限、頭盔數量，排好實機測試時段

---

## 第 1–2 週：共用元件＋灰盒原型

> 「灰盒」＝用方塊、圓柱等簡單形狀代替正式模型，先把玩法做出來。

### Anton927OWO
- [ ] **A1** `TableAnchor`：MRUK 偵測桌面；偵測不到時生成虛擬檯面；可以抓住把手調整位置
- [ ] **A1** `XRGrabbable`、`XRSnapSocket`（含放錯彈回）
- [ ] **A1** `BrushTool`（接觸點、速度、按壓深度）、`XRPokeTarget`
- [ ] **A1** `Haptics`、`WorldPanel` prefab、暫停鍵 → 既有暫停流程
- [ ] 每個元件附一個示範場景，放在 `Common/XR/Samples/`
- **完成條件**：示範場景可以抓起方塊放進插槽、放錯會彈回並震動

### Teatarossa
- [ ] **B0** `Levels/Shared/XR`：`XRIntroPanel`、`XRHud`、`XRPausePanel`、`XRResultPanel`
- [ ] 百年軌跡灰盒：卡片插入時間軸插槽，接上原版規則
- **完成條件**：B0 可以讓其他關直接拖進場景使用；百年軌跡可以從介紹玩到結算

### Juelang0305
- [ ] 考古小隊灰盒：刷子接觸土堆讓土層變薄（先做覆蓋率計算，再做視覺）
- [ ] 序章灰盒：三本書依序放進書箱
- **完成條件**：考古小隊在 Simulator 中可以刷出文物並判定成功／失敗

### Chun
- [ ] 草澤擋鹿灰盒：桌上沙盤、鹿的移動路徑、放置柵欄
- [ ] **D1** 6 件文物的 3D 模型（優先：考古小隊、地層、終章會用到）

---

## 第 3–4 週：完成所有關卡玩法

### Anton927OWO
- [ ] `GameManager` 優先載入 `L_<Id>_XR`，不存在時退回 2D 版
- [ ] `Map_XR.unity`：桌上校園／圖書館模型，點地點進關（第一版可以先用 WorldPanel 顯示現有地圖）
- [ ] 啟動時的桌面設定流程
- [ ] 每週從 Boot 跑一次完整流程，把問題開成 issue 指派給負責人

### Teatarossa
- [ ] 知識列車灰盒 → 可玩
- [ ] 終章：在桌上翻牌、配對成功的文物立起變成 3D 模型
- [ ] 三關都接上 `LevelSession` 與共用面板

### Juelang0305
- [ ] 地層解碼兩階段：文物塞進地層、紋飾片放到陶器
- [ ] 考古小隊：損傷判定、震動警告、知識卡
- [ ] 三關都接上 `LevelSession` 與共用面板

### Chun
- [ ] 草澤擋鹿接上 `LevelSession` 與共用面板
- [ ] 道具模型：書、書箱、刷子、卡片、柵欄、鹿、列車
- [ ] 地層斷面模型、校園模型（給 Anton927OWO 的地圖用）

**第 4 週結束的里程碑**：從 Boot 開始，序章 → 地圖 → 3 關 → 終章 → 結算可以在頭盔上完整玩一輪（允許還有灰盒物件）。

---

## 第 5–6 週：正式素材、手感、試玩

### 所有人
- [ ] 把灰盒換成正式模型
- [ ] 在 Quest 3 實機調整：物件大小、抓取距離、文字大小、震動強度
- [ ] 依試玩回饋調整限時與失誤扣分（數值改動向 Anton927OWO 提出，由他改 `LevelCatalog`）
- [ ] 補互動音效（從物件位置發出）

### Anton927OWO
- [ ] 效能檢查：維持 72 fps 以上（`ARCHITECTURE.md` 第 9 節）
- [ ] 安排至少 3 位組外的人試玩，整理回饋

### Chun
- [ ] 檢查所有素材的授權紀錄是否完整

---

## 加分項（有時間再做）

- [ ] 手勢追蹤：不拿手把也能抓取、刷土（Anton927OWO）
- [ ] 桌上地圖做成完整的 3D 立體校園（Anton927OWO＋Chun）
- [ ] 挑一關做成 VR 沉浸場景（例如進入記憶庫展區）
- [ ] 黃春明撕畫工作室 MR 版

## 每週固定事項

- 週會：每人報告完成、卡住、下週要做的事；更新本檔狀態
- 共用元件或共用面板的 API 有變動時，負責人要在週會或群組通知所有人
