// filepath: /home/a/Музика/AI Unity Agent/Editor/AIAgentSettingsWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text;

namespace UnityAIAgent
{
    /// <summary>
    /// Вікно налаштувань для AI Agent - містить конфігурації API ключів та джерел асетів
    /// </summary>
    public partial class AIAgentSettingsWindow : EditorWindow
    {
        // Settings
        private AIAgentSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;
        
        // URLs for documentation and support
        private const string DOCUMENTATION_URL = "https://github.com/your-organization/unity-ai-agent";
        private const string SUPPORT_URL = "https://github.com/your-organization/unity-ai-agent/issues";
        
        // API Keys
        private string _openAIKey = ""; 
        // Видалено _anthropicKey, оскільки тепер використовується _settings.anthropicApiKey напряму
        
        // Кешовані дані для моделей API
        private List<string> _cachedGeminiModels;
    private string _geminiModelUpdateStatus;
        
        // Asset Sources
        private List<AssetSource> _assetSources = new List<AssetSource>();
        private List<GithubSource> _githubSources = new List<GithubSource>();
        
        // Temp
        private string _newAssetSourceUrl = "https://assetstore.unity.com/";
        private string _newAssetSourceCategory = "3D Models";
        private string _newGithubRepoUrl = "https://github.com/Unity-Technologies/";
        private string _newGithubRepoDesc = "Unity official repositories";
        
        [MenuItem("Window/AI Assistant/Settings")]
        public static void ShowWindow()
        {
            GetWindow<AIAgentSettingsWindow>("AI Agent Settings");
        }
        
    private void OnEnable()
    {
        LoadSettings();
        LoadSources();
        
        // Якщо є валідний ключ Gemini, завантажуємо список доступних моделей у фоні
        if (_settings != null && !string.IsNullOrEmpty(_settings.geminiApiKey) && _settings.geminiApiKey.Trim().Length >= 10)
        {
            // Завантажуємо з невеликою затримкою, щоб уникнути блокування інтерфейсу
            EditorApplication.delayCall += () => FetchAvailableGeminiModels();
        }
    }
        
        private void LoadSettings()
        {
            _settings = AIAgentSettingsCreator.GetSettings(true);
            
            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
                _openAIKey = _settings.openAIApiKey;
                // Прибрано завантаження ключа Anthropic Claude тут, оскільки він тепер використовується лише в секції Claude
                Debug.Log($"AIAgentSettingsWindow: Налаштування успішно завантажено");
            }
            else
            {
                Debug.LogError("AIAgentSettingsWindow: Не вдалося завантажити налаштування");
            }
        }
        
