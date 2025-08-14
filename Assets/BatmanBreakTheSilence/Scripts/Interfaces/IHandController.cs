using System;
using UnityEngine;
using UnityEngine.Events;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Интерфейс для управления и анимации руки.
    /// Определяет методы и свойства для активации, обновления и управления движением руки в Batman: Break The Silence.
    /// </summary>
    public interface IHandController
    {
        /// <summary>
        /// Получает состояние активности руки
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Получает, следует ли рука за курсором
        /// </summary>
        bool IsFollowing { get; }

        /// <summary>
        /// Получает прогресс следования руки за курсором (0-1)
        /// </summary>
        float FollowProgress { get; }

        /// <summary>
        /// Получает тип руки (например, левая или правая)
        /// </summary>
        HandType HandType { get; }

        /// <summary>
        /// Активирует руку на основе позиций курсора
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора в пространстве</param>
        void ActivateHand(Vector3[] cursorPositions);

        /// <summary>
        /// Деактивирует руку
        /// </summary>
        void DeactivateHand();

        /// <summary>
        /// Обновляет состояние руки на основе позиций курсора, расстояния и луча
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора</param>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра (0-1)</param>
        /// <param name="cursorRay">Луч курсора для определения направления</param>
        void UpdateHand(Vector3[] cursorPositions, float normalizedDistance, Ray cursorRay);

        /// <summary>
        /// Обновляет состояние руки в неактивном режиме
        /// </summary>
        void UpdateInactiveState();

        /// <summary>
        /// Устанавливает зоны вращения для руки
        /// </summary>
        /// <param name="areas">Массив зон вращения</param>
        void SetRotationAreas(RotationArea[] areas);

        /// <summary>
        /// Обновляет последнюю позицию курсора
        /// </summary>
        /// <param name="cursorPosition">Последняя позиция курсора</param>
        void UpdateLastCursorPosition(Vector3 cursorPosition);

        /// <summary>
        /// Получает текущую позицию руки
        /// </summary>
        /// <returns>Вектор текущей позиции руки</returns>
        Vector3 GetCurrentPosition();

        /// <summary>
        /// Получает текущую ротацию руки
        /// </summary>
        /// <returns>Кватернион текущей ротации руки</returns>
        Quaternion GetCurrentRotation();

        /// <summary>
        /// Сбрасывает руку в начальное состояние
        /// </summary>
        void Reset();

        /// <summary>
        /// Событие, вызываемое при активации руки
        /// </summary>
        event Action<HandType> OnHandActivated;

        /// <summary>
        /// Событие, вызываемое при деактивации руки
        /// </summary>
        event Action<HandType> OnHandDeactivated;
    }
}