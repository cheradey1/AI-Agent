using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine; // For Debug.LogError, Debug.LogWarning

namespace UnityAIAgent
{
    public class AIServiceFactory
    {
        private readonly AIAgentSettings _settings;
        private Dictionary<string, IAIService> _serviceInstances = new Dictionary<string, IAIService>();
        private AIRequestCacheService _cacheService;
        private bool _ollamaAvailabilityChecked = false;
        private bool _ollamaAvailable = false;

        public AIServiceFactory(AIAgentSettings settings)
        {
            _settings = settings;
            _cacheService = new AIRequestCacheService();
        }
        
        /// <summary>
        /// Повертає сервіс кешування запитів
        /// </summary>
        public AIRequestCacheService GetCacheService() => _cacheService;

        /// <summary>
        /// Створює та повертає екземпляр сервісу AI
        /// </summary>
        public IAIService CreateService(string serviceName)
        {
            if (_settings == null) 
            {
                Debug.LogError("AIServiceFactory: AIAgentSettings is null. Cannot create service.");
                return null;
            }
            
            // Якщо serviceName не вказано, використовуємо найкращий доступний сервіс
            if (string.IsNullOrEmpty(serviceName))
            {
                serviceName = _settings.GetBestAvailableServiceName();
                
                // Якщо немає жодного сервісу та увімкнено демо-режим
                if (string.IsNullOrEmpty(serviceName) && _settings.enableDemoMode)
                {
                    serviceName = "Demo";
                }
                
                if (string.IsNullOrEmpty(serviceName))
                {
                    Debug.LogError("AIServiceFactory: No AI service available. Please configure at least one service.");
                    return null;
                }
            }
            
            // Автоматично налаштовуємо Ollama, якщо він доступний і увімкнено безкоштовні моделі
            if (_settings.enableFreeModels && !_ollamaAvailabilityChecked && 
                (serviceName.ToLower() == "ollama" || serviceName.ToLower() == "auto"))
            {
                _ollamaAvailabilityChecked = true;
                CheckOllamaAvailabilityAsync().ContinueWith(task => {
                    _ollamaAvailable = task.Result;
                    if (_ollamaAvailable && !_settings.useOllamaAPI)
                    {
                        _settings.useOllamaAPI = true;
                        Debug.Log("Виявлено локальний сервіс Ollama. Увімкнено режим безкоштовних моделей.");
                    }
                });
            }
            
            // Перевіряємо, чи вже є створений екземпляр сервісу в кеші
            if (_serviceInstances.TryGetValue(serviceName?.ToLower() ?? "", out IAIService cachedService))
            {
                return cachedService;
            }

            IAIService service = null;
            switch (serviceName?.ToLower())
            {
                case "openai":
                    service = new OpenAIService(_settings);
                    break;
                
                case "google gemini": 
                     if(_settings.useGeminiAPI || (_settings.autoDetectAPIKeys && !string.IsNullOrEmpty(_settings.geminiApiKey)))
                     {
                         _settings.useGeminiAPI = true;
                         service = new GeminiService(_settings);
                     }
                     else 
                     {
                        Debug.LogWarning("Google Gemini service selected but API key is not configured.");
                        return GetFallbackService(); 
                     }
                    break;
                
                case "anthropic claude": 
                     if(_settings.useAnthropicClaudeAPI || (_settings.autoDetectAPIKeys && !string.IsNullOrEmpty(_settings.anthropicApiKey)))
                     {
                         _settings.useAnthropicClaudeAPI = true;
                         service = new AnthropicService(_settings);
                     }
                     else 
                     {
                        Debug.LogWarning("Anthropic Claude service selected but API key is not configured.");
                        return GetFallbackService();
                     }
                    break;
                
                case "ollama": 
                     if(_settings.useOllamaAPI || (_settings.enableLocalLlama && _ollamaAvailable))
                     {
                         _settings.useOllamaAPI = true;
                         service = new OllamaService(_settings);
                     }
                     else 
                     {
                        Debug.LogWarning("Ollama service selected but it's not available or not enabled.");
                        if (_settings.enableFreeModels)
                        {
                            Debug.Log("Trying other free models...");
                            return GetFallbackService();
                        }
                        return null;
                     }
                    break;
                
                case "demo":
                    if (_settings.enableDemoMode)
                    {
                        service = new DemoModeService(_settings);
                    }
                    else 
                    {
                        Debug.LogWarning("Demo mode is disabled in settings.");
                        return null;
                    }
                    break;
                
                case "auto":
                    return CreateBestAvailableService();
                
                default:
                    Debug.LogWarning($"Unknown AI service type: '{serviceName}'. Cannot create service. Available types depend on configuration.");
                    return null;
            }
            
            // Зберігаємо екземпляр сервісу для повторного використання
            if (service != null)
            {
                _serviceInstances[serviceName.ToLower()] = service;
            }
            
            return service;
        }

