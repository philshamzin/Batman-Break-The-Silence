using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Настройки поведения камеры для управления вращением, полем зрения и эффектами постобработки.
    /// Используется для конфигурации камеры в боевой системе Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class CameraControlSettings
    {
        #region Serialized Fields

        [Header("Настройки следования камеры")]
        [SerializeField, Range(0f, 1f), Tooltip("Сила отклонения камеры (0-1, где 1 = полный поворот к курсору)")]
        public float DeviationStrength = 0.25f; // Сила реакции камеры на движение курсора

        [SerializeField, Tooltip("Время сглаживания поворота камеры (в секундах)"), Min(0f)]
        public float SmoothTime = 10f; // Время для плавного вращения камеры

        [SerializeField, Tooltip("Максимальный угол поворота камеры по оси Y (в градусах)"), Min(0f)]
        public float MaxYawAngle = 40f; // Максимальный угол рыскания

        [SerializeField, Tooltip("Максимальный угол поворота камеры по оси X (в градусах)"), Min(0f)]
        public float MaxPitchAngle = 25f; // Максимальный угол тангажа

        [Header("Настройки поля зрения (FOV)")]
        [SerializeField, Tooltip("Минимальное поле зрения камеры (в градусах)"), Min(0f)]
        public float MinFOV = 40f; // Минимальное значение FOV

        [SerializeField, Tooltip("Максимальное поле зрения камеры (в градусах)"), Min(0f)]
        public float MaxFOV = 60f; // Максимальное значение FOV

        [SerializeField, Tooltip("Кривая интерполяции FOV в зависимости от расстояния курсора (0 = центр, 1 = край)")]
        public AnimationCurve FOVCurve = AnimationCurve.Linear(0, 0, 1, 1); // Кривая для изменения FOV

        [SerializeField, Tooltip("Время сглаживания изменения FOV (в секундах)"), Min(0f)]
        public float FOVSmoothTime = 0.5f; // Время для плавного изменения FOV

        [Header("Настройки хроматической аберрации")]
        [SerializeField, Tooltip("Минимальная интенсивность хроматической аберрации (0-1)"), Range(0f, 1f)]
        public float MinChromaticAberration = 0f; // Минимальная интенсивность эффекта

        [SerializeField, Tooltip("Максимальная интенсивность хроматической аберрации (0-1)"), Range(0f, 1f)]
        public float MaxChromaticAberration = 1f; // Максимальная интенсивность эффекта

        [SerializeField, Tooltip("Кривая интерполяции хроматической аберрации в зависимости от расстояния курсора (0 = центр, 1 = край)")]
        public AnimationCurve ChromaticAberrationCurve = AnimationCurve.Linear(0, 0, 1, 1); // Кривая для изменения интенсивности

        [SerializeField, Tooltip("Время сглаживания изменения интенсивности хроматической аберрации (в секундах)"), Min(0f)]
        public float ChromaticAberrationSmoothTime = 0.5f; // Время для плавного изменения эффекта

        #endregion
    }
}