using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Настройки для отладки и визуализации в боевой системе.
    /// Определяет параметры отображения гизмосов и логирования в Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class DebugSettings
    {
        #region Serialized Fields

        [Header("Настройки визуализации отладки")]
        [SerializeField, Tooltip("Размер точек курсора в окне Scene View (в единицах мира)"), Min(0f)]
        public float PointSize = 0.1f; // Размер точек для визуализации позиций курсора

        [SerializeField, Tooltip("Цвет точек курсора для правой руки")]
        public Color RightHandPointColor = Color.red; // Цвет для обозначения правой руки

        [SerializeField, Tooltip("Цвет точек курсора для левой руки")]
        public Color LeftHandPointColor = Color.blue; // Цвет для обозначения левой руки

        [SerializeField, Tooltip("Цвет линий зон вращения для рук")]
        public Color HandAreaColor = Color.green; // Цвет линий, обозначающих зоны вращения

        [SerializeField, Tooltip("Включить детальное логирование отладочной информации")]
        public bool EnableLogging = false; // Флаг для включения логирования

        #endregion

        #region Constants

        public const float GIZMO_RADIUS = 2f; // Константа радиуса гизмосов
        public const float GIZMO_Z_DEPTH = 5f; // Константа глубины гизмосов по оси Z

        #endregion
    }
}