using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Типы анимации заполнения прогресс-бара
    /// </summary>
    public enum FillAnimationType
    {
        /// <summary>Линейная анимация</summary>
        Linear,
        
        /// <summary>Плавное замедление в конце</summary>
        EaseOut,
        
        /// <summary>Плавное ускорение в начале</summary>
        EaseIn,
        
        /// <summary>Эффект отскока</summary>
        Bounce,
        
        /// <summary>Эластичный эффект с колебаниями</summary>
        Elastic,
        
        /// <summary>Плавное ускорение и замедление</summary>
        EaseInOut,
        
        /// <summary>Выход с превышением границы</summary>
        EaseOutBack,
        
        /// <summary>Вход с превышением границы</summary>
        EaseInBack
    }

    /// <summary>
    /// Типы визуальных эффектов для прогресс-бара
    /// </summary>
    public enum VisualEffectType
    {
        /// <summary>Без эффектов</summary>
        None,
        
        /// <summary>Эффект тряски</summary>
        Shake,
        
        /// <summary>Эффект отскока</summary>
        Bounce,
        
        /// <summary>Масштабирование</summary>
        Scale,
        
        /// <summary>Эффект удара</summary>
        Punch,
        
        /// <summary>Пульсация</summary>
        Pulse
    }

    /// <summary>
    /// Уровни детализации логирования системы
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Без логирования</summary>
        None,
        
        /// <summary>Только критические ошибки</summary>
        Error,
        
        /// <summary>Предупреждения и ошибки</summary>
        Warning,
        
        /// <summary>Информационные сообщения</summary>
        Info,
        
        /// <summary>Отладочная информация</summary>
        Debug,
        
        /// <summary>Подробная техническая информация</summary>
        Verbose
    }

    /// <summary>
    /// Конфигурация визуального эффекта с полным набором параметров
    /// </summary>
    [System.Serializable]
    public struct EffectConfig
    {
        /// <summary>Тип визуального эффекта</summary>
        [Tooltip("Тип визуального эффекта для применения")]
        public VisualEffectType type;
        
        /// <summary>Интенсивность эффекта (0-2)</summary>
        [Tooltip("Сила эффекта - чем больше значение, тем заметнее эффект")]
        public float intensity;
        
        /// <summary>Скорость воспроизведения эффекта</summary>
        [Tooltip("Скорость анимации эффекта")]
        public float speed;
        
        /// <summary>Длительность эффекта в секундах</summary>
        [Tooltip("Время выполнения эффекта")]
        public float duration;
        
        /// <summary>Пользовательская кривая анимации</summary>
        [Tooltip("Кастомная кривая для точного контроля анимации")]
        public AnimationCurve customCurve;

        /// <summary>
        /// Возвращает конфигурацию эффекта по умолчанию
        /// </summary>
        public static EffectConfig Default => new EffectConfig
        {
            type = VisualEffectType.None,
            intensity = 1f,
            speed = 10f,
            duration = 0.25f,
            customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
        };
    }

    /// <summary>
    /// Unity Event для изменения значения прогресс-бара
    /// </summary>
    [System.Serializable]
    public class ValueChangedEvent : UnityEngine.Events.UnityEvent<float> { }

    /// <summary>
    /// Unity Event для достижения граничного значения
    /// </summary>
    [System.Serializable]
    public class BoundaryReachedEvent : UnityEngine.Events.UnityEvent { }

    /// <summary>
    /// Продвинутая система прогресс-бара с комплексной анимацией, эффектами и аудио-фидбеком для Batman: Break The Silence.
    /// Обеспечивает плавные анимации, визуальные эффекты, звуковое сопровождение и высокую производительность.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class AdvancedBar : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Serialized Fields

        [Header("Основные настройки")]
        [SerializeField, Range(0f, 1f), Tooltip("Текущее нормализованное значение прогресс-бара в диапазоне [0-1]")] 
        private float _currentValue = 1f;
        
        [SerializeField, Tooltip("Минимальное значение пользовательского диапазона")] 
        private float _minValue = 0f;
        
        [SerializeField, Tooltip("Максимальное значение пользовательского диапазона")] 
        private float _maxValue = 100f;

        [Header("UI Компоненты")]
        [SerializeField, Tooltip("Image компонент для отображения заполнения прогресс-бара")] 
        private Image _fillImage;
        
        [SerializeField, Tooltip("Slider компонент для альтернативного отображения прогресса")] 
        private Slider _slider;

        [Header("Система анимации")]
        [SerializeField, Tooltip("Включить плавную анимацию при изменении значений")] 
        private bool _useAnimation = true;
        
        [SerializeField, Tooltip("Тип анимации заполнения прогресс-бара")] 
        private FillAnimationType _animationType = FillAnimationType.EaseOut;
        
        [SerializeField, Range(0.1f, 10f), Tooltip("Скорость анимации (значений в секунду)")] 
        private float _animationSpeed = 3f;
        
        [SerializeField, Tooltip("Кастомная кривая анимации (используется только для типа Custom)")] 
        private AnimationCurve _customAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Система визуальных эффектов")]
        [SerializeField, Tooltip("Включить визуальные эффекты при изменении значений")] 
        private bool _useVisualEffects = true;
        
        [SerializeField, Tooltip("Конфигурация эффекта при увеличении значения")] 
        private EffectConfig _increaseEffect = EffectConfig.Default;
        
        [SerializeField, Tooltip("Конфигурация эффекта при уменьшении значения")] 
        private EffectConfig _decreaseEffect = EffectConfig.Default;

        [Header("Система пульсации")]
        [SerializeField, Tooltip("Включить систему пульсации для дополнительных эффектов")] 
        private bool _enablePulseSystem = true;
        
        [SerializeField, Range(0.001f, 0.2f), Tooltip("Интенсивность пульсации")] 
        private float _pulseIntensity = 0.05f;
        
        [SerializeField, Range(0.1f, 2f), Tooltip("Длительность одного цикла пульсации")] 
        private float _pulseDuration = 0.6f;
        
        [SerializeField, Tooltip("Кривая пульсации для точного контроля эффекта")] 
        private AnimationCurve _pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Аудио система")]
        [SerializeField, Tooltip("Источник звука для аудио фидбека")] 
        private AudioSource _audioSource;
        
        [SerializeField, Tooltip("Звуки при увеличении значения")] 
        private AudioClip[] _increaseSounds;
        
        [SerializeField, Tooltip("Звуки при уменьшении значения")] 
        private AudioClip[] _decreaseSounds;
        
        [SerializeField, Tooltip("Звуки при достижении максимального значения")] 
        private AudioClip[] _completionSounds;
        
        [SerializeField, Range(0f, 1f), Tooltip("Базовая громкость звуков")] 
        private float _baseVolume = 0.7f;
        
        [SerializeField, Tooltip("Диапазон изменения громкости в зависимости от интенсивности")] 
        private Vector2 _volumeRange = new Vector2(0.3f, 1f);
        
        [SerializeField, Tooltip("Диапазон изменения высоты тона в зависимости от интенсивности")] 
        private Vector2 _pitchRange = new Vector2(0.8f, 1.3f);

        [Header("Оптимизация производительности")]
        [SerializeField, Range(30, 120), Tooltip("Целевой FPS для анимаций и эффектов")] 
        private int _targetFrameRate = 60;
        
        [SerializeField, Tooltip("Использовать пулинг объектов для эффектов")] 
        private bool _useObjectPooling = true;
        
        [SerializeField, Tooltip("Включить батчинг операций для лучшей производительности")] 
        private bool _enableBatching = true;

        [Header("Отладка и мониторинг")]
        [SerializeField, Tooltip("Показывать визуальные гизмосы для отладки")] 
        private bool _showGizmos = true;
        
        [SerializeField, Tooltip("Включить детальное логирование операций")] 
        private bool _enableLogging = false;

        #endregion

        #region Private Fields

        // Кешированные компоненты для оптимизации
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;

        // Состояние анимации и эффектов
        private float _displayValue;
        private float _targetValue;
        private Coroutine _animationCoroutine;
        private Coroutine _effectCoroutine;
        private Coroutine _pulseCoroutine;

        // Исходные значения трансформации для восстановления
        private Vector3 _originalScale;
        private Vector3 _originalPosition;

        // Оптимизация производительности
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        private readonly System.Collections.Generic.Queue<IEnumerator> _effectQueue = new System.Collections.Generic.Queue<IEnumerator>();

        // Кеш наличия компонентов
        private bool _hasSlider;
        private bool _hasFillImage;
        private bool _hasAudioSource;

        // Система отслеживания граничных состояний
        private bool _previousMaxReached;
        private bool _previousMinReached;

        // Системы метрик и профилирования
        private float _lastUpdateTime;
        private int _animationFrameCount;
        
        // Максимальное количество одновременно активных экземпляров
        private static readonly int MaxBarInstances = 50;
        private static int ActiveInstances = 0;

        #endregion

        #region Events

        // Unity Events для инспектора
        [SerializeField] private ValueChangedEvent _onValueChanged = new ValueChangedEvent();
        [SerializeField] private BoundaryReachedEvent _onMaxValueReached = new BoundaryReachedEvent();
        [SerializeField] private BoundaryReachedEvent _onMinValueReached = new BoundaryReachedEvent();

        /// <summary>Вызывается при изменении значения прогресс-бара (C# Action)</summary>
        public event Action<float> OnValueChanged;
        
        /// <summary>Вызывается при достижении максимального значения</summary>
        public event Action OnMaxValueReached;
        
        /// <summary>Вызывается при достижении минимального значения</summary>
        public event Action OnMinValueReached;
        
        /// <summary>Вызывается при начале анимации заполнения</summary>
        public event Action OnAnimationStart;
        
        /// <summary>Вызывается при завершении анимации заполнения</summary>
        public event Action OnAnimationComplete;

        #endregion

        #region Properties

        /// <summary>
        /// Получает или устанавливает нормализованное значение прогресс-бара в диапазоне [0-1]
        /// </summary>
        public float Value
        {
            get => _currentValue;
            set => SetValue(value);
        }

        /// <summary>
        /// Получает или устанавливает фактическое значение в пользовательском диапазоне [MinValue-MaxValue]
        /// </summary>
        public float RawValue
        {
            get => Mathf.Lerp(_minValue, _maxValue, _currentValue);
            set => SetRawValue(value);
        }

        /// <summary>Получает минимальное значение пользовательского диапазона</summary>
        public float MinValue => _minValue;

        /// <summary>Получает максимальное значение пользовательского диапазона</summary>
        public float MaxValue => _maxValue;

        /// <summary>Получает состояние выполнения анимации заполнения</summary>
        public bool IsAnimating => _animationCoroutine != null;

        /// <summary>Получает состояние выполнения визуальных эффектов</summary>
        public bool IsEffectActive => _effectCoroutine != null;

        /// <summary>Получает прогресс в процентах [0-100]</summary>
        public float Percentage => _currentValue * 100f;

        /// <summary>Получает, инициализирован ли компонент</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Получает текущее состояние системы</summary>
        public bool IsSystemActive { get; private set; } = true;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Инициализация компонента при создании
        /// </summary>
        private void Awake()
        {
            if (ActiveInstances >= MaxBarInstances)
            {
                LogMessage(LogLevel.Warning, $"Достигнуто максимальное количество экземпляров AdvancedBar ({MaxBarInstances}). Отключение компонента на {gameObject.name}");
                enabled = false;
                return;
            }

            ActiveInstances++;
            InitializeComponent();
            LogMessage(LogLevel.Info, "AdvancedBar успешно инициализирован");
        }

        /// <summary>
        /// Настройка начального состояния при активации
        /// </summary>
        private void OnEnable()
        {
            SetupInitialState();
            _displayValue = _currentValue;
            UpdateDisplayComponents(true);
            IsSystemActive = true;
            
            LogMessage(LogLevel.Debug, $"Компонент активирован со значением: {_currentValue:F3}");
        }

        /// <summary>
        /// Очистка ресурсов при деактивации
        /// </summary>
        private void OnDisable()
        {
            StopAllActiveEffects();
            IsSystemActive = false;
            LogMessage(LogLevel.Debug, "Компонент деактивирован, эффекты остановлены");
        }

        /// <summary>
        /// Финальная очистка при уничтожении объекта
        /// </summary>
        private void OnDestroy()
        {
            ActiveInstances = Mathf.Max(0, ActiveInstances - 1);
            StopAllActiveEffects();
            ClearEventListeners();
            LogMessage(LogLevel.Debug, "AdvancedBar уничтожен, ресурсы освобождены");
        }

        /// <summary>
        /// Валидация данных в редакторе
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            
            ValidateSettings();
            LogMessage(LogLevel.Verbose, "Валидация данных завершена");
        }

        /// <summary>
        /// Отрисовка отладочных гизмосов
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;

            DrawDebugGizmos();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует все системы компонента
        /// </summary>
        private void InitializeComponent()
        {
            ValidateAndCacheComponents();
            InitializeDefaultConfigurations();
            SetupInitialState();
            
            IsInitialized = true;
        }

        /// <summary>
        /// Валидирует наличие и кеширует ссылки на компоненты
        /// </summary>
        private void ValidateAndCacheComponents()
        {
            // Обязательные компоненты
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                LogMessage(LogLevel.Error, "RectTransform не найден! Компонент не может функционировать.");
                return;
            }

            // Опциональные компоненты
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();

            // Автопоиск UI компонентов если не назначены в инспекторе
            if (_fillImage == null) _fillImage = GetComponentInChildren<Image>();
            if (_slider == null) _slider = GetComponent<Slider>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

            // Кеширование состояния компонентов для оптимизации
            _hasSlider = _slider != null;
            _hasFillImage = _fillImage != null;
            _hasAudioSource = _audioSource != null;

            LogMessage(LogLevel.Debug, $"Компоненты кеширован - Slider: {_hasSlider}, Fill: {_hasFillImage}, Audio: {_hasAudioSource}");

            // Валидация критических компонентов
            if (!_hasSlider && !_hasFillImage)
            {
                LogMessage(LogLevel.Warning, "Не найдено ни одного UI компонента для отображения прогресса!");
            }
        }

        /// <summary>
        /// Инициализирует конфигурации по умолчанию
        /// </summary>
        private void InitializeDefaultConfigurations()
        {
            // Инициализация эффектов по умолчанию
            if (_increaseEffect.type == VisualEffectType.None)
            {
                _increaseEffect = new EffectConfig
                {
                    type = VisualEffectType.Bounce,
                    intensity = 0.1f,
                    speed = 10f,
                    duration = 0.25f,
                    customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                };
                LogMessage(LogLevel.Debug, "Установлен эффект увеличения по умолчанию: Bounce");
            }

            if (_decreaseEffect.type == VisualEffectType.None)
            {
                _decreaseEffect = new EffectConfig
                {
                    type = VisualEffectType.Shake,
                    intensity = 0.5f,
                    speed = 15f,
                    duration = 0.15f,
                    customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                };
                LogMessage(LogLevel.Debug, "Установлен эффект уменьшения по умолчанию: Shake");
            }

            // Валидация диапазонов
            ValidateRanges();
        }

        /// <summary>
        /// Валидирует и исправляет некорректные диапазоны
        /// </summary>
        private void ValidateRanges()
        {
            if (_volumeRange.x >= _volumeRange.y)
            {
                _volumeRange = new Vector2(0.3f, 1f);
                LogMessage(LogLevel.Warning, "Диапазон громкости сброшен к значениям по умолчанию");
            }

            if (_pitchRange.x >= _pitchRange.y)
            {
                _pitchRange = new Vector2(0.8f, 1.3f);
                LogMessage(LogLevel.Warning, "Диапазон высоты тона сброшен к значениям по умолчанию");
            }
        }

        /// <summary>
        /// Настраивает начальное состояние компонента
        /// </summary>
        private void SetupInitialState()
        {
            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
                _originalPosition = _rectTransform.localPosition;
            }

            _targetValue = _currentValue;
            _displayValue = _currentValue;
            _lastUpdateTime = Time.time;

            LogMessage(LogLevel.Debug, "Начальное состояние настроено");
        }

        /// <summary>
        /// Валидирует все настройки компонента
        /// </summary>
        private void ValidateSettings()
        {
            // Нормализация диапазона значений
            _minValue = Mathf.Min(_minValue, _maxValue - 0.01f);
            _maxValue = Mathf.Max(_maxValue, _minValue + 0.01f);
            _currentValue = Mathf.Clamp01(_currentValue);
            
            // Валидация параметров анимации
            _animationSpeed = Mathf.Clamp(_animationSpeed, 0.1f, 10f);
            _pulseIntensity = Mathf.Clamp(_pulseIntensity, 0.001f, 0.2f);
            _pulseDuration = Mathf.Clamp(_pulseDuration, 0.1f, 2f);
            _baseVolume = Mathf.Clamp01(_baseVolume);
            _targetFrameRate = Mathf.Clamp(_targetFrameRate, 30, 120);
            
            // Валидация диапазонов звука
            ValidateRanges();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Устанавливает нормализованное значение прогресс-бара с возможностью настройки поведения
        /// </summary>
        /// <param name="value">Нормализованное значение в диапазоне [0-1]</param>
        /// <param name="animate">Использовать плавную анимацию</param>
        /// <param name="playSound">Воспроизвести звуковой фидбек</param>
        /// <param name="triggerEffects">Запустить визуальные эффекты</param>
        public void SetValue(float value, bool animate = true, bool playSound = true, bool triggerEffects = true)
        {
            if (!IsSystemActive) return;
            
            float clampedValue = Mathf.Clamp01(value);
            float changeAmount = clampedValue - _currentValue;
            
            // Игнорирование микроскопических изменений для оптимизации
            if (Mathf.Abs(changeAmount) < 0.001f) 
            {
                LogMessage(LogLevel.Verbose, $"Изменение значения проигнорировано: {changeAmount:F6} < 0.001");
                return;
            }

            bool isIncrease = changeAmount > 0f;
            float previousValue = _currentValue;
            _targetValue = clampedValue;

            LogMessage(LogLevel.Debug, $"Установка значения: {clampedValue:F3} (изменение: {changeAmount:F3})");

            // Обработка анимации
            if (animate && _useAnimation && Application.isPlaying)
            {
                StartValueAnimation(_displayValue, _targetValue);
            }
            else
            {
                _displayValue = _targetValue;
                _currentValue = _targetValue;
                UpdateDisplayComponents();
            }

            // Обработка дополнительных систем
            if (Application.isPlaying)
            {
                if (triggerEffects) ProcessValueChangeEffects(changeAmount, isIncrease);
                if (playSound) PlayAudioFeedback(changeAmount, isIncrease);
                
                TriggerValueChangeEvents(previousValue, _targetValue);
            }
        }

        /// <summary>
        /// Устанавливает фактическое значение в пользовательском диапазоне
        /// </summary>
        /// <param name="rawValue">Значение в диапазоне [MinValue-MaxValue]</param>
        /// <param name="animate">Использовать анимацию</param>
        /// <param name="playSound">Воспроизвести звук</param>
        /// <param name="triggerEffects">Запустить эффекты</param>
        public void SetRawValue(float rawValue, bool animate = true, bool playSound = true, bool triggerEffects = true)
        {
            float normalizedValue = Mathf.InverseLerp(_minValue, _maxValue, rawValue);
            SetValue(normalizedValue, animate, playSound, triggerEffects);
            
            LogMessage(LogLevel.Debug, $"Установка Raw значения: {rawValue:F2} -> нормализованное: {normalizedValue:F3}");
        }

        /// <summary>
        /// Добавляет указанное количество к текущему нормализованному значению
        /// </summary>
        /// <param name="amount">Количество для добавления (может быть отрицательным)</param>
        /// <param name="animate">Использовать анимацию</param>
        /// <param name="playSound">Воспроизвести звук</param>
        /// <param name="triggerEffects">Запустить эффекты</param>
        public void AddValue(float amount, bool animate = true, bool playSound = true, bool triggerEffects = true)
        {
            SetValue(_currentValue + amount, animate, playSound, triggerEffects);
        }

        /// <summary>
        /// Добавляет указанное количество к текущему Raw значению
        /// </summary>
        /// <param name="rawAmount">Количество в пользовательских единицах</param>
        /// <param name="animate">Использовать анимацию</param>
        /// <param name="playSound">Воспроизвести звук</param>
        /// <param name="triggerEffects">Запустить эффекты</param>
        public void AddRawValue(float rawAmount, bool animate = true, bool playSound = true, bool triggerEffects = true)
        {
            float normalizedAmount = rawAmount / (_maxValue - _minValue);
            AddValue(normalizedAmount, animate, playSound, triggerEffects);
        }

        /// <summary>
        /// Устанавливает новый диапазон значений для прогресс-бара
        /// </summary>
        /// <param name="minValue">Новое минимальное значение</param>
        /// <param name="maxValue">Новое максимальное значение</param>
        /// <param name="preserveRawValue">Сохранить текущее Raw значение после изменения диапазона</param>
        public void SetRange(float minValue, float maxValue, bool preserveRawValue = true)
        {
            if (maxValue <= minValue)
            {
                LogMessage(LogLevel.Error, $"Некорректный диапазон: min={minValue}, max={maxValue}. Диапазон не изменен.");
                return;
            }

            float currentRawValue = preserveRawValue ? RawValue : 0f;
            
            _minValue = minValue;
            _maxValue = maxValue;

            if (preserveRawValue)
            {
                SetRawValue(currentRawValue, false, false, false);
            }

            LogMessage(LogLevel.Info, $"Диапазон установлен: [{_minValue:F1}, {_maxValue:F1}]");
        }

        /// <summary>
        /// Запускает эффект пульсации с настраиваемыми параметрами
        /// </summary>
        /// <param name="intensity">Интенсивность пульсации (-1 для использования настроек по умолчанию)</param>
        /// <param name="duration">Длительность пульсации (-1 для использования настроек по умолчанию)</param>
        /// <param name="customCurve">Кастомная кривая анимации (null для использования настроек по умолчанию)</param>
        public void TriggerPulse(float intensity = -1f, float duration = -1f, AnimationCurve customCurve = null)
        {
            if (!_enablePulseSystem) 
            {
                LogMessage(LogLevel.Warning, "Система пульсации отключена");
                return;
            }

            float pulseIntensity = intensity > 0 ? intensity : _pulseIntensity;
            float pulseDuration = duration > 0 ? duration : _pulseDuration;
            AnimationCurve curve = customCurve ?? _pulseCurve;

            StopPulseEffect();
            _pulseCoroutine = StartCoroutine(PulseEffectCoroutine(pulseIntensity, pulseDuration, curve));
            
            LogMessage(LogLevel.Debug, $"Пульсация запущена: интенсивность={pulseIntensity:F3}, длительность={pulseDuration:F2}");
        }

        /// <summary>
        /// Запускает безопасную пульсацию, оптимизированную для Slider компонентов
        /// </summary>
        /// <param name="intensity">Интенсивность эффекта</param>
        /// <param name="duration">Длительность эффекта</param>
        public void TriggerSafePulse(float intensity = 0.02f, float duration = 0.6f)
        {
            if (!_enablePulseSystem) return;

            // Автоматическое ограничение интенсивности для слайдеров
            float safeIntensity = _hasSlider ? Mathf.Min(intensity, 0.03f) : intensity;
            
            StopPulseEffect();
            _pulseCoroutine = StartCoroutine(_hasSlider ? 
                SafeSliderPulseCoroutine(safeIntensity, duration) : 
                PulseEffectCoroutine(safeIntensity, duration, _pulseCurve));
                
            LogMessage(LogLevel.Debug, $"Безопасная пульсация запущена для {(_hasSlider ? "Slider" : "Image")}");
        }

        /// <summary>
        /// Запускает стандартный визуальный эффект с базовыми параметрами
        /// </summary>
        /// <param name="effectType">Тип визуального эффекта</param>
        /// <param name="intensity">Интенсивность эффекта</param>
        /// <param name="duration">Длительность эффекта</param>
        public void TriggerEffect(VisualEffectType effectType, float intensity = 1f, float duration = 0.25f)
        {
            if (!_useVisualEffects) 
            {
                LogMessage(LogLevel.Warning, "Визуальные эффекты отключены");
                return;
            }

            EffectConfig config = new EffectConfig
            {
                type = effectType,
                intensity = intensity,
                speed = 15f,
                duration = duration,
                customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };

            TriggerEffect(config);
        }

        /// <summary>
        /// Запускает визуальный эффект с полной конфигурацией
        /// </summary>
        /// <param name="config">Конфигурация эффекта</param>
        public void TriggerEffect(EffectConfig config)
        {
            if (!_useVisualEffects) return;

            // Автоматическая защита от проблемных эффектов для слайдеров
            if (_hasSlider && (config.type == VisualEffectType.Scale || config.type == VisualEffectType.Punch))
            {
                LogMessage(LogLevel.Debug, $"Эффект {config.type} заменен на Shake для совместимости со Slider");
                config.type = VisualEffectType.Shake;
                config.intensity = Mathf.Min(config.intensity, 0.5f);
            }

            StopVisualEffect();
            _effectCoroutine = StartCoroutine(ExecuteEffectCoroutine(config));
            
            LogMessage(LogLevel.Debug, $"Эффект запущен: {config.type}, интенсивность={config.intensity:F2}");
        }

        /// <summary>
        /// Останавливает все активные эффекты и анимации
        /// </summary>
        public void StopAllActiveEffects()
        {
            StopValueAnimation();
            StopVisualEffect();
            StopPulseEffect();
            RestoreOriginalTransform();
            
            LogMessage(LogLevel.Debug, "Все эффекты остановлены");
        }

        /// <summary>
        /// Конфигурирует эффекты для увеличения и уменьшения значений
        /// </summary>
        /// <param name="increaseEffect">Конфигурация для увеличения</param>
        /// <param name="decreaseEffect">Конфигурация для уменьшения</param>
        public void ConfigureEffects(EffectConfig increaseEffect, EffectConfig decreaseEffect)
        {
            _increaseEffect = increaseEffect;
            _decreaseEffect = decreaseEffect;
            
            LogMessage(LogLevel.Info, "Конфигурация эффектов обновлена");
        }

        /// <summary>
        /// Настраивает параметры аудио системы
        /// </summary>
        /// <param name="baseVolume">Базовая громкость [0-1]</param>
        /// <param name="volumeRange">Диапазон изменения громкости</param>
        /// <param name="pitchRange">Диапазон изменения высоты тона</param>
        public void ConfigureAudio(float baseVolume, Vector2 volumeRange, Vector2 pitchRange)
        {
            _baseVolume = Mathf.Clamp01(baseVolume);
            _volumeRange = volumeRange;
            _pitchRange = pitchRange;
            
            LogMessage(LogLevel.Info, $"Аудио настройки обновлены: громкость={_baseVolume:F2}");
        }

        /// <summary>
        /// Возвращает подробную информацию о текущем состоянии системы
        /// </summary>
        /// <returns>Строка с системной информацией</returns>
        public string GetSystemInfo()
        {
            return $"=== AdvancedBar System Info ===\n" +
                   $"Значение: {_currentValue:F3} ({Percentage:F1}%)\n" +
                   $"Raw значение: {RawValue:F1}\n" +
                   $"Диапазон: [{_minValue:F1}, {_maxValue:F1}]\n" +
                   $"Состояние анимации: {IsAnimating}\n" +
                   $"Активные эффекты: {IsEffectActive}\n" +
                   $"Компоненты: Slider={_hasSlider}, Fill={_hasFillImage}, Audio={_hasAudioSource}\n" +
                   $"FPS цель: {_targetFrameRate}\n" +
                   $"Активных экземпляров: {ActiveInstances}/{MaxBarInstances}";
        }

        /// <summary>
        /// Получает метрики производительности системы
        /// </summary>
        /// <returns>Строка с метриками производительности</returns>
        public string GetPerformanceMetrics()
        {
            float timeSinceLastUpdate = Time.time - _lastUpdateTime;
            float estimatedFPS = _animationFrameCount > 0 ? _animationFrameCount / timeSinceLastUpdate : 0f;

            return $"=== Метрики производительности ===\n" +
                   $"Время с последнего обновления: {timeSinceLastUpdate:F3}s\n" +
                   $"Кадров анимации: {_animationFrameCount}\n" +
                   $"Оценочный FPS анимации: {estimatedFPS:F1}\n" +
                   $"Целевой FPS: {_targetFrameRate}\n" +
                   $"Активные корутины: {(IsAnimating ? 1 : 0) + (IsEffectActive ? 1 : 0)}\n" +
                   $"Использование объектного пула: {_useObjectPooling}\n" +
                   $"Батчинг включен: {_enableBatching}";
        }

        /// <summary>
        /// Сбрасывает счетчики производительности
        /// </summary>
        public void ResetPerformanceCounters()
        {
            _animationFrameCount = 0;
            _lastUpdateTime = Time.time;
            LogMessage(LogLevel.Debug, "Счетчики производительности сброшены");
        }

        #endregion

        #region Animation System

        /// <summary>
        /// Запускает анимацию изменения значения
        /// </summary>
        /// <param name="fromValue">Начальное значение</param>
        /// <param name="toValue">Целевое значение</param>
        private void StartValueAnimation(float fromValue, float toValue)
        {
            StopValueAnimation();
            _animationCoroutine = StartCoroutine(AnimateValueCoroutine(fromValue, toValue));
            _animationFrameCount = 0;
        }

        /// <summary>
        /// Останавливает текущую анимацию значения
        /// </summary>
        private void StopValueAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
                LogMessage(LogLevel.Verbose, "Анимация значения остановлена");
            }
        }

        /// <summary>
        /// Корутина анимации изменения значения
        /// </summary>
        private IEnumerator AnimateValueCoroutine(float fromValue, float toValue)
        {
            OnAnimationStart?.Invoke();
            LogMessage(LogLevel.Debug, $"Анимация начата: {fromValue:F3} -> {toValue:F3}");
            
            float elapsedTime = 0f;
            float duration = Mathf.Abs(toValue - fromValue) / _animationSpeed;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = GetAnimationCurveValue(progress);
                
                _displayValue = Mathf.Lerp(fromValue, toValue, curveValue);
                _currentValue = _displayValue;
                
                UpdateDisplayComponents();
                _animationFrameCount++;
                
                yield return null;
            }

            // Финализация анимации
            _displayValue = toValue;
            _currentValue = toValue;
            UpdateDisplayComponents();
            
            OnAnimationComplete?.Invoke();
            LogMessage(LogLevel.Debug, $"Анимация завершена за {_animationFrameCount} кадров");
        }

        /// <summary>
        /// Вычисляет значение кривой анимации для указанного прогресса
        /// </summary>
        /// <param name="t">Прогресс анимации [0-1]</param>
        /// <returns>Значение кривой анимации</returns>
        private float GetAnimationCurveValue(float t)
        {
            return _animationType switch
            {
                FillAnimationType.Linear => t,
                FillAnimationType.EaseOut => 1f - Mathf.Pow(1f - t, 3f),
                FillAnimationType.EaseIn => Mathf.Pow(t, 3f),
                FillAnimationType.EaseInOut => Mathf.SmoothStep(0f, 1f, t),
                FillAnimationType.Bounce => CalculateBounceEase(t),
                FillAnimationType.Elastic => CalculateElasticEase(t),
                FillAnimationType.EaseOutBack => CalculateBackEase(t, false),
                FillAnimationType.EaseInBack => CalculateBackEase(t, true),
                _ => _customAnimationCurve.Evaluate(t)
            };
        }

        /// <summary>
        /// Вычисляет Bounce easing функцию
        /// </summary>
        private float CalculateBounceEase(float t)
        {
            if (t < 1f / 2.75f) return 7.5625f * t * t;
            if (t < 2f / 2.75f) return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
            if (t < 2.5f / 2.75f) return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
            return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }

        /// <summary>
        /// Вычисляет Elastic easing функцию
        /// </summary>
        private float CalculateElasticEase(float t)
        {
            if (t == 0f || t == 1f) return t;
            const float p = 0.3f;
            const float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        /// <summary>
        /// Вычисляет Back easing функцию (с превышением границ)
        /// </summary>
        private float CalculateBackEase(float t, bool easeIn)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return easeIn ? 
                c3 * t * t * t - c1 * t * t :
                1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #endregion

        #region Visual Effects System

        /// <summary>
        /// Обрабатывает визуальные эффекты при изменении значения
        /// </summary>
        /// <param name="changeAmount">Величина изменения</param>
        /// <param name="isIncrease">Увеличение или уменьшение значения</param>
        private void ProcessValueChangeEffects(float changeAmount, bool isIncrease)
        {
            if (!_useVisualEffects) return;

            EffectConfig config = isIncrease ? _increaseEffect : _decreaseEffect;
            
            // Масштабирование интенсивности эффекта в зависимости от изменения
            config.intensity *= Mathf.Abs(changeAmount) * 2f; // Усиление эффекта
            
            TriggerEffect(config);
            LogMessage(LogLevel.Verbose, $"Эффект изменения значения: {(isIncrease ? "увеличение" : "уменьшение")}");
        }

        /// <summary>
        /// Останавливает текущий визуальный эффект
        /// </summary>
        private void StopVisualEffect()
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
                LogMessage(LogLevel.Verbose, "Визуальный эффект остановлен");
            }
        }

        /// <summary>
        /// Корутина выполнения визуального эффекта
        /// </summary>
        private IEnumerator ExecuteEffectCoroutine(EffectConfig config)
        {
            float elapsedTime = 0f;

            while (elapsedTime < config.duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / config.duration;
                float fadeOut = 1f - progress;
                float curveValue = config.customCurve.Evaluate(progress);

                ApplyVisualEffect(config, progress, fadeOut, curveValue);
                yield return null;
            }

            RestoreOriginalTransform();
        }

        /// <summary>
        /// Применяет конкретный визуальный эффект
        /// </summary>
        private void ApplyVisualEffect(EffectConfig config, float progress, float fadeOut, float curveValue)
        {
            switch (config.type)
            {
                case VisualEffectType.Shake:
                    ApplyShakeEffect(config.intensity, fadeOut);
                    break;

                case VisualEffectType.Bounce:
                    if (!_hasSlider) 
                        ApplyBounceEffect(config.intensity, config.speed, progress, fadeOut);
                    else 
                        ApplyShakeEffect(config.intensity * 0.5f, fadeOut);
                    break;

                case VisualEffectType.Scale:
                    if (!_hasSlider) 
                        ApplyScaleEffect(config.intensity, curveValue, fadeOut);
                    else 
                        ApplyShakeEffect(config.intensity * 0.3f, fadeOut);
                    break;

                case VisualEffectType.Punch:
                    if (!_hasSlider) 
                        ApplyPunchEffect(config.intensity, progress);
                    else 
                        ApplyShakeEffect(config.intensity * 0.7f, fadeOut);
                    break;

                case VisualEffectType.Pulse:
                    ApplyPulseEffect(config.intensity, curveValue);
                    break;
            }
        }

        /// <summary>
        /// Эффект тряски (безопасен для всех компонентов)
        /// </summary>
        private void ApplyShakeEffect(float intensity, float fadeOut)
        {
            float maxIntensity = _hasSlider ? 0.5f : 1.0f;
            intensity = Mathf.Min(intensity, maxIntensity);

            Vector3 shake = new Vector3(
                UnityEngine.Random.Range(-intensity, intensity),
                UnityEngine.Random.Range(-intensity, intensity),
                0f
            ) * fadeOut * 30f;

            _rectTransform.localPosition = _originalPosition + shake;
        }

        /// <summary>
        /// Эффект отскока с синусоидальным движением
        /// </summary>
        private void ApplyBounceEffect(float intensity, float speed, float progress, float fadeOut)
        {
            float bounce = Mathf.Sin(progress * Mathf.PI * speed) * intensity * fadeOut;
            _rectTransform.localScale = _originalScale * (1f + bounce);
        }

        /// <summary>
        /// Эффект масштабирования
        /// </summary>
        private void ApplyScaleEffect(float intensity, float curveValue, float fadeOut)
        {
            float scale = curveValue * intensity * fadeOut;
            _rectTransform.localScale = _originalScale * (1f + scale);
        }

        /// <summary>
        /// Эффект "удара" с быстрым увеличением и плавным возвратом
        /// </summary>
        private void ApplyPunchEffect(float intensity, float progress)
        {
            float punch = (1f - Mathf.Pow(progress, 2f)) * intensity;
            _rectTransform.localScale = _originalScale * (1f + punch);
        }

        /// <summary>
        /// Эффект пульсации масштаба
        /// </summary>
        private void ApplyPulseEffect(float intensity, float curveValue)
        {
            float pulse = curveValue * intensity;
            _rectTransform.localScale = _originalScale * (1f + pulse);
        }

        /// <summary>
        /// Восстанавливает исходную трансформацию объекта
        /// </summary>
        private void RestoreOriginalTransform()
        {
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _originalScale;
                _rectTransform.localPosition = _originalPosition;
            }
        }

        #endregion

        #region Pulse System

        /// <summary>
        /// Останавливает эффект пульсации
        /// </summary>
        private void StopPulseEffect()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
                LogMessage(LogLevel.Verbose, "Пульсация остановлена");
            }
        }

        /// <summary>
        /// Корутина стандартного эффекта пульсации
        /// </summary>
        private IEnumerator PulseEffectCoroutine(float intensity, float duration, AnimationCurve curve)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                float curveValue = curve.Evaluate(progress);

                // Синусоидальная пульсация с затуханием
                float pulseValue = Mathf.Sin(progress * Mathf.PI * 2f) * curveValue;
                float fadeOut = 1f - Mathf.Pow(progress, 1.5f);
                pulseValue = (pulseValue + 1f) * 0.5f * fadeOut;

                float scaleValue = 1f + (pulseValue * intensity);
                _rectTransform.localScale = _originalScale * scaleValue;

                yield return null;
            }

            RestoreOriginalTransform();
        }

        /// <summary>
        /// Корутина безопасной пульсации для Slider компонентов
        /// </summary>
        private IEnumerator SafeSliderPulseCoroutine(float intensity, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                float fadeOut = 1f - progress;

                // Очень легкая тряска для слайдера
                Vector3 shake = new Vector3(
                    Mathf.Sin(progress * Mathf.PI * 8f) * intensity,
                    Mathf.Sin(progress * Mathf.PI * 6f) * intensity * 0.5f,
                    0f
                ) * fadeOut * 10f;

                _rectTransform.localPosition = _originalPosition + shake;

                yield return null;
            }

            _rectTransform.localPosition = _originalPosition;
        }

        #endregion

        #region Audio System

        /// <summary>
        /// Воспроизводит аудио фидбек в зависимости от изменения значения
        /// </summary>
        /// <param name="changeAmount">Величина изменения значения</param>
        /// <param name="isIncrease">Увеличение или уменьшение значения</param>
        private void PlayAudioFeedback(float changeAmount, bool isIncrease)
        {
            if (!_hasAudioSource) 
            {
                LogMessage(LogLevel.Verbose, "AudioSource не найден для воспроизведения звука");
                return;
            }

            AudioClip selectedClip = SelectAudioClip(isIncrease);
            if (selectedClip == null) 
            {
                LogMessage(LogLevel.Verbose, $"Звук не найден для {(isIncrease ? "увеличения" : "уменьшения")}");
                return;
            }

            ConfigureAudioSource(changeAmount);
            _audioSource.PlayOneShot(selectedClip);

            LogMessage(LogLevel.Debug, $"Воспроизведен звук: {selectedClip.name} (громкость: {_audioSource.volume:F2})");
        }

        /// <summary>
        /// Выбирает подходящий аудиоклип в зависимости от ситуации
        /// </summary>
        /// <param name="isIncrease">Увеличение или уменьшение значения</param>
        /// <returns>Выбранный AudioClip или null</returns>
        private AudioClip SelectAudioClip(bool isIncrease)
        {
            AudioClip[] soundArray = null;

            // Проверка специальных состояний
            if (Mathf.Approximately(_currentValue, 1f) && _completionSounds.Length > 0)
            {
                soundArray = _completionSounds;
                LogMessage(LogLevel.Verbose, "Выбран звук завершения (100%)");
            }
            else
            {
                soundArray = isIncrease ? _increaseSounds : _decreaseSounds;
            }

            if (soundArray == null || soundArray.Length == 0) return null;

            return soundArray[UnityEngine.Random.Range(0, soundArray.Length)];
        }

        /// <summary>
        /// Настраивает параметры AudioSource в зависимости от изменения
        /// </summary>
        /// <param name="changeAmount">Величина изменения значения</param>
        private void ConfigureAudioSource(float changeAmount)
        {
            float changeIntensity = Mathf.Abs(changeAmount);
            
            // Динамическая громкость в зависимости от интенсивности изменения
            float volume = Mathf.Lerp(_volumeRange.x, _volumeRange.y, changeIntensity) * _baseVolume;
            
            // Динамическая высота тона
            float pitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, changeIntensity);
            
            // Добавление небольшой случайности для разнообразия
            pitch += UnityEngine.Random.Range(-0.05f, 0.05f);

            _audioSource.volume = volume;
            _audioSource.pitch = pitch;
        }

        #endregion

        #region Event System

        /// <summary>
        /// Запускает события при изменении значения
        /// </summary>
        /// <param name="previousValue">Предыдущее значение</param>
        /// <param name="newValue">Новое значение</param>
        private void TriggerValueChangeEvents(float previousValue, float newValue)
        {
            // Основные события изменения значения
            OnValueChanged?.Invoke(newValue);
            _onValueChanged.Invoke(newValue);

            // Проверка граничных значений
            bool maxReached = Mathf.Approximately(newValue, 1f);
            bool minReached = Mathf.Approximately(newValue, 0f);

            // События достижения максимума
            if (maxReached && !_previousMaxReached)
            {
                OnMaxValueReached?.Invoke();
                _onMaxValueReached.Invoke();
                LogMessage(LogLevel.Info, "Достигнуто максимальное значение (100%)");
            }

            // События достижения минимума
            if (minReached && !_previousMinReached)
            {
                OnMinValueReached?.Invoke();
                _onMinValueReached.Invoke();
                LogMessage(LogLevel.Info, "Достигнуто минимальное значение (0%)");
            }

            // Обновление состояния граничных значений
            _previousMaxReached = maxReached;
            _previousMinReached = minReached;

            LogMessage(LogLevel.Verbose, $"События значения обработаны: {previousValue:F3} -> {newValue:F3}");
        }

        /// <summary>
        /// Очищает все подписчики событий
        /// </summary>
        private void ClearEventListeners()
        {
            OnValueChanged = null;
            OnMaxValueReached = null;
            OnMinValueReached = null;
            OnAnimationStart = null;
            OnAnimationComplete = null;
            
            LogMessage(LogLevel.Debug, "Подписчики событий очищены");
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Обновляет отображение всех UI компонентов
        /// </summary>
        /// <param name="forceUpdate">Принудительное обновление без проверки активности</param>
        private void UpdateDisplayComponents(bool forceUpdate = false)
        {
            if (!forceUpdate && !gameObject.activeInHierarchy) return;

            float displayValue = IsAnimating ? _displayValue : _currentValue;

            // Обновление Fill Image
            if (_hasFillImage)
            {
                _fillImage.fillAmount = displayValue;
            }

            // Обновление Slider
            if (_hasSlider)
            {
                _slider.value = Mathf.Lerp(_minValue, _maxValue, displayValue);
            }

            _lastUpdateTime = Time.time;
        }

        #endregion

        #region Debug and Gizmos

        /// <summary>
        /// Отрисовывает отладочные гизмосы
        /// </summary>
        private void DrawDebugGizmos()
        {
            if (!IsInitialized || _rectTransform == null)
                return;

            // Визуализация текущего состояния
            Color stateColor = GetStateColor();
            Gizmos.color = stateColor;
            
            Vector3 center = _rectTransform.position;
            Vector3 size = new Vector3(_rectTransform.rect.width, _rectTransform.rect.height, 1f) * 0.01f;
            
            Gizmos.DrawWireCube(center, size);

            // Визуализация значения через размер
            float normalizedValue = _currentValue;
            Gizmos.color = Color.Lerp(Color.red, Color.green, normalizedValue);
            Gizmos.DrawCube(center, size * normalizedValue);

            // Индикация активности эффектов
            if (IsAnimating)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(center + Vector3.up * 0.5f, 0.1f);
            }

            if (IsEffectActive)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(center + Vector3.down * 0.5f, 0.1f);
            }