        private void LoadSources()
        {
            _assetSources = new List<AssetSource>();
            _githubSources = new List<GithubSource>();
            
            // Додавання стандартних джерел
            if (!_assetSources.Any(s => s.Url.Contains("assetstore.unity.com")))
            {
                _assetSources.Add(new AssetSource 
                { 
                    Name = "Unity Asset Store",
                    Url = "https://assetstore.unity.com/",
                    Category = "All",
                    IsFree = true
                });
                
                _assetSources.Add(new AssetSource 
                { 
                    Name = "Unity Free Assets",
                    Url = "https://assetstore.unity.com/free-assets",
                    Category = "Free",
                    IsFree = true
                });
            }
            
            if (!_githubSources.Any(s => s.Url.Contains("Unity-Technologies")))
            {
                _githubSources.Add(new GithubSource
                {
                    Name = "Unity Technologies",
                    Url = "https://github.com/Unity-Technologies/",
                    Description = "Офіційні репозиторії Unity"
                });
                
                _githubSources.Add(new GithubSource
                {
                    Name = "Unity Open Projects",
                    Url = "https://github.com/UnityTechnologies/open-project-1",
                    Description = "Відкриті проекти Unity для навчання"
                });
            }
            
            // TODO: Тут буде завантаження користувацьких джерел із збереженого списку
        }
        
        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("AIAgentSettings не знайдено. Створюю нові налаштування...", MessageType.Warning);
                if (GUILayout.Button("Спробувати перезавантажити"))
                {
                    LoadSettings();
                }
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // API Keys section
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔑 API Ключі", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Інфо про API ключі", EditorGUIUtility.IconContent("_Help").image), GUILayout.Width(130)))
            {
                EditorUtility.DisplayDialog("Інформація про API ключі", 
                    "Для використання різних AI моделей необхідні відповідні API ключі:\n\n" +
                    "1. OpenAI API: https://platform.openai.com\n" +
                    "   - Потрібна реєстрація та додавання методу оплати\n" +
                    "   - Нові облікові записи отримують $5 безкоштовних кредитів\n\n" +
                    "2. Google Gemini API: https://ai.google.dev/\n" +
                    "   - Дозволяє отримати безкоштовний API ключ\n" +
                    "   - Має обмеження на кількість запитів\n\n" +
                    "3. Anthropic Claude API: https://console.anthropic.com\n" +
                    "   - Потрібна реєстрація та додавання методу оплати\n" +
                    "   - Надається пробний період з кредитами\n\n" +
                    "4. Ollama: https://ollama.ai/\n" +
                    "   - Безкоштовне програмне забезпечення для локального запуску моделей\n" +
                    "   - Не потребує API ключа, працює через локальний ендпоінт\n" +
                    "   - За замовчуванням: http://localhost:11434", 
                "Зрозуміло");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("OpenAI API Key:");
            string newOpenAIKey = EditorGUILayout.PasswordField(_openAIKey);
            if (newOpenAIKey != _openAIKey)
            {
                _openAIKey = newOpenAIKey;
                _settings.openAIApiKey = _openAIKey;
                EditorUtility.SetDirty(_settings);
            }
            
            // Додаємо кнопку для очищення поля API ключа OpenAI
            if (GUILayout.Button("Очистити ключ OpenAI", GUILayout.Width(150)))
            {
                _openAIKey = "";
                _settings.openAIApiKey = "";
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Отримати ключ OpenAI API", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image), 
                GUILayout.Height(28), GUILayout.Width(220)))
            {
                Application.OpenURL("https://platform.openai.com");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Перейдіть на офіційний сайт OpenAI для отримання API ключа. Після реєстрації ви зможете створити ключ у розділі 'API keys'.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            // Інформація про налаштування Anthropic Claude та Gemini перенесена у відповідні розділи для уникнення дублювання
            EditorGUILayout.HelpBox("Ключі для Anthropic Claude та Google Gemini доступні у відповідних розділах нижче.", MessageType.Info);
            
            // Додаємо поле для вибору моделі OpenAI
            // OpenAI секція
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Модель OpenAI GPT:", EditorStyles.boldLabel);
            string[] availableOpenAIModels = new string[] {
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k",
                "gpt-4o",
                "gpt-4o-mini",
                "gpt-4.1",
                "gpt-4.1-mini"
            };
            
            int currentModelIndex = Array.IndexOf(availableOpenAIModels, _settings.modelName);
            if (currentModelIndex < 0) currentModelIndex = -1; // -1 означає "Інша модель"
            
            // Додаємо пункт "Інша модель" до списку
            string[] displayedOptions = new string[availableOpenAIModels.Length + 1];
            Array.Copy(availableOpenAIModels, 0, displayedOptions, 0, availableOpenAIModels.Length);
            displayedOptions[availableOpenAIModels.Length] = "Інша модель...";
            
            int displayIndex = currentModelIndex >= 0 ? currentModelIndex : availableOpenAIModels.Length;
            int newDisplayIndex = EditorGUILayout.Popup("Вибрати модель OpenAI:", displayIndex, displayedOptions);
            
            // Якщо вибрано "Інша модель", показуємо текстове поле
            if (newDisplayIndex == availableOpenAIModels.Length)
            {
                _settings.modelName = EditorGUILayout.TextField("Назва моделі:", _settings.modelName);
                EditorGUILayout.HelpBox("Введіть точну назву моделі OpenAI. Для перевірки доступних моделей натисніть кнопку нижче.", MessageType.Info);
                EditorUtility.SetDirty(_settings);
            }
            // Якщо вибрано конкретну модель зі списку
            else if (newDisplayIndex != displayIndex)
            {
                _settings.modelName = availableOpenAIModels[newDisplayIndex];
                EditorUtility.SetDirty(_settings);
                Debug.Log($"Змінено модель OpenAI на: {_settings.modelName}");
            }
            
            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Перевірити ключ OpenAI"))
            {
                CheckOpenAIKey();
            }
            
            if (GUILayout.Button("Доступні моделі GPT"))
            {
                CheckGptModels();
            }
            GUILayout.EndHorizontal();
            
            // Секція Google Gemini
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Google Gemini API", EditorStyles.boldLabel);
            
            bool newUseGemini = EditorGUILayout.Toggle("Використовувати Gemini API:", _settings.useGeminiAPI);
            if (newUseGemini != _settings.useGeminiAPI)
            {
                _settings.useGeminiAPI = newUseGemini;
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUI.BeginDisabledGroup(!_settings.useGeminiAPI);
            
            EditorGUILayout.LabelField("Gemini API Key:");
            string newGeminiKey = EditorGUILayout.PasswordField(_settings.geminiApiKey);
            if (newGeminiKey != _settings.geminiApiKey)
            {
                // Застосовуємо автоматичну корекцію формату API ключа
                _settings.geminiApiKey = APIKeyHelper.FixGeminiApiKey(newGeminiKey);
                EditorUtility.SetDirty(_settings);
            }
            
            // Додаємо кнопку для очищення поля API ключа Gemini
            if (GUILayout.Button("Очистити ключ Gemini", GUILayout.Width(150)))
            {
                _settings.geminiApiKey = "";
                EditorUtility.SetDirty(_settings);
            }
            
            // Показуємо інформацію про стан ключа API
            if (!string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                APIKeyHelper.ShowGeminiKeyStatus(_settings.geminiApiKey);
            }
            
            EditorGUILayout.Space(2);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Перевірити ключ", GUILayout.Height(24), GUILayout.Width(120)))
            {
                CheckGeminiKey();
            }
            if (string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                EditorGUILayout.HelpBox("Введіть API ключ для використання Google Gemini", MessageType.Info);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Отримати ключ Gemini API", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image), 
                GUILayout.Height(28), GUILayout.Width(220)))
            {
                Application.OpenURL("https://makersuite.google.com/app/apikey");
            }
            
            if (GUILayout.Button(new GUIContent(" Докладний посібник", EditorGUIUtility.IconContent("_Help").image), 
                GUILayout.Height(28), GUILayout.Width(160)))
            {
                // Перевіряємо, чи існує файл у проекті
                string guidePath = AssetDatabase.GUIDToAssetPath(
                    AssetDatabase.FindAssets("GeminiApiGuide t:TextAsset").Length > 0 ? 
                    AssetDatabase.FindAssets("GeminiApiGuide t:TextAsset")[0] : "");
                
                if (!string.IsNullOrEmpty(guidePath))
                {
                    // Відкриваємо файл у редакторі Unity
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(guidePath, 1);
                }
                else
                {
                    // Якщо файл не знайдено, відкриваємо веб-сторінку
                    Application.OpenURL("https://ai.google.dev/tutorials/setup");
                }
            }
            
            if (GUILayout.Button(new GUIContent(" Усунення проблем", EditorGUIUtility.IconContent("console.warnicon.sml").image), 
                GUILayout.Height(28), GUILayout.Width(160)))
            {
                // Відкриваємо вікно усунення проблем з API
                APITroubleshooterWindow.ShowWindow();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Google Gemini пропонує безкоштовний API ключ з обмеженням на кількість запитів. Якщо виникають помилки з ключем, ознайомтеся з докладним посібником.", MessageType.Info);
            
            // Інформація про моделі Gemini
            Dictionary<string, string> geminiModelDescriptions = new Dictionary<string, string>()
            {
                { "gemini-pro", "Стандартна текстова модель для спілкування та генерації коду" },
                { "gemini-pro-vision", "Модель з підтримкою обробки зображень" },
                { "gemini-1.5-pro", "Нова покращена модель (більш потужна, багатомовна)" },
                { "gemini-1.5-flash", "Швидка модель з оптимізованою продуктивністю" },
                { "gemini-1.5-pro-latest", "Найновіша версія Gemini з покращеннями" }
            };
            
            // Отримуємо доступні моделі з кешу або базові, якщо кеш відсутній
            List<string> availableGeminiModels = new List<string>();
            if (_cachedGeminiModels != null && _cachedGeminiModels.Count > 0)
            {
                availableGeminiModels = _cachedGeminiModels;
            }
            else
            {
                availableGeminiModels = new List<string> { "gemini-pro", "gemini-pro-vision", "gemini-1.5-pro", "gemini-1.5-flash" };
            }
            
            // Створення масиву міток для випадаючого списку
            string[] modelLabels = new string[availableGeminiModels.Count];
            for (int i = 0; i < availableGeminiModels.Count; i++)
            {
                string model = availableGeminiModels[i];
                string description = "Модель Gemini";
                
                // Шукаємо опис у словнику або використовуємо базовий
                if (geminiModelDescriptions.ContainsKey(model))
                {
                    description = geminiModelDescriptions[model];
                }
                
                modelLabels[i] = $"{model} ({description})";
            }
            
            // Знаходимо поточний індекс моделі
            int currentGeminiModel = availableGeminiModels.IndexOf(_settings.geminiModelName);
            if (currentGeminiModel < 0) currentGeminiModel = 0;
            
            // Відображаємо випадаючий список
            int newGeminiModel = EditorGUILayout.Popup("Модель Gemini:", currentGeminiModel, modelLabels);
            if (newGeminiModel != currentGeminiModel && newGeminiModel < availableGeminiModels.Count)
            {
                _settings.geminiModelName = availableGeminiModels[newGeminiModel];
                EditorUtility.SetDirty(_settings);
                Debug.Log($"Змінено модель Gemini на: {_settings.geminiModelName}");
            }
            
            // Кнопка оновлення списку доступних моделей
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Оновити список моделей", GUILayout.Width(150)))
            {
                FetchAvailableGeminiModels();
            }
            
            // Відображаємо статус оновлення, якщо він є
            if (!string.IsNullOrEmpty(_geminiModelUpdateStatus))
            {
                EditorGUILayout.LabelField(_geminiModelUpdateStatus, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            // Секція Anthropic Claude
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Anthropic Claude API", EditorStyles.boldLabel);
            
            bool newUseAnthropicClaude = EditorGUILayout.Toggle("Використовувати Claude API:", _settings.useAnthropicClaudeAPI);
            if (newUseAnthropicClaude != _settings.useAnthropicClaudeAPI) 
            {
                _settings.useAnthropicClaudeAPI = newUseAnthropicClaude;
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUI.BeginDisabledGroup(!_settings.useAnthropicClaudeAPI);
            
            EditorGUILayout.LabelField("Claude API Key:");
            string newClaudeKey = EditorGUILayout.PasswordField(_settings.anthropicApiKey);
            if (newClaudeKey != _settings.anthropicApiKey)
            {
                _settings.anthropicApiKey = newClaudeKey;
                EditorUtility.SetDirty(_settings);
            }
            
            // Додаємо кнопку для очищення поля API ключа Claude
            if (GUILayout.Button("Очистити ключ Claude", GUILayout.Width(150)))
            {
                _settings.anthropicApiKey = "";
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Отримати ключ Claude API", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image), 
                GUILayout.Height(28), GUILayout.Width(220)))
            {
                Application.OpenURL("https://console.anthropic.com");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Зареєструйтеся на сайті Anthropic для доступу до консолі розробника. У розділі 'API Keys' ви можете створити ключ для використання з моделями Claude.", MessageType.Info);
            
            string[] claudeModels = new string[] { 
                "claude-3-haiku-20240307",
                "claude-3-sonnet-20240229", 
                "claude-3-opus-20240229" 
            };
            int currentClaudeModel = Array.IndexOf(claudeModels, _settings.claudeModelName);
            if (currentClaudeModel < 0) currentClaudeModel = 0;
            
            int newClaudeModel = EditorGUILayout.Popup("Модель Claude:", currentClaudeModel, claudeModels);
            if (newClaudeModel != currentClaudeModel)
            {
                _settings.claudeModelName = claudeModels[newClaudeModel];
                EditorUtility.SetDirty(_settings);
                Debug.Log($"Змінено модель Claude на: {_settings.claudeModelName}");
            }
            
            if (GUILayout.Button("Перевірити ключ Claude"))
            {
                CheckAnthropicKey();
            }
            
            EditorGUI.EndDisabledGroup();
            
            // Секція Ollama (локальні моделі)
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ollama (локальні моделі)", EditorStyles.boldLabel);
            
            bool newUseOllama = EditorGUILayout.Toggle("Використовувати Ollama:", _settings.useOllamaAPI);
            if (newUseOllama != _settings.useOllamaAPI)
            {
                _settings.useOllamaAPI = newUseOllama;
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUI.BeginDisabledGroup(!_settings.useOllamaAPI);
            
            EditorGUILayout.LabelField("Ollama API Endpoint:");
            string newOllamaEndpoint = EditorGUILayout.TextField(_settings.ollamaEndpoint);
            if (newOllamaEndpoint != _settings.ollamaEndpoint)
            {
                _settings.ollamaEndpoint = newOllamaEndpoint;
                EditorUtility.SetDirty(_settings);
            }
            
            EditorGUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Завантажити Ollama", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image), 
                GUILayout.Height(28), GUILayout.Width(220)))
            {
                Application.OpenURL("https://ollama.ai/");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Ollama дозволяє запускати локальні LLM моделі на вашому комп'ютері безкоштовно. Завантажте та встановіть Ollama, потім запустіть його і вкажіть ендпоінт за замовчуванням (http://localhost:11434).", MessageType.Info);
            
            EditorGUILayout.LabelField("Ollama Model:");
            string newOllamaModel = EditorGUILayout.TextField(_settings.ollamaModelName);
            if (newOllamaModel != _settings.ollamaModelName)
            {
                _settings.ollamaModelName = newOllamaModel;
                EditorUtility.SetDirty(_settings);
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Перевірити Ollama"))
            {
                CheckOllamaConnection();
            }
            
            if (GUILayout.Button("Доступні моделі"))
            {
                CheckOllamaModels();
            }
            GUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Asset Sources section
            EditorGUILayout.Space(20);
            GUILayout.Label("🛒 Джерела асетів", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            // Існуючі джерела
            for (int i = 0; i < _assetSources.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                
                // Ім'я та кнопка видалення
                EditorGUILayout.LabelField(_assetSources[i].Name, EditorStyles.boldLabel);
                if (GUILayout.Button("✖", GUILayout.Width(25)))
                {
                    _assetSources.RemoveAt(i);
                    i--;
                    continue;
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"URL: {_assetSources[i].Url}");
                EditorGUILayout.LabelField($"Категорія: {_assetSources[i].Category}");
                EditorGUILayout.LabelField($"Тільки безкоштовні: {(_assetSources[i].IsFree ? "Так" : "Ні")}");
                
                EditorGUILayout.EndVertical();
            }
            
            // Додавання нового джерела
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Додати нове джерело асетів:", EditorStyles.boldLabel);
            
            _newAssetSourceUrl = EditorGUILayout.TextField("URL:", _newAssetSourceUrl);
            _newAssetSourceCategory = EditorGUILayout.TextField("Категорія:", _newAssetSourceCategory);
            bool isFree = EditorGUILayout.Toggle("Тільки безкоштовні:", true);
            
            if (GUILayout.Button("Додати джерело асетів"))
            {
                if (!string.IsNullOrEmpty(_newAssetSourceUrl))
                {
                    string name = Path.GetHost(_newAssetSourceUrl);
                    if (string.IsNullOrEmpty(name)) name = "Нове джерело";
                    
                    _assetSources.Add(new AssetSource
                    {
                        Name = name,
                        Url = _newAssetSourceUrl,
                        Category = _newAssetSourceCategory,
                        IsFree = isFree
                    });
                    
                    _newAssetSourceUrl = "https://assetstore.unity.com/";
                    _newAssetSourceCategory = "3D Models";
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // GitHub Sources section
            EditorGUILayout.Space(20);
            GUILayout.Label("📂 Джерела GitHub", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            // Існуючі репозиторії
            for (int i = 0; i < _githubSources.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                
                // Ім'я та кнопка видалення
                EditorGUILayout.LabelField(_githubSources[i].Name, EditorStyles.boldLabel);
                if (GUILayout.Button("✖", GUILayout.Width(25)))
                {
                    _githubSources.RemoveAt(i);
                    i--;
                    continue;
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"URL: {_githubSources[i].Url}");
                EditorGUILayout.LabelField($"Опис: {_githubSources[i].Description}");
                
                EditorGUILayout.EndVertical();
            }
            
            // Додавання нового репозиторію
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Додати новий репозиторій GitHub:", EditorStyles.boldLabel);
            
            _newGithubRepoUrl = EditorGUILayout.TextField("URL:", _newGithubRepoUrl);
            _newGithubRepoDesc = EditorGUILayout.TextField("Опис:", _newGithubRepoDesc);
            
            if (GUILayout.Button("Додати репозиторій GitHub"))
            {
                if (!string.IsNullOrEmpty(_newGithubRepoUrl))
                {
                    string name = GetGithubRepoName(_newGithubRepoUrl);
                    if (string.IsNullOrEmpty(name)) name = "Новий репозиторій";
                    
                    _githubSources.Add(new GithubSource
                    {
                        Name = name,
                        Url = _newGithubRepoUrl,
                        Description = _newGithubRepoDesc
                    });
                    
                    _newGithubRepoUrl = "https://github.com/Unity-Technologies/";
                    _newGithubRepoDesc = "Репозиторій Unity";
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Save Settings
            EditorGUILayout.Space(20);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Зберегти всі налаштування", GUILayout.Height(30)))
            {
                SaveSettings();
            }
            GUI.backgroundColor = Color.white;
            
            // Додаємо секцію з кнопками навігації та підтримки
            EditorGUILayout.Space(20);
            GUILayout.Label("📚 Документація та підтримка", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.HelpBox("Якщо у вас виникли питання щодо використання AI Agent або ви хочете дізнатися більше про функціональність, скористайтеся посиланнями нижче.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Документація", EditorGUIUtility.IconContent("_Help").image), GUILayout.Height(30)))
            {
                Application.OpenURL(DOCUMENTATION_URL);
            }
            
            if (GUILayout.Button(new GUIContent(" Повідомити про проблему", EditorGUIUtility.IconContent("console.warnicon").image), GUILayout.Height(30)))
            {
                Application.OpenURL(SUPPORT_URL);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void SaveSettings()
        {
            if (_settings == null)
            {
                Debug.LogError("AIAgentSettingsWindow: Не вдалося зберегти налаштування, об'єкт не створено");
                return;
            }
            
            _settings.openAIApiKey = _openAIKey;
            // Ключ Anthropic Claude вже збережено при редагуванні поля в розділі Claude API
            
            // TODO: Зберігати джерела асетів і репозиторії в PlayerPrefs або окремому файлі
            
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log("AIAgentSettingsWindow: Налаштування успішно збережено");
        }
        
        private string GetGithubRepoName(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            
            // Виокремлюємо ім'я репозиторію з URL
            string[] parts = url.TrimEnd('/').Split('/');
            if (parts.Length >= 2)
            {
                return parts[parts.Length - 1];
            }
            
            return "";
        }
        
        /// <summary>
        /// Перевіряє доступні моделі GPT для поточного API ключа
        /// </summary>
        private async void CheckGptModels()
        {
            if (string.IsNullOrEmpty(_openAIKey))
            {
                EditorUtility.DisplayDialog("Помилка", "Будь ласка, введіть API ключ OpenAI перед перевіркою моделей.", "OK");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Перевірка моделей", "Отримання списку доступних моделей OpenAI...", 0.5f);
                
                var openAiService = new OpenAIService(_settings);
                var availableModels = await openAiService.CheckAvailableModels();
                
                EditorUtility.ClearProgressBar();
                
                if (availableModels.Count > 0)
                {
                    // Групуємо моделі за категоріями для кращого відображення
                    var gpt4o = availableModels.Where(m => m.StartsWith("gpt-4o")).OrderBy(m => m).ToList();
                    var gpt41 = availableModels.Where(m => m.StartsWith("gpt-4.1")).OrderBy(m => m).ToList();
                    var gpt35 = availableModels.Where(m => m.StartsWith("gpt-3.5")).OrderBy(m => m).ToList();
                    var otherModels = availableModels.Where(m => 
                        !m.StartsWith("gpt-4o") && 
                        !m.StartsWith("gpt-4.1") && 
                        !m.StartsWith("gpt-3.5")).OrderBy(m => m).ToList();
                    
                    // Визначаємо рекомендовану модель
                    string recommendedModel = "";
                    if (gpt4o.Count > 0) recommendedModel = gpt4o[0];
                    else if (gpt41.Count > 0) recommendedModel = gpt41[0];
                    else if (gpt35.Count > 0) recommendedModel = gpt35[0];
                    
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Для вашого API ключа доступні наступні моделі:");
                    
                    // Додаємо рекомендовану модель якщо вона є
                    if (!string.IsNullOrEmpty(recommendedModel))
                    {
                        sb.AppendLine($"\n🌟 Рекомендована модель: {recommendedModel}");
                    }
                    
                    // Додаємо групи моделей
                    if (gpt4o.Count > 0)
                    {
                        sb.AppendLine("\n📊 GPT-4o моделі:");
                        gpt4o.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    if (gpt41.Count > 0)
                    {
                        sb.AppendLine("\n📊 GPT-4.1 моделі:");
                        gpt41.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    if (gpt35.Count > 0)
                    {
                        sb.AppendLine("\n📊 GPT-3.5 моделі:");
                        gpt35.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    if (otherModels.Count > 0)
                    {
                        sb.AppendLine("\n📊 Інші моделі:");
                        otherModels.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    sb.AppendLine($"\nПоточна модель: {_settings.modelName}");
                    
                    // Додаємо рекомендацію, якщо поточна модель недоступна
                    if (!availableModels.Contains(_settings.modelName) && !string.IsNullOrEmpty(recommendedModel))
                    {
                        sb.AppendLine($"\n⚠️ Поточна модель недоступна для вашого API ключа!");
                        sb.AppendLine($"Рекомендуємо змінити модель на {recommendedModel}");
                    }
                    
                    EditorUtility.DisplayDialog("Доступні моделі GPT", sb.ToString(), "Закрити");
                    
                    // Записуємо доступні моделі у лог
                    Debug.Log($"Доступні моделі GPT:\n{string.Join("\n", availableModels.OrderBy(m => m))}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Результат перевірки", 
                        "Не вдалося отримати список доступних моделей. Перевірте правильність API ключа або наявність прав доступу.", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Помилка", $"Помилка при перевірці доступних моделей: {ex.Message}", "OK");
                Debug.LogError($"Помилка при перевірці доступних моделей: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Клас для зберігання даних про джерело асетів
    /// </summary>
    [System.Serializable]
    public class AssetSource
    {
        public string Name;
        public string Url;
        public string Category;
        public bool IsFree;
    }
    
    /// <summary>
    /// Клас для зберігання даних про GitHub репозиторій
    /// </summary>
    [System.Serializable]
    public class GithubSource
    {
        public string Name;
        public string Url;
        public string Description;
    }
    
    /// <summary>
    /// Допоміжний клас для роботи з URL
    /// </summary>
    public static class Path
    {
        public static string GetHost(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            
            try
            {
                Uri uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return "";
            }
        }
    }
}
