using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;
using Object = UnityEngine.Object;

namespace XunJiYiDa.Levels.Timeline.Tests
{
#if UNITY_EDITOR
    public sealed class TimelineGameplayTests
    {
        private const string Path = "Assets/Levels/Timeline/L_Timeline.unity";
        private readonly int[] years = { 1926, 1946, 1988, 1998, 2003 };
        private Scene scene;
        private Scene previous;
        private SharedLevelController controller;
        private LevelResultRelay relay;
        private float previousScale;

        [UnitySetUp]
        public IEnumerator LoadDeliveredScene()
        {
            Assert.That(File.Exists(Path), Is.True, "Generate the real scene before testing its gameplay.");
            previous = SceneManager.GetActiveScene();
            previousScale = Time.timeScale;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Path,
                new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(Path);
            SceneManager.SetActiveScene(scene);
            yield return null;
            controller = Find<SharedLevelController>();
            relay = Find<LevelResultRelay>();
        }

        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousScale;
        }

        [UnityTest]
        public IEnumerator IntroductionKnowledgeAndPauseGateBothTimerAndDragging()
        {
            Assert.That(Panel("Introduction").gameObject.activeInHierarchy, Is.True);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(45));
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(45));
            var home = Card(1926).position;
            Drag(1926, 1926);
            Assert.That(Card(1926).position, Is.EqualTo(home));
            Capture("introduction");
            Begin();
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(controller.Session.RemainingTime, Is.LessThan(45));
            Capture("gameplay");
            Button("Knowledge").onClick.Invoke();
            Assert.That(Panel("KnowledgeCard").gameObject.activeInHierarchy, Is.True);
            float frozen = controller.Session.RemainingTime;
            Drag(1926, 1926);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(frozen));
            Assert.That(Card(1926).position, Is.EqualTo(home));
            Assert.That(controller.Session.MistakeCount, Is.Zero);
            Capture("knowledge");
            controller.Pause();
            Panel("KnowledgeCard").PrimaryButton.onClick.Invoke();
            Assert.That(controller.CanInteract, Is.False, "Closing knowledge must preserve a manual pause.");
            Capture("pause");
            Button("Resume").onClick.Invoke();
            Assert.That(controller.CanInteract, Is.True);
            Button("SpaceIntroduction").onClick.Invoke();
            Assert.That(controller.CanInteract, Is.False);
            Panel("Introduction").PrimaryButton.onClick.Invoke();
            Assert.That(controller.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator WrongSlotAndOutsideDropBounceBackAndCountExactlyOnce()
        {
            Begin();
            var home = Card(1926).position;
            Drag(1926, 1946);
            Assert.That(controller.Session.MistakeCount, Is.EqualTo(1));
            Assert.That(Card(1926).GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector3.Distance(Card(1926).position, home), Is.LessThan(0.1f));
            Drag(1926, null);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(controller.Session.MistakeCount, Is.EqualTo(2));
            Assert.That(Vector3.Distance(Card(1926).position, home), Is.LessThan(0.1f));
            Assert.That(relay.LastResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator CorrectDropsSnapLockAndPublishSingleSharedSuccess()
        {
            Begin();
            int completed = 0;
            controller.Completed += _ => completed++;
            Drag(1926, 1926);
            Assert.That(Vector3.Distance(Card(1926).position, Slot(1926).position), Is.LessThan(0.1f));
            var snapped = Card(1926).position;
            Drag(1926, 1946);
            Assert.That(Card(1926).position, Is.EqualTo(snapped), "A correct card stays fixed.");
            Assert.That(controller.Session.MistakeCount, Is.Zero);
            foreach (int year in years.Skip(1)) Drag(year, year);
            Assert.That(controller.Session.State, Is.EqualTo(LevelState.Succeeded));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(relay.LastResult.LevelId, Is.EqualTo("Timeline"));
            Assert.That(relay.LastResult.Success, Is.True);
            Assert.That(relay.LastResult.MistakeCount, Is.Zero);
            Assert.That(relay.LastResult.ArtifactIds, Is.Empty);
            Assert.That(relay.LastResultJson, Does.Contain("\"levelId\":\"Timeline\""));
            Assert.That(relay.LastResultJson, Does.Contain("\"success\":true"));
            Assert.That(GameObject("ResultOverlay").activeInHierarchy, Is.True);
            yield return null;
            Capture("success");
            Drag(1926, null);
            controller.Session.Tick(90);
            Assert.That(completed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TimeoutCancelsActiveDragAndRetryResetsTheWholeAttempt()
        {
            Begin();
            Drag(1926, 1946);
            Drag(1946, 1946);
            var card = Card(1988);
            var home = card.position;
            var pointer = Pointer(card);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = ScreenPoint(Slot(1988));
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.dragHandler);
            int completed = 0;
            controller.Completed += _ => completed++;
            controller.Session.Tick(45);
            Assert.That(controller.Session.State, Is.EqualTo(LevelState.Failed));
            Assert.That(relay.LastResult.Success, Is.False);
            Assert.That(relay.LastResult.RemainingTime, Is.Zero);
            Assert.That(relay.LastResult.MistakeCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(card.position, home), Is.LessThan(0.1f));
            Assert.That(card.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            ExecuteEvents.Execute(Slot(1988).gameObject, pointer, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.endDragHandler);
            Assert.That(completed, Is.EqualTo(1));
            Capture("timeout");
            GameObject("ResultOverlay").GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(45));
            Assert.That(controller.Session.MistakeCount, Is.Zero);
            Assert.That(relay.LastResult, Is.Null);
            Assert.That(relay.LastResultJson, Is.Empty);
            Assert.That(Panel("Introduction").gameObject.activeInHierarchy, Is.True);
            foreach (int year in years) Assert.That(Card(year).localPosition.y, Is.LessThan(-250));
            Begin();
            foreach (int year in years) Drag(year, year);
            Assert.That(controller.Session.State, Is.EqualTo(LevelState.Succeeded));
            Assert.That(relay.LastResult.MistakeCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseDuringDragReturnsCardWithoutMistakeAndPauseRetryWorks()
        {
            Begin();
            var card = Card(1926);
            var home = card.position;
            var pointer = Pointer(card);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = ScreenPoint(Slot(1926));
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.dragHandler);
            controller.Pause();
            Assert.That(Vector3.Distance(card.position, home), Is.LessThan(0.1f));
            ExecuteEvents.Execute(Slot(1926).gameObject, pointer, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.endDragHandler);
            Assert.That(controller.Session.MistakeCount, Is.Zero);
            float remaining = controller.Session.RemainingTime;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(remaining));
            GameObject("PauseOverlay").GetComponentsInChildren<Button>().Single(b => b.name == "Retry").onClick.Invoke();
            Assert.That(Panel("Introduction").gameObject.activeInHierarchy, Is.True);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(45));
        }

        [UnityTest]
        public IEnumerator RetryShufflesFiveUniqueCardsInsteadOfKeepingThePreviousOrder()
        {
            var initial = years.OrderBy(y => Card(y).position.x).ToArray();
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Begin();
                controller.Pause();
                GameObject("PauseOverlay").GetComponentsInChildren<Button>().Single(b => b.name == "Retry").onClick.Invoke();
                var current = years.OrderBy(y => Card(y).position.x).ToArray();
                Assert.That(current, Is.EquivalentTo(years));
                Assert.That(current, Is.Not.EqualTo(initial), "Each retry must reshuffle the tray.");
                Assert.That(current, Is.Not.EqualTo(years), "Do not start with the solved chronological order.");
                initial = current;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OtherMouseButtonCannotEndAnActiveLeftDragOrCountAMistake()
        {
            Begin();
            var card = Card(1926);
            var left = Pointer(card);
            ExecuteEvents.Execute(card.gameObject, left, ExecuteEvents.beginDragHandler);
            var right = Pointer(card);
            right.button = PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(card.gameObject, right, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(card.gameObject, right, ExecuteEvents.endDragHandler);
            Assert.That(controller.Session.MistakeCount, Is.Zero, "An ignored right-button gesture cannot count a mistake.");
            Assert.That(card.GetComponent<CanvasGroup>().alpha, Is.LessThan(1), "The left drag must remain active.");
            left.position = ScreenPoint(Slot(1926));
            ExecuteEvents.Execute(card.gameObject, left, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(Slot(1926).gameObject, left, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(card.gameObject, left, ExecuteEvents.endDragHandler);
            Assert.That(Vector3.Distance(card.position, Slot(1926).position), Is.LessThan(0.1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DragThresholdMovementKeepsTheOriginalGrabOffset()
        {
            Begin();
            Canvas.ForceUpdateCanvases();
            var card = Card(1926);
            var pointer = Pointer(card);
            // Real EventSystem begins a drag after the pointer already crossed its threshold.
            pointer.position = ScreenPoint(Slot(1926));
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.dragHandler);
            Assert.That(Vector3.Distance(card.position, Slot(1926).position), Is.LessThan(0.1f),
                "The first movement must follow the pointer, rather than becoming a permanent offset.");
            controller.Pause();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InputSystemMouseCanActuallyDragOntoTheRaycastedSlot()
        {
            Begin();
            // Batch Editor has no foreground Game View. Keep synthetic input active only for
            // this test, then restore the setting; no ProjectSettings asset is written.
            var background = InputSystem.settings.backgroundBehavior;
            var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var from = ScreenPoint(Card(1926));
                var to = ScreenPoint(Slot(1926));
                var module = Find<InputSystemUIInputModule>();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = from });
                InputSystem.Update();
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left));
                InputSystem.Update();
                yield return null;
                yield return null;
                Assert.That(mouse.enabled, Is.True, "Synthetic input must stay enabled in batch Editor.");
                Assert.That(mouse.position.ReadValue(), Is.EqualTo(from));
                Assert.That(module.GetLastRaycastResult(mouse.deviceId).gameObject, Is.EqualTo(Card(1926).gameObject));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = to }.WithButton(MouseButton.Left));
                InputSystem.Update();
                yield return null;
                yield return null;
                Assert.That(Vector3.Distance(Card(1926).position, Slot(1926).position), Is.LessThan(0.1f),
                    "The card must follow the native pointer before release.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = to });
                InputSystem.Update();
                yield return null;
                yield return null;
                Assert.That(Vector3.Distance(Card(1926).position, Slot(1926).position), Is.LessThan(0.1f));
                Assert.That(controller.Session.MistakeCount, Is.Zero);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = routing;
            }
        }

        [UnityTest]
        public IEnumerator CorrectPlacementTeachesEachEraImmediately()
        {
            Begin();
            var changes = new[]
            {
                "1926｜創立臺北州立宜蘭農林學校，開啟農林教育。",
                "1946｜更名為臺灣省立宜蘭農業職業學校。",
                "1988｜改制為國立宜蘭農工專科學校，進入專科階段。",
                "1998｜升格為國立宜蘭技術學院，從專科走向學院。",
                "2003｜更名為國立宜蘭大學，邁入大學階段。"
            };
            for (int i = 0; i < years.Length; i++)
            {
                Drag(years[i], years[i]);
                Assert.That(GameObject("Feedback").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(changes[i]));
                if (i == 0) Capture("correct-history");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrongPlacementGivesDifferentLearningCluesWithoutAnAnswerYear()
        {
            Begin();
            var clues = new HashSet<string>();
            for (int i = 0; i < years.Length; i++)
            {
                Drag(years[i], years[(i + 1) % years.Length]);
                string clue = GameObject("Feedback").GetComponent<TMPro.TMP_Text>().text;
                Assert.That(clue, Does.StartWith("線索｜"));
                Assert.That(clue, Does.Not.Match("[0-9]"), "The clue must not give the answer year or slot number.");
                Assert.That(clue.Length, Is.LessThanOrEqualTo(48));
                Assert.That(clues.Add(clue), Is.True, "Give a clue about this card's educational stage.");
                Assert.That(controller.Session.MistakeCount, Is.EqualTo(i + 1));
                if (i == 0) Capture("wrong-clue");
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        [UnityTest]
        public IEnumerator KnowledgeBrowserTeachesCultureAndKeepsTheTimerPaused()
        {
            Begin();
            Button("Knowledge").onClick.Invoke();
            float frozen = controller.Session.RemainingTime;
            Assert.That(Button("KnowledgePrevious").interactable, Is.False);
            for (int i = 0; i < years.Length; i++)
            {
                Assert.That(Panel("KnowledgeCard").GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Title").text,
                    Does.Contain(years[i].ToString()));
                var body = Panel("KnowledgeCard").GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Body");
                Assert.That(body.text, Does.Contain("文化連結（非當年事件）"));
                body.ForceMeshUpdate();
                Assert.That(body.isTextOverflowing, Is.False, body.text);
                Assert.That(Button("KnowledgeHistorySource").gameObject.activeInHierarchy, Is.True);
                Assert.That(Button("KnowledgeCultureSource").gameObject.activeInHierarchy, Is.True);
                if (i < years.Length - 1) Button("KnowledgeNext").onClick.Invoke();
            }
            Assert.That(Button("KnowledgeNext").interactable, Is.False);
            controller.Pause();
            Button("KnowledgePrevious").onClick.Invoke();
            Assert.That(Panel("KnowledgeCard").GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Title").text,
                Does.Contain("2003"), "Manual pause must block hidden browser input.");
            controller.Resume();
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(frozen));
            Capture("knowledge-browser");
            Assert.That(Button("SpaceIntroduction").gameObject.activeInHierarchy, Is.True, "Keep the guide inside the learning panel.");
            Button("SpaceIntroduction").onClick.Invoke();
            Assert.That(Panel("Introduction").gameObject.activeInHierarchy, Is.True);
            Assert.That(controller.CanInteract, Is.False);
            Panel("Introduction").PrimaryButton.onClick.Invoke();
            Assert.That(controller.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator CompletionSummaryBrowsesFiveStagesAndRetryClearsItsState()
        {
            Begin();
            int reports = 0;
            controller.Completed += _ => reports++;
            foreach (int year in years) Drag(year, year);
            yield return new WaitForSecondsRealtime(.12f); // Let newly enabled UI complete its colour transition.
            string result = relay.LastResultJson;
            Canvas.ForceUpdateCanvases();
            var resultCard = GameObject("ResultOverlay").transform.Find("ResultCard");
            foreach (string name in new[] { "Title", "Body" })
            {
                var text = resultCard.Find(name).GetComponent<TMPro.TMP_Text>();
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False, $"{name}: font={text.fontSize}, rect={text.rectTransform.rect}, text={text.text}");
            }
            Assert.That(GameObject("ResultHistory").activeInHierarchy, Is.True);
            Assert.That(GameObject("ResultHistoryTitle").GetComponent<TMPro.TMP_Text>().text, Does.Contain("1926"),
                "The completed summary must begin at the first stage, regardless of the last placed card.");
            Assert.That(Button("ResultPrevious").interactable, Is.False);
            for (int i = 0; i < years.Length; i++)
            {
                Assert.That(GameObject("ResultHistoryTitle").GetComponent<TMPro.TMP_Text>().text, Does.Contain(years[i].ToString()));
                var body = GameObject("ResultHistoryBody").GetComponent<TMPro.TMP_Text>();
                Assert.That(body.text, Does.Contain("文化連結（非當年事件）"));
                body.ForceMeshUpdate();
                Assert.That(body.isTextOverflowing, Is.False, body.text);
                foreach (string name in new[] { "ResultHistorySource", "ResultCultureSource" })
                {
                    var button = Button(name);
                    Assert.That(button.interactable, Is.True);
                    var hits = new List<RaycastResult>();
                    Find<EventSystem>().RaycastAll(Pointer((RectTransform)button.transform), hits);
                    Assert.That(hits, Is.Not.Empty);
                    Assert.That(ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject),
                        Is.EqualTo(button.gameObject), "Source controls must be reachable above the result overlay.");
                }
                yield return new WaitForSecondsRealtime(.12f); // Capture each page after its button tint settles.
                Capture("summary-" + years[i]);
                if (i < years.Length - 1) Button("ResultNext").onClick.Invoke();
            }
            Assert.That(Button("ResultNext").interactable, Is.False);
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(relay.LastResultJson, Is.EqualTo(result));
            Assert.That(controller.Session.State, Is.EqualTo(LevelState.Succeeded));
            GameObject("ResultOverlay").GetComponentsInChildren<Button>().Single(b => b.name == "Retry").onClick.Invoke();
            Assert.That(GameObject("ResultHistory").activeInHierarchy, Is.False);
            Assert.That(relay.LastResult, Is.Null);
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(45));
            Begin();
            Button("Knowledge").onClick.Invoke();
            Assert.That(Panel("KnowledgeCard").GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Title").text, Does.Contain("1926"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimeoutAllowsHistoricalReviewWithoutChangingTheFailureReport()
        {
            Begin();
            Drag(1926, 1946);
            Drag(1946, 1946);
            controller.Session.Tick(45);
            string result = relay.LastResultJson;
            Assert.That(controller.Session.State, Is.EqualTo(LevelState.Failed));
            Assert.That(GameObject("ResultHistory").activeInHierarchy, Is.True);
            Assert.That(GameObject("ResultHistoryTitle").GetComponent<TMPro.TMP_Text>().text, Does.Contain("1926"),
                "The timeout summary must also start at the first stage after a later card was placed.");
            Assert.That(Button("ResultPrevious").interactable, Is.False);
            Button("ResultNext").onClick.Invoke();
            Assert.That(GameObject("ResultHistoryTitle").GetComponent<TMPro.TMP_Text>().text, Does.Contain("1946"));
            Assert.That(relay.LastResultJson, Is.EqualTo(result));
            Assert.That(relay.LastResult.Success, Is.False);
            Assert.That(relay.LastResult.MistakeCount, Is.EqualTo(1));
            yield return null;
            Capture("failure-learning");
        }

        private void Begin() => Panel("Introduction").PrimaryButton.onClick.Invoke();
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
        private GameObject GameObject(string name) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == name).gameObject;
        private RectTransform Card(int year) => (RectTransform)GameObject("Card_" + year).transform;
        private RectTransform Slot(int year) => (RectTransform)GameObject("Slot_" + year).transform;
        private Button Button(string name) => GameObject(name).GetComponent<Button>();
        private SharedContentPanel Panel(string name) => GameObject(name).GetComponent<SharedContentPanel>();
        private static Vector2 ScreenPoint(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.position);

        private PointerEventData Pointer(RectTransform card) => new PointerEventData(Find<EventSystem>())
        {
            button = PointerEventData.InputButton.Left, pointerId = -1,
            position = ScreenPoint(card), pressPosition = ScreenPoint(card), pointerDrag = card.gameObject
        };

        private void Drag(int year, int? targetYear)
        {
            Canvas.ForceUpdateCanvases();
            var card = Card(year);
            var pointer = Pointer(card);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = targetYear.HasValue ? ScreenPoint(Slot(targetYear.Value)) : new Vector2(-100, -100);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.dragHandler);
            if (targetYear.HasValue)
            {
                // Use the actual scene's GraphicRaycaster; label/art/locked-card graphics must not swallow a drop.
                var hits = new List<RaycastResult>();
                Find<EventSystem>().RaycastAll(pointer, hits);
                if (controller.CanInteract)
                {
                    Assert.That(hits, Is.Not.Empty);
                    var dropTarget = ExecuteEvents.GetEventHandler<IDropHandler>(hits[0].gameObject);
                    Assert.That(dropTarget, Is.EqualTo(Slot(targetYear.Value).gameObject));
                    ExecuteEvents.Execute(dropTarget, pointer, ExecuteEvents.dropHandler);
                }
                else ExecuteEvents.Execute(Slot(targetYear.Value).gameObject, pointer, ExecuteEvents.dropHandler);
            }
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.endDragHandler);
        }

        private void Capture(string name)
        {
            string directory = Environment.GetEnvironmentVariable("NIU_TIMELINE_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Directory.CreateDirectory(directory);
            var camera = Find<Camera>();
            var canvas = Find<Canvas>();
            var mode = canvas.renderMode;
            var worldCamera = canvas.worldCamera;
            var plane = canvas.planeDistance;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                canvas.renderMode = mode;
                canvas.worldCamera = worldCamera;
                canvas.planeDistance = plane;
                Canvas.ForceUpdateCanvases();
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
#endif
}
