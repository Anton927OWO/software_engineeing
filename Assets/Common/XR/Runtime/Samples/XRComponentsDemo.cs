using UnityEngine;

namespace TrackingNIU.XR.Samples
{
    /// <summary>
    /// 共用元件示範（Common/XR/Samples/XR_Components_Demo.unity）：
    /// 抓方塊放進插槽、放錯彈回並震動、戳按鈕、用刷子刷表面。各關可以照著這裡的寫法使用。
    /// </summary>
    public sealed class XRComponentsDemo : MonoBehaviour
    {
        [SerializeField] XRSnapSocket socket;
        [SerializeField] XRPokeTarget resetButton;
        [SerializeField] XRGrabbable[] grabbables;
        [SerializeField] Vector3 panelPosition = new Vector3(0f, 0.02f, 0.25f);

        WorldPanel _panel;

        void Start()
        {
            _panel = WorldPanel.Create(transform, panelPosition, "共用元件示範",
                "把藍色方塊放進綠色插槽（紅色會彈回）。\n戳黃色按鈕重來，拿刷子刷右邊的土塊。");
            socket.Accepted += (_, item) =>
            {
                Haptics.Play(item.LastHolder, HapticPreset.Correct);
                _panel.SetBody($"放對了：{item.ItemId}");
            };
            socket.Rejected += (_, item) =>
            {
                Haptics.Play(item.LastHolder, HapticPreset.Wrong);
                _panel.SetBody($"放錯了：{item.ItemId}，會彈回原位。");
            };
            resetButton.Poked += (_, __) => ResetDemo();
        }

        void ResetDemo()
        {
            foreach (var item in grabbables)
                item.ResetToHome();
            socket.ResetSocket();
            _panel.SetBody("已重來。");
        }
    }
}
