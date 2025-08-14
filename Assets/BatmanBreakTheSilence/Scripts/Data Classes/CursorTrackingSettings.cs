using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Настройки для системы отслеживания курсора.
    /// Определяет параметры для управления количеством точек, расстояниями и границами экрана в Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class CursorTrackingSettings
    {
        #region Serialized Fields

        [Header("Настройки отслеживания курсора")]
        [SerializeField, Range(2, 100), Tooltip("Максимальное количество точек для отслеживания курсора")]
        public int MaxPoints = 5; // Максимальное количество точек для хранения позиций курсора

        [SerializeField, Tooltip("Минимальное расстояние между точками курсора (в пикселях, относительно 1920x1080)"), Min(0f)]
        public float MinDistanceBetweenPoints = 10f; // Минимальное расстояние между последовательными точками

        [SerializeField, Tooltip("Отступ от левого и правого края экрана (в пикселях, относительно 1920x1080)"), Min(0f)]
        public float HorizontalPadding = 50f; // Горизонтальный отступ от краев экрана

        [SerializeField, Tooltip("Отступ от верхнего и нижнего края экрана (в пикселях, относительно 1920x1080)"), Min(0f)]
        public float VerticalPadding = 50f; // Вертикальный отступ от краев экрана

        [SerializeField, Tooltip("Включить детальное логирование для системы отслеживания курсора")]
        public bool EnableLogging = false; // Флаг для включения отладочного логирования

        #endregion

        #region Constants

        public const int ReferenceWidth = 1920; // Эталонная ширина экрана для нормализации
        public const int ReferenceHeight = 1080; // Эталонная высота экрана для нормализации

        #endregion
    }
}