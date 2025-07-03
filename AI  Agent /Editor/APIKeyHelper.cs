using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace UnityAIAgent
{
    /// <summary>
    /// Клас для допомоги з перевіркою та корекцією форматів API ключів
    /// </summary>
    public static class APIKeyHelper
    {
        /// <summary>
        /// Перевіряє та виправляє поширені проблеми з форматуванням ключа API Gemini
        /// </summary>
        /// <param name="apiKey">Оригінальний ключ API</param>
        /// <returns>Виправлений ключ API або оригінальний, якщо виправлення не потрібні</returns>
        public static string FixGeminiApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return apiKey;
            
            // Видалення зайвих пробілів на початку та в кінці
            string trimmedKey = apiKey.Trim();
            
            // Перевірка на ключ з лапками
            if (trimmedKey.StartsWith("\"") && trimmedKey.EndsWith("\""))
            {
                trimmedKey = trimmedKey.Substring(1, trimmedKey.Length - 2);
            }
            
            // Якщо ключ містить цитату з Makersuite
            var match = Regex.Match(trimmedKey, @"API key\s*:\s*([A-Za-z0-9\-_]+)");
            if (match.Success && match.Groups.Count > 1)
            {
                trimmedKey = match.Groups[1].Value;
            }
            
            return trimmedKey;
        }
        
        /// <summary>
        /// Аналізує ключ API Gemini та повертає інформацію про його формат
        /// </summary>
        /// <param name="apiKey">Ключ API для аналізу</param>
        /// <returns>Інформація про формат ключа</returns>
        public static (bool isValid, string format, string recommendation) AnalyzeGeminiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return (false, "Пустий ключ", "Введіть API ключ Gemini");
            
            string trimmedKey = apiKey.Trim();
            
            if (trimmedKey.Length < 10)
                return (false, "Занадто короткий", "API ключ повинен бути довшим за 10 символів");
                
            if (trimmedKey.StartsWith("AIza"))
                return (true, "Класичний Gemini", "Формат вірний");
                
            if (trimmedKey.StartsWith("g-"))
                return (true, "Новий формат Gemini", "Формат вірний");
            
            if (Regex.IsMatch(trimmedKey, @"^[A-Za-z0-9\-_]{30,}$"))
                return (true, "Невідомий формат", "Формат може бути вірним, перевірте роботу ключа");
                
            return (false, "Невірний формат", "Ключ Google Gemini зазвичай починається з 'AIza' або 'g-'");
        }
        
        /// <summary>
        /// Відображає інформацію про стан ключа API Gemini
        /// </summary>
        public static void ShowGeminiKeyStatus(string apiKey)
        {
            var (isValid, format, recommendation) = AnalyzeGeminiKey(apiKey);
            
            if (isValid)
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(0.2f, 0.8f, 0.2f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"✓ Формат ключа: {format}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.color = originalColor;
            }
            else
            {
                GUILayout.BeginHorizontal("box");
                EditorGUILayout.HelpBox($"⚠️ {recommendation}", MessageType.Warning);
                GUILayout.EndHorizontal();
            }
        }
    }
}
