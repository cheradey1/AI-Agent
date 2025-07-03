/*
 * AIAgentUnity.cs
 * 
 * Версія: 2.0.0
 * Дата оновлення: 18-05-2025
 * 
 * Зміни:
 * - Спрощено інтерфейс: прибрано кнопки і залишено тільки чат-інтерфейс
 * - Перенесено функціональність кнопок у команди, які вводяться через чат
 * - Додано нові команди: #generate_hero, #generate_battlefield, #connect_scripts, #fix_errors
 * - Додано допоміжні команди: #help, #clear_chat, #settings, #pause
 * - Розширено інформаційну панель з описом усіх доступних команд
 * - Виправлено різні помилки у коді проекту
 */

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO; // Required for Path and File operations
using System;
// Використовуємо Unity-сумісні рішення замість System.Speech
// Для STT/TTS потрібні Unity-сумісні плагіни або сервіси
using System.Linq; // Required for Linq operations
using System.Text.RegularExpressions; // Required for Regex
// System.Text вже використовується вище
using UnityEditor.SceneManagement; // Required for Scene operations
using UnityEngine.SceneManagement; // Required for SceneManager
using UnityEngine.Networking; // Required for URL encoding

namespace UnityAIAgent
{
    public class AIAgentUnity : EditorWindow
    {
        // Settings
        private AIAgentSettings _settings;
        private SerializedObject _serializedSettings;

        // Chat panel
        private string _userInput = "";
        private Vector2 _chatScrollPosition;
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private bool _isWaitingForAIResponse = false;
        private bool _isSpeechToTextActive = false; // Для стану голосового вводу
        private bool _isTextToSpeechEnabled = false; // Для стану озвучування
        private bool _showAPITroubleshooterButton = false; // Для показу кнопки усунення проблем з API
        private float _recordingStartTime; // Час початку запису голосу
        
        // Публічна властивість для доступу до статусу голосового вводу
        public bool IsSpeechToTextActive
        {
            get { return _isSpeechToTextActive; }
            set { _isSpeechToTextActive = value; }
        }
        
        // Публічна властивість для доступу до часу початку запису голосу
        public float RecordingStartTime
        {
            get { return _recordingStartTime; }
        }

        // Game Parameters panel
        private string _apiKeyInput = ""; 
        private GameType _selectedGameType = GameType.Platformer;
        private PlayerCount _selectedPlayerCount = PlayerCount.SinglePlayer;
        private ArtStyle _selectedArtStyle = ArtStyle.SciFi;
        private MapSize _selectedMapSize = MapSize.Medium;
        private string _gameGoal = "Collect resources and survive";
        // Використовуємо для майбутніх оновлень кольорової теми
        #pragma warning disable 0414
        private ColorTheme _selectedColorTheme = ColorTheme.Light;
        #pragma warning restore 0414
        private Vector2 _paramsScrollPosition;
        private int _selectedServiceIndex = 0; // Для вибору AI сервісу
        private string[] _availableServiceNames = new string[0];
        
        // Asset Store / GitHub search
        // Деякі поля зараз не використовуються у спрощеному інтерфейсі, але можуть бути потрібні в майбутньому
        #pragma warning disable 0414
        private string _assetSearchQuery = ""; 
        private Vector2 _assetSearchScrollPosition;
        private string _githubSearchQuery = "";
        private string _githubRepoUrl = "";
        private Vector2 _githubSearchScrollPosition;
        private List<string> _assetSearchResults = new List<string>();
        private List<string> _githubSearchResults = new List<string>();
        #pragma warning restore 0414
        
        // Progress/Output panel
        private string _progressText = "Idle";
        private List<string> _insertedAssets = new List<string>();
        private string _scriptPreview = "// Script preview will appear here";
        // Використовуємо для перемикання вкладок у майбутніх оновленнях
        #pragma warning disable 0414
        private int _selectedOutputTab = 0;
        #pragma warning restore 0414
        private Vector2 _assetsScrollPosition;
        private Vector2 _scriptPreviewScrollPosition;


        // Константи UI
        private const float PADDING = 10f;
        
        // Шляхи до файлів з ресурсами
        private const string FreeModelsFileName = "FreeModels.md";
        private const string FreeUnityAssetsFileName = "FreeUnityAssets.md";

        // Константи для голосового вводу
        private const int MAX_RECORDING_TIME_SEC = 10;

        // Services
        private IAIService _currentService;
        private AIServiceFactory _serviceFactory;
        private ChatHistoryService _historyService; // For managing chat history persistence
        private const string ChatHistoryFileName = "ai_agent_chat_history.json";

        // Audio Services
        private AudioManager _audioManager;


        [MenuItem("Tools/AI Agent")]
        [MenuItem("Window/AI Assistant")]
        public static void ShowWindow()
        {
            GetWindow<AIAgentUnity>("AI Agent");
        }

        private void OnEnable()
        {
            // Завантажуємо налаштування
            LoadSettings();
            
            // Ініціалізуємо AudioManager
            _audioManager = AudioManager.Instance;
            
            // Перевіряємо наявність необхідних пакетів
            CheckRequiredPackages();
            
            if (_settings != null)
            {
                _apiKeyInput = _settings.openAIApiKey;
                _serviceFactory = new AIServiceFactory(_settings);
                
                // Створюємо історію чату, якщо потрібно
                _historyService = new ChatHistoryService(System.IO.Path.Combine(Application.persistentDataPath, ChatHistoryFileName), _settings.maxHistoryLength);
                
                // Завантажуємо історію чату
                List<string> historyEntries = _historyService.LoadHistory();
                if (historyEntries != null && historyEntries.Count > 0)
                {
                    _chatHistory = historyEntries.Select(entry => {
                        var parts = entry.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        return new ChatMessage(parts.Length > 1 ? parts[0] : "Unknown", parts.Length > 1 ? parts[1] : entry);
                    }).ToList();
                }
                else
                {
                    _chatHistory = new List<ChatMessage>
                    {
                        new ChatMessage("System", "Ласкаво просимо до AI Unity Agent! Чим я можу допомогти?")
                    };
                }
                
                // Асинхронно визначаємо доступні сервіси
                InitializeServicesAsync();
            }
            else
            {
                Debug.LogError("AIAgentSettings not found. Creating new settings.");
                // Автоматично створюємо налаштування і завантажуємо їх
                _settings = AIAgentSettingsCreator.CreateSettings();
                
                if (_settings != null) {
                    _serializedSettings = new SerializedObject(_settings);
                    _chatHistory = new List<ChatMessage>
                    {
                        new ChatMessage("System", "Налаштування було автоматично створено.")
                    };
                    
                    // Створюємо фабрику сервісів та історію чату
                    _serviceFactory = new AIServiceFactory(_settings);
                    _historyService = new ChatHistoryService(System.IO.Path.Combine(Application.persistentDataPath, ChatHistoryFileName), _settings.maxHistoryLength);
                    
                    // Асинхронно визначаємо доступні сервіси
                    InitializeServicesAsync();
                    
                    Debug.Log("AIAgentUnity: Налаштування успішно створені та завантажені.");
                }
                else {
                    _chatHistory = new List<ChatMessage>
                    {
                        new ChatMessage("System", "Не вдалося створити налаштування. Будь ласка, створіть їх вручну через меню Window > AI Assistant > Create Settings.")
                    };
                    Debug.LogError("AIAgentUnity: Не вдалося створити налаштування автоматично.");
                }
            }
        }
        
        /// <summary>
        /// Асинхронно ініціалізує доступні AI сервіси
        /// </summary>
        private async void InitializeServicesAsync()
        {
            try
            {
                _progressText = "Ініціалізація AI сервісів...";
                
                // Завантажуємо свіжі налаштування з можливо виявленими API ключами
                if (_settings.autoDetectAPIKeys)
                {
                    AIAgentSettingsCreator.GetSettings(true); // Форсуємо оновлення налаштувань з середовища
                }
                
                // Отримуємо список доступних сервісів
                var availableServicesList = _serviceFactory.GetAvailableServices();
                
                // Якщо список порожній або містить тільки "Auto"/"Demo" та увімкнено автовиявлення безкоштовних моделей
                if ((availableServicesList.Count == 0 || 
                    (availableServicesList.Count == 1 && (availableServicesList[0] == "Auto" || availableServicesList[0] == "Demo"))) 
                    && _settings.enableFreeModels)
                {
                    _progressText = "Шукаємо доступні AI моделі...";
                    
                    // Перевіряємо, чи доступний Ollama
                    bool ollamaAvailable = await _serviceFactory.CheckOllamaAvailabilityAsync();
                    
                    if (ollamaAvailable)
                    {
                        _settings.useOllamaAPI = true;
                        
                        // Якщо "Ollama" ще немає у списку
                        if (!availableServicesList.Contains("Ollama"))
                        {
                            availableServicesList.Add("Ollama");
                        }
                        
                        EditorUtility.SetDirty(_settings);
                        AssetDatabase.SaveAssets();
                        _progressText = "Знайдено локальний Ollama сервіс";
                        _chatHistory.Add(new ChatMessage("System", "Знайдено локальний Ollama сервіс. Для запуску локальних моделей, переконайтесь, що Ollama запущено з моделлю llama3."));
                    }
                    else if (_settings.enableDemoMode) // Якщо Ollama недоступний, але дозволений демо-режим
                    {
                        // Якщо "Demo" ще немає у списку
                        if (!availableServicesList.Contains("Demo"))
                        {
                            availableServicesList.Add("Demo");
                        }
                        
                        _progressText = "Активовано демо-режим";
                        _chatHistory.Add(new ChatMessage("System", "Не знайдено налаштованих AI сервісів. Активовано демо-режим з обмеженою функціональністю."));
                    }
                    
                    Repaint(); // Оновлюємо інтерфейс
                }
                
                // Оновлюємо список доступних сервісів
                _availableServiceNames = availableServicesList.ToArray();
                
                // Якщо є доступні сервіси, вибираємо перший
                if (_availableServiceNames.Length > 0)
                {
                    _selectedServiceIndex = 0; // Вибираємо за замовчуванням перший доступний сервіс
                    
                    // Якщо доступний "Demo" або "Auto", але є інші варіанти, вибираємо не-демо сервіс
                    if (_availableServiceNames.Length > 1 && 
                        (_availableServiceNames[0] == "Demo" || _availableServiceNames[0] == "Auto"))
                    {
                        for (int i = 1; i < _availableServiceNames.Length; i++)
                        {
                            if (_availableServiceNames[i] != "Demo" && _availableServiceNames[i] != "Auto")
                            {
                                _selectedServiceIndex = i;
                                break;
                            }
                        }
                    }
                    
                    // Створюємо сервіс
                    SwitchService(_availableServiceNames[_selectedServiceIndex]);
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", "Не знайдено налаштованих або доступних AI сервісів. Будь ласка, перевірте налаштування."));
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка ініціалізації AI сервісів: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"Помилка ініціалізації: {ex.Message}"));
            }
        }
        
