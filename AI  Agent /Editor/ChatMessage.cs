using System;
using UnityEngine;

namespace UnityAIAgent
{
    /// <summary>
    /// Представляє повідомлення в чаті між користувачем та AI
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string Sender { get; private set; }
        public string Text { get; private set; }
        public DateTime Timestamp { get; private set; }
        
        /// <summary>
        /// Створює нове повідомлення в чаті
        /// </summary>
        /// <param name="sender">Відправник повідомлення (User, AI, System)</param>
        /// <param name="text">Текст повідомлення</param>
        public ChatMessage(string sender, string text)
        {
            Sender = sender;
            Text = text;
            Timestamp = DateTime.Now;
        }
        
        /// <summary>
        /// Повертає рядкове представлення повідомлення
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {Sender}: {Text}";
        }
        
        /// <summary>
        /// Створює повідомлення з рядка у форматі "Sender: Text"
        /// </summary>
        /// <param name="messageString">Рядок у форматі "Sender: Text"</param>
        /// <returns>Новий об'єкт ChatMessage</returns>
        public static ChatMessage FromString(string messageString)
        {
            string[] parts = messageString.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return new ChatMessage(parts[0], parts[1]);
            }
            
            // Якщо формат не відповідає очікуванням, створюємо системне повідомлення
            return new ChatMessage("System", messageString);
        }
    }
}
