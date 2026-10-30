# 朱峻廷共用音訊與圖片

圖片及音訊已補入團隊主專案的 Assets 目錄。本次只新增素材與本說明，不取代團隊既有字型、程式、場景或專案設定。

- 共用音訊：`Assets/Common/Audio/Resources/Audio/`，共 15 個 WAV，含兩關專屬 BGM、終章 BGM 與 12 個音效。原創合成音訊附 `Assets/Common/Audio/CC0.txt`。
- 關卡圖片：`Assets/Junting/Resources/Junting/`，共 6 張。library.jpg 是使用者提供的五樓中文圖書區實景；corridor.jpg 來源為 https://lic.niu.edu.tw/p/404-1010-63584.php ，保留原權利。pot.png、paddle.png、deer.png、rounded.png 為程式產生的簡化圖像；文物外形參考 https://lic.niu.edu.tw/p/412-1010-6107.php 。使用者照片與官方照片不屬於音訊 CC0 範圍。
- 程式可用 `Resources.Load<AudioClip>("Audio/bgm_books")`、`Resources.Load<AudioClip>("Audio/bgm_deer")` 與 `Resources.Load<Texture2D>("Junting/library")` 載入。
- 素材與先前獨立測試專案完全相同，GUID 一併保留。`library.jpg.meta` 保留 nPOTScale: 0，避免直式照片變形。
- 兩關的獨立原始專案、WebGL 測試建置及完整測試紀錄仍位於 `Contributions/Junting/`；本次素材上架不表示已接入主流程。
- 圖片、音訊採 Git LFS 保存。以 Git 取得專案後請確認已執行 `git lfs pull`；直接下載 ZIP 的 LFS 還原行為取決於儲存庫設定。