        private void OnDisable()
        {
            if (_historyService != null && _chatHistory.Any())
            {
                _historyService.SaveHistory(_chatHistory.Select(msg => $"{msg.Sender}: {msg.Text}").ToList());
            }
        }

        private void SwitchService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                Debug.LogWarning("Неможливо перемкнутися на порожній сервіс.");
                return;
            }
            
            // Зберігаємо попередню модель
            string previousServiceName = _currentService != null ? _currentService.GetServiceName() : "";
            
            // Створюємо новий сервіс
            _currentService = _serviceFactory.CreateService(serviceName);
            
            if (_currentService != null && _currentService.IsConfigured())
            {
                // Якщо успішно перемкнулися на новий сервіс
                if (serviceName != previousServiceName)
                {
                    if (serviceName == "Demo")
                    {
                        _chatHistory.Add(new ChatMessage("System", "Увімкнено демо-режим. У цьому режимі доступні лише базові відповіді на типові запитання."));
                    }
                    else if (serviceName == "Auto")
                    {
                        _chatHistory.Add(new ChatMessage("System", $"Увімкнено автоматичний вибір сервісу. Активний сервіс: {_currentService.GetServiceName()}"));
                    }
                    else
                    {
                        _chatHistory.Add(new ChatMessage("System", $"Перемкнуто на {serviceName}."));
                    }
                }
            }
            else if (_currentService != null)
            {
                _chatHistory.Add(new ChatMessage("System", $"Перемкнуто на {serviceName}, але він не налаштований. Будь ласка, перевірте налаштування."));
            }
            else
            {
                // Якщо не вдалося створити сервіс, пробуємо автоматично визначити доступний
                if (_settings?.enableFreeModels == true)
                {
                    _chatHistory.Add(new ChatMessage("System", $"Не вдалося перемкнутися на {serviceName}. Шукаємо доступні безкоштовні моделі..."));
                    DetectAvailableServices();
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", $"Не вдалося перемкнутися на {serviceName}. Сервіс недоступний або відсутні налаштування."));
                }
            }
        }
        
        /// <summary>
        /// Автоматично визначає і підключається до доступних сервісів AI
        /// </summary>
        private async void DetectAvailableServices()
        {
            bool serviceFound = false;
            
            _chatHistory.Add(new ChatMessage("System", "Пошук доступних AI сервісів..."));
            
            // 1. Перевірка наявності Ollama (локальні моделі)
            if (_settings.enableFreeModels || _settings.enableLocalLlama)
            {
                _progressText = "Шукаємо локальні моделі Ollama...";
                bool ollamaAvailable = await _serviceFactory.CheckOllamaAvailabilityAsync();
                
                if (ollamaAvailable)
                {
                    _settings.useOllamaAPI = true;
                    _currentService = _serviceFactory.CreateService("Ollama");
                    _chatHistory.Add(new ChatMessage("System", "Знайдено локальний сервіс Ollama для безкоштовних моделей."));
                    serviceFound = true;
                    
                    // Запитуємо доступні моделі
                    await GetOllamaModels();
                }
            }
            
            // 2. Перевірка наявності API ключів у середовищі
            if (!serviceFound && _settings.autoDetectAPIKeys)
            {
                _progressText = "Шукаємо API ключі...";
                
                // Оновлюємо налаштування з можливими ключами
                AIAgentSettingsCreator.GetSettings(true);
                
                // Перевіряємо наявність ключів та пробуємо підключитися
                if (!string.IsNullOrEmpty(_settings.openAIApiKey))
                {
                    _currentService = _serviceFactory.CreateService("OpenAI");
                    _chatHistory.Add(new ChatMessage("System", "Знайдено та активовано ключ OpenAI API."));
                    serviceFound = true;
                }
                else if (!string.IsNullOrEmpty(_settings.geminiApiKey))
                {
                    _settings.useGeminiAPI = true;
                    _currentService = _serviceFactory.CreateService("Google Gemini");
                    _chatHistory.Add(new ChatMessage("System", "Знайдено та активовано ключ Google Gemini API."));
                    serviceFound = true;
                }
                else if (!string.IsNullOrEmpty(_settings.anthropicApiKey))
                {
                    _settings.useAnthropicClaudeAPI = true;
                    _currentService = _serviceFactory.CreateService("Anthropic Claude");
                    _chatHistory.Add(new ChatMessage("System", "Знайдено та активовано ключ Anthropic Claude API."));
                    serviceFound = true;
                }
            }
            
            // 3. Якщо нічого не знайдено, але дозволений демо-режим
            if (!serviceFound && _settings.enableDemoMode)
            {
                _currentService = _serviceFactory.CreateService("Demo");
                _chatHistory.Add(new ChatMessage("System", "Не знайдено жодного налаштованого AI сервісу. Активовано демо-режим."));
                serviceFound = true;
            }
            
            // 4. Якщо нічого не допомогло
            if (!serviceFound)
            {
                _chatHistory.Add(new ChatMessage("System", "Не знайдено жодного доступного AI сервісу. Будь ласка, налаштуйте один із сервісів у налаштуваннях."));
                _progressText = "Немає доступних AI сервісів";
            }
            else
            {
                _progressText = $"AI сервіс активний: {_currentService.GetServiceName()}";
                
                // Зберігаємо зміни у налаштуваннях
                if (_settings != null)
                {
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                }
            }
            
            // Перемальовуємо вікно
            Repaint();
        }
        
        /// <summary>
        /// Отримує доступні моделі для поточного сервісу
        /// </summary>
        private async void GetAvailableModelsForCurrentService()
        {
            if (_currentService == null)
            {
                _chatHistory.Add(new ChatMessage("System", "Неможливо перевірити моделі: AI сервіс не вибраний."));
                return;
            }
            
            string serviceName = _currentService.GetServiceName();
            
            _progressText = $"Отримуємо список моделей для {serviceName}...";
            _chatHistory.Add(new ChatMessage("System", $"Перевіряємо доступні моделі для {serviceName}..."));
            
            try
            {
                List<string> models = await _serviceFactory.GetAvailableModelsForService(serviceName);
                
                if (models != null && models.Count > 0)
                {
                    _chatHistory.Add(new ChatMessage("System", $"Доступні моделі для {serviceName}:"));
                    _chatHistory.Add(new ChatMessage("System", string.Join("\n", models)));
                    _progressText = $"Знайдено {models.Count} моделей для {serviceName}";
                    
                    // Рекомендуємо найкращу модель
                    if (serviceName == "Ollama")
                    {
                        // Для Ollama рекомендуємо llama3
                        if (models.Contains("llama3"))
                        {
                            _chatHistory.Add(new ChatMessage("System", "Рекомендована модель для Ollama: llama3"));
                            _settings.ollamaModelName = "llama3";
                        }
                    }
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", $"Не вдалося отримати список моделей для {serviceName} або список порожній."));
                    _progressText = "Моделі не знайдено";
                }
            }
            catch (Exception ex)
            {
                _chatHistory.Add(new ChatMessage("System", $"Помилка при отриманні списку моделей: {ex.Message}"));
                _progressText = "Помилка отримання моделей";
                Debug.LogError($"Error getting models: {ex}");
            }
            
            // Перемальовуємо вікно
            Repaint();
        }
        
        /// <summary>
        /// Отримує список доступних моделей Ollama
        /// </summary>
        private async Task GetOllamaModels()
        {
            if (_settings?.useOllamaAPI != true)
                return;
            
            try
            {
                var ollamaService = _serviceFactory.CreateService("Ollama") as OllamaService;
                if (ollamaService == null)
                    return;
                
                List<string> models = await ollamaService.GetAvailableModels();
                
                if (models.Count > 0)
                {
                    _chatHistory.Add(new ChatMessage("System", "Доступні локальні моделі:"));
                    _chatHistory.Add(new ChatMessage("System", string.Join(", ", models)));
                    
                    // Якщо llama3 доступна, використовуємо її
                    if (models.Contains("llama3"))
                    {
                        _settings.ollamaModelName = "llama3";
                    }
                    // Інакше вибираємо першу доступну модель
                    else if (models.Count > 0)
                    {
                        _settings.ollamaModelName = models[0];
                    }
                    
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", "Локальні моделі Ollama не знайдено. Щоб додати модель, виконайте команду у терміналі: ollama pull llama3"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting Ollama models: {ex}");
            }
        }
        
        private void LoadSettings()
        {
            // Використання покращеного механізму пошуку і створення налаштувань
            _settings = AIAgentSettingsCreator.GetSettings(true);

            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
                _apiKeyInput = _settings.openAIApiKey;
                Debug.Log($"AIAgentUnity: Налаштування успішно завантажені. API ключ {(_settings.openAIApiKey.Length > 0 ? "встановлено" : "не встановлено")}.");
            }
            else
            {
                Debug.LogError("AIAgentUnity: Не вдалося завантажити або створити налаштування.");
            }
        }

        // Метод DefinePanelRects видалено, оскільки ми перейшли до спрощеного інтерфейсу


        private void OnGUI()
        {
            try
            {
                if (_settings == null)
                {
                    EditorGUILayout.HelpBox("AIAgentSettings not found. Please create one via Assets > Create > AI Agent > Settings and then reopen this window.", MessageType.Error);
                    if (GUILayout.Button("Attempt to Reload Settings"))
                    {
                        LoadSettings();
                        if (_settings != null) OnEnable(); // Re-initialize if settings are found
                    }
                    return;
                }
                
                // Показуємо інформаційну панель з командами
                // ShowApiStatusPanel(); // ВИДАЛЕНО: більше не відкриваємо вікно статусу API автоматично
                
                // Використовуємо максимально спрощений чат-інтерфейс без додаткових кнопок
                SimplifiedChatUI.DrawSimplifiedChatInterface(
                    position,
                    ref _userInput,
                    ref _chatScrollPosition,
                    ref _chatHistory,
                    ref _isWaitingForAIResponse,
                    _isTextToSpeechEnabled,
                    SendMessageToAI,
                    ClearChatHistory,
                    OpenSettingsWindow,
                    (content) => SaveGenerationToFile(content, "AIGeneration", "txt"), // <-- виправлено: додано аргументи
                    _showAPITroubleshooterButton
                );
            }
            catch (Exception ex)
            {
                // Відловлюємо будь-які помилки інтерфейсу, щоб запобігти повному виходу з ладу редактора
                GUILayout.BeginVertical();
                EditorGUILayout.HelpBox($"Помилка відображення інтерфейсу: {ex.Message}\n\nСпробуйте перезапустити вікно редактора.", MessageType.Error);
                
                if (GUILayout.Button("Очистити інтерфейс і перезавантажити"))
                {
                    _chatHistory = new List<ChatMessage>();
                    _chatScrollPosition = Vector2.zero;
                    _userInput = "";
                    _isWaitingForAIResponse = false;
                    OnEnable(); // Перезавантажуємо стан вікна
                }
                GUILayout.EndVertical();
                
                // Записуємо детальну інформацію про помилку у лог
                Debug.LogError($"Критична помилка інтерфейсу AIAgentUnity: {ex.Message}\n{ex.StackTrace}");
            }
            
            ProcessEvents(Event.current);
            
            if (GUI.changed) Repaint(); // Repaint if any GUI element changed
        }

        // [Методи видалені, оскільки вони не використовуються в спрощеному інтерфейсі]
        
        private void ProcessEvents(Event e)
        {
            // У спрощеній версії обробляємо тільки натискання клавіш
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "UserInputTextField")
            {
                if (!string.IsNullOrEmpty(_userInput) && !_isWaitingForAIResponse)
                {
                    SendMessageToAI(_userInput);
                    _userInput = ""; // Clear input field
                    e.Use(); // Consume the event to prevent other controls from using it
                }
            }
        }


        // [Методи видалені, оскільки вони не використовуються в спрощеному інтерфейсі]
        
        // Метод для очищення історії чату
        private void ClearChat()
        {
            ClearChatHistory(); // Використовуємо публічний метод ClearChatHistory
        }
        
        private async void SendMessageToAI(string message, bool isRetry = false)
        {
            if (_currentService == null)
            {
                 _chatHistory.Add(new ChatMessage("System", "AI Service not selected or available. Please check configuration."));
                Repaint();
                return;
            }
            if (!_currentService.IsConfigured())
            {
                _chatHistory.Add(new ChatMessage("System", $"{_currentService.GetServiceName()} not configured. Please check API key in settings."));
                Repaint();
                return;
            }

            if (!isRetry)
            {
                _chatHistory.Add(new ChatMessage("User", message));
            }
            _isWaitingForAIResponse = true;
            _progressText = $"Querying {_currentService.GetServiceName()}...";
            Repaint(); // Update UI to show user message and disable input

            // Prepare history for AI
            var historyForAI = _chatHistory.Select(m => $"{m.Sender}: {m.Text}").ToList();

            AIResponse response = await _currentService.QueryAI(message, historyForAI);
            _isWaitingForAIResponse = false;

            if (response.IsSuccess)
            {
                string formattedContent = response.GetFormattedResponse();
                _chatHistory.Add(new ChatMessage("AI", formattedContent));
                ParseAndExecuteUnityCommand(response.Content); // Використовуємо оригінальний контент для команд
                _scriptPreview = ExtractCodeFromMarkdown(response.Content);
                
                // Додаємо діагностичну інформацію у статусний рядок
                string statusInfo = "AI відповідь отримана";
                if (response.IsDemoMode)
                {
                    statusInfo += " (демо-режим)";
                }
                else if (response.IsCached)
                {
                    statusInfo += " (з кешу)";
                }
                
                if (response.ResponseTimeMs > 0)
                {
                    statusInfo += $" за {response.ResponseTimeMs} мс";
                }
                
                _progressText = statusInfo;
                
                // Text-to-Speech if enabled
                if (_isTextToSpeechEnabled)
                {
                    ConvertTextToSpeech(response.Content);
                }
            }
            else
            {
                // Додаємо інструкцію щодо усунення проблем з API ключем
                string errorText = $"Помилка: {response.ErrorMessage}";
                
                // Якщо помилка стосується API ключа Gemini, додаємо рекомендацію
                bool isGeminiApiKeyError = false;
                if (_settings != null && _settings.useGeminiAPI)
                {
                    if (response.ErrorMessage.Contains("API key") || 
                        response.ErrorMessage.Contains("API_KEY") ||
                        response.ErrorMessage.Contains("ключ") ||
                        response.ErrorMessage.Contains("quota") ||
                        response.ErrorMessage.Contains("доступ"))
                    {
                        isGeminiApiKeyError = true;
                        errorText += "\n\nСхоже, виникла проблема з API ключем Gemini. " +
                            "Ви можете використати інструмент усунення проблем, щоб діагностувати проблему:\n" +
                            "Window > AI Assistant > Troubleshoot API";
                        
                        // Якщо це інтерфейс у вікні редактора Unity, додаємо кнопку
                        _showAPITroubleshooterButton = true;
                    }
                }
                
                _chatHistory.Add(new ChatMessage("AI", errorText));
                _progressText = $"Помилка від AI: {response.ErrorMessage}";
                
                // Логуємо помилки для діагностики
                if (isGeminiApiKeyError)
                {
                    Debug.LogWarning($"Помилка API Gemini: {response.ErrorMessage} - рекомендовано використати інструмент усунення проблем");
                }
            }
            
            // Save history after new messages
            if(_historyService != null) _historyService.SaveHistory(_chatHistory.Select(msg => $"{msg.Sender}: {msg.Text}").ToList());

            _chatScrollPosition.y = float.MaxValue; // Scroll to bottom
            Repaint(); // Update UI with AI response
        }
        
        // Метод для перетворення тексту на голос
        private async void ConvertTextToSpeech(string text)
        {
            if (_audioManager == null)
            {
                _audioManager = AudioManager.Instance;
            }
            
            _chatHistory.Add(new ChatMessage("System", "Запуск синтезу мовлення..."));
            
            // Використовуємо AudioManager для перетворення тексту на голос
            bool success = await _audioManager.TextToSpeech(text);
            
            if (success)
            {
                _chatHistory.Add(new ChatMessage("System", "✓ Текст успішно озвучено"));
                _audioManager.PlayNotificationSound(AudioType.Success);
            }
            else
            {
                _chatHistory.Add(new ChatMessage("System", "✗ Не вдалося озвучити текст"));
                _audioManager.PlayNotificationSound(AudioType.Error);
            }
            
            Repaint();
        }
        
        // Метод для перетворення голосу на текст
        public async void ConvertSpeechToText()
        {
            if (_isSpeechToTextActive)
            {
                if (_audioManager == null)
                {
                    _audioManager = AudioManager.Instance;
                }
                
                // Встановлюємо час початку запису
                _recordingStartTime = (float)EditorApplication.timeSinceStartup;
                
                _chatHistory.Add(new ChatMessage("System", "🎙️ Запис голосу... Говоріть чітко."));
                _audioManager.PlayNotificationSound(AudioType.Recording);
                
                // Показуємо індикатор прогресу
                int progressPercentage = 0;
                
                // Створюємо задачу для відстеження прогресу запису
                System.Threading.Tasks.Task progressTask = System.Threading.Tasks.Task.Run(async () => {
                    while (_isSpeechToTextActive && progressPercentage < 100)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                        
                        // Обчислюємо прогрес на основі часу
                        float elapsedTime = (float)EditorApplication.timeSinceStartup - _recordingStartTime;
                        progressPercentage = (int)((elapsedTime / MAX_RECORDING_TIME_SEC) * 100);
                        if (progressPercentage >= 100) 
                        {
                            progressPercentage = 100;
                            // Автоматично зупиняємо запис, якщо перевищено максимальний час
                            if (_isSpeechToTextActive)
                            {
                                Debug.Log($"[STT] Автоматичне завершення запису через {MAX_RECORDING_TIME_SEC} секунд");
                                _isSpeechToTextActive = false; // Зупиняємо запис
                            }
                        }
                        
                        // Оновлюємо повідомлення з прогресом
                        if (_chatHistory.Count > 0)
                        {
                            int remainingSec = MAX_RECORDING_TIME_SEC - (int)elapsedTime;
                            if (remainingSec < 0) remainingSec = 0;
                            
                            // Візуальний індикатор прогресу
                            string progressBar = "";
                            for (int i = 0; i < 20; i++) {
                                if (i < progressPercentage / 5) {
                                    progressBar += "█";
                                } else {
                                    progressBar += "░";
                                }
                            }
                            
                            _chatHistory[_chatHistory.Count - 1] = new ChatMessage("System", 
                                $"🎙️ Запис голосу... Говоріть чітко. \n{progressBar} {progressPercentage}% (залишилось {remainingSec} сек)");
                            Repaint();
                        }
                    }
                });
                
                // Додаємо звуковий сигнал про закінчення запису
                _audioManager.PlayNotificationSound(AudioType.Notification);
                
                // Додаємо системне повідомлення про обробку результатів
                _chatHistory.Add(new ChatMessage("System", "🔍 Обробка записаного звуку..."));
                Repaint();
                
                // Використовуємо AudioManager для розпізнавання мовлення
                string recognizedText = await _audioManager.SpeechToText();
                
                if (!string.IsNullOrEmpty(recognizedText))
                {
                    _userInput = recognizedText;
                    
                    // Визначаємо стан якості розпізнавання
                    string qualityIndicator = "";
                    if (recognizedText.Contains("[недостатньо чітко]"))
                    {
                        qualityIndicator = "⚠️ (низька точність) ";
                        _userInput = recognizedText.Replace("[недостатньо чітко]", "").Trim();
                    }
                    else
                    {
                        qualityIndicator = "✅ (висока точність) ";
                    }
                    
                    _chatHistory.Add(new ChatMessage("System", $"{qualityIndicator}Розпізнано: {_userInput}"));
                    
                    // Звукове повідомлення залежить від якості розпізнавання
                    if (recognizedText.Contains("[недостатньо чітко]"))
                    {
                        _audioManager.PlayNotificationSound(AudioType.Warning);
                    }
                    else
                    {
                        _audioManager.PlayNotificationSound(AudioType.Success);
                    }
                    
                    // Автоматичний фокус на поле введення тексту
                    EditorGUI.FocusTextInControl("UserInputTextField");
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", "❌ Не вдалося розпізнати текст. Спробуйте говорити чіткіше або перевірте мікрофон."));
                    _audioManager.PlayNotificationSound(AudioType.Error);
                }
                
                _isSpeechToTextActive = false;
                Repaint();
            }
        }

        // Метод для генерації гри на основі обраних параметрів
        private void GenerateGame()
        {
            if (_currentService == null || !_currentService.IsConfigured())
            {
                _chatHistory.Add(new ChatMessage("System", "AI сервіс не налаштовано. Перевірте налаштування."));
                return;
            }

            // Формуємо запит до AI на основі обраних параметрів гри
            string prompt = $"Створи базову структуру для Unity гри з наступними параметрами:\n" +
                $"- Тип гри: {_selectedGameType}\n" +
                $"- Кількість гравців: {_selectedPlayerCount}\n" +
                $"- Стиль: {_selectedArtStyle}\n" +
                $"- Розмір карти: {_selectedMapSize}\n" +
                $"- Мета гри: {_gameGoal}\n\n" +
                "Потрібно: основний структурний опис гри, головні скрипти для контролера гравця, ігрової логіки, " +
                "менеджера рівнів та короткі інструкції щодо створення головних елементів сцени. " +
                "Усі скрипти повинні бути на С# з повними коментарями.";

            // Додаємо повідомлення до історії чату та надсилаємо запит до AI
            _chatHistory.Add(new ChatMessage("System", "Починаємо генерацію гри на основі обраних параметрів..."));
            SendMessageToAI(prompt);
        }
        
        /// <summary>
        /// Генерує базову сцену за обраними параметрами
        /// </summary>
        public void GenerateBasicScene()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", $"Генерую базову {_selectedGameType} сцену в стилі {_selectedArtStyle}..."));
                
                // Створюємо нову сцену з базовими об'єктами
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                // Створюємо землю
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.localScale = new Vector3(10, 1, 10);
                ground.transform.position = Vector3.zero;
                ground.name = "Ground";
                
                // Створюємо гравця
                GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.transform.position = new Vector3(0, 2, 0);
                player.name = "Player";

                // Налаштовуємо камеру
                Camera cam = Camera.main;
                if (cam == null)
                {
                    GameObject camObj = new GameObject("Main Camera");
                    cam = camObj.AddComponent<Camera>();
                    cam.tag = "MainCamera";
                }
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.LookAt(player.transform);

                // Створюємо скрипт руху гравця
                string scriptContent = @"using UnityEngine;
