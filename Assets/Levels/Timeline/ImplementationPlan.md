# 百年軌跡實作計畫

需求來源：本次使用者指示與 `Assets/Levels/Shared/README_共用基礎.md`。

目標：Unity 6000.3.19f1 真實場景，45 秒、五張隨機學制卡、五格拖放判題，沿用共用介紹、知識卡、暫停、重試與結果接口。

限制：目前 Worktree 內只交付 Timeline 及專用 Editor／測試檔案；不改 Shared、其他關卡、Packages、ProjectSettings，不合併 main。Editor 在暫存副本執行並只帶回本關資產。

- [x] 場景契約測試先行：已儲存 Scene、Shared Prefab 實例、45 秒設定、五卡五格、一組 Camera／EventSystem、共用中文字型。
- [x] 拖放與重置：TimelineBoard 管理進度與 Shared 接口；TimelineCard 管理指標拖曳、吸附固定、彈回；TimelineSlot 接收有效拖放。狀態中斷取消拖曳，動畫採 unscaledDeltaTime。重試保證與上一局順序不同。
- [x] Editor 產生器：建立 TimelineDefinition、素材副本和真實 L_Timeline.unity，不覆蓋既有場景，不儲存無關 dirty 資產，不改 Build Settings。
- [x] 實際原生 EditMode／PlayMode：錯放與放空一次失誤、正確固定、五卡成功且只結算一次、逾時中斷、暫停／知識卡禁止拖曳和倒數、重試全清、Input System 真實滑鼠拖放。
- [x] 原生渲染視覺檢查、範圍雜湊檢查、操作與測試報告。

內容依官方校史整理五個素材對應節點（1926／1946／1988／1998／2003），知識卡補充 1967／1969 的職校改名，避免將五張素材誤稱為完整校史。

來源：https://www.niu.edu.tw/p/412-1000-1049.php 及 https://lic.niu.edu.tw/p/412-1010-3425.php （查核：2026-10-26）。

完成紀錄：場景不存在的先行測試確認失敗；生成後發現 NewScene 卸載設定引用，改在新場景建立後重新載入資產。獨立唯讀審查發現拖曳門檻偏移與右鍵干擾，分別由回歸測試重現後修正。完整測試 EditMode 19/19、PlayMode 13/13 通過，含 Shared 回歸。
