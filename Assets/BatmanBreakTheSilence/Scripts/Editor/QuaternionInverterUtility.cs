using UnityEngine;
using UnityEditor;
using System;
using System.Text;

namespace BatmanBreakTheSilence
{
    /// <summary>
    /// Утилита для инвертирования кватернионов и углов в Unity Editor
    /// </summary>
    public class QuaternionInverterUtility : EditorWindow
    {
        #region Serialized Settings

        [System.Serializable]
        public class InverterSettings
        {
            [Header("Основные настройки")]
            public bool autoCalculate = true;
            public bool showDebugInfo = false;
            public bool copyToClipboard = false;
            public bool useRadians = false;
            
            [Header("Форматирование")]
            public int decimalPlaces = 3;
            public bool showComponentLabels = true;
            public bool compactOutput = false;
            
            [Header("Инверсия")]
            public bool invertX = true;
            public bool invertY = true;
            public bool invertZ = true;
            public bool invertW = false;
            
            [Header("Цветовая схема")]
            public Color positiveColor = Color.green;
            public Color negativeColor = Color.red;
            public Color neutralColor = Color.white;
            public Color highlightColor = Color.yellow;
        }

        #endregion

        #region Private Fields

        private InverterSettings _settings;
        private Vector2 _scrollPosition;
        
        // Входные данные
        private Quaternion _inputQuaternion = Quaternion.identity;
        private Vector3 _inputEulerAngles = Vector3.zero;
        private Vector3 _inputAxisAngle = Vector3.zero;
        private float _inputAngleValue = 0f;
        
        // Результаты
        private Quaternion _outputQuaternion = Quaternion.identity;
        private Vector3 _outputEulerAngles = Vector3.zero;
        private Vector3 _outputAxisAngle = Vector3.zero;
        private float _outputAngleValue = 0f;
        
        // UI состояние
        private int _selectedTab = 0;
        private string[] _tabNames = { "Кватернион", "Углы Эйлера", "Ось-Угол", "Настройки" };
        private bool _showAdvancedOptions = false;
        private bool _showHistory = false;
        
        // История операций
        private System.Collections.Generic.List<string> _operationHistory;
        private const int MAX_HISTORY_ITEMS = 50;
        
        // Стили GUI
        private GUIStyle _headerStyle;
        private GUIStyle _resultStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized = false;

        #endregion

        #region Menu Item

        [MenuItem("Batman Break The Silence/Tools/Quaternion Inverter", false, 100)]
        public static void ShowWindow()
        {
            QuaternionInverterUtility window = GetWindow<QuaternionInverterUtility>();
            window.titleContent = new GUIContent("Quaternion Inverter", "Утилита для инвертирования кватернионов и углов");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            LoadSettings();
            InitializeHistory();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnGUI()
        {
            // Обработка горячих клавиш
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.F5:
                        CalculateInversion();
                        e.Use();
                        break;
                        
                    case KeyCode.Escape:
                        ClearAllValues();
                        e.Use();
                        break;
                        
                    case KeyCode.C when e.control:
                        CopyCurrentResultToClipboard();
                        e.Use();
                        break;
                        
                    case KeyCode.R when e.control:
                        _inputQuaternion = UnityEngine.Random.rotation;
                        CalculateInversion();
                        e.Use();
                        break;
                }
            }
            
            // Основной GUI
            InitializeStyles();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            DrawTabs();
            
            EditorGUILayout.Space(10);
            
            switch (_selectedTab)
            {
                case 0: DrawQuaternionTab(); break;
                case 1: DrawEulerAnglesTab(); break;
                case 2: DrawAxisAngleTab(); break;
                case 3: DrawSettingsTab(); break;
            }
            
            EditorGUILayout.Space(10);
            DrawUtilityButtons();
            DrawKeyboardShortcuts();
            
            if (_showHistory)
            {
                DrawHistory();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (_settings.autoCalculate)
            {
                CalculateInversion();
            }
        }

        #endregion

        #region Initialization

        private void LoadSettings()
        {
            string settingsJson = EditorPrefs.GetString("QuaternionInverterSettings", "");
            if (!string.IsNullOrEmpty(settingsJson))
            {
                try
                {
                    _settings = JsonUtility.FromJson<InverterSettings>(settingsJson);
                }
                catch
                {
                    _settings = new InverterSettings();
                }
            }
            else
            {
                _settings = new InverterSettings();
            }
        }

