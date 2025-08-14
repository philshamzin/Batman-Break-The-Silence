using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Типы целей для системы дыхания
    /// </summary>
    public enum BreathTargetType
    {
        /// <summary>
        /// Управление Transform объекта
        /// </summary>
        Transform,
        
        /// <summary>
        /// Управление ConfigurableJoint сустава
        /// </summary>
        ConfigurableJoint
    }

    /// <summary>
    /// Оси вращения для дыхательного движения
    /// </summary>
    public enum BreathAxis
    {
        /// <summary>
        /// Вращение по оси X
        /// </summary>
        X,
        
        /// <summary>
        /// Вращение по оси Y
        /// </summary>
        Y,
        
        /// <summary>
        /// Вращение по оси Z
        /// </summary>
        Z
    }

    /// <summary>
    /// Настройки цели дыхания для анимации
    /// </summary>
    [System.Serializable]
    public class BreathTarget
    {
        [Header("Основные настройки")]
        [SerializeField, Tooltip("Тип цели для анимации дыхания")]
        public BreathTargetType targetType = BreathTargetType.Transform;
        
        [SerializeField, Tooltip("Transform для анимации (если не указан, используется transform компонента)")]
        public Transform targetTransform;
        
        [SerializeField, Tooltip("ConfigurableJoint для анимации физических объектов")]
        public ConfigurableJoint targetJoint;

        [Header("Параметры вращения")]
        [SerializeField, Tooltip("Ось вращения для дыхательного движения")]
        public BreathAxis axis = BreathAxis.X;
        
        [SerializeField, Tooltip("Кривая анимации дыхания (время 0-1, значение -1 до 1)")]
        public AnimationCurve breathCurve = AnimationCurve.EaseInOut(0f, -1f, 1f, 1f);
        
        [SerializeField, Tooltip("Базовая амплитуда вращения в градусах"), Range(0f, 45f)]
        public float amplitude = 5f;
        
        [SerializeField, Tooltip("Случайная вариация амплитуды в процентах"), Range(0f, 50f)]
        public float amplitudeVariation = 10f;
        
        [SerializeField, Tooltip("Смещение фазы относительно основного цикла"), Range(0f, 1f)]
        public float phaseOffset = 0f;

        // Runtime data - скрыты от инспектора для чистоты интерфейса
        [HideInInspector] public Quaternion initialRotation;
        [HideInInspector] public float currentAngle;
        [HideInInspector] public float velocity;
        [HideInInspector] public float actualAmplitude;
        [HideInInspector] public bool isInitialized;
    }

    /// <summary>
    /// Универсальная система дыхания с поддержкой множественных целей, физики, аудио и событий.
    /// Обеспечивает реалистичные дыхательные движения для персонажей в Batman: Break The Silence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UniversalBreathingSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Основные настройки")]
        [SerializeField, Tooltip("Включить/выключить систему дыхания")]
        private bool _isBreathing = true;

        [SerializeField, Tooltip("Длительность полного цикла дыхания (вдох + выдох) в секундах"), Range(2f, 30f)]
        private float _breathDuration = 4f;

        [SerializeField, Tooltip("Время сглаживания для плавных переходов"), Range(0.1f, 2f)]
        private float _smoothTime = 0.5f;

        [Header("Объекты дыхания")]
        [SerializeField, Tooltip("Массив целей для анимации дыхательных движений")]
        private BreathTarget[] _breathTargets = new BreathTarget[0];

        [Header("Настройки звука")]
        [SerializeField, Tooltip("Источник звука для дыхательных эффектов")]
        private AudioSource _breathAudioSource;

        [SerializeField, Tooltip("Коллекция звуков вдоха")]
        private AudioClip[] _inhaleClips;

        [SerializeField, Tooltip("Коллекция звуков выдоха")]
        private AudioClip[] _exhaleClips;

        [SerializeField, Tooltip("Вероятность воспроизведения звука при смене фазы дыхания"), Range(0f, 1f)]
        private float _soundProbability = 0.8f;

        [SerializeField, Tooltip("Диапазон громкости для звуков дыхания")]
        private Vector2 _volumeRange = new Vector2(0.7f, 1f);

        [SerializeField, Tooltip("Диапазон питча для звуков дыхания")]
        private Vector2 _pitchRange = new Vector2(0.9f, 1.1f);

        [SerializeField, Tooltip("Растягивать аудио клипы под длительность фазы дыхания")]
        private bool _stretchAudio = true;

        [Header("Настройки времени")]
        [SerializeField, Tooltip("Диапазон случайной вариации длительности цикла"), Range(0f, 2f)]
        private float _durationVariation = 0.5f;

        [SerializeField, Tooltip("Использовать случайную вариацию длительности циклов")]
        private bool _useRandomDuration = false;

        [SerializeField, Tooltip("Диапазон паузы между циклами дыхания")]
        private Vector2 _pauseRange = new Vector2(0f, 1f);

        [SerializeField, Tooltip("Использовать паузы между циклами дыхания")]
        private bool _usePauses = false;

        [Header("Продвинутые настройки")]
        [SerializeField, Tooltip("Автоматически начинать дыхание при активации")]
        private bool _autoStart = true;

        [SerializeField, Tooltip("Сбрасывать позиции целей при остановке дыхания")]
        private bool _resetTargetsOnStop = true;

        [SerializeField, Tooltip("Частота обновления системы дыхания"), Range(30f, 120f)]
        private float _updateFrequency = 60f;

        [Header("События Unity")]
        [SerializeField, Tooltip("Событие, вызываемое при начале вдоха")]
        private UnityEvent _onInhale = new UnityEvent();

        [SerializeField, Tooltip("Событие, вызываемое при начале выдоха")]
        private UnityEvent _onExhale = new UnityEvent();

        [SerializeField, Tooltip("Событие, вызываемое при завершении полного цикла дыхания")]
        private UnityEvent _onBreathCycleComplete = new UnityEvent();

        [Header("Отладка")]
        [SerializeField, Tooltip("Показывать визуальные гизмосы для отладки")]
        private bool _showGizmos = true;

        [SerializeField, Tooltip("Включить детальное логирование")]
        private bool _enableLogging = false;

        #endregion

        #region Private Fields

        // Основные переменные состояния
        private float _breathTimer; // Текущее время цикла
        private float _currentCycleDuration; // Длительность текущего цикла
        private bool _wasInhaling = true; // Предыдущее состояние фазы
        private bool _initialized; // Флаг инициализации системы
        private bool _isInPause; // Находится ли система в паузе
        private float _pauseDuration; // Длительность текущей паузы
        private float _pauseTimer; // Таймер паузы
        private float _lastUpdateTime; // Время последнего обновления
        private float _updateInterval; // Интервал обновления

        // Аудио переменные
        private Coroutine _currentAudioCoroutine; // Текущая корутина воспроизведения звука
        private float _originalVolume = 1f; // Оригинальная громкость AudioSource
        private float _originalPitch = 1f; // Оригинальный питч AudioSource
        private bool _audioPlaying; // Флаг воспроизведения звука

        // Оптимизация производительности
        private static readonly int MaxBreathingInstances = 30; // Максимальное количество экземпляров
        private static int ActiveInstances = 0; // Количество активных экземпляров

        #endregion

        #region Properties

        /// <summary>
        /// Получает или устанавливает, активна ли система дыхания
        /// </summary>
        public bool IsBreathing
        {
            get => _isBreathing;
            set => SetBreathingState(value);
        }

        /// <summary>
        /// Получает текущий прогресс цикла дыхания (0-1)
        /// </summary>
        public float Progress => _currentCycleDuration > 0 ? (_breathTimer % _currentCycleDuration) / _currentCycleDuration : 0f;

        /// <summary>
        /// Получает, находится ли система в фазе вдоха
        /// </summary>
        public bool IsInhaling => Progress < 0.5f;

        /// <summary>
        /// Получает, находится ли система в паузе между циклами
        /// </summary>
        public bool IsInPause => _isInPause;

        /// <summary>
        /// Получает текущую длительность цикла дыхания
        /// </summary>
        public float CurrentCycleDuration => _currentCycleDuration;

        /// <summary>
        /// Получает количество настроенных целей дыхания
        /// </summary>
        public int TargetCount => _breathTargets?.Length ?? 0;

        /// <summary>
        /// Получает, воспроизводится ли в данный момент звук дыхания
        /// </summary>
        public bool IsAudioPlaying => _audioPlaying;

        /// <summary>
        /// Получает, инициализирована ли система
        /// </summary>
        public bool IsInitialized => _initialized;

        #endregion

        #region Events

        /// <summary>
        /// Вызывается при начале фазы вдоха
        /// </summary>
        public event Action OnInhaleStart;

        /// <summary>
        /// Вызывается при начале фазы выдоха
        /// </summary>
        public event Action OnExhaleStart;

        /// <summary>
        /// Вызывается при завершении полного цикла дыхания
        /// </summary>
        public event Action OnBreathCycleComplete;

        /// <summary>
        /// Вызывается при начале паузы между циклами
        /// </summary>
        public event Action OnPauseStart;

        /// <summary>
        /// Вызывается при завершении паузы между циклами
        /// </summary>
        public event Action OnPauseEnd;

        /// <summary>
        /// Вызывается при инициализации цели дыхания
        /// </summary>
        public event Action<BreathTarget> OnTargetInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponent();
        }

        private void OnEnable()
        {
            if (ActiveInstances >= MaxBreathingInstances)
            {
                LogWarning($"Достигнуто максимальное количество экземпляров дыхания ({MaxBreathingInstances}). Отключение компонента на {gameObject.name}");
                enabled = false;
                return;
            }

            ActiveInstances++;
        }

        private void Start()
        {
            if (_autoStart && _isBreathing && _initialized)
            {
                // Запуск с небольшой задержкой для корректной инициализации
                Invoke(nameof(StartBreathing), 0.1f);
            }
        }

        private void Update()
        {
            if (!ShouldUpdate())
                return;

            ProcessBreathingSystem();
            _lastUpdateTime = Time.time;
        }

        private void OnDisable()
        {
            ActiveInstances = Mathf.Max(0, ActiveInstances - 1);
            StopAllAudio();
            RestoreOriginalAudioSettings();
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;

            DrawDebugGizmos();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            if (_breathTargets == null || _breathTargets.Length == 0)
            {
                LogError("Нет настроенных целей дыхания! Создание базовой цели с текущим Transform.");
                CreateDefaultTarget();
            }

            // Инициализация аудио настроек
            InitializeAudioSettings();

            // Инициализация параметров обновления
            _updateInterval = 1f / _updateFrequency;
            _currentCycleDuration = _breathDuration;

            // Инициализация всех целей
            InitializeAllTargets();

            _initialized = true;
            Log("UniversalBreathingSystem успешно инициализирована.");
        }

        private void CreateDefaultTarget()
        {
            _breathTargets = new BreathTarget[1];
            _breathTargets[0] = new BreathTarget
            {
                targetType = BreathTargetType.Transform,
                targetTransform = transform,
                axis = BreathAxis.X,
                amplitude = 5f,
                amplitudeVariation = 10f,
                phaseOffset = 0f,
                breathCurve = AnimationCurve.EaseInOut(0f, -1f, 1f, 1f)
            };
        }

        private void InitializeAudioSettings()
        {
            if (_breathAudioSource != null)
            {
                _originalVolume = _breathAudioSource.volume;
                _originalPitch = _breathAudioSource.pitch;
                Log($"Аудио настройки сохранены - Громкость: {_originalVolume:F2}, Питч: {_originalPitch:F2}");
            }
        }

        private void InitializeAllTargets()
        {
            int initializedCount = 0;
            
            foreach (var target in _breathTargets)
            {
                if (InitializeTarget(target))
                {
                    initializedCount++;
                }
            }
            
            Log($"Инициализировано целей дыхания: {initializedCount}/{_breathTargets.Length}");
        }

        private bool InitializeTarget(BreathTarget target)
        {
            if (target == null)
            {
                LogWarning("Обнаружена пустая цель дыхания, пропуск...");
                return false;
            }

            // Установка Transform если не назначен
            if (target.targetType == BreathTargetType.Transform && target.targetTransform == null)
            {
                target.targetTransform = transform;
                Log($"Автоматически назначен Transform для цели: {gameObject.name}");
            }

            // Сохранение изначального поворота
            if (!StoreInitialRotation(target))
            {
                LogWarning($"Не удалось сохранить изначальный поворот для цели типа {target.targetType}");
                return false;
            }

            // Расчет фактической амплитуды с вариацией
            CalculateActualAmplitude(target);

            // Инициализация runtime переменных
            target.currentAngle = 0f;
            target.velocity = 0f;
            target.isInitialized = true;

            OnTargetInitialized?.Invoke(target);
            Log($"Цель инициализирована успешно - Тип: {target.targetType}, Ось: {target.axis}, Амплитуда: {target.actualAmplitude:F2}°");
            
            return true;
        }

        private bool StoreInitialRotation(BreathTarget target)
        {
            switch (target.targetType)
            {
                case BreathTargetType.Transform:
                    if (target.targetTransform != null)
                    {
                        target.initialRotation = target.targetTransform.localRotation;
                        return true;
                    }
                    break;
                    
                case BreathTargetType.ConfigurableJoint:
                    if (target.targetJoint != null)
                    {
                        target.initialRotation = target.targetJoint.targetRotation;
                        return true;
                    }
                    break;
            }
            
            return false;
        }

        private void CalculateActualAmplitude(BreathTarget target)
        {
            float variationMultiplier = UnityEngine.Random.Range(
                1f - target.amplitudeVariation / 100f,
                1f + target.amplitudeVariation / 100f
            );
            
            target.actualAmplitude = target.amplitude * variationMultiplier;
        }

        #endregion

        #region Core Logic

        private bool ShouldUpdate()
        {
            return _initialized && 
                   _isBreathing && 
                   (Time.time - _lastUpdateTime) >= _updateInterval;
        }

        private void ProcessBreathingSystem()
        {
            if (_isInPause)
            {
                ProcessPause();
                return;
            }

            UpdateBreathingCycle();
            UpdateAllTargets();
        }

        private void ProcessPause()
        {
            _pauseTimer += Time.deltaTime;
            
            if (_pauseTimer >= _pauseDuration)
            {
                EndPause();
            }
        }

        private void StartBreathing()
        {
            if (!_initialized)
            {
                LogWarning("Попытка запуска дыхания до инициализации системы");
                return;
            }
            
            InitializeNewCycle();
            
            // Сразу запускаем первый звук вдоха
            TriggerPhaseChange(true);
            
            Log("Система дыхания запущена");
        }

        private void InitializeNewCycle()
        {
            // Расчет длительности цикла с вариацией
            if (_useRandomDuration)
            {
                float variation = UnityEngine.Random.Range(-_durationVariation, _durationVariation);
                _currentCycleDuration = Mathf.Max(2f, _breathDuration + variation);
            }
            else
            {
                _currentCycleDuration = _breathDuration;
            }
            
            _breathTimer = 0f;
            _wasInhaling = true;
            _isInPause = false;
            
            Log($"Новый цикл дыхания инициализирован - Длительность: {_currentCycleDuration:F2}с");
        }

        private void UpdateBreathingCycle()
        {
            _breathTimer += Time.deltaTime;
            
            bool currentlyInhaling = IsInhaling;
            
            // Проверка смены фазы дыхания
            if (_wasInhaling != currentlyInhaling)
            {
                _wasInhaling = currentlyInhaling;
                TriggerPhaseChange(currentlyInhaling);
            }
            
            // Проверка завершения цикла
            if (_breathTimer >= _currentCycleDuration)
            {
                CompleteCycle();
            }
        }

        private void TriggerPhaseChange(bool isInhaling)
        {
            Log($"Смена фазы дыхания: {(isInhaling ? "ВДОХ" : "ВЫДОХ")}");
            
            if (isInhaling)
            {
                PlayBreathSound(_inhaleClips, true);
                _onInhale?.Invoke();
                OnInhaleStart?.Invoke();
            }
            else
            {
                PlayBreathSound(_exhaleClips, false);
                _onExhale?.Invoke();
                OnExhaleStart?.Invoke();
            }
        }

        private void CompleteCycle()
        {
            Log("Цикл дыхания завершен");
            
            _onBreathCycleComplete?.Invoke();
            OnBreathCycleComplete?.Invoke();
            
            // Проверка необходимости паузы
            if (_usePauses && ShouldStartPause())
            {
                StartPause();
            }
            else
            {
                InitializeNewCycle();
            }
        }

        private bool ShouldStartPause()
        {
            return _pauseRange.y > 0f && UnityEngine.Random.value < 0.3f; // 30% шанс паузы
        }

        private void StartPause()
        {
            _isInPause = true;
            _pauseDuration = UnityEngine.Random.Range(_pauseRange.x, _pauseRange.y);
            _pauseTimer = 0f;
            
            OnPauseStart?.Invoke();
            Log($"Начата пауза между циклами дыхания - Длительность: {_pauseDuration:F2}с");
        }

        private void EndPause()
        {
            _isInPause = false;
            _pauseTimer = 0f;
            
            OnPauseEnd?.Invoke();
            Log("Пауза между циклами завершена");
            
            InitializeNewCycle();
        }

        private void UpdateAllTargets()
        {
            float normalizedTime = Progress;
            
            foreach (var target in _breathTargets)
            {
                if (target.isInitialized)
                {
                    UpdateTarget(target, normalizedTime);
                }
            }
        }

        private void UpdateTarget(BreathTarget target, float normalizedTime)
        {
            // Применение смещения фазы
            float adjustedTime = (normalizedTime + target.phaseOffset) % 1f;
            
            // Получение значения из кривой
            float curveValue = target.breathCurve.Evaluate(adjustedTime);
            
            // Расчет целевого угла
            float targetAngle = curveValue * target.actualAmplitude;
            
            // Плавное движение к целевому углу
            target.currentAngle = Mathf.SmoothDamp(
                target.currentAngle, 
                targetAngle, 
                ref target.velocity, 
                _smoothTime
            );
            
            // Применение поворота
            ApplyRotationToTarget(target);
        }

        private void ApplyRotationToTarget(BreathTarget target)
        {
            Vector3 eulerAngles = Vector3.zero;
            
            // Установка угла для соответствующей оси
            switch (target.axis)
            {
                case BreathAxis.X:
                    eulerAngles.x = target.currentAngle;
                    break;
                case BreathAxis.Y:
                    eulerAngles.y = target.currentAngle;
                    break;
                case BreathAxis.Z:
                    eulerAngles.z = target.currentAngle;
                    break;
            }
            
            // Расчет финального поворота
            Quaternion deltaRotation = Quaternion.Euler(eulerAngles);
            Quaternion finalRotation = target.initialRotation * deltaRotation;
            
            // Применение поворота к соответствующему объекту
            switch (target.targetType)
            {
                case BreathTargetType.Transform:
                    if (target.targetTransform != null)
                    {
                        target.targetTransform.localRotation = finalRotation;
                    }
                    break;
                    
                case BreathTargetType.ConfigurableJoint:
                    if (target.targetJoint != null)
                    {
                        target.targetJoint.targetRotation = finalRotation;
                    }
                    break;
            }
        }

        #endregion

        #region Audio System

        private void PlayBreathSound(AudioClip[] clips, bool isInhale)
        {
            if (!ValidateAudioPlayback(clips))
                return;

            if (UnityEngine.Random.value > _soundProbability)
            {
                Log($"Звук пропущен из-за вероятности: {_soundProbability:F2}");
                return;
            }

            // Остановка текущего аудио
            StopAllAudio();

            // Выбор случайного клипа
            AudioClip selectedClip = SelectRandomClip(clips);
            if (selectedClip == null)
                return;

            // Воспроизведение звука
            if (_stretchAudio)
            {
                PlayStretchedAudio(selectedClip, _currentCycleDuration * 0.5f);
            }
            else
            {
                PlaySimpleAudio(selectedClip);
            }

            Log($"Воспроизведение звука дыхания - Тип: {(isInhale ? "вдох" : "выдох")}, Клип: {selectedClip.name}");
        }

        private bool ValidateAudioPlayback(AudioClip[] clips)
        {
            if (_breathAudioSource == null)
            {
                Log("AudioSource не назначен, пропуск воспроизведения звука");
                return false;
            }
            
            if (clips == null || clips.Length == 0)
            {
                Log("Коллекция аудио клипов пуста, пропуск воспроизведения");
                return false;
            }
            
            return true;
        }

        private AudioClip SelectRandomClip(AudioClip[] clips)
        {
            AudioClip selectedClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            
            if (selectedClip == null)
            {
                LogWarning("Выбран пустой аудио клип из коллекции");
            }
            
            return selectedClip;
        }

        private void PlaySimpleAudio(AudioClip clip)
        {
            ApplyAudioSettings(clip, 1f);
            _breathAudioSource.Play();
            _audioPlaying = true;
            
            Log($"Простое воспроизведение аудио - Длительность: {clip.length:F2}с");
        }

        private void PlayStretchedAudio(AudioClip clip, float targetDuration)
        {
            if (clip == null)
            {
                LogWarning("Попытка воспроизведения пустого клипа с растяжением");
                return;
            }

            _currentAudioCoroutine = StartCoroutine(PlayStretchedAudioCoroutine(clip, targetDuration));
        }

        private IEnumerator PlayStretchedAudioCoroutine(AudioClip clip, float targetDuration)
        {
            _audioPlaying = true;
            
            // Расчет множителя скорости для соответствия целевой длительности
            float speedMultiplier = CalculateSpeedMultiplier(clip.length, targetDuration);
            
            // Применение настроек аудио
            ApplyAudioSettings(clip, speedMultiplier);
            
            // Начало воспроизведения
            _breathAudioSource.Play();
            
            Log($"Растянутое воспроизведение - Целевая длительность: {targetDuration:F2}с, Скорость: {speedMultiplier:F2}x");
            
            // Ожидание завершения или смены фазы
            yield return StartCoroutine(WaitForAudioCompletion(targetDuration));
            
            _audioPlaying = false;
            _currentAudioCoroutine = null;
        }

        private float CalculateSpeedMultiplier(float clipLength, float targetDuration)
        {
            float speedMultiplier = clipLength / targetDuration;
            return Mathf.Clamp(speedMultiplier, 0.3f, 2.5f); // Разумные ограничения
        }

        private void ApplyAudioSettings(AudioClip clip, float speedMultiplier)
        {
            _breathAudioSource.clip = clip;
            _breathAudioSource.volume = CalculateVolume();
            _breathAudioSource.pitch = CalculatePitch(speedMultiplier);
        }

        private float CalculateVolume()
        {
            float randomVolume = UnityEngine.Random.Range(_volumeRange.x, _volumeRange.y);
            return randomVolume * _originalVolume;
        }

        private float CalculatePitch(float speedMultiplier)
        {
            float randomPitch = UnityEngine.Random.Range(_pitchRange.x, _pitchRange.y);
            return randomPitch * _originalPitch * speedMultiplier;
        }

        private IEnumerator WaitForAudioCompletion(float targetDuration)
        {
            float elapsed = 0f;
            bool initialInhaleState = IsInhaling;
            
            while (elapsed < targetDuration && _audioPlaying && IsInhaling == initialInhaleState)
            {
                elapsed += Time.deltaTime;
                
                // Обработка зацикливания для коротких клипов
                if (!_breathAudioSource.isPlaying && elapsed < targetDuration - 0.1f)
                {
                    _breathAudioSource.Play();
                    Log("Зацикливание аудио клипа");
                }
                
                yield return null;
            }
        }

        private void StopAllAudio()
        {
            if (_currentAudioCoroutine != null)
            {
                StopCoroutine(_currentAudioCoroutine);
                _currentAudioCoroutine = null;
                Log("Корутина аудио остановлена");
            }
            
            if (_breathAudioSource != null && _breathAudioSource.isPlaying)
            {
                _breathAudioSource.Stop();
                Log("Воспроизведение аудио остановлено");
            }
            
            _audioPlaying = false;
        }

        private void RestoreOriginalAudioSettings()
        {
            if (_breathAudioSource != null)
            {
                _breathAudioSource.volume = _originalVolume;
                _breathAudioSource.pitch = _originalPitch;
                Log("Оригинальные настройки аудио восстановлены");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Включает или отключает систему дыхания
        /// </summary>
        /// <param name="enabled">True для включения, false для отключения</param>
        public void SetBreathingState(bool enabled)
        {
            bool wasBreathing = _isBreathing;
            _isBreathing = enabled;
            
            if (enabled && !wasBreathing && _initialized)
            {
                StartBreathing();
            }
            else if (!enabled && wasBreathing)
            {
                StopBreathingSystem();
            }
            
            Log($"Состояние системы дыхания изменено: {(enabled ? "включено" : "отключено")}");
        }

        /// <summary>
        /// Устанавливает новую длительность цикла дыхания
        /// </summary>
        /// <param name="duration">Длительность в секундах (минимум 2 секунды)</param>
        public void SetBreathDuration(float duration)
        {
            _breathDuration = Mathf.Max(2f, duration);
            
            if (!_isInPause && _isBreathing)
            {
                _currentCycleDuration = _breathDuration;
            }
            
            Log($"Длительность дыхания установлена: {_breathDuration:F2}с");
        }

        /// <summary>
        /// Устанавливает время сглаживания для переходов
        /// </summary>
        /// <param name="smoothTime">Время сглаживания в секундах</param>
        public void SetSmoothTime(float smoothTime)
        {
            _smoothTime = Mathf.Max(0.1f, smoothTime);
            Log($"Время сглаживания установлено: {_smoothTime:F2}с");
        }

        /// <summary>
        /// Принудительно перезапускает текущий цикл дыхания
        /// </summary>
        public void RestartCycle()
        {
            if (!_initialized)
            {
                LogWarning("Попытка перезапуска цикла до инициализации системы");
                return;
            }

            if (_isBreathing)
            {
                StopAllAudio();
                InitializeNewCycle();
                TriggerPhaseChange(true);
                Log("Цикл дыхания принудительно перезапущен");
            }
        }

        /// <summary>
        /// Временно приостанавливает дыхание на указанную длительность
        /// </summary>
        /// <param name="duration">Длительность паузы в секундах</param>
        public void PauseBreathing(float duration)
        {
            if (duration > 0f && _isBreathing)
            {
                StartCoroutine(PauseBreathingCoroutine(duration));
                Log($"Дыхание приостановлено на {duration:F2}с");
            }
        }

        /// <summary>
        /// Возобновляет дыхание после паузы
        /// </summary>
        public void ResumeBreathing()
        {
            if (_initialized && !_isBreathing)
            {
                SetBreathingState(true);
                Log("Дыхание возобновлено");
            }
        }

        /// <summary>
        /// Устанавливает вероятность воспроизведения звуков дыхания
        /// </summary>
        /// <param name="probability">Вероятность от 0 до 1</param>
        public void SetSoundProbability(float probability)
        {
            _soundProbability = Mathf.Clamp01(probability);
            Log($"Вероятность звуков дыхания: {_soundProbability:F2}");
        }

        /// <summary>
        /// Устанавливает диапазон громкости для звуков дыхания
        /// </summary>
        /// <param name="minVolume">Минимальная громкость</param>
        /// <param name="maxVolume">Максимальная громкость</param>
        public void SetVolumeRange(float minVolume, float maxVolume)
        {
            _volumeRange.x = Mathf.Clamp01(minVolume);
            _volumeRange.y = Mathf.Clamp01(maxVolume);
            
            if (_volumeRange.x > _volumeRange.y)
            {
                float temp = _volumeRange.x;
                _volumeRange.x = _volumeRange.y;
                _volumeRange.y = temp;
            }
            
            Log($"Диапазон громкости установлен: {_volumeRange.x:F2} - {_volumeRange.y:F2}");
        }

        /// <summary>
        /// Устанавливает диапазон питча для звуков дыхания
        /// </summary>
        /// <param name="minPitch">Минимальный питч</param>
        /// <param name="maxPitch">Максимальный питч</param>
        public void SetPitchRange(float minPitch, float maxPitch)
        {
            _pitchRange.x = Mathf.Clamp(minPitch, 0.1f, 3f);
            _pitchRange.y = Mathf.Clamp(maxPitch, 0.1f, 3f);
            
            if (_pitchRange.x > _pitchRange.y)
            {
                float temp = _pitchRange.x;
                _pitchRange.x = _pitchRange.y;
                _pitchRange.y = temp;
            }
            
            Log($"Диапазон питча установлен: {_pitchRange.x:F2} - {_pitchRange.y:F2}");
        }

        /// <summary>
        /// Включает или отключает растяжение аудио клипов
        /// </summary>
        /// <param name="enable">True для включения растяжения</param>
        public void SetAudioStretching(bool enable)
        {
            _stretchAudio = enable;
            Log($"Растяжение аудио {(enable ? "включено" : "отключено")}");
        }

        /// <summary>
        /// Включает или отключает использование пауз между циклами
        /// </summary>
        /// <param name="enable">True для включения пауз</param>
        public void SetPauseMode(bool enable)
        {
            _usePauses = enable;
            Log($"Режим пауз между циклами {(enable ? "включен" : "отключен")}");
        }

        /// <summary>
        /// Устанавливает диапазон длительности пауз между циклами
        /// </summary>
        /// <param name="minPause">Минимальная длительность паузы</param>
        /// <param name="maxPause">Максимальная длительность паузы</param>
        public void SetPauseRange(float minPause, float maxPause)
        {
            _pauseRange.x = Mathf.Max(0f, minPause);
            _pauseRange.y = Mathf.Max(_pauseRange.x, maxPause);
            Log($"Диапазон пауз установлен: {_pauseRange.x:F2} - {_pauseRange.y:F2}с");
        }

        /// <summary>
        /// Включает или отключает случайную вариацию длительности циклов
        /// </summary>
        /// <param name="enable">True для включения вариации</param>
        public void SetRandomDuration(bool enable)
        {
            _useRandomDuration = enable;
            Log($"Случайная вариация длительности {(enable ? "включена" : "отключена")}");
        }

        /// <summary>
        /// Устанавливает величину случайной вариации длительности
        /// </summary>
        /// <param name="variation">Максимальная вариация в секундах</param>
        public void SetDurationVariation(float variation)
        {
            _durationVariation = Mathf.Max(0f, variation);
            Log($"Вариация длительности установлена: ±{_durationVariation:F2}с");
        }

        /// <summary>
        /// Получает информацию о текущем состоянии системы для отладки
        /// </summary>
        /// <returns>Строка с детальной информацией о состоянии</returns>
        public string GetDebugInfo()
        {
            if (!_initialized)
                return "Система не инициализирована";

            return $"=== Universal Breathing System ===\n" +
                   $"Состояние: {(_isBreathing ? "АКТИВНА" : "НЕАКТИВНА")}\n" +
                   $"Прогресс цикла: {(Progress * 100):F0}%\n" +
                   $"Текущая фаза: {(IsInhaling ? "ВДОХ" : "ВЫДОХ")}\n" +
                   $"Время цикла: {_breathTimer:F1}/{_currentCycleDuration:F1}с\n" +
                   $"В паузе: {(_isInPause ? $"ДА ({_pauseTimer:F1}/{_pauseDuration:F1}с)" : "НЕТ")}\n" +
                   $"Аудио: {(_audioPlaying ? "ВОСПРОИЗВОДИТСЯ" : "ОСТАНОВЛЕНО")}\n" +
                   $"Целей дыхания: {TargetCount}\n" +
                   $"Активных экземпляров: {ActiveInstances}/{MaxBreathingInstances}";
        }

        /// <summary>
        /// Получает информацию о конкретной цели дыхания
        /// </summary>
        /// <param name="targetIndex">Индекс цели в массиве</param>
        /// <returns>Строка с информацией о цели или null если индекс неверен</returns>
        public string GetTargetInfo(int targetIndex)
        {
            if (_breathTargets == null || targetIndex < 0 || targetIndex >= _breathTargets.Length)
                return null;

            var target = _breathTargets[targetIndex];
            if (target == null)
                return "Цель не настроена";

            return $"Цель #{targetIndex}:\n" +
                   $"Тип: {target.targetType}\n" +
                   $"Ось: {target.axis}\n" +
                   $"Амплитуда: {target.actualAmplitude:F2}° (базовая: {target.amplitude:F2}°)\n" +
                   $"Смещение фазы: {target.phaseOffset:F2}\n" +
                   $"Текущий угол: {target.currentAngle:F2}°\n" +
                   $"Скорость: {target.velocity:F2}°/с\n" +
                   $"Инициализирована: {(target.isInitialized ? "ДА" : "НЕТ")}";
        }

        #endregion

        #region Private Helper Methods

        private void StopBreathingSystem()
        {
            StopAllAudio();
            _isInPause = false;
            _pauseTimer = 0f;
            
            if (_resetTargetsOnStop)
            {
                ResetAllTargetsToInitialPosition();
            }
            
            Log("Система дыхания остановлена");
        }

        private void ResetAllTargetsToInitialPosition()
        {
            foreach (var target in _breathTargets)
            {
                if (target != null && target.isInitialized)
                {
                    target.currentAngle = 0f;
                    target.velocity = 0f;
                    ApplyRotationToTarget(target);
                }
            }
            
            Log("Все цели дыхания сброшены к изначальным позициям");
        }

        private IEnumerator PauseBreathingCoroutine(float duration)
        {
            bool wasBreathing = _isBreathing;
            SetBreathingState(false);
            
            yield return new WaitForSeconds(duration);
            
            if (wasBreathing)
            {
                SetBreathingState(true);
            }
        }

        #endregion

        #region Utility Methods

        private void ValidateSettings()
        {
            // Валидация основных параметров
            _breathDuration = Mathf.Max(2f, _breathDuration);
            _smoothTime = Mathf.Clamp(_smoothTime, 0.1f, 2f);
            _updateFrequency = Mathf.Clamp(_updateFrequency, 30f, 120f);
            _soundProbability = Mathf.Clamp01(_soundProbability);
            _durationVariation = Mathf.Max(0f, _durationVariation);
            
            // Валидация диапазонов
            ValidateVectorRange(ref _volumeRange, 0f, 1f);
            ValidateVectorRange(ref _pitchRange, 0.1f, 3f);
            ValidateVectorRange(ref _pauseRange, 0f, float.MaxValue);
            
            // Валидация целей дыхания
            if (_breathTargets != null)
            {
                foreach (var target in _breathTargets)
                {
                    if (target != null)
                    {
                        ValidateBreathTarget(target);
                    }
                }
            }
        }

        private void ValidateVectorRange(ref Vector2 range, float min, float max)
        {
            range.x = Mathf.Clamp(range.x, min, max);
            range.y = Mathf.Clamp(range.y, min, max);
            
            if (range.x > range.y)
            {
                float temp = range.x;
                range.x = range.y;
                range.y = temp;
            }
        }

        private void ValidateBreathTarget(BreathTarget target)
        {
            target.amplitude = Mathf.Clamp(target.amplitude, 0f, 45f);
            target.amplitudeVariation = Mathf.Clamp(target.amplitudeVariation, 0f, 50f);
            target.phaseOffset = Mathf.Clamp01(target.phaseOffset);
        }

        private void DrawDebugGizmos()
        {
            // Основная визуализация состояния
            Color mainColor = GetStateColor();
            Gizmos.color = mainColor;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Индикатор фазы дыхания
            if (_isBreathing && !_isInPause)
            {
                Gizmos.color = IsInhaling ? Color.cyan : Color.yellow;
                float radius = Mathf.Lerp(0.15f, 0.25f, Progress);
                Gizmos.DrawSphere(transform.position, radius);
            }

            // Индикатор аудио активности
            if (_audioPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            }

            // Индикатор паузы
            if (_isInPause)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, Vector3.one * 0.15f);
            }

            // Визуализация целей дыхания
            DrawTargetsGizmos();

#if UNITY_EDITOR
            // Информационная панель
            if (_enableLogging)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, GetDebugInfo());
            }