        /// <summary>
        /// Створює службу з найкращим доступним API
        /// </summary>
        private IAIService CreateBestAvailableService()
        {
            // Спробуємо спочатку локальні моделі
            if (_settings.enableLocalLlama && (_settings.useOllamaAPI || _ollamaAvailable))
            {
                var ollamaService = new OllamaService(_settings);
                if (ollamaService.IsConfigured())
                {
                    _settings.useOllamaAPI = true;
                    _serviceInstances["ollama"] = ollamaService;
                    return ollamaService;
                }
            }
            
            // Перевіряємо OpenAI
            if (!string.IsNullOrEmpty(_settings.openAIApiKey))
            {
                var openAiService = new OpenAIService(_settings);
                _serviceInstances["openai"] = openAiService;
                return openAiService;
            }
            
            // Перевіряємо Gemini
            if (_settings.autoDetectAPIKeys && !string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                _settings.useGeminiAPI = true;
                var geminiService = new GeminiService(_settings);
                _serviceInstances["google gemini"] = geminiService;
                return geminiService;
            }
            
            // Перевіряємо Claude
            if (_settings.autoDetectAPIKeys && !string.IsNullOrEmpty(_settings.anthropicApiKey))
            {
                _settings.useAnthropicClaudeAPI = true;
                var claudeService = new AnthropicService(_settings);
                _serviceInstances["anthropic claude"] = claudeService;
                return claudeService;
            }
            
            // Якщо нічого не доступно і увімкнено демо-режим
            if (_settings.enableDemoMode)
            {
                var demoService = new DemoModeService(_settings);
                _serviceInstances["demo"] = demoService;
                return demoService;
            }
            
            Debug.LogError("Не знайдено доступних AI сервісів. Будь ласка, налаштуйте хоча б один сервіс.");
            return null;
        }
        
        /// <summary>
        /// Повертає запасний сервіс
        /// </summary>
        private IAIService GetFallbackService()
        {
            // Якщо доступний Ollama
            if (_settings.enableFreeModels && (_settings.useOllamaAPI || _ollamaAvailable))
            {
                _settings.useOllamaAPI = true;
                var service = new OllamaService(_settings);
                _serviceInstances["ollama"] = service;
                return service;
            }
            
            // Якщо включено демо-режим
            if (_settings.enableDemoMode)
            {
                var service = new DemoModeService(_settings);
                _serviceInstances["demo"] = service;
                return service;
            }
            
            // Спробуємо знайти будь-який інший налаштований сервіс
            return CreateBestAvailableService();
        }

        /// <summary>
        /// Повертає список назв доступних сервісів
        /// </summary>
        public List<string> GetAvailableServices()
        {
            var services = new List<string>();
            if (_settings == null) 
            {
                Debug.LogWarning("AIServiceFactory: AIAgentSettings is null. Cannot determine available services.");
                return services;
            }

            // Завжди додаємо автоматичний вибір першим, якщо увімкнено безкоштовні моделі
            if (_settings.enableFreeModels)
            {
                services.Add("Auto");
            }
            
            // Перевіряємо OpenAI
            if (!string.IsNullOrEmpty(_settings.openAIApiKey))
            {
                services.Add("OpenAI");
            }

            // Перевіряємо Google Gemini
            if ((_settings.useGeminiAPI || _settings.autoDetectAPIKeys) && !string.IsNullOrEmpty(_settings.geminiApiKey))
            {
                services.Add("Google Gemini");
            }

            // Перевіряємо Anthropic Claude
            if ((_settings.useAnthropicClaudeAPI || _settings.autoDetectAPIKeys) && !string.IsNullOrEmpty(_settings.anthropicApiKey))
            {
                services.Add("Anthropic Claude");
            }
            
            // Перевіряємо Ollama
            if (_settings.useOllamaAPI || (_settings.enableFreeModels && _ollamaAvailable))
            {
                services.Add("Ollama");
            }
            
            // Додаємо демо-режим, якщо він увімкнений
            if (_settings.enableDemoMode)
            {
                services.Add("Demo");
            }

            if (services.Count == 0)
            {
                Debug.LogWarning("Не знайдено налаштованих AI сервісів.");
                
                // Запускаємо асинхронну перевірку доступних сервісів
                if (_settings.enableFreeModels && !_ollamaAvailabilityChecked)
                {
                    CheckOllamaAvailabilityAsync().ContinueWith(task => {
                        if (task.Result)
                        {
                            _settings.useOllamaAPI = true;
                            Debug.Log("Знайдено локальний Ollama сервіс. Активовано.");
                        }
                    });
                }
            }
            
            return services;
        }
        
        /// <summary>
        /// Асинхронно перевіряє доступність сервісу Ollama
        /// </summary>
        public async Task<bool> CheckOllamaAvailabilityAsync()
        {
            if (_ollamaAvailabilityChecked)
            {
                return _ollamaAvailable;
            }
            
            if (_settings?.enableLocalLlama != true)
            {
                _ollamaAvailabilityChecked = true;
                _ollamaAvailable = false;
                return false;
            }
            
            // Створюємо сервіс Ollama для перевірки
            var ollamaService = new OllamaService(_settings);
            _ollamaAvailabilityChecked = true;
            _ollamaAvailable = await ollamaService.IsServerRunning();
            
            return _ollamaAvailable;
        }
        
        /// <summary>
        /// Отримує список доступних моделей для вказаного сервісу
        /// </summary>
        public async Task<List<string>> GetAvailableModelsForService(string serviceName)
        {
            var service = CreateService(serviceName);
            var models = new List<string>();
            
            if (service == null)
            {
                Debug.LogWarning($"Failed to create service for {serviceName} to get available models");
                return models;
            }
            
            try
            {
                if (serviceName.ToLower() == "ollama")
                {
                    var ollamaService = service as OllamaService;
                    if (ollamaService != null)
                    {
                        return await ollamaService.GetAvailableModels(true);
                    }
                }
                else if (serviceName.ToLower() == "google gemini")
                {
                    var geminiService = service as GeminiService;
                    if (geminiService != null)
                    {
                        return await geminiService.GetAvailableModels(true);
                    }
                }
                // Стандартні моделі для інших сервісів
                else if (serviceName.ToLower() == "openai")
                {
                    return new List<string> { 
                        "gpt-3.5-turbo", 
                        "gpt-4", 
                        "gpt-4-turbo",
                        "gpt-3.5-turbo-0125", 
                        "gpt-4-vision-preview" 
                    };
                }
                else if (serviceName.ToLower() == "anthropic claude")
                {
                    return new List<string> { 
                        "claude-3-haiku-20240307", 
                        "claude-3-sonnet-20240229",
                        "claude-3-opus-20240229"
                    };
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error getting models for {serviceName}: {ex.Message}");
            }
            
            return models;
        }
    }
}
