using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityAIAgent
{
    /// <summary>
    /// Допоміжний клас для повторних спроб виконання операцій з API при виникненні помилок
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Виконує операцію з можливістю повторних спроб при невдачі
        /// </summary>
        /// <typeparam name="T">Тип значення, яке повертається</typeparam>
        /// <param name="operation">Функція, яка буде виконана</param>
        /// <param name="retryCount">Максимальна кількість спроб (не включаючи першу)</param>
        /// <param name="delayMilliseconds">Затримка між спробами в мілісекундах</param>
        /// <param name="isRetryableError">Функція для перевірки, чи варто повторювати спробу при цій помилці</param>
        /// <returns>Результат операції</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation, 
            int retryCount = 3, 
            int delayMilliseconds = 1000,
            Func<Exception, bool> isRetryableError = null)
        {
            int currentRetry = 0;
            
            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    
                    // Якщо перевищена максимальна кількість спроб або помилка не потребує повторної спроби
                    bool shouldRetry = currentRetry <= retryCount && 
                                     (isRetryableError == null || isRetryableError(ex));
                    
                    if (!shouldRetry)
                    {
                        Debug.LogError($"Операція не вдалася після {currentRetry} спроб: {ex.Message}");
                        throw;
                    }
                    
                    Debug.LogWarning($"Спроба {currentRetry}/{retryCount} не вдалася: {ex.Message}. Повторна спроба через {delayMilliseconds} мс.");
                    await Task.Delay(delayMilliseconds);
                }
            }
        }
    }
}
