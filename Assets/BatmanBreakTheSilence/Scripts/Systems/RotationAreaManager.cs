using System;
using System.Linq;
using UnityEngine;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Управляет зонами вращения и вычислением весов для позиционирования руки.
    /// Обеспечивает расчет взвешенной ротации и визуализацию зон в Batman: Break The Silence.
    /// </summary>
    public sealed class RotationAreaManager : IRotationAreaManager
    {
        #region Private Fields

        private readonly RotationArea[] _areas; // Массив зон вращения
        private readonly float[] _weights; // Веса для каждой зоны вращения
        private readonly Vector2 _screenCenter; // Центр экрана в пикселях
        private readonly bool _enableLogging; // Флаг для включения детального логирования

        #endregion

        #region Properties

        /// <summary>
        /// Получает количество зон вращения
        /// </summary>
        public int AreaCount => _areas?.Length ?? 0;

        /// <summary>
        /// Получает массив зон вращения
        /// </summary>
        public RotationArea[] Areas => _areas;

        #endregion

        #region Constructor

        /// <summary>
        /// Создает менеджер зон вращения с заданными зонами
        /// </summary>
        /// <param name="areas">Массив зон вращения</param>
        /// <param name="enableLogging">Включить детальное логирование</param>
        public RotationAreaManager(RotationArea[] areas, bool enableLogging = false)
        {
            if (areas == null)
                throw new ArgumentNullException(nameof(areas));

            _areas = areas.OrderBy(a => a.BoundaryAngle).ToArray();
            _weights = new float[_areas.Length];
            _screenCenter = new Vector2(
                CursorTrackingSettings.ReferenceWidth * 0.5f, 
                CursorTrackingSettings.ReferenceHeight * 0.5f
            );
            _enableLogging = enableLogging;

            if (_enableLogging)
            {
                Log($"Инициализирован с {_areas.Length} зонами вращения");
                for (int i = 0; i < _areas.Length; i++)
                {
                    Log($"Зона {i}: Граница={_areas[i].BoundaryAngle}°, Центр={_areas[i].CenterHandEuler}, Внешняя={_areas[i].OuterHandEuler}");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Вычисляет веса зон на основе позиций курсора
        /// </summary>
        /// <param name="cursorPositions">Массив позиций курсора в экранных координатах</param>
        /// <returns>Массив весов для каждой зоны</returns>
        public float[] CalculateWeights(Vector3[] cursorPositions)
        {
            if (_areas.Length == 0 || cursorPositions == null)
                return _weights;

            Array.Clear(_weights, 0, _weights.Length);

            foreach (Vector3 pos in cursorPositions)
            {
                if (pos == Vector3.zero) 
                    continue;

                float angle = CalculateAngle(pos);
                CalculateWeightsForPosition(angle);
            }

            NormalizeWeights();
            return _weights;
        }

        /// <summary>
        /// Вычисляет взвешенную ротацию на основе нормализованного расстояния и резервной ротации
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра (0-1)</param>
        /// <param name="fallbackRotation">Резервная ротация при отсутствии данных</param>
        /// <returns>Рассчитанная кватернионная ротация</returns>
        public Quaternion ComputeWeightedRotation(float normalizedDistance, Quaternion fallbackRotation)
        {
            if (_areas.Length == 0)
                return fallbackRotation;

            Quaternion blendedRotation = Quaternion.identity;
            bool hasValidRotation = false;

            for (int i = 0; i < _areas.Length; i++)
            {
                if (_weights[i] <= 0f) 
                    continue;

                RotationArea area = _areas[i];
                // Интерполяция между центральной и внешней ротацией зоны
                Quaternion interpolatedRotation = Quaternion.Slerp(
                    area.CenterRotation, 
                    area.OuterRotation, 
                    normalizedDistance
                );

                if (!hasValidRotation)
                {
                    blendedRotation = interpolatedRotation;
                    hasValidRotation = true;
                }
                else
                {
                    blendedRotation = Quaternion.Slerp(blendedRotation, interpolatedRotation, _weights[i]);
                }

                if (_enableLogging)
                {
                    Log($"Зона {i} вес: {_weights[i]:F3}, интерполяция: {normalizedDistance:F3}");
                }
            }

            return hasValidRotation ? blendedRotation : fallbackRotation;
        }

        /// <summary>
        /// Отрисовывает гизмосы для визуализации зон вращения
        /// </summary>
        /// <param name="center">Центр зоны в мировых координатах</param>
        /// <param name="radius">Радиус зоны в единицах мира</param>
        /// <param name="areaColor">Цвет линий зон</param>
        public void DrawGizmos(Vector3 center, float radius, Color areaColor)
        {
            if (_areas.Length == 0)
                return;

            Gizmos.color = areaColor;
            
            for (int i = 0; i < _areas.Length; i++)
            {
                DrawAreaBoundary(center, radius, _areas[i], i);
            }

            // Отрисовка центральной точки
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.1f);
        }

        /// <summary>
        /// Проверяет равенство двух массивов зон вращения
        /// </summary>
        /// <param name="otherAreas">Другой массив зон для сравнения</param>
        /// <returns>True, если зоны идентичны</returns>
        public bool AreAreasEqual(RotationArea[] otherAreas)
        {
            if (otherAreas == null && _areas == null)
                return true;
            
            if (otherAreas == null || _areas == null)
                return false;
                
            if (_areas.Length != otherAreas.Length) 
                return false;

            for (int i = 0; i < _areas.Length; i++)
            {
                if (!AreAreasEqual(_areas[i], otherAreas[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Получает текущий вес для указанной зоны
        /// </summary>
        /// <param name="areaIndex">Индекс зоны</param>
        /// <returns>Вес зоны или 0, если индекс некорректен</returns>
        public float GetCurrentWeight(int areaIndex)
        {
            if (areaIndex < 0 || areaIndex >= _weights.Length)
                return 0f;
            
            return _weights[areaIndex];
        }

        /// <summary>
        /// Получает координаты центра экрана
        /// </summary>
        /// <returns>Вектор центра экрана в пикселях</returns>
        public Vector2 GetScreenCenter()
        {
            return _screenCenter;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Вычисляет угол позиции курсора относительно центра экрана
        /// </summary>
        /// <param name="pos">Позиция курсора в экранных координатах</param>
        /// <returns>Угол в градусах (0-360)</returns>
        private float CalculateAngle(Vector3 pos)
        {
            Vector2 cursorVec = new Vector2(pos.x, pos.y) - _screenCenter;
            float angle = Mathf.Atan2(cursorVec.y, cursorVec.x) * Mathf.Rad2Deg;
            if (angle < 0) 
                angle += 360f;
            return angle;
        }

        /// <summary>
        /// Вычисляет веса для каждой зоны на основе угла курсора
        /// </summary>
        /// <param name="angle">Угол курсора в градусах</param>
        private void CalculateWeightsForPosition(float angle)
        {
            for (int i = 0; i < _areas.Length; i++)
            {
                float currentAngle = _areas[i].BoundaryAngle;
                float nextAngle = (i + 1 < _areas.Length) ? 
                    _areas[i + 1].BoundaryAngle : 
                    _areas[0].BoundaryAngle + 360f;

                float midAngle = CalculateMidAngle(currentAngle, nextAngle);
                float distance = Mathf.Abs(Mathf.DeltaAngle(angle, midAngle));
                float maxDistance = Mathf.Abs(Mathf.DeltaAngle(currentAngle, nextAngle)) / 2f;
                
                // Расчет веса на основе расстояния до средней точки зоны
                float weight = Mathf.Clamp01(1f - distance / maxDistance);
                _weights[i] = Mathf.Max(_weights[i], weight);
            }
        }

        /// <summary>
        /// Вычисляет средний угол между двумя граничными углами
        /// </summary>
        /// <param name="currentAngle">Текущий граничный угол</param>
        /// <param name="nextAngle">Следующий граничный угол</param>
        /// <returns>Средний угол в градусах</returns>
        private float CalculateMidAngle(float currentAngle, float nextAngle)
        {
            float midAngle = (currentAngle + nextAngle) / 2f;
            if (nextAngle < currentAngle) 
                midAngle += 180f;
            return midAngle;
        }

        /// <summary>
        /// Нормализует веса зон для их суммирования к 1
        /// </summary>
        private void NormalizeWeights()
        {
            float totalWeight = _weights.Sum();
            
            if (totalWeight > 0)
            {
                for (int i = 0; i < _weights.Length; i++)
                {
                    _weights[i] /= totalWeight;
                }

                if (_enableLogging)
                {
                    Log($"Нормализованы веса - Общая сумма: {totalWeight:F3}");
                }
            }
        }

        /// <summary>
        /// Отрисовывает границу зоны вращения в виде гизмоса
        /// </summary>
        /// <param name="center">Центр зоны в мировых координатах</param>
        /// <param name="radius">Радиус зоны в единицах мира</param>
        /// <param name="area">Зона вращения</param>
        /// <param name="index">Индекс зоны</param>
        private void DrawAreaBoundary(Vector3 center, float radius, RotationArea area, int index)
        {
            float angleRad = area.BoundaryAngle * Mathf.Deg2Rad;
            Vector3 boundaryPoint = center + new Vector3(
                Mathf.Cos(angleRad), 
                Mathf.Sin(angleRad), 
                0
            ) * radius;
            
            Gizmos.DrawLine(center, boundaryPoint);

#if UNITY_EDITOR
            // Отображение информации о зоне в окне Scene
            string label = $"Зона {index}: {area.BoundaryAngle:F0}°";
            if (_weights.Length > index)
            {
                label += $"\nВес: {_weights[index]:F2}";
            }
            UnityEditor.Handles.Label(boundaryPoint, label);
#endif
        }

        /// <summary>
        /// Проверяет равенство двух зон вращения с учетом допуска
        /// </summary>
        /// <param name="a">Первая зона вращения</param>
        /// <param name="b">Вторая зона вращения</param>
        /// <returns>True, если зоны эквивалентны</returns>
        private bool AreAreasEqual(RotationArea a, RotationArea b)
        {
            const float tolerance = 0.001f;
            
            return Mathf.Abs(a.BoundaryAngle - b.BoundaryAngle) < tolerance &&
                   Vector3.Distance(a.CenterHandEuler, b.CenterHandEuler) < tolerance &&
                   Vector3.Distance(a.OuterHandEuler, b.OuterHandEuler) < tolerance;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Логирует сообщение, если включено детальное логирование
        /// </summary>
        /// <param name="message">Сообщение для логирования</param>
        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[RotationAreaManager] {message}");
            }
        }

        #endregion
    }
}