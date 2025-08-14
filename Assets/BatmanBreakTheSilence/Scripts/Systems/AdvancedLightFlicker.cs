using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Режимы мерцания света
    /// </summary>
    public enum FlickerMode
    {
        /// <summary>
        /// Обычный режим: свет тусклый, мерцает становясь ярче
        /// </summary>
        Normal,
        
        /// <summary>
        /// Инвертированный режим: свет яркий, мерцает становясь тусклее/выключаясь
        /// </summary>
        Inverted
    }

    /// <summary>
    /// Продвинутая система мерцания света с настраиваемыми кривыми интенсивности, цветовыми градиентами и контролем времени.
    /// Обеспечивает реалистичные эффекты мерцания для атмосферного освещения в Batman: Break The Silence.
    /// </summary>
    [RequireComponent(typeof(Light))]
    [DisallowMultipleComponent]
    public sealed class AdvancedLightFlicker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Основные настройки")]
        [SerializeField, Tooltip("Включить/выключить эффект мерцания")]
        private bool _isFlickering = true;

        [SerializeField, Tooltip("Базовая интенсивность света, которая масштабируется кривой"), Min(0f)]
        private float _baseIntensity = 1f;

        [SerializeField, Tooltip("Кривая анимации, контролирующая изменение интенсивности (0-1)")]
        private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Диапазоны интенсивности")]
        [SerializeField, Tooltip("Диапазон минимальной интенсивности относительно базовой")]
        private Vector2 _minIntensityRange = new Vector2(0.1f, 0.3f);

        [SerializeField, Tooltip("Диапазон максимальной интенсивности относительно базовой")]
        private Vector2 _maxIntensityRange = new Vector2(0.8f, 1.2f);

        [Header("Настройки времени")]
        [SerializeField, Tooltip("Диапазон длительности одного цикла мерцания (секунды)")]
        private Vector2 _cycleDurationRange = new Vector2(0.5f, 2f);

        [SerializeField, Tooltip("Диапазон задержки между группами мерцаний (секунды)")]
        private Vector2 _delayRange = new Vector2(0f, 0.5f);

        [Header("Режим мерцания")]
        [SerializeField, Tooltip("Режим мерцания: Normal = свет тусклый, мерцает ярче; Inverted = свет яркий, мерцает тусклее")]
        private FlickerMode _flickerMode = FlickerMode.Normal;

        [Header("Группировка мерцаний (Burst Mode)")]
        [SerializeField, Tooltip("Включить режим групп мерцаний")]
        private bool _useBurstMode = true;

        [SerializeField, Tooltip("Количество мерцаний в одной группе"), Range(1, 10)]
        private int _burstCount = 3;

        [SerializeField, Tooltip("Диапазон случайного количества мерцаний в группе")]
        private Vector2Int _burstCountRange = new Vector2Int(2, 5);

        [SerializeField, Tooltip("Использовать случайное количество мерцаний в группе")]
        private bool _useRandomBurstCount = true;

        [SerializeField, Tooltip("Диапазон интервалов между мерцаниями в группе (секунды)")]
        private Vector2 _burstIntervalRange = new Vector2(0.05f, 0.3f);

        [SerializeField, Tooltip("Диапазон длительности каждого мерцания в группе (секунды)")]
        private Vector2 _burstFlickerDurationRange = new Vector2(0.1f, 0.5f);

        [Header("Настройки цвета")]
        [SerializeField, Tooltip("Включить изменение цвета с использованием градиента")]
        private bool _useColorFlicker = false;

        [SerializeField, Tooltip("Градиент цвета для изменения оттенка света")]
        private Gradient _colorGradient = new Gradient();

        [Header("Аудио интеграция")]
        [SerializeField, Tooltip("Источник звука для звуков электрических потрескиваний")]
        private AudioSource _audioSource;

        [SerializeField, Tooltip("Звуковые клипы для эффектов мерцания")]
        private AudioClip[] _flickerSounds;

        [SerializeField, Tooltip("Вероятность воспроизведения звука при цикле мерцания"), Range(0f, 1f)]
        private float _soundProbability = 0.3f;

        [Header("Настройки звука")]
        [SerializeField, Tooltip("Диапазон громкости для звуков мерцания")]
        private Vector2 _volumeRange = new Vector2(0.5f, 1f);

        [SerializeField, Tooltip("Диапазон питча для звуков мерцания")]
        private Vector2 _pitchRange = new Vector2(0.8f, 1.2f);

        [SerializeField, Tooltip("Связать громкость с интенсивностью света")]
        private bool _linkVolumeToIntensity = true;

        [SerializeField, Tooltip("Связать питч с скоростью мерцания")]
        private bool _linkPitchToFlickerSpeed = true;

        [SerializeField, Tooltip("Кривая для связи громкости с интенсивностью (0-1)")]
        private AnimationCurve _volumeIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Кривая для связи питча со скоростью (0-1)")]
        private AnimationCurve _pitchSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Производительность")]
        [SerializeField, Tooltip("Частота обновления для расчетов мерцания"), Range(30f, 120f)]
        private float _updateFrequency = 60f;

        [Header("Отладка")]
        [SerializeField, Tooltip("Показывать визуальные гизмосы для отладки")]
        private bool _showGizmos = true;

        [SerializeField, Tooltip("Включить детальное логирование")]
        private bool _enableLogging = false;

        #endregion

        #region Private Fields

        private Light _lightComponent; // Компонент источника света
        private float _currentCycleTime; // Текущее время цикла
        private float _currentCycleDuration; // Длительность текущего цикла
        private float _currentDelay; // Текущая задержка
        private float _currentMinIntensity; // Текущая минимальная интенсивность
        private float _currentMaxIntensity; // Текущая максимальная интенсивность
        private Color _originalColor; // Оригинальный цвет света
        private bool _isInDelay; // Находится ли в фазе задержки
        private float _progress; // Прогресс цикла (0-1)
        private float _lastUpdateTime; // Время последнего обновления
        private float _updateInterval; // Интервал обновления

        // Переменные для режима групп мерцаний
        private bool _isInBurstMode; // Находится ли в режиме группы
        private int _currentBurstIndex; // Текущий индекс мерцания в группе
        private int _currentBurstCount; // Количество мерцаний в текущей группе
        private float _burstInterval; // Интервал между мерцаниями в группе
        private float _burstFlickerDuration; // Длительность текущего мерцания в группе
        private bool _isInBurstInterval; // Находится ли в интервале между мерцаниями

        // Аудио переменные
        private float _originalVolume; // Оригинальная громкость AudioSource
        private float _originalPitch; // Оригинальный питч AudioSource

        // Оптимизация производительности
        private static readonly int MaxFlickerInstances = 50; // Максимальное количество экземпляров
        private static int ActiveInstances = 0; // Количество активных экземпляров

        #endregion

        #region Properties

        /// <summary>
        /// Получает или устанавливает, мерцает ли свет в данный момент
        /// </summary>
        public bool IsFlickering
        {
            get => _isFlickering;
            set => SetFlickeringState(value);
        }

        /// <summary>
        /// Получает текущую интенсивность света
        /// </summary>
        public float CurrentIntensity => _lightComponent?.intensity ?? 0f;

        /// <summary>
        /// Получает текущий прогресс мерцания (0-1)
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// Получает, находится ли мерцание в фазе задержки
        /// </summary>
        public bool IsInDelay => _isInDelay;

        /// <summary>
        /// Получает, находится ли в режиме группы мерцаний
        /// </summary>
        public bool IsInBurstMode => _isInBurstMode;

        /// <summary>
        /// Получает текущий режим мерцания
        /// </summary>
        public FlickerMode CurrentFlickerMode => _flickerMode;

        #endregion

        #region Events

        /// <summary>
        /// Вызывается при начале нового цикла мерцания
        /// </summary>
        public event Action OnFlickerCycleStart;

        /// <summary>
        /// Вызывается при завершении цикла мерцания
        /// </summary>
        public event Action OnFlickerCycleComplete;

        /// <summary>
        /// Вызывается при начале группы мерцаний
        /// </summary>
        public event Action OnBurstStart;

        /// <summary>
        /// Вызывается при завершении группы мерцаний
        /// </summary>
        public event Action OnBurstComplete;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponent();
        }

        private void OnEnable()
        {
            if (ActiveInstances >= MaxFlickerInstances)
            {
                LogWarning($"Достигнуто максимальное количество экземпляров мерцания ({MaxFlickerInstances}). Отключение компонента на {gameObject.name}");
                enabled = false;
                return;
            }

            ActiveInstances++;
            InitializeCycle();
        }

        private void OnDisable()
        {
            ActiveInstances = Mathf.Max(0, ActiveInstances - 1);
            RestoreOriginalAudioSettings();
        }

        private void Update()
        {
            if (!ShouldUpdate())
                return;

            ProcessFlicker();
            _lastUpdateTime = Time.time;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _lightComponent == null)
                return;

            DrawDebugGizmos();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            _lightComponent = GetComponent<Light>();
            
            if (_lightComponent == null)
            {
                LogError("Компонент Light не найден. Отключение AdvancedLightFlicker.");
                enabled = false;
                return;
            }

            _originalColor = _lightComponent.color;
            _updateInterval = 1f / _updateFrequency;
            
            // Сохранение оригинальных настроек аудио
            if (_audioSource != null)
            {
                _originalVolume = _audioSource.volume;
                _originalPitch = _audioSource.pitch;
            }
            
            ValidateSettings();
            Log("AdvancedLightFlicker успешно инициализирован.");
        }

        private void InitializeCycle()
        {
            if (_lightComponent == null)
                return;

            // Инициализация основных параметров цикла
            _currentCycleDuration = UnityEngine.Random.Range(_cycleDurationRange.x, _cycleDurationRange.y);
            _currentDelay = UnityEngine.Random.Range(_delayRange.x, _delayRange.y);
            
            // Настройка интенсивности в зависимости от режима
            SetupIntensityForMode();
            
            _currentCycleTime = 0f;
            _progress = 0f;
            _isInDelay = _currentDelay > 0f;

            // Инициализация режима групп мерцаний
            if (_useBurstMode)
            {
                InitializeBurstMode();
            }
            else
            {
                _isInBurstMode = false;
            }

            // Установка начального состояния света
            SetInitialLightState();

            PlayFlickerSound();
            OnFlickerCycleStart?.Invoke();
            
            Log($"Новый цикл мерцания инициализирован - Режим: {_flickerMode}, Длительность: {_currentCycleDuration:F2}с, Задержка: {_currentDelay:F2}с, Режим групп: {_useBurstMode}");
        }

        private void SetupIntensityForMode()
        {
            float minRange = UnityEngine.Random.Range(_minIntensityRange.x, _minIntensityRange.y);
            float maxRange = UnityEngine.Random.Range(_maxIntensityRange.x, _maxIntensityRange.y);

            if (_flickerMode == FlickerMode.Normal)
            {
                // Обычный режим: от тусклого к яркому
                _currentMinIntensity = minRange * _baseIntensity;
                _currentMaxIntensity = maxRange * _baseIntensity;
            }
            else
            {
                // Инвертированный режим: от яркого к тусклому
                _currentMinIntensity = maxRange * _baseIntensity;  // Начинаем с яркого
                _currentMaxIntensity = minRange * _baseIntensity;  // Мерцаем к тусклому
            }
        }

        private void SetInitialLightState()
        {
            if (_lightComponent == null)
                return;

            if (_isInDelay)
            {
                // Во время задержки устанавливаем состояние покоя
                if (_flickerMode == FlickerMode.Normal)
                {
                    _lightComponent.intensity = _currentMinIntensity; // Тусклый в покое
                }
                else
                {
                    _lightComponent.intensity = _currentMinIntensity; // Яркий в покое (у нас уже инвертированы значения)
                }
            }
            else if (!_isInBurstMode)
            {
                ApplyLightValues();
            }
        }

        private void InitializeBurstMode()
        {
            _isInBurstMode = true;
            _currentBurstIndex = 0;
            _isInBurstInterval = false;
            
            // Определить количество мерцаний в группе
            if (_useRandomBurstCount)
            {
                _currentBurstCount = UnityEngine.Random.Range(_burstCountRange.x, _burstCountRange.y + 1);
            }
            else
            {
                _currentBurstCount = _burstCount;
            }

            // Установить параметры для первого мерцания в группе
            SetupNextBurstFlicker();
            
            OnBurstStart?.Invoke();
            Log($"Начата группа мерцаний - Количество: {_currentBurstCount}");
        }

        private void SetupNextBurstFlicker()
        {
            // Каждое мерцание в группе имеет свои уникальные параметры
            _burstInterval = UnityEngine.Random.Range(_burstIntervalRange.x, _burstIntervalRange.y);
            _burstFlickerDuration = UnityEngine.Random.Range(_burstFlickerDurationRange.x, _burstFlickerDurationRange.y);
            
            // Настройка интенсивности для каждого мерцания в зависимости от режима
            SetupIntensityForMode();
            
            _currentCycleDuration = _burstFlickerDuration;
            _currentCycleTime = 0f;
            _progress = 0f;

            Log($"Настройка мерцания {_currentBurstIndex + 1}/{_currentBurstCount} - Режим: {_flickerMode}, Длительность: {_burstFlickerDuration:F2}с, Интервал: {_burstInterval:F2}с");
        }

        #endregion

        #region Core Logic

        private bool ShouldUpdate()
        {
            return _isFlickering && 
                   _lightComponent != null && 
                   (Time.time - _lastUpdateTime) >= _updateInterval;
        }

        private void ProcessFlicker()
        {
            if (_isInDelay)
            {
                ProcessDelay();
                return;
            }

            if (_isInBurstMode)
            {
                ProcessBurstMode();
                return;
            }

            ProcessFlickerCycle();
        }

        private void ProcessDelay()
        {
            _currentCycleTime += Time.deltaTime;
            
            if (_currentCycleTime >= _currentDelay)
            {
                _isInDelay = false;
                _currentCycleTime = 0f;
                
                if (_useBurstMode)
                {
                    InitializeBurstMode();
                }
                else
                {
                    ApplyLightValues();
                }
                
                Log("Фаза задержки завершена, начинается цикл мерцания");
            }
        }

        private void ProcessBurstMode()
        {
            if (_isInBurstInterval)
            {
                // Обработка интервала между мерцаниями в группе
                _currentCycleTime += Time.deltaTime;
                
                if (_currentCycleTime >= _burstInterval)
                {
                    _isInBurstInterval = false;
                    _currentBurstIndex++;
                    
                    if (_currentBurstIndex >= _currentBurstCount)
                    {
                        // Группа мерцаний завершена
                        CompleteBurstMode();
                        return;
                    }
                    
                    // Настроить следующее мерцание в группе
                    SetupNextBurstFlicker();
                    ApplyLightValues();
                }
                else
                {
                    // Во время интервала свет возвращается к состоянию покоя
                    if (_flickerMode == FlickerMode.Normal)
                    {
                        _lightComponent.intensity = _currentMinIntensity; // Тусклый в покое
                    }
                    else
                    {
                        _lightComponent.intensity = _currentMinIntensity; // Яркий в покое (значения уже инвертированы)
                    }
                }
            }
            else
            {
                // Обработка отдельного мерцания в группе
                ProcessFlickerCycle();
                
                // Проверить, завершилось ли текущее мерцание
                if (_progress >= 1f)
                {
                    // Переход к интервалу между мерцаниями
                    _isInBurstInterval = true;
                    _currentCycleTime = 0f;
                    
                    // Воспроизвести звук для каждого мерцания в группе
                    PlayFlickerSound();
                }
            }
        }

        private void CompleteBurstMode()
        {
            _isInBurstMode = false;
            OnBurstComplete?.Invoke();
            Log($"Группа мерцаний завершена ({_currentBurstCount} мерцаний)");
            
            // Перейти к обычному циклу или инициализировать новый
            InitializeCycle();
        }

        private void ProcessFlickerCycle()
        {
            _currentCycleTime += Time.deltaTime;
            _progress = Mathf.Clamp01(_currentCycleTime / _currentCycleDuration);

            ApplyLightValues();

            if (_progress >= 1f && !_isInBurstMode)
            {
                OnFlickerCycleComplete?.Invoke();
                InitializeCycle();
            }
        }

        private void ApplyLightValues()
        {
            if (_lightComponent == null)
                return;

            float curveValue = _intensityCurve.Evaluate(_progress);
            float targetIntensity = Mathf.Lerp(_currentMinIntensity, _currentMaxIntensity, curveValue);
            
            _lightComponent.intensity = targetIntensity;

            if (_useColorFlicker)
            {
                _lightComponent.color = _colorGradient.Evaluate(_progress);
            }
        }

        #endregion

        #region Audio System

        private void PlayFlickerSound()
        {
            if (_audioSource == null || 
                _flickerSounds == null || 
                _flickerSounds.Length == 0 ||
                UnityEngine.Random.value > _soundProbability)
                return;

            AudioClip randomClip = _flickerSounds[UnityEngine.Random.Range(0, _flickerSounds.Length)];
            if (randomClip != null)
            {
                // Применить настройки громкости и питча
                ApplyAudioSettings();
                
                _audioSource.PlayOneShot(randomClip);
                
                Log($"Воспроизведен звук мерцания - Громкость: {_audioSource.volume:F2}, Питч: {_audioSource.pitch:F2}");
            }
        }

        private void ApplyAudioSettings()
        {
            if (_audioSource == null)
                return;

            // Расчет громкости
            float volume = CalculateVolume();
            
            // Расчет питча
            float pitch = CalculatePitch();
            
            // Применение настроек
            _audioSource.volume = volume;
            _audioSource.pitch = pitch;
        }

        private float CalculateVolume()
        {
            float baseVolume = UnityEngine.Random.Range(_volumeRange.x, _volumeRange.y);
            
            if (_linkVolumeToIntensity && _lightComponent != null)
            {
                // Нормализация интенсивности света (0-1)
                float normalizedIntensity = Mathf.Clamp01((_lightComponent.intensity - _currentMinIntensity) / 
                                                         (_currentMaxIntensity - _currentMinIntensity));
                
                // Применение кривой связи с интенсивностью
                float intensityModifier = _volumeIntensityCurve.Evaluate(normalizedIntensity);
                
                // Комбинирование базовой громкости с модификатором интенсивности
                baseVolume = Mathf.Lerp(baseVolume * 0.3f, baseVolume, intensityModifier);
            }
            
            // Применение оригинальной громкости как множителя
            return baseVolume * _originalVolume;
        }

        private float CalculatePitch()
        {
            float basePitch = UnityEngine.Random.Range(_pitchRange.x, _pitchRange.y);
            
            if (_linkPitchToFlickerSpeed)
            {
                // Расчет скорости мерцания (обратная к длительности цикла)
                float flickerSpeed = 1f / _currentCycleDuration;
                
                // Нормализация скорости мерцания (предполагаем диапазон от 0.5 до 2 Hz)
                float normalizedSpeed = Mathf.Clamp01((flickerSpeed - 0.5f) / 1.5f);
                
                // Применение кривой связи со скоростью
                float speedModifier = _pitchSpeedCurve.Evaluate(normalizedSpeed);
                
                // Модификация питча в зависимости от скорости
                basePitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, speedModifier);
                
                // Дополнительная вариация для групп мерцаний
                if (_isInBurstMode)
                {
                    // Повышение питча для каждого следующего мерцания в группе
                    float burstModifier = (float)_currentBurstIndex / (_currentBurstCount - 1);
                    basePitch += burstModifier * 0.2f; // Небольшое повышение питча
                }
            }
            
            // Применение оригинального питча как базы
            return basePitch * _originalPitch;
        }

        private void RestoreOriginalAudioSettings()
        {
            if (_audioSource != null)
            {
                _audioSource.volume = _originalVolume;
                _audioSource.pitch = _originalPitch;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Включает или отключает эффект мерцания
        /// </summary>
        /// <param name="state">True для включения, false для отключения</param>
        public void SetFlickeringState(bool state)
        {
            if (_isFlickering == state)
                return;

            _isFlickering = state;

            if (_lightComponent == null)
                return;

            if (_isFlickering)
            {
                InitializeCycle();
                Log("Мерцание включено");
            }
            else
            {
                RestoreOriginalState();
                Log("Мерцание отключено");
            }
        }

        /// <summary>
        /// Устанавливает новую базовую интенсивность и переинициализирует цикл мерцания
        /// </summary>
        /// <param name="intensity">Новое значение базовой интенсивности</param>
        public void SetBaseIntensity(float intensity)
        {
            _baseIntensity = Mathf.Max(0f, intensity);
            
            if (_isFlickering)
            {
                InitializeCycle();
            }
            else
            {
                _lightComponent.intensity = _baseIntensity;
            }
        }

        /// <summary>
        /// Принудительно перезапускает цикл мерцания
        /// </summary>
        public void RestartCycle()
        {
            if (_isFlickering)
            {
                InitializeCycle();
                Log("Цикл мерцания перезапущен вручную");
            }
        }

        /// <summary>
        /// Временно приостанавливает мерцание на указанную длительность
        /// </summary>
        /// <param name="duration">Длительность паузы в секундах</param>
        public void PauseFlicker(float duration)
        {
            if (duration > 0f)
            {
                Invoke(nameof(ResumeFlicker), duration);
                SetFlickeringState(false);
            }
        }

        /// <summary>
        /// Возобновляет мерцание после паузы
        /// </summary>
        public void ResumeFlicker()
        {
            SetFlickeringState(true);
        }

        /// <summary>
        /// Включает или отключает режим групп мерцаний
        /// </summary>
        /// <param name="enable">True для включения режима групп</param>
        public void SetBurstMode(bool enable)
        {
            _useBurstMode = enable;
            if (_isFlickering)
            {
                InitializeCycle();
                Log($"Режим групп мерцаний {(enable ? "включен" : "отключен")}");
            }
        }

        /// <summary>
        /// Устанавливает режим мерцания
        /// </summary>
        /// <param name="mode">Новый режим мерцания</param>
        public void SetFlickerMode(FlickerMode mode)
        {
            _flickerMode = mode;
            if (_isFlickering)
            {
                InitializeCycle();
                Log($"Режим мерцания изменен на: {mode}");
            }
        }

        /// <summary>
        /// Устанавливает количество мерцаний в группе
        /// </summary>
        /// <param name="count">Количество мерцаний (1-10)</param>
        public void SetBurstCount(int count)
        {
            _burstCount = Mathf.Clamp(count, 1, 10);
            Log($"Количество мерцаний в группе установлено: {_burstCount}");
        }

        /// <summary>
        /// Устанавливает диапазон громкости для звуков мерцания
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
        /// Устанавливает диапазон питча для звуков мерцания
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
        /// Включает/отключает связь громкости с интенсивностью света
        /// </summary>
        /// <param name="enable">True для включения связи</param>
        public void SetVolumeIntensityLink(bool enable)
        {
            _linkVolumeToIntensity = enable;
            Log($"Связь громкости с интенсивностью {(enable ? "включена" : "отключена")}");
        }

        /// <summary>
        /// Включает/отключает связь питча со скоростью мерцания
        /// </summary>
        /// <param name="enable">True для включения связи</param>
        public void SetPitchSpeedLink(bool enable)
        {
            _linkPitchToFlickerSpeed = enable;
            Log($"Связь питча со скоростью мерцания {(enable ? "включена" : "отключена")}");
        }

        #endregion

        #region Utility Methods

        private void RestoreOriginalState()
        {
            if (_lightComponent == null)
                return;

            _lightComponent.intensity = _baseIntensity;
            _lightComponent.color = _originalColor;
            
            // Сброс состояния режима групп
            _isInBurstMode = false;
            _isInBurstInterval = false;
            _currentBurstIndex = 0;
            
            // Восстановление оригинальных настроек аудио
            RestoreOriginalAudioSettings();
        }

        private void ValidateSettings()
        {
            _baseIntensity = Mathf.Max(0f, _baseIntensity);
            _minIntensityRange.x = Mathf.Max(0f, _minIntensityRange.x);
            _minIntensityRange.y = Mathf.Max(_minIntensityRange.x, _minIntensityRange.y);
            _maxIntensityRange.x = Mathf.Max(0f, _maxIntensityRange.x);
            _maxIntensityRange.y = Mathf.Max(_maxIntensityRange.x, _maxIntensityRange.y);
            _cycleDurationRange.x = Mathf.Max(0.01f, _cycleDurationRange.x);
            _cycleDurationRange.y = Mathf.Max(_cycleDurationRange.x, _cycleDurationRange.y);
            _delayRange.x = Mathf.Max(0f, _delayRange.x);
            _delayRange.y = Mathf.Max(_delayRange.x, _delayRange.y);
            _updateFrequency = Mathf.Clamp(_updateFrequency, 30f, 120f);
            _soundProbability = Mathf.Clamp01(_soundProbability);
            
            // Валидация настроек режима групп
            _burstCount = Mathf.Clamp(_burstCount, 1, 10);
            _burstCountRange.x = Mathf.Max(1, _burstCountRange.x);
            _burstCountRange.y = Mathf.Max(_burstCountRange.x, _burstCountRange.y);
            _burstIntervalRange.x = Mathf.Max(0.01f, _burstIntervalRange.x);
            _burstIntervalRange.y = Mathf.Max(_burstIntervalRange.x, _burstIntervalRange.y);
            _burstFlickerDurationRange.x = Mathf.Max(0.01f, _burstFlickerDurationRange.x);
            _burstFlickerDurationRange.y = Mathf.Max(_burstFlickerDurationRange.x, _burstFlickerDurationRange.y);
            
            // Валидация аудио настроек
            _volumeRange.x = Mathf.Clamp01(_volumeRange.x);
            _volumeRange.y = Mathf.Clamp01(_volumeRange.y);
            if (_volumeRange.x > _volumeRange.y)
            {
                float temp = _volumeRange.x;
                _volumeRange.x = _volumeRange.y;
                _volumeRange.y = temp;
            }
            
            _pitchRange.x = Mathf.Clamp(_pitchRange.x, 0.1f, 3f);
            _pitchRange.y = Mathf.Clamp(_pitchRange.y, 0.1f, 3f);
            if (_pitchRange.x > _pitchRange.y)
            {
                float temp = _pitchRange.x;
                _pitchRange.x = _pitchRange.y;
                _pitchRange.y = temp;
            }
        }

        private void DrawDebugGizmos()
        {
            // Визуализация интенсивности
            Gizmos.color = Color.Lerp(Color.red, Color.yellow, _progress);
            float radius = Mathf.Lerp(0.1f, 0.5f, _lightComponent.intensity / _baseIntensity);
            Gizmos.DrawWireSphere(transform.position, radius);

            // Индикатор диапазона света
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _lightComponent.range);

            // Индикация режима групп
            if (_isInBurstMode)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, Vector3.one * 0.2f);
            }

            // Визуализация аудио активности
            if (_audioSource != null && _audioSource.isPlaying)
            {
                Gizmos.color = Color.green;
                float audioRadius = Mathf.Lerp(0.05f, 0.15f, _audioSource.volume);
                Gizmos.DrawSphere(transform.position + Vector3.up * 1.2f, audioRadius);
            }

#if UNITY_EDITOR
            string info = $"Интенсивность: {_lightComponent.intensity:F2}\n" +
                         $"Прогресс: {_progress:F2}\n" +
                         $"Режим: {_flickerMode}\n" +
                         $"Цикл: {_currentCycleTime:F2}/{_currentCycleDuration:F2}с";

            if (_isInBurstMode)
            {
                info += $"\nГруппа: {_currentBurstIndex + 1}/{_currentBurstCount}";
                info += _isInBurstInterval ? "\n(Интервал)" : "\n(Мерцание)";
            }
            
            if (_audioSource != null)
            {
                info += $"\nГромкость: {_audioSource.volume:F2}";
                info += $"\nПитч: {_audioSource.pitch:F2}";
                info += _audioSource.isPlaying ? "\n(♪ Звук)" : "";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, info);
#endif
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[AdvancedLightFlicker] {gameObject.name}: {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AdvancedLightFlicker] {gameObject.name}: {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AdvancedLightFlicker] {gameObject.name}: {message}");
        }

        #endregion
    }
}