using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Управляет отслеживанием позиций курсора и их нормализацией для боевой системы.
    /// Обеспечивает функционал хранения и обработки позиций курсора в Batman: Break The Silence.
    /// </summary>
    public sealed class CursorTrackingSystem : ICursorTracker
    {
        #region Events

        /// <summary>
        /// Событие, вызываемое при начале отслеживания для указанной руки
        /// </summary>
        public event Action<HandType> OnTrackingStarted;

        /// <summary>
        /// Событие, вызываемое при остановке отслеживания
        /// </summary>
        public event Action OnTrackingStopped;

        #endregion

        #region Properties

        /// <summary>
        /// Получает состояние отслеживания курсора
        /// </summary>
        public bool IsTracking { get; private set; }

        /// <summary>
        /// Получает массив текущих позиций курсора
        /// </summary>
        public Vector3[] CursorPositions => _cursorPositions;

        /// <summary>
        /// Получает активную руку, связанную с отслеживанием
        /// </summary>
        public HandType ActiveHand { get; private set; }

        #endregion

        #region Private Fields

        private readonly CursorTrackingSettings _settings; // Настройки системы отслеживания курсора
        private Vector3[] _cursorPositions; // Массив позиций курсора
        private int _positionIndex; // Текущий индекс для добавления новой позиции
        private Vector3 _lastValidPosition; // Последняя подтвержденная позиция курсора
        private float _accumulatedDistance; // Накопленное расстояние между точками

        #endregion

        #region Constructor

        /// <summary>
        /// Создает систему отслеживания курсора с заданными настройками
        /// </summary>
        /// <param name="settings">Настройки отслеживания курсора</param>
        public CursorTrackingSystem(CursorTrackingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cursorPositions = new Vector3[_settings.MaxPoints];
            Reset();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Начинает отслеживание курсора для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки (например, левая или правая)</param>
        public void StartTracking(HandType handType)
        {
            if (IsTracking)
            {
                LogWarning($"Уже выполняется отслеживание с рукой {ActiveHand}. Остановка текущего отслеживания.");
                StopTracking();
            }

            IsTracking = true;
            ActiveHand = handType;
            Reset();

            OnTrackingStarted?.Invoke(handType);

            if (_settings.EnableLogging)
            {
                Log($"Начато отслеживание для руки {handType}");
            }
        }

        /// <summary>
        /// Останавливает отслеживание курсора
        /// </summary>
        public void StopTracking()
        {
            if (!IsTracking)
                return;

            IsTracking = false;
            OnTrackingStopped?.Invoke();

            if (_settings.EnableLogging)
            {
                Log($"Остановлено отслеживание для руки {ActiveHand}");
            }
        }

        /// <summary>
        /// Обновляет позиции курсора на основе экранной позиции
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        public void UpdateCursorPositions(Vector3 screenPosition)
        {
            if (!IsTracking)
                return;

            Vector3 normalizedPosition = NormalizeCursorPosition(screenPosition);
            Vector3 boundedPosition = ClampCursorToBounds(normalizedPosition);

            ProcessCursorMovement(boundedPosition);
        }

        /// <summary>
        /// Получает нормализованную позицию курсора относительно центра экрана
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        /// <returns>Нормализованная позиция курсора</returns>
        public Vector3 GetNormalizedCursorPosition(Vector3 screenPosition)
        {
            return NormalizeCursorPosition(screenPosition);
        }

        /// <summary>
        /// Получает позицию курсора, ограниченную пределами экрана
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        /// <returns>Ограниченная позиция курсора</returns>
        public Vector3 GetBoundedCursorPosition(Vector3 screenPosition)
        {
            Vector3 normalizedPosition = NormalizeCursorPosition(screenPosition);
            return ClampCursorToBounds(normalizedPosition);
        }

        /// <summary>
        /// Преобразует нормализованную позицию курсора в экранные координаты
        /// </summary>
        /// <param name="normalizedPosition">Нормализованная позиция курсора</param>
        /// <returns>Позиция курсора в экранных координатах</returns>
        public Vector3 DeNormalizeCursorPosition(Vector3 normalizedPosition)
        {
            float scaleX = Screen.width / (float)CursorTrackingSettings.ReferenceWidth;
            float scaleY = Screen.height / (float)CursorTrackingSettings.ReferenceHeight;
            return new Vector3(normalizedPosition.x * scaleX, normalizedPosition.y * scaleY, normalizedPosition.z);
        }

        /// <summary>
        /// Сбрасывает состояние системы отслеживания
        /// </summary>
        public void Reset()
        {
            Array.Clear(_cursorPositions, 0, _cursorPositions.Length);
            _positionIndex = 0;
            _lastValidPosition = Vector3.zero;
            _accumulatedDistance = 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Нормализует позицию курсора относительно эталонного разрешения
        /// </summary>
        /// <param name="screenPos">Позиция курсора в экранных координатах</param>
        /// <returns>Нормализованная позиция курсора</returns>
        private Vector3 NormalizeCursorPosition(Vector3 screenPos)
        {
            float scaleX = CursorTrackingSettings.ReferenceWidth / (float)Screen.width;
            float scaleY = CursorTrackingSettings.ReferenceHeight / (float)Screen.height;
            return new Vector3(screenPos.x * scaleX, screenPos.y * scaleY, screenPos.z);
        }

        /// <summary>
        /// Ограничивает позицию курсора в пределах экрана с учетом отступов
        /// </summary>
        /// <param name="cursorPos">Нормализованная позиция курсора</param>
        /// <returns>Ограниченная позиция курсора</returns>
        private Vector3 ClampCursorToBounds(Vector3 cursorPos)
        {
            float minX = _settings.HorizontalPadding;
            float maxX = CursorTrackingSettings.ReferenceWidth - _settings.HorizontalPadding;
            float minY = _settings.VerticalPadding;
            float maxY = CursorTrackingSettings.ReferenceHeight - _settings.VerticalPadding;

            float clampedX = Mathf.Clamp(cursorPos.x, minX, maxX);
            float clampedY = Mathf.Clamp(cursorPos.y, minY, maxY);

            return new Vector3(clampedX, clampedY, cursorPos.z);
        }

        /// <summary>
        /// Обрабатывает движение курсора и добавляет новые точки при необходимости
        /// </summary>
        /// <param name="cursorScreenPos">Ограниченная позиция курсора в экранных координатах</param>
        private void ProcessCursorMovement(Vector3 cursorScreenPos)
        {
            if (_lastValidPosition == Vector3.zero)
            {
                _lastValidPosition = cursorScreenPos;
                return;
            }

            // Расчет расстояния между текущей и последней позицией
            float distanceThisFrame = Vector3.Distance(cursorScreenPos, _lastValidPosition);
            _accumulatedDistance += distanceThisFrame;

            // Добавление новых точек, если накопленное расстояние превышает минимальное
            while (_accumulatedDistance >= _settings.MinDistanceBetweenPoints)
            {
                float overshoot = _accumulatedDistance - _settings.MinDistanceBetweenPoints;
                float t = distanceThisFrame > 0 ? (distanceThisFrame - overshoot) / distanceThisFrame : 0f;
                Vector3 newPoint = Vector3.Lerp(_lastValidPosition, cursorScreenPos, t);

                AddCursorPoint(newPoint);

                _lastValidPosition = newPoint;
                _accumulatedDistance = overshoot;
                distanceThisFrame = Vector3.Distance(cursorScreenPos, _lastValidPosition);
            }
        }

        /// <summary>
        /// Добавляет новую точку курсора в массив
        /// </summary>
        /// <param name="point">Позиция новой точки курсора</param>
        private void AddCursorPoint(Vector3 point)
        {
            _cursorPositions[_positionIndex] = point;
            _positionIndex = (_positionIndex + 1) % _settings.MaxPoints;

            if (_settings.EnableLogging)
            {
                Log($"Добавлена точка курсора: {point} на индекс {_positionIndex}");
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Логирует сообщение, если включено детальное логирование
        /// </summary>
        /// <param name="message">Сообщение для логирования</param>
        private void Log(string message)
        {
            if (_settings.EnableLogging)
            {
                Debug.Log($"[CursorTrackingSystem] {message}");
            }
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        /// <param name="message">Сообщение предупреждения</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[CursorTrackingSystem] {message}");
        }

        #endregion
    }
}