#if UNITY_EDITOR
            // Информационная панель в редакторе
            if (_enableLogging)
            {
                string info = $"Value: {_currentValue:F2}\nRaw: {RawValue:F1}\nRange: [{_minValue:F1}, {_maxValue:F1}]";
                if (IsAnimating) info += "\n[ANIMATING]";
                if (IsEffectActive) info += "\n[EFFECT ACTIVE]";
                
                UnityEditor.Handles.Label(center + Vector3.up * 1f, info);
            }
#endif
        }

        /// <summary>
        /// Определяет цвет состояния для гизмосов
        /// </summary>
        private Color GetStateColor()
        {
            if (!IsInitialized) return Color.gray;
            if (!IsSystemActive) return Color.red;
            if (IsAnimating) return Color.cyan;
            if (IsEffectActive) return Color.magenta;
            return Color.green;
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// Устанавливает значение с задержкой
        /// </summary>
        /// <param name="value">Целевое значение</param>
        /// <param name="delay">Задержка в секундах</param>
        /// <param name="animate">Использовать анимацию</param>
        public void SetValueWithDelay(float value, float delay, bool animate = true)
        {
            StartCoroutine(SetValueWithDelayCoroutine(value, delay, animate));
        }

        /// <summary>
        /// Корутина установки значения с задержкой
        /// </summary>
        private IEnumerator SetValueWithDelayCoroutine(float value, float delay, bool animate)
        {
            LogMessage(LogLevel.Debug, $"Установка значения {value:F3} с задержкой {delay:F2}s");
            yield return new WaitForSeconds(delay);
            SetValue(value, animate);
        }

        /// <summary>
        /// Анимирует изменение значения в течение указанного времени
        /// </summary>
        /// <param name="targetValue">Целевое значение</param>
        /// <param name="duration">Длительность анимации</param>
        /// <param name="curve">Кривая анимации</param>
        public void AnimateToValueOverTime(float targetValue, float duration, AnimationCurve curve = null)
        {
            StartCoroutine(AnimateToValueOverTimeCoroutine(targetValue, duration, curve));
        }

        /// <summary>
        /// Корутина анимации к значению за определенное время
        /// </summary>
        private IEnumerator AnimateToValueOverTimeCoroutine(float targetValue, float duration, AnimationCurve curve)
        {
            float startValue = _currentValue;
            float elapsedTime = 0f;
            curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);

            LogMessage(LogLevel.Debug, $"Анимация к значению {targetValue:F3} за {duration:F2}s");

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = curve.Evaluate(progress);
                float currentValue = Mathf.Lerp(startValue, targetValue, curveValue);

                _currentValue = currentValue;
                _displayValue = currentValue;
                UpdateDisplayComponents();

                yield return null;
            }

            _currentValue = targetValue;
            _displayValue = targetValue;
            UpdateDisplayComponents();

            LogMessage(LogLevel.Debug, "Анимация по времени завершена");
        }

        /// <summary>
        /// Создает цепочку изменений значений с интервалами
        /// </summary>
        /// <param name="values">Массив значений</param>
        /// <param name="intervals">Массив интервалов между изменениями</param>
        public void CreateValueSequence(float[] values, float[] intervals)
        {
            if (values.Length != intervals.Length)
            {
                LogMessage(LogLevel.Error, "Количество значений должно соответствовать количеству интервалов");
                return;
            }

            StartCoroutine(ValueSequenceCoroutine(values, intervals));
        }

        /// <summary>
        /// Корутина выполнения последовательности значений
        /// </summary>
        private IEnumerator ValueSequenceCoroutine(float[] values, float[] intervals)
        {
            LogMessage(LogLevel.Info, $"Запуск последовательности из {values.Length} значений");

            for (int i = 0; i < values.Length; i++)
            {
                SetValue(values[i]);
                
                // Ждем завершения анимации
                while (IsAnimating)
                {
                    yield return null;
                }

                // Интервал до следующего значения
                if (i < intervals.Length)
                {
                    yield return new WaitForSeconds(intervals[i]);
                }
            }

            LogMessage(LogLevel.Info, "Последовательность значений завершена");
        }

        #endregion

        #region Logging System

        /// <summary>
        /// Централизованная система логирования с фильтрацией по уровням
        /// </summary>
        /// <param name="level">Уровень важности сообщения</param>
        /// <param name="message">Текст сообщения</param>
        private void LogMessage(LogLevel level, string message)
        {
            // Устанавливаем базовый уровень логирования
            LogLevel currentLogLevel = _enableLogging ? LogLevel.Debug : LogLevel.Warning;
            
            if (level > currentLogLevel) return;

            string formattedMessage = $"[AdvancedBar][{name}] {message}";

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formattedMessage, this);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, this);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                case LogLevel.Verbose:
                    Debug.Log(formattedMessage, this);
                    break;
            }
        }

        #endregion

        #region ISerializationCallbackReceiver Implementation

        /// <summary>
        /// Валидация данных перед сериализацией
        /// </summary>
        public void OnBeforeSerialize()
        {
            ValidateSettings();
        }

        /// <summary>
        /// Восстановление данных после десериализации
        /// </summary>
        public void OnAfterDeserialize()
        {
            _displayValue = _currentValue;
            _targetValue = _currentValue;
        }

        #endregion

        #region Context Menu Methods (Editor Only)

