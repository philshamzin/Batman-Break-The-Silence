using System;
using System.Linq;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Доступные типы рук для боевой системы
    /// </summary>
    public enum HandType
    {
        Right, // Правая рука
        Left   // Левая рука
    }

    /// <summary>
    /// Управляет поведением отдельной руки, включая позиционирование, вращение и механику следования.
    /// Реализует функционал для активации, деактивации и обновления состояния руки в Batman: Break The Silence.
    /// </summary>
    public sealed class HandController : IHandController
    {
        #region Events

        /// <summary>
        /// Событие, вызываемое при активации руки
        /// </summary>
        public event Action<HandType> OnHandActivated;

        /// <summary>
        /// Событие, вызываемое при деактивации руки
        /// </summary>
        public event Action<HandType> OnHandDeactivated;

        #endregion

        #region Properties

        /// <summary>
        /// Получает состояние активности руки
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Получает тип руки (правая или левая)
        /// </summary>
        public HandType HandType { get; }

        /// <summary>
        /// Получает, следует ли рука за курсором
        /// </summary>
        public bool IsFollowing => _handState.IsFollowing;

        /// <summary>
        /// Получает прогресс следования руки за курсором (0-1)
        /// </summary>
        public float FollowProgress => _handState.FollowProgress;

        #endregion

        #region Private Fields

        private readonly HandSettings _settings; // Настройки поведения руки
        private readonly Camera _camera; // Камера для преобразования координат
        private readonly bool _enableLogging; // Флаг для включения детального логирования
        private IRotationAreaManager _areaManager; // Менеджер зон вращения
        private HandState _handState; // Состояние руки

        #endregion

        #region Constructor

        /// <summary>
        /// Создает контроллер руки с заданными параметрами
        /// </summary>
        /// <param name="handType">Тип руки (правая или левая)</param>
        /// <param name="settings">Настройки поведения руки</param>
        /// <param name="camera">Камера для расчета позиций</param>
        /// <param name="enableLogging">Включить детальное логирование</param>
        public HandController(
            HandType handType, 
            HandSettings settings, 
            Camera camera, 
            bool enableLogging = false)
        {
            HandType = handType;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _enableLogging = enableLogging;

            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Активирует руку на основе позиций курсора
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора в экранных координатах</param>
        public void ActivateHand(Vector3[] cursorPositions)
        {
            if (IsActive)
                return;

            IsActive = true;
            _handState.IsActive = true;
            _handState.IsFollowing = false;
            _handState.FollowProgress = 0f;

            // Создание менеджера зон вращения, если зоны указаны
            if (_settings.HandAreas != null && _settings.HandAreas.Length > 0)
            {
                _areaManager = new RotationAreaManager(_settings.HandAreas, _enableLogging);
            }

            OnHandActivated?.Invoke(HandType);

            if (_enableLogging)
            {
                Log($"Рука {HandType} активирована");
            }
        }

        /// <summary>
        /// Деактивирует руку и запускает поведение следования
        /// </summary>
        public void DeactivateHand()
        {
            if (!IsActive)
                return;

            IsActive = false;
            _handState.IsActive = false;
            _handState.IsFollowing = true;
            _handState.FollowProgress = 0f;

            OnHandDeactivated?.Invoke(HandType);

            if (_enableLogging)
            {
                Log($"Рука {HandType} деактивирована, начато поведение следования");
            }
        }

        /// <summary>
        /// Обновляет состояние активной руки на основе позиций курсора, расстояния и луча
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора</param>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра (0-1)</param>
        /// <param name="cursorRay">Луч курсора для определения позиции</param>
        public void UpdateHand(Vector3[] cursorPositions, float normalizedDistance, Ray cursorRay)
        {
            if (!IsActive || _settings.HandTransform == null)
                return;

            UpdateActiveHand(cursorPositions, normalizedDistance, cursorRay);
        }

        /// <summary>
        /// Обновляет состояние руки в неактивном режиме
        /// </summary>
        public void UpdateInactiveState()
        {
            if (IsActive)
                return;

            if (_handState.IsFollowing)
            {
                UpdateFollowBehavior();
            }
            else
            {
                UpdateReturnToRest();
            }
        }

        /// <summary>
        /// Устанавливает новые зоны вращения для руки
        /// </summary>
        /// <param name="areas">Массив зон вращения</param>
        public void SetRotationAreas(RotationArea[] areas)
        {
            if (areas != null && areas.Length > 0)
            {
                _areaManager = new RotationAreaManager(areas, _enableLogging);
                
                if (_enableLogging)
                {
                    Log($"Зоны вращения руки {HandType} обновлены ({areas.Length} зон)");
                }
            }
        }

        /// <summary>
        /// Обновляет последнюю позицию курсора
        /// </summary>
        /// <param name="cursorPosition">Позиция курсора в экранных координатах</param>
        public void UpdateLastCursorPosition(Vector3 cursorPosition)
        {
            _handState.LastCursorPosition = cursorPosition;
        }

        /// <summary>
        /// Получает текущую позицию руки
        /// </summary>
        /// <returns>Позиция руки или Vector3.zero, если трансформ не задан</returns>
        public Vector3 GetCurrentPosition()
        {
            return _settings.HandTransform?.position ?? Vector3.zero;
        }

        /// <summary>
        /// Получает текущую ротацию руки
        /// </summary>
        /// <returns>Ротация руки или Quaternion.identity, если трансформ не задан</returns>
        public Quaternion GetCurrentRotation()
        {
            return _settings.HandTransform?.rotation ?? Quaternion.identity;
        }

        /// <summary>
        /// Сбрасывает руку в начальное состояние
        /// </summary>
        public void Reset()
        {
            if (_settings.HandTransform != null)
            {
                _settings.HandTransform.position = _handState.InitialPosition;
                _settings.HandTransform.rotation = _handState.InitialHandRotation;
            }

            _handState.IsActive = false;
            _handState.IsFollowing = false;
            _handState.FollowProgress = 0f;
            _handState.CurrentVelocity = Vector3.zero;
            
            IsActive = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализирует контроллер руки
        /// </summary>
        private void Initialize()
        {
            ValidateSettings();
            InitializeHandState();

            if (_enableLogging)
            {
                Log($"Контроллер руки {HandType} инициализирован");
            }
        }

        /// <summary>
        /// Проверяет корректность настроек руки
        /// </summary>
        private void ValidateSettings()
        {
            if (_settings.HandTransform == null)
            {
                throw new InvalidOperationException($"Трансформ руки обязателен для {HandType}");
            }

            if (_settings.MinDepth >= _settings.MaxDepth)
            {
                LogWarning($"Некорректные настройки глубины для руки {HandType}. MinDepth должен быть меньше MaxDepth.");
            }
        }

        /// <summary>
        /// Инициализирует состояние руки
        /// </summary>
        private void InitializeHandState()
        {
            _handState = new HandState
            {
                InitialPosition = _settings.HandTransform.position,
                InitialHandRotation = _settings.HandTransform.rotation,
                TargetPosition = _settings.HandTransform.position,
                TargetHandRotation = _settings.HandTransform.rotation,
                IsActive = false,
                IsFollowing = false,
                FollowProgress = 0f,
                LastCursorPosition = Vector3.zero,
                CurrentVelocity = Vector3.zero
            };
        }

        /// <summary>
        /// Обновляет состояние активной руки
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора</param>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        /// <param name="cursorRay">Луч курсора для позиционирования</param>
        private void UpdateActiveHand(Vector3[] cursorPositions, float normalizedDistance, Ray cursorRay)
        {
            // Расчет множителя скорости вращения
            int activePoints = cursorPositions.Count(pos => pos != Vector3.zero);
            float pointRatio = activePoints / (float)cursorPositions.Length;
            float rotationSpeedMultiplier = _settings.RotationSpeedCurve.Evaluate(pointRatio);

            // Обновление позиции
            UpdateHandPosition(normalizedDistance, cursorRay);

            // Обновление вращений
            UpdateHandRotations(cursorPositions, normalizedDistance, rotationSpeedMultiplier);
        }

        /// <summary>
        /// Обновляет позицию руки на основе луча и расстояния
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        /// <param name="cursorRay">Луч курсора</param>
        private void UpdateHandPosition(float normalizedDistance, Ray cursorRay)
        {
            float depth = Mathf.Lerp(
                _settings.MaxDepth, 
                _settings.MinDepth, 
                _settings.DepthCurve.Evaluate(normalizedDistance)
            );
            
            _handState.TargetPosition = cursorRay.GetPoint(depth);

            // Плавное перемещение руки
            _settings.HandTransform.position = Vector3.SmoothDamp(
                _settings.HandTransform.position,
                _handState.TargetPosition,
                ref _handState.CurrentVelocity,
                _settings.SmoothTime
            );
        }

        /// <summary>
        /// Обновляет вращение руки на основе позиций курсора и множителя скорости
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора</param>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        /// <param name="speedMultiplier">Множитель скорости вращения</param>
        private void UpdateHandRotations(Vector3[] cursorPositions, float normalizedDistance, float speedMultiplier)
        {
            if (_areaManager != null)
            {
                _areaManager.CalculateWeights(cursorPositions);
                _handState.TargetHandRotation = _areaManager.ComputeWeightedRotation(
                    normalizedDistance, 
                    _handState.InitialHandRotation
                );
            }
            else
            {
                _handState.TargetHandRotation = _handState.InitialHandRotation;
            }

            // Плавное вращение руки
            _settings.HandTransform.rotation = Quaternion.RotateTowards(
                _settings.HandTransform.rotation,
                _handState.TargetHandRotation,
                _settings.RotationSpeed * speedMultiplier * Time.deltaTime
            );
        }

        /// <summary>
        /// Обновляет поведение следования руки за курсором
        /// </summary>
        private void UpdateFollowBehavior()
        {
            _handState.FollowProgress = Mathf.Clamp01(
                _handState.FollowProgress + Time.deltaTime / _settings.FollowTime
            );

            float followWeight = _settings.FollowCurve.Evaluate(_handState.FollowProgress);
            float effectiveFollowStrength = _settings.FollowStrength * followWeight;
            float returnWeight = 1f - followWeight;

            UpdateFollowPosition(effectiveFollowStrength, returnWeight);
            UpdateFollowRotations(returnWeight);

            // Проверка завершения поведения следования
            if (_handState.FollowProgress >= 1f)
            {
                _handState.IsFollowing = false;
                _handState.FollowProgress = 0f;

                if (_enableLogging)
                {
                    Log($"Поведение следования руки {HandType} завершено");
                }
            }
        }

        /// <summary>
        /// Обновляет позицию руки в режиме следования
        /// </summary>
        /// <param name="effectiveFollowStrength">Эффективная сила следования</param>
        /// <param name="returnWeight">Вес возврата к начальной позиции</param>
        private void UpdateFollowPosition(float effectiveFollowStrength, float returnWeight)
        {
            if (_handState.LastCursorPosition == Vector3.zero || _camera == null)
            {
                _handState.TargetPosition = _handState.InitialPosition;
            }
            else
            {
                // Расчет позиции курсора в мировых координатах
                Vector2 screenCenter = new Vector2(
                    CursorTrackingSettings.ReferenceWidth * 0.5f,
                    CursorTrackingSettings.ReferenceHeight * 0.5f
                );
                
                Vector2 cursorVec = new Vector2(
                    _handState.LastCursorPosition.x, 
                    _handState.LastCursorPosition.y
                ) - screenCenter;
                
                float normalizedDistance = Mathf.Clamp01(cursorVec.magnitude / screenCenter.magnitude);
                
                // Преобразование в экранные координаты для луча
                Vector3 screenPos = new Vector3(
                    _handState.LastCursorPosition.x * Screen.width / CursorTrackingSettings.ReferenceWidth,
                    _handState.LastCursorPosition.y * Screen.height / CursorTrackingSettings.ReferenceHeight,
                    0
                );
                
                Ray ray = _camera.ScreenPointToRay(screenPos);
                float depth = Mathf.Lerp(_settings.MaxDepth, _settings.MinDepth, _settings.DepthCurve.Evaluate(normalizedDistance));
                Vector3 cursorWorldPos = ray.GetPoint(depth);

                Vector3 cursorTarget = Vector3.Lerp(_handState.InitialPosition, cursorWorldPos, effectiveFollowStrength);
                _handState.TargetPosition = Vector3.Lerp(cursorTarget, _handState.InitialPosition, returnWeight);
            }

            // Плавное перемещение руки
            _settings.HandTransform.position = Vector3.SmoothDamp(
                _settings.HandTransform.position,
                _handState.TargetPosition,
                ref _handState.CurrentVelocity,
                _settings.SmoothTime
            );
        }

        /// <summary>
        /// Обновляет вращение руки в режиме следования
        /// </summary>
        /// <param name="returnWeight">Вес возврата к начальной ротации</param>
        private void UpdateFollowRotations(float returnWeight)
        {
            int activePoints = 1; // Предполагается одна точка в режиме следования
            float rotationSpeedMultiplier = _settings.RotationSpeedCurve.Evaluate(activePoints / (float)5); // Предполагается максимум 5 точек

            // Интерполяция между целевой и начальной ротацией
            Quaternion targetHandRotation = Quaternion.Slerp(
                _handState.TargetHandRotation, 
                _handState.InitialHandRotation, 
                returnWeight
            );

            _settings.HandTransform.rotation = Quaternion.RotateTowards(
                _settings.HandTransform.rotation,
                targetHandRotation,
                _settings.RotationSpeed * rotationSpeedMultiplier * Time.deltaTime
            );
        }

        /// <summary>
        /// Возвращает руку в начальное состояние
        /// </summary>
        private void UpdateReturnToRest()
        {
            float rotationSpeedMultiplier = _settings.RotationSpeedCurve.Evaluate(0f);

            // Плавное возвращение к начальной позиции
            _settings.HandTransform.position = Vector3.SmoothDamp(
                _settings.HandTransform.position,
                _handState.InitialPosition,
                ref _handState.CurrentVelocity,
                _settings.SmoothTime
            );

            // Плавное возвращение к начальной ротации
            _settings.HandTransform.rotation = Quaternion.RotateTowards(
                _settings.HandTransform.rotation,
                _handState.InitialHandRotation,
                _settings.RotationSpeed * rotationSpeedMultiplier * Time.deltaTime
            );
        }

        #endregion

        #region Logging

        /// <summary>
        /// Логирует сообщение, если включено детальное логирование
        /// </summary>
        /// <param name="message">Сообщение для логирования</param>
        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[HandController] {message}");
            }
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        /// <param name="message">Сообщение предупреждения</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[HandController] {message}");
        }

        #endregion

        #region Hand State Class

        /// <summary>
        /// Внутренний класс для хранения состояния руки
        /// </summary>
        private class HandState
        {
            public Vector3 InitialPosition; // Начальная позиция руки
            public Quaternion InitialHandRotation; // Начальная ротация руки
            public Vector3 TargetPosition; // Целевая позиция руки
            public Vector3 CurrentVelocity; // Текущая скорость для сглаживания
            public Quaternion TargetHandRotation; // Целевая ротация руки
            public bool IsActive; // Состояние активации
            public bool IsFollowing; // Состояние следования за курсором
            public float FollowProgress; // Прогресс следования (0-1)
            public Vector3 LastCursorPosition; // Последняя позиция курсора (нормализованные координаты 1920x1080)
        }

        #endregion
    }
}