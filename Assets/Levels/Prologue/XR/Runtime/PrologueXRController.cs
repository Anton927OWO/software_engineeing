using System;
using TrackingNIU;
using TrackingNIU.XR;
using UnityEngine;

namespace XunJiYiDa.Levels.Prologue.XR
{
    /// <summary>
    /// 序章 MR 版：依照紅 → 藍 → 黃的順序，把桌上的書抓起來放進書箱。
    /// 放錯的書會彈回原位並震動；三本收齊即通關（序章不計時、不計分）。
    /// 同時是「抓取＋插槽」的教學關。
    /// </summary>
    public sealed class PrologueXRController : MonoBehaviour
    {
        [Serializable]
        public struct BookInfo
        {
            public XRGrabbable book;
            public string displayName;  // 例如「紅色」
            public string colorHex;     // 面板上的文字顏色
        }

        [SerializeField, Tooltip("依照收集順序排列")]
        BookInfo[] books = Array.Empty<BookInfo>();

        [SerializeField] XRSnapSocket bookBox;

        [SerializeField, Tooltip("書放進書箱後停的位置，依收集順序")]
        Transform[] boxSlots = Array.Empty<Transform>();

        [SerializeField, Tooltip("面板底邊在桌面上的位置（桌面座標）")]
        Vector3 panelPosition = new Vector3(0f, 0.02f, 0.22f);

        [Header("音效（可留空）")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip clickClip;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;
        [SerializeField] AudioClip completeClip;

        BookOrderRule _rule;
        WorldPanel _panel;
        bool _playing;
        bool _finished;

        void Start()
        {
            var ids = new string[books.Length];
            for (var i = 0; i < books.Length; i++)
            {
                ids[i] = books[i].book.ItemId;
                books[i].book.SetGrabEnabled(false);
            }
            _rule = new BookOrderRule(ids);

            bookBox.Validator = item => _rule.IsNext(item.ItemId);
            bookBox.Accepted += OnAccepted;
            bookBox.Rejected += OnRejected;

            var context = LevelSession.Current;
            _panel = WorldPanel.Create(transform, panelPosition, context != null ? context.DisplayName : "序章：知識之林尋書",
                $"知識之林的書散落一地，幫忙把書收回書箱。\n收集順序：{OrderText()}\n用握把鍵抓起書本，放進右邊的書箱。");
            _panel.AddButton("開始", BeginPlay);
        }

        void OnDestroy()
        {
            if (bookBox == null)
                return;
            bookBox.Accepted -= OnAccepted;
            bookBox.Rejected -= OnRejected;
        }

        void BeginPlay()
        {
            if (_playing || _finished)
                return;
            _playing = true;
            Play(clickClip);
            foreach (var info in books)
                info.book.SetGrabEnabled(true);
            _panel.ClearButtons();
            ShowHint(null);
        }

        void OnAccepted(XRSnapSocket socket, XRGrabbable item)
        {
            if (!_playing || !_rule.TryCollect(item.ItemId))
                return;

            var slot = _rule.Collected - 1 < boxSlots.Length ? boxSlots[_rule.Collected - 1] : null;
            item.LockAt(slot);
            Haptics.Play(item.LastHolder, HapticPreset.Correct);
            Play(correctClip);

            if (_rule.IsComplete)
                Complete();
            else
                ShowHint(null);
        }

        void OnRejected(XRSnapSocket socket, XRGrabbable item)
        {
            if (!_playing)
                return;
            Haptics.Play(item.LastHolder, HapticPreset.Wrong);
            Play(wrongClip);
            ShowHint("順序不對，書彈回去了！");
        }

        void ShowHint(string warning)
        {
            var next = Array.FindIndex(books, b => b.book.ItemId == _rule.NextId);
            var text = next >= 0 ? $"請把 {Colored(books[next])} 的書放進書箱" : "";
            if (!string.IsNullOrEmpty(warning))
                text = warning + "\n" + text;
            _panel.SetText($"收集書本 {_rule.Collected} / {_rule.Total}", text + $"\n收集順序：{OrderText()}");
        }

        void Complete()
        {
            _playing = false;
            _finished = true;
            Play(completeClip);
            Haptics.PlayBoth(HapticPreset.Correct);

            if (LevelSession.IsActive)
                LevelSession.Report(new LevelResult { Cleared = true });

            _panel.SetText("找齊三本書了！", "知識之林恢復了秩序。\n準備前往圖資館，展開尋跡宜大的旅程。");
            _panel.AddButton("繼續", Continue);
        }

        void Continue()
        {
            Play(clickClip);
            if (LevelSession.IsActive && LevelSession.HasReported)
                LevelSession.Exit();
        }

        string OrderText()
        {
            var parts = new string[books.Length];
            for (var i = 0; i < books.Length; i++)
                parts[i] = Colored(books[i]);
            return string.Join(" → ", parts);
        }

        static string Colored(BookInfo info) =>
            string.IsNullOrEmpty(info.colorHex) ? info.displayName : $"<color={info.colorHex}>{info.displayName}</color>";

        void Play(AudioClip clip)
        {
            if (sfx != null && clip != null)
                sfx.PlayOneShot(clip);
        }
    }
}
