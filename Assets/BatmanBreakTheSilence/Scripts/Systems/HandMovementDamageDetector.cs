using System;
using System.Collections.Generic;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Данные о движении руки за анализируемый период
    /// </summary>
    [System.Serializable]
    public struct MovementData
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public float currentDistance;
        public float straightLineDistance;
        public float linearity;
        public float movementTime;
        public float timestamp;

        public MovementData(Vector3 start, Vector3 end, float currentDist, float straightDist, float linearity, float time)
        {
            this.startPosition = start;
            this.endPosition = end;
            this.currentDistance = currentDist;
            this.straightLineDistance = straightDist;
            this.linearity = linearity;
            this.movementTime = time;
            this.timestamp = Time.time;
        }
    }

    /// <summary>
    /// Информация о нанесенном уроне
    /// </summary>
    public struct DamageInfo
    {
        public Vector3 impactPoint;
        public Vector3 impactVelocity;
        public float calculatedDamage;
        public MovementData movement;
        public GameObject target;

        public DamageInfo(Vector3 point, Vector3 velocity, float damage, MovementData movement, GameObject target)
        {
            this.impactPoint = point;
            this.impactVelocity = velocity;
            this.calculatedDamage = damage;
            this.movement = movement;
            this.target = target;
        }
    }

    /// <summary>
    /// Упрощенный детектор урона на основе пройденного расстояния и прямолинейности движения
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HandMovementDamageDetector : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Анализ движения")]
        [SerializeField, Tooltip("Время анализа движения для расчета урона (сек)"), Range(0.1f, 3f)]
        private float _movementAnalysisTime = 0.8f;

        [SerializeField, Tooltip("Частота записи позиций (Гц)"), Range(10f, 120f)]
        private float _trackingFrequency = 60f;

        [Header("Расчет урона")]
        [SerializeField, Tooltip("Минимальная дистанция для урона (м)"), Range(0.01f, 2f)]
        private float _minDamageDistance = 0.05f;

        [SerializeField, Tooltip("Максимальная дистанция для максимального урона (м)"), Range(0.1f, 5f)]
        private float _maxDamageDistance = 1f;

        [SerializeField, Tooltip("Минимальный урон"), Range(0f, 100f)]
        private float _minDamage = 5f;

        [SerializeField, Tooltip("Максимальный урон"), Range(1f, 1000f)]
        private float _maxDamage = 100f;

        [Header("Фильтры")]
        [SerializeField, Tooltip("Слои для обнаружения ударов")]
        private LayerMask _impactLayers = -1;

        [Header("Отладка")]
        [SerializeField, Tooltip("Показывать траекторию движения")]
        private bool _showMovementGizmos = false;

        [SerializeField, Tooltip("Показывать информацию об уроне")]
        private bool _showDamageInfo = false;

        [SerializeField, Tooltip("Включить детальное логирование")]
        private bool _enableLogging = false;

        #endregion

        #region Private Fields

        private Rigidbody _rigidbody;
        private List<Vector3> _positionHistory;
        private List<float> _timeHistory;
        private float _trackingTimer;

        // Кэшированные данные
        private Vector3 _previousPosition;
        private Vector3 _currentVelocity;

        // Статистика
        private float _totalDamageDealt;
        private int _damageHitCount;

        #endregion

        #region Properties

        /// <summary>
        /// Получает данные о последнем движении
        /// </summary>
        public MovementData LastMovement { get; private set; }

        /// <summary>
        /// Получает общий нанесенный урон
        /// </summary>
        public float TotalDamageDealt => _totalDamageDealt;

        /// <summary>
        /// Получает количество попаданий
        /// </summary>
        public int DamageHitCount => _damageHitCount;

        /// <summary>
        /// Получает список позиций в текущем анализируемом промежутке
        /// </summary>
        public IReadOnlyList<Vector3> CurrentPositions => GetCurrentPositions();

        /// <summary>
        /// Получает текущую дистанцию движения
        /// </summary>
        public float CurrentMovementDistance => CalculateCurrentDistance();

        /// <summary>
        /// Получает текущую прямолинейность
        /// </summary>
        public float CurrentLinearity => CalculateCurrentLinearity();

        #endregion

        #region Events

        /// <summary>
        /// Вызывается при нанесении урона
        /// </summary>
        public event Action<DamageInfo> OnDamageDealt;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponent();
        }

        private void FixedUpdate()
        {
            UpdateMovementTracking();
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void OnDrawGizmos()
        {
            if (!_showMovementGizmos) return;
            DrawMovementGizmos();
        }

        private void OnGUI()
        {
            if (!_showDamageInfo) return;
            DrawDamageInfo();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _positionHistory = new List<Vector3>();
            _timeHistory = new List<float>();
            
            _previousPosition = transform.position;
            _trackingTimer = 0f;
            _totalDamageDealt = 0f;
            _damageHitCount = 0;

            ValidateSettings();
            Log("HandMovementDamageDetector инициализирован");
        }

        #endregion

        #region Movement Tracking

        private void UpdateMovementTracking()
        {
            _trackingTimer += Time.fixedDeltaTime;
            
            if (_trackingTimer >= (1f / _trackingFrequency))
            {
                RecordPosition();
                _trackingTimer = 0f;
            }

            UpdateCurrentVelocity();
            CleanupOldHistory();
        }

        private void RecordPosition()
        {
            Vector3 currentPos = transform.position;
            float currentTime = Time.time;

            _positionHistory.Add(currentPos);
            _timeHistory.Add(currentTime);

            _previousPosition = currentPos;
        }

        private void UpdateCurrentVelocity()
        {
            if (_rigidbody != null)
            {
                _currentVelocity = _rigidbody.velocity;
            }
            else
            {
                Vector3 currentPos = transform.position;
                _currentVelocity = (currentPos - _previousPosition) / Time.fixedDeltaTime;
                _previousPosition = currentPos;
            }
        }

        private void CleanupOldHistory()
        {
            if (_timeHistory == null || _positionHistory == null) return;
            
            float cutoffTime = Time.time - _movementAnalysisTime;
            
            // Удаляем старые записи с начала списка - оставляем только текущий период
            while (_timeHistory.Count > 0 && _timeHistory[0] < cutoffTime)
            {
                _timeHistory.RemoveAt(0);
                _positionHistory.RemoveAt(0);
            }
        }

        private List<Vector3> GetCurrentPositions()
        {
            if (_positionHistory == null)
                return new List<Vector3>();
            return new List<Vector3>(_positionHistory);
        }

        private float CalculateCurrentDistance()
        {
            if (_positionHistory == null || _positionHistory.Count < 2) return 0f;

            float totalDistance = 0f;
            for (int i = 1; i < _positionHistory.Count; i++)
            {
                totalDistance += Vector3.Distance(_positionHistory[i - 1], _positionHistory[i]);
            }

            return totalDistance;
        }

        private float CalculateCurrentLinearity()
        {
            if (_positionHistory == null || _positionHistory.Count < 2) return 0f;

            Vector3 startPos = _positionHistory[0];
            Vector3 endPos = _positionHistory[_positionHistory.Count - 1];
            
            // Прямая дистанция от начала до конца движения
            float straightDistance = Vector3.Distance(startPos, endPos);
            
            // Фактический пройденный путь
            float actualPath = CalculateCurrentDistance();

            if (actualPath <= 0f || straightDistance <= 0f) return 0f;

            // Прямолинейность = прямая_дистанция / фактический_путь
            // 1.0 = идеально прямо, меньше 1.0 = кривое движение
            return Mathf.Clamp01(straightDistance / actualPath);
        }

        #endregion

        #region Collision Processing

        private void ProcessCollision(Collision collision)
        {
            // Проверяем фильтры
            if (!IsValidTarget(collision.gameObject)) return;

            // Анализируем текущее движение
            MovementData movement = AnalyzeCurrentMovement();

            // Рассчитываем урон
            float damage = CalculateDamage(movement);

            // Проверяем если урон равен 0
            if (damage <= 0f) return;

            // Создаем информацию об уроне
            Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            DamageInfo damageInfo = new DamageInfo(
                impactPoint, _currentVelocity, damage, movement, collision.gameObject
            );

            // Обновляем статистику
            _totalDamageDealt += damage;
            _damageHitCount++;

            // Вызываем событие
            OnDamageDealt?.Invoke(damageInfo);

            // Очищаем историю после удара для следующего анализа
            ClearMovementHistory();

            Log($"Урон: {damage:F1}, дистанция: {movement.currentDistance:F2}м, прямолинейность: {movement.linearity:F2}");
        }

        private MovementData AnalyzeCurrentMovement()
        {
            if (_positionHistory == null || _positionHistory.Count < 2)
            {
                return new MovementData(transform.position, transform.position, 0f, 0f, 0f, 0f);
            }

            Vector3 startPos = _positionHistory[0];
            Vector3 endPos = _positionHistory[_positionHistory.Count - 1];
            float movementTime = _timeHistory[_timeHistory.Count - 1] - _timeHistory[0];

            // Рассчитываем текущую дистанцию движения
            float currentDistance = CalculateCurrentDistance();

            // Рассчитываем прямую дистанцию и прямолинейность
            float straightLineDistance = Vector3.Distance(startPos, endPos);
            float linearity = CalculateCurrentLinearity();

            LastMovement = new MovementData(startPos, endPos, currentDistance, straightLineDistance, 
                                          linearity, movementTime);
            return LastMovement;
        }

        private float CalculateDamage(MovementData movement)
        {
            // Проверяем минимальную дистанцию
            if (movement.currentDistance < _minDamageDistance)
            {
                return 0f;
            }

            // Ограничиваем дистанцию максимальным значением
            float clampedDistance = Mathf.Min(movement.currentDistance, _maxDamageDistance);

            // Линейная интерполяция урона на основе дистанции
            // При _minDamageDistance = _minDamage, при _maxDamageDistance = _maxDamage
            float interpolatedDamage = Mathf.Lerp(
                _minDamage, 
                _maxDamage, 
                (clampedDistance - _minDamageDistance) / (_maxDamageDistance - _minDamageDistance)
            );

            // Применяем прямолинейность как множитель
            float finalDamage = interpolatedDamage * movement.linearity;

            return finalDamage;
        }

        #endregion

        #region Utility Methods

        private bool IsValidTarget(GameObject target)
        {
            return (_impactLayers.value & (1 << target.layer)) != 0;
        }

        private void ValidateSettings()
        {
            _movementAnalysisTime = Mathf.Clamp(_movementAnalysisTime, 0.1f, 3f);
            _trackingFrequency = Mathf.Clamp(_trackingFrequency, 10f, 120f);
            
            _minDamageDistance = Mathf.Max(0.01f, _minDamageDistance);
            _maxDamageDistance = Mathf.Max(_minDamageDistance + 0.01f, _maxDamageDistance);
            
            _minDamage = Mathf.Max(0f, _minDamage);
            _maxDamage = Mathf.Max(_minDamage, _maxDamage);
        }

        private void DrawMovementGizmos()
        {
            List<Vector3> currentPositions = GetCurrentPositions();
            if (currentPositions == null || currentPositions.Count < 2) return;

            Vector3 startPos = currentPositions[0];
            Vector3 endPos = currentPositions[currentPositions.Count - 1];
            Vector3 movementDirection = (endPos - startPos).normalized;

            // Рисуем текущую траекторию движения (только за анализируемый период)
            Gizmos.color = Color.cyan;
            for (int i = 1; i < currentPositions.Count; i++)
            {
                Gizmos.DrawLine(currentPositions[i - 1], currentPositions[i]);
            }

            // Рисуем прямую линию по направлению движения
            if (movementDirection != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPos, endPos);
                
                // Рисуем линии отклонения от прямой траектории
                for (int i = 0; i < currentPositions.Count; i++)
                {
                    Vector3 pointOnLine = Vector3.Project(currentPositions[i] - startPos, movementDirection) + startPos;
                    float deviation = Vector3.Distance(currentPositions[i], pointOnLine);
                    
                    if (deviation > 0.01f)
                    {
                        Gizmos.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(deviation * 10f));
                        Gizmos.DrawLine(currentPositions[i], pointOnLine);
                    }
                }
            }

            // Рисуем точки траектории
            Gizmos.color = Color.blue;
            foreach (Vector3 pos in currentPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.01f);
            }

            // Показываем начальную и конечную точки
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startPos, 0.02f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(endPos, 0.02f);

            // Визуализация прямолинейности и потенциального урона
            float currentLinearity = CalculateCurrentLinearity();
            float currentDistance = CalculateCurrentDistance();
            
            if (currentDistance >= _minDamageDistance)
            {
                Vector3 center = (startPos + endPos) * 0.5f;
                
                // Цвет зависит от прямолинейности
                Gizmos.color = Color.Lerp(Color.red, Color.green, currentLinearity);
                Gizmos.DrawWireCube(center, Vector3.one * 0.05f * (0.5f + currentLinearity));
                
                // Показываем направление движения стрелкой
                if (movementDirection != Vector3.zero)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawRay(center, movementDirection * 0.2f);
                }
            }
        }

        private void DrawDamageInfo()
        {
            float yOffset = 50f;
            GUI.Label(new Rect(10, yOffset, 400, 20), $"Общий урон: {_totalDamageDealt:F1}");
            GUI.Label(new Rect(10, yOffset + 20, 400, 20), $"Попаданий: {_damageHitCount}");
            GUI.Label(new Rect(10, yOffset + 40, 400, 20), $"Текущая дистанция: {CurrentMovementDistance:F2} м");
            GUI.Label(new Rect(10, yOffset + 60, 400, 20), $"Текущая прямолинейность: {CurrentLinearity:F2}");
            
            // Показываем потенциальный урон в реальном времени
            if (CurrentMovementDistance >= _minDamageDistance)
            {
                float clampedDistance = Mathf.Min(CurrentMovementDistance, _maxDamageDistance);
                float interpolatedDamage = Mathf.Lerp(
                    _minDamage, 
                    _maxDamage, 
                    (clampedDistance - _minDamageDistance) / (_maxDamageDistance - _minDamageDistance)
                );
                float potentialDamage = interpolatedDamage * CurrentLinearity;
                
                GUI.Label(new Rect(10, yOffset + 80, 400, 20), $"Потенциальный урон: {potentialDamage:F1}");
                GUI.Label(new Rect(10, yOffset + 100, 400, 20), $"Базовый урон (без прямолинейности): {interpolatedDamage:F1}");
            }
            else
            {
                GUI.Label(new Rect(10, yOffset + 80, 400, 20), "Дистанция слишком мала для урона");
            }
            
            if (LastMovement.currentDistance > 0f)
            {
                GUI.Label(new Rect(10, yOffset + 120, 400, 20), $"Последний урон: {CalculateDamage(LastMovement):F1}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Очищает историю движения
        /// </summary>
        public void ClearMovementHistory()
        {
            _positionHistory.Clear();
            _timeHistory.Clear();
        }

        /// <summary>
        /// Сбрасывает статистику урона
        /// </summary>
        public void ResetDamageStats()
        {
            _totalDamageDealt = 0f;
            _damageHitCount = 0;
        }

        /// <summary>
        /// Принудительно рассчитывает урон для текущего движения
        /// </summary>
        public float CalculateCurrentDamage()
        {
            MovementData currentMovement = AnalyzeCurrentMovement();
            return CalculateDamage(currentMovement);
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[HandMovementDamageDetector] {gameObject.name}: {message}");
            }
        }

        #endregion
    }
}