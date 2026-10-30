using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace XunJiYiDa.Levels.Shared
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SharedLevelController : MonoBehaviour
    {
        [SerializeField] private SharedLevelDefinition definition;
        [SerializeField] private SharedLevelUI ui;
        [SerializeField] private SharedAudioPlayer audioPlayer;
        [SerializeField] private bool freezeWorldWhenInactive = true;
        [SerializeField] private UnityEvent onRetried = new UnityEvent();
        private LevelSession session;
        private bool ownsTimeScale;
        private float savedTimeScale;
        private bool hasFocus = true;
        private bool applicationPaused;
        private int lastTickSecond = -1;

        public SharedLevelDefinition Definition => definition;
        public LevelSession Session => session ?? (session = definition != null
            ? definition.CreateSession() : new LevelSession("SharedFoundation", 120));
        public bool CanInteract => isActiveAndEnabled && Session.CanInteract;
        public Action RetryOverride { get; set; }
        public void InitializeSession(LevelSession value)
        {
            if (session != null) throw new InvalidOperationException("Initialize before the controller awakens.");
            session = value ?? throw new ArgumentNullException(nameof(value));
        }
        public event Action Retried;
        public event Action<LevelResult> Completed;

        private void Awake() { _ = Session; }

        private void OnEnable()
        {
            Session.StateChanged += HandleState;
            Session.Completed += HandleCompleted;
            Session.Retried += HandleRetried;
            HandleState(Session.State);
        }

        private void Start() => PrepareAttempt();

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
            Session.Tick(Time.unscaledDeltaTime);
            int second = Mathf.CeilToInt(Session.RemainingTime);
            if (Session.CanInteract && second != lastTickSecond)
            {
                if (second > 0 && second <= 10) audioPlayer?.Play(SharedSound.Tick);
                lastTickSecond = second;
            }
        }

        public void Begin() => Session.Begin();
        public void Succeed() => Session.Succeed();
        public void Fail() => Session.Fail();
        public void CollectArtifact(string artifactId) => Session.CollectArtifact(artifactId);
        public void RecordMistake()
        {
            if (Session.RecordMistake()) audioPlayer?.Play(SharedSound.Wrong);
        }
        public void Pause() => Session.SetPaused(PauseReason.Manual, true);
        public void Resume() => Session.SetPaused(PauseReason.Manual, false);
        public void TogglePause()
        {
            if (Session.State == LevelState.Ready || Session.Result != null) return;
            Session.SetPaused(PauseReason.Manual, (Session.PauseReasons & PauseReason.Manual) == 0);
        }

        public void Retry()
        {
            if (RetryOverride != null) RetryOverride();
            else RetryLocally();
        }

        public void RetryLocally() => Session.Retry();

        private void HandleRetried()
        {
            ui?.CloseContent();
            lastTickSecond = -1;
            Retried?.Invoke();
            onRetried.Invoke();
            PrepareAttempt();
        }

        private void PrepareAttempt()
        {
            Session.SetPaused(PauseReason.Focus, !hasFocus || applicationPaused);
            if (definition != null && definition.ShowIntroduction && ui != null)
                ui.ShowIntroduction(definition.IntroductionTitle, definition.IntroductionBody,
                    definition.IntroductionImage, Begin);
            else Begin();
        }

        private void HandleState(LevelState state)
        {
            bool frozen = state != LevelState.Running;
            if (freezeWorldWhenInactive && frozen && !ownsTimeScale)
            {
                savedTimeScale = Time.timeScale;
                ownsTimeScale = true;
                Time.timeScale = 0;
            }
            else if (!frozen) ReleaseTimeScale();
            audioPlayer?.SetGameplayPaused(frozen);
        }

        private void HandleCompleted(LevelResult result)
        {
            audioPlayer?.Play(result.Success ? SharedSound.Victory : SharedSound.Wrong);
            Completed?.Invoke(result);
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            Session.SetPaused(PauseReason.Focus, !hasFocus || applicationPaused);
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            Session.SetPaused(PauseReason.Focus, !hasFocus || applicationPaused);
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= HandleState;
                session.Completed -= HandleCompleted;
                session.Retried -= HandleRetried;
            }
            ReleaseTimeScale();
            audioPlayer?.SetGameplayPaused(false);
        }

        private void ReleaseTimeScale()
        {
            if (!ownsTimeScale) return;
            Time.timeScale = savedTimeScale;
            ownsTimeScale = false;
        }
    }
}
