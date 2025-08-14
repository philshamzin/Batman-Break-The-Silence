using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Управляет вращением камеры, изменением поля зрения (FOV) и постобработкой на основе движения курсора.
    /// Обеспечивает плавные эффекты для атмосферного взаимодействия в Batman: Break The Silence.
    /// </summary>
    public sealed class CameraController : ICameraController
    {
        #region Private Fields

        // Основные компоненты
        private readonly Camera _camera; // Камера, которой управляет контроллер
        private readonly Transform _cursorFollower; // Трансформ, следующий за курсором
        private readonly CameraControlSettings _settings; // Настройки управления камерой
        private readonly PostProcessProfile _postProcessProfile; // Профиль постобработки для эффектов
        private readonly bool _enableLogging; // Включение детального логирования

        // Переменные состояния
        private Quaternion _initialRotation; // Начальная ротация камеры
        private Quaternion _targetRotation; // Целевая ротация камеры
        private float _initialFOV; // Начальное поле зрения
        private float _currentFOV; // Текущее поле зрения
        private float _fovVelocity; // Скорость изменения FOV для сглаживания
        private float _currentChromaticAberration; // Текущая интенсивность хроматической аберрации
        private float _chromaticAberrationVelocity; // Скорость изменения хроматической аберрации
        private Vector2 _screenCenter; // Центр экрана в пикселях
        private float _maxScreenDistance; // Максимальное расстояние от центра экрана

        // Компонент хроматической аберрации
        private ChromaticAberration _chromaticAberrationComponent; // Компонент постобработки для хроматической аберрации

        #endregion

        #region Properties

        /// <summary>
        /// Получает компонент камеры
        /// </summary>
        public Camera Camera => _camera;

        /// <summary>
        /// Получает начальную ротацию камеры
        /// </summary>
        public Quaternion InitialRotation => _initialRotation;

        /// <summary>
        /// Получает целевую ротацию камеры
        /// </summary>
        public Quaternion TargetRotation => _targetRotation;

        /// <summary>
        /// Получает текущее поле зрения камеры
        /// </summary>
        public float CurrentFOV => _currentFOV;

        /// <summary>
        /// Получает состояние инициализации контроллера
        /// </summary>
        public bool IsInitialized { get; private set; }

        #endregion

        #region Constructor

        /// < Sunday, August 03, 2025summary>
        /// Создает контроллер камеры с заданными параметрами
        /// </summary>
        /// <param name="camera">Компонент камеры для управления</param>
        /// <param name="cursorFollower">Трансформ, следующий за курсором (может быть null)</param>
        /// <param name="settings">Настройки управления камерой</param>
        /// <param name="postProcessProfile">Профиль постобработки (опционально)</param>
        /// <param name="enableLogging">Включить детальное логирование</param>
        public CameraController(
            Camera camera, 
            Transform cursorFollower, 
            CameraControlSettings settings, 
            PostProcessProfile postProcessProfile = null,
            bool enableLogging = false)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            _cursorFollower = cursorFollower;
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _postProcessProfile = postProcessProfile;
            _enableLogging = enableLogging;

            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обновляет вращение камеры на основе вектора курсора
        /// </summary>
        /// <param name="cursorVector">Вектор позиции курсора относительно центра экрана</param>
        public void UpdateCameraRotation(Vector2 cursorVector)
        {
            if (!IsInitialized)
                return;

            // Нормализация координат курсора относительно максимального расстояния
            float normalizedX = cursorVector.x / _maxScreenDistance;
            float normalizedY = cursorVector.y / _maxScreenDistance;

            // Расчет углов рыскания и тангажа
            float yawAngle = CalculateYawAngle(normalizedX);
            float pitchAngle = CalculatePitchAngle(normalizedY);

            // Формирование целевой ротации
            _targetRotation = _initialRotation * Quaternion.Euler(pitchAngle, yawAngle, 0f);

            // Применение вращения
            ApplyCameraRotation();

            if (_enableLogging)
            {
                Log($"Вращение камеры - Рыскание: {yawAngle:F2}°, Тангаж: {pitchAngle:F2}°");
            }
        }

        /// <summary>
        /// Обновляет эффекты камеры (FOV и хроматическую аберрацию) на основе расстояния курсора
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора от центра экрана (0-1)</param>
        public void UpdateCameraEffects(float normalizedDistance)
        {
            if (!IsInitialized)
                return;

            UpdateFOV(normalizedDistance);
            UpdateChromaticAberration(normalizedDistance);
        }

        /// <summary>
        /// Сбрасывает камеру в начальное состояние
        /// </summary>
        public void ResetCamera()
        {
            if (!IsInitialized)
                return;

            // Сброс ротации и поля зрения
            _targetRotation = _initialRotation;
            _currentFOV = _initialFOV;
            _camera.fieldOfView = _initialFOV;

            // Сброс хроматической аберрации
            if (_chromaticAberrationComponent != null)
            {
                _chromaticAberrationComponent.intensity.value = _settings.MinChromaticAberration;
            }

            // Применение ротации
            ApplyCameraRotation();

            if (_enableLogging)
            {
                Log("Камера сброшена в начальное состояние");
            }
        }

        /// <summary>
        /// Устанавливает новые настройки камеры
        /// </summary>
        /// <param name="newSettings">Новые настройки управления камерой</param>
        public void SetCameraSettings(CameraControlSettings newSettings)
        {
            if (newSettings == null)
                return;

            // Временное ограничение: смена настроек во время выполнения не поддерживается полностью
            LogWarning("Смена настроек во время выполнения не полностью поддерживается. Рекомендуется пересоздать контроллер.");
        }

        /// <summary>
        /// Получает координаты центра экрана
        /// </summary>
        /// <returns>Вектор центра экрана в пикселях</returns>
        public Vector2 GetScreenCenter()
        {
            return _screenCenter;
        }

        /// <summary>
        /// Получает максимальное расстояние от центра экрана
        /// </summary>
        /// <returns>Максимальное расстояние в пикселях</returns>
        public float GetMaxScreenDistance()
        {
            return _maxScreenDistance;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализирует контроллер камеры
        /// </summary>
        private void Initialize()
        {
            ValidateComponents();
            InitializeState();
            InitializePostProcessing();
            
            IsInitialized = true;

            if (_enableLogging)
            {
                Log("Контроллер камеры успешно инициализирован");
            }
        }

        /// <summary>
        /// Проверяет корректность компонентов и настроек
        /// </summary>
        private void ValidateComponents()
        {
            if (_camera == null)
            {
                throw new System.InvalidOperationException("Компонент камеры обязателен");
            }

            if (_settings.MinFOV <= 0 || _settings.MaxFOV < _settings.MinFOV)
            {
                LogWarning($"Некорректные настройки FOV: Мин={_settings.MinFOV}, Макс={_settings.MaxFOV}. Используются значения по умолчанию.");
            }
        }

        /// <summary>
        /// Инициализирует состояние камеры
        /// </summary>
        private void InitializeState()
        {
            _initialRotation = _camera.transform.rotation;
            _targetRotation = _initialRotation;
            _initialFOV = _camera.fieldOfView;
            _currentFOV = _initialFOV;

            // Расчет центра экрана и максимального расстояния
            _screenCenter = new Vector2(
                CursorTrackingSettings.ReferenceWidth * 0.5f, 
                CursorTrackingSettings.ReferenceHeight * 0.5f
            );
            _maxScreenDistance = _screenCenter.magnitude;

            // Коррекция настроек FOV при необходимости
            if (_settings.MinFOV <= 0 || _settings.MaxFOV < _settings.MinFOV)
            {
                float correctedMinFOV = Mathf.Max(10f, _initialFOV - 20f);
                float correctedMaxFOV = _initialFOV + 20f;
                
                LogWarning($"Исправленные значения FOV: Мин={correctedMinFOV}, Макс={correctedMaxFOV}");
            }
        }

        /// <summary>
        /// Инициализирует настройки постобработки
        /// </summary>
        private void InitializePostProcessing()
        {
            if (_postProcessProfile == null)
            {
                LogWarning("Профиль постобработки не предоставлен. Эффекты хроматической аберрации отключены.");
                return;
            }

            if (_postProcessProfile.TryGetSettings(out _chromaticAberrationComponent))
            {
                _currentChromaticAberration = _chromaticAberrationComponent.intensity.value;
                
                if (_enableLogging)
                {
                    Log($"Хроматическая аберрация инициализирована с интенсивностью: {_currentChromaticAberration:F3}");
                }
            }
            else
            {
                LogWarning("Профиль постобработки не содержит настроек хроматической аберрации.");
            }
        }

        /// <summary>
        /// Рассчитывает угол рыскания на основе нормализованной координаты X
        /// </summary>
        /// <param name="normalizedX">Нормализованная координата X (-1..1)</param>
        /// <returns>Угол рыскания в градусах</returns>
        private float CalculateYawAngle(float normalizedX)
        {
            float yawAngle = normalizedX * _settings.MaxYawAngle * _settings.DeviationStrength;
            return Mathf.Clamp(yawAngle, -_settings.MaxYawAngle, _settings.MaxYawAngle);
        }

        /// <summary>
        /// Рассчитывает угол тангажа на основе нормализованной координаты Y
        /// </summary>
        /// <param name="normalizedY">Нормализованная координата Y (-1..1)</param>
        /// <returns>Угол тангажа в градусах</returns>
        private float CalculatePitchAngle(float normalizedY)
        {
            float pitchAngle = -normalizedY * _settings.MaxPitchAngle * _settings.DeviationStrength;
            return Mathf.Clamp(pitchAngle, -_settings.MaxPitchAngle, _settings.MaxPitchAngle);
        }

        /// <summary>
        /// Применяет плавное вращение камеры или объекта, следующего за курсором
        /// </summary>
        private void ApplyCameraRotation()
        {
            if (_cursorFollower != null)
            {
                // Плавное вращение объекта, следующего за курсором
                _cursorFollower.rotation = Quaternion.Slerp(
                    _cursorFollower.rotation,
                    _targetRotation,
                    1f - Mathf.Exp(-_settings.SmoothTime * Time.deltaTime)
                );
            }
            else
            {
                // Плавное вращение камеры, если объект-следователь не указан
                _camera.transform.rotation = Quaternion.Slerp(
                    _camera.transform.rotation,
                    _targetRotation,
                    1f - Mathf.Exp(-_settings.SmoothTime * Time.deltaTime)
                );
            }
        }

        /// <summary>
        /// Обновляет поле зрения (FOV) на основе нормализованного расстояния курсора
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        private void UpdateFOV(float normalizedDistance)
        {
            // Расчет целевого FOV с использованием кривой
            float fovCurveValue = _settings.FOVCurve.Evaluate(normalizedDistance);
            float targetFOV = Mathf.Lerp(_settings.MinFOV, _settings.MaxFOV, fovCurveValue);
            
            // Плавное изменение FOV
            _currentFOV = Mathf.SmoothDamp(
                _currentFOV, 
                targetFOV, 
                ref _fovVelocity, 
                _settings.FOVSmoothTime
            );
            
            _camera.fieldOfView = _currentFOV;

            if (_enableLogging && Time.frameCount % 60 == 0) // Логирование каждые 60 кадров для избежания спама
            {
                Log($"FOV обновлен - Текущий: {_currentFOV:F2}, Целевой: {targetFOV:F2}");
            }
        }

        /// <summary>
        /// Обновляет интенсивность хроматической аберрации на основе нормализованного расстояния курсора
        /// </summary>
        /// <param name="normalizedDistance">Нормализованное расстояние курсора (0-1)</param>
        private void UpdateChromaticAberration(float normalizedDistance)
        {
            if (_chromaticAberrationComponent == null)
                return;

            // Расчет целевой интенсивности хроматической аберрации
            float caCurveValue = _settings.ChromaticAberrationCurve.Evaluate(normalizedDistance);
            float targetChromaticAberration = Mathf.Lerp(
                _settings.MinChromaticAberration, 
                _settings.MaxChromaticAberration, 
                caCurveValue
            );

            // Плавное изменение интенсивности
            _currentChromaticAberration = Mathf.SmoothDamp(
                _currentChromaticAberration,
                targetChromaticAberration,
                ref _chromaticAberrationVelocity,
                _settings.ChromaticAberrationSmoothTime
            );

            _chromaticAberrationComponent.intensity.value = _currentChromaticAberration;

            if (_enableLogging && Time.frameCount % 60 == 0)
            {
                Log($"Хроматическая аберрация обновлена - Текущая интенсивность: {_currentChromaticAberration:F3}");
            }
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
                Debug.Log($"[CameraController] {message}");
            }
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        /// <param name="message">Сообщение предупреждения</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[CameraController] {message}");
        }

        #endregion
    }
}