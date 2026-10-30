using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DigManager : MonoBehaviour
{
    [Header("刷土系統")]
    [SerializeField] private SoilBrush soilBrush;

    [Header("三件考古文物")]
    [SerializeField] private RectTransform[] artifacts;

    [Header("遊戲介面")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text timerText;

    // 選填：之後可指定紅色警告文字
    [SerializeField] private TMP_Text warningText;

    [Header("首次看見文物")]
    [SerializeField] private TMP_Text discoveryText;
    [SerializeField] private float firstSightProtectionDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float firstVisibleOpacity = 0.5f;
    [SerializeField, Min(1)] private int firstVisibleSamples = 4;
    [SerializeField] private float discoveryMessageDuration = 1.5f;

    public event System.Action<int> OnArtifactFirstSeen;

    private bool[] firstSeen;
    private float[] protectedUntil;
    private float[] artifactDanger;
    private float discoveryMessageUntil = -999f;

    [Header("發掘設定")]
    [SerializeField, Range(0f, 1f)]
    private float revealThreshold = 0.7f;

    [Header("時間設定")]
    [SerializeField] private float gameDuration = 60f;

    [Header("損傷設定")]
    [SerializeField] private int maxDamage = 3;
    [SerializeField] private float damageCooldown = 2f;
    [SerializeField] private float damageFlashDuration = 0.3f;

    [Header("速度門檻")]
    [SerializeField] private float highSpeedThreshold = 1100f;
    [SerializeField] private float lowSpeedThreshold = 850f;
    [SerializeField] private float safeInstantSpeed = 600f;

    [Header("危險值設定")]
    [SerializeField] private float damageThreshold = 0.12f;
    [SerializeField] private float dangerDecay = 0.5f;
    [SerializeField, Min(0.01f)] private float brushMotionTimeout = 0.05f;
    [SerializeField] private float warningThreshold = 0.05f;

    [Header("文物判定")]
    [SerializeField, Range(0.1f, 1f)]
    private float artifactHitScale = 0.75f;

    private float remainingTime;
    private float lastDamageTime = -999f;

    private float dangerValue = 0f;
    private float lastMotionTime = -999f;

    private float lastInstantBrushSpeed;
    private bool brushingFast;
    private bool fastMotionOnArtifact;

    private int dangerArtifactIndex = -1;

    private bool[] discovered;
    private Color[] initialArtifactColors;

    private int foundCount;
    private int damageCount;

    private bool gameEnded;
    private bool gameplayActive = true;
    private bool initialized;
    private float suspendedAt;
    public bool IsInitialized => initialized;
    public bool IsRunning => initialized && gameplayActive && !gameEnded;
    public bool HasEnded => gameEnded;
    // The instantaneous gate removes the efficiency penalty on the very next normal stroke.
    public bool IsFastBrushStroke => IsRunning && brushingFast &&
        lastInstantBrushSpeed >= highSpeedThreshold;
    public int ArtifactCount => artifacts != null ? artifacts.Length : 0;

    public RectTransform GetArtifactTransform(int index)
    {
        return artifacts != null && index >= 0 && index < artifacts.Length ? artifacts[index] : null;
    }

    public bool IsArtifactUnfinished(int index)
    {
        return discovered != null && index >= 0 && index < discovered.Length &&
            artifacts[index] != null && !discovered[index];
    }

    public TMP_Text TimerLabel => timerText;
    public TMP_Text ProgressLabel => progressText;
    public TMP_Text DiscoveryLabel => discoveryText;
    public event System.Action<DigLevelResult> ChallengeEnded;
    public event System.Action<int> ArtifactDamaged;
    public event System.Action<int> ArtifactExcavated;
    public DigLevelResult LastResult { get; private set; }

    public Sprite GetArtifactSprite(int index)
    {
        if (artifacts == null || index < 0 || index >= artifacts.Length || artifacts[index] == null)
            return null;
        Image image = artifacts[index].GetComponent<Image>();
        return image != null ? (image.overrideSprite != null ? image.overrideSprite : image.sprite) : null;
    }

    public void SetWarningLabel(TMP_Text label)
    {
        warningText = label;
        if (label != null) label.raycastTarget = false;
    }

    // Local pause; never changes the application's timeScale.
    public void SetGameplayActive(bool active)
    {
        if (gameEnded || gameplayActive == active) return;
        if (!active) suspendedAt = Time.unscaledTime;
        else if (initialized)
        {
            float elapsed = Mathf.Max(0f, Time.unscaledTime - suspendedAt);
            if (lastDamageTime > -900f) lastDamageTime += elapsed;
            if (discoveryMessageUntil > suspendedAt) discoveryMessageUntil += elapsed;
            for (int i = 0; i < protectedUntil.Length; i++)
                if (firstSeen[i] && protectedUntil[i] > suspendedAt)
                    protectedUntil[i] += elapsed;
        }
        gameplayActive = active;
        lastMotionTime = -999f;
        HandleBrushReleased();
        if (initialized)
        {
            UpdateWarningUI();
            UpdateDiscoveryUI();
        }
    }

    private DigLevelResult CaptureResult(bool success, string reason)
    {
        DigArtifactResult[] items = new DigArtifactResult[artifacts.Length];
        for (int i = 0; i < items.Length; i++)
            items[i] = new DigArtifactResult
            {
                sceneObject = artifacts[i] != null ? artifacts[i].name : "",
                found = discovered[i], firstSeen = firstSeen[i]
            };
        return new DigLevelResult
        {
            success = success, remainingSeconds = remainingTime, damageCount = damageCount,
            foundCount = foundCount, failureReason = reason, artifacts = items
        };
    }

    void Start()
    {
        if (artifacts == null || artifacts.Length != 3)
        {
            Debug.LogError("DigManager 必須指定三件文物！");
            enabled = false;
            return;
        }

        if (soilBrush == null)
        {
            Debug.LogError("DigManager 尚未指定 SoilBrush！");
            enabled = false;
            return;
        }

        initialArtifactColors=new Color[artifacts.Length];
        for(int i=0;i<artifacts.Length;i++)
        {
            Image image=artifacts[i]!=null ? artifacts[i].GetComponent<Image>() : null;
            initialArtifactColors[i]=image!=null ? image.color : Color.white;
        }
        discovered = new bool[artifacts.Length];
        firstSeen = new bool[artifacts.Length];
        protectedUntil = new float[artifacts.Length];
        artifactDanger = new float[artifacts.Length];
        if (discoveryText != null)
        {
            discoveryText.raycastTarget = false;
            discoveryText.gameObject.SetActive(false);
        }

        foundCount = 0;
        damageCount = 0;

        dangerValue = 0f;
        lastMotionTime = -999f;
        lastDamageTime = -999f;

        brushingFast = false;
        fastMotionOnArtifact = false;

        remainingTime = gameDuration;
        gameEnded = false;

        UpdateUI();
        UpdateTimerUI();
        UpdateWarningUI();

        soilBrush.OnSoilChanged += CheckArtifacts;
        soilBrush.OnBrushMotion += HandleBrushMotion;
        soilBrush.OnBrushReleased += HandleBrushReleased;

        initialized = true;
        Debug.Log("考古小隊初始化完成，等待開始挑戰。");
    }

    public bool ResetForNewChallenge()
    {
        if(!initialized) return false;
        StopAllCoroutines();
        gameplayActive=false; gameEnded=false; LastResult=null;
        System.Array.Clear(discovered,0,discovered.Length);
        System.Array.Clear(firstSeen,0,firstSeen.Length);
        System.Array.Clear(protectedUntil,0,protectedUntil.Length);
        System.Array.Clear(artifactDanger,0,artifactDanger.Length);
        for(int i=0;i<artifacts.Length;i++)
        {
            Image image=artifacts[i]!=null ? artifacts[i].GetComponent<Image>() : null;
            if(image!=null) image.color=initialArtifactColors[i];
        }
        foundCount=damageCount=0; remainingTime=gameDuration;
        dangerValue=0; dangerArtifactIndex=-1;
        lastDamageTime=lastMotionTime=discoveryMessageUntil=-999f;
        suspendedAt=Time.unscaledTime;
        HandleBrushReleased();
        UpdateUI(); UpdateTimerUI(); UpdateWarningUI(); UpdateDiscoveryUI();
        return true;
    }

    void Update()
    {
        if (gameEnded || !gameplayActive)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimerUI();

            FailGame("時間已到！");
            return;
        }

        UpdateTimerUI();

        // 沒有持續收到刷動事件，就不再視為快刷
        if (Time.unscaledTime - lastMotionTime > brushMotionTimeout)
        {
            brushingFast = false;
            fastMotionOnArtifact = false;
        }

        bool coolingDown =
            Time.unscaledTime - lastDamageTime < damageCooldown;

        // Keep danger separate, so one artifact never inherits another's charge.
        dangerValue = 0f;
        for (int i = 0; i < artifactDanger.Length; i++)
        {
            if (!CanDamageArtifact(i))
            {
                artifactDanger[i] = 0f;
                continue;
            }
            if (!coolingDown && brushingFast && fastMotionOnArtifact &&
                dangerArtifactIndex == i)
            {
                // Exclude the protected portion of a frame that straddles expiry.
                float dt = Mathf.Min(Time.unscaledDeltaTime,
                    Mathf.Max(0f, Time.unscaledTime - protectedUntil[i]));
                artifactDanger[i] += dt;
            }
            else
            {
                artifactDanger[i] -= dangerDecay * Time.unscaledDeltaTime;
            }
            artifactDanger[i] = Mathf.Clamp(artifactDanger[i], 0f, damageThreshold);
            dangerValue = Mathf.Max(dangerValue, artifactDanger[i]);
        }

        UpdateWarningUI();
        UpdateDiscoveryUI();
        if (!coolingDown && CanDamageArtifact(dangerArtifactIndex) &&
            artifactDanger[dangerArtifactIndex] >= damageThreshold)
            CauseDamage();
    }

    private bool CanDamageArtifact(int index)
    {
        return !gameEnded && firstSeen != null && index >= 0 &&
            index < artifacts.Length && artifacts[index] != null &&
            firstSeen[index] && !discovered[index] &&
            Time.unscaledTime >= protectedUntil[index];
    }

    private void HandleBrushReleased()
    {
        lastInstantBrushSpeed = 0f;
        brushingFast = false;
        fastMotionOnArtifact = false;
    }

    private void UpdateDiscoveryUI()
    {
        if (discoveryText != null)
            DigHintFade.SetVisible(discoveryText,
                gameplayActive && !gameEnded && Time.unscaledTime < discoveryMessageUntil);
    }

    private void FirstSight(int index)
    {
        if (firstSeen[index])
            return;

        firstSeen[index] = true;
        protectedUntil[index] = Time.unscaledTime + firstSightProtectionDuration;
        artifactDanger[index] = 0f;
        if (dangerArtifactIndex == index)
        {
            dangerValue = 0f;
            fastMotionOnArtifact = false;
        }
        discoveryMessageUntil = Time.unscaledTime + discoveryMessageDuration;
        if (discoveryText != null)
            discoveryText.text = "發現文物！請小心清理";
        UpdateDiscoveryUI();
        Debug.Log(artifacts[index].name + " 首次看見，開始保護期。");
        OnArtifactFirstSeen?.Invoke(index);
    }

    // 接收 SoilBrush 每次回報的刷動狀態
    private void HandleBrushMotion(
        Vector2 startPoint,
        Vector2 endPoint,
        Camera eventCamera,
        float averageSpeed,
        float instantSpeed)
    {
        if (gameEnded || !gameplayActive || discovered == null)
            return;

        lastMotionTime = Time.unscaledTime;
        lastInstantBrushSpeed = instantSpeed;

        // 速度非常慢時，直接視為安全
        if (instantSpeed < safeInstantSpeed)
        {
            brushingFast = false;
            fastMotionOnArtifact = false;
            return;
        }

        // 高速啟動、低速退出，避免在門檻附近反覆切換
        if (!brushingFast)
        {
            brushingFast =
                averageSpeed >= highSpeedThreshold;
        }
        else
        {
            brushingFast =
                averageSpeed >= lowSpeedThreshold;
        }

        // Damage follows the average-speed hysteresis above. Requiring the
        // instantaneous high threshold again would cancel its low-speed exit band.
        // The safe instantaneous gate still stops danger immediately on slow strokes.
        if (!brushingFast)
        {
            fastMotionOnArtifact = false;
            return;
        }

        dangerArtifactIndex = FindTouchedArtifact(
            startPoint,
            endPoint,
            eventCamera
        );

        fastMotionOnArtifact =
            dangerArtifactIndex >= 0;
    }

    // 判斷本次刷動是否經過尚未找到的文物
    private int FindTouchedArtifact(
        Vector2 startPoint,
        Vector2 endPoint,
        Camera eventCamera)
    {
        for (int i = 0; i < artifacts.Length; i++)
        {
            if (CanDamageArtifact(i) && soilBrush.BrushPathTouchesArtifact(
                artifacts[i], startPoint, endPoint, eventCamera, artifactHitScale))
                return i;
        }
        return -1;
    }

    // 實際造成一次損傷
    private void CauseDamage()
    {
        if (gameEnded)
            return;

        if (!CanDamageArtifact(dangerArtifactIndex))
        {
            dangerValue = 0f;
            return;
        }

        artifactDanger[dangerArtifactIndex] = 0f;
        damageCount++;
        ArtifactDamaged?.Invoke(dangerArtifactIndex);

        dangerValue = 0f;
        brushingFast = false;
        fastMotionOnArtifact = false;

        lastDamageTime = Time.unscaledTime;

        RectTransform damagedArtifact =
            artifacts[dangerArtifactIndex];

        Debug.LogWarning(
            damagedArtifact.name +
            " 刷動過快！損傷 " +
            damageCount + " / " + maxDamage
        );

        StartCoroutine(
            FlashArtifact(damagedArtifact)
        );

        UpdateUI();
        UpdateWarningUI();

        if (damageCount >= maxDamage)
        {
            FailGame("文物損傷達到上限！");
        }
    }

    // 文物短暫閃紅
    private IEnumerator FlashArtifact(RectTransform artifact)
    {
        if (artifact == null)
            yield break;

        Image image = artifact.GetComponent<Image>();

        if (image == null)
            yield break;

        Color originalColor = image.color;

        image.color = new Color32(
            255, 90, 90, 255
        );

        yield return new WaitForSecondsRealtime(
            damageFlashDuration
        );

        if (image != null)
            image.color = originalColor;
    }

    // 檢查文物露出比例
    private void CheckArtifacts()
    {
        if (gameEnded || !gameplayActive || discovered == null)
            return;

        for (int i = 0; i < artifacts.Length; i++)
        {
            if (discovered[i] || artifacts[i] == null)
                continue;

            if (!firstSeen[i] && soilBrush.HasVisibleArtifactPixels(
                artifacts[i], firstVisibleOpacity, firstVisibleSamples))
                FirstSight(i);

            float revealed =
                soilBrush.GetRevealedRatio(artifacts[i]);

            if (revealed >= revealThreshold)
            {
                discovered[i] = true;
                artifactDanger[i] = 0f;
                if (dangerArtifactIndex == i) fastMotionOnArtifact = false;
                foundCount++;

                Debug.Log(
                    artifacts[i].name +
                    " 發掘成功！找到：" +
                    foundCount + " / 3"
                );

                UpdateUI();
                ArtifactExcavated?.Invoke(i);
            }
        }

        if (foundCount == artifacts.Length)
        {
            CompleteGame();
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text =
                "找到 " + foundCount + " / 3" +
                "　損壞 " + damageCount +
                " / " + maxDamage;
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingTime);

        timerText.text =
            "剩餘時間：" + seconds + " 秒";

        if (remainingTime <= 10f)
        {
            timerText.color =
                new Color32(255, 90, 90, 255);
        }
        else
        {
            timerText.color = new Color32(32, 61, 50, 255);
        }
    }

    // 警告文字是選填，未指定不影響損傷判定
    private void UpdateWarningUI()
    {
        if (warningText == null)
            return;

        bool showWarning =
            gameplayActive && !gameEnded &&
            dangerValue >= warningThreshold;

        DigHintFade.SetVisible(warningText, showWarning);

        if (showWarning)
        {
            warningText.text = "請放慢刷動速度！";
            warningText.color =
                new Color32(214, 69, 69, 255);
        }
    }

    private void CompleteGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        StopBrushing();
        UpdateWarningUI();
        UpdateDiscoveryUI();

        LastResult = CaptureResult(true, "");
        ChallengeEnded?.Invoke(LastResult);

        Debug.Log(
            "挑戰成功！三件文物全部找到！" +
            "剩餘時間：" +
            Mathf.CeilToInt(remainingTime) + " 秒"
        );
    }

    private void FailGame(string reason)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        StopBrushing();
        UpdateWarningUI();
        UpdateDiscoveryUI();

        LastResult = CaptureResult(false, reason);
        ChallengeEnded?.Invoke(LastResult);
        Debug.Log("挑戰失敗：" + reason);
    }

    private void StopBrushing()
    {
        if (soilBrush == null)
            return;

        soilBrush.enabled = false;

        Image soilImage = soilBrush.GetComponent<Image>();

        if (soilImage != null)
        {
            soilImage.raycastTarget = false;
        }
    }

    void OnDestroy()
    {
        if (soilBrush != null)
        {
            soilBrush.OnSoilChanged -= CheckArtifacts;
            soilBrush.OnBrushMotion -= HandleBrushMotion;
            soilBrush.OnBrushReleased -= HandleBrushReleased;
        }
    }
}