using System;

namespace UnityAIAgent
{
    /// <summary>
    /// Клас для зберігання відповіді від AI сервісу
    /// </summary>
    public class AIResponse
    {
        /// <summary>
        /// Зміст відповіді
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// Чи успішно отримана відповідь
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Повідомлення про помилку (якщо є)
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Чи була відповідь отримана з кешу
        /// </summary>
        public bool IsCached { get; set; }
        
        /// <summary>
        /// Чи відповідь згенерована в демо-режимі
        /// </summary>
        public bool IsDemoMode { get; set; }
        
        /// <summary>
        /// Час, затрачений на запит (мілісекунди)
        /// </summary>
        public long ResponseTimeMs { get; set; }
        
        /// <summary>
        /// Назва використаної моделі
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Токенів використано у цьому запиті (якщо доступно)
        /// </summary>
        public int TokensUsed { get; set; }
        
        /// <summary>
        /// Джерело відповіді (API, локальна модель, демо, кеш)
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// Повертає форматовану відповідь з міткою про статус
        /// </summary>
        public string GetFormattedResponse()
        {
            string content = Content ?? string.Empty;
            
            // Не додаємо мітки, якщо це відповідь з помилкою
            if (!IsSuccess) return content;
            
            // Додаємо відповідну мітку
            if (IsDemoMode)
            {
                return $"{content}\n\n_[Відповідь згенерована в демо-режимі]_";
            }
            else if (IsCached)
            {
                return $"{content}\n\n_[Відповідь отримана з кешу]_";
            }
            
            return content;
        }
    }
}
