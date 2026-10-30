using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class SoilBrush : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerUpHandler
{
    [Header("刷土設定")]
    [SerializeField] private int brushRadius = 16;

    [Header("每秒清土強度")]
    [SerializeField, Range(0.1f, 10f)]
    private float soilRemovalPerSecond = 2.5f;

    [Header("平均速度設定")]
    [SerializeField] private float speedWindow = 0.25f;

    [Header("文物露出面積判定")]
    [SerializeField, Range(0f, 0.5f)]
    private float revealedSoilOpacity = 0.2f;

    [Header("高速接觸文物的清土效率")]
    [SerializeField] private DigManager digManager;
    [SerializeField, Range(0f, 1f)] private float fastArtifactRemovalEfficiency = 0.3f;
    private byte[] artifactCoverage;
    private RevealMask[] efficiencyMasks;
    private int unfinishedArtifactBits;
    private bool fastRemovalThisStroke;

    private bool masksWarmed;
    private bool textureDirty;
    private int[] paintStamps;
    private int paintStamp;
    private Sprite runtimeSoilSprite;

    private sealed class RevealMask
    {
        public Sprite sprite;
        public Rect drawingRect;
        public Rect soilRect;
        public Matrix4x4 toSoil;
        public int[] indices;
        public int[] weights;
        public int total;
        public bool preserveAspect;
    }

    private readonly Dictionary<RectTransform, RevealMask> revealMasks =
        new Dictionary<RectTransform, RevealMask>();

    private Image soilImage;
    private RectTransform rectTransform;

    private Texture2D soilTexture;
    private Color32[] pixels, initialPixels;
    public bool CanResetSoil => soilTexture!=null && pixels!=null && initialPixels!=null;
    private float[] soilAlpha;

    private int textureWidth;
    private int textureHeight;

    private Vector2Int? previousPoint;

    private struct SpeedSample
    {
        public float time;
        public float distance;
    }

    private readonly Queue<SpeedSample> speedSamples =
        new Queue<SpeedSample>();

    // Integer stamps avoid per-pixel hashing and duplicate removal within a drag event.

    private float totalDistance;
    private float lastMotionTime;

    private int activePointerId;
    private bool pointerActive;

    public event Action OnSoilChanged;

    public event Action<Vector2, Vector2, Camera, float, float>
        OnBrushMotion;

    public event Action OnBrushReleased;

    void Start()
    {
        soilImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (soilImage.sprite == null)
        {
            Debug.LogError("SoilOverlay 尚未指定泥土圖片！");
            enabled = false;
            return;
        }

        Texture2D originalTexture = soilImage.sprite.texture;

        textureWidth = originalTexture.width;
        textureHeight = originalTexture.height;

        pixels = originalTexture.GetPixels32();
        initialPixels = (Color32[])pixels.Clone();

        soilAlpha = new float[pixels.Length];
        paintStamps = new int[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            soilAlpha[i] = pixels[i].a;
        }

        soilTexture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            false
        );

        soilTexture.filterMode = FilterMode.Bilinear;
        soilTexture.SetPixels32(pixels);
        soilTexture.Apply();

        Sprite newSprite = Sprite.Create(
            soilTexture,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f)
        );

        runtimeSoilSprite = newSprite;
        soilImage.sprite = newSprite;

        Debug.Log("刷土系統初始化完成！");
    }

    public bool ResetSoil()
    {
        if(!CanResetSoil) return false;
        pointerActive=false;
        ResetMotion();
        Array.Copy(initialPixels,pixels,pixels.Length);
        for(int i=0;i<soilAlpha.Length;i++) soilAlpha[i]=initialPixels[i].a;
        Array.Clear(paintStamps,0,paintStamps.Length); paintStamp=0;
        revealMasks.Clear(); masksWarmed=false;
        artifactCoverage=null; efficiencyMasks=null; unfinishedArtifactBits=0;
        textureDirty=true;
        FlushSoil(false); // Reset is not an excavation event.
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerActive)
            return;

        pointerActive = true;
        activePointerId = eventData.pointerId;

        ResetMotion();

        // Seed the first stroke without removing soil on a click.
        Vector2 localPoint;
        if (rectTransform != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Rect rect = rectTransform.rect;
            if (rect.width > 0f && rect.height > 0f && rect.Contains(localPoint))
                previousPoint = new Vector2Int(
                    Mathf.RoundToInt((localPoint.x - rect.xMin) / rect.width * (textureWidth - 1)),
                    Mathf.RoundToInt((localPoint.y - rect.yMin) / rect.height * (textureHeight - 1)));
        }

        // 點擊只開始操作，不直接清土
        // 避免連續點擊繞過刷動速度判定
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (soilTexture == null ||
            !pointerActive ||
            eventData.pointerId != activePointerId)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();

        float scale = canvas != null
            ? Mathf.Max(0.01f, canvas.scaleFactor)
            : 1f;

        float now = Time.unscaledTime;
        float elapsed = now - lastMotionTime;

        if (elapsed > speedWindow)
        {
            speedSamples.Clear();
            totalDistance = 0f;
            previousPoint = null;
        }

        float dt = Mathf.Clamp(elapsed, 0.001f, 0.05f);

        float distance = eventData.delta.magnitude / scale;
        float instantSpeed = distance / dt;

        speedSamples.Enqueue(new SpeedSample
        {
            time = now,
            distance = distance
        });

        totalDistance += distance;

        while (speedSamples.Count > 0 &&
               now - speedSamples.Peek().time > speedWindow)
        {
            SpeedSample sample = speedSamples.Dequeue();

            totalDistance -= sample.distance;
        }

        totalDistance = Mathf.Max(0f, totalDistance);

        float averageSpeed =
            totalDistance / Mathf.Max(0.01f, speedWindow);

        lastMotionTime = now;

        Vector2 startPoint =
            eventData.position - eventData.delta;

        OnBrushMotion?.Invoke(
            startPoint,
            eventData.position,
            eventData.pressEventCamera,
            averageSpeed,
            instantSpeed
        );

        if (!isActiveAndEnabled)
            return;

        fastRemovalThisStroke = digManager != null && digManager.IsFastBrushStroke;
        if (fastRemovalThisStroke) PrepareFastRemovalRegions();
        BrushAt(eventData, dt);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndPointer(eventData);
    }

    private void EndPointer(PointerEventData eventData)
    {
        if (!pointerActive ||
            eventData.pointerId != activePointerId)
        {
            return;
        }

        pointerActive = false;

        ResetMotion();

        OnBrushReleased?.Invoke();
    }

    private void ResetMotion()
    {
        fastRemovalThisStroke = false;
        previousPoint = null;

        speedSamples.Clear();
        totalDistance = 0f;

        lastMotionTime = Time.unscaledTime;
    }

    private void BrushAt(
        PointerEventData eventData,
        float deltaTime)
    {
        Vector2 localPoint;

        bool success =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

        if (!success)
            return;

        Rect rect = rectTransform.rect;

        float normalizedX =
            (localPoint.x - rect.xMin) / rect.width;

        float normalizedY =
            (localPoint.y - rect.yMin) / rect.height;

        if (normalizedX < 0f || normalizedX > 1f ||
            normalizedY < 0f || normalizedY > 1f)
        {
            previousPoint = null;
            return;
        }

        int pixelX = Mathf.RoundToInt(
            normalizedX * (textureWidth - 1)
        );

        int pixelY = Mathf.RoundToInt(
            normalizedY * (textureHeight - 1)
        );

        Vector2Int currentPoint =
            new Vector2Int(pixelX, pixelY);

        // 本次拖曳所有刷圈共用一份清除量
        if (paintStamp == int.MaxValue)
        {
            Array.Clear(paintStamps, 0, paintStamps.Length);
            paintStamp = 0;
        }
        paintStamp++;

        float alphaReduction =
            255f * soilRemovalPerSecond * deltaTime;

        if (previousPoint.HasValue)
        {
            Vector2Int start = previousPoint.Value;

            float distance = Vector2.Distance(
                start,
                currentPoint
            );

            int steps = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance / Mathf.Max(1f, brushRadius * 0.5f)
                )
            );

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;

                int x = Mathf.RoundToInt(
                    Mathf.Lerp(start.x, currentPoint.x, t)
                );

                int y = Mathf.RoundToInt(
                    Mathf.Lerp(start.y, currentPoint.y, t)
                );

                EraseCircle(x, y, alphaReduction);
            }
        }
        else
        {
            EraseCircle(
                pixelX,
                pixelY,
                alphaReduction
            );
        }

        previousPoint = currentPoint;

    }

    // Upload at most once per rendered frame, before that frame is displayed.
    void LateUpdate()
    {
        if (!masksWarmed && soilTexture != null)
        {
            masksWarmed = true;
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Image[] images = parentCanvas != null
                ? parentCanvas.GetComponentsInChildren<Image>()
                : new Image[0];
            foreach (Image candidate in images)
            {
                if (candidate != soilImage && candidate.transform.parent == transform.parent &&
                    candidate.sprite != null)
                    GetRevealedRatio(candidate.rectTransform);
            }
        }
        FlushSoil(true);
    }

    private void FlushSoil(bool notify)
    {
        if (!textureDirty || soilTexture == null)
            return;

        textureDirty = false;
        soilTexture.SetPixels32(pixels);
        soilTexture.Apply(false, false);

        if (notify)
            OnSoilChanged?.Invoke();
    }

    private void EraseCircle(
        int centerX,
        int centerY,
        float alphaReduction)
    {
        int radiusSquared = brushRadius * brushRadius;

        int minX = Mathf.Max(0, centerX - brushRadius);
        int maxX = Mathf.Min(textureWidth - 1, centerX + brushRadius);

        int minY = Mathf.Max(0, centerY - brushRadius);
        int maxY = Mathf.Min(textureHeight - 1, centerY + brushRadius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy > radiusSquared)
                    continue;

                int index = y * textureWidth + x;

                // Do not revisit overlapping circles or already clear soil.
                if (paintStamps[index] == paintStamp || soilAlpha[index] <= 0f)
                    continue;

                paintStamps[index] = paintStamp;
                float efficiency = fastRemovalThisStroke && artifactCoverage != null &&
                    (artifactCoverage[index] & unfinishedArtifactBits) != 0
                    ? Mathf.Clamp01(fastArtifactRemovalEfficiency) : 1f;
                soilAlpha[index] = Mathf.Max(0f, soilAlpha[index] - alphaReduction * efficiency);
                byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(soilAlpha[index]), 0, 255);
                if (pixels[index].a != alpha)
                {
                    Color32 color = pixels[index];
                    color.a = alpha;
                    pixels[index] = color;
                    textureDirty = true;
                }
            }
        }
    }

    // 查詢世界座標位置的剩餘泥土不透明度
    public float GetSoilOpacityAtWorldPoint(Vector3 worldPoint)
    {
        if (soilAlpha == null)
            return 1f;

        Vector3 localPoint =
            rectTransform.InverseTransformPoint(worldPoint);

        Rect rect = rectTransform.rect;

        float normalizedX =
            (localPoint.x - rect.xMin) / rect.width;

        float normalizedY =
            (localPoint.y - rect.yMin) / rect.height;

        if (normalizedX < 0f || normalizedX > 1f ||
            normalizedY < 0f || normalizedY > 1f)
        {
            return 1f;
        }

        int x = Mathf.Clamp(
            Mathf.RoundToInt(normalizedX * (textureWidth - 1)),
            0,
            textureWidth - 1
        );

        int y = Mathf.Clamp(
            Mathf.RoundToInt(normalizedY * (textureHeight - 1)),
            0,
            textureHeight - 1
        );

        return soilAlpha[y * textureWidth + x] / 255f;
    }

    // Sweep the actual brush footprint against cached nontransparent artifact texels.
    // Unlike a fixed sample count, even a long fast segment cannot jump over the artifact.
    public bool BrushPathTouchesArtifact(RectTransform artifact, Vector2 screenStart,
        Vector2 screenEnd, Camera eventCamera, float hitScale)
    {
        RevealMask mask = GetRevealMask(artifact);
        if (mask == null || mask.total == 0) return false;
        Vector2 localStart, localEnd;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenStart, eventCamera, out localStart) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenEnd, eventCamera, out localEnd))
            return false;
        Rect soilRect = rectTransform.rect;
        Vector2 from = new Vector2(
            (localStart.x - soilRect.xMin) / soilRect.width * (textureWidth - 1),
            (localStart.y - soilRect.yMin) / soilRect.height * (textureHeight - 1));
        Vector2 to = new Vector2(
            (localEnd.x - soilRect.xMin) / soilRect.width * (textureWidth - 1),
            (localEnd.y - soilRect.yMin) / soilRect.height * (textureHeight - 1));
        Vector2 delta = to - from;
        float lengthSquared = delta.sqrMagnitude;
        float radius = Mathf.Max(1, brushRadius);
        float minX = Mathf.Min(from.x, to.x) - radius, maxX = Mathf.Max(from.x, to.x) + radius;
        float minY = Mathf.Min(from.y, to.y) - radius, maxY = Mathf.Max(from.y, to.y) + radius;
        Rect originalRect = artifact.rect;
        float scale = Mathf.Clamp01(hitScale);
        Rect hitRect = new Rect(originalRect.center - originalRect.size * scale * 0.5f,
            originalRect.size * scale);
        Matrix4x4 toArtifact = artifact.worldToLocalMatrix * rectTransform.localToWorldMatrix;
        for (int i = 0; i < mask.indices.Length; i++)
        {
            int index = mask.indices[i];
            if (index < 0 || mask.weights[i] <= 0) continue;
            Vector2 point = new Vector2(index % textureWidth, index / textureWidth);
            if (point.x < minX || point.x > maxX || point.y < minY || point.y > maxY) continue;
            float t = lengthSquared > 0.0001f
                ? Mathf.Clamp01(Vector2.Dot(point - from, delta) / lengthSquared) : 0f;
            if ((point - (from + delta * t)).sqrMagnitude > radius * radius) continue;
            Vector3 artifactPoint = toArtifact.MultiplyPoint3x4(new Vector3(
                soilRect.xMin + point.x / (textureWidth - 1) * soilRect.width,
                soilRect.yMin + point.y / (textureHeight - 1) * soilRect.height, 0f));
            if (hitRect.Contains(new Vector2(artifactPoint.x, artifactPoint.y))) return true;
        }
        return false;
    }

    private void PrepareFastRemovalRegions()
    {
        int count = Mathf.Min(8, digManager.ArtifactCount); // This level always has three artifacts.
        bool rebuild = artifactCoverage == null || artifactCoverage.Length != pixels.Length ||
            efficiencyMasks == null || efficiencyMasks.Length != count;
        if (rebuild)
        {
            artifactCoverage = new byte[pixels.Length];
            efficiencyMasks = new RevealMask[count];
        }
        unfinishedArtifactBits = 0;
        for (int i = 0; i < count; i++)
        {
            RevealMask mask = GetRevealMask(digManager.GetArtifactTransform(i));
            if (!ReferenceEquals(efficiencyMasks[i], mask)) rebuild = true;
            efficiencyMasks[i] = mask;
            if (digManager.IsArtifactUnfinished(i)) unfinishedArtifactBits |= 1 << i;
        }
        if (!rebuild) return;
        Array.Clear(artifactCoverage, 0, artifactCoverage.Length);
        for (int i = 0; i < count; i++)
        {
            RevealMask mask = efficiencyMasks[i];
            if (mask == null) continue;
            for (int j = 0; j < mask.indices.Length; j++)
            {
                int index = mask.indices[j];
                if (index >= 0 && mask.weights[j] > 0)
                    artifactCoverage[index] |= (byte)(1 << i);
            }
        }
    }

    private RevealMask GetRevealMask(RectTransform artifact)
    {
        if (artifact == null || soilAlpha == null)
            return null;

        Image image = artifact.GetComponent<Image>();
        Sprite sprite = image != null
            ? (image.overrideSprite != null ? image.overrideSprite : image.sprite)
            : null;
        Rect drawingRect = image != null ? image.GetPixelAdjustedRect() : artifact.rect;
        bool preserve = image != null && image.preserveAspect;
        if (sprite != null && preserve && sprite.rect.height > 0f)
        {
            // Same pivot-based fitting used by a Simple UI Image.
            float aspect = sprite.rect.width / sprite.rect.height;
            Vector2 pivot = artifact.pivot;
            if (drawingRect.width / Mathf.Max(0.001f, drawingRect.height) > aspect)
            {
                float width = drawingRect.height * aspect;
                drawingRect.x += (drawingRect.width - width) * pivot.x;
                drawingRect.width = width;
            }
            else
            {
                float height = drawingRect.width / aspect;
                drawingRect.y += (drawingRect.height - height) * pivot.y;
                drawingRect.height = height;
            }
        }

        Matrix4x4 toSoil = rectTransform.worldToLocalMatrix * artifact.localToWorldMatrix;
        RevealMask mask;
        if (!revealMasks.TryGetValue(artifact, out mask) || mask.sprite != sprite ||
            mask.drawingRect != drawingRect || mask.soilRect != rectTransform.rect ||
            mask.toSoil != toSoil || mask.preserveAspect != preserve)
        {
            mask = BuildRevealMask(artifact, sprite, drawingRect, toSoil, preserve);
            revealMasks[artifact] = mask;
        }

        return mask;
    }

    // Count revealed silhouette area, not average transparency of a rectangle.
    public float GetRevealedRatio(RectTransform artifact)
    {
        RevealMask mask = GetRevealMask(artifact);
        if (mask == null) return 0f;

        if (mask.total == 0)
            return 0f;

        int revealed = 0;
        float cutoff = revealedSoilOpacity * 255f;
        for (int i = 0; i < mask.indices.Length; i++)
        {
            int index = mask.indices[i];
            if (index >= 0 && soilAlpha[index] <= cutoff)
                revealed += mask.weights[i];
        }
        return (float)revealed / mask.total;
    }

    public bool HasVisibleArtifactPixels(RectTransform artifact,
        float opacityThreshold = 0.5f, int minimumSamples = 4)
    {
        // Refresh only when sprite/layout changed; never read PNG pixels each frame.
        RevealMask mask = GetRevealMask(artifact);
        if (mask == null) return false;

        int visibleSamples = 0;
        float cutoff = Mathf.Clamp01(opacityThreshold) * 255f;
        int required = Mathf.Max(1, minimumSamples);
        for (int i = 0; i < mask.indices.Length; i++)
        {
            int index = mask.indices[i];
            // Count distinct soil texels, not thousands of source pixels in one texel.
            if (index >= 0 && mask.weights[i] > 0 && soilAlpha[index] <= cutoff &&
                ++visibleSamples >= required)
                return true;
        }
        return false;
    }

    private RevealMask BuildRevealMask(RectTransform artifact, Sprite sprite,
        Rect drawingRect, Matrix4x4 toSoil, bool preserve)
    {
        RevealMask mask = new RevealMask
        {
            sprite = sprite, drawingRect = drawingRect, toSoil = toSoil,
            soilRect = rectTransform.rect, preserveAspect = preserve
        };
        Color32[] source = null;
        int sourceWidth = 0;
        int sourceHeight = 0;
        Vector2[] vertices = null;
        Vector2[] uvs = null;
        ushort[] triangles = null;
        if (sprite != null)
        {
            try
            {
                // Mesh UVs handle subrects, trimming and rotated/tight atlas packing.
                vertices = sprite.vertices;
                uvs = sprite.uv;
                triangles = sprite.triangles;
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector2(
                        (vertices[i].x * sprite.pixelsPerUnit + sprite.pivot.x) / sprite.rect.width,
                        (vertices[i].y * sprite.pixelsPerUnit + sprite.pivot.y) / sprite.rect.height);
                if (sprite.texture.isReadable)
                {
                    sourceWidth = sprite.texture.width;
                    sourceHeight = sprite.texture.height;
                    source = sprite.texture.GetPixels32();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(artifact.name + ": alpha mask unavailable; using sprite geometry. " + exception.Message, artifact);
            }
        }
        if (source == null)
            Debug.LogWarning(artifact.name + ": using conservative sprite-mesh reveal fallback (no readable alpha).", artifact);

        // Exact source-pixel area for the project's ordinary, unpacked PNG sprites.
        // Aggregate pixels into soil texels once; later queries only scan those bins.
        if (source != null && sprite != null && !sprite.packed)
        {
            Rect spriteRect = sprite.rect;
            Rect soilRectExact = rectTransform.rect;
            Dictionary<int, int> exactCounts = new Dictionary<int, int>();
            int minX = Mathf.Max(0, Mathf.CeilToInt(spriteRect.xMin - 0.5f));
            int maxX = Mathf.Min(sourceWidth, Mathf.CeilToInt(spriteRect.xMax - 0.5f));
            int minY = Mathf.Max(0, Mathf.CeilToInt(spriteRect.yMin - 0.5f));
            int maxY = Mathf.Min(sourceHeight, Mathf.CeilToInt(spriteRect.yMax - 0.5f));
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    if (source[y * sourceWidth + x].a == 0) continue;
                    float sourceU = (x + 0.5f - spriteRect.xMin) / spriteRect.width;
                    float sourceV = (y + 0.5f - spriteRect.yMin) / spriteRect.height;
                    Vector3 local = toSoil.MultiplyPoint3x4(new Vector3(
                        drawingRect.xMin + sourceU * drawingRect.width,
                        drawingRect.yMin + sourceV * drawingRect.height, 0f));
                    float u = (local.x - soilRectExact.xMin) / soilRectExact.width;
                    float v = (local.y - soilRectExact.yMin) / soilRectExact.height;
                    int index = -1;
                    if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                    {
                        int px = Mathf.Clamp(Mathf.RoundToInt(u * (textureWidth - 1)), 0, textureWidth - 1);
                        int py = Mathf.Clamp(Mathf.RoundToInt(v * (textureHeight - 1)), 0, textureHeight - 1);
                        index = py * textureWidth + px;
                    }
                    int count;
                    exactCounts.TryGetValue(index, out count);
                    exactCounts[index] = count + 1;
                    mask.total++;
                }
            }
            StoreMaskBins(mask, exactCounts);
            return mask;
        }

        // Roughly two samples per soil texel, bounded to avoid runtime allocations per drag.
        Vector3 xAxis = toSoil.MultiplyVector(new Vector3(drawingRect.width, 0f, 0f));
        Vector3 yAxis = toSoil.MultiplyVector(new Vector3(0f, drawingRect.height, 0f));
        Rect soilRect = rectTransform.rect;
        int nx = Mathf.Clamp(Mathf.CeilToInt(xAxis.magnitude / Mathf.Max(0.001f, soilRect.width) * textureWidth * 2f), 32, 256);
        int ny = Mathf.Clamp(Mathf.CeilToInt(yAxis.magnitude / Mathf.Max(0.001f, soilRect.height) * textureHeight * 2f), 32, 256);
        Dictionary<int, int> counts = new Dictionary<int, int>();
        for (int y = 0; y < ny; y++)
        {
            for (int x = 0; x < nx; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / nx, (y + 0.5f) / ny);
                Vector2 uv = point;
                bool inside = vertices == null || triangles == null || triangles.Length == 0;
                if (!inside)
                {
                    for (int t = 0; t + 2 < triangles.Length; t += 3)
                    {
                        int ia = triangles[t], ib = triangles[t + 1], ic = triangles[t + 2];
                        Vector2 a = vertices[ia], ab = vertices[ib] - a, ac = vertices[ic] - a, ap = point - a;
                        float det = ab.x * ac.y - ab.y * ac.x;
                        if (Mathf.Abs(det) < 0.0000001f) continue;
                        float v = (ap.x * ac.y - ap.y * ac.x) / det;
                        float w = (ab.x * ap.y - ab.y * ap.x) / det;
                        if (v < -0.00001f || w < -0.00001f || v + w > 1.00001f) continue;
                        uv = uvs[ia] * (1f - v - w) + uvs[ib] * v + uvs[ic] * w;
                        inside = true;
                        break;
                    }
                }
                if (!inside) continue;
                if (source != null)
                {
                    int sx = Mathf.Clamp(Mathf.FloorToInt(uv.x * sourceWidth), 0, sourceWidth - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(uv.y * sourceHeight), 0, sourceHeight - 1);
                    if (source[sy * sourceWidth + sx].a == 0) continue;
                }
                Vector3 local = toSoil.MultiplyPoint3x4(new Vector3(
                    drawingRect.xMin + point.x * drawingRect.width,
                    drawingRect.yMin + point.y * drawingRect.height, 0f));
                float u = (local.x - soilRect.xMin) / soilRect.width;
                float vSoil = (local.y - soilRect.yMin) / soilRect.height;
                int index = -1; // Outside soil stays covered, never disappears from the denominator.
                if (u >= 0f && u <= 1f && vSoil >= 0f && vSoil <= 1f)
                {
                    int px = Mathf.Clamp(Mathf.RoundToInt(u * (textureWidth - 1)), 0, textureWidth - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(vSoil * (textureHeight - 1)), 0, textureHeight - 1);
                    index = py * textureWidth + px;
                }
                int count;
                counts.TryGetValue(index, out count);
                counts[index] = count + 1;
                mask.total++;
            }
        }
        StoreMaskBins(mask, counts);
        return mask;
    }

    private static void StoreMaskBins(RevealMask mask, Dictionary<int, int> counts)
    {
        mask.indices = new int[counts.Count];
        mask.weights = new int[counts.Count];
        int entry = 0;
        foreach (KeyValuePair<int, int> pair in counts)
        {
            mask.indices[entry] = pair.Key;
            mask.weights[entry++] = pair.Value;
        }
    }

    void OnDisable()
    {
        FlushSoil(false);
        pointerActive = false;
        ResetMotion();

        OnBrushReleased?.Invoke();
    }

    void OnDestroy()
    {
        revealMasks.Clear();
        if (runtimeSoilSprite != null)
            Destroy(runtimeSoilSprite);
        if (soilTexture != null)
            Destroy(soilTexture);
    }
}