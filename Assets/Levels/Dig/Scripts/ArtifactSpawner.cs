using UnityEngine;

public class ArtifactSpawner : MonoBehaviour
{
    [Header("三件考古文物")]
    [SerializeField] private RectTransform[] artifacts;

    [Header("隨機位置設定")]
    [SerializeField] private float randomOffsetX = 35f;
    [SerializeField] private float randomOffsetY = 30f;

    void Start()
    {
        RandomizeArtifacts();
    }

    // 隨機排列三件文物
    public void RandomizeArtifacts()
    {
        if (artifacts == null || artifacts.Length != 3)
        {
            Debug.LogWarning("請指定三件考古文物！");
            return;
        }

        // 六個預設安全位置
        Vector2[] positions =
        {
            new Vector2(-420, 220),
            new Vector2(0, 220),
            new Vector2(420, 220),

            new Vector2(-420, -220),
            new Vector2(0, -220),
            new Vector2(420, -220)
        };

        // 打亂位置順序
        for (int i = positions.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Vector2 temp = positions[i];
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }

        // 將三件文物放到不同位置
        for (int i = 0; i < artifacts.Length; i++)
        {
            if (artifacts[i] == null)
                continue;

            float offsetX = Random.Range(
                -randomOffsetX,
                randomOffsetX
            );

            float offsetY = Random.Range(
                -randomOffsetY,
                randomOffsetY
            );

            artifacts[i].anchoredPosition =
                positions[i] + new Vector2(offsetX, offsetY);
        }

        Debug.Log("三件考古文物已隨機排列！");
    }
}
