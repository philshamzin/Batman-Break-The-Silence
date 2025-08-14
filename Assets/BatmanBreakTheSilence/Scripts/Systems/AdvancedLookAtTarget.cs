using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Локальные оси объекта для направления взгляда
    /// </summary>
    public enum LookAxis
    {
        /// <summary>
        /// Локальная ось +Z (вперед)
        /// </summary>
        Forward,
        
        /// <summary>
        /// Локальная ось -Z (назад)
        /// </summary>
        Back,
        
        /// <summary>
        /// Локальная ось +Y (вверх)
        /// </summary>
        Up,
        
        /// <summary>
        /// Локальная ось -Y (вниз)
        /// </summary>
        Down,
        
        /// <summary>
        /// Локальная ось +X (вправо)
        /// </summary>
        Right,
        
        /// <summary>
        /// Локальная ось -X (влево)
        /// </summary>
        Left
    }

    /// <summary>
    /// Оси для копирования поворота от цели
    /// </summary>
    public enum CopyRotationAxis
    {
        /// <summary>
        /// Не копировать поворот
        /// </summary>
        None,
        
        /// <summary>
        /// Копировать поворот по глобальной оси X
        /// </summary>
        X,
        
        /// <summary>
        /// Копировать поворот по глобальной оси Y
        /// </summary>
        Y,
        
        /// <summary>
        /// Копировать поворот по глобальной оси Z
        /// </summary>
        Z
    }

    /// <summary>
    /// Типы ограничений поворота
    /// </summary>
    public enum RotationConstraint
    {
        /// <summary>
        /// Без ограничений поворота
        /// </summary>
        None,
        
        /// <summary>
        /// Ограничение только по вертикали (pitch)
        /// </summary>
        Vertical,
        
        /// <summary>
        /// Ограничение только по горизонтали (yaw)
        /// </summary>
        Horizontal,
        
        /// <summary>
        /// Ограничение по обеим осям
        /// </summary>
        Both
    }

    /// <summary>
    /// Продвинутая система слежения за целью с настраиваемыми осями, ограничениями поворота и сглаживанием.
    /// Обеспечивает точное направление объектов на цели в Batman: Break The Silence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdvancedLookAtTarget : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Основные настройки")]
        [SerializeField, Tooltip("Включить/выключить систему слежения за целью")]
        private bool _isLookingEnabled = true;

        [SerializeField, Tooltip("Целевой объект для слежения")]
        private Transform _target;

        [SerializeField, Tooltip("Скорость поворота для плавного слежения"), Range(0.1f, 300f)]
        private float _rotationSpeed = 5f;

        [SerializeField, Tooltip("Локальная ось объекта, которая будет направлена на цель")]
        private LookAxis _lookAxis = LookAxis.Forward;

        [Header("Настройки копирования поворота")]
        [SerializeField, Tooltip("Ось для копирования поворота от цели")]
        private CopyRotationAxis _copyRotationAxis = CopyRotationAxis.None;

        [SerializeField, Tooltip("Мультипликатор чувствительности копирования поворота"), Range(0f, 5f)]
        private float _rotationCopyMultiplier = 0.5f;

        [Header("Ограничения и фильтрация")]
        [SerializeField, Tooltip("Минимальный порог изменения поворота для игнорирования мелких движений"), Range(0.001f, 1f)]
        private float _minRotationThreshold = 0.01f;

        [SerializeField, Tooltip("Минимальное расстояние до цели для активации слежения"), Range(0.01f, 10f)]
        private float _minTargetDistance = 0.1f;

        [SerializeField, Tooltip("Максимальное расстояние для слежения (0 = без ограничений)"), Min(0f)]
        private float _maxTargetDistance = 0f;

        [SerializeField, Tooltip("Тип ограничений поворота")]
        private RotationConstraint _rotationConstraint = RotationConstraint.None;

        [SerializeField, Tooltip("Минимальный угол по вертикали (градусы)"), Range(-90f, 90f)]
        private float _minVerticalAngle = -90f;

        [SerializeField, Tooltip("Максимальный угол по вертикали (градусы)"), Range(-90f, 90f)]
        private float _maxVerticalAngle = 90f;

        [SerializeField, Tooltip("Минимальный угол по горизонтали (градусы)"), Range(-180f, 180f)]
        private float _minHorizontalAngle = -180f;

        [SerializeField, Tooltip("Максимальный угол по горизонтали (градусы)"), Range(-180f, 180f)]
        private float _maxHorizontalAngle = 180f;

        [Header("Настройки сглаживания")]
        [SerializeField, Tooltip("Использовать адаптивную скорость в зависимости от расстояния")]
        private bool _useAdaptiveSpeed = false;

        [SerializeField, Tooltip("Кривая скорости в зависимости от расстояния (0-1)")]
        private AnimationCurve _speedDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

        [SerializeField, Tooltip("Максимальное расстояние для кривой скорости"), Min(1f)]
        private float _maxSpeedDistance = 10f;

        [SerializeField, Tooltip("Использовать инерцию при остановке слежения")]
        private bool _useInertia = true;

        [SerializeField, Tooltip("Время затухания инерции"), Range(0.1f, 3f)]
        private float _inertiaDamping = 1f;

        [Header("Продвинутые настройки")]
        [SerializeField, Tooltip("Частота обновления системы слежения"), Range(30f, 120f)]
        private float _updateFrequency = 60f;

        [SerializeField, Tooltip("Использовать предсказание движения цели")]
        private bool _usePrediction = false;

        [SerializeField, Tooltip("Время предсказания движения цели"), Range(0.1f, 2f)]
        private float _predictionTime = 0.5f;

        [SerializeField, Tooltip("Плавность предсказания"), Range(0.1f, 1f)]
        private float _predictionSmoothing = 0.8f;

        [SerializeField, Tooltip("Автоматически находить ближайшую цель с указанным тегом")]
        private bool _autoFindTarget = false;

        [SerializeField, Tooltip("Тег для автоматического поиска цели")]
        private string _targetTag = "Player";

        [SerializeField, Tooltip("Интервал поиска новой цели (секунды)"), Range(0.5f, 10f)]
        private float _targetSearchInterval = 2f;

        [Header("События и отладка")]
        [SerializeField, Tooltip("Показывать визуальные гизмосы для отладки")]
        private bool _showGizmos = true;

        [SerializeField, Tooltip("Включить детальное логирование")]
        private bool _enableLogging = false;

        #endregion

        #region Private Fields

        // Основные переменные состояния
        private Quaternion _initialRotation; // Изначальный поворот объекта
        private Vector3 _lastTargetPosition; // Последняя позиция цели
        private Vector3 _targetVelocity; // Скорость движения цели
        private float _currentAngularVelocity; // Текущая угловая скорость
        private bool _isInitialized; // Флаг инициализации
        private bool _wasTargetValid; // Была ли цель валидной в предыдущем кадре

        // Переменные предсказания
        private Vector3 _predictedTargetPosition; // Предсказанная позиция цели
        private Vector3[] _positionHistory; // История позиций цели
        private int _historyIndex; // Индекс в истории позиций
        private const int HistorySize = 5; // Размер истории позиций

        // Переменные автопоиска цели
        private float _lastTargetSearchTime; // Время последнего поиска цели
        private Transform[] _potentialTargets; // Массив потенциальных целей

        // Оптимизация производительности
        private float _lastUpdateTime; // Время последнего обновления
        private float _updateInterval; // Интервал обновления
        private static readonly int MaxLookAtInstances = 50; // Максимальное количество экземпляров
        private static int ActiveInstances = 0; // Количество активных экземпляров

        #endregion

        #region Properties

        /// <summary>
        /// Получает или устанавливает, включено ли слежение за целью
        /// </summary>
        public bool IsLookingEnabled
        {
            get => _isLookingEnabled;
            set => SetLookingState(value);
        }

        /// <summary>
        /// Получает или устанавливает текущую цель для слежения
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => SetTarget(value);
        }

        /// <summary>
        /// Получает текущее расстояние до цели
        /// </summary>
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        /// <summary>
        /// Получает, находится ли цель в допустимом диапазоне расстояний
        /// </summary>
        public bool IsTargetInRange
        {
            get
            {
                if (_target == null) return false;
                float distance = DistanceToTarget;
                return distance >= _minTargetDistance && (_maxTargetDistance <= 0f || distance <= _maxTargetDistance);
            }
        }

        /// <summary>
        /// Получает направление к цели в мировых координатах
        /// </summary>
        public Vector3 DirectionToTarget => _target != null ? (_target.position - transform.position).normalized : Vector3.zero;

        /// <summary>
        /// Получает текущую угловую скорость поворота
        /// </summary>
        public float CurrentAngularVelocity => _currentAngularVelocity;

        /// <summary>
        /// Получает, активно ли в данный момент слежение
        /// </summary>
        public bool IsActivelyLooking => _isLookingEnabled && _target != null && IsTargetInRange;

        /// <summary>
        /// Получает, инициализирована ли система
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Вызывается при установке новой цели
        /// </summary>
        public event Action<Transform> OnTargetChanged;

        /// <summary>
        /// Вызывается при потере цели
        /// </summary>
        public event Action OnTargetLost;

        /// <summary>
        /// Вызывается при входе цели в диапазон слежения
        /// </summary>
        public event Action<Transform> OnTargetInRange;

        /// <summary>
        /// Вызывается при выходе цели из диапазона слежения
        /// </summary>
        public event Action<Transform> OnTargetOutOfRange;

        /// <summary>
        /// Вызывается при достижении цели (минимальное расстояние)
        /// </summary>
        public event Action<Transform> OnTargetReached;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponent();
        }

        private void OnEnable()
        {
            if (ActiveInstances >= MaxLookAtInstances)
            {
                LogWarning($"Достигнуто максимальное количество экземпляров слежения ({MaxLookAtInstances}). Отключение компонента на {gameObject.name}");
                enabled = false;
                return;
            }

            ActiveInstances++;
        }

        private void Start()
        {
            if (_autoFindTarget && _target == null)
            {
                FindNearestTarget();
            }
        }

        private void Update()
        {
            if (!ShouldUpdate())
                return;

            ProcessLookAtSystem();
            _lastUpdateTime = Time.time;
        }

        private void OnDisable()
        {
            ActiveInstances = Mathf.Max(0, ActiveInstances - 1);
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
            // Сохранение изначального поворота
            _initialRotation = transform.rotation;

            // Инициализация массивов для предсказания
            _positionHistory = new Vector3[HistorySize];
            _historyIndex = 0;

            // Настройка интервала обновления
            _updateInterval = 1f / _updateFrequency;

            _isInitialized = true;
            Log("AdvancedLookAtTarget успешно инициализирована.");
        }

        #endregion

        #region Core Logic

        private bool ShouldUpdate()
        {
            return _isInitialized && 
                   _isLookingEnabled && 
                   (Time.time - _lastUpdateTime) >= _updateInterval;
        }

        private void ProcessLookAtSystem()
        {
            // Автоматический поиск цели
            if (_autoFindTarget && ShouldSearchForTarget())
            {
                FindNearestTarget();
            }

            // Обновление предсказания движения цели
            if (_usePrediction && _target != null)
            {
                UpdateTargetPrediction();
            }

            // Основная логика слежения
            if (_target != null)
            {
                ProcessTargetTracking();
            }
            else if (_useInertia)
            {
                ProcessInertia();
            }
        }

        private bool ShouldSearchForTarget()
        {
            return Time.time - _lastTargetSearchTime > _targetSearchInterval;
        }

        private void FindNearestTarget()
        {
            _lastTargetSearchTime = Time.time;

            GameObject[] potentialObjects = GameObject.FindGameObjectsWithTag(_targetTag);
            if (potentialObjects.Length == 0)
            {
                Log($"Не найдено объектов с тегом '{_targetTag}'");
                return;
            }

            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var obj in potentialObjects)
            {
                if (obj.transform == transform) continue; // Исключаем себя

                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = obj.transform;
                }
            }

            if (nearestTarget != null && nearestTarget != _target)
            {
                SetTarget(nearestTarget);
                Log($"Автоматически найдена новая цель: {nearestTarget.name} на расстоянии {nearestDistance:F2}");
            }
        }

        private void UpdateTargetPrediction()
        {
            // Обновление истории позиций
            _positionHistory[_historyIndex] = _target.position;
            _historyIndex = (_historyIndex + 1) % HistorySize;

            // Расчет средней скорости
            Vector3 averageVelocity = Vector3.zero;
            int validSamples = 0;

            for (int i = 1; i < HistorySize; i++)
            {
                Vector3 velocity = (_positionHistory[i] - _positionHistory[i - 1]) / Time.deltaTime;
                if (velocity.sqrMagnitude < 100f) // Фильтр чрезмерно больших скоростей
                {
                    averageVelocity += velocity;
                    validSamples++;
                }
            }

            if (validSamples > 0)
            {
                averageVelocity /= validSamples;
                _targetVelocity = Vector3.Lerp(_targetVelocity, averageVelocity, _predictionSmoothing);
            }

            // Предсказание будущей позиции
            _predictedTargetPosition = _target.position + _targetVelocity * _predictionTime;
        }

        private void ProcessTargetTracking()
        {
            Vector3 targetPosition = _usePrediction ? _predictedTargetPosition : _target.position;
            Vector3 directionToTarget = targetPosition - transform.position;

            // Проверка расстояния до цели
            if (!ValidateTargetDistance(directionToTarget))
                return;

            // Расчет поворота для слежения
            Quaternion targetRotation = CalculateLookAtRotation(directionToTarget);

            // Применение ограничений поворота
            targetRotation = ApplyRotationConstraints(targetRotation);

            // Расчет скорости поворота
            float effectiveSpeed = CalculateEffectiveRotationSpeed();

            // Плавный поворот к цели
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, effectiveSpeed * Time.deltaTime);

            // Обновление угловой скорости
            UpdateAngularVelocity();

            // Обработка копирования поворота
            if (_copyRotationAxis != CopyRotationAxis.None)
            {
                ProcessRotationCopying(directionToTarget);
            }
        }

        private bool ValidateTargetDistance(Vector3 directionToTarget)
        {
            float distanceSqr = directionToTarget.sqrMagnitude;
            float minDistanceSqr = _minTargetDistance * _minTargetDistance;

            if (distanceSqr < minDistanceSqr)
            {
                if (!_wasTargetValid)
                {
                    OnTargetReached?.Invoke(_target);
                    Log($"Цель достигнута: {_target.name}");
                }
                _wasTargetValid = false;
                return false;
            }

            if (_maxTargetDistance > 0f)
            {
                float maxDistanceSqr = _maxTargetDistance * _maxTargetDistance;
                if (distanceSqr > maxDistanceSqr)
                {
                    if (_wasTargetValid)
                    {
                        OnTargetOutOfRange?.Invoke(_target);
                        Log($"Цель вышла из диапазона: {_target.name}");
                    }
                    _wasTargetValid = false;
                    return false;
                }
            }

            if (!_wasTargetValid)
            {
                OnTargetInRange?.Invoke(_target);
                Log($"Цель в диапазоне слежения: {_target.name}");
            }
            _wasTargetValid = true;
            return true;
        }

        private Quaternion CalculateLookAtRotation(Vector3 directionToTarget)
        {
            // Получение локальной оси для направления
            Vector3 localAxis = GetLocalAxisVector(_lookAxis);

            // Преобразование в мировые координаты
            Vector3 worldAxis = transform.TransformDirection(localAxis);

            // Расчет поворота для направления выбранной оси на цель
            Quaternion lookRotation = Quaternion.FromToRotation(worldAxis, directionToTarget.normalized) * transform.rotation;

            return lookRotation;
        }

        private Vector3 GetLocalAxisVector(LookAxis axis)
        {
            switch (axis)
            {
                case LookAxis.Forward: return Vector3.forward;
                case LookAxis.Back: return Vector3.back;
                case LookAxis.Up: return Vector3.up;
                case LookAxis.Down: return Vector3.down;
                case LookAxis.Right: return Vector3.right;
                case LookAxis.Left: return Vector3.left;
                default: return Vector3.forward;
            }
        }

        private Quaternion ApplyRotationConstraints(Quaternion targetRotation)
        {
            if (_rotationConstraint == RotationConstraint.None)
                return targetRotation;

            Vector3 eulerAngles = targetRotation.eulerAngles;

            // Нормализация углов к диапазону -180 до 180
            eulerAngles.x = NormalizeAngle(eulerAngles.x);
            eulerAngles.y = NormalizeAngle(eulerAngles.y);

            // Применение ограничений
            if (_rotationConstraint == RotationConstraint.Vertical || _rotationConstraint == RotationConstraint.Both)
            {
                eulerAngles.x = Mathf.Clamp(eulerAngles.x, _minVerticalAngle, _maxVerticalAngle);
            }

            if (_rotationConstraint == RotationConstraint.Horizontal || _rotationConstraint == RotationConstraint.Both)
            {
                eulerAngles.y = Mathf.Clamp(eulerAngles.y, _minHorizontalAngle, _maxHorizontalAngle);
            }

            return Quaternion.Euler(eulerAngles);
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private float CalculateEffectiveRotationSpeed()
        {
            float effectiveSpeed = _rotationSpeed;

            if (_useAdaptiveSpeed && _target != null)
            {
                float distance = DistanceToTarget;
                float normalizedDistance = Mathf.Clamp01(distance / _maxSpeedDistance);
                float speedMultiplier = _speedDistanceCurve.Evaluate(normalizedDistance);
                effectiveSpeed *= speedMultiplier;
            }

            return effectiveSpeed;
        }

        private void UpdateAngularVelocity()
        {
            // Простой расчет угловой скорости
            float deltaAngle = Quaternion.Angle(transform.rotation, _initialRotation);
            _currentAngularVelocity = deltaAngle / Time.deltaTime;
        }

        private void ProcessRotationCopying(Vector3 directionToTarget)
        {
            Vector3 rotationAxis = GetGlobalAxisVector(_copyRotationAxis);
            if (rotationAxis == Vector3.zero) return;

            // Извлечение угла поворота цели относительно выбранной оси
            Vector3 targetAxis = _target.TransformDirection(rotationAxis);
            Vector3 projectedAxis = Vector3.ProjectOnPlane(targetAxis, directionToTarget.normalized);

            if (projectedAxis.sqrMagnitude > _minRotationThreshold * _minRotationThreshold)
            {
                // Расчет угла между текущей и целевой ориентацией оси
                Vector3 currentAxis = transform.TransformDirection(rotationAxis);
                float angle = Vector3.SignedAngle(currentAxis, projectedAxis, directionToTarget.normalized);
                
                float copiedRotation = angle * _rotationCopyMultiplier;

                // Применение дополнительного поворота без сглаживания
                if (Mathf.Abs(copiedRotation) > _minRotationThreshold)
                {
                    Quaternion copyRotation = Quaternion.AngleAxis(copiedRotation, directionToTarget.normalized);
                    transform.rotation = copyRotation * transform.rotation;
                }
            }
        }

        private Vector3 GetGlobalAxisVector(CopyRotationAxis axis)
        {
            switch (axis)
            {
                case CopyRotationAxis.X: return Vector3.right;
                case CopyRotationAxis.Y: return Vector3.up;
                case CopyRotationAxis.Z: return Vector3.forward;
                default: return Vector3.zero;
            }
        }

        private void ProcessInertia()
        {
            // Постепенное замедление при отсутствии цели
            if (_currentAngularVelocity > 0.1f)
            {
                float dampingFactor = Mathf.Exp(-_inertiaDamping * Time.deltaTime);
                _currentAngularVelocity *= dampingFactor;
                
                // Применение остаточного поворота
                float residualRotation = _currentAngularVelocity * Time.deltaTime;
                transform.Rotate(Vector3.up, residualRotation, Space.World);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Включает или отключает систему слежения
        /// </summary>
        /// <param name="enabled">True для включения, false для отключения</param>
        public void SetLookingState(bool enabled)
        {
            bool wasEnabled = _isLookingEnabled;
            _isLookingEnabled = enabled;

            if (enabled && !wasEnabled)
            {
                Log("Система слежения включена");
            }
            else if (!enabled && wasEnabled)
            {
                if (_useInertia)
                {
                    Log("Система слежения отключена, активирована инерция");
                }
                else
                {
                    _currentAngularVelocity = 0f;
                    Log("Система слежения отключена");
                }
            }
        }

        /// <summary>
        /// Устанавливает новую цель для слежения
        /// </summary>
        /// <param name="newTarget">Новая цель (может быть null)</param>
        public void SetTarget(Transform newTarget)
        {
            Transform previousTarget = _target;
            _target = newTarget;

            if (newTarget != null)
            {
                // Инициализация истории позиций для предсказания
                for (int i = 0; i < HistorySize; i++)
                {
                    _positionHistory[i] = newTarget.position;
                }
                _historyIndex = 0;
                _targetVelocity = Vector3.zero;

                OnTargetChanged?.Invoke(newTarget);
                Log($"Установлена новая цель: {newTarget.name}");
            }
            else if (previousTarget != null)
            {
                OnTargetLost?.Invoke();
                Log("Цель потеряна");
            }
        }

        /// <summary>
        /// Устанавливает скорость поворота
        /// </summary>
        /// <param name="speed">Новая скорость поворота</param>
        public void SetRotationSpeed(float speed)
        {
            _rotationSpeed = Mathf.Max(0.1f, speed);
            Log($"Скорость поворота установлена: {_rotationSpeed:F2}");
        }

        /// <summary>
        /// Устанавливает ось для направления взгляда
        /// </summary>
        /// <param name="axis">Новая ось взгляда</param>
        public void SetLookAxis(LookAxis axis)
        {
            _lookAxis = axis;
            Log($"Ось взгляда установлена: {axis}");
        }

        /// <summary>
        /// Устанавливает ось для копирования поворота
        /// </summary>
        /// <param name="axis">Ось копирования поворота</param>
        public void SetCopyRotationAxis(CopyRotationAxis axis)
        {
            _copyRotationAxis = axis;
            Log($"Ось копирования поворота установлена: {axis}");
        }

        /// <summary>
        /// Устанавливает мультипликатор копирования поворота
        /// </summary>
        /// <param name="multiplier">Мультипликатор чувствительности</param>
        public void SetRotationCopyMultiplier(float multiplier)
        {
            _rotationCopyMultiplier = Mathf.Clamp(multiplier, 0f, 5f);
            Log($"Мультипликатор копирования поворота установлен: {_rotationCopyMultiplier:F2}");
        }

        /// <summary>
        /// Устанавливает диапазон расстояний для слежения
        /// </summary>
        /// <param name="minDistance">Минимальное расстояние</param>
        /// <param name="maxDistance">Максимальное расстояние (0 = без ограничений)</param>
        public void SetDistanceRange(float minDistance, float maxDistance)
        {
            _minTargetDistance = Mathf.Max(0.01f, minDistance);
            _maxTargetDistance = Mathf.Max(0f, maxDistance);
            
            if (_maxTargetDistance > 0f && _maxTargetDistance < _minTargetDistance)
            {
                _maxTargetDistance = _minTargetDistance;
            }
            
            Log($"Диапазон расстояний установлен: {_minTargetDistance:F2} - {(_maxTargetDistance > 0f ? _maxTargetDistance.ToString("F2") : "∞")}");
        }

        /// <summary>
        /// Устанавливает ограничения поворота
        /// </summary>
        /// <param name="constraint">Тип ограничения</param>
        /// <param name="minVertical">Минимальный вертикальный угол</param>
        /// <param name="maxVertical">Максимальный вертикальный угол</param>
        /// <param name="minHorizontal">Минимальный горизонтальный угол</param>
        /// <param name="maxHorizontal">Максимальный горизонтальный угол</param>
        public void SetRotationConstraints(RotationConstraint constraint, float minVertical = -90f, float maxVertical = 90f, float minHorizontal = -180f, float maxHorizontal = 180f)
        {
            _rotationConstraint = constraint;
            _minVerticalAngle = Mathf.Clamp(minVertical, -90f, 90f);
            _maxVerticalAngle = Mathf.Clamp(maxVertical, -90f, 90f);
            _minHorizontalAngle = Mathf.Clamp(minHorizontal, -180f, 180f);
            _maxHorizontalAngle = Mathf.Clamp(maxHorizontal, -180f, 180f);
            
            Log($"Ограничения поворота установлены: {constraint}");
        }

        /// <summary>
        /// Включает или отключает адаптивную скорость
        /// </summary>
        /// <param name="enable">True для включения адаптивной скорости</param>
        public void SetAdaptiveSpeed(bool enable)
        {
            _useAdaptiveSpeed = enable;
            Log($"Адаптивная скорость {(enable ? "включена" : "отключена")}");
        }

        /// <summary>
        /// Включает или отключает предсказание движения цели
        /// </summary>
        /// <param name="enable">True для включения предсказания</param>
        public void SetPrediction(bool enable)
        {
            _usePrediction = enable;
            
            if (enable && _target != null)
            {
                // Инициализация истории позиций
                for (int i = 0; i < HistorySize; i++)
                {
                    _positionHistory[i] = _target.position;
                }
                _historyIndex = 0;
                _targetVelocity = Vector3.zero;
            }
            
            Log($"Предсказание движения цели {(enable ? "включено" : "отключено")}");
        }

        /// <summary>
        /// Устанавливает время предсказания движения
        /// </summary>
        /// <param name="predictionTime">Время предсказания в секундах</param>
        public void SetPredictionTime(float predictionTime)
        {
            _predictionTime = Mathf.Clamp(predictionTime, 0.1f, 2f);
            Log($"Время предсказания установлено: {_predictionTime:F2}с");
        }

        /// <summary>
        /// Включает или отключает автоматический поиск цели
        /// </summary>
        /// <param name="enable">True для включения автопоиска</param>
        /// <param name="targetTag">Тег для поиска целей</param>
        public void SetAutoTargetSearch(bool enable, string targetTag = "Player")
        {
            _autoFindTarget = enable;
            _targetTag = targetTag;
            
            if (enable)
            {
                _lastTargetSearchTime = 0f; // Сброс таймера для немедленного поиска
                Log($"Автопоиск цели включен для тега: '{targetTag}'");
            }
            else
            {
                Log("Автопоиск цели отключен");
            }
        }

        /// <summary>
        /// Принудительно ищет ближайшую цель с заданным тегом
        /// </summary>
        public void ForceTargetSearch()
        {
            FindNearestTarget();
        }

        /// <summary>
        /// Сбрасывает поворот объекта к изначальному состоянию
        /// </summary>
        public void ResetToInitialRotation()
        {
            transform.rotation = _initialRotation;
            _currentAngularVelocity = 0f;
            Log("Поворот сброшен к изначальному состоянию");
        }

        /// <summary>
        /// Мгновенно поворачивает объект к цели без сглаживания
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null || !IsTargetInRange)
            {
                LogWarning("Невозможно повернуться к цели: цель отсутствует или вне диапазона");
                return;
            }

            Vector3 directionToTarget = DirectionToTarget;
            Quaternion targetRotation = CalculateLookAtRotation(directionToTarget);
            targetRotation = ApplyRotationConstraints(targetRotation);
            
            transform.rotation = targetRotation;
            _currentAngularVelocity = 0f;
            
            Log($"Мгновенный поворот к цели: {_target.name}");
        }

        /// <summary>
        /// Получает детальную информацию о состоянии системы
        /// </summary>
        /// <returns>Строка с полной информацией о состоянии</returns>
        public string GetDebugInfo()
        {
            if (!_isInitialized)
                return "Система не инициализирована";

            string targetInfo = _target != null ? $"{_target.name} ({DistanceToTarget:F2}м)" : "НЕТ";
            string rangeStatus = IsTargetInRange ? "В ДИАПАЗОНЕ" : "ВНЕ ДИАПАЗОНА";
            string predictionInfo = _usePrediction ? $"ВКЛЮЧЕНО ({_predictionTime:F2}с)" : "ОТКЛЮЧЕНО";
            
            return $"=== Advanced Look At Target ===\n" +
                   $"Состояние: {(_isLookingEnabled ? "АКТИВНА" : "НЕАКТИВНА")}\n" +
                   $"Цель: {targetInfo}\n" +
                   $"Статус цели: {rangeStatus}\n" +
                   $"Активное слежение: {(IsActivelyLooking ? "ДА" : "НЕТ")}\n" +
                   $"Ось взгляда: {_lookAxis}\n" +
                   $"Копирование поворота: {_copyRotationAxis}\n" +
                   $"Скорость поворота: {_rotationSpeed:F2}°/с\n" +
                   $"Угловая скорость: {_currentAngularVelocity:F2}°/с\n" +
                   $"Ограничения: {_rotationConstraint}\n" +
                   $"Предсказание: {predictionInfo}\n" +
                   $"Автопоиск: {(_autoFindTarget ? $"ВКЛЮЧЕН ('{_targetTag}')" : "ОТКЛЮЧЕН")}\n" +
                   $"Активных экземпляров: {ActiveInstances}/{MaxLookAtInstances}";
        }

        /// <summary>
        /// Получает информацию о текущих углах поворота
        /// </summary>
        /// <returns>Строка с информацией об углах</returns>
        public string GetRotationInfo()
        {
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 initialEuler = _initialRotation.eulerAngles;
            
            // Нормализация углов
            currentEuler.x = NormalizeAngle(currentEuler.x);
            currentEuler.y = NormalizeAngle(currentEuler.y);
            currentEuler.z = NormalizeAngle(currentEuler.z);
            
            initialEuler.x = NormalizeAngle(initialEuler.x);
            initialEuler.y = NormalizeAngle(initialEuler.y);
            initialEuler.z = NormalizeAngle(initialEuler.z);
            
            Vector3 deltaEuler = currentEuler - initialEuler;
            
            return $"Текущие углы: ({currentEuler.x:F1}°, {currentEuler.y:F1}°, {currentEuler.z:F1}°)\n" +
                   $"Изначальные углы: ({initialEuler.x:F1}°, {initialEuler.y:F1}°, {initialEuler.z:F1}°)\n" +
                   $"Изменение: ({deltaEuler.x:F1}°, {deltaEuler.y:F1}°, {deltaEuler.z:F1}°)";
        }

        #endregion

        #region Utility Methods

        private void ValidateSettings()
        {
            // Валидация основных параметров
            _rotationSpeed = Mathf.Clamp(_rotationSpeed, 0.1f, 50f);
            _rotationCopyMultiplier = Mathf.Clamp(_rotationCopyMultiplier, 0f, 5f);
            _minRotationThreshold = Mathf.Clamp(_minRotationThreshold, 0.001f, 1f);
            _minTargetDistance = Mathf.Max(0.01f, _minTargetDistance);
            _maxTargetDistance = Mathf.Max(0f, _maxTargetDistance);
            _updateFrequency = Mathf.Clamp(_updateFrequency, 30f, 120f);
            
            // Валидация углов ограничений
            _minVerticalAngle = Mathf.Clamp(_minVerticalAngle, -90f, 90f);
            _maxVerticalAngle = Mathf.Clamp(_maxVerticalAngle, -90f, 90f);
            _minHorizontalAngle = Mathf.Clamp(_minHorizontalAngle, -180f, 180f);
            _maxHorizontalAngle = Mathf.Clamp(_maxHorizontalAngle, -180f, 180f);
            
            // Проверка корректности диапазонов
            if (_minVerticalAngle > _maxVerticalAngle)
            {
                float temp = _minVerticalAngle;
                _minVerticalAngle = _maxVerticalAngle;
                _maxVerticalAngle = temp;
            }
            
            if (_minHorizontalAngle > _maxHorizontalAngle)
            {
                float temp = _minHorizontalAngle;
                _minHorizontalAngle = _maxHorizontalAngle;
                _maxHorizontalAngle = temp;
            }
            
            // Валидация настроек предсказания
            _predictionTime = Mathf.Clamp(_predictionTime, 0.1f, 2f);
            _predictionSmoothing = Mathf.Clamp01(_predictionSmoothing);
            
            // Валидация настроек инерции
            _inertiaDamping = Mathf.Clamp(_inertiaDamping, 0.1f, 3f);
            
            // Валидация автопоиска
            _targetSearchInterval = Mathf.Clamp(_targetSearchInterval, 0.5f, 10f);
            _maxSpeedDistance = Mathf.Max(1f, _maxSpeedDistance);
            
            // Проверка расстояний
            if (_maxTargetDistance > 0f && _maxTargetDistance < _minTargetDistance)
            {
                _maxTargetDistance = _minTargetDistance;
            }
        }

        private void DrawDebugGizmos()
        {
            // Основная визуализация состояния
            Color mainColor = GetStateColor();
            Gizmos.color = mainColor;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Визуализация направления взгляда
            Vector3 lookDirection = transform.TransformDirection(GetLocalAxisVector(_lookAxis));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, lookDirection * 2f);

            // Визуализация цели и связи с ней
            if (_target != null)
            {
                DrawTargetGizmos();
            }

            // Визуализация диапазона расстояний
            DrawDistanceRangeGizmos();

            // Визуализация ограничений поворота
            DrawRotationConstraintGizmos();

            // Визуализация предсказания
            if (_usePrediction && _target != null)
            {
                DrawPredictionGizmos();
            }

#if UNITY_EDITOR
            // Информационная панель
            if (_enableLogging)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, GetDebugInfo());
            }
