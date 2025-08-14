using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    [System.Serializable]
    public struct RecognitionHit
    {
        public float force, timestamp, effectiveness;
        public Vector3 impactPoint;
        public string detectorId;

        public RecognitionHit(float force, float timestamp, Vector3 impactPoint, string detectorId = "", float effectiveness = 1f)
        {
            this.force = force; this.timestamp = timestamp; this.impactPoint = impactPoint;
            this.detectorId = detectorId; this.effectiveness = effectiveness;
        }

        public float Age => Time.time - timestamp;
        public bool IsValid => Age < 10f && force > 0f;
    }

    [System.Serializable]
    public struct PsychologicalResistanceConfig
    {
        public float strongHitThreshold, strongHitsReductionPercent, threeHitsSumThreshold, sumExceededReductionPercent;

        public static PsychologicalResistanceConfig Default => new PsychologicalResistanceConfig
        {
            strongHitThreshold = 70f, strongHitsReductionPercent = 15f, threeHitsSumThreshold = 150f,
            sumExceededReductionPercent = 20f
        };
    }

    [System.Serializable]
    public struct TensionTriggersConfig
    {
        public float increasingSeriesBonus, weakHitThreshold, seriesInterruptionPenalty;
        public float comboTimeWindow, perfectTimingBonus, perfectTimingWindow;

        public static TensionTriggersConfig Default => new TensionTriggersConfig
        {
            increasingSeriesBonus = 10f, weakHitThreshold = 20f, seriesInterruptionPenalty = 25f,
            comboTimeWindow = 3f, perfectTimingBonus = 5f, perfectTimingWindow = 0.5f
        };
    }

    [System.Serializable]
    public struct HeartbeatSystemConfig
    {
        public float baseHeartRate, maxStressHeartRate, heartRateThreshold;
        public float baseIntensity, hitPulseIntensity, hitPulseDuration;
        public bool useSmoothPulse, useSharpHitEffects, enableDynamicIntensity;

        public static HeartbeatSystemConfig Default => new HeartbeatSystemConfig
        {
            baseHeartRate = 80f, maxStressHeartRate = 160f, heartRateThreshold = 30f,
            baseIntensity = 0.05f, hitPulseIntensity = 0.15f, hitPulseDuration = 2f,
            useSmoothPulse = true, useSharpHitEffects = true, enableDynamicIntensity = true
        };
    }

    // Новая структура для звуков ударов
    [System.Serializable]
    public struct ImpactSoundSettings
    {
        [Header("Force Thresholds")]
        public float lightImpactThreshold;
        public float mediumImpactThreshold;
        public float heavyImpactThreshold;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float lightImpactVolume;
        [Range(0f, 1f)] public float mediumImpactVolume;
        [Range(0f, 1f)] public float heavyImpactVolume;
        [Range(0f, 1f)] public float criticalImpactVolume;
        
        [Header("Pitch Settings")]
        [Range(0.5f, 2f)] public float lightImpactPitch;
        [Range(0.5f, 2f)] public float mediumImpactPitch;
        [Range(0.5f, 2f)] public float heavyImpactPitch;
        [Range(0.5f, 2f)] public float criticalImpactPitch;
        
        [Header("Random Variations")]
        [Range(0f, 0.5f)] public float pitchVariation;
        [Range(0f, 0.3f)] public float volumeVariation;
        
        [Header("Dynamic Effects")]
        public bool enableForceBasedPitch;
        public bool enableForceBasedVolume;
        public bool enableRandomSelection;
        
        [Header("Combo Effects")]
        public bool enableComboVolumeBoost;
        [Range(0f, 0.5f)] public float comboVolumeBonus;
        public bool enableComboReverb;
        [Range(0f, 1f)] public float comboReverbLevel;

        public static ImpactSoundSettings Default => new ImpactSoundSettings
        {
            lightImpactThreshold = 25f,
            mediumImpactThreshold = 50f,
            heavyImpactThreshold = 75f,
            
            lightImpactVolume = 0.4f,
            mediumImpactVolume = 0.6f,
            heavyImpactVolume = 0.8f,
            criticalImpactVolume = 1f,
            
            lightImpactPitch = 1.2f,
            mediumImpactPitch = 1f,
            heavyImpactPitch = 0.8f,
            criticalImpactPitch = 0.6f,
            
            pitchVariation = 0.2f,
            volumeVariation = 0.1f,
            
            enableForceBasedPitch = true,
            enableForceBasedVolume = true,
            enableRandomSelection = true,
            
            enableComboVolumeBoost = true,
            comboVolumeBonus = 0.2f,
            enableComboReverb = false,
            comboReverbLevel = 0.3f
        };
    }

    [System.Serializable]
    public class SoundConfiguration
    {
        [Header("Audio Sources")]
        public AudioSource heartbeatAudioSource, painAudioSource, impactAudioSource;
        
        [Header("Audio Clips")]
        public AudioClip[] heartbeatSounds, painSounds;
        public AudioClip recognitionAchievedSound;
        
        [Header("Impact Sound Clips")]
        public AudioClip[] lightImpactSounds;
        public AudioClip[] mediumImpactSounds;
        public AudioClip[] heavyImpactSounds;
        public AudioClip[] criticalImpactSounds;
        
        [Header("Volume Controls")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float heartbeatVolume = 0.7f;
        [Range(0f, 1f)] public float painVolume = 0.8f;
        [Range(0f, 1f)] public float impactVolume = 0.9f;
        [Range(0f, 1f)] public float recognitionAchievedVolume = 1f;
        
        [Header("General Settings")]
        public bool enableDynamicPitch = true;
        public bool enableDynamicVolume = true;
        public bool enablePainSounds = true;
        public bool enableImpactSounds = true;
        
        [Header("Stress Curves")]
        public AnimationCurve stressPitchCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 1.2f);
        public AnimationCurve stressVolumeCurve = AnimationCurve.Linear(0f, 0.7f, 1f, 1f);
        
        [Header("Pain Sound Settings")]
        [Range(0f, 2f)] public float painPitchVariation = 0.3f;
        [Range(0f, 1f)] public float painChance = 0.7f;
        
        [Header("Recognition Achieved Sound Settings")]
        [Range(0.5f, 2f)] public float recognitionAchievedPitch = 1f;
        [Range(0f, 0.5f)] public float recognitionPitchVariation = 0.1f;
        public bool enableRecognitionPitchStress = true;
        
        [Header("Impact Sound Settings")]
        public ImpactSoundSettings impactSettings = ImpactSoundSettings.Default;
        
        [Header("Advanced Impact Settings")]
        [Range(0f, 1f)] public float impactChance = 0.95f;
        public bool preventImpactOverlap = true;
        [Range(0.1f, 1f)] public float minimumImpactInterval = 0.15f;
        public bool enableSpatialImpactSounds = false;
        [Range(0f, 10f)] public float spatialBlendLevel = 0.5f;
    }

    public enum RecognitionState { Idle, Building, Resistance, Tension, Critical, Achieved, Reset }
    public enum ImpactSoundType { Light, Medium, Heavy, Critical }

    [DisallowMultipleComponent]
    public sealed class RecognitionSystem : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Nested Classes
        private class PerformanceMonitor
        {
            public float averageFrameTime, memoryUsage;
            public int peakHitCount;
            private Queue<float> _frameTimes = new Queue<float>();
            
            public void Update()
            {
                _frameTimes.Enqueue(Time.deltaTime);
                if (_frameTimes.Count > 60) _frameTimes.Dequeue();
                averageFrameTime = _frameTimes.Average();
                memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / 1024f / 1024f;
            }
            
            public void RecordHit() => peakHitCount = Mathf.Max(peakHitCount, peakHitCount + 1);
        }

        private class GameplayAnalytics
        {
            public float sessionStartTime, totalDamageDealt, averageHitInterval;
            public int perfectTimingHits, comboBreaks, maxComboLength, currentComboLength;
            public Dictionary<string, int> detectorUsage = new Dictionary<string, int>();
            public List<float> ipProgression = new List<float>();

            public GameplayAnalytics() 
            {
                sessionStartTime = 0f;
            }

            public void Initialize()
            {
                sessionStartTime = Time.time;
            }

            public void RecordHit(RecognitionHit hit)
            {
                totalDamageDealt += hit.force;
                if (!detectorUsage.ContainsKey(hit.detectorId)) detectorUsage[hit.detectorId] = 0;
                detectorUsage[hit.detectorId]++;
            }

            public void RecordIPChange(float newIP) => ipProgression.Add(newIP);
            public void RecordCombo(int length) { currentComboLength = length; maxComboLength = Mathf.Max(maxComboLength, length); }
            public void RecordComboBreak() { comboBreaks++; currentComboLength = 0; }
            public void RecordPerfectTiming() => perfectTimingHits++;
        }

        [System.Serializable]
        public struct RecognitionStats
        {
            public float currentIP, averageHitForce, timeSinceLastHit, currentHeartRate, stressLevel;
            public int totalHits, effectiveHits, consecutiveFailures;
        }
        #endregion

        #region Events
        public event Action<float> OnIPChanged;
        public event Action OnRecognitionAchieved;
        public event Action<string> OnIPReset;
        public event Action<float> OnHeartRateChanged;
        public event Action<RecognitionHit> OnHitProcessed;
        public event Action OnResistanceTriggered, OnTensionTriggered;
        public event Action<RecognitionState, RecognitionState> OnStateChanged;
        public event Action<ImpactSoundType, float> OnImpactSoundPlayed; // Новое событие
        #endregion

        #region Properties
        public float CurrentIP => _currentIP;
        public float NormalizedIP => _currentIP / 100f;
        public int TotalHitCount => _totalHitCount;
        public int EffectiveHitCount => _hitHistory.Count(h => h.effectiveness > 0.5f);
        public float TimeSinceLastHit => Time.time - _lastHitTime;
        public float CurrentHeartRate => _currentHeartRate;
        public float StressLevel => Mathf.Clamp01((_currentIP - _heartbeatConfig.heartRateThreshold) / (100f - _heartbeatConfig.heartRateThreshold));
        public bool IsHitPulseActive => Time.time < _hitPulseEndTime;
        public bool IsSystemActive { get; private set; } = true;
        public RecognitionState CurrentState => _currentState;
        public RecognitionStats Statistics => CalculateStatistics();
        public IReadOnlyList<RecognitionHit> HitHistory => _hitHistory.AsReadOnly();
        public IReadOnlyList<RecognitionHit> RecentHits => _lastThreeHits.AsReadOnly();
        public bool ImpactSoundsEnabled => _soundConfig?.enableImpactSounds ?? false;
        public ImpactSoundSettings ImpactSettings => _soundConfig?.impactSettings ?? ImpactSoundSettings.Default;
        #endregion

        #region Serialized Fields
        [Header("Components")]
        [SerializeField] private HandMovementDamageDetector[] _damageDetectors;
        [SerializeField] private AdvancedBar _recognitionBar;
        [SerializeField] private BatmanBreakTheSilence.UI.SplashController _splashController;

        [Header("Basic Settings")]
        [SerializeField, Range(0.1f, 3f)] private float _baseForceMultiplier = 0.8f;
        [SerializeField, Range(0f, 50f)] private float _minimumForceThreshold = 5f;
        [SerializeField, Range(50f, 200f)] private float _maximumForceLimit = 100f;
        [SerializeField, Range(1, 20)] private int _maxHitsWithoutSuccess = 6;
        [SerializeField, Range(0f, 50f)] private float _resetIPValue = 10f;

        [Header("Auto Decay")]
        [SerializeField] private bool _enableAutoDecay = true;
        [SerializeField, Range(0.1f, 10f)] private float _decayRate = 2f;
        [SerializeField] private bool _pauseDecayDuringCombos = true;

        [Header("Configurations")]
        [SerializeField] private PsychologicalResistanceConfig _resistanceConfig = PsychologicalResistanceConfig.Default;
        [SerializeField] private TensionTriggersConfig _tensionConfig = TensionTriggersConfig.Default;
        [SerializeField] private HeartbeatSystemConfig _heartbeatConfig = HeartbeatSystemConfig.Default;
        [SerializeField] private SoundConfiguration _soundConfig;

        [Header("Advanced")]
        [SerializeField, Range(1f, 10f)] private float _sequenceAnalysisTime = 5f;
        [SerializeField, Range(10, 100)] private int _maxHistorySize = 50;

        [Header("Debug")]
        [SerializeField] private bool _showDetailedInfo = false;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _showDebugGizmos = false;
        #endregion

        #region Private Fields
        private float _currentIP, _lastHitTime, _currentHeartRate, _hitPulseEndTime;
        private int _totalHitCount, _consecutiveFailures;
        private float _lastDecayTime;
        private float _lastImpactSoundTime; // Новое поле для контроля интервалов между звуками ударов
        private List<RecognitionHit> _hitHistory, _lastThreeHits;
        private Queue<RecognitionHit> _recentHitsQueue;
        private Dictionary<string, HandMovementDamageDetector> _detectorCache;
        private bool _hasRecognitionBar;
        private Coroutine _heartbeatCoroutine;
        private RecognitionState _currentState = RecognitionState.Idle, _previousState = RecognitionState.Idle;
        private PerformanceMonitor _performanceMonitor;
        private GameplayAnalytics _analytics;
        private int _framesSinceLastUpdate;
        private float _lastUpdateTime;
        #endregion

        #region Unity Lifecycle
        private void Awake() => InitializeAllSystems();
        private void OnEnable() 
        { 
            SubscribeToEvents(); 
            StartHeartbeatSystem(); 
            IsSystemActive = true;
            
            // Инициализируем время здесь, а не в OnAfterDeserialize
            if (_lastDecayTime == 0f) _lastDecayTime = Time.time;
            if (_lastImpactSoundTime == 0f) _lastImpactSoundTime = 0f;
            
            if (_analytics != null && _analytics.sessionStartTime == 0f)
                _analytics.Initialize();
        }
        private void OnDisable() { UnsubscribeFromEvents(); StopHeartbeatSystem(); IsSystemActive = false; }
        private void Update() => UpdateAllSystems();
        private void OnGUI() { if (_showDetailedInfo && IsSystemActive) DrawDebugInterface(); }
        private void OnDestroy() => CleanupAllSystems();
        #endregion

        #region Initialization
        private void InitializeAllSystems()
        {
            _hitHistory = new List<RecognitionHit>(_maxHistorySize);
            _lastThreeHits = new List<RecognitionHit>(3);
            _recentHitsQueue = new Queue<RecognitionHit>(10);
            _detectorCache = new Dictionary<string, HandMovementDamageDetector>();
            
            _currentIP = 0f; _totalHitCount = 0; _lastHitTime = Time.time;
            _currentHeartRate = _heartbeatConfig.baseHeartRate; _hitPulseEndTime = 0f;
            _lastDecayTime = Time.time;
            _lastImpactSoundTime = 0f;
            
            _performanceMonitor = new PerformanceMonitor();
            _analytics = new GameplayAnalytics();
            _analytics.Initialize();
            
            AutoDiscoverComponents();
            ValidateConfiguration();
        }

        private void AutoDiscoverComponents()
        {
            if (_damageDetectors == null || _damageDetectors.Length == 0)
                _damageDetectors = FindObjectsOfType<HandMovementDamageDetector>();
            
            if (_recognitionBar == null) _recognitionBar = FindObjectOfType<AdvancedBar>();
            _hasRecognitionBar = _recognitionBar != null;
            
            if (_hasRecognitionBar)
            {
                _recognitionBar.SetRange(0f, 100f);
                _recognitionBar.SetValue(0f, false, false, false);
            }
            
            if (_damageDetectors != null)
            {
                _detectorCache.Clear();
                foreach (var detector in _damageDetectors)
                    if (detector != null) _detectorCache[detector.name] = detector;
            }
            
            // Автоматическое создание AudioSource для звуков ударов если его нет
            if (_soundConfig != null && _soundConfig.impactAudioSource == null)
            {
                var impactSourceGO = new GameObject("ImpactAudioSource");
                impactSourceGO.transform.SetParent(transform);
                _soundConfig.impactAudioSource = impactSourceGO.AddComponent<AudioSource>();
                _soundConfig.impactAudioSource.playOnAwake = false;
                _soundConfig.impactAudioSource.spatialBlend = _soundConfig.enableSpatialImpactSounds ? _soundConfig.spatialBlendLevel : 0f;
            }
        }

        private void ValidateConfiguration()
        {
            _baseForceMultiplier = Mathf.Clamp(_baseForceMultiplier, 0.1f, 3f);
            _minimumForceThreshold = Mathf.Max(0f, _minimumForceThreshold);
            _maximumForceLimit = Mathf.Max(_minimumForceThreshold + 1f, _maximumForceLimit);
            
            _resistanceConfig.strongHitThreshold = Mathf.Clamp(_resistanceConfig.strongHitThreshold, 0f, _maximumForceLimit);
            _heartbeatConfig.baseHeartRate = Mathf.Clamp(_heartbeatConfig.baseHeartRate, 60f, 120f);
            
            // Валидация настроек звуков ударов
            if (_soundConfig != null)
            {
                var impact = _soundConfig.impactSettings;
                impact.lightImpactThreshold = Mathf.Clamp(impact.lightImpactThreshold, 0f, _maximumForceLimit);
                impact.mediumImpactThreshold = Mathf.Clamp(impact.mediumImpactThreshold, impact.lightImpactThreshold, _maximumForceLimit);
                impact.heavyImpactThreshold = Mathf.Clamp(impact.heavyImpactThreshold, impact.mediumImpactThreshold, _maximumForceLimit);
                _soundConfig.impactSettings = impact;
            }
        }
        #endregion

        #region Event Management
        private void SubscribeToEvents()
        {
            if (_damageDetectors != null)
                foreach (var detector in _damageDetectors)
                    if (detector != null) detector.OnDamageDealt += HandleDamageDealt;
        }

        private void UnsubscribeFromEvents()
        {
            if (_damageDetectors != null)
                foreach (var detector in _damageDetectors)
                    if (detector != null) detector.OnDamageDealt -= HandleDamageDealt;
        }
        #endregion

        #region Hit Processing
        private void HandleDamageDealt(DamageInfo damageInfo)
        {
            if (!IsSystemActive) return;
            float rawForce = damageInfo.calculatedDamage;
            if (rawForce < _minimumForceThreshold) return;
            ProcessHit(Mathf.Clamp(rawForce, 0f, _maximumForceLimit), damageInfo.impactPoint, "Unknown");
        }

        public void ProcessHit(float force, Vector3 impactPoint = default, string detectorId = "Manual")
        {
            if (!IsSystemActive) return;
            force = Mathf.Clamp(force, 0f, _maximumForceLimit);
            
            float effectiveness = CalculateHitEffectiveness(force, detectorId);
            RecognitionHit hit = new RecognitionHit(force, Time.time, impactPoint, detectorId, effectiveness);
            
            AddHitToHistory(hit);
            _totalHitCount++; _lastHitTime = Time.time;
            
            float oldIP = _currentIP;
            CalculateNewIP(hit);
            UpdateProgressBar();
            TriggerHitEffects(hit);
            ProcessHitIntegrated(hit);
            CheckSystemLimits(oldIP);
            
            OnHitProcessed?.Invoke(hit);
        }

        private float CalculateHitEffectiveness(float force, string detectorId)
        {
            float effectiveness = Mathf.Lerp(0.5f, 1f, force / _maximumForceLimit);
            
            if (_lastThreeHits.Count > 0)
            {
                float timeSinceLast = Time.time - _lastThreeHits.Last().timestamp;
                if (timeSinceLast <= _tensionConfig.perfectTimingWindow) effectiveness *= 1.2f;
                else if (timeSinceLast <= _tensionConfig.comboTimeWindow) effectiveness *= 1.1f;
            }
            
            return Mathf.Clamp(effectiveness, 0.1f, 2f);
        }

        private void AddHitToHistory(RecognitionHit hit)
        {
            _hitHistory.Add(hit);
            if (_hitHistory.Count > _maxHistorySize) _hitHistory.RemoveAt(0);
            
            _lastThreeHits.Add(hit);
            if (_lastThreeHits.Count > 3) _lastThreeHits.RemoveAt(0);
            
            _recentHitsQueue.Enqueue(hit);
            if (_recentHitsQueue.Count > 10) _recentHitsQueue.Dequeue();
        }
        #endregion

        #region IP Calculation
        private void CalculateNewIP(RecognitionHit hit)
        {
            float previousIP = _currentIP;
            float baseIncrease = hit.force * _baseForceMultiplier * hit.effectiveness;
            _currentIP = Mathf.Min(_currentIP + baseIncrease, 100f);
            
            ApplyPsychologicalResistance();
            ApplyTensionTriggers(hit);
            
            _currentIP = Mathf.Clamp(_currentIP, 0f, 100f);
            OnIPChanged?.Invoke(_currentIP);
        }

        private void ApplyPsychologicalResistance()
        {
            if (_lastThreeHits.Count < 2) return;
            bool resistanceTriggered = false;
            
            var lastTwo = _lastThreeHits.TakeLast(2).ToList();
            if (lastTwo.Count == 2 && lastTwo.All(h => h.force > _resistanceConfig.strongHitThreshold))
            {
                float reduction = _currentIP * (_resistanceConfig.strongHitsReductionPercent / 100f);
                _currentIP -= reduction; 
                resistanceTriggered = true;
            }
            
            if (_lastThreeHits.Count == 3)
            {
                float sumForce = _lastThreeHits.Sum(h => h.force);
                if (sumForce > _resistanceConfig.threeHitsSumThreshold)
                {
                    float reduction = _currentIP * (_resistanceConfig.sumExceededReductionPercent / 100f);
                    _currentIP -= reduction; 
                    resistanceTriggered = true;
                }
            }
            
            if (resistanceTriggered) 
            { 
                OnResistanceTriggered?.Invoke(); 
                LogDebug("Psychological resistance triggered");
            }
        }

        private void ApplyTensionTriggers(RecognitionHit currentHit)
        {
            if (_lastThreeHits.Count < 3) return;
            bool tensionTriggered = false;
            var forces = _lastThreeHits.Select(h => h.force).ToList();
            
            if (IsIncreasingSequence(forces))
            {
                float bonus = _tensionConfig.increasingSeriesBonus;
                if (IsInPerfectTimingWindow()) 
                { 
                    bonus += _tensionConfig.perfectTimingBonus; 
                    _analytics?.RecordPerfectTiming(); 
                }
                _currentIP += bonus; 
                tensionTriggered = true;
            }
            
            if (currentHit.force < _tensionConfig.weakHitThreshold && _lastThreeHits.Count > 1)
            {
                _currentIP -= _tensionConfig.seriesInterruptionPenalty;
                _analytics?.RecordComboBreak(); 
                tensionTriggered = true;
            }
            
            if (tensionTriggered) 
            { 
                OnTensionTriggered?.Invoke(); 
                LogDebug("Tension trigger activated");
            }
        }

        private bool IsIncreasingSequence(List<float> forces) => forces.Count == 3 && forces[0] < forces[1] && forces[1] < forces[2];
        private bool IsInPerfectTimingWindow()
        {
            if (_lastThreeHits.Count < 2) return false;
            var lastTwo = _lastThreeHits.TakeLast(2).ToList();
            return lastTwo[1].timestamp - lastTwo[0].timestamp <= _tensionConfig.perfectTimingWindow;
        }
        #endregion

        #region Auto Decay & Heartbeat
        private void ProcessAutoDecay()
        {
            if (!_enableAutoDecay || _currentIP <= 0f) return;
            if (_pauseDecayDuringCombos && IsInComboWindow()) return;
            
            float currentTime = Time.time;
            float deltaTime = currentTime - _lastDecayTime;
            _lastDecayTime = currentTime;
            
            float decayAmount = _decayRate * deltaTime;
            float previousIP = _currentIP;
            _currentIP = Mathf.Max(0f, _currentIP - decayAmount);
            
            if (Mathf.Abs(previousIP - _currentIP) > 0.1f) 
            { 
                UpdateProgressBar(); 
                OnIPChanged?.Invoke(_currentIP); 
            }
        }

        private bool IsInComboWindow() => TimeSinceLastHit <= _tensionConfig.comboTimeWindow;

        private void StartHeartbeatSystem()
        {
            StopHeartbeatSystem();
            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
        }

        private void StopHeartbeatSystem()
        {
            if (_heartbeatCoroutine != null) { StopCoroutine(_heartbeatCoroutine); _heartbeatCoroutine = null; }
        }

        private void UpdateHeartRate()
        {
            float targetHeartRate = _heartbeatConfig.baseHeartRate;
            if (_currentIP >= _heartbeatConfig.heartRateThreshold)
                targetHeartRate = Mathf.Lerp(_heartbeatConfig.baseHeartRate, _heartbeatConfig.maxStressHeartRate, StressLevel);
            
            _currentHeartRate = Mathf.Lerp(_currentHeartRate, targetHeartRate, Time.deltaTime * (2f + StressLevel));
            OnHeartRateChanged?.Invoke(_currentHeartRate);
        }

        private IEnumerator HeartbeatCoroutine()
        {
            while (IsSystemActive && _hasRecognitionBar)
            {
                float heartbeatInterval = 60f / _currentHeartRate;
                float pulseIntensity = CalculatePulseIntensity();
                TriggerHeartbeatPulse(pulseIntensity);
                PlayHeartbeatSound();
                yield return new WaitForSeconds(heartbeatInterval);
            }
        }

        private float CalculatePulseIntensity()
        {
            float baseIntensity = _heartbeatConfig.baseIntensity;
            if (_heartbeatConfig.enableDynamicIntensity)
            {
                baseIntensity *= (1f + StressLevel * 0.5f);
                if (IsHitPulseActive) baseIntensity *= _heartbeatConfig.hitPulseIntensity;
            }
            return Mathf.Clamp(baseIntensity, 0.001f, 0.5f);
        }

        private void TriggerHeartbeatPulse(float intensity)
        {
            if (!_hasRecognitionBar) return;
            if (_heartbeatConfig.useSmoothPulse)
            {
                float pulseDuration = 60f / _currentHeartRate * 0.4f;
                _recognitionBar.TriggerSafePulse(intensity, pulseDuration);
            }
            else _recognitionBar.TriggerPulse(intensity);
        }
        #endregion

        #region Hit Effects & Sound
        private void TriggerHitEffects(RecognitionHit hit)
        {
            TriggerHitPulse(hit);
            if (_heartbeatConfig.useSharpHitEffects && _hasRecognitionBar) TriggerSharpEffect(hit);
            
            // Проигрывание звуков ударов и криков
            PlayImpactSound(hit);
            PlayPainSound(hit);
        }

        private void TriggerHitPulse(RecognitionHit hit)
        {
            _hitPulseEndTime = Time.time + _heartbeatConfig.hitPulseDuration;
            if (_hasRecognitionBar)
            {
                float intensity = _heartbeatConfig.hitPulseIntensity * hit.effectiveness;
                float duration = Mathf.Lerp(0.4f, 1.0f, hit.force / _maximumForceLimit);
                _recognitionBar.TriggerSafePulse(intensity, duration);
            }
        }

        private void TriggerSharpEffect(RecognitionHit hit)
        {
            float normalizedForce = hit.force / _maximumForceLimit;
            if (normalizedForce > 0.6f) _recognitionBar.TriggerEffect(VisualEffectType.Punch, 1.0f, 0.3f);
            else if (normalizedForce > 0.3f) _recognitionBar.TriggerEffect(VisualEffectType.Shake, 0.8f, 0.2f);
            else _recognitionBar.TriggerEffect(VisualEffectType.Bounce, 0.5f, 0.15f);
        }

        #region Impact Sound System
        private void PlayImpactSound(RecognitionHit hit)
        {
            if (_soundConfig?.impactAudioSource == null || !_soundConfig.enableImpactSounds) return;
            
            // Проверка шанса проигрывания
            if (UnityEngine.Random.value > _soundConfig.impactChance) return;
            
            // Проверка минимального интервала между ударами
            if (_soundConfig.preventImpactOverlap && 
                Time.time - _lastImpactSoundTime < _soundConfig.minimumImpactInterval) return;
            
            ImpactSoundType soundType = GetImpactSoundType(hit.force);
            AudioClip[] soundArray = GetImpactSoundArray(soundType);
            
            if (soundArray == null || soundArray.Length == 0) return;
            
            AudioClip clip = _soundConfig.impactSettings.enableRandomSelection ? 
                soundArray[UnityEngine.Random.Range(0, soundArray.Length)] : 
                soundArray[0];
            
            if (clip == null) return;
            
            float volume, pitch;
            CalculateImpactSoundParameters(hit, soundType, out volume, out pitch);
            
            // Настройка пространственного звука
            if (_soundConfig.enableSpatialImpactSounds && hit.impactPoint != Vector3.zero)
            {
                _soundConfig.impactAudioSource.transform.position = hit.impactPoint;
            }
            
            _soundConfig.impactAudioSource.pitch = pitch;
            _soundConfig.impactAudioSource.PlayOneShot(clip, volume);
            
            _lastImpactSoundTime = Time.time;
            OnImpactSoundPlayed?.Invoke(soundType, hit.force);
            
            LogDebug($"Impact sound played - Type: {soundType}, Force: {hit.force:F1}, Volume: {volume:F2}, Pitch: {pitch:F2}");
        }

        private ImpactSoundType GetImpactSoundType(float force)
        {
            var settings = _soundConfig.impactSettings;
            
            if (force >= settings.heavyImpactThreshold) return ImpactSoundType.Critical;
            if (force >= settings.mediumImpactThreshold) return ImpactSoundType.Heavy;
            if (force >= settings.lightImpactThreshold) return ImpactSoundType.Medium;
            return ImpactSoundType.Light;
        }

        private AudioClip[] GetImpactSoundArray(ImpactSoundType soundType)
        {
            return soundType switch
            {
                ImpactSoundType.Light => _soundConfig.lightImpactSounds,
                ImpactSoundType.Medium => _soundConfig.mediumImpactSounds,
                ImpactSoundType.Heavy => _soundConfig.heavyImpactSounds,
                ImpactSoundType.Critical => _soundConfig.criticalImpactSounds,
                _ => null
            };
        }

        private void CalculateImpactSoundParameters(RecognitionHit hit, ImpactSoundType soundType, out float volume, out float pitch)
        {
            var settings = _soundConfig.impactSettings;
            
            // Базовые значения в зависимости от типа удара
            volume = soundType switch
            {
                ImpactSoundType.Light => settings.lightImpactVolume,
                ImpactSoundType.Medium => settings.mediumImpactVolume,
                ImpactSoundType.Heavy => settings.heavyImpactVolume,
                ImpactSoundType.Critical => settings.criticalImpactVolume,
                _ => 0.5f
            };
            
            pitch = soundType switch
            {
                ImpactSoundType.Light => settings.lightImpactPitch,
                ImpactSoundType.Medium => settings.mediumImpactPitch,
                ImpactSoundType.Heavy => settings.heavyImpactPitch,
                ImpactSoundType.Critical => settings.criticalImpactPitch,
                _ => 1f
            };
            
            // Динамическая корректировка на основе силы удара
            if (settings.enableForceBasedVolume)
            {
                float forceNormalized = hit.force / _maximumForceLimit;
                volume *= Mathf.Lerp(0.7f, 1.2f, forceNormalized);
            }
            
            if (settings.enableForceBasedPitch)
            {
                float forceNormalized = hit.force / _maximumForceLimit;
                pitch *= Mathf.Lerp(1.1f, 0.9f, forceNormalized); // Более сильные удары = более низкий тон
            }
            
            // Случайные вариации
            if (settings.volumeVariation > 0f)
            {
                float volumeVar = UnityEngine.Random.Range(-settings.volumeVariation, settings.volumeVariation);
                volume *= (1f + volumeVar);
            }
            
            if (settings.pitchVariation > 0f)
            {
                float pitchVar = UnityEngine.Random.Range(-settings.pitchVariation, settings.pitchVariation);
                pitch += pitchVar;
            }
            
            // Комбо-бонусы
            if (settings.enableComboVolumeBoost && IsInComboWindow())
            {
                volume *= (1f + settings.comboVolumeBonus);
            }
            
            // Эффект реверберации для комбо (если поддерживается)
            if (settings.enableComboReverb && IsInComboWindow())
            {
                // Можно добавить эффект через AudioMixerGroup или другие способы
                // Здесь оставляем заготовку для будущей реализации
            }
            
            // Применение общих настроек громкости и стресса
            volume *= _soundConfig.impactVolume * _soundConfig.masterVolume;
            
            if (_soundConfig.enableDynamicVolume)
                volume *= _soundConfig.stressVolumeCurve.Evaluate(StressLevel);
            
            if (_soundConfig.enableDynamicPitch)
                pitch *= _soundConfig.stressPitchCurve.Evaluate(StressLevel);
            
            // Ограничение значений
            volume = Mathf.Clamp01(volume);
            pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        }
        #endregion

        private void PlayHeartbeatSound()
        {
            if (_soundConfig?.heartbeatAudioSource == null || _soundConfig.heartbeatSounds?.Length == 0) return;
            var clip = _soundConfig.heartbeatSounds[UnityEngine.Random.Range(0, _soundConfig.heartbeatSounds.Length)];
            float volume = _soundConfig.heartbeatVolume * _soundConfig.masterVolume;
            float pitch = 1f;
            if (_soundConfig.enableDynamicVolume) volume *= _soundConfig.stressVolumeCurve.Evaluate(StressLevel);
            if (_soundConfig.enableDynamicPitch) pitch = _soundConfig.stressPitchCurve.Evaluate(StressLevel);
            _soundConfig.heartbeatAudioSource.volume = volume; 
            _soundConfig.heartbeatAudioSource.pitch = pitch;
            _soundConfig.heartbeatAudioSource.PlayOneShot(clip);
        }

        private void PlayRecognitionAchievedSound()
        {
            if (_soundConfig?.heartbeatAudioSource == null || _soundConfig.recognitionAchievedSound == null) 
                return;

            float volume = _soundConfig.recognitionAchievedVolume * _soundConfig.masterVolume;
            float pitch = _soundConfig.recognitionAchievedPitch;

            // Применяем случайную вариацию питча
            if (_soundConfig.recognitionPitchVariation > 0f)
            {
                float pitchVar = UnityEngine.Random.Range(-_soundConfig.recognitionPitchVariation, _soundConfig.recognitionPitchVariation);
                pitch += pitchVar;
            }

            // Применяем влияние стресса на питч (если включено)
            if (_soundConfig.enableRecognitionPitchStress)
            {
                pitch *= _soundConfig.stressPitchCurve.Evaluate(StressLevel);
            }

            // Применяем динамическую громкость
            if (_soundConfig.enableDynamicVolume)
            {
                volume *= _soundConfig.stressVolumeCurve.Evaluate(StressLevel);
            }

            // Ограничиваем значения
            pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            volume = Mathf.Clamp01(volume);

            // Проигрываем звук
            _soundConfig.heartbeatAudioSource.pitch = pitch;
            _soundConfig.heartbeatAudioSource.PlayOneShot(_soundConfig.recognitionAchievedSound, volume);

            LogDebug($"Recognition achieved sound played - Volume: {volume:F2}, Pitch: {pitch:F2}, Stress: {StressLevel:F2}");
        }

        private void PlayPainSound(RecognitionHit hit)
        {
            if (_soundConfig?.painAudioSource == null || 
                _soundConfig.painSounds?.Length == 0 || 
                !_soundConfig.enablePainSounds) return;

            // Случайный шанс проигрывания крика
            if (UnityEngine.Random.value > _soundConfig.painChance) return;

            var clip = _soundConfig.painSounds[UnityEngine.Random.Range(0, _soundConfig.painSounds.Length)];
            float volume = _soundConfig.painVolume * _soundConfig.masterVolume;
            float pitch = 1f;

            // Увеличиваем громкость и изменяем высоту звука в зависимости от силы удара
            float forceNormalized = hit.force / _maximumForceLimit;
            volume *= Mathf.Lerp(0.6f, 1f, forceNormalized);
            
            // Случайная вариация высоты звука + зависимость от силы удара
            float pitchVariation = UnityEngine.Random.Range(-_soundConfig.painPitchVariation, _soundConfig.painPitchVariation);
            pitch += pitchVariation;
            pitch = Mathf.Lerp(pitch, pitch * 0.8f, forceNormalized); // Более низкий звук при сильных ударах

            // Учитываем уровень стресса
            if (_soundConfig.enableDynamicVolume) 
                volume *= _soundConfig.stressVolumeCurve.Evaluate(StressLevel);
            if (_soundConfig.enableDynamicPitch) 
                pitch *= _soundConfig.stressPitchCurve.Evaluate(StressLevel);

            _soundConfig.painAudioSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            _soundConfig.painAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));

            LogDebug($"Pain sound played - Force: {hit.force:F1}, Volume: {volume:F2}, Pitch: {pitch:F2}");
        }
        #endregion

        #region System Updates & State Machine
        private void UpdateAllSystems()
        {
            if (!IsSystemActive) return;
            try
            {
                ProcessAutoDecay(); 
                UpdateHeartRate(); 
                UpdateState();
                _performanceMonitor?.Update(); 
                CleanupOldData();
                if (Time.frameCount % 300 == 0) ValidateSystemIntegrity();
                _framesSinceLastUpdate++;
            }
            catch (System.Exception e) { LogError($"Error in UpdateAllSystems: {e.Message}"); }
        }

        private void ProcessHitIntegrated(RecognitionHit hit)
        {
            try
            {
                _analytics?.RecordHit(hit); 
                _analytics?.RecordIPChange(_currentIP);
                _performanceMonitor?.RecordHit();
                UpdateState();
            }
            catch (System.Exception e) { LogError($"Error in ProcessHitIntegrated: {e.Message}"); }
        }

        private void UpdateState()
        {
            RecognitionState newState = DetermineState();
            if (newState != _currentState)
            {
                _previousState = _currentState; 
                _currentState = newState;
                OnStateChanged?.Invoke(_previousState, _currentState);
                OnStateEnter(_currentState); 
                OnStateExit(_previousState);
            }
        }

        private RecognitionState DetermineState()
        {
            if (_currentIP >= 100f) return RecognitionState.Achieved;
            if (_currentIP == 0f) return RecognitionState.Idle;
            if (_currentIP >= 80f) return RecognitionState.Critical;
            
            bool hasRecentResistance = _lastThreeHits.Count >= 2 && 
                Time.time - _lastThreeHits.Last().timestamp < 2f &&
                _lastThreeHits.TakeLast(2).All(h => h.force > _resistanceConfig.strongHitThreshold);
            if (hasRecentResistance) return RecognitionState.Resistance;
            
            bool isInTension = IsInComboWindow() && _lastThreeHits.Count >= 2;
            if (isInTension) return RecognitionState.Tension;
            return RecognitionState.Building;
        }

        private void OnStateEnter(RecognitionState state)
        {
            switch (state)
            {
                case RecognitionState.Achieved:
                    PlayRecognitionAchievedSound();
                    ShowCompletionSplash();
                    LogDebug("Recognition Achieved!");
                    break;
                case RecognitionState.Critical:
                    LogDebug("Critical state entered");
                    break;
            }
        }

        private void ShowCompletionSplash()
        {
            if (_splashController != null)
            {
                _splashController.ShowCompletionSplash();
                LogDebug("Completion splash shown via SplashController");
            }
            else
            {
                LogWarning("SplashController not assigned - cannot show completion splash");
            }
        }

        private void OnStateExit(RecognitionState state)
        {
            // Можно добавить дополнительную логику при выходе из состояний
        }
        #endregion

        #region System Checks & UI
        private void CheckSystemLimits(float oldIP)
        {
            if (oldIP < 100f && _currentIP >= 100f) 
            { 
                OnRecognitionAchieved?.Invoke(); 
                LogDebug("RECOGNITION ACHIEVED! IP = 100"); 
            }
            if (_totalHitCount >= _maxHitsWithoutSuccess && _currentIP < 100f)
                ResetSystem($"Hit limit reached ({_maxHitsWithoutSuccess})", _resetIPValue);
        }

        private void CleanupOldData()
        {
            float cutoffTime = Time.time - _sequenceAnalysisTime;
            _hitHistory.RemoveAll(hit => hit.timestamp < cutoffTime || !hit.IsValid);
            while (_recentHitsQueue.Count > 0 && _recentHitsQueue.Peek().Age > _tensionConfig.comboTimeWindow)
                _recentHitsQueue.Dequeue();
        }

        private void UpdateProgressBar()
        {
            if (_hasRecognitionBar) _recognitionBar.SetRawValue(_currentIP, true, true, false);
        }

        private RecognitionStats CalculateStatistics()
        {
            return new RecognitionStats
            {
                currentIP = _currentIP, totalHits = _totalHitCount, effectiveHits = EffectiveHitCount,
                averageHitForce = _hitHistory.Count > 0 ? _hitHistory.Average(h => h.force) : 0f,
                timeSinceLastHit = TimeSinceLastHit,
                currentHeartRate = _currentHeartRate, stressLevel = StressLevel,
                consecutiveFailures = _consecutiveFailures
            };
        }
        #endregion

        #region Public API
        public void ResetSystem(string reason = "Manual reset", float newIP = 0f)
        {
            _currentIP = newIP; _totalHitCount = 0; _lastHitTime = Time.time;
            _hitHistory.Clear(); _lastThreeHits.Clear(); _recentHitsQueue.Clear();
            _currentHeartRate = _heartbeatConfig.baseHeartRate; _hitPulseEndTime = 0f;
            _consecutiveFailures = 0;
            _lastDecayTime = Time.time;
            _lastImpactSoundTime = 0f; // Сброс времени последнего звука удара
            
            UpdateProgressBar();
            OnIPChanged?.Invoke(_currentIP); 
            OnHeartRateChanged?.Invoke(_currentHeartRate); 
            OnIPReset?.Invoke(reason);
        }

        public void SetIP(float ip)
        {
            float oldIP = _currentIP; 
            _currentIP = Mathf.Clamp(ip, 0f, 100f);
            UpdateProgressBar(); 
            OnIPChanged?.Invoke(_currentIP);
        }

        public void SetDamageDetectors(HandMovementDamageDetector[] detectors)
        {
            UnsubscribeFromEvents(); 
            _damageDetectors = detectors; 
            _detectorCache.Clear();
            if (detectors != null)
                foreach (var detector in detectors)
                    if (detector != null) _detectorCache[detector.name] = detector;
            SubscribeToEvents();
        }

        public void UpdateResistanceConfig(PsychologicalResistanceConfig config) 
        { 
            _resistanceConfig = config; 
            ValidateConfiguration(); 
        }
        
        public void UpdateTensionConfig(TensionTriggersConfig config) 
        { 
            _tensionConfig = config; 
            ValidateConfiguration(); 
        }
        
        public void UpdateHeartbeatConfig(HeartbeatSystemConfig config) 
        { 
            _heartbeatConfig = config; 
            ValidateConfiguration(); 
        }

        // Новые методы для работы со звуками ударов
        public void UpdateImpactSoundSettings(ImpactSoundSettings settings)
        {
            if (_soundConfig != null)
            {
                _soundConfig.impactSettings = settings;
                ValidateConfiguration();
            }
        }

        public void TestImpactSound(ImpactSoundType soundType, float force = 50f)
        {
            var testHit = new RecognitionHit(force, Time.time, Vector3.zero, "Test", 1f);
            PlayImpactSound(testHit);
        }

        public void SetImpactSoundsEnabled(bool enabled)
        {
            if (_soundConfig != null)
                _soundConfig.enableImpactSounds = enabled;
        }

        // Новые методы для звука достижения
        public void SetRecognitionAchievedPitch(float pitch)
        {
            if (_soundConfig != null)
                _soundConfig.recognitionAchievedPitch = Mathf.Clamp(pitch, 0.5f, 2f);
        }

        public void SetRecognitionAchievedVolume(float volume)
        {
            if (_soundConfig != null)
                _soundConfig.recognitionAchievedVolume = Mathf.Clamp01(volume);
        }

        public void TestRecognitionAchievedSound()
        {
            PlayRecognitionAchievedSound();
        }

        public void SetSplashController(BatmanBreakTheSilence.UI.SplashController splashController)
        {
            _splashController = splashController;
        }

        public string GetSystemInfo()
        {
            var stats = Statistics;
            return $"IP: {stats.currentIP:F1}% | Hits: {stats.effectiveHits}/{stats.totalHits} | " +
                   $"Heart: {stats.currentHeartRate:F0}bpm | " +
                   $"Detectors: {_damageDetectors?.Length ?? 0} | State: {_currentState}";
        }

        public string GetAnalyticsReport() => _analytics?.ToString() ?? "No analytics data";
        public void RunDiagnostics() => Debug.Log($"=== RECOGNITION SYSTEM ===\n{GetSystemInfo()}");
        #endregion

        #region Data Validation & Error Handling
        private bool ValidateSystemIntegrity()
        {
            bool isValid = true;
            if (_currentIP < 0f || _currentIP > 100f) { _currentIP = Mathf.Clamp(_currentIP, 0f, 100f); isValid = false; }
            if (_totalHitCount < 0) { _totalHitCount = 0; isValid = false; }
            if (_hitHistory == null) { _hitHistory = new List<RecognitionHit>(); isValid = false; }
            if (_lastThreeHits == null) { _lastThreeHits = new List<RecognitionHit>(); isValid = false; }
            return isValid;
        }
        #endregion

        #region Debug Interface
        private void DrawDebugInterface()
        {
            var stats = Statistics; 
            float y = 50f;
            GUI.Box(new Rect(10, y, 650, 380), "Recognition System Debug"); y += 25f;
            GUI.Label(new Rect(20, y, 580, 20), $"IP: {stats.currentIP:F1}% | Hits: {stats.effectiveHits}/{stats.totalHits} | State: {_currentState}"); y += 20f;
            GUI.Label(new Rect(20, y, 580, 20), $"Heart: {stats.currentHeartRate:F0}bpm | Stress: {stats.stressLevel:P0}"); y += 20f;
            GUI.Label(new Rect(20, y, 580, 20), $"Auto Decay: {(_enableAutoDecay ? $"{_decayRate:F1} IP/s" : "OFF")} | Time Since Hit: {stats.timeSinceLastHit:F1}s"); y += 30f;
            
            // Основные тестовые кнопки
            if (GUI.Button(new Rect(20, y, 80, 25), "Test Hit")) ProcessHit(75f, Vector3.zero, "Debug");
            if (GUI.Button(new Rect(110, y, 80, 25), "Reset")) ResetSystem("Debug reset");
            if (GUI.Button(new Rect(200, y, 80, 25), "Add +10 IP")) SetIP(_currentIP + 10f);
            if (GUI.Button(new Rect(290, y, 80, 25), "Pulse Test")) if (_hasRecognitionBar) _recognitionBar.TriggerSafePulse();
            if (GUI.Button(new Rect(380, y, 80, 25), "Pain Sound")) 
            {
                var testHit = new RecognitionHit(60f, Time.time, Vector3.zero, "Debug", 1f);
                PlayPainSound(testHit);
            }
            if (GUI.Button(new Rect(470, y, 100, 25), "Achievement")) TestRecognitionAchievedSound();
            
            y += 35f;
            
            // Тестовые кнопки для звуков ударов
            GUI.Label(new Rect(20, y, 580, 20), "Impact Sound Tests:"); y += 20f;
            if (GUI.Button(new Rect(20, y, 70, 25), "Light")) TestImpactSound(ImpactSoundType.Light, 20f);
            if (GUI.Button(new Rect(100, y, 70, 25), "Medium")) TestImpactSound(ImpactSoundType.Medium, 40f);
            if (GUI.Button(new Rect(180, y, 70, 25), "Heavy")) TestImpactSound(ImpactSoundType.Heavy, 70f);
            if (GUI.Button(new Rect(260, y, 70, 25), "Critical")) TestImpactSound(ImpactSoundType.Critical, 95f);
            
            // Переключатель звуков ударов
            bool impactEnabled = _soundConfig?.enableImpactSounds ?? false;
            bool newImpactEnabled = GUI.Toggle(new Rect(350, y, 120, 25), impactEnabled, "Impact Sounds");
            if (newImpactEnabled != impactEnabled) SetImpactSoundsEnabled(newImpactEnabled);
            
            y += 35f;
            
            // Тест Completion Splash
            GUI.Label(new Rect(20, y, 580, 20), "UI Tests:"); y += 20f;
            if (GUI.Button(new Rect(20, y, 120, 25), "Show Completion")) ShowCompletionSplash();
            if (GUI.Button(new Rect(150, y, 120, 25), "Force Achievement")) 
            {
                SetIP(100f);
                // Trigger achievement manually for testing
                OnRecognitionAchieved?.Invoke();
            }
            
            // Информация о SplashController
            string splashStatus = _splashController != null ? "Connected" : "Not Assigned";
            GUI.Label(new Rect(280, y, 200, 25), $"SplashController: {splashStatus}");
            
            y += 35f;
            
            if (_lastThreeHits.Count > 0)
            {
                GUI.Label(new Rect(20, y, 580, 20), "Recent Hits:"); y += 20f;
                for (int i = 0; i < _lastThreeHits.Count; i++)
                {
                    var hit = _lastThreeHits[i];
                    ImpactSoundType soundType = GetImpactSoundType(hit.force);
                    GUI.Label(new Rect(20, y, 580, 20), $"{i + 1}. Force: {hit.force:F1} ({soundType}) | Eff: {hit.effectiveness:F2} | Age: {hit.Age:F1}s"); y += 20f;
                }
            }
            
            // Информация о звуковых настройках
            y += 10f;
            if (_soundConfig != null)
            {
                GUI.Label(new Rect(20, y, 580, 20), $"Impact Volume: {_soundConfig.impactVolume:F2} | Last Impact: {(Time.time - _lastImpactSoundTime):F1}s ago"); y += 20f;
                GUI.Label(new Rect(20, y, 580, 20), $"Achievement Pitch: {_soundConfig.recognitionAchievedPitch:F2} | Volume: {_soundConfig.recognitionAchievedVolume:F2}"); y += 20f;
            }
        }
        #endregion

        #region Cleanup & Serialization
        private void CleanupAllSystems()
        {
            StopHeartbeatSystem(); 
            UnsubscribeFromEvents();
            _hitHistory?.Clear(); 
            _lastThreeHits?.Clear(); 
            _recentHitsQueue?.Clear(); 
            _detectorCache?.Clear();
            
            // Очистка событий
            OnIPChanged = null;
            OnRecognitionAchieved = null;
            OnIPReset = null;
            OnHeartRateChanged = null;
            OnHitProcessed = null;
            OnResistanceTriggered = null;
            OnTensionTriggered = null;
            OnStateChanged = null;
            OnImpactSoundPlayed = null;
        }

        public void OnBeforeSerialize() => ValidateConfiguration();
        
        public void OnAfterDeserialize()
        {
            if (_hitHistory == null) _hitHistory = new List<RecognitionHit>();
            if (_lastThreeHits == null) _lastThreeHits = new List<RecognitionHit>();
            if (_recentHitsQueue == null) _recentHitsQueue = new Queue<RecognitionHit>();
            if (_detectorCache == null) _detectorCache = new Dictionary<string, HandMovementDamageDetector>();
            if (_performanceMonitor == null) _performanceMonitor = new PerformanceMonitor();
            if (_analytics == null) 
            {
                _analytics = new GameplayAnalytics();
            }
            // Не используем Time.time во время сериализации - инициализируем в OnEnable
            _lastDecayTime = 0f;
            _lastImpactSoundTime = 0f;
        }
        #endregion

        #region Logging
        private void LogDebug(string message) 
        { 
            if (_enableLogging) Debug.Log($"[RecognitionSystem-{name}] {message}", this); 
        }
        
        private void LogWarning(string message) => Debug.LogWarning($"[RecognitionSystem-{name}] {message}", this);
        private void LogError(string message) => Debug.LogError($"[RecognitionSystem-{name}] {message}", this);
        #endregion

        #region Editor Utilities
