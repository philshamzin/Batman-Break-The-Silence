using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Главный контроллер, координирующий компоненты боевой системы Batman: Break The Silence.
    /// Следует шаблону композиции, делегируя обязанности специализированным системам.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatmanCombatController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Конфигурация системы")]
        [SerializeField, Tooltip("Настройки отслеживания курсора")]
        private CursorTrackingSettings _cursorTrackingSettings = new CursorTrackingSettings(); // Настройки для системы отслеживания курсора

        [SerializeField, Tooltip("Настройки управления камерой")]
        private CameraControlSettings _cameraControlSettings = new CameraControlSettings(); // Настройки для управления камерой

        [SerializeField, Tooltip("Настройки поведения правой руки")]
        private HandSettings _rightHandSettings = new HandSettings(); // Настройки для правой руки

        [SerializeField, Tooltip("Настройки поведения левой руки")]
        private HandSettings _leftHandSettings = new HandSettings(); // Настройки для левой руки

        [SerializeField, Tooltip("Настройки отладки и визуализации")]
        private DebugSettings _debugSettings = new DebugSettings(); // Настройки для отладки и гизмосов

        [SerializeField, Tooltip("События боевой системы")]
        private CombatEvents _combatEvents = new CombatEvents(); // События, связанные с боевой системой

        [Header("Ссылки на компоненты")]
        [SerializeField, Tooltip("Профиль постобработки для управления эффектами")]
        private PostProcessProfile _postProcessProfile; // Профиль для эффектов постобработки

        [SerializeField, Tooltip("Трансформ объекта, следующего за курсором")]
        private Transform _cursorFollower; // Трансформ для следования за курсором

        #endregion

        #region Private Systems

        private ICursorTracker _cursorTracker; // Система отслеживания курсора
        private ICameraController _cameraController; // Контроллер камеры
        private IHandController _rightHandController; // Контроллер правой руки
        private IHandController _leftHandController; // Контроллер левой руки
        private Camera _mainCamera; // Главная камера сцены

        // Управление состоянием
        private bool _isInitialized; // Флаг инициализации системы
        private Vector2 _screenCenter; // Центр экрана в пикселях
        private float _maxScreenDistance; // Максимальное расстояние от центра экрана
        private Vector3 _gizmoCenter; // Центр для отрисовки гизмосов

        #endregion

        #region Properties

        /// <summary>
        /// Получает, активно ли отслеживание ввода курсора
        /// </summary>
        public bool IsTracking => _cursorTracker?.IsTracking ?? false;

        /// <summary>
        /// Получает тип активной руки
        /// </summary>
        public HandType? ActiveHand => IsTracking ? 
            (_leftHandController?.IsActive == true ? HandType.Left : 
             _rightHandController?.IsActive == true ? HandType.Right : null) : null;

        /// <summary>
        /// Получает, инициализирована ли система
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Получает ссылку на главную камеру
        /// </summary>
        public Camera MainCamera => _mainCamera;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Вызывается при создании объекта
        /// </summary>
        private void Awake()
        {
            InitializeSystem();
        }

        /// <summary>
        /// Вызывается при старте сцены
        /// </summary>
        private void Start()
        {
            if (!_isInitialized)
            {
                LogError("Не удалось инициализировать систему. Компонент отключен.", this);
                enabled = false;
                return;
            }

            StartListening();
        }

        /// <summary>
        /// Вызывается каждый кадр
        /// </summary>
        private void Update()
        {
            if (!_isInitialized)
                return;

            ProcessInput();
            UpdateSystems();
            UpdateGizmoCenter();
        }

        /// <summary>
        /// Отрисовывает отладочные гизмосы
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_debugSettings.EnableLogging || !_isInitialized)
                return;

            DrawDebugGizmos();
        }

        /// <summary>
        /// Вызывается при уничтожении объекта
        /// </summary>
        private void OnDestroy()
        {
            StopListening();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует боевую систему
        /// </summary>
        private void InitializeSystem()
        {
            try
            {
                ValidateSettings();
                InitializeCamera();
                InitializeSystems();
                InitializeState();
                
                _isInitialized = true;
                
                if (_debugSettings.EnableLogging)
                {
                    Log("Система успешно инициализирована");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка инициализации: {ex.Message}", this);
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Инициализирует главную камеру
        /// </summary>
        private void InitializeCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                throw new InvalidOperationException("Главная камера не найдена. Убедитесь, что камера помечена как 'MainCamera'.");
            }
        }

        /// <summary>
        /// Инициализирует подсистемы
        /// </summary>
        private void InitializeSystems()
        {
            // Инициализация системы отслеживания курсора
            _cursorTracker = new CursorTrackingSystem(_cursorTrackingSettings);

            // Инициализация контроллера камеры
            _cameraController = new CameraController(
                _mainCamera, 
                _cursorFollower, 
                _cameraControlSettings, 
                _postProcessProfile,
                _debugSettings.EnableLogging
            );

            // Инициализация контроллеров рук
            _leftHandController = new HandController(
                HandType.Left, 
                _leftHandSettings, 
                _mainCamera, 
                _debugSettings.EnableLogging
            );

            _rightHandController = new HandController(
                HandType.Right, 
                _rightHandSettings, 
                _mainCamera, 
                _debugSettings.EnableLogging
            );

            if (_debugSettings.EnableLogging)
            {
                Log("Все подсистемы инициализированы");
            }
        }

        /// <summary>
        /// Инициализирует состояние системы
        /// </summary>
        private void InitializeState()
        {
            _screenCenter = new Vector2(
                CursorTrackingSettings.ReferenceWidth * 0.5f, 
                CursorTrackingSettings.ReferenceHeight * 0.5f
            );
            _maxScreenDistance = _screenCenter.magnitude;
        }

        /// <summary>
        /// Проверяет корректность настроек
        /// </summary>
        private void ValidateSettings()
        {
            // Проверка трансформов рук
            if (_leftHandSettings?.HandTransform == null || _rightHandSettings?.HandTransform == null)
            {
                LogWarning("Трансформы рук не назначены корректно", this);
            }

            // Проверка трансформа следования за курсором
            if (_cursorFollower == null)
            {
                LogWarning("Трансформ следования за курсором не назначен", this);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Подписывается на события подсистем
        /// </summary>
        private void StartListening()
        {
            if (_cursorTracker != null)
            {
                _cursorTracker.OnTrackingStarted += HandleTrackingStarted;
                _cursorTracker.OnTrackingStopped += HandleTrackingStopped;
            }

            if (_leftHandController != null)
            {
                _leftHandController.OnHandActivated += HandleHandActivated;
                _leftHandController.OnHandDeactivated += HandleHandDeactivated;
            }

            if (_rightHandController != null)
            {
                _rightHandController.OnHandActivated += HandleHandActivated;
                _rightHandController.OnHandDeactivated += HandleHandDeactivated;
            }
        }

        /// <summary>
        /// Отписывается от событий подсистем
        /// </summary>
        private void StopListening()
        {
            if (_cursorTracker != null)
            {
                _cursorTracker.OnTrackingStarted -= HandleTrackingStarted;
                _cursorTracker.OnTrackingStopped -= HandleTrackingStopped;
            }

            if (_leftHandController != null)
            {
                _leftHandController.OnHandActivated -= HandleHandActivated;
                _leftHandController.OnHandDeactivated -= HandleHandDeactivated;
            }

            if (_rightHandController != null)
            {
                _rightHandController.OnHandActivated -= HandleHandActivated;
                _rightHandController.OnHandDeactivated -= HandleHandDeactivated;
            }
        }

        /// <summary>
        /// Обрабатывает начало отслеживания
        /// </summary>
        /// <param name="handType">Тип руки, начавшей отслеживание</param>
        private void HandleTrackingStarted(HandType handType)
        {
            _combatEvents.OnTrackingStarted?.Invoke(handType);
            
            if (_debugSettings.EnableLogging)
            {
                Log($"Начато отслеживание для руки {handType}");
            }
        }

        /// <summary>
        /// Обрабатывает остановку отслеживания
        /// </summary>
        private void HandleTrackingStopped()
        {
            _combatEvents.OnTrackingStopped?.Invoke();
            
            if (_debugSettings.EnableLogging)
            {
                Log("Отслеживание остановлено");
            }
        }

        /// <summary>
        /// Обрабатывает активацию руки
        /// </summary>
        /// <param name="handType">Тип активированной руки</param>
        private void HandleHandActivated(HandType handType)
        {
            _combatEvents.OnHandActivated?.Invoke(handType);
            
            if (_debugSettings.EnableLogging)
            {
                Log($"Рука {handType} активирована");
            }
        }

        /// <summary>
        /// Обрабатывает деактивацию руки
        /// </summary>
        /// <param name="handType">Тип деактивированной руки</param>
        private void HandleHandDeactivated(HandType handType)
        {
            _combatEvents.OnHandDeactivated?.Invoke(handType);
            
            if (_debugSettings.EnableLogging)
            {
                Log($"Рука {handType} деактивирована");
            }
        }

        #endregion

        #region Input Processing

        /// <summary>
        /// Обрабатывает ввод пользователя
        /// </summary>
        private void ProcessInput()
        {
            if (!IsTracking)
            {
                CheckForTrackingStart();
            }
            else
            {
                CheckForTrackingStop();
                UpdateCursorTracking();
            }
        }

        /// <summary>
        /// Проверяет условия для начала отслеживания
        /// </summary>
        private void CheckForTrackingStart()
        {
            Vector3 mousePosition = Input.mousePosition;
            
            if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
            {
                StartTracking(HandType.Left, mousePosition);
            }
            else if (Input.GetMouseButtonDown(1)) // Правая кнопка мыши
            {
                StartTracking(HandType.Right, mousePosition);
            }
        }

        /// <summary>
        /// Проверяет условия для остановки отслеживания
        /// </summary>
        private void CheckForTrackingStop()
        {
            bool shouldStop = false;
            
            if (ActiveHand == HandType.Left && Input.GetMouseButtonUp(0))
            {
                shouldStop = true;
            }
            else if (ActiveHand == HandType.Right && Input.GetMouseButtonUp(1))
            {
                shouldStop = true;
            }
            
            if (shouldStop)
            {
                StopTracking();
            }
        }

        /// <summary>
        /// Обновляет отслеживание курсора
        /// </summary>
        private void UpdateCursorTracking()
        {
            Vector3 mousePosition = Input.mousePosition;
            _cursorTracker.UpdateCursorPositions(mousePosition);
        }

        /// <summary>
        /// Запускает отслеживание для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки для отслеживания</param>
        /// <param name="initialPosition">Начальная позиция курсора</param>
        private void StartTracking(HandType handType, Vector3 initialPosition)
        {
            _cursorTracker.StartTracking(handType);
            _cursorTracker.UpdateCursorPositions(initialPosition);
            
            // Активация соответствующей руки
            IHandController activeHandController = GetHandController(handType);
            activeHandController?.ActivateHand(_cursorTracker.CursorPositions);
            
            // Обновление зон вращения для активной руки
            UpdateRotationAreas(handType);
        }

        /// <summary>
        /// Останавливает отслеживание и деактивирует руки
        /// </summary>
        private void StopTracking()
        {
            // Деактивация активных рук
            _leftHandController?.DeactivateHand();
            _rightHandController?.DeactivateHand();
            
            _cursorTracker.StopTracking();
        }

        #endregion

        #region System Updates

        /// <summary>
        /// Обновляет состояние всех подсистем
        /// </summary>
        private void UpdateSystems()
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 normalizedPosition = _cursorTracker.GetNormalizedCursorPosition(currentMousePosition);
            Vector3 boundedPosition = _cursorTracker.GetBoundedCursorPosition(currentMousePosition);
            
            // Расчет вектора курсора и нормализованного расстояния
            Vector2 cursorVector = new Vector2(boundedPosition.x, boundedPosition.y) - _screenCenter;
            float normalizedDistance = Mathf.Clamp01(cursorVector.magnitude / _maxScreenDistance);
            
            // Обновление камеры
            UpdateCamera(cursorVector, normalizedDistance);
            
            // Обновление рук
            UpdateHands(normalizedDistance, boundedPosition);
        }

        /// <summary>
        /// Обновляет вращение и эффекты камеры
        /// </summary>
        /// <param name="cursorVector">Вектор курсора относительно центра экрана</param>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        private void UpdateCamera(Vector2 cursorVector, float normalizedDistance)
        {
            _cameraController.UpdateCameraRotation(cursorVector);
            _cameraController.UpdateCameraEffects(normalizedDistance);
        }

        /// <summary>
        /// Обновляет состояние рук
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        /// <param name="boundedPosition">Ограниченная позиция курсора</param>
        private void UpdateHands(float normalizedDistance, Vector3 boundedPosition)
        {
            Ray cursorRay = _mainCamera.ScreenPointToRay(_cursorTracker.DeNormalizeCursorPosition(boundedPosition));
            
            if (IsTracking)
            {
                IHandController activeController = GetActiveHandController();
                if (activeController != null)
                {
                    activeController.UpdateHand(_cursorTracker.CursorPositions, normalizedDistance, cursorRay);
                    activeController.UpdateLastCursorPosition(boundedPosition);
                }
            }
            
            // Обновление неактивных состояний рук
            _leftHandController.UpdateInactiveState();
            _rightHandController.UpdateInactiveState();
        }

        /// <summary>
        /// Обновляет зоны вращения для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки</param>
        private void UpdateRotationAreas(HandType handType)
        {
            HandSettings settings = GetHandSettings(handType);
            IHandController controller = GetHandController(handType);
            
            if (settings?.HandAreas != null && controller != null)
            {
                controller.SetRotationAreas(settings.HandAreas);
            }
        }

        /// <summary>
        /// Обновляет центр для отрисовки гизмосов
        /// </summary>
        private void UpdateGizmoCenter()
        {
            if (_mainCamera != null)
            {
                Vector3 centerScreenPos = new Vector3(
                    CursorTrackingSettings.ReferenceWidth * 0.5f,
                    CursorTrackingSettings.ReferenceHeight * 0.5f,
                    DebugSettings.GIZMO_Z_DEPTH
                );
                
                _gizmoCenter = _mainCamera.ScreenToWorldPoint(
                    _cursorTracker.DeNormalizeCursorPosition(centerScreenPos)
                );
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Получает контроллер руки по типу
        /// </summary>
        /// <param name="handType">Тип руки</param>
        /// <returns>Контроллер руки</returns>
        private IHandController GetHandController(HandType handType)
        {
            return handType == HandType.Left ? _leftHandController : _rightHandController;
        }

        /// <summary>
        /// Получает активный контроллер руки
        /// </summary>
        /// <returns>Активный контроллер руки или null</returns>
        private IHandController GetActiveHandController()
        {
            if (_leftHandController?.IsActive == true)
                return _leftHandController;
            if (_rightHandController?.IsActive == true)
                return _rightHandController;
            return null;
        }

        /// <summary>
        /// Получает настройки для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки</param>
        /// <returns>Настройки руки</returns>
        private HandSettings GetHandSettings(HandType handType)
        {
            return handType == HandType.Left ? _leftHandSettings : _rightHandSettings;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Запускает отслеживание вручную для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки для отслеживания</param>
        /// <param name="initialPosition">Начальная позиция курсора</param>
        public void StartTrackingManual(HandType handType, Vector3 initialPosition)
        {
            if (IsTracking)
            {
                LogWarning("Отслеживание уже активно. Сначала остановите текущее отслеживание.");
                return;
            }
            
            StartTracking(handType, initialPosition);
        }

        /// <summary>
        /// Останавливает отслеживание вручную
        /// </summary>
        public void StopTrackingManual()
        {
            if (!IsTracking)
                return;
                
            StopTracking();
        }

        /// <summary>
        /// Получает текущий массив позиций курсора
        /// </summary>
        /// <returns>Массив позиций курсора</returns>
        public Vector3[] GetCursorPositions()
        {
            return _cursorTracker?.CursorPositions ?? new Vector3[0];
        }

        /// <summary>
        /// Сбрасывает всю систему в начальное состояние
        /// </summary>
        public void ResetSystem()
        {
            if (IsTracking)
            {
                StopTracking();
            }
            
            _leftHandController?.Reset();
            _rightHandController?.Reset();
            _cameraController?.ResetCamera();
            
            if (_debugSettings.EnableLogging)
            {
                Log("Сброс системы завершен");
            }
        }

        /// <summary>
        /// Обновляет настройки руки во время выполнения
        /// </summary>
        /// <param name="handType">Тип руки для обновления</param>
        /// <param name="newSettings">Новые настройки руки</param>
        public void UpdateHandSettings(HandType handType, HandSettings newSettings)
        {
            if (newSettings == null)
                return;
                
            if (handType == HandType.Left)
            {
                _leftHandSettings = newSettings;
                _leftHandController = new HandController(HandType.Left, _leftHandSettings, _mainCamera, _debugSettings.EnableLogging);
            }
            else
            {
                _rightHandSettings = newSettings;
                _rightHandController = new HandController(HandType.Right, _rightHandSettings, _mainCamera, _debugSettings.EnableLogging);
            }
            
            // Переподписка на события
            StopListening();
            StartListening();
            
            if (_debugSettings.EnableLogging)
            {
                Log($"Настройки руки {handType} обновлены");
            }
        }

        /// <summary>
        /// Получает текущую позицию руки в мировых координатах
        /// </summary>
        /// <param name="handType">Тип руки</param>
        /// <returns>Текущая позиция руки</returns>
        public Vector3 GetHandPosition(HandType handType)
        {
            IHandController controller = GetHandController(handType);
            return controller?.GetCurrentPosition() ?? Vector3.zero;
        }

        /// <summary>
        /// Получает текущую ротацию руки
        /// </summary>
        /// <param name="handType">Тип руки</param>
        /// <returns>Текущая ротация руки</returns>
        public Quaternion GetHandRotation(HandType handType)
        {
            IHandController controller = GetHandController(handType);
            return controller?.GetCurrentRotation() ?? Quaternion.identity;
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Отрисовывает отладочные гизмосы для визуализации
        /// </summary>
        private void DrawDebugGizmos()
        {
            if (_cursorTracker == null || _mainCamera == null)
                return;

            DrawCursorPoints();
            DrawRotationAreas();
            DrawFollowingIndicators();
        }

        /// <summary>
        /// Отрисовывает точки курсора в окне Scene
        /// </summary>
        private void DrawCursorPoints()
        {
            Vector3[] positions = _cursorTracker.CursorPositions;
            if (positions == null)
                return;

            Color pointColor = ActiveHand == HandType.Left ? 
                _debugSettings.LeftHandPointColor : 
                _debugSettings.RightHandPointColor;
            
            Gizmos.color = pointColor;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] != Vector3.zero)
                {
                    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
                        _cursorTracker.DeNormalizeCursorPosition(
                            new Vector3(positions[i].x, positions[i].y, DebugSettings.GIZMO_Z_DEPTH)
                        )
                    );
                    Gizmos.DrawSphere(worldPos, _debugSettings.PointSize);
                }
            }
        }

        /// <summary>
        /// Отрисовывает зоны вращения активной руки
        /// </summary>
        private void DrawRotationAreas()
        {
            if (!IsTracking)
                return;
                
            HandSettings activeSettings = GetHandSettings(ActiveHand.Value);
            if (activeSettings?.HandAreas != null && activeSettings.HandAreas.Length > 0)
            {
                var areaManager = new RotationAreaManager(activeSettings.HandAreas);
                areaManager.DrawGizmos(_gizmoCenter, DebugSettings.GIZMO_RADIUS, _debugSettings.HandAreaColor);
            }
        }

        /// <summary>
        /// Отрисовывает индикаторы следования для рук
        /// </summary>
        private void DrawFollowingIndicators()
        {
            DrawHandFollowIndicator(_leftHandController, _debugSettings.LeftHandPointColor);
            DrawHandFollowIndicator(_rightHandController, _debugSettings.RightHandPointColor);
        }

        /// <summary>
        /// Отрисовывает индикатор следования для конкретной руки
        /// </summary>
        /// <param name="controller">Контроллер руки</param>
        /// <param name="color">Цвет индикатора</param>
        private void DrawHandFollowIndicator(IHandController controller, Color color)
        {
            if (controller == null || !controller.IsFollowing)
                return;

            // Отрисовка индикатора на позиции руки
            Vector3 handPos = controller.GetCurrentPosition();
            
            Gizmos.color = color;
            Gizmos.DrawWireSphere(handPos, _debugSettings.PointSize * 2f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(handPos + Vector3.up * 0.5f, $"Следование: {controller.FollowProgress:F2}");
#endif
        }

        #endregion

        #region Logging

        /// <summary>
        /// Логирует сообщение, если включено детальное логирование
        /// </summary>
        /// <param name="message">Сообщение для логирования</param>
        private void Log(string message)
        {
            if (_debugSettings.EnableLogging)
            {
                Debug.Log($"[BatmanCombatController] {message}");
            }
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        /// <param name="message">Сообщение предупреждения</param>
        /// <param name="context">Контекст для логирования</param>
        private void LogWarning(string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning($"[BatmanCombatController] {message}", context);
        }

        /// <summary>
        /// Логирует ошибку
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="context">Контекст для логирования</param>
        private void LogError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"[BatmanCombatController] {message}", context);
        }

        #endregion
    }
}