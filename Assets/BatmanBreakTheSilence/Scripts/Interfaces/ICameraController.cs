using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Интерфейс для систем управления камерой.
    /// Определяет методы для обновления вращения камеры, эффектов и сброса состояния в Batman: Break The Silence.
    /// </summary>
    public interface ICameraController
    {
        /// <summary>
        /// Обновляет вращение камеры на основе вектора курсора.
        /// </summary>
        /// <param name="cursorVector">Вектор позиции курсора относительно центра экрана</param>
        void UpdateCameraRotation(Vector2 cursorVector);

        /// <summary>
        /// Обновляет эффекты камеры (например, поле зрения или постобработку) на основе нормализованного расстояния.
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра (0-1)</param>
        void UpdateCameraEffects(float normalizedDistance);

        /// <summary>
        /// Сбрасывает камеру в начальное состояние.
        /// </summary>
        void ResetCamera();
    }
}