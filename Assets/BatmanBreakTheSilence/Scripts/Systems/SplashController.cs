using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BatmanBreakTheSilence.UI
{
    /// <summary>
    /// Типы сплешей
    /// </summary>
    public enum SplashType
    {
        StartSplash,
        CompletionSplash,
        FadeTransition
    }

    /// <summary>
    /// Состояние сплеша
    /// </summary>
    public enum SplashState
    {
        Hidden,
        FadingIn,
        Visible,
        FadingOut
    }

    /// <summary>
    /// Настройки анимации
    /// </summary>
    [Serializable]
    public struct AnimationSettings
    {
        public float fadeInDuration;
        public float fadeOutDuration;
        public float delay;
        public AnimationCurve easeCurve;

        public static AnimationSettings Default => new AnimationSettings
        {
            fadeInDuration = 1f,
            fadeOutDuration = 1f,
            delay = 0f,
            easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };
    }

    /// <summary>
    /// Настройки прозрачности текста
    /// </summary>
    [Serializable]
    public struct TextOpacitySettings
    {
        public float maxDistanceFromCenter;
        [Range(0f, 1f)] public float minAlpha;
        [Range(0f, 1f)] public float maxAlpha;
        public AnimationCurve opacityCurve;

        public static TextOpacitySettings Default => new TextOpacitySettings
        {
            maxDistanceFromCenter = 200f,
            minAlpha = 0.1f,
            maxAlpha = 1f,
            opacityCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f)
        };
    }

    /// <summary>
    /// Базовый класс для сплешей
    /// </summary>
    public abstract class BaseSplash
    {
        public abstract SplashType Type { get; }
        public SplashState State { get; protected set; } = SplashState.Hidden;
        
        protected readonly CanvasGroup canvasGroup;
        protected readonly AnimationSettings settings;
        protected readonly MonoBehaviour runner;
        protected Coroutine currentAnimation;

        protected BaseSplash(CanvasGroup canvasGroup, AnimationSettings settings, MonoBehaviour runner)
        {
            this.canvasGroup = canvasGroup;
            this.settings = settings;
            this.runner = runner;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public virtual void Show()
        {
            if (!IsValid() || State == SplashState.Visible || State == SplashState.FadingIn)
                return;

            State = SplashState.FadingIn;
            StopAnimation();
            currentAnimation = runner.StartCoroutine(ShowAnimation());
        }

        public virtual void Hide()
        {
            if (!IsValid() || State == SplashState.Hidden || State == SplashState.FadingOut)
                return;

            State = SplashState.FadingOut;
            StopAnimation();
            currentAnimation = runner.StartCoroutine(HideAnimation());
        }

        public virtual void HideImmediate()
        {
            if (!IsValid()) return;
            
            StopAnimation();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            State = SplashState.Hidden;
        }

        public virtual void Update() { }

        public virtual void Cleanup()
        {
            StopAnimation();
        }

        protected bool IsValid()
        {
            return canvasGroup != null && runner != null;
        }

        protected void StopAnimation()
        {
            if (currentAnimation != null && runner != null)
            {
                runner.StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
        }

        protected virtual IEnumerator ShowAnimation()
        {
            if (settings.delay > 0f)
                yield return new WaitForSeconds(settings.delay);

            if (!IsValid()) yield break;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            yield return AnimateAlpha(0f, 1f, settings.fadeInDuration);
            
            if (IsValid())
                State = SplashState.Visible;
            
            currentAnimation = null;
        }

        protected virtual IEnumerator HideAnimation()
        {
            yield return AnimateAlpha(1f, 0f, settings.fadeOutDuration);
            
            if (!IsValid()) yield break;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            State = SplashState.Hidden;
            currentAnimation = null;
        }

        protected IEnumerator AnimateAlpha(float start, float end, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration && IsValid())
            {
                float t = elapsed / duration;
                float eased = settings.easeCurve.Evaluate(t);
                canvasGroup.alpha = Mathf.Lerp(start, end, eased);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (IsValid())
                canvasGroup.alpha = end;
        }
    }

    /// <summary>
    /// Стартовый сплеш с динамическими текстами
    /// </summary>
    public sealed class StartSplash : BaseSplash
    {
        public override SplashType Type => SplashType.StartSplash;
        
        private readonly TextMeshProUGUI gameText;
        private readonly TextMeshProUGUI startText;
        private readonly TextOpacitySettings gameOpacity;
        private readonly TextOpacitySettings startOpacity;

        public StartSplash(CanvasGroup canvas, AnimationSettings anim, MonoBehaviour runner,
                          TextMeshProUGUI gameText, TextMeshProUGUI startText,
                          TextOpacitySettings gameOpacity, TextOpacitySettings startOpacity)
            : base(canvas, anim, runner)
        {
            this.gameText = gameText;
            this.startText = startText;
            this.gameOpacity = gameOpacity;
            this.startOpacity = startOpacity;

            // Инициализация начальной прозрачности текстов
            Vector3 mouse = Input.mousePosition;
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            float distance = Vector3.Distance(mouse, center);
            UpdateTextAlpha(gameText, distance, gameOpacity, true);
            UpdateTextAlpha(startText, distance, startOpacity, false);
        }

        public override void Update()
        {
            if (State != SplashState.Visible) return;

            Vector3 mouse = Input.mousePosition;
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            float distance = Vector3.Distance(mouse, center);

            UpdateTextAlpha(gameText, distance, gameOpacity, true);
            UpdateTextAlpha(startText, distance, startOpacity, false);
        }

        private void UpdateTextAlpha(TextMeshProUGUI text, float distance, TextOpacitySettings settings, bool invert)
        {
            if (text == null) return;

            float normalized = Mathf.Clamp01(distance / settings.maxDistanceFromCenter);
            if (invert) normalized = 1f - normalized;

            float curve = settings.opacityCurve.Evaluate(normalized);
            float alpha = Mathf.Lerp(settings.minAlpha, settings.maxAlpha, curve);

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    /// <summary>
    /// Сплеш завершения
    /// </summary>
    public sealed class CompletionSplash : BaseSplash
    {
        public override SplashType Type => SplashType.CompletionSplash;

        public CompletionSplash(CanvasGroup canvas, AnimationSettings anim, MonoBehaviour runner)
            : base(canvas, anim, runner) { }
    }

    /// <summary>
    /// Переходный фейд
    /// </summary>
    public sealed class FadeTransition : BaseSplash
    {
        public override SplashType Type => SplashType.FadeTransition;

        public FadeTransition(CanvasGroup canvas, AnimationSettings anim, MonoBehaviour runner)
            : base(canvas, anim, runner)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // Устанавливаем полную непрозрачность
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                State = SplashState.Visible; // Устанавливаем начальное состояние как видимое
            }
        }
    }

    /// <summary>
    /// Главный контроллер сплешей
    /// </summary>
    public sealed class SplashController : MonoBehaviour
    {
        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup startCanvas;
        [SerializeField] private CanvasGroup completionCanvas;
        [SerializeField] private CanvasGroup fadeCanvas;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI gameHintText;
        [SerializeField] private TextMeshProUGUI startHintText;

        [Header("Settings")]
        [SerializeField] private KeyCode hideStartKey = KeyCode.Y;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private AnimationSettings startAnim = AnimationSettings.Default;
        [SerializeField] private AnimationSettings completionAnim = AnimationSettings.Default;
        [SerializeField] private AnimationSettings fadeAnim = AnimationSettings.Default;
        [SerializeField] private TextOpacitySettings gameOpacity = TextOpacitySettings.Default;
        [SerializeField] private TextOpacitySettings startOpacity = TextOpacitySettings.Default;

        private StartSplash startSplash;
        private CompletionSplash completionSplash;
        private FadeTransition fadeTransition;
        private bool isTransitioning;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            StartCoroutine(DelayedStart());
        }

        private void Update()
        {
            HandleInput();
            UpdateSplashes();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Initialize()
        {
            if (!ValidateComponents())
            {
                Debug.LogError("SplashController: One or more required components are missing!");
                return;
            }

            startSplash = new StartSplash(startCanvas, startAnim, this, 
                                        gameHintText, startHintText, gameOpacity, startOpacity);
            completionSplash = new CompletionSplash(completionCanvas, completionAnim, this);
            fadeTransition = new FadeTransition(fadeCanvas, fadeAnim, this);
        }

        private bool ValidateComponents()
        {
            return startCanvas != null && completionCanvas != null && fadeCanvas != null;
        }

        private void Cleanup()
        {
            startSplash?.Cleanup();
            completionSplash?.Cleanup();
            fadeTransition?.Cleanup();
        }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForEndOfFrame();
            Debug.Log("SplashController: Starting scene with fade transition and start splash");
            if (fadeTransition != null && fadeTransition.State == SplashState.Visible)
            {
                fadeTransition.Hide(); // Запускаем исчезновение фейд-сплеша
            }
            startSplash?.Show();
        }

        private void HandleInput()
        {
            if (isTransitioning) return;

            if (Input.GetKeyDown(hideStartKey) && startSplash?.State == SplashState.Visible)
                startSplash.Hide();

            if (Input.GetKeyDown(restartKey))
                StartCoroutine(RestartScene());
        }

        private void UpdateSplashes()
        {
            startSplash?.Update();
            completionSplash?.Update();
            fadeTransition?.Update();
        }

        public void ShowSplash(SplashType type)
        {
            GetSplash(type)?.Show();
        }

        public void HideSplash(SplashType type)
        {
            GetSplash(type)?.Hide();
        }

        public void ShowCompletionSplash()
        {
            completionSplash?.Show();
        }

        public SplashState GetSplashState(SplashType type)
        {
            return GetSplash(type)?.State ?? SplashState.Hidden;
        }

        private BaseSplash GetSplash(SplashType type)
        {
            return type switch
            {
                SplashType.StartSplash => startSplash,
                SplashType.CompletionSplash => completionSplash,
                SplashType.FadeTransition => fadeTransition,
                _ => null
            };
        }

        private IEnumerator RestartScene()
        {
            if (isTransitioning) yield break;
            
            isTransitioning = true;

            // Скрываем все сплеши кроме фейда
            startSplash?.HideImmediate();
            completionSplash?.HideImmediate();

            // Показываем фейд
            fadeTransition?.Show();

            // Ждем завершения фейда
            yield return new WaitUntil(() => fadeTransition?.State == SplashState.Visible);
            yield return new WaitForSeconds(0.1f);

            // Очищаем и перезагружаем
            Cleanup();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        #region Editor Support
#if UNITY_EDITOR
        [ContextMenu("Show Start Splash")]
        private void TestShowStart() => startSplash?.Show();

        [ContextMenu("Hide Start Splash")]
        private void TestHideStart() => startSplash?.Hide();

        [ContextMenu("Show Completion")]
        private void TestCompletion() => completionSplash?.Show();

        [ContextMenu("Restart Scene")]
        private void TestRestart() => StartCoroutine(RestartScene());
#endif
        #endregion
    }
}