#if UNITY_EDITOR
        [ContextMenu("Тест: Увеличение (+10%)")]
        private void TestIncrease()
        {
            AddValue(0.1f);
        }

        [ContextMenu("Тест: Уменьшение (-10%)")]
        private void TestDecrease()
        {
            AddValue(-0.1f);
        }

        [ContextMenu("Тест: Пульсация")]
        private void TestPulse()
        {
            TriggerPulse();
        }

        [ContextMenu("Сброс к 100%")]
        private void ResetToDefault()
        {
            SetValue(1f, false, false, false);
        }

        [ContextMenu("Сброс к 0%")]
        private void ResetToEmpty()
        {
            SetValue(0f, false, false, false);
        }

        [ContextMenu("Валидация настройки")]
        private void ValidateSetup()
        {
            string issues = "";
            
            if (_fillImage == null && _slider == null)
                issues += "• Не назначен ни Image, ни Slider для отображения прогресса\n";
            
            if (_useAnimation && _animationSpeed <= 0)
                issues += "• Скорость анимации должна быть больше 0\n";
            
            if (_useVisualEffects && _increaseEffect.type == VisualEffectType.None && _decreaseEffect.type == VisualEffectType.None)
                issues += "• Визуальные эффекты включены, но не настроены\n";
                
            if (_hasAudioSource && _audioSource.clip == null && _increaseSounds.Length == 0 && _decreaseSounds.Length == 0)
                issues += "• AudioSource найден, но звуки не назначены\n";
                
            if (_minValue >= _maxValue)
                issues += "• Некорректный диапазон: минимум >= максимума\n";
                
            if (_volumeRange.x >= _volumeRange.y)
                issues += "• Некорректный диапазон громкости\n";
                
            if (_pitchRange.x >= _pitchRange.y)
                issues += "• Некорректный диапазон высоты тона\n";

            if (string.IsNullOrEmpty(issues))
            {
                Debug.Log($"[AdvancedBar][{name}] ✓ Валидация пройдена успешно!", this);
            }
            else
            {
                Debug.LogWarning($"[AdvancedBar][{name}] ⚠ Обнаружены проблемы в настройке:\n{issues}", this);
            }
        }

        [ContextMenu("Показать информацию о системе")]
        private void ShowSystemInfo()
        {
            Debug.Log(GetSystemInfo(), this);
        }

        [ContextMenu("Тест: Все эффекты")]
        private void TestAllEffects()
        {
            StartCoroutine(TestAllEffectsCoroutine());
        }

        /// <summary>
        /// Корутина для тестирования всех эффектов по очереди
        /// </summary>
        private IEnumerator TestAllEffectsCoroutine()
        {
            var effects = new VisualEffectType[] 
            { 
                VisualEffectType.Shake, 
                VisualEffectType.Bounce, 
                VisualEffectType.Scale, 
                VisualEffectType.Punch, 
                VisualEffectType.Pulse 
            };

            foreach (var effect in effects)
            {
                Debug.Log($"[AdvancedBar] Тестирование эффекта: {effect}", this);
                TriggerEffect(effect, 1f, 1f);
                yield return new WaitForSeconds(1.2f);
            }

            Debug.Log($"[AdvancedBar] Тестирование эффектов завершено", this);
        }

        [ContextMenu("Автонастройка компонентов")]
        private void AutoSetupComponents()
        {
            // Поиск Image компонента
            if (_fillImage == null)
            {
                _fillImage = GetComponentInChildren<Image>();
                if (_fillImage != null)
                    Debug.Log($"[AdvancedBar] Найден и назначен Image: {_fillImage.name}", this);
            }

            // Поиск Slider компонента
            if (_slider == null)
            {
                _slider = GetComponent<Slider>();
                if (_slider != null)
                    Debug.Log($"[AdvancedBar] Найден и назначен Slider: {_slider.name}", this);
            }

            // Поиск AudioSource
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource != null)
                    Debug.Log($"[AdvancedBar] Найден и назначен AudioSource: {_audioSource.name}", this);
            }

            // Обновление кеша
            ValidateAndCacheComponents();
            
            Debug.Log($"[AdvancedBar] Автонастройка компонентов завершена", this);
        }

        [ContextMenu("Создать базовую конфигурацию")]
        private void CreateBasicConfiguration()
        {
            // Настройка эффектов
            _increaseEffect = new EffectConfig
            {
                type = VisualEffectType.Bounce,
                intensity = 0.15f,
                speed = 12f,
                duration = 0.3f,
                customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };

            _decreaseEffect = new EffectConfig
            {
                type = VisualEffectType.Shake,
                intensity = 0.8f,
                speed = 18f,
                duration = 0.2f,
                customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };

            // Базовые настройки
            _useAnimation = true;
            _animationType = FillAnimationType.EaseOut;
            _animationSpeed = 4f;
            _useVisualEffects = true;
            _enablePulseSystem = true;

            // Диапазон значений
            _minValue = 0f;
            _maxValue = 100f;
            _currentValue = 1f;

            Debug.Log($"[AdvancedBar] Базовая конфигурация создана", this);
        }
#endif

        #endregion
    }
}