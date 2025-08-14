using System;
using UnityEngine;
using UnityEngine.Events;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Интерфейс для систем расчета зон вращения.
    /// Определяет методы для вычисления весов позиций курсора, расчета взвешенной ротации и визуализации зон в Batman: Break The Silence.
    /// </summary>
    public interface IRotationAreaManager
    {
        /// <summary>
        /// Вычисляет веса для массива позиций курсора.
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора в пространстве</param>
        /// <returns>Массив весов, соответствующих позициям курсора</returns>
        float[] CalculateWeights(Vector3[] cursorPositions);

        /// <summary>
        /// Вычисляет взвешенную ротацию на основе нормализованного расстояния и резервной ротации.
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра (0-1)</param>
        /// <param name="fallbackRotation">Резервная ротация при отсутствии данных</param>
        /// <returns>Рассчитанная кватернионная ротация</returns>
        Quaternion ComputeWeightedRotation(float normalizedDistance, Quaternion fallbackRotation);

        /// <summary>
        /// Отрисовывает гизмосы для визуализации зоны вращения.
        /// </summary>
        /// <param name="center">Центр зоны вращения</param>
        /// <param name="radius">Радиус зоны в единицах Unity</param>
        /// <param name="areaColor">Цвет зоны для отображения</param>
        void DrawGizmos(Vector3 center, float radius, Color areaColor);
    }
}