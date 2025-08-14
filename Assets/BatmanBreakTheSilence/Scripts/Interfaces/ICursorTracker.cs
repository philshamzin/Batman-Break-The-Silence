using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Интерфейс для функционала отслеживания курсора в боевой системе.
    /// Определяет методы и свойства для управления отслеживанием курсора и обработки его позиций в Batman: Break The Silence.
    /// </summary>
    public interface ICursorTracker
    {
        /// <summary>
        /// Событие, вызываемое при начале отслеживания для указанной руки
        /// </summary>
        event Action<HandType> OnTrackingStarted;

        /// <summary>
        /// Событие, вызываемое при остановке отслеживания
        /// </summary>
        event Action OnTrackingStopped;

        /// <summary>
        /// Получает состояние отслеживания курсора
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        /// Получает массив текущих позиций курсора
        /// </summary>
        Vector3[] CursorPositions { get; }

        /// <summary>
        /// Получает активную руку, связанную с отслеживанием
        /// </summary>
        HandType ActiveHand { get; }

        /// <summary>
        /// Начинает отслеживание курсора для указанной руки
        /// </summary>
        /// <param name="handType">Тип руки (например, левая или правая)</param>
        void StartTracking(HandType handType);

        /// <summary>
        /// Останавливает отслеживание курсора
        /// </summary>
        void StopTracking();

        /// <summary>
        /// Обновляет позиции курсора на основе экранной позиции
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        void UpdateCursorPositions(Vector3 screenPosition);

        /// <summary>
        /// Получает нормализованную позицию курсора относительно центра экрана
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        /// <returns>Нормализованная позиция курсора (0-1)</returns>
        Vector3 GetNormalizedCursorPosition(Vector3 screenPosition);

        /// <summary>
        /// Получает позицию курсора, ограниченную пределами экрана
        /// </summary>
        /// <param name="screenPosition">Позиция курсора в экранных координатах</param>
        /// <returns>Ограниченная позиция курсора в экранных координатах</returns>
        Vector3 GetBoundedCursorPosition(Vector3 screenPosition);

        /// <summary>
        /// Преобразует нормализованную позицию курсора в экранные координаты
        /// </summary>
        /// <param name="normalizedPosition">Нормализованная позиция курсора (0-1)</param>
        /// <returns>Позиция курсора в экранных координатах</returns>
        Vector3 DeNormalizeCursorPosition(Vector3 normalizedPosition);

        /// <summary>
        /// Сбрасывает состояние отслеживания курсора
        /// </summary>
        void Reset();
    }
}