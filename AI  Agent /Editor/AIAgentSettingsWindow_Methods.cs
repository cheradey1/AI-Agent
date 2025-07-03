using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace UnityAIAgent
{
    // Часткова реалізація класу AIAgentSettingsWindow з методами перевірки ключів
    public partial class AIAgentSettingsWindow : EditorWindow
    {
        /// <summary>
        /// Перевіряє валідність API ключа OpenAI
        /// </summary>
        private async void CheckOpenAIKey()
        {
            if (string.IsNullOrEmpty(_openAIKey))
            {
                EditorUtility.DisplayDialog("Помилка", "OpenAI API ключ не введено", "OK");
                return;
            }
            
            Debug.Log("Перевірка ключа OpenAI...");
            
            try 
            {
                var openAiService = new OpenAIService(_settings);
                var testResponse = await openAiService.QueryAI("Привіт. Це тест API.", new List<string>());
                
                if (testResponse.IsSuccess)
                {
                    Debug.Log("✅ API ключ OpenAI працює коректно.");
                    EditorUtility.DisplayDialog("Успіх", "API ключ OpenAI працює коректно.", "OK");
                }
                else
                {
                    Debug.LogError($"❌ Помилка перевірки API ключа OpenAI: {testResponse.ErrorMessage}");
                    EditorUtility.DisplayDialog("Помилка", $"Ключ OpenAI не працює: {testResponse.ErrorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Помилка перевірки API ключа OpenAI: {ex.Message}");
                EditorUtility.DisplayDialog("Помилка", $"Помилка перевірки ключа OpenAI: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// Перевіряє валідність API ключа Anthropic Claude
        /// </summary>
        private async void CheckAnthropicKey()
        {
            if (string.IsNullOrEmpty(_settings.anthropicApiKey))
            {
                EditorUtility.DisplayDialog("Помилка", "Anthropic API ключ не введено", "OK");
                return;
            }
            
            Debug.Log("Перевірка ключа Anthropic...");
            
            try
            {
                var anthropicService = new AnthropicService(_settings);
                var testResponse = await anthropicService.QueryAI("Це тестове повідомлення для перевірки API ключа.", new List<string>());
                
                if (testResponse.IsSuccess)
                {
                    Debug.Log("✅ API ключ Anthropic працює коректно.");
                    EditorUtility.DisplayDialog("Успіх", "API ключ Anthropic працює коректно.", "OK");
                }
                else
                {
                    Debug.LogError($"❌ Помилка перевірки API ключа Anthropic: {testResponse.ErrorMessage}");
                    EditorUtility.DisplayDialog("Помилка", $"Ключ Anthropic не працює: {testResponse.ErrorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Помилка перевірки API ключа Anthropic: {ex.Message}");
                EditorUtility.DisplayDialog("Помилка", $"Помилка перевірки ключа Anthropic: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// Перевіряє валідність API ключа Google Gemini
        /// </summary>
        private async void CheckGeminiKey()
        {
            if (string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                EditorUtility.DisplayDialog("Помилка", "Google Gemini API ключ не введено", "OK");
                return;
            }
            
            // Спочатку виконуємо автоматичну корекцію формату API ключа
            string fixedKey = APIKeyHelper.FixGeminiApiKey(_settings.geminiApiKey);
            if (fixedKey != _settings.geminiApiKey)
            {
                _settings.geminiApiKey = fixedKey;
                EditorUtility.SetDirty(_settings);
                Debug.Log("Виконано автоматичне виправлення формату API ключа Gemini");
            }
            
            // Проводимо аналіз ключа
            var analysis = APIKeyHelper.AnalyzeGeminiKey(_settings.geminiApiKey);
            
            // Якщо формат ключа не виглядає валідним
            if (!analysis.isValid)
            {
                string warning = "Увага! Введений API ключ не схожий на стандартний формат ключа Google Gemini.\n\n" +
                                "Ключі Gemini зазвичай починаються з 'AI' або 'g-' та мають значну довжину.\n\n" +
                                "Переконайтеся, що ви отримали ключ з офіційного сайту: https://ai.google.dev/";
                
                bool proceed = EditorUtility.DisplayDialog(
                    "Можливо некоректний формат ключа", 
                    warning,
                    "Все одно перевірити", "Скасувати");
                
                if (!proceed) return;
            }
            
            Debug.Log("Перевірка ключа Google Gemini...");
            
            try
            {
                // Видаляємо зайві пробіли в ключі
                _settings.geminiApiKey = _settings.geminiApiKey.Trim();
                
                var geminiService = new GeminiService(_settings);
                var testResponse = await geminiService.QueryAI("Це тестове повідомлення для перевірки API ключа.", new List<string>());
                
                if (testResponse.IsSuccess)
                {
                    Debug.Log("✅ API ключ Google Gemini працює коректно.");
                    EditorUtility.DisplayDialog("Успіх", "API ключ Google Gemini працює коректно.", "OK");
                    
                    // Зберігаємо налаштування, оскільки ми могли видалити пробіли
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                    
                    // Оновлюємо список доступних моделей у фоні
                    FetchAvailableGeminiModels();
                }
                else
                {
                    Debug.LogError($"❌ Помилка перевірки API ключа Google Gemini: {testResponse.ErrorMessage}");
                    
                    // Показуємо більш інформативну помилку
                    string errorMsg = $"Ключ Google Gemini не працює: {testResponse.ErrorMessage}";
                    
                    // Пропонуємо кілька опцій для вирішення проблеми
                    int option = EditorUtility.DisplayDialogComplex(
                        "Помилка перевірки ключа", 
                        errorMsg + "\n\nОберіть дію для вирішення проблеми:", 
                        "Відкрити інструмент усунення проблем", 
                        "Отримати новий ключ", 
                        "Скасувати");
                    
                    switch (option)
                    {
                        case 0: // Інструмент усунення проблем
                            APITroubleshooterWindow.ShowWindow();
                            break;
                        case 1: // Отримати новий ключ
                            Application.OpenURL("https://makersuite.google.com/app/apikey");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Помилка перевірки API ключа Google Gemini: {ex.Message}");
                EditorUtility.DisplayDialog("Помилка", $"Помилка перевірки ключа Google Gemini: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// Перевіряє підключення до Ollama
        /// </summary>
        private async void CheckOllamaConnection()
        {
            if (string.IsNullOrEmpty(_settings.ollamaEndpoint) || string.IsNullOrEmpty(_settings.ollamaModelName))
            {
                EditorUtility.DisplayDialog("Помилка", "Перевірте налаштування Ollama: потрібно вказати ендпоінт та назву моделі", "OK");
                return;
            }
            
            Debug.Log("Перевірка підключення до Ollama...");
            
            try
            {
                var ollamaService = new OllamaService(_settings);
                var testResponse = await ollamaService.QueryAI("Це тестове повідомлення для перевірки Ollama.", new List<string>());
                
                if (testResponse.IsSuccess)
                {
                    Debug.Log("✅ З'єднання з Ollama працює коректно.");
                    EditorUtility.DisplayDialog("Успіх", 
                        $"З'єднання з Ollama працює коректно.\n\nВідповідь моделі '{_settings.ollamaModelName}':\n{testResponse.Content.Substring(0, Math.Min(200, testResponse.Content.Length))}...", 
                        "OK");
                }
                else
                {
                    Debug.LogError($"❌ Помилка з'єднання з Ollama: {testResponse.ErrorMessage}");
                    EditorUtility.DisplayDialog("Помилка", $"З'єднання з Ollama не працює: {testResponse.ErrorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Помилка з'єднання з Ollama: {ex.Message}");
                EditorUtility.DisplayDialog("Помилка", $"Помилка з'єднання з Ollama: {ex.Message}\n\nПереконайтесь, що Ollama запущена на вашому комп'ютері.", "OK");
            }
        }
        
        /// <summary>
        /// Отримує та відображає список доступних локальних моделей Ollama
        /// </summary>
        private async void CheckOllamaModels()
        {
            if (string.IsNullOrEmpty(_settings.ollamaEndpoint))
            {
                EditorUtility.DisplayDialog("Помилка", "Необхідно вказати API Endpoint для Ollama.", "OK");
                return;
            }
            
            Debug.Log("Отримання списку моделей Ollama...");
            EditorUtility.DisplayProgressBar("Ollama", "Отримання списку доступних моделей...", 0.5f);
            
            try
            {
                var ollamaService = new OllamaService(_settings);
                var isRunning = await ollamaService.IsServerRunning();
                
                if (!isRunning)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Помилка", 
                        "Неможливо підключитися до Ollama. Переконайтеся, що Ollama запущена на вашому комп'ютері.", "OK");
                    return;
                }
                
                var models = await ollamaService.GetAvailableModels(true);
                EditorUtility.ClearProgressBar();
                
                if (models != null && models.Count > 0)
                {
                    // Групуємо моделі для кращого відображення
                    var llama = models.Where(m => m.Contains("llama")).OrderBy(m => m).ToList();
                    var mistral = models.Where(m => m.Contains("mistral")).OrderBy(m => m).ToList();
                    var other = models.Where(m => !m.Contains("llama") && !m.Contains("mistral")).OrderBy(m => m).ToList();
                    
                    // Перевіряємо чи поточна вибрана модель доступна
                    bool isCurrentModelAvailable = models.Contains(_settings.ollamaModelName);
                    
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Доступні локальні моделі Ollama:");
                    
                    if (llama.Count > 0)
                    {
                        sb.AppendLine("\n📊 Llama моделі:");
                        llama.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    if (mistral.Count > 0)
                    {
                        sb.AppendLine("\n📊 Mistral моделі:");
                        mistral.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    if (other.Count > 0)
                    {
                        sb.AppendLine("\n📊 Інші моделі:");
                        other.ForEach(m => sb.AppendLine($"  • {m}"));
                    }
                    
                    sb.AppendLine("\nВиберіть модель, щоб використати її:");
                    
                    // Створюємо список кнопок для кожної моделі
                    var combinedList = models.OrderBy(m => m).ToList();
                    
                    sb.AppendLine($"\nПоточна модель: {_settings.ollamaModelName}");
                    
                    if (!isCurrentModelAvailable)
                    {
                        sb.AppendLine("\n⚠️ Поточна модель не знайдена серед доступних моделей!");
                        sb.AppendLine("Щоб встановити нову модель, використайте команду:");
                        sb.AppendLine($"ollama pull {_settings.ollamaModelName}");
                    }
                    
                    int selection = EditorUtility.DisplayDialogComplex(
                        "Доступні моделі Ollama", 
                        sb.ToString(), 
                        "Закрити", 
                        "Використати вибрану", 
                        "Показати деталі моделі");
                    
                    if (selection == 1) // "Використати вибрану"
                    {
                        ShowModelSelectionDialog(models);
                    }
                    else if (selection == 2) // "Показати деталі моделі"
                    {
                        ShowModelDetailsDialog(models);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Результат", 
                        "Не знайдено жодної встановленої моделі Ollama.\n\nЩоб встановити модель, виконайте команду:\nollama pull MODEL_NAME\n\nПриклади: ollama pull llama3, ollama pull mistral", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"❌ Помилка отримання списку моделей Ollama: {ex.Message}");
                EditorUtility.DisplayDialog("Помилка", 
                    $"Помилка отримання списку моделей Ollama: {ex.Message}\n\nПереконайтесь, що Ollama запущена на вашому комп'ютері.", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Показує діалог для вибору моделі Ollama
        /// </summary>
        private void ShowModelSelectionDialog(List<string> models)
        {
            if (models == null || models.Count == 0)
                return;
                
            GenericMenu menu = new GenericMenu();
            
            foreach (var model in models.OrderBy(m => m))
            {
                menu.AddItem(new GUIContent(model), model == _settings.ollamaModelName, OnModelSelected, model);
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Обробник вибору моделі
        /// </summary>
        private void OnModelSelected(object userData)
        {
            string selectedModel = userData as string;
            if (!string.IsNullOrEmpty(selectedModel))
            {
                _settings.ollamaModelName = selectedModel;
                EditorUtility.SetDirty(_settings);
                Debug.Log($"Вибрано модель Ollama: {selectedModel}");
                
                // Оновлюємо UI
                Repaint();
            }
        }
        
        /// <summary>
        /// Показує діалог для отримання деталей про модель
        /// </summary>
        private void ShowModelDetailsDialog(List<string> models)
        {
            if (models == null || models.Count == 0)
                return;
                
            GenericMenu menu = new GenericMenu();
            
            foreach (var model in models.OrderBy(m => m))
            {
                menu.AddItem(new GUIContent(model), model == _settings.ollamaModelName, OnModelDetailsRequested, model);
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Обробник запиту деталей моделі
        /// </summary>
        private async void OnModelDetailsRequested(object userData)
        {
            string selectedModel = userData as string;
            if (string.IsNullOrEmpty(selectedModel))
                return;
                
            EditorUtility.DisplayProgressBar("Ollama", $"Отримання інформації про модель {selectedModel}...", 0.5f);
            
            try
            {
                var ollamaService = new OllamaService(_settings);
                string modelInfo = await ollamaService.GetModelInfo(selectedModel);
                
                EditorUtility.ClearProgressBar();
                
                // Форматуємо вивід для кращого читання
                string formattedInfo = FormatModelInfo(modelInfo, selectedModel);
                
                EditorUtility.DisplayDialog(
                    $"Інформація про модель: {selectedModel}", 
                    formattedInfo,
                    "Закрити"
                );
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Помилка", 
                    $"Не вдалося отримати інформацію про модель: {ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Форматує JSON інформацію про модель для кращого відображення
        /// </summary>
        private string FormatModelInfo(string jsonInfo, string modelName)
        {
            try 
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonInfo);
                StringBuilder sb = new StringBuilder();
                
                sb.AppendLine($"Модель: {modelName}");
                
                // Основна інформація
                if (jsonObject.parameters != null)
                {
                    sb.AppendLine("\nПараметри:");
                    foreach (var param in jsonObject.parameters)
                    {
                        string name = param.Name;
                        string value = param.Value?.ToString();
                        sb.AppendLine($"  • {name}: {value}");
                    }
                }
                
                // Розмір моделі
                if (jsonObject.size != null)
                {
                    double sizeInMB = Convert.ToDouble(jsonObject.size.ToString()) / (1024 * 1024);
                    double sizeInGB = sizeInMB / 1024;
                    
                    if (sizeInGB >= 1)
                        sb.AppendLine($"\nРозмір: {sizeInGB:F2} ГБ");
                    else
                        sb.AppendLine($"\nРозмір: {sizeInMB:F2} МБ");
                }
                
                // Інформація про сімейство моделі
                if (jsonObject.family != null)
                {
                    sb.AppendLine($"\nСімейство: {jsonObject.family}");
                }
                
                return sb.ToString();
            }
            catch
            {
                // При помилці парсингу JSON просто виводимо необроблений текст
                return jsonInfo;
            }
        }
        
        /// <summary>
        /// Отримує список доступних моделей Gemini для поточного API ключа
        /// </summary>
        private async void FetchAvailableGeminiModels()
        {
            if (string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                _geminiModelUpdateStatus = "⚠️ Необхідно ввести API ключ";
                return;
            }
            
            try
            {
                _geminiModelUpdateStatus = "⏳ Отримання списку моделей...";
                Repaint();
                
                var geminiService = new GeminiService(_settings);
                var availableModels = await geminiService.GetAvailableModels(true);
                
                if (availableModels != null && availableModels.Count > 0)
                {
                    _cachedGeminiModels = availableModels;
                    _geminiModelUpdateStatus = $"✓ Отримано {availableModels.Count} моделей";
                    
                    // Якщо поточна модель не в списку, але список не порожній
                    if (!string.IsNullOrEmpty(_settings.geminiModelName) && 
                        !availableModels.Contains(_settings.geminiModelName) &&
                        availableModels.Count > 0)
                    {
                        // Встановлюємо першу доступну модель
                        _settings.geminiModelName = availableModels[0];
                        EditorUtility.SetDirty(_settings);
                        Debug.Log($"Автоматично змінено модель Gemini на доступну: {_settings.geminiModelName}");
                    }
                }
                else
                {
                    _geminiModelUpdateStatus = "⚠️ Моделі не знайдено";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка отримання моделей Gemini: {ex.Message}");
                _geminiModelUpdateStatus = "❌ Помилка при отриманні моделей";
            }
            
            Repaint();
        }
    }
}
