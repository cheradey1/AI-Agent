using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine; // For Debug.LogError, Debug.LogWarning
using System.Linq; // For TakeLast
using System.Reflection; // For property access via reflection

namespace UnityAIAgent
{
    public class AnthropicService : IAIService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly AIAgentSettings _settings;
        private readonly string _apiUrl = "https://api.anthropic.com/v1/messages";

        public AnthropicService(AIAgentSettings settings)
        {
            _settings = settings;
            _httpClient.Timeout = TimeSpan.FromSeconds(120); // Increased timeout
        }

        public bool IsConfigured() => _settings != null && !string.IsNullOrEmpty(_settings.anthropicApiKey) && _settings.useAnthropicClaudeAPI;
        public string GetServiceName() => "Anthropic Claude";
        
        public async Task<AIResponse> QueryAI(string prompt, List<string> chatHistory)
        {
            if (!IsConfigured())
                return new AIResponse { IsSuccess = false, ErrorMessage = "Anthropic API key not configured or service disabled." };

            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.anthropicApiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01"); // Or a newer version if available

                var messages = new List<object>();
                // System prompt is handled by the 'system' parameter in the request body for Claude 3
                foreach (var entry in chatHistory.TakeLast(_settings.maxHistoryLength))
                {
                    var parts = entry.Split(new[] { ": " }, 2, StringSplitOptions.None);
                    string role = parts[0].ToLower();
                    string content = parts.Length > 1 ? parts[1] : "";
                    if (role == "user") messages.Add(new { role = "user", content = content });
                    else if (role == "ai" || role == "assistant") messages.Add(new { role = "assistant", content = content });
                }
                messages.Add(new { role = "user", content = prompt });

                // Використовуємо безпечне значення для max_tokens, оскільки поле maxTokens може бути відсутнім
                int maxTokens = 4096; // Значення за замовчуванням
                
                // Використовуємо рефлексію для безпечного доступу до поля maxTokens
                try {
                    var maxTokensProperty = _settings.GetType().GetProperty("maxTokens");
                    if (maxTokensProperty != null) {
                        maxTokens = (int)maxTokensProperty.GetValue(_settings, null);
                    }
                }
                catch (Exception ex) {
                    Debug.LogWarning($"Could not access maxTokens property: {ex.Message}. Using default value of 4096.");
                }
                
                var requestBody = new
                {
                    model = _settings.claudeModelName,
                    system = _settings.systemPrompt, // System prompt for Claude
                    messages = messages,
                    temperature = _settings.temperature,
                    max_tokens = maxTokens
                };
                string jsonPayload = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, httpContent);
                string resultJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new AIResponse { IsSuccess = false, ErrorMessage = $"API Error: {response.StatusCode}. Details: {resultJson}" };
                }

                var responseObject = JsonConvert.DeserializeObject<dynamic>(resultJson);
                string aiContent = "";
                // Standard Claude API response structure
                if (responseObject.content != null && responseObject.content.Count > 0 && responseObject.content[0].text != null)
                {
                    aiContent = responseObject.content[0].text;
                }
                // Fallback for older/different structures if necessary, though 'completion' is usually for older text completion APIs
                else if (responseObject.completion != null) 
                {
                    aiContent = responseObject.completion;
                }
                else 
                {
                    Debug.LogWarning("Could not extract content from Anthropic response. Full response: " + resultJson);
                    return new AIResponse { IsSuccess = false, ErrorMessage = "Could not parse content from Anthropic response."};
                }

                return new AIResponse { Content = aiContent, IsSuccess = true };
            }
            catch (HttpRequestException e)
            {
                 Debug.LogError($"HTTP Request Error (Anthropic): {e.Message} - {e.StackTrace}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"Network error: {e.Message}" };
            }
            catch (JsonException e)
            {
                Debug.LogError($"JSON Parsing Error (Anthropic): {e.Message} - {e.StackTrace}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"Error parsing AI response: {e.Message}" };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in AnthropicService: {e.Message} - {e.StackTrace}");
                return new AIResponse { IsSuccess = false, ErrorMessage = $"An unexpected error occurred: {e.Message}" };
            }
        }
    }
}