public class PlayerMovement : MonoBehaviour {
    public float speed = 5f;
    public float jumpForce = 5f;
    private bool isGrounded;
    private Rigidbody rb;
    
    void Start() {
        rb = GetComponent<Rigidbody>();
        if (rb == null) {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    
    void Update() {
        float h = Input.GetAxis(""Horizontal"");
        float v = Input.GetAxis(""Vertical"");
        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
        
        // Перевірка приземлення
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        
        // Стрибок
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}";

                // Створюємо папки для скриптів
                string folderPath = "Assets/Scripts";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    string parentFolder = System.IO.Path.GetDirectoryName(folderPath);
                    string newFolderName = System.IO.Path.GetFileName(folderPath);
                    AssetDatabase.CreateFolder(parentFolder, newFolderName);
                }
                
                string scriptPath = $"{folderPath}/PlayerMovement.cs";
                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.ImportAsset(scriptPath);
                AssetDatabase.Refresh();

                // Створюємо папки для сцен
                string scenesFolderPath = "Assets/Scenes";
                if (!AssetDatabase.IsValidFolder(scenesFolderPath))
                {
                    string parentFolder2 = System.IO.Path.GetDirectoryName(scenesFolderPath);
                    string newFolderName2 = System.IO.Path.GetFileName(scenesFolderPath);
                    AssetDatabase.CreateFolder(parentFolder2, newFolderName2);
                }
                
                // Додаємо компонент до гравця після імпорту скрипту
                EditorApplication.delayCall += () => {
                    var playerMovementType = System.Type.GetType("PlayerMovement, Assembly-CSharp");
                    if (playerMovementType != null) {
                        player.AddComponent(playerMovementType);
                        
                        // Додаємо Rigidbody, якщо його ще немає
                        if (player.GetComponent<Rigidbody>() == null)
                        {
                            Rigidbody rb = player.AddComponent<Rigidbody>();
                            rb.constraints = RigidbodyConstraints.FreezeRotation;
                        }
                    } else {
                        Debug.LogError("Не вдалося знайти тип PlayerMovement");
                    }
                };
                
                // Зберігаємо сцену
                string sceneName = $"{_selectedGameType}_{_selectedArtStyle}";
                if (!string.IsNullOrEmpty(_gameGoal))
                {
                    sceneName += $"_{System.IO.Path.GetFileNameWithoutExtension(_gameGoal.Replace(" ", "_"))}";
                }
                string sanitizedSceneName = string.Join("_", sceneName.Split(System.IO.Path.GetInvalidFileNameChars()));
                string scenePath = $"{scenesFolderPath}/{sanitizedSceneName}.unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
                
                _chatHistory.Add(new ChatMessage("System", $"✅ Сцену успішно створено за адресою {scenePath}"));
                _chatHistory.Add(new ChatMessage("System", $"✅ Для пошуку відповідних ресурсів у Asset Store, використайте кнопку 'Знайти ассети за типом гри'"));

                if (_audioManager != null)
                {
                    _audioManager.PlayNotificationSound(AudioType.Success);
                }
                
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Помилка генерації сцени: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка генерації сцени: {ex.Message}"));
                
                if (_audioManager != null)
                {
                    _audioManager.PlayNotificationSound(AudioType.Error);
                }
                
                Repaint();
            }
        }
        
        /// <summary>
        /// Відкриває Asset Store з пошуком за типом гри
        /// </summary>
        public void SearchAssetsForGameType()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", $"Відкриваю Asset Store для пошуку ресурсів за типом: {_selectedGameType} та стилем: {_selectedArtStyle}"));
                
                // Формуємо пошуковий запит на основі типу гри та стилю
                string searchQuery = $"{_selectedGameType.ToString()} {_selectedArtStyle.ToString()}";
                
                // Додаємо релевантні ключові слова залежно від типу гри
                switch (_selectedGameType)
                {
                    case GameType.FPS:
                        searchQuery += " shooter fps weapons";
                        break;
                    case GameType.TPS:
                        searchQuery += " third-person shooter character controller";
                        break;
                    case GameType.Platformer:
                        searchQuery += " platform 2d character controller";
                        break;
                    case GameType.RPG:
                        searchQuery += " role-playing inventory character";
                        break;
                    case GameType.Strategy:
                        searchQuery += " rts strategy units";
                        break;
                    case GameType.Puzzle:
                        searchQuery += " puzzle mechanics";
                        break;
                    case GameType.Racing:
                        searchQuery += " cars vehicles";
                        break;
                }
                
                // Формуємо URL для пошуку в Asset Store
                string assetStoreSearchUrl = $"https://assetstore.unity.com/search?q={UnityWebRequest.EscapeURL(searchQuery)}";
                Application.OpenURL(assetStoreSearchUrl);
                
                _chatHistory.Add(new ChatMessage("System", $"🔍 Пошук ресурсів за запитом: {searchQuery}"));
                
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Помилка відкриття Asset Store: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка відкриття Asset Store: {ex.Message}"));
                Repaint();
            }
        }
        
        /// <summary>
        /// Відкриває вікно налаштувань
        /// </summary>
        public void OpenSettingsWindow()
        {
            try
            {
                AIAgentSettingsWindow.ShowWindow();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка відкриття вікна налаштувань: {ex.Message}\n{ex.StackTrace}");
                
                // Альтернативний шлях для відображення налаштувань
                _chatHistory.Add(new ChatMessage("System", "Не вдалося відкрити вікно налаштувань. Спробуйте через меню Window > AI Assistant > Settings"));
                
                // Намагаємося відкрити вікно через використання меню
                EditorApplication.ExecuteMenuItem("Window/AI Assistant/Settings");
            }
        }
        
        /// <summary>
        /// Призупиняє поточну генерацію для отримання уточнень від користувача
        /// </summary>
    public void PauseForClarification()
    {
        if (_isWaitingForAIResponse)
        {
            _isWaitingForAIResponse = false;
            _chatHistory.Add(new ChatMessage("System", "Генерацію призупинено. Ви можете додати уточнення до запиту."));
            
            // Відтворюємо звук сповіщення
            if (_audioManager != null)
            {
                _audioManager.PlayNotificationSound(AudioType.Info);
            }
            
            Repaint();
        }
        else
        {
            _chatHistory.Add(new ChatMessage("System", "Немає активної генерації для призупинення."));
            Repaint();
        }
    }
        
        /// <summary>
        /// Очищає історію чату
        /// </summary>
        public void ClearChatHistory()
        {
            // Створюємо нову історію замість того, щоб очищувати існуючу
            // Це допомагає запобігти NullReferenceException
            _chatHistory = new List<ChatMessage>();
            _chatHistory.Add(new ChatMessage("System", "Історію чату очищено"));
            
            // Ініціалізуємо historyService, якщо потрібно
            if (_historyService == null && _settings != null)
            {
                string historyPath = System.IO.Path.Combine(Application.persistentDataPath, ChatHistoryFileName);
                _historyService = new ChatHistoryService(historyPath, _settings.maxHistoryLength);
            }
            
            // Зберігаємо пусту історію
            if (_historyService != null)
            {
                _historyService.SaveHistory(new List<string>());
            }
            
            Repaint();
        }
        
        /// <summary>
        /// Витягує код з markdown відповіді AI з підтримкою різних мов програмування
        /// </summary>
        private string ExtractCodeFromMarkdown(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
            {
                return "// Порожній текст відповіді";
            }
            
            // Шукаємо блоки коду в форматі markdown (обгорнуті ```)
            // Підтримуємо кілька мов програмування: C#, JavaScript, JSON, та код без вказаної мови
            MatchCollection matches = Regex.Matches(
                markdownText, 
                @"```(?:csharp|cs|c#|json|js|javascript)?\s*\n([\s\S]*?)\n```"
            );
            
            StringBuilder result = new StringBuilder();
            int codeBlockCount = 0;
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1 && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    codeBlockCount++;
                    if (codeBlockCount > 1)
                    {
                        result.AppendLine("\n// ---------- Блок коду " + codeBlockCount + " ----------");
                    }
                    
                    string code = match.Groups[1].Value.Trim();
                    result.AppendLine(code);
                    result.AppendLine();
                }
            }
            
            if (result.Length > 0)
            {
                return result.ToString();
            }
            else
            {
                // Спробуємо знайти код навіть без ```
                Match inlineCode = Regex.Match(markdownText, @"public\s+(class|void|static|async)\s+\w+");
                if (inlineCode.Success)
                {
                    return "// Знайдено код без markdown розмітки:\n" + markdownText.Substring(inlineCode.Index);
                }
                
                return "// Код не знайдено в відповіді";
            }
        }
        
        /// <summary>
        /// Парсить відповідь AI для виконання команд Unity
        /// </summary>
        private void ParseAndExecuteUnityCommand(string aiMessage)
        {
            if (string.IsNullOrEmpty(aiMessage))
            {
                return;
            }
            
            // Перевіряємо наявність команд для створення об'єктів: #create_object:(\w+)
            var createObjectMatches = Regex.Matches(aiMessage, @"#create_object:(\w+)");
            foreach (Match match in createObjectMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string prefabName = match.Groups[1].Value;
                    GameObject obj = null;
                    
                    if (prefabName.ToLower() == "cube") obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    else if (prefabName.ToLower() == "sphere") obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    else if (prefabName.ToLower() == "capsule") obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    else if (prefabName.ToLower() == "plane") obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    else if (prefabName.ToLower() == "cylinder") obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    else if (prefabName.ToLower() == "quad") obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    
                    if (obj != null)
                    {
                        obj.name = prefabName;
                        Selection.activeGameObject = obj;
                        _chatHistory.Add(new ChatMessage("System", $"✓ Створено об'єкт {prefabName} на сцені"));
                    }
                }
            }
            
            // Перевіряємо наявність команд для створення скриптів: #create_script:FileName.cs
            var createScriptMatches = Regex.Matches(aiMessage, @"#create_script:(\w+\.cs)");
            foreach (Match match in createScriptMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string scriptName = match.Groups[1].Value;
                    string scriptContent = ExtractCodeFromMarkdown(aiMessage);
                    
                    if (!string.IsNullOrEmpty(scriptContent) && scriptContent != "// Код не знайдено в відповіді")
                    {
                        string folderPath = "Assets/Scripts";
                        if (!AssetDatabase.IsValidFolder(folderPath))
                        {
                            string parentFolder3 = System.IO.Path.GetDirectoryName(folderPath);
                            string newFolderName3 = System.IO.Path.GetFileName(folderPath);
                            AssetDatabase.CreateFolder(parentFolder3, newFolderName3);
                        }
                        
                        string scriptPath = $"{folderPath}/{scriptName}";
                        File.WriteAllText(scriptPath, scriptContent);
                        AssetDatabase.ImportAsset(scriptPath);
                        AssetDatabase.Refresh();
                        
                        _chatHistory.Add(new ChatMessage("System", $"✓ Створено скрипт {scriptName} в папці {folderPath}"));
                    }
                    else
                    {
                        _chatHistory.Add(new ChatMessage("System", $"⚠️ Не знайдено код для скрипту {scriptName}"));
                    }
                }
            }
            
            // Команда для створення папки: #create_folder:FolderName
            var createFolderMatches = Regex.Matches(aiMessage, @"#create_folder:(\w+)");
            foreach (Match match in createFolderMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string folderName = match.Groups[1].Value;
                    string folderPath = $"Assets/{folderName}";
                    
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        AssetDatabase.CreateFolder("Assets", folderName);
                        AssetDatabase.Refresh();
                        _chatHistory.Add(new ChatMessage("System", $"✓ Створено папку {folderName}"));
                    }
                    else
                    {
                        _chatHistory.Add(new ChatMessage("System", $"⚠️ Папка {folderName} вже існує"));
                    }
                }
            }
            
            // Команда для пошуку ресурсів: #find_assets:Asset Type
            var findAssetsMatches = Regex.Matches(aiMessage, @"#find_assets:(.+)");
            foreach (Match match in findAssetsMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string assetType = match.Groups[1].Value.Trim();
                    SearchAssetsForGameType(); // Викликаємо функцію пошуку ресурсів
                    _chatHistory.Add(new ChatMessage("System", $"🔍 Виконую пошук ресурсів для: {assetType}"));
                }
            }
            
            // Команда для генерації сцени: #generate_scene
            if (aiMessage.Contains("#generate_scene"))
            {
                GenerateBasicScene();
            }
            
            // Команда для генерації героя: #generate_hero
            if (aiMessage.Contains("#generate_hero"))
            {
                CreateHeroCharacter();
                _chatHistory.Add(new ChatMessage("System", "✓ Створено базового персонажа з контролером руху"));
            }
            
            // Команда для генерації поля бою: #generate_battlefield
            if (aiMessage.Contains("#generate_battlefield"))
            {
                CreateBattlefield();
                _chatHistory.Add(new ChatMessage("System", "✓ Створено поле бою з перешкодами"));
            }
            
            // Команда для з'єднання скриптів: #connect_scripts
            if (aiMessage.Contains("#connect_scripts"))
            {
                ConnectAllScripts();
                _chatHistory.Add(new ChatMessage("System", "✓ З'єднано усі скрипти з відповідними об'єктами"));
            }
            
            // Команда для виправлення помилок: #fix_errors
            if (aiMessage.Contains("#fix_errors"))
            {
                FixScriptErrors();
                _chatHistory.Add(new ChatMessage("System", "✓ Запущено процес виправлення помилок у скриптах"));
            }
            
            // Команда для паузи та уточнення запиту: #pause або #clarify
            if (aiMessage.Contains("#pause") || aiMessage.Contains("#clarify"))
            {
                PauseForClarification();
                _chatHistory.Add(new ChatMessage("System", "⏸️ Генерація призупинена для уточнення запиту"));
            }
            
            // Команда для очищення історії чату: #clear_chat
            if (aiMessage.Contains("#clear_chat"))
            {
                ClearChatHistory();
                _chatHistory.Add(new ChatMessage("System", "🗑️ Історію чату очищено"));
            }
            
            // Команда для відкриття налаштувань: #settings
            if (aiMessage.Contains("#settings"))
            {
                OpenSettingsWindow();
                _chatHistory.Add(new ChatMessage("System", "⚙️ Відкрито вікно налаштувань"));
            }
            
            // Команда для отримання списку безкоштовних ресурсів Unity: #free_assets або #free_assets:Category
            if (aiMessage.Contains("#free_assets"))
            {
                var freeAssetsMatch = Regex.Match(aiMessage, @"#free_assets:(\w+)");
                if (freeAssetsMatch.Success && freeAssetsMatch.Groups.Count > 1)
                {
                    string category = freeAssetsMatch.Groups[1].Value;
                    ShowFreeUnityAssetsByCategory(category);
                }
                else if (aiMessage.Contains("#free_assets"))
                {
                    ShowFreeUnityAssets();
                }
            }
            
            // Команда для додавання нового ресурсу до списку безкоштовних ресурсів Unity:
            // #add_resource:Category:Name:Description:URL
            var addResourceMatch = Regex.Match(aiMessage, @"#add_resource:([^:]+):([^:]+):([^:]+):([^:\s]+)");
            if (addResourceMatch.Success && addResourceMatch.Groups.Count > 4)
            {
                string category = addResourceMatch.Groups[1].Value.Trim();
                string name = addResourceMatch.Groups[2].Value.Trim();
                string description = addResourceMatch.Groups[3].Value.Trim();
                string url = addResourceMatch.Groups[4].Value.Trim();
                
                AddResourceToFreeAssetsList(category, name, description, url);
            }
            
            // Команда для додавання нового ресурсу до списку безкоштовних ресурсів: #add_free_asset
            if (aiMessage.Contains("#add_free_asset"))
            {
                // Приклад команди: #add_free_asset:FPS, My Cool Asset, Це мій класний актив, https://example.com/my-cool-asset
                var addAssetMatch = Regex.Match(aiMessage, @"#add_free_asset:([^,]+),\s*([^,]+),\s*([^,]+),\s*(.+)");
                if (addAssetMatch.Success && addAssetMatch.Groups.Count > 4)
                {
                    string category = addAssetMatch.Groups[1].Value.Trim();
                    string name = addAssetMatch.Groups[2].Value.Trim();
                    string description = addAssetMatch.Groups[3].Value.Trim();
                    string url = addAssetMatch.Groups[4].Value.Trim();
                    
                    AddResourceToFreeAssetsList(category, name, description, url);
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", "⚠️ Невірний формат команди. Використовуйте: #add_free_asset:Категорія, Назва, Опис, URL"));
                }
            }
            
            // Команда для отримання списку команд: #help
            if (aiMessage.Contains("#help"))
            {
                string commandsHelp = 
                    "📌 <b>Доступні команди:</b>\n\n" +
                    "• #generate_hero - створити персонажа з контролером руху\n" +
                    "• #generate_battlefield - створити поле бою з перешкодами\n" +
                    "• #generate_scene - створити базову сцену з персонажем та світлом\n" +
                    "• #connect_scripts - автоматично підключити скрипти до відповідних об'єктів\n" +
                    "• #fix_errors - виправити типові помилки у скриптах проекту\n" +
                    "• #create_object:Cube - створити примітив (Cube, Sphere, Cylinder та інші)\n" +
                    "• #create_script:FileName.cs - створити новий скрипт з коду у повідомленні\n" +
                    "• #create_folder:Name - створити папку в Assets\n" +
                    "• #find_assets:Query - знайти відповідні ресурси в Asset Store\n" +
                    "• #free_assets - показати список безкоштовних ресурсів для різних типів ігор\n" +
                    "• #free_assets:Category - показати ресурси для конкретної категорії (FPS, Platformer, Racing, тощо)\n" +
                    "• #add_resource:Category:Name:Description:URL - додати новий ресурс до списку безкоштовних ресурсів\n" +
                    "• #pause або #clarify - призупинати генерацію для уточнення\n" +
                    "• #clear_chat - очистити історію чату\n" +
                    "• #settings - відкрити налаштування API ключів\n" +
                    "• #help - показати цей список команд";
                
                _chatHistory.Add(new ChatMessage("System", commandsHelp));
            }
            
            // Команда для показу безкоштовних ресурсів Unity: #free_assets
            if (aiMessage.Contains("#free_assets"))
            {
                // Отримуємо категорію з команди, якщо є
                string category = aiMessage.Replace("#free_assets:", "").Trim();
                
                if (string.IsNullOrEmpty(category))
                {
                    // Якщо категорія не вказана, показуємо всі доступні категорії
                    ShowFreeUnityAssets();
                }
                else
                {
                    // Показуємо ресурси тільки для вказаної категорії
                    ShowFreeUnityAssetsByCategory(category);
                }
            }
        }
        
        /// <summary>
        /// Показує інформацію про безкоштовні ресурси Unity для розробки ігор
        /// </summary>
        private void ShowFreeUnityAssets()
        {
            try
            {
                // Шукаємо файл з безкоштовними ресурсами
                string filePath = null;
                string[] assetPaths = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(FreeUnityAssetsFileName));
                
                foreach (string guid in assetPaths)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(FreeUnityAssetsFileName))
                    {
                        filePath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    _chatHistory.Add(new ChatMessage("System", "❌ Не знайдено файл зі списком безкоштовних ресурсів Unity"));
                    return;
                }
                
                // Читаємо файл
                string content = File.ReadAllText(filePath);
                
                // Витягуємо перелік категорій та закладок з файлу
                var categoryMatches = Regex.Matches(content, @"##\s+([^\n]+)");
                var categories = new List<string>();
                
                foreach (Match match in categoryMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        categories.Add(match.Groups[1].Value.Trim());
                    }
                }
                
                // Формуємо коротке повідомлення зі списком категорій
                string message = "📚 <b>Безкоштовні ресурси для Unity</b>\n\n" +
                    "Доступні категорії ресурсів:\n";
                
                foreach (string category in categories)
                {
                    message += $"• {category}\n";
                }
                
                message += "\nДля отримання детальної інформації по конкретній категорії, введіть:" +
                    "\n#free_assets:назва_категорії\n" +
                    "\nПриклад: #free_assets:FPS або #free_assets:3D\n" +
                    "\nПовний перелік ресурсів знаходиться у файлі: " + FreeUnityAssetsFileName;
                
                _chatHistory.Add(new ChatMessage("System", message));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка показу безкоштовних ресурсів: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка при отриманні списку ресурсів: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Показує інформацію про безкоштовні ресурси Unity для конкретної категорії
        /// </summary>
        private void ShowFreeUnityAssetsByCategory(string category)
        {
            try
            {
                // Шукаємо файл з безкоштовними ресурсами
                string filePath = null;
                string[] assetPaths = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(FreeUnityAssetsFileName));
                
                foreach (string guid in assetPaths)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(FreeUnityAssetsFileName))
                    {
                        filePath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    _chatHistory.Add(new ChatMessage("System", "❌ Не знайдено файл зі списком безкоштовних ресурсів Unity"));
                    return;
                }
                
                // Читаємо файл
                string content = File.ReadAllText(filePath);
                
                // Шукаємо розділ за категорією
                string categoryPattern = $@"##\s+[^#\n]*{Regex.Escape(category)}[^#\n]*\n(.*?)(?=##|\Z)";
                Match categoryMatch = Regex.Match(content, categoryPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                
                if (categoryMatch.Success && categoryMatch.Groups.Count > 1)
                {
                    string categoryContent = categoryMatch.Groups[1].Value.Trim();
                    
                    // Додаємо заголовок категорії
                    string header = content.Substring(categoryMatch.Index, categoryMatch.Groups[1].Index - categoryMatch.Index).Trim();
                    
                    string message = $"📚 <b>{header}</b>\n\n{categoryContent}";
                    
                    // Додаємо повідомлення з інформацією про ресурси категорії
                    _chatHistory.Add(new ChatMessage("System", message));
                }
                else
                {
                    // Шукаємо схожі категорії
                    var categoryMatches = Regex.Matches(content, @"##\s+([^\n]+)");
                    var similarCategories = new List<string>();
                    
                    foreach (Match match in categoryMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string cat = match.Groups[1].Value.Trim();
                            if (cat.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (cat.Length > 3 && category.IndexOf(cat, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                similarCategories.Add(cat);
                            }
                        }
                    }
                    
                    if (similarCategories.Count > 0)
                    {
                        string message = $"⚠️ Категорія '{category}' не знайдена. Можливо, ви шукали одну з цих категорій:\n\n";
                        
                        foreach (string cat in similarCategories)
                        {
                            message += $"• {cat}\n";
                        }
                        
                        message += "\nВведіть #free_assets:назва_категорії для отримання інформації по конкретній категорії.";
                        
                        _chatHistory.Add(new ChatMessage("System", message));
                    }
                    else
                    {
                        _chatHistory.Add(new ChatMessage("System", $"⚠️ Категорія '{category}' не знайдена. Введіть #free_assets для перегляду всіх доступних категорій."));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка показу безкоштовних ресурсів для категорії {category}: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка при отриманні списку ресурсів: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Додає новий ресурс до списку безкоштовних ресурсів Unity
        /// </summary>
        private void AddResourceToFreeAssetsList(string category, string name, string description, string url)
        {
            try
            {
                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name) || 
                    string.IsNullOrEmpty(url))
                {
                    _chatHistory.Add(new ChatMessage("System", "⚠️ Для додавання ресурсу необхідно вказати категорію, назву та URL"));
                    return;
                }
                
                bool result = DocumentationUpdater.AddResourceToFreeAssetsList(
                    category, name, description ?? "Безкоштовний ресурс для Unity", url);
                
                if (result)
                {
                    _chatHistory.Add(new ChatMessage("System", $"✅ Ресурс '{name}' успішно додано до категорії '{category}'"));
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", 
                        $"⚠️ Не вдалося додати ресурс '{name}' до категорії '{category}'. " +
                        "Перевірте, чи правильно вказані параметри та чи існує така категорія."));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при додаванні ресурсу до списку безкоштовних ресурсів: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Додає системне повідомлення до історії чату
        /// </summary>
        /// <param name="message">Текст системного повідомлення</param>
        public void AddSystemMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
                
            _chatHistory.Add(new ChatMessage("System", message));
            
            // Якщо є сервіс історії чату, зберігаємо повідомлення
            _historyService?.SaveHistory(_chatHistory.Select(m => $"{m.Sender}: {m.Text}").ToList());
            
            Repaint();
        }
        
        /// <summary>
        /// Перевіряє наявність необхідних пакетів для роботи AI Agent
        /// </summary>
        private void CheckRequiredPackages()
        {
            try
            {
                _progressText = "Перевірка необхідних пакетів...";
                
                // Перевіряємо наявність Newtonsoft.Json
                bool hasNewtonsoftJson = false;
                
                try
                {
                    // Перевіряємо чи можемо створити об'єкт JsonSerializerSettings
                    var settings = new JsonSerializerSettings();
                    hasNewtonsoftJson = true;
                }
                catch (Exception)
                {
                    hasNewtonsoftJson = false;
                }
                
                if (!hasNewtonsoftJson)
                {
                    _chatHistory.Add(new ChatMessage("System", 
                        "⚠️ Необхідний пакет Newtonsoft.Json не знайдено. Для коректної роботи AI Agent, додайте пакет через Package Manager: " +
                        "\nWindow > Package Manager > + > Add package by name > com.unity.nuget.newtonsoft-json"));
                    
                    // Показуємо спливаюче повідомлення
                    EditorUtility.DisplayDialog("Відсутній необхідний пакет", 
                        "Необхідний пакет Newtonsoft.Json не знайдено. Для коректної роботи AI Agent, додайте пакет через Package Manager.", "ОК");
                }
                
                _progressText = "Перевірка пакетів завершена";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка перевірки пакетів: {ex.Message}");
                _progressText = "Помилка перевірки пакетів";
            }
        }
        
        /// <summary>
        /// Показує панель статусу API для виправлення проблем з підключенням
        /// </summary>
        private void ShowApiStatusPanel()
        {
            try
            {
                // Створюємо нове вікно для відображення статусу API
                var apiStatusWindow = EditorWindow.GetWindow<APITroubleshooterWindow>("API Status");
                apiStatusWindow.minSize = new Vector2(400, 300);
                // Передаємо поточні налаштування (якщо потрібно, додайте відповідний метод ініціалізації)
                // apiStatusWindow.Initialize(_settings, _serviceFactory); // закоментовано, якщо такого методу немає
                Debug.Log("Відкрито вікно статусу API");
                _chatHistory.Add(new ChatMessage("System", "Відкрито вікно налагодження API підключення"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка відкриття вікна статусу API: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Зберігає результат генерації у файл
        /// </summary>
        /// <param name="content">Вміст для збереження</param>
        /// <param name="defaultFileName">Ім'я файлу за замовчуванням</param>
        /// <param name="extension">Розширення файлу (наприклад, "cs", "json", "txt")</param>
        private void SaveGenerationToFile(string content, string defaultFileName, string extension)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    _chatHistory.Add(new ChatMessage("System", "❌ Немає вмісту для збереження"));
                    return;
                }
                
                // Переконуємося, що розширення починається з крапки
                if (!extension.StartsWith("."))
                    extension = "." + extension;
                
                // Пропонуємо користувачу обрати місце для збереження файлу
                string path = EditorUtility.SaveFilePanel(
                    "Зберегти генерацію",
                    "Assets/Scripts",
                    defaultFileName + extension,
                    extension.Replace(".", ""));
                
                // Перевіряємо, чи користувач обрав шлях
                if (!string.IsNullOrEmpty(path))
                {
                    // Зберігаємо вміст у файл
                    System.IO.File.WriteAllText(path, content);
                    
                    // Якщо файл збережено в межах проекту Unity, оновлюємо AssetDatabase
                    if (path.StartsWith(Application.dataPath))
                    {
                        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                        AssetDatabase.ImportAsset(relativePath);
                        _chatHistory.Add(new ChatMessage("System", $"✅ Файл збережено: {relativePath}"));
                    }
                    else
                    {
                        _chatHistory.Add(new ChatMessage("System", $"✅ Файл збережено: {path}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка збереження файлу: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка збереження: {ex.Message}"));
            }
        }
        
        
        /// <summary>
        /// Створює персонажа героя з базовим контролером руху
        /// /// </summary
        /// <summary>
        /// Створює персонажа героя з базовим контролером руху
        /// /// </summary>
        private void CreateHeroCharacter()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", "🔄 Генерую персонажа героя..."));
                
                // Створюємо новий об'єкт для героя
                GameObject hero = new GameObject("Hero");
                
                // Додаємо необхідні компоненти
                hero.AddComponent<CharacterController>();
                
                // Додаємо капсулу для візуалізації
                if (hero.GetComponent<MeshFilter>() == null)
                {
                    // Створюємо дочірній об'єкт для візуалізації
                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    visual.transform.SetParent(hero.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.name = "Visual";
                }
                
                // Додаємо базовий скрипт руху персонажа
                string scriptContent = @"using UnityEngine;

public class HeroController : MonoBehaviour
{
    public float speed = 5.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        isGrounded = controller.isGrounded;
        
        // Рух по горизонталі
        float horizontal = Input.GetAxis(""Horizontal"");
        float vertical = Input.GetAxis(""Vertical"");
        
        if (isGrounded)
        {
            moveDirection = new Vector3(horizontal, 0, vertical);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            
            // Стрибок
            if (Input.GetButton(""Jump""))
            {
                moveDirection.y = jumpSpeed;
            }
        }
        
        // Застосовуємо гравітацію
        moveDirection.y -= gravity * Time.deltaTime;
        
        // Рухаємо персонажа
        controller.Move(moveDirection * Time.deltaTime);
    }
}";

                // Створюємо папку для скриптів, якщо вона не існує
                string scriptsFolder = "Assets/Scripts";
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Scripts")))
                {
                    AssetDatabase.CreateFolder("Assets", "Scripts");
                }
                
                // Зберігаємо скрипт
                string scriptPath = System.IO.Path.Combine(scriptsFolder, "HeroController.cs");
                System.IO.File.WriteAllText(scriptPath.Replace('\\', '/'), scriptContent);
                AssetDatabase.Refresh();
                
                // Через деякий час додаємо новостворений компонент до героя
                EditorApplication.delayCall += () => {
                    var heroControllerType = System.Type.GetType("HeroController, Assembly-CSharp");
                    if (heroControllerType != null)
                    {
                        hero.AddComponent(heroControllerType);
                        _chatHistory.Add(new ChatMessage("System", "✅ Персонажа героя успішно створено"));
                    }
                    else
                    {
                        Debug.LogError("Не вдалося знайти тип HeroController");
                        _chatHistory.Add(new ChatMessage("System", "⚠️ Скрипт героя створено, але не вдалося додати компонент"));
                    }
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Помилка створення героя: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Створює ігрове поле бою з базовими елементами
        /// </summary>
        private void CreateBattlefield()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", "🔄 Генерую поле бою..."));
                
                // Створюємо підлогу
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Battlefield";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(5, 1, 5);
                
                // Створюємо стіни для оточення
                CreateWall("WallNorth", new Vector3(0, 2.5f, 50), new Vector3(50, 5, 1));
                CreateWall("WallSouth", new Vector3(0, 2.5f, -50), new Vector3(50, 5, 1));
                CreateWall("WallEast", new Vector3(50, 2.5f, 0), new Vector3(1, 5, 50));
                CreateWall("WallWest", new Vector3(-50, 2.5f, 0), new Vector3(1, 5, 50));
                
                // Створюємо перешкоди
                for (int i = 0; i < 10; i++)
                {
                    float x = UnityEngine.Random.Range(-45f, 45f);
                    float z = UnityEngine.Random.Range(-45f, 45f);
                    
                    GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obstacle.name = $"Obstacle_{i}";
                    obstacle.transform.position = new Vector3(x, 1, z);
                    obstacle.transform.localScale = new Vector3(
                        UnityEngine.Random.Range(1f, 3f),
                        UnityEngine.Random.Range(1f, 5f),
                        UnityEngine.Random.Range(1f, 3f));
                }
                
                // Створюємо джерело світла
                GameObject light = new GameObject("Directional Light");
                Light lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                lightComp.intensity = 1.0f;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                
                _chatHistory.Add(new ChatMessage("System", "✅ Поле бою успішно згенеровано"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка створення поля бою: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Допоміжний метод для створення стіни
        /// </summary>
        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            
            // Додаємо компонент статичного зіткнення
            wall.AddComponent<BoxCollider>();
        }
        
        /// <summary>
        /// З'єднує всі скрипти, створюючи необхідні залежності
        /// </summary>
        private void ConnectAllScripts()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", "🔄 З'єдную скрипти..."));
                
                // Шукаємо всі скрипти в проекті
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script");
                List<string> scriptPaths = new List<string>();
                
                foreach (string guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    scriptPaths.Add(path);
                }
                
                // Логуємо знайдені скрипти для налагодження
                if (scriptPaths.Count == 0)
                {
                    _chatHistory.Add(new ChatMessage("System", "⚠️ Не знайдено жодного скрипту для з'єднання"));
                    return;
                }
                
                _chatHistory.Add(new ChatMessage("System", $"📋 Знайдено {scriptPaths.Count} скриптів"));
                
                // Перебираємо всі ігрові об'єкти сцени
                GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                
                // Аналізуємо назви об'єктів і скриптів для автоматичного з'єднання
                foreach (GameObject obj in allObjects)
                {
                    foreach (string scriptPath in scriptPaths)
                    {
                        string scriptName = System.IO.Path.GetFileNameWithoutExtension(scriptPath);
                        
                        // Якщо назва об'єкта містить частину назви скрипта
                        if (obj.name.Contains(scriptName) || 
                            CheckObjectCategoryMatch(obj.name, scriptName))
                        {
                            // Перевіряємо, чи скрипт вже додано до об'єкта
                            MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                            bool scriptAlreadyAdded = false;
                            
                            foreach (var comp in components)
                            {
                                if (comp != null && comp.GetType().Name == scriptName)
                                {
                                    scriptAlreadyAdded = true;
                                    break;
                                }
                            }
                            
                            // Якщо скрипт ще не додано, додаємо його
                            if (!scriptAlreadyAdded)
                            {
                                // Завантажуємо скрипт і додаємо до об'єкта
                                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                                if (script != null)
                                {
                                    System.Type scriptType = script.GetClass();
                                    if (scriptType != null && typeof(MonoBehaviour).IsAssignableFrom(scriptType))
                                    {
                                        try
                                        {
                                            obj.AddComponent(scriptType);
                                            _chatHistory.Add(new ChatMessage("System", $"✅ Додано скрипт {scriptName} до об'єкта {obj.name}"));
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.LogError($"Помилка додавання скрипта {scriptName} до об'єкта {obj.name}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                _chatHistory.Add(new ChatMessage("System", "✅ З'єднання скриптів завершено"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка з'єднання скриптів: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Перевіряє, чи категорія об'єкта відповідає призначенню скрипта
        /// </summary>
        private bool CheckObjectCategoryMatch(string objectName, string scriptName)
        {
            // Простий приклад відповідності категорій
            if ((objectName.ToLower().Contains("player") || objectName.ToLower().Contains("hero")) && 
                (scriptName.ToLower().Contains("player") || scriptName.ToLower().Contains("controller") || 
                 scriptName.ToLower().Contains("character") || scriptName.ToLower().Contains("hero")))
            {
                return true;
            }
            
            if (objectName.ToLower().Contains("enemy") && 
                (scriptName.ToLower().Contains("enemy") || scriptName.ToLower().Contains("ai") || 
                 scriptName.ToLower().Contains("npc")))
            {
                return true;
            }
            
            if ((objectName.ToLower().Contains("ui") || objectName.ToLower().Contains("canvas")) && 
                (scriptName.ToLower().Contains("ui") || scriptName.ToLower().Contains("hud") || 
                 scriptName.ToLower().Contains("menu")))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Спроба виправити помилки в скриптах проекту
        /// </summary>
        private void FixScriptErrors()
        {
            try
            {
                _chatHistory.Add(new ChatMessage("System", "🔄 Аналізую скрипти на помилки..."));
                
                // Отримуємо всі скрипти в проекті
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script");
                List<string> scriptPaths = new List<string>();
                
                foreach (string guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    scriptPaths.Add(path);
                }
                
                if (scriptPaths.Count == 0)
                {
                    _chatHistory.Add(new ChatMessage("System", "⚠️ Не знайдено жодного скрипту для перевірки"));
                    return;
                }
                
                _chatHistory.Add(new ChatMessage("System", $"Знайдено {scriptPaths.Count} скриптів для перевірки"));
                
                // Список для зберігання помилок
                List<string> errorMessages = new List<string>();
                
                // Для кожного скрипта перевіряємо наявність типових помилок
                foreach (string scriptPath in scriptPaths)
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(scriptPath);
                        string scriptName = System.IO.Path.GetFileNameWithoutExtension(scriptPath);
                        
                        // Перелік базових виправлень:
                        bool wasModified = false;
                        
                        // 1. Виправлення відсутності using директив
                        if (!content.Contains("using UnityEngine;") && content.Contains("MonoBehaviour"))
                        {
                            content = "using UnityEngine;\n" + content;
                            wasModified = true;
                        }
                        
                        if (!content.Contains("using System.Collections;") && content.Contains("IEnumerator"))
                        {
                            content = "using System.Collections;\n" + content;
                            wasModified = true;
                        }
                        
                        // 2. Виправлення неправильної реалізації MonoBehaviour
                        if (content.Contains("public class " + scriptName) && content.Contains("MonoBehaviour") && 
                            !content.Contains("public class " + scriptName + " : MonoBehaviour"))
                        {
                            content = content.Replace(
                                "public class " + scriptName, 
                                "public class " + scriptName + " : MonoBehaviour");
                            wasModified = true;
                        }
                        
                        // 3. Виправлення відсутніх дужок
                        int openBraces = content.Count(c => c == '{');
                        int closeBraces = content.Count(c => c == '}');
                        
                        if (openBraces > closeBraces)
                        {
                            // Додаємо закриваючі дужки
                            for (int i = 0; i < openBraces - closeBraces; i++)
                            {
                                content += "\n}";
                            }
                            wasModified = true;
                        }
                        
                        // Зберігаємо виправлений файл
                        if (wasModified)
                        {
                            System.IO.File.WriteAllText(scriptPath, content);
                            errorMessages.Add($"✅ Виправлено скрипт: {scriptName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Помилка під час перевірки скрипта {scriptPath}: {ex.Message}");
                        errorMessages.Add($"❌ Не вдалося перевірити {System.IO.Path.GetFileNameWithoutExtension(scriptPath)}: {ex.Message}");
                    }
                }
                
                // Оновлюємо базу ресурсів
                AssetDatabase.Refresh();
                
                // Виводимо результат
                if (errorMessages.Count > 0)
                {
                    _chatHistory.Add(new ChatMessage("System", string.Join("\n", errorMessages)));
                }
                else
                {
                    _chatHistory.Add(new ChatMessage("System", "✅ Не знайдено помилок для виправлення"));
                }
                
                _chatHistory.Add(new ChatMessage("System", "⚠️ Зверніть увагу, що автоматичне виправлення може не усунути всі помилки. Для складніших випадків може знадобитися ручне редагування скриптів."));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка перевірки скриптів: {ex.Message}");
                _chatHistory.Add(new ChatMessage("System", $"❌ Помилка: {ex.Message}"));
            }
        }
    }
}

