using System;

namespace XunJiYiDa.Levels.Prologue.XR
{
    /// <summary>序章的收書規則：必須依照指定順序放進書箱。不依賴 Unity，可以直接寫測試。</summary>
    public sealed class BookOrderRule
    {
        readonly string[] _order;

        public BookOrderRule(params string[] order)
        {
            if (order == null || order.Length == 0)
                throw new ArgumentException("至少要有一本書", nameof(order));
            _order = (string[])order.Clone();
        }

        public int Total => _order.Length;
        public int Collected { get; private set; }
        public bool IsComplete => Collected >= _order.Length;

        /// <summary>下一本要放的書；全部收齊時為 null。</summary>
        public string NextId => IsComplete ? null : _order[Collected];

        public bool IsNext(string id) => !IsComplete && id == _order[Collected];

        /// <summary>是下一本就收下並回傳 true；否則不變並回傳 false。</summary>
        public bool TryCollect(string id)
        {
            if (!IsNext(id))
                return false;
            Collected++;
            return true;
        }

        public void Reset() => Collected = 0;
    }
}