#endif
        }

        private Color GetStateColor()
        {
            if (!_initialized)
                return Color.gray;
            
            if (!_isBreathing)
                return Color.red;
            
            if (_isInPause)
                return Color.blue;
            
            return Color.green;
        }

        private void DrawTargetsGizmos()
        {
            if (_breathTargets == null)
                return;

            for (int i = 0; i < _breathTargets.Length; i++)
            {
                var target = _breathTargets[i];
                if (target?.isInitialized != true)
                    continue;

                Vector3 targetPosition = GetTargetPosition(target);
                if (targetPosition == Vector3.zero)
                    continue;

                // Линия связи с целью
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(transform.position, targetPosition);

                // Индикатор цели
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.1f);

#if UNITY_EDITOR
                // Номер цели
                UnityEditor.Handles.Label(targetPosition + Vector3.up * 0.2f, $"Target {i}");
#endif
            }
        }

        private Vector3 GetTargetPosition(BreathTarget target)
        {
            switch (target.targetType)
            {
                case BreathTargetType.Transform:
                    return target.targetTransform?.position ?? Vector3.zero;
                
                case BreathTargetType.ConfigurableJoint:
                    return target.targetJoint?.transform?.position ?? Vector3.zero;
                
                default:
                    return Vector3.zero;
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[UniversalBreathingSystem] {gameObject.name}: {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[UniversalBreathingSystem] {gameObject.name}: {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[UniversalBreathingSystem] {gameObject.name}: {message}");
        }

        #endregion
    }
}