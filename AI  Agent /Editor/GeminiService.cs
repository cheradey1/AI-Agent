using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;

namespace UnityAIAgent
{
    public class GeminiService : IAIService
    {
        private readonly AIAgentSettings _settings;
        private readonly string _baseApiUrl = "https://generativelanguage.googleapis.com/v1";
        private readonly HttpClient _httpClient = new HttpClient();
        private List<string> _availableModels;
        
        public GeminiService(AIAgentSettings settings)
        {
            _settings = settings;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool IsConfigured() => _settings != null && !string.IsNullOrEmpty(_settings.geminiApiKey);
        public string GetServiceName() => "Google Gemini";

    public async Task<AIResponse> QueryAI(string prompt, List<string> chatHistory)
    {
        if (!IsConfigured())
            return new AIResponse { IsSuccess = false, ErrorMessage = "Gemini API key not configured." };

        try
        {
            // Додаткова перевірка ключа API перед запитом
            if (string.IsNullOrEmpty(_settings.geminiApiKey) || _settings.geminiApiKey.Trim().Length < 10)
            {
                return new AIResponse { 
                    IsSuccess = false, 
                    ErrorMessage = "Gemini API ключ недійсний або занадто короткий. Будь ласка, перевірте правильність ключа." 
                };
            }
            
            // Перевірка формату API ключа Google Gemini
            string trimmedKey = _settings.geminiApiKey.Trim();
            if (!trimmedKey.StartsWith("AI") && !trimmedKey.StartsWith("g-"))
            {
                return new AIResponse { 
                    IsSuccess = false, 
                    ErrorMessage = "Gemini API ключ має неправильний формат. Правильний ключ повинен починатися з 'AI' або 'g-'. " +
                                  "Будь ласка, перевірте, що ви використовуєте ключ з Google AI Studio (https://makersuite.google.com/app/apikey)." 
                };
            }
            
            // Використовуємо RetryHelper для автоматичних повторних спроб
            return await RetryHelper.ExecuteWithRetryAsync(async () =>
            {
                // Формуємо URL із ключем API та моделлю
                string modelName = string.IsNullOrEmpty(_settings.geminiModelName) ? 
                    "gemini-1.5-flash" : _settings.geminiModelName;

                // Перевірка та заміна застарілих моделей
                if (modelName == "gemini-pro-vision")
                {
                    Debug.LogWarning("Модель gemini-pro-vision застаріла з 12 липня 2024. Використання gemini-1.5-pro замість неї.");
                    modelName = "gemini-1.5-pro";
                }

                // ОТРИМУЄМО СПИСОК ДОСТУПНИХ МОДЕЛЕЙ АСИНХРОННО
                var availableModels = await GetAvailableModels();

                // ПРИВОДИМО ІМ'Я ДО ОДНОГО ФОРМАТУ (без "models/")
                string normalizedModelName = modelName;
                if (normalizedModelName.StartsWith("models/"))
                    normalizedModelName = normalizedModelName.Substring("models/".Length);

                if (!string.IsNullOrEmpty(normalizedModelName) && availableModels.Contains(normalizedModelName))
                {
                    modelName = normalizedModelName;
                }
                else if (availableModels != null && availableModels.Count > 0)
                {
                    Debug.LogWarning($"Виявлено невідому модель: '{modelName}'. Заміна на стандартну модель gemini-1.5-flash.");
                    modelName = "gemini-1.5-flash";
                    _settings.geminiModelName = modelName;
                    UnityEditor.EditorUtility.SetDirty(_settings);
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                // Формування URL
                string urlWithKey = $"{_baseApiUrl}/models/{modelName}:generateContent?key={_settings.geminiApiKey.Trim()}";
                // Створюємо історію чату у форматі Gemini
                    List<Dictionary<string, object>> messages = new List<Dictionary<string, object>>();
                    
                    // Додаємо системну інструкцію з розширеним контекстом
                    if (!string.IsNullOrEmpty(_settings.systemPrompt))
                    {
                        // Збираємо повну інформацію про проект Unity та поточну сцену
                        string projectContext = CollectUnityProjectContext();
                        string sceneContext = CollectUnitySceneContext();
                        
                        string enhancedPrompt = $"[Instructions: {_settings.systemPrompt}]\n\n" +
                            $"[Unity Project Context: {projectContext}]\n\n" +
                            $"[Current Unity Scene: {sceneContext}]\n\n" +
                            "Я працюю над Unity проектом. Ви маєте повний доступ до інформації про проект та сцену через контекст вище. " +
                            "Ви можете створювати або модифікувати ігрові сцени за запитом, використовуючи спеціальні команди, такі як #create_object, " +
                            "#generate_scene, #connect_scripts та інші. Будь ласка, генеруйте готові до використання команди та скрипти, що працюватимуть " +
                            "без додаткових модифікацій. Ви повинні відповідати повним готовим рішенням для запитаних ігор або сцен.";
                        
                        messages.Add(new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "parts", new[] { new { text = enhancedPrompt } } }
                        });
                        
                        messages.Add(new Dictionary<string, object>
                        {
                            { "role", "model" },
                            { "parts", new[] { new { text = "Я розумію і буду діяти як асистент з розробки Unity, беручи до уваги наданий контекст проекту." } } }
                        });
                    }
                    
                    // Додаємо історію чату
                    foreach (var entry in chatHistory.TakeLast(_settings.maxHistoryLength))
                    {
                        var parts = entry.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        string role = parts[0].ToLower();
                        string content = parts.Length > 1 ? parts[1] : "";
                        
                        if (role == "user")
                        {
                            messages.Add(new Dictionary<string, object>
                            {
                                { "role", "user" },
                                { "parts", new[] { new { text = content } } }
                            });
                        }
                        else if (role == "ai" || role == "assistant")
                        {
                            messages.Add(new Dictionary<string, object>
                            {
                                { "role", "model" },
                                { "parts", new[] { new { text = content } } }
                            });
                        }
                    }
                    
                    // Додаємо поточний запит
                    messages.Add(new Dictionary<string, object>
                    {
                        { "role", "user" },
                        { "parts", new[] { new { text = prompt } } }
                    });
                    
                    // Створюємо запит
                    var requestBody = new
                    {
                        contents = messages,
                        generationConfig = new
                        {
                            temperature = _settings.temperature,
                            maxOutputTokens = _settings.maxTokens,
                            topK = 40,
                            topP = 0.95
                        },
                        safetySettings = new object[] 
                        {
                            new { 
                                category = "HARM_CATEGORY_HARASSMENT", 
                                threshold = "BLOCK_ONLY_HIGH" 
                            },
                            new { 
                                category = "HARM_CATEGORY_HATE_SPEECH", 
                                threshold = "BLOCK_ONLY_HIGH" 
                            }
                        }
                    };
                    
                    string jsonPayload = JsonConvert.SerializeObject(requestBody);
                    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    
                    // Викликаємо API з правильним URL
                    Debug.Log($"Виклик Gemini API на ендпоінт: {urlWithKey}");
                    
                    // Додаємо додаткові заголовки для запиту
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "UnityAIAgent/2.0.0");
                    
                    HttpResponseMessage response = await _httpClient.PostAsync(urlWithKey, httpContent);
                    string resultJson = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMessage = $"Gemini API Error: {response.StatusCode}. Details: {resultJson}";
                        
                        try
                        {
                            // Додаємо інформацію про використану модель
                            string errorContextInfo = $"Модель: {modelName}\n";
                            
                            // Отримуємо конкретні рекомендації для типу помилки
                            string recommendation = APITroubleshooterWindow.GetSpecificErrorRecommendation(resultJson);
                            
                            // Додаємо до повідомлення помилки
                            errorMessage += $"\n\n{errorContextInfo}Рекомендації:\n{recommendation}" + 
                                           "\n\nВідкрийте вікно усунення проблем (Window > AI Assistant > Troubleshoot API) для детальної діагностики.";
                        }
                        catch (Exception recommendationEx)
                        {
                            // Якщо виникла проблема з отриманням рекомендацій, використовуємо стандартний варіант
                            Debug.LogWarning($"Помилка при отриманні рекомендацій: {recommendationEx.Message}");
                            
                            if (resultJson.Contains("API_KEY_INVALID") || resultJson.Contains("key not valid"))
                            {
                                errorMessage += "\n\nПорада: Ваш API ключ невалідний або має неправильний формат. " +
                                               "Скористайтесь інструментом усунення проблем для діагностики ключа.";
                            }
                            else if (resultJson.Contains("not found") || resultJson.Contains("model not found"))
                            {
                                errorMessage += $"\n\nПорада: Обрана модель '{modelName}' не знайдена або недоступна. " +
                                              "Спробуйте іншу модель, наприклад gemini-1.5-flash або gemini-1.5-pro.";
                            }
                            else if (resultJson.Contains("deprecated") || resultJson.Contains("deprecat"))
                            {
                                errorMessage += $"\n\nПорада: Модель '{modelName}' застаріла та більше не підтримується. " +
                                              "Будь ласка, переключіться на одну з нових моделей, наприклад gemini-1.5-flash або gemini-1.5-pro.";
                            }
                        }
                        
                        return new AIResponse { IsSuccess = false, ErrorMessage = errorMessage };
                    }
                    
                    // Розбираємо відповідь
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    string responseContent = "";
                    
                    try
                    {
                        // Перевіряємо наявність блокування з боку Safety Filter
                        if (responseObject.promptFeedback != null && 
                            responseObject.promptFeedback.blockReason != null)
                        {
                            string blockReason = responseObject.promptFeedback.blockReason;
                            return new AIResponse 
                            { 
                                IsSuccess = false, 
                                ErrorMessage = $"Запит заблоковано системою безпеки Gemini: {blockReason}" 
                            };
                        }
                        
                        responseContent = responseObject.candidates[0].content.parts[0].text;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing Gemini response: {ex.Message}");
                        return new AIResponse 
                        { 
                            IsSuccess = false, 
                            ErrorMessage = $"Помилка обробки відповіді: {ex.Message}. JSON: {resultJson}" 
                        };
                    }
                    
                    return new AIResponse { Content = responseContent, IsSuccess = true };
                },
                3, // кількість повторних спроб
                1500, // затримка між спробами
                ex => ex is HttpRequestException || (ex is JsonException) // які помилки слід повторно пробувати
                );
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"HTTP Request Error: {e.Message}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"Помилка мережі: {e.Message}" };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GeminiService: {e.Message}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"Непередбачена помилка: {e.Message}" };
            }
        }
        
        /// <summary>
        /// Отримує список доступних моделей Gemini
        /// </summary>
        public async Task<List<string>> GetAvailableModels(bool forceRefresh = false)
        {
            if (_availableModels != null && !forceRefresh)
            {
                return _availableModels;
            }
            
            // Встановлюємо список стандартних моделей, які будуть доступні навіть без успішного API запиту
            _availableModels = new List<string>
            {
                "gemini-pro",
                "gemini-1.5-pro",
                "gemini-1.5-flash",
                "gemini-1.5-pro-latest",
                "gemini-1.5-pro-preview"
            };
            
            // Якщо API ключ не встановлено, повертаємо стандартний список
            if (string.IsNullOrEmpty(_settings.geminiApiKey) || _settings.geminiApiKey.Trim().Length < 10)
            {
                Debug.LogWarning("Запит моделей Gemini пропущено: не встановлено валідний API ключ");
                return _availableModels;
            }
            
            try
            {
                string modelsUrl = $"{_baseApiUrl}/models?key={_settings.geminiApiKey}";
                
                HttpResponseMessage response = await _httpClient.GetAsync(modelsUrl);
                string resultJson = await response.Content.ReadAsStringAsync();                    if (response.IsSuccessStatusCode)
                    {
                        var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                        
                        if (responseObject.models != null)
                        {
                            _availableModels.Clear();
                            
                            foreach (var model in responseObject.models)
                            {
                                string modelName = model.name;
                                                // Отримуємо тільки назву моделі з повного шляху
                                if (modelName.Contains("/models/"))
                                {
                                    modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                                }
                                
                                // Додаємо тільки моделі Gemini
                                if (modelName.Contains("gemini"))
                                {
                                    _availableModels.Add(modelName);
                                    Debug.Log($"Знайдено модель Gemini: {modelName}");
                                }
                            }
                            
                            // Якщо це виклик для оновлення (forceRefresh=true), 
                            // оновлюємо також документацію
                            if (forceRefresh && _availableModels.Count > 0)
                            {
                                try
                                {
                                    // Оновлюємо документацію з новим списком моделей
                                    DocumentationUpdater.UpdateGeminiModelDocs(_availableModels);
                                }
                                catch (Exception docEx)
                                {
                                    Debug.LogWarning($"Не вдалося оновити документацію: {docEx.Message}");
                                }
                            }
                        }
                    }
                else
                {
                    Debug.LogError($"Failed to get Gemini models: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Gemini models: {e.Message}");
            }
            
            return _availableModels;
        }
        
        /// <summary>
        /// Збирає короткий контекст про Unity-проєкт для передачі в AI
        /// </summary>
        private string CollectUnityProjectContext()
        {
            try
            {
                // Збираємо основну інформацію про проект
                string projectName = Application.productName;
                string unityVersion = Application.unityVersion;
                string[] scenes = UnityEditor.EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();
                string scenesList = string.Join(", ", scenes);
                string[] scriptFiles = System.IO.Directory.GetFiles(Application.dataPath, "*.cs", System.IO.SearchOption.AllDirectories);
                int scriptCount = scriptFiles.Length;
                string mainSettings = $"Project: {projectName}\nUnity Version: {unityVersion}\nScenes: {scenesList}\nC# Scripts: {scriptCount} files";
                return mainSettings;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Не вдалося зібрати контекст проекту: {ex.Message}");
                return "[Context unavailable]";
            }
        }
        
        /// <summary>
        /// Збирає повний контекст поточної сцени Unity для AI агента
        /// </summary>
        private string CollectUnitySceneContext()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Scene context:");
                foreach (var go in UnityEngine.Object.FindObjectsByType<UnityEngine.GameObject>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (!go.scene.isLoaded) continue;
                    sb.AppendLine($"- GameObject: {go.name}");
                    sb.AppendLine($"  Active: {go.activeSelf}");
                    sb.AppendLine($"  Position: {go.transform.position}");
                    sb.AppendLine($"  Components:");
                    foreach (var comp in go.GetComponents<UnityEngine.Component>())
                    {
                        if (comp == null) continue;
                        sb.AppendLine($"    - {comp.GetType().Name}");
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Не вдалося зібрати повний контекст сцени: {ex.Message}");
                return "[Scene context unavailable]";
            }
        }
    }
}
