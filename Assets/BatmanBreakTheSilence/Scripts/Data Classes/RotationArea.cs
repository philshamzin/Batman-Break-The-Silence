using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Конфигурация зон вращения для управления ориентацией руки.
    /// Определяет центральные и внешние углы вращения, а также граничный угол области в Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class RotationArea
    {
        #region Serialized Fields

        [SerializeField, Tooltip("Центральные углы Эйлера для вращения руки (в градусах)")]
        public Vector3 CenterHandEuler; // Углы Эйлера для центральной ориентации руки

        [SerializeField, Tooltip("Внешние углы Эйлера для вращения руки (в градусах)")]
        public Vector3 OuterHandEuler; // Углы Эйлера для внешней ориентации руки

        [SerializeField, Tooltip("Граничный угол области в градусах (0-360)"), Range(0f, 360f)]
        public float BoundaryAngle; // Граничный угол области вращения

        #endregion

        #region Properties

        /// <summary>
        /// Получает кватернион центральной ротации на основе углов Эйлера
        /// </summary>
        public Quaternion CenterRotation => Quaternion.Euler(CenterHandEuler);

        /// <summary>
        /// Получает кватернион внешней ротации на основе углов Эйлера
        /// </summary>
        public Quaternion OuterRotation => Quaternion.Euler(OuterHandEuler);

        #endregion
    }
}