using UnityEngine;

namespace UnityAIAgent
{
    [CreateAssetMenu(fileName = "AIAgentSettings", menuName = "AI Agent/Settings", order = 1)]
    public class AIAgentSettings : ScriptableObject
    {
        [Header("OpenAI")]
        public string openAIApiKey = "";
        public string modelName = "gpt-3.5-turbo";
        [TextArea(3, 10)]
        public string systemPrompt = "Ти агент Unity, який генерує C# код, допомагає створювати сцени, ігрові об'єкти, компоненти, вирішує помилки та шукає корисні скрипти або ассети на GitHub. Відповідай лише корисним кодом або інструкцією.";
        [Range(0.0f, 1.0f)]
        public float temperature = 0.7f;

        [Header("Google Gemini")]
        public bool useGeminiAPI = false;
        public string geminiApiKey = "";
        public string geminiModelName = "gemini-pro";

        [Header("Anthropic Claude")]
        public bool useAnthropicClaudeAPI = false;
        public string anthropicApiKey = "";
        public string claudeModelName = "claude-3-haiku-20240307";
        
        [Header("Ollama (локальні моделі)")]
        public bool useOllamaAPI = false;
        public string ollamaEndpoint = "http://localhost:11434/api/generate"; 
        public string ollamaModelName = "llama3";
        
        [Header("Безкоштовний режим")] 
        public bool enableFreeModels = true;
        [Tooltip("Автоматично визначати доступні API ключі у середовищі")]
        public bool autoDetectAPIKeys = true;
        [Tooltip("Використовувати режим демонстрації, якщо немає доступних моделей")]
        public bool enableDemoMode = true;
        
        [Header("Доступні безкоштовні моделі")]
        [Tooltip("Локальна модель llama3 через Ollama")]
        public bool enableLocalLlama = true;
        [Tooltip("Вбудована мініатюрна модель для базової допомоги")]
        public bool enableFallbackEmbeddedModel = true;

        [Header("Загальні налаштування")]
        [Range(1000, 8000)]
        public int maxTokens = 4096; // Максимальна кількість токенів у відповіді

        [Header("Chat & Scripts")]
        [Range(5, 100)]
        public int maxHistoryLength = 50;
        public bool autoSaveScripts = true;
        public string scriptSavePath = "Scripts/AI_Generated";
        
        [Header("Кешування & Оптимізація")]
        public bool enableRequestCaching = true;
        [Range(1, 72)]
        public int cacheExpirationHours = 24;
        public bool useAutoRetry = true;
        
        /// <summary>
        /// Повертає найкращий доступний сервіс на основі налаштувань
        /// </summary>
        public string GetBestAvailableServiceName()
        {
            // Якщо є ключ OpenAI
            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                return "OpenAI";
            }
            
            // Якщо є ключ Gemini
            if (useGeminiAPI && !string.IsNullOrEmpty(geminiApiKey))
            {
                return "Google Gemini";
            }
            
            // Якщо є ключ Claude
            if (useAnthropicClaudeAPI && !string.IsNullOrEmpty(anthropicApiKey))
            {
                return "Anthropic Claude";
            }
            
            // Якщо доступний Ollama
            if (useOllamaAPI)
            {
                return "Ollama";
            }
            
            // Якщо увімкнено демо-режим
            if (enableDemoMode)
            {
                return "Demo";
            }
            
            return null;
        }
    }
}