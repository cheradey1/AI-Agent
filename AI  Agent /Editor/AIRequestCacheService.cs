using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace UnityAIAgent
{
    /// <summary>
    /// Сервіс для кешування запитів до AI API для економії токенів
    /// </summary>
    public class AIRequestCacheService
    {
        private Dictionary<string, CacheEntry> _cache;
        private readonly string _cachePath;
        private readonly int _maxCacheItems;
        private readonly TimeSpan _cacheExpiration;
        private bool _isDirty = false;
        
        public class CacheEntry
        {
            public string ServiceName { get; set; }
            public string Response { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        /// <summary>
        /// Створює новий екземпляр сервісу кешування
        /// </summary>
        /// <param name="cacheFileName">Ім'я файлу для зберігання кешу</param>
        /// <param name="maxCacheItems">Максимальна кількість елементів у кеші</param>
        /// <param name="expirationHours">Час життя кешу у годинах</param>
        public AIRequestCacheService(string cacheFileName = "ai_request_cache.json", int maxCacheItems = 1000, int expirationHours = 48)
        {
            // Створюємо шлях для зберігання кешу у директорії проекту
            string cacheDirectory = System.IO.Path.Combine(Application.dataPath, "..", "AIAgentCache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            
            _cachePath = System.IO.Path.Combine(cacheDirectory, cacheFileName);
            _maxCacheItems = maxCacheItems;
            _cacheExpiration = TimeSpan.FromHours(expirationHours);
            
            LoadCache();
        }
        
        /// <summary>
        /// Завантажує кеш з диску
        /// </summary>
        private void LoadCache()
        {
            try
            {
                if (File.Exists(_cachePath))
                {
                    string json = File.ReadAllText(_cachePath);
                    _cache = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(json);
                    
                    // Видаляємо прострочені записи
                    CleanupExpiredEntries();
                }
                else
                {
                    _cache = new Dictionary<string, CacheEntry>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Помилка при завантаженні кешу запитів: {ex.Message}. Створено новий кеш.");
                _cache = new Dictionary<string, CacheEntry>();
            }
        }
        
        /// <summary>
        /// Зберігає кеш на диск
        /// </summary>
        public void SaveCache()
        {
            if (!_isDirty) return;
            
            try
            {
                string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(_cachePath, json);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при збереженні кешу запитів: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Видаляє прострочені записи з кешу
        /// </summary>
        private void CleanupExpiredEntries()
        {
            var expiredKeys = new List<string>();
            var now = DateTime.Now;
            
            foreach (var entry in _cache)
            {
                if (now - entry.Value.Timestamp > _cacheExpiration)
                {
                    expiredKeys.Add(entry.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
            
            if (expiredKeys.Count > 0)
            {
                _isDirty = true;
            }
        }
        
        /// <summary>
        /// Створює хеш-ключ для запиту
        /// </summary>
        private string CreateCacheKey(string serviceName, string prompt, string systemPrompt, List<string> chatHistory)
        {
            // Створюємо композитний ключ з параметрів запиту
            string key = $"{serviceName}_{systemPrompt}_{prompt}";
            
            if (chatHistory != null && chatHistory.Count > 0)
            {
                // Додаємо тільки останні 5 повідомлень з історії
                int start = Math.Max(0, chatHistory.Count - 5);
                for (int i = start; i < chatHistory.Count; i++)
                {
                    key += "_" + chatHistory[i];
                }
            }
            
            // Хешуємо ключ для скорочення довжини
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(key);
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
        
        /// <summary>
        /// Перевіряє, чи є відповідь у кеші
        /// </summary>
        public bool TryGetCachedResponse(string serviceName, string prompt, string systemPrompt, 
                                        List<string> chatHistory, out string cachedResponse)
        {
            cachedResponse = null;
            
            if (_cache == null) LoadCache();
            
            string key = CreateCacheKey(serviceName, prompt, systemPrompt, chatHistory);
            
            if (_cache.TryGetValue(key, out CacheEntry entry))
            {
                // Перевіряємо, чи не простроченим є запис
                if (DateTime.Now - entry.Timestamp <= _cacheExpiration)
                {
                    cachedResponse = entry.Response;
                    return true;
                }
                else
                {
                    // Запис прострочений, видаляємо його
                    _cache.Remove(key);
                    _isDirty = true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Зберігає відповідь у кеш
        /// </summary>
        public void CacheResponse(string serviceName, string prompt, string systemPrompt, 
                                 List<string> chatHistory, string response)
        {
            if (_cache == null) LoadCache();
            
            // Очищаємо кеш, якщо він занадто великий
            if (_cache.Count >= _maxCacheItems)
            {
                // Видаляємо 20% найстаріших записів
                int itemsToRemove = _maxCacheItems / 5;
                var orderedEntries = _cache.OrderBy(e => e.Value.Timestamp).Take(itemsToRemove).ToList();
                
                foreach (var entry in orderedEntries)
                {
                    _cache.Remove(entry.Key);
                }
                
                _isDirty = true;
            }
            
            string key = CreateCacheKey(serviceName, prompt, systemPrompt, chatHistory);
            
            _cache[key] = new CacheEntry
            {
                ServiceName = serviceName,
                Response = response,
                Timestamp = DateTime.Now
            };
            
            _isDirty = true;
            
            // Здійснюємо автоматичне збереження
            SaveCache();
        }
        
        /// <summary>
        /// Очищає весь кеш
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _isDirty = true;
            SaveCache();
        }
        
        /// <summary>
        /// Повертає статистику кешу
        /// </summary>
        public (int totalItems, int sizeInBytes) GetCacheStats()
        {
            int totalItems = _cache.Count;
            int sizeEstimation = 0;
            
            foreach (var entry in _cache)
            {
                // Приблизний розмір запису в байтах
                sizeEstimation += entry.Key.Length * 2; // UTF-16 символи
                sizeEstimation += entry.Value.Response.Length * 2;
                sizeEstimation += entry.Value.ServiceName.Length * 2;
                sizeEstimation += 8; // DateTime
                sizeEstimation += 16; // службова інформація
            }
            
            return (totalItems, sizeEstimation);
        }
    }
}
