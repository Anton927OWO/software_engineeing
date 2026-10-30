# 終章文物記憶翻牌

**Goal:** 在目前 Worktree 完成 Unity 6000.3.19f1 可直接遊玩的 `L_Finale.unity`，重用 Shared prefab、控制器與 UI。

**Architecture:** 純 C# 配對狀態處理十二張牌；場景控制器負責動畫鎖、兩秒文物提示、共用暫停／結果與重試。Editor 選單以 Unity API 建立並儲存真正的場景。所有新資產、測試與驗證證據只放在 Finale。

**Constraints:** 不修改 Shared、Timeline、Train、Common、Packages、ProjectSettings，不切換 Worktree、不合併 main。保留原有 `.DS_Store` 與 `docs/` 變動。素材只讀取既有 Finale PNG。

**Design decisions:** 六種文物 ID 為 `artifact-01` 至 `artifact-06`，對應素材 manifest 順序。三件取得文物來自可設定的來源元件／GameManager 介面，缺少來源時使用 Inspector 測試 ID；去除未知與重複 ID 後補足三件。三件取得文物寫入共用結果；六組配對是通關條件，不額外產生六件獎勵。介紹及兩秒提示不扣 90 秒。暫停／失焦／知識卡凍結動畫與錯誤等待。

## Tasks

- [x] 先建立 Editor 場景驗收測試，實際執行，確認場景尚未建立而失敗。
- [x] 實作配對狀態與純邏輯測試：亂數牌組、同牌重點、第三張鎖定、成功保留、錯誤復原、六組完成。
- [x] 實作場景控制器、文物來源與 UI，加入真實 Scene 的 PlayMode 測試。
- [x] 用 Editor 工具建立場景，測試配對、時間、暫停、知識卡、重試與結果回報。
- [x] 執行 Finale 與 Shared 全部測試，檢查畫面與檔案範圍，記錄報告。

## Review focus

- 兩張牌與動畫進行中不得透過滑鼠／鍵盤翻第三張。
- 暫停、知識卡及失焦必須保留錯誤等待的剩餘時間。
- 提示／動畫／等待途中重新開始不得留下 coroutine 或過期結算。
- 倒數歸零後不得再接受輸入、重複結算或改寫不可變結果。
- 三件來源 ID 去重、驗證與補足；重試應重新讀取來源並清空舊結果。

## Progress

- 已閱讀 Shared README 與共用實作；目前沒有 GameManager，Unity 6000.3.19f1 已開啟此專案。
- 依使用者要求直接在目前 checkout 實作，使用原有 Shared prefab 實例及專用 Editor 工具。
- Editor 場景驗收發現 NewScene 會卸載尚未綁定的資產：改為在建立 Scene 後重新載入設定與字型，並對缺少引用立即報錯。
- 唯讀審查發現根物件停用會永久停止 coroutine：先以 PlayMode 重現，再於停用時取消未完成的選牌並保留已配對進度。
- TMP 文字容量測試發現側欄與介紹溢出：調整 Finale 生成版面與場景內 Prefab 覆寫，未修改 Shared。
- 最終結果：EditMode 30/30、PlayMode 16/16，均零失敗、零略過；已在圖形版 Editor 操作驗證。詳見 Verification/驗證報告.md。
