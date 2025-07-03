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
    /// <summary>
    /// Сервіс для роботи з локальними моделями через Ollama
    /// </summary>
    public class OllamaService : IAIService
    {
        private readonly AIAgentSettings _settings;
        private readonly HttpClient _httpClient = new HttpClient();
        private List<string> _availableModels = null;
        
        public OllamaService(AIAgentSettings settings)
        {
            _settings = settings;
            // Встановлюємо timeout для запитів до локального серверу
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public bool IsConfigured() => _settings != null && 
                                      _settings.useOllamaAPI && 
                                      !string.IsNullOrEmpty(_settings.ollamaEndpoint) &&
                                      !string.IsNullOrEmpty(_settings.ollamaModelName);
        
        public string GetServiceName() => "Ollama";
        
        public async Task<AIResponse> QueryAI(string prompt, List<string> chatHistory)
        {
            if (!IsConfigured())
                return new AIResponse { IsSuccess = false, ErrorMessage = "Ollama not properly configured." };

            try
            {
                return await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Підготовка контексту з історії чату
                    StringBuilder contextBuilder = new StringBuilder();
                    
                    // Додаємо системний промпт
                    if (!string.IsNullOrEmpty(_settings.systemPrompt))
                    {
                        contextBuilder.AppendLine($"System: {_settings.systemPrompt}");
                        contextBuilder.AppendLine();
                    }
                    
                    // Додаємо історію чату
                    foreach (var entry in chatHistory.TakeLast(_settings.maxHistoryLength))
                    {
                        contextBuilder.AppendLine(entry);
                    }
                    
                    // Формуємо запит
                    var requestBody = new
                    {
                        model = _settings.ollamaModelName,
                        prompt = prompt,
                        context = contextBuilder.ToString(),
                        stream = false,
                        temperature = _settings.temperature,
                        num_predict = _settings.maxTokens
                    };
                    
                    string jsonPayload = JsonConvert.SerializeObject(requestBody);
                    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    
                    // Викликаємо API
                    HttpResponseMessage response = await _httpClient.PostAsync(_settings.ollamaEndpoint, httpContent);
                    string resultJson = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"Ollama API Error: {response.StatusCode}. Details: {resultJson}");
                        
                        string errorMsg = $"API Error: {response.StatusCode}. Details: {resultJson}";
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            errorMsg = "Не вдається підключитися до Ollama API. Переконайтеся, що Ollama запущена за адресою: " + 
                                      _settings.ollamaEndpoint;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            errorMsg = $"Помилка запиту до Ollama. Можливо, модель '{_settings.ollamaModelName}' не встановлена. " + 
                                     $"Встановіть її командою: ollama pull {_settings.ollamaModelName}";
                        }
                        
                        return new AIResponse { IsSuccess = false, ErrorMessage = errorMsg };
                    }
                    
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    string response_text = responseObject.response;
                    
                    return new AIResponse { Content = response_text, IsSuccess = true };
                }, 
                3, // кількість повторних спроб 
                1500, // затримка між спробами (мс)
                ex => ex is HttpRequestException); // повторювати тільки при помилках HTTP запиту
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"HTTP Request Error: {e.Message}");
                return new AIResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Помилка підключення до Ollama: {e.Message}\n\n" +
                                   "Переконайтеся, що Ollama запущена на вашому комп'ютері." 
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OllamaService: {e.Message}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"Непередбачена помилка: {e.Message}" };
            }
        }
        
        /// <summary>
        /// Отримує список доступних моделей Ollama
        /// </summary>
        public async Task<List<string>> GetAvailableModels(bool forceRefresh = false)
        {
            if (_availableModels != null && !forceRefresh)
            {
                return _availableModels;
            }
            
            _availableModels = new List<string>();
            
            try
            {
                // Формуємо URL для запиту списку моделей
                string listModelsUrl = _settings.ollamaEndpoint.Replace("/generate", "/tags");
                
                HttpResponseMessage response = await _httpClient.GetAsync(listModelsUrl);
                string resultJson = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    
                    if (responseObject.models != null)
                    {
                        foreach (var model in responseObject.models)
                        {
                            string modelName = model.name;
                            _availableModels.Add(modelName);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to get Ollama models: {response.StatusCode}. Response: {resultJson}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Ollama models: {e.Message}");
            }
            
            return _availableModels;
        }
        
        /// <summary>
        /// Перевіряє, чи запущений Ollama сервер та повертає статус
        /// </summary>
        public async Task<bool> IsServerRunning()
        {
            try
            {
                string baseUrl = _settings.ollamaEndpoint.Replace("/api/generate", "");
                if (!baseUrl.EndsWith("/api")) baseUrl += "/api";
                string healthCheckUrl = baseUrl + "/version";
                
                // Встановлюємо короткий таймаут для перевірки
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                {
                    // Робимо запит до API версії
                    var response = await client.GetAsync(healthCheckUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string versionInfo = await response.Content.ReadAsStringAsync();
                        Debug.Log($"Ollama доступний: {versionInfo}");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Ollama сервер відповів з кодом: {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Не вдалося підключитися до Ollama: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Отримує детальну інформацію про конкретну модель Ollama
        /// </summary>
        public async Task<string> GetModelInfo(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return "Не вказана назва моделі.";
                
            try
            {
                string baseUrl = _settings.ollamaEndpoint.Replace("/api/generate", "");
                if (!baseUrl.EndsWith("/api")) baseUrl += "/api";
                string modelInfoUrl = $"{baseUrl}/show";
                
                // Формуємо запит для отримання інформації про модель
                var requestBody = new { name = modelName };
                string jsonPayload = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                HttpResponseMessage response = await _httpClient.PostAsync(modelInfoUrl, httpContent);
                string resultJson = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    return JsonConvert.SerializeObject(responseObject, Formatting.Indented);
                }
                else
                {
                    return $"Помилка отримання інформації про модель: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Помилка: {ex.Message}";
            }
        }
    }
}