#if UNITY_EDITOR
        [ContextMenu("Test Weak Hit")] private void TestWeakHit() => ProcessHit(25f, Vector3.zero, "Editor");
        [ContextMenu("Test Strong Hit")] private void TestStrongHit() => ProcessHit(85f, Vector3.zero, "Editor");
        [ContextMenu("Test Light Impact Sound")] private void TestLightImpact() => TestImpactSound(ImpactSoundType.Light, 20f);
        [ContextMenu("Test Heavy Impact Sound")] private void TestHeavyImpact() => TestImpactSound(ImpactSoundType.Heavy, 80f);
        [ContextMenu("Test Recognition Achieved Sound")] private void TestAchievementSound() => TestRecognitionAchievedSound();
        [ContextMenu("Force Recognition Achievement")] private void ForceAchievement() { SetIP(100f); OnRecognitionAchieved?.Invoke(); }
        [ContextMenu("Show Completion Splash")] private void TestCompletionSplash() => ShowCompletionSplash();
        [ContextMenu("Simulate High Stress")] private void SimulateHighStress() => SetIP(85f);
        [ContextMenu("Reset All")] private void ResetAll() { ResetSystem("Editor reset"); _consecutiveFailures = 0; }
#endif
        #endregion
    }

    #region Extension Methods
    public static class RecognitionSystemExtensions
    {
        public static float GetIPPercentage(this RecognitionSystem system) => system.CurrentIP;
        public static bool IsNearCompletion(this RecognitionSystem system, float threshold = 80f) => system.CurrentIP >= threshold;
        
        public static string GetStressCategory(this RecognitionSystem system) => system.StressLevel switch
        {
            < 0.3f => "Low", < 0.7f => "Medium", _ => "High"
        };
        
        public static float GetRecentEffectiveness(this RecognitionSystem system)
        {
            var recentHits = system.RecentHits.TakeLast(5);
            return recentHits.Any() ? recentHits.Average(h => h.effectiveness) : 0f;
        }
        
        public static bool IsInComboMode(this RecognitionSystem system) => system.TimeSinceLastHit <= 3f && system.RecentHits.Count >= 2;
        
        public static string GetPlayerFeedback(this RecognitionSystem system)
        {
            float effectiveness = system.GetRecentEffectiveness();
            if (effectiveness < 0.3f) return "Try hitting harder and more accurately";
            if (system.StressLevel > 0.8f && system.TimeSinceLastHit > 5f) return "Strike quickly to maintain pressure";
            if (system.IsInComboMode()) return "Great combo! Keep the rhythm";
            if (system.IsNearCompletion()) return "Almost there! One more strong hit";
            return "Good work, keep applying pressure";
        }
        
        public static string GetSessionQuality(this RecognitionSystem system)
        {
            var stats = system.Statistics;
            if (stats.effectiveHits > 5 && stats.effectiveHits > stats.totalHits * 0.8f) return "Excellent";
            if (stats.effectiveHits > 3 && stats.effectiveHits > stats.totalHits * 0.6f) return "Good";
            if (stats.effectiveHits > stats.totalHits * 0.4f) return "Average";
            return stats.totalHits > 0 ? "Poor" : "No Data";
        }
        
        public static string GetThreatLevel(this RecognitionSystem system) => system.CurrentIP switch
        {
            < 20f => "Minimal", < 40f => "Low", < 60f => "Moderate", < 80f => "High", < 95f => "Critical", _ => "Extreme"
        };
        
        public static string GetAIAction(this RecognitionSystem system) => system.CurrentState switch
        {
            RecognitionState.Idle => "Wait", 
            RecognitionState.Building => "Resist", 
            RecognitionState.Resistance => "Counter",
            RecognitionState.Tension => "Defend", 
            RecognitionState.Critical => "Panic", 
            RecognitionState.Achieved => "Submit", 
            _ => "Observe"
        };
        
        // Новые методы расширения для работы со звуками ударов
        public static string GetImpactDescription(this RecognitionSystem system, float force)
        {
            var settings = system.ImpactSettings;
            if (force >= settings.heavyImpactThreshold) return "Devastating Impact";
            if (force >= settings.mediumImpactThreshold) return "Heavy Impact";
            if (force >= settings.lightImpactThreshold) return "Solid Impact";
            return "Light Impact";
        }
        
        public static float GetImpactIntensity(this RecognitionSystem system, float force)
        {
            return Mathf.Clamp01(force / 100f);
        }
        
        public static bool ShouldPlayImpactSound(this RecognitionSystem system, float force)
        {
            if (!system.ImpactSoundsEnabled) return false;
            return force >= system.ImpactSettings.lightImpactThreshold;
        }
    }

    public static class MathExtensions
    {
        public static float Variance(this IEnumerable<float> values)
        {
            var valueArray = values.ToArray();
            if (valueArray.Length == 0) return 0f;
            float mean = valueArray.Average();
            return valueArray.Sum(x => Mathf.Pow(x - mean, 2f)) / valueArray.Length;
        }
        
        public static float StandardDeviation(this IEnumerable<float> values) => Mathf.Sqrt(values.Variance());
    }
    #endregion

    #region Utility Classes
    public static class RecognitionUtilities
    {
        public static float ForceToEffectivenessPercent(float force, float maxForce) => Mathf.Clamp01(force / maxForce) * 100f;
        
        public static float CalculateRecommendedForce(IEnumerable<RecognitionHit> hitHistory, float currentIP)
        {
            if (!hitHistory.Any()) return 50f;
            var recentHits = hitHistory.TakeLast(3);
            float averageForce = recentHits.Average(h => h.force);
            float averageEffectiveness = recentHits.Average(h => h.effectiveness);
            
            if (averageEffectiveness < 0.5f) return Mathf.Min(averageForce * 1.3f, 100f);
            if (currentIP > 80f) return Mathf.Max(averageForce * 0.8f, 30f);
            return averageForce;
        }
        
        public static string GenerateSessionSummary(RecognitionSystem system)
        {
            var stats = system.Statistics;
            return $"Session: {system.GetSessionQuality()} | Pace: {system.TimeSinceLastHit switch { > 10f => "Idle", > 5f => "Slow", > 2f => "Moderate", _ => "Fast" }} | " +
                   $"Threat: {system.GetThreatLevel()}\nIP: {stats.currentIP:F0}% | " +
                   $"Performance: {stats.effectiveHits}/{stats.totalHits} effective hits";
        }
        
        public static PsychologicalResistanceConfig CreateOptimalConfig(float difficultyLevel)
        {
            difficultyLevel = Mathf.Clamp01(difficultyLevel);
            return new PsychologicalResistanceConfig
            {
                strongHitThreshold = Mathf.Lerp(50f, 80f, difficultyLevel),
                strongHitsReductionPercent = Mathf.Lerp(10f, 25f, difficultyLevel),
                threeHitsSumThreshold = Mathf.Lerp(120f, 200f, difficultyLevel),
                sumExceededReductionPercent = Mathf.Lerp(15f, 30f, difficultyLevel)
            };
        }
        
        // Новые утилиты для звуков ударов
        public static ImpactSoundSettings CreateBalancedImpactSettings(float gameplayIntensity = 0.5f)
        {
            gameplayIntensity = Mathf.Clamp01(gameplayIntensity);
            
            return new ImpactSoundSettings
            {
                lightImpactThreshold = Mathf.Lerp(15f, 35f, gameplayIntensity),
                mediumImpactThreshold = Mathf.Lerp(35f, 60f, gameplayIntensity),
                heavyImpactThreshold = Mathf.Lerp(60f, 85f, gameplayIntensity),
                
                lightImpactVolume = Mathf.Lerp(0.3f, 0.5f, gameplayIntensity),
                mediumImpactVolume = Mathf.Lerp(0.5f, 0.7f, gameplayIntensity),
                heavyImpactVolume = Mathf.Lerp(0.7f, 0.9f, gameplayIntensity),
                criticalImpactVolume = Mathf.Lerp(0.8f, 1f, gameplayIntensity),
                
                lightImpactPitch = 1.2f,
                mediumImpactPitch = 1f,
                heavyImpactPitch = 0.8f,
                criticalImpactPitch = 0.6f,
                
                pitchVariation = 0.2f,
                volumeVariation = 0.1f,
                
                enableForceBasedPitch = true,
                enableForceBasedVolume = true,
                enableRandomSelection = true,
                
                enableComboVolumeBoost = gameplayIntensity > 0.3f,
                comboVolumeBonus = Mathf.Lerp(0.1f, 0.3f, gameplayIntensity),
                enableComboReverb = gameplayIntensity > 0.7f,
                comboReverbLevel = 0.3f
            };
        }
        
        public static string GetImpactSoundTypeDescription(ImpactSoundType soundType) => soundType switch
        {
            ImpactSoundType.Light => "Soft thud, quick contact",
            ImpactSoundType.Medium => "Solid impact, moderate force",
            ImpactSoundType.Heavy => "Powerful strike, strong contact",
            ImpactSoundType.Critical => "Devastating blow, maximum force",
            _ => "Unknown impact type"
        };
        
        public static Color GetImpactSoundTypeColor(ImpactSoundType soundType) => soundType switch
        {
            ImpactSoundType.Light => Color.green,
            ImpactSoundType.Medium => Color.yellow,
            ImpactSoundType.Heavy => new Color(1f, 0.5f, 0f), // Orange
            ImpactSoundType.Critical => Color.red,
            _ => Color.white
        };
        
        public static float GetRecommendedImpactVolume(float currentStress, float baseVolume)
        {
            // Увеличиваем громкость звуков ударов при высоком стрессе
            float stressMultiplier = Mathf.Lerp(0.8f, 1.2f, currentStress);
            return Mathf.Clamp01(baseVolume * stressMultiplier);
        }
        
        public static bool ShouldTriggerComboEffect(List<RecognitionHit> recentHits, float comboTimeWindow)
        {
            if (recentHits.Count < 2) return false;
            
            float timeBetweenHits = recentHits.Last().timestamp - recentHits[recentHits.Count - 2].timestamp;
            return timeBetweenHits <= comboTimeWindow;
        }
    }
    
    #region Impact Sound Manager
    [System.Serializable]
    public class ImpactSoundManager
    {
        private Dictionary<ImpactSoundType, Queue<float>> _recentPlayTimes;
        private const float SOUND_COOLDOWN = 0.1f;
        
        public ImpactSoundManager()
        {
            _recentPlayTimes = new Dictionary<ImpactSoundType, Queue<float>>();
            foreach (ImpactSoundType type in System.Enum.GetValues(typeof(ImpactSoundType)))
            {
                _recentPlayTimes[type] = new Queue<float>();
            }
        }
        
        public bool CanPlaySound(ImpactSoundType soundType)
        {
            var queue = _recentPlayTimes[soundType];
            
            // Очищаем старые записи
            while (queue.Count > 0 && Time.time - queue.Peek() > SOUND_COOLDOWN)
            {
                queue.Dequeue();
            }
            
            // Проверяем, можно ли проигрывать звук
            return queue.Count == 0;
        }
        
        public void RegisterSoundPlayed(ImpactSoundType soundType)
        {
            _recentPlayTimes[soundType].Enqueue(Time.time);
        }
        
        public float GetTimeSinceLastSound(ImpactSoundType soundType)
        {
            var queue = _recentPlayTimes[soundType];
            return queue.Count > 0 ? Time.time - queue.Peek() : float.MaxValue;
        }
    }
    #endregion
    
    #endregion
}