#endif
        }

        private Color GetStateColor()
        {
            if (!_isInitialized)
                return Color.gray;
            
            if (!_isLookingEnabled)
                return Color.red;
            
            if (_target == null)
                return Color.yellow;
            
            if (!IsTargetInRange)
                return Color.blue;
            
            return Color.green;
        }

        private void DrawTargetGizmos()
        {
            Vector3 targetPos = _target.position;
            
            // Линия к цели
            Color lineColor = IsTargetInRange ? Color.green : Color.red;
            Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.7f);
            Gizmos.DrawLine(transform.position, targetPos);

            // Маркер цели
            Gizmos.color = lineColor;
            Gizmos.DrawWireSphere(targetPos, 0.2f);

            // Направление к цели
            Vector3 direction = DirectionToTarget;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, direction * DistanceToTarget);

#if UNITY_EDITOR
            // Информация о цели
            string targetInfo = $"Цель: {_target.name}\nРасстояние: {DistanceToTarget:F2}м";
            UnityEditor.Handles.Label(targetPos + Vector3.up * 0.5f, targetInfo);
#endif
        }

        private void DrawDistanceRangeGizmos()
        {
            // Минимальное расстояние
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _minTargetDistance);

            // Максимальное расстояние
            if (_maxTargetDistance > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, _maxTargetDistance);
            }
        }

        private void DrawRotationConstraintGizmos()
        {
            if (_rotationConstraint == RotationConstraint.None)
                return;

            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            
            // Визуализация ограничений поворота (упрощенная)
            if (_rotationConstraint == RotationConstraint.Vertical || _rotationConstraint == RotationConstraint.Both)
            {
                // Показываем вертикальные ограничения
                Vector3 minVerticalDir = Quaternion.Euler(_minVerticalAngle, 0, 0) * transform.forward;
                Vector3 maxVerticalDir = Quaternion.Euler(_maxVerticalAngle, 0, 0) * transform.forward;
                
                Gizmos.DrawRay(transform.position, minVerticalDir * 1.5f);
                Gizmos.DrawRay(transform.position, maxVerticalDir * 1.5f);
            }
        }

        private void DrawPredictionGizmos()
        {
            // Предсказанная позиция цели
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_predictedTargetPosition, 0.15f);
            
            // Линия от текущей к предсказанной позиции
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
            Gizmos.DrawLine(_target.position, _predictedTargetPosition);
            
            // Вектор скорости
            if (_targetVelocity.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(_target.position, _targetVelocity * _predictionTime);
            }

#if UNITY_EDITOR
            // Информация о предсказании
            string predictionInfo = $"Предсказание\nСкорость: {_targetVelocity.magnitude:F2}м/с";
            UnityEditor.Handles.Label(_predictedTargetPosition + Vector3.up * 0.3f, predictionInfo);
#endif
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[AdvancedLookAtTarget] {gameObject.name}: {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AdvancedLookAtTarget] {gameObject.name}: {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AdvancedLookAtTarget] {gameObject.name}: {message}");
        }

        #endregion
    }
}