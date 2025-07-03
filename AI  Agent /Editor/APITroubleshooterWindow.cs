using UnityEditor;
using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace UnityAIAgent
{
    /// <summary>
    /// Вікно для вирішення проблем з API ключами
    /// </summary>
    public class APITroubleshooterWindow : EditorWindow
    {
        private string _apiKeyToCheck = "";
        private string _statusMessage = "";
        private bool _isChecking = false;
        private GUIStyle _linkStyle;
        private Vector2 _scrollPosition;
        private Color _statusColor = Color.white;

        /// <summary>
        /// Відкриває вікно усунення несправностей API
        /// </summary>
        [MenuItem("Window/AI Assistant/Troubleshoot API")]
        public static void ShowWindow()
        {
            var window = GetWindow<APITroubleshooterWindow>("API Troubleshooter");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            _linkStyle = new GUIStyle(EditorStyles.linkLabel);
            _linkStyle.fontSize = 12;
        }

        private void OnGUI()
        {
            // Ініціалізуємо стиль посилань, якщо потрібно
            if (_linkStyle == null)
            {
                _linkStyle = new GUIStyle(EditorStyles.linkLabel);
                _linkStyle.fontSize = 12;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Заголовок
            EditorGUILayout.Space(10);
            GUILayout.Label("Помічник з усунення проблем API Gemini", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Цей інструмент допоможе вирішити поширені проблеми з API ключами Gemini.\n\n" +
                "Якщо у вас виникають помилки при перевірці ключа, спробуйте використати цей інструмент для діагностики.", 
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Перевірка API ключа
            EditorGUILayout.LabelField("API ключ для перевірки:", EditorStyles.boldLabel);
            _apiKeyToCheck = EditorGUILayout.PasswordField(_apiKeyToCheck);
            
            // Горизонтальна група для кнопок
            EditorGUILayout.BeginHorizontal();
            
            // Кнопка для перевірки ключа
            EditorGUI.BeginDisabledGroup(_isChecking || string.IsNullOrEmpty(_apiKeyToCheck));
            
            if (GUILayout.Button("Перевірити ключ Gemini", GUILayout.Height(30)))
            {
                TestGeminiAPIKey();
            }
            
            EditorGUI.EndDisabledGroup();
            
            // Кнопка для очищення ключа
            if (GUILayout.Button("Очистити", GUILayout.Width(80), GUILayout.Height(30)))
            {
                _apiKeyToCheck = "";
                _statusMessage = "";
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Додаткові дії для валідного ключа
            if (!string.IsNullOrEmpty(_apiKeyToCheck) && _statusColor.g > 0.5f) // Якщо ключ валідний
            {
                if (GUILayout.Button("Перевірити доступні моделі", GUILayout.Height(30)))
                {
                    TestAvailableModels();
                }
                
                if (GUILayout.Button("Застосувати цей ключ у налаштуваннях", GUILayout.Height(30)))
                {
                    ApplyKeyToSettings();
                }
            }
            
            // Відображення статусу
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical("box");
                
                Color originalColor = GUI.color;
                GUI.color = _statusColor;
                EditorGUILayout.LabelField(_statusMessage, EditorStyles.wordWrappedLabel);
                GUI.color = originalColor;
                
                EditorGUILayout.EndVertical();
            }
            
            // Інформація про можливі рішення
            EditorGUILayout.Space(10);
            
            // Додаємо інструкцію з документацією
            EditorGUILayout.HelpBox(
                "Цей інструмент допомагає перевірити ваш API ключ Gemini і виявити поширені проблеми. " +
                "Вставте ваш API ключ у поле вище та натисніть 'Перевірити ключ Gemini'.\n\n" +
                "Після успішної перевірки ви можете відразу застосувати ключ у налаштуваннях " +
                "або перевірити доступні моделі для вашого ключа.", 
                MessageType.Info);
            
            EditorGUILayout.Space(20);
            GUILayout.Label("Поширені проблеми та рішення:", EditorStyles.boldLabel);
            
            DrawProblemAndSolution(
                "Помилка 'API key not valid'", 
                "• Перевірте, чи коректно скопійовано ключ (повністю, без пробілів)\n" +
                "• Створіть новий ключ API на сайті Google AI Studio\n" +
                "• Переконайтеся, що ключ починається з 'AIza' або 'g-'\n" +
                "• Зачекайте кілька хвилин після створення ключа"
            );
            
            DrawProblemAndSolution(
                "Перевищення квоти (quota exceeded)", 
                "• Безкоштовний рівень має обмеження: 60 запитів/хвилину\n" +
                "• Зачекайте деякий час, щоб скинулися ліміти\n" +
                "• Розгляньте можливість використання локальних моделей Ollama"
            );
            
            DrawProblemAndSolution(
                "Помилки мережі або таймаути", 
                "• Перевірте підключення до інтернету\n" +
                "• Переконайтеся, що файрвол не блокує запити до Google API\n" +
                "• Спробуйте використовувати VPN, якщо Google API недоступний у вашому регіоні"
            );
            
            DrawProblemAndSolution(
                "Модель недоступна", 
                "• Переконайтеся, що ви використовуєте доступну модель: gemini-pro, gemini-1.5-pro\n" +
                "• Деякі моделі можуть бути недоступні для безкоштовних ключів\n" +
                "• Використовуйте кнопку 'Перевірити доступні моделі' після валідації ключа"
            );
            
            // Посилання на офіційні ресурси
            EditorGUILayout.Space(20);
            GUILayout.Label("Корисні посилання:", EditorStyles.boldLabel);
            
            if (GUILayout.Button(" Створити ключ Google Gemini", GUILayout.Width(250), GUILayout.Height(25)))
            {
                Application.OpenURL("https://makersuite.google.com/app/apikey");
            }
            
            if (GUILayout.Button(" Документація Google Gemini API", GUILayout.Width(250), GUILayout.Height(25)))
            {
                Application.OpenURL("https://ai.google.dev/docs/gemini-api/reference");
            }
            
            if (GUILayout.Button(" Перевірка квот та лімітів", GUILayout.Width(250), GUILayout.Height(25)))
            {
                Application.OpenURL("https://ai.google.dev/api/quotas");
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Якщо проблему не вдалося вирішити, спробуйте використовувати інші API (OpenAI, Claude) або локальні моделі через Ollama.", MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Малює блок з проблемою та рішенням
        /// </summary>
        private void DrawProblemAndSolution(string problem, string solution)
        {
            EditorGUILayout.BeginVertical("box");
            
            GUIStyle problemStyle = new GUIStyle(EditorStyles.boldLabel);
            problemStyle.normal.textColor = new Color(0.8f, 0.3f, 0.3f);
            EditorGUILayout.LabelField(problem, problemStyle);
            
            EditorGUILayout.LabelField(solution, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Тестує API ключ Gemini
        /// </summary>
        private async void TestGeminiAPIKey()
        {
            _isChecking = true;
            _statusMessage = "Перевірка ключа...";
            _statusColor = Color.yellow;
            Repaint();
            
            // Фіксуємо формат ключа
            string fixedKey = APIKeyHelper.FixGeminiApiKey(_apiKeyToCheck);
            if (fixedKey != _apiKeyToCheck)
            {
                _apiKeyToCheck = fixedKey;
                _statusMessage = "✓ Формат ключа автоматично виправлено";
            }
            
            // Аналізуємо формат ключа
            var analysis = APIKeyHelper.AnalyzeGeminiKey(fixedKey);
            
            if (!analysis.isValid)
            {
                _statusMessage = $"⚠️ {analysis.recommendation}";
                _statusColor = new Color(1.0f, 0.8f, 0.2f);
                _isChecking = false;
                Repaint();
                return;
            }
            
            try
            {
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                string url = $"https://generativelanguage.googleapis.com/v1/models?key={fixedKey}";
                
                var response = await httpClient.GetAsync(url);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    _statusMessage = "✓ API ключ працює коректно! Тест успішний.";
                    _statusColor = new Color(0.2f, 0.8f, 0.2f);
                }
                else
                {
                    // Базове повідомлення про помилку
                    _statusMessage = $"❌ Помилка API: {response.StatusCode}";
                    _statusColor = new Color(0.8f, 0.2f, 0.2f);
                    
                    // Додаємо деталі помилки
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        // Додаємо коротку версію відповіді для уникнення перенавантаження інтерфейсу
                        string shortResponse = responseContent.Length > 150 ? 
                            responseContent.Substring(0, 150) + "..." : responseContent;
                        
                        _statusMessage += $"\nДеталі: {shortResponse}";
                    }
                    
                    // Отримуємо конкретні рекомендації на основі тексту помилки
                    string recommendation = GetSpecificErrorRecommendation(responseContent);
                    _statusMessage += $"\n\nРекомендація:\n{recommendation}";
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"❌ Помилка тесту: {ex.Message}";
                _statusColor = new Color(0.8f, 0.2f, 0.2f);
            }
            
            _isChecking = false;
            Repaint();
        }
        
        /// <summary>
        /// Перевіряє доступні для API ключа моделі
        /// </summary>
        private async void TestAvailableModels()
        {
            _isChecking = true;
            _statusMessage = "Завантаження доступних моделей...";
            _statusColor = Color.yellow;
            Repaint();
            
            try
            {
                var settings = ScriptableObject.CreateInstance<AIAgentSettings>();
                settings.geminiApiKey = _apiKeyToCheck;
                
                var geminiService = new GeminiService(settings);
                var models = await geminiService.GetAvailableModels(true);
                
                if (models != null && models.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"✓ Доступні моделі Gemini ({models.Count}):");
                    
                    foreach (var model in models)
                    {
                        sb.AppendLine($"• {model}");
                    }
                    
                    _statusMessage = sb.ToString();
                    _statusColor = new Color(0.2f, 0.8f, 0.2f);
                }
                else
                {
                    _statusMessage = "❌ Не вдалося отримати список моделей. Перевірте з'єднання або права доступу.";
                    _statusColor = new Color(0.8f, 0.8f, 0.2f);
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"❌ Помилка при отриманні моделей: {ex.Message}";
                _statusColor = new Color(0.8f, 0.2f, 0.2f);
            }
            
            _isChecking = false;
            Repaint();
        }
        
        /// <summary>
        /// Застосовує перевірений ключ у налаштуваннях
        /// </summary>
        private void ApplyKeyToSettings()
        {
            try
            {
                // Отримуємо поточні налаштування
                var settings = AIAgentSettingsCreator.GetSettings();
                
                if (settings != null)
                {
                    // Застосовуємо ключ у налаштуваннях
                    settings.geminiApiKey = _apiKeyToCheck;
                    settings.useGeminiAPI = true;
                    
                    // Зберігаємо налаштування
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    
                    // Повідомляємо про успіх
                    _statusMessage += "\n\n✓ Ключ успішно застосовано в налаштуваннях!";
                    Debug.Log("API ключ Gemini успішно застосовано в налаштуваннях.");
                    
                    // Відразу перевіряємо доступні моделі у фоновому режимі
                    // щоб оновити кеш моделей у налаштуваннях
                    EditorApplication.delayCall += async () => 
                    {
                        var geminiService = new GeminiService(settings);
                        await geminiService.GetAvailableModels(true);
                    };
                }
                else
                {
                    // Створюємо нові налаштування, якщо вони відсутні
                    var newSettings = AIAgentSettingsCreator.GetSettings(true);
                    if (newSettings != null)
                    {
                        newSettings.geminiApiKey = _apiKeyToCheck;
                        newSettings.useGeminiAPI = true;
                        EditorUtility.SetDirty(newSettings);
                        AssetDatabase.SaveAssets();
                        
                        _statusMessage += "\n\n✓ Створено нові налаштування з вашим ключем!";
                    }
                    else
                    {
                        _statusMessage += "\n\n❌ Не вдалося створити файл налаштувань.";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при застосуванні ключа: {ex.Message}");
                _statusMessage += $"\n\n❌ Помилка: {ex.Message}";
            }
            
            Repaint();
        }
        
        /// <summary>
        /// Аналізує помилку API та повертає конкретні рекомендації
        /// </summary>
        public static string GetSpecificErrorRecommendation(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return "Необхідно вказати текст помилки для аналізу.";
            
            // Аналізуємо типові помилки API
            if (errorMessage.Contains("API_KEY_INVALID") || errorMessage.Contains("key not valid") || 
                errorMessage.Contains("invalid api key"))
            {
                return "Ваш API ключ невалідний. Переконайтеся, що:\n" +
                       "• Ключ скопійовано повністю, включно з префіксом 'AIza' або 'g-'\n" +
                       "• Не містить зайвих пробілів на початку/в кінці\n" +
                       "• Ви використовуєте саме ключ для Gemini API, а не іншого сервісу\n" +
                       "• Ключ не було деактивовано в Google AI Studio";
            }
            else if (errorMessage.Contains("quota") || errorMessage.Contains("rate limit"))
            {
                return "Перевищено ліміт запитів API. Рекомендації:\n" +
                       "• Безкоштовні ключі мають обмеження до 60 запитів/хв\n" +
                       "• Зачекайте кілька хвилин перед наступною спробою\n" +
                       "• Створіть новий API ключ у Google AI Studio\n" +
                       "• Розгляньте можливість використання локальних моделей через Ollama";
            }
            else if (errorMessage.Contains("permission") || errorMessage.Contains("access") || 
                     errorMessage.Contains("authorization"))
            {
                return "Проблеми з авторизацією або правами доступу. Перевірте:\n" +
                       "• Чи активовано API Gemini для вашого облікового запису\n" +
                       "• Чи не закінчився термін дії ключа\n" +
                       "• Чи немає регіональних обмежень для API (спробуйте VPN)";
            }
            else if (errorMessage.Contains("model not found") || errorMessage.Contains("not found"))
            {
                return "Обрана модель недоступна. Рекомендації:\n" +
                       "• Використовуйте стандартні моделі: gemini-pro або gemini-1.5-pro\n" +
                       "• Виконайте оновлення списку моделей у налаштуваннях\n" +
                       "• Перевірте, чи правильний формат назви моделі без зайвих символів";
            }
            else if (errorMessage.Contains("timeout") || errorMessage.Contains("timed out"))
            {
                return "Запит до API завершився таймаутом. Рекомендації:\n" +
                       "• Перевірте стабільність підключення до інтернету\n" +
                       "• Зменшіть розмір запиту або використовуйте простіший запит\n" +
                       "• Спробуйте пізніше, сервера Google можуть бути перевантажені";
            }
            else if (errorMessage.Contains("internal") || errorMessage.Contains("server error"))
            {
                return "Внутрішня помилка серверів Google. Рекомендації:\n" +
                       "• Це тимчасова проблема на стороні Google, не з вашим ключем\n" +
                       "• Спробуйте повторити запит через деякий час\n" +
                       "• Перевірте статус сервісів Google AI на офіційному сайті";
            }
            
            // Узагальнена рекомендація, якщо не знайдено конкретної помилки
            return "Невідома помилка API. Загальні рекомендації:\n" +
                   "• Перевірте правильність ключа API\n" +
                   "• Переконайтесь у стабільності інтернет-з'єднання\n" +
                   "• Спробуйте створити новий ключ API\n" +
                   "• Перевірте доступність сервісу Google Gemini у вашому регіоні";
        }
    }
}