        private void SaveSettings()
        {
            string settingsJson = JsonUtility.ToJson(_settings, true);
            EditorPrefs.SetString("QuaternionInverterSettings", settingsJson);
        }

        private void InitializeHistory()
        {
            _operationHistory = new System.Collections.Generic.List<string>();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            _resultStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 25
            };
            
            _stylesInitialized = true;
        }

        #endregion

        #region GUI Drawing

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quaternion & Angle Inverter", _headerStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawTabs()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
        }

        private void DrawQuaternionTab()
        {
            EditorGUILayout.LabelField("Инверсия Кватерниона", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Входной кватернион
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Входной кватернион:", EditorStyles.miniBoldLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("X", GUILayout.Width(15));
                    float x = EditorGUILayout.FloatField(_inputQuaternion.x);
                    EditorGUILayout.LabelField("Y", GUILayout.Width(15));
                    float y = EditorGUILayout.FloatField(_inputQuaternion.y);
                    EditorGUILayout.LabelField("Z", GUILayout.Width(15));
                    float z = EditorGUILayout.FloatField(_inputQuaternion.z);
                    EditorGUILayout.LabelField("W", GUILayout.Width(15));
                    float w = EditorGUILayout.FloatField(_inputQuaternion.w);
                    
                    _inputQuaternion = new Quaternion(x, y, z, w);
                }
                
                // Быстрые кнопки
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Identity", _buttonStyle))
                    {
                        _inputQuaternion = Quaternion.identity;
                        AddToHistory("Установлен Identity кватернион");
                    }
                    if (GUILayout.Button("Normalize", _buttonStyle))
                    {
                        _inputQuaternion = _inputQuaternion.normalized;
                        AddToHistory("Нормализован входной кватернион");
                    }
                    if (GUILayout.Button("From Selection", _buttonStyle))
                    {
                        LoadFromSelectedTransform();
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Результат
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Результат инверсии:", EditorStyles.miniBoldLabel);
                
                DrawQuaternionResult(_outputQuaternion);
                
                // Кнопки копирования
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy Quaternion", _buttonStyle))
                    {
                        CopyQuaternionToClipboard(_outputQuaternion);
                    }
                    if (GUILayout.Button("Copy C# Code", _buttonStyle))
                    {
                        CopyQuaternionCode(_outputQuaternion);
                    }
                    if (GUILayout.Button("Apply to Selection", _buttonStyle))
                    {
                        ApplyToSelectedTransform(_outputQuaternion);
                    }
                }
            }
            
            if (_settings.showDebugInfo)
            {
                DrawQuaternionDebugInfo();
            }
        }

        private void DrawEulerAnglesTab()
        {
            EditorGUILayout.LabelField("Инверсия Углов Эйлера", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Входные углы
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Входные углы:", EditorStyles.miniBoldLabel);
                
                string unitLabel = _settings.useRadians ? " (радианы)" : " (градусы)";
                EditorGUILayout.LabelField($"Единицы измерения{unitLabel}", EditorStyles.miniLabel);
                
                _inputEulerAngles = EditorGUILayout.Vector3Field("", _inputEulerAngles);
                
                // Быстрые кнопки
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Zero", _buttonStyle))
                    {
                        _inputEulerAngles = Vector3.zero;
                        AddToHistory("Установлены нулевые углы");
                    }
                    if (GUILayout.Button("90° X", _buttonStyle))
                    {
                        _inputEulerAngles = new Vector3(_settings.useRadians ? Mathf.PI/2 : 90, 0, 0);
                        AddToHistory("Установлен угол 90° по X");
                    }
                    if (GUILayout.Button("90° Y", _buttonStyle))
                    {
                        _inputEulerAngles = new Vector3(0, _settings.useRadians ? Mathf.PI/2 : 90, 0);
                        AddToHistory("Установлен угол 90° по Y");
                    }
                    if (GUILayout.Button("90° Z", _buttonStyle))
                    {
                        _inputEulerAngles = new Vector3(0, 0, _settings.useRadians ? Mathf.PI/2 : 90);
                        AddToHistory("Установлен угол 90° по Z");
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Результат
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Результат инверсии:", EditorStyles.miniBoldLabel);
                
                DrawVector3Result(_outputEulerAngles, _settings.useRadians ? "радианы" : "градусы");
                
                // Кнопки копирования
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy Vector3", _buttonStyle))
                    {
                        CopyVector3ToClipboard(_outputEulerAngles);
                    }
                    if (GUILayout.Button("Copy C# Code", _buttonStyle))
                    {
                        CopyVector3Code(_outputEulerAngles);
                    }
                }
            }
        }

        private void DrawAxisAngleTab()
        {
            EditorGUILayout.LabelField("Инверсия Ось-Угол", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Входные данные
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Входные данные:", EditorStyles.miniBoldLabel);
                
                _inputAxisAngle = EditorGUILayout.Vector3Field("Ось:", _inputAxisAngle);
                
                string angleLabel = _settings.useRadians ? "Угол (радианы):" : "Угол (градусы):";
                _inputAngleValue = EditorGUILayout.FloatField(angleLabel, _inputAngleValue);
                
                // Нормализация оси
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Normalize Axis", _buttonStyle))
                    {
                        _inputAxisAngle = _inputAxisAngle.normalized;
                        AddToHistory("Нормализована ось вращения");
                    }
                    
                    EditorGUILayout.LabelField($"Magnitude: {_inputAxisAngle.magnitude:F3}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Результат
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Результат инверсии:", EditorStyles.miniBoldLabel);
                
                DrawVector3Result(_outputAxisAngle, "ось");
                EditorGUILayout.LabelField($"Угол: {FormatFloat(_outputAngleValue)} {(_settings.useRadians ? "радиан" : "градусов")}");
                
                // Кнопки копирования
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy Axis-Angle", _buttonStyle))
                    {
                        string axisAngle = $"Axis: {FormatVector3(_outputAxisAngle)}, Angle: {FormatFloat(_outputAngleValue)}";
                        CopyToClipboard(axisAngle);
                    }
                    if (GUILayout.Button("Copy C# Code", _buttonStyle))
                    {
                        CopyAxisAngleCode(_outputAxisAngle, _outputAngleValue);
                    }
                }
            }
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Настройки", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Основные настройки:", EditorStyles.miniBoldLabel);
                
                _settings.autoCalculate = EditorGUILayout.Toggle("Автоматический расчет", _settings.autoCalculate);
                _settings.showDebugInfo = EditorGUILayout.Toggle("Показывать отладочную информацию", _settings.showDebugInfo);
                _settings.copyToClipboard = EditorGUILayout.Toggle("Автокопирование в буфер", _settings.copyToClipboard);
                _settings.useRadians = EditorGUILayout.Toggle("Использовать радианы", _settings.useRadians);
            }
            
            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Форматирование:", EditorStyles.miniBoldLabel);
                
                _settings.decimalPlaces = EditorGUILayout.IntSlider("Десятичные знаки", _settings.decimalPlaces, 1, 6);
                _settings.showComponentLabels = EditorGUILayout.Toggle("Показывать метки компонентов", _settings.showComponentLabels);
                _settings.compactOutput = EditorGUILayout.Toggle("Компактный вывод", _settings.compactOutput);
            }
            
            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Параметры инверсии:", EditorStyles.miniBoldLabel);
                
                _settings.invertX = EditorGUILayout.Toggle("Инвертировать X", _settings.invertX);
                _settings.invertY = EditorGUILayout.Toggle("Инвертировать Y", _settings.invertY);
                _settings.invertZ = EditorGUILayout.Toggle("Инвертировать Z", _settings.invertZ);
                _settings.invertW = EditorGUILayout.Toggle("Инвертировать W (кватернион)", _settings.invertW);
            }
            
            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Цветовая схема:", EditorStyles.miniBoldLabel);
                
                _settings.positiveColor = EditorGUILayout.ColorField("Положительные значения", _settings.positiveColor);
                _settings.negativeColor = EditorGUILayout.ColorField("Отрицательные значения", _settings.negativeColor);
                _settings.neutralColor = EditorGUILayout.ColorField("Нейтральные значения", _settings.neutralColor);
                _settings.highlightColor = EditorGUILayout.ColorField("Выделение", _settings.highlightColor);
            }
            
            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Сбросить настройки", _buttonStyle))
                {
                    _settings = new InverterSettings();
                    AddToHistory("Настройки сброшены к значениям по умолчанию");
                }
                
                if (GUILayout.Button("Экспорт настроек", _buttonStyle))
                {
                    ExportSettings();
                }
                
                if (GUILayout.Button("Импорт настроек", _buttonStyle))
                {
                    ImportSettings();
                }
            }
        }

        private void DrawUtilityButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Пересчитать", _buttonStyle))
                {
                    CalculateInversion();
                    AddToHistory("Выполнен ручной пересчет");
                }
                
                if (GUILayout.Button("Очистить все", _buttonStyle))
                {
                    ClearAllValues();
                }
                
                _showAdvancedOptions = GUILayout.Toggle(_showAdvancedOptions, "Дополнительно", GUI.skin.button);
                
                _showHistory = GUILayout.Toggle(_showHistory, "История", GUI.skin.button);
            }
            
            if (_showAdvancedOptions)
            {
                DrawAdvancedOptions();
            }
        }

        private void DrawAdvancedOptions()
        {
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Дополнительные операции:", EditorStyles.miniBoldLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Conjugate", _buttonStyle))
                    {
                        _outputQuaternion = Quaternion.Inverse(_inputQuaternion);
                        AddToHistory("Применено сопряжение кватерниона");
                    }
                    
                    if (GUILayout.Button("Normalize", _buttonStyle))
                    {
                        _inputQuaternion = _inputQuaternion.normalized;
                        CalculateInversion();
                        AddToHistory("Применена нормализация");
                    }
                    
                    if (GUILayout.Button("Lerp to Identity", _buttonStyle))
                    {
                        _outputQuaternion = Quaternion.Lerp(_inputQuaternion, Quaternion.identity, 0.5f);
                        AddToHistory("Применена интерполяция к Identity");
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Random Rotation", _buttonStyle))
                    {
                        _inputQuaternion = UnityEngine.Random.rotation;
                        CalculateInversion();
                        AddToHistory("Сгенерирована случайная ротация");
                    }
                    
                    if (GUILayout.Button("Flip 180°", _buttonStyle))
                    {
                        _inputQuaternion = _inputQuaternion * Quaternion.Euler(180, 0, 0);
                        CalculateInversion();
                        AddToHistory("Применен поворот на 180°");
                    }
                }
            }
        }

        private void DrawHistory()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("История операций:", EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("Очистить", GUILayout.Width(80)))
                    {
                        _operationHistory.Clear();
                    }
                }
                
                if (_operationHistory.Count == 0)
                {
                    EditorGUILayout.LabelField("История пуста", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = _operationHistory.Count - 1; i >= 0 && i >= _operationHistory.Count - 10; i--)
                    {
                        EditorGUILayout.LabelField($"• {_operationHistory[i]}", EditorStyles.miniLabel);
                    }
                    
                    if (_operationHistory.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... и еще {_operationHistory.Count - 10} операций", EditorStyles.centeredGreyMiniLabel);
                    }
                }
            }
        }

        #endregion

        #region Result Display

        private void DrawQuaternionResult(Quaternion quaternion)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("X:", GUILayout.Width(20));
                }
                DrawColoredFloat(quaternion.x);
                
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
                }
                DrawColoredFloat(quaternion.y);
                
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("Z:", GUILayout.Width(20));
                }
                DrawColoredFloat(quaternion.z);
                
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("W:", GUILayout.Width(20));
                }
                DrawColoredFloat(quaternion.w);
            }
        }

        private void DrawVector3Result(Vector3 vector, string unit)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("X:", GUILayout.Width(20));
                }
                DrawColoredFloat(vector.x);
                
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
                }
                DrawColoredFloat(vector.y);
                
                if (_settings.showComponentLabels)
                {
                    EditorGUILayout.LabelField("Z:", GUILayout.Width(20));
                }
                DrawColoredFloat(vector.z);
            }
        }

        private void DrawColoredFloat(float value)
        {
            Color originalColor = GUI.color;
            
            if (value > 0)
                GUI.color = _settings.positiveColor;
            else if (value < 0)
                GUI.color = _settings.negativeColor;
            else
                GUI.color = _settings.neutralColor;
                
            EditorGUILayout.LabelField(FormatFloat(value), _resultStyle, GUILayout.MinWidth(80));
            
            GUI.color = originalColor;
        }

        private void DrawQuaternionDebugInfo()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Отладочная информация:", EditorStyles.miniBoldLabel);
                
                Vector3 eulerAngles = _inputQuaternion.eulerAngles;
                EditorGUILayout.LabelField($"Углы Эйлера: ({eulerAngles.x:F2}, {eulerAngles.y:F2}, {eulerAngles.z:F2})");
                
                float angle;
                Vector3 axis;
                _inputQuaternion.ToAngleAxis(out angle, out axis);
                EditorGUILayout.LabelField($"Ось-Угол: {axis} @ {angle:F2}°");
                
                EditorGUILayout.LabelField($"Dot с Identity: {Quaternion.Dot(_inputQuaternion, Quaternion.identity):F3}");
                EditorGUILayout.LabelField($"Угловое расстояние: {Quaternion.Angle(_inputQuaternion, Quaternion.identity):F2}°");
            }
        }

        #endregion

        #region Calculations

        private void CalculateInversion()
        {
            // Инверсия кватерниона
            _outputQuaternion = InvertQuaternion(_inputQuaternion);
            
            // Инверсия углов Эйлера
            _outputEulerAngles = InvertEulerAngles(_inputEulerAngles);
            
            // Инверсия ось-угол
            InvertAxisAngle(_inputAxisAngle, _inputAngleValue, out _outputAxisAngle, out _outputAngleValue);
            
            if (_settings.copyToClipboard)
            {
                CopyCurrentResultToClipboard();
            }
        }

        private Quaternion InvertQuaternion(Quaternion input)
        {
            float x = _settings.invertX ? -input.x : input.x;
            float y = _settings.invertY ? -input.y : input.y;
            float z = _settings.invertZ ? -input.z : input.z;
            float w = _settings.invertW ? -input.w : input.w;
            
            return new Quaternion(x, y, z, w);
        }

        private Vector3 InvertEulerAngles(Vector3 input)
        {
            float x = _settings.invertX ? -input.x : input.x;
            float y = _settings.invertY ? -input.y : input.y;
            float z = _settings.invertZ ? -input.z : input.z;
            
            return new Vector3(x, y, z);
        }

        private void InvertAxisAngle(Vector3 inputAxis, float inputAngle, out Vector3 outputAxis, out float outputAngle)
        {
            float x = _settings.invertX ? -inputAxis.x : inputAxis.x;
            float y = _settings.invertY ? -inputAxis.y : inputAxis.y;
            float z = _settings.invertZ ? -inputAxis.z : inputAxis.z;
            
            outputAxis = new Vector3(x, y, z);
            outputAngle = -inputAngle;
        }

        #endregion

        #region Clipboard Operations

        private void CopyCurrentResultToClipboard()
        {
            switch (_selectedTab)
            {
                case 0: CopyQuaternionToClipboard(_outputQuaternion); break;
                case 1: CopyVector3ToClipboard(_outputEulerAngles); break;
                case 2: 
                    string axisAngle = $"Axis: {FormatVector3(_outputAxisAngle)}, Angle: {FormatFloat(_outputAngleValue)}";
                    CopyToClipboard(axisAngle);
                    break;
            }
        }

        private void CopyQuaternionToClipboard(Quaternion q)
        {
            string result = _settings.compactOutput 
                ? $"{FormatFloat(q.x)},{FormatFloat(q.y)},{FormatFloat(q.z)},{FormatFloat(q.w)}"
                : FormatQuaternion(q);
            CopyToClipboard(result);
        }

        private void CopyVector3ToClipboard(Vector3 v)
        {
            string result = _settings.compactOutput 
                ? $"{FormatFloat(v.x)},{FormatFloat(v.y)},{FormatFloat(v.z)}"
                : FormatVector3(v);
            CopyToClipboard(result);
        }

        private void CopyQuaternionCode(Quaternion q)
        {
            string code = $"new Quaternion({FormatFloat(q.x)}f, {FormatFloat(q.y)}f, {FormatFloat(q.z)}f, {FormatFloat(q.w)}f)";
            CopyToClipboard(code);
            AddToHistory($"Скопирован C# код кватерниона: {code}");
        }

        private void CopyVector3Code(Vector3 v)
        {
            string code = $"new Vector3({FormatFloat(v.x)}f, {FormatFloat(v.y)}f, {FormatFloat(v.z)}f)";
            CopyToClipboard(code);
            AddToHistory($"Скопирован C# код Vector3: {code}");
        }

        private void CopyAxisAngleCode(Vector3 axis, float angle)
        {
            string axisCode = $"new Vector3({FormatFloat(axis.x)}f, {FormatFloat(axis.y)}f, {FormatFloat(axis.z)}f)";
            string angleCode = $"{FormatFloat(angle)}f";
            string code = $"// Axis: {axisCode}\n// Angle: {angleCode}";
            CopyToClipboard(code);
            AddToHistory($"Скопирован C# код ось-угол");
        }

        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent($"Скопировано: {text.Substring(0, Math.Min(50, text.Length))}..."));
        }

        #endregion

        #region Transform Operations

        private void LoadFromSelectedTransform()
        {
            Transform selectedTransform = Selection.activeTransform;
            if (selectedTransform != null)
            {
                _inputQuaternion = selectedTransform.rotation;
                _inputEulerAngles = selectedTransform.eulerAngles;
                CalculateInversion();
                AddToHistory($"Загружена ротация из {selectedTransform.name}");
            }
            else
            {
                ShowNotification(new GUIContent("Не выбран объект с Transform"));
            }
        }

        private void ApplyToSelectedTransform(Quaternion rotation)
        {
            Transform selectedTransform = Selection.activeTransform;
            if (selectedTransform != null)
            {
                Undo.RecordObject(selectedTransform, "Apply Inverted Rotation");
                selectedTransform.rotation = rotation;
                EditorUtility.SetDirty(selectedTransform);
                AddToHistory($"Применена ротация к {selectedTransform.name}");
            }
            else
            {
                ShowNotification(new GUIContent("Не выбран объект с Transform"));
            }
        }

        #endregion

        #region Formatting

        private string FormatFloat(float value)
        {
            return value.ToString($"F{_settings.decimalPlaces}");
        }

        private string FormatVector3(Vector3 v)
        {
            if (_settings.compactOutput)
            {
                return $"({FormatFloat(v.x)}, {FormatFloat(v.y)}, {FormatFloat(v.z)})";
            }
            else
            {
                return $"X: {FormatFloat(v.x)}, Y: {FormatFloat(v.y)}, Z: {FormatFloat(v.z)}";
            }
        }

        private string FormatQuaternion(Quaternion q)
        {
            if (_settings.compactOutput)
            {
                return $"({FormatFloat(q.x)}, {FormatFloat(q.y)}, {FormatFloat(q.z)}, {FormatFloat(q.w)})";
            }
            else
            {
                return $"X: {FormatFloat(q.x)}, Y: {FormatFloat(q.y)}, Z: {FormatFloat(q.z)}, W: {FormatFloat(q.w)}";
            }
        }

        #endregion

        #region Utility Methods

        private void ClearAllValues()
        {
            _inputQuaternion = Quaternion.identity;
            _inputEulerAngles = Vector3.zero;
            _inputAxisAngle = Vector3.zero;
            _inputAngleValue = 0f;
            
            _outputQuaternion = Quaternion.identity;
            _outputEulerAngles = Vector3.zero;
            _outputAxisAngle = Vector3.zero;
            _outputAngleValue = 0f;
            
            AddToHistory("Очищены все значения");
        }

        private void AddToHistory(string operation)
        {
            if (_operationHistory == null) return;
            
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _operationHistory.Add($"[{timestamp}] {operation}");
            
            // Ограничиваем размер истории
            while (_operationHistory.Count > MAX_HISTORY_ITEMS)
            {
                _operationHistory.RemoveAt(0);
            }
        }

        private void ExportSettings()
        {
            string path = EditorUtility.SaveFilePanel(
                "Экспорт настроек", 
                Application.dataPath, 
                "QuaternionInverterSettings.json", 
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = JsonUtility.ToJson(_settings, true);
                    System.IO.File.WriteAllText(path, json);
                    ShowNotification(new GUIContent("Настройки экспортированы"));
                    AddToHistory($"Настройки экспортированы в {System.IO.Path.GetFileName(path)}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Ошибка", $"Не удалось экспортировать настройки: {e.Message}", "OK");
                }
            }
        }

        private void ImportSettings()
        {
            string path = EditorUtility.OpenFilePanel(
                "Импорт настроек", 
                Application.dataPath, 
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    _settings = JsonUtility.FromJson<InverterSettings>(json);
                    ShowNotification(new GUIContent("Настройки импортированы"));
                    AddToHistory($"Настройки импортированы из {System.IO.Path.GetFileName(path)}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Ошибка", $"Не удалось импортировать настройки: {e.Message}", "OK");
                }
            }
        }

        #endregion

        #region Math Utilities

        /// <summary>
        /// Преобразует градусы в радианы
        /// </summary>
        private float DegreesToRadians(float degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Преобразует радианы в градусы
        /// </summary>
        private float RadiansToDegrees(float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Получает противоположный кватернион (обратное вращение)
        /// </summary>
        private Quaternion GetOppositeRotation(Quaternion rotation)
        {
            return Quaternion.Inverse(rotation);
        }

        /// <summary>
        /// Интерполирует между двумя кватернионами
        /// </summary>
        private Quaternion InterpolateQuaternions(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.Lerp(a, b, t);
        }

        /// <summary>
        /// Проверяет, являются ли два кватерниона приблизительно равными
        /// </summary>
        private bool ApproximatelyEqual(Quaternion a, Quaternion b, float threshold = 0.001f)
        {
            return Quaternion.Angle(a, b) < threshold;
        }

        #endregion

        #region Keyboard Shortcuts

        private void DrawKeyboardShortcuts()
        {
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Горячие клавиши:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("F5 - Пересчитать", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Esc - Очистить все", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Ctrl+C - Копировать результат", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Ctrl+R - Случайная ротация", EditorStyles.miniLabel);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Валидирует входной кватернион
        /// </summary>
        private bool ValidateQuaternion(Quaternion q)
        {
            // Проверка на NaN
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
            {
                EditorUtility.DisplayDialog("Ошибка", "Кватернион содержит NaN значения", "OK");
                return false;
            }

            // Проверка на бесконечность
            if (float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w))
            {
                EditorUtility.DisplayDialog("Ошибка", "Кватернион содержит бесконечные значения", "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Валидирует углы Эйлера
        /// </summary>
        private bool ValidateEulerAngles(Vector3 angles)
        {
            // Проверка на экстремальные значения
            float maxAngle = _settings.useRadians ? Mathf.PI * 4 : 720f;
            
            if (Mathf.Abs(angles.x) > maxAngle || Mathf.Abs(angles.y) > maxAngle || Mathf.Abs(angles.z) > maxAngle)
            {
                return EditorUtility.DisplayDialog(
                    "Предупреждение", 
                    "Обнаружены очень большие углы. Это может привести к неожиданным результатам. Продолжить?", 
                    "Да", "Нет"
                );
            }

            return true;
        }

        #endregion

        #region Context Menu Integration

        [MenuItem("CONTEXT/Transform/Invert Rotation")]
        private static void InvertTransformRotation(MenuCommand command)
        {
            Transform transform = command.context as Transform;
            if (transform != null)
            {
                Undo.RecordObject(transform, "Invert Rotation");
                
                // Простая инверсия - обратный кватернион
                transform.rotation = Quaternion.Inverse(transform.rotation);
                
                EditorUtility.SetDirty(transform);
            }
        }

        [MenuItem("CONTEXT/Transform/Copy Rotation to Inverter")]
        private static void CopyRotationToInverter(MenuCommand command)
        {
            Transform transform = command.context as Transform;
            if (transform != null)
            {
                // Открываем окно утилиты если закрыто
                QuaternionInverterUtility window = GetWindow<QuaternionInverterUtility>();
                
                // Копируем ротацию
                window._inputQuaternion = transform.rotation;
                window._inputEulerAngles = transform.eulerAngles;
                window.CalculateInversion();
                
                window.ShowNotification(new GUIContent($"Ротация скопирована из {transform.name}"));
            }
        }

        #endregion

        #region Help and Documentation

        private void ShowHelpDialog()
        {
            StringBuilder helpText = new StringBuilder();
            helpText.AppendLine("Quaternion & Angle Inverter Utility");
            helpText.AppendLine("=====================================");
            helpText.AppendLine();
            helpText.AppendLine("Эта утилита предназначена для инвертирования кватернионов и углов.");
            helpText.AppendLine();
            helpText.AppendLine("ВКЛАДКИ:");
            helpText.AppendLine("• Кватернион - работа с кватернионами");
            helpText.AppendLine("• Углы Эйлера - работа с углами Эйлера");
            helpText.AppendLine("• Ось-Угол - работа с представлением ось-угол");
            helpText.AppendLine("• Настройки - конфигурация утилиты");
            helpText.AppendLine();
            helpText.AppendLine("ГОРЯЧИЕ КЛАВИШИ:");
            helpText.AppendLine("F5 - Пересчитать");
            helpText.AppendLine("Esc - Очистить все поля");
            helpText.AppendLine("Ctrl+C - Копировать результат");
            helpText.AppendLine("Ctrl+R - Сгенерировать случайную ротацию");
            helpText.AppendLine();
            helpText.AppendLine("КОНТЕКСТНОЕ МЕНЮ:");
            helpText.AppendLine("Щелкните правой кнопкой по Transform в Inspector для быстрого доступа к функциям инверсии.");

            EditorUtility.DisplayDialog("Справка", helpText.ToString(), "OK");
        }

        #endregion
    }

    /// <summary>
    /// Расширения для работы с кватернионами
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Безопасное нормализование кватерниона
        /// </summary>
        public static Quaternion SafeNormalized(this Quaternion quaternion)
        {
            // Calculate magnitude manually (this is the only way in Unity)
            float magnitude = Mathf.Sqrt(quaternion.x * quaternion.x + 
                                        quaternion.y * quaternion.y + 
                                        quaternion.z * quaternion.z + 
                                        quaternion.w * quaternion.w);
            
            if (magnitude > Mathf.Epsilon)
            {
                return new Quaternion(
                    quaternion.x / magnitude,
                    quaternion.y / magnitude,
                    quaternion.z / magnitude,
                    quaternion.w / magnitude
                );
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// Проверка на валидность кватерниона
        /// </summary>
        public static bool IsValid(this Quaternion quaternion)
        {
            return !float.IsNaN(quaternion.x) && !float.IsNaN(quaternion.y) && 
                   !float.IsNaN(quaternion.z) && !float.IsNaN(quaternion.w) &&
                   !float.IsInfinity(quaternion.x) && !float.IsInfinity(quaternion.y) && 
                   !float.IsInfinity(quaternion.z) && !float.IsInfinity(quaternion.w);
        }

        /// <summary>
        /// Получение строкового представления кватерниона с заданной точностью
        /// </summary>
        public static string ToString(this Quaternion quaternion, int decimalPlaces)
        {
            string format = $"F{decimalPlaces}";
            return $"({quaternion.x.ToString(format)}, {quaternion.y.ToString(format)}, " +
                   $"{quaternion.z.ToString(format)}, {quaternion.w.ToString(format)})";
        }
    }

    /// <summary>
    /// Расширения для работы с Vector3 в контексте углов
    /// </summary>
    public static class AngleExtensions
    {
        /// <summary>
        /// Нормализация угла к диапазону [-180, 180] градусов
        /// </summary>
        public static float NormalizeAngle(this float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Нормализация углов Эйлера к диапазону [-180, 180] градусов
        /// </summary>
        public static Vector3 NormalizeAngles(this Vector3 angles)
        {
            return new Vector3(
                angles.x.NormalizeAngle(),
                angles.y.NormalizeAngle(),
                angles.z.NormalizeAngle()
            );
        }

        /// <summary>
        /// Преобразование углов из градусов в радианы
        /// </summary>
        public static Vector3 ToRadians(this Vector3 degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Преобразование углов из радиан в градусы
        /// </summary>
        public static Vector3 ToDegrees(this Vector3 radians)
        {
            return radians * Mathf.Rad2Deg;
        }
    }
}