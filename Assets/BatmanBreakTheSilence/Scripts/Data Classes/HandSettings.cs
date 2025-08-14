using System;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Настройки поведения руки для управления позицией, вращением и следованием за курсором.
    /// Используется для конфигурации руки в боевой системе Batman: Break The Silence.
    /// </summary>
    [Serializable]
    public class HandSettings
    {
        #region Serialized Fields

        [Header("Ссылки на трансформы")]
        [SerializeField, Tooltip("Трансформ руки в сцене")]
        public Transform HandTransform; // Трансформ, представляющий руку

        [Header("Настройки позиционирования")]
        [SerializeField, Tooltip("Минимальная глубина позиции руки от камеры (в единицах мира)"), Min(0f)]
        public float MinDepth = 1f; // Минимальное расстояние от камеры

        [SerializeField, Tooltip("Максимальная глубина позиции руки от камеры (в единицах мира)"), Min(0f)]
        public float MaxDepth = 10f; // Максимальное расстояние от камеры

        [SerializeField, Tooltip("Кривая интерполяции глубины в зависимости от нормализованного расстояния курсора (0-1)")]
        public AnimationCurve DepthCurve = AnimationCurve.Linear(0, 0, 1, 1); // Кривая для управления глубиной

        [Header("Настройки анимации")]
        [SerializeField, Tooltip("Время сглаживания движения активной руки (в секундах)"), Min(0f)]
        public float SmoothTime = 0.1f; // Время для плавного перемещения руки

        [SerializeField, Tooltip("Скорость вращения руки и предплечья (градусов в секунду)"), Min(0f)]
        public float RotationSpeed = 180f; // Базовая скорость вращения

        [SerializeField, Tooltip("Кривая множителя скорости вращения в зависимости от количества точек курсора (0 = 0 точек, 1 = максимум точек)")]
        public AnimationCurve RotationSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1); // Кривая для модификации скорости вращения

        [Header("Зоны вращения")]
        [SerializeField, Tooltip("Массив зон вращения для руки")]
        public RotationArea[] HandAreas = new RotationArea[0]; // Зоны, определяющие ориентацию руки

        [Header("Поведение следования")]
        [SerializeField, Tooltip("Время следования руки за курсором после деактивации (в секундах)"), Min(0f)]
        public float FollowTime = 0.5f; // Длительность следования после деактивации

        [SerializeField, Tooltip("Сила следования за курсором после деактивации (0-1)"), Range(0f, 1f)]
        public float FollowStrength = 0.5f; // Интенсивность следования

        [SerializeField, Tooltip("Кривая интерполяции для следования и возврата руки (0-1)")]
        public AnimationCurve FollowCurve = AnimationCurve.Linear(0, 1, 1, 0); // Кривая для сглаживания следования

        #endregion
    }
}