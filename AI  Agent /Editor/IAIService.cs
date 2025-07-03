using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityAIAgent
{
    public interface IAIService
    {
        Task<AIResponse> QueryAI(string prompt, List<string> chatHistory);
        string GetServiceName();
        bool IsConfigured();
        
        /// <summary>
        /// Виконує запит до AI з можливістю кешування результатів
        /// </summary>
        /// <param name="prompt">Текст запиту</param>
        /// <param name="chatHistory">Історія чату</param>
        /// <param name="useCache">Чи використовувати кеш</param>
        /// <param name="cacheService">Сервіс кешування запитів (якщо використовується)</param>
        /// <param name="systemPrompt">Системний промпт</param>
        /// <returns>Відповідь AI</returns>
        async Task<AIResponse> QueryAIWithCache(string prompt, List<string> chatHistory, bool useCache, 
                                                AIRequestCacheService cacheService, string systemPrompt)
        {
            // Якщо кешування не використовується або сервіс не налаштований
            if (!useCache || cacheService == null)
            {
                return await QueryAI(prompt, chatHistory);
            }
            
            // Перевіряємо наявність відповіді в кеші
            if (cacheService.TryGetCachedResponse(GetServiceName(), prompt, systemPrompt, chatHistory, out string cachedResponse))
            {
                return new AIResponse { Content = cachedResponse, IsSuccess = true, IsCached = true };
            }
            
            // Якщо відповіді немає в кеші, виконуємо запит до API
            var response = await QueryAI(prompt, chatHistory);
            
            // Якщо запит успішний, зберігаємо відповідь у кеш
            if (response.IsSuccess)
            {
                cacheService.CacheResponse(GetServiceName(), prompt, systemPrompt, chatHistory, response.Content);
            }
            
            return response;
        }
    }
}
