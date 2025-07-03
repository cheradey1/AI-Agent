using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.IO;

namespace UnityAIAgent
{
    /// <summary>
    /// Перевіряє наявність оновлень для пакету Unity AI Agent
    /// </summary>
    [InitializeOnLoad]
    public static class UpdateChecker
    {
        // Поточна версія пакету
        private const string CurrentVersion = "1.4.0";
        
        // URL для перевірки оновлень (наприклад, GitHub API для релізів)
        private const string UpdateCheckUrl = "https://api.github.com/repos/yourcompany/unityaichatagent/releases/latest";
        
        // Key for EditorPrefs to store the last check time
        private const string LastCheckTimeKey = "AIAgent_UpdateLastCheck";
        private static string lastCheckTimePath = System.IO.Path.Combine(Application.persistentDataPath, "lastCheckTime.dat");
        
        // Інтервал перевірки оновлень (1 день)
        private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);
        private static double checkIntervalHours = CheckInterval.TotalHours;
        
        static UpdateChecker()
        {
            // При завантаженні редактора перевіряємо наявність оновлень
            EditorApplication.delayCall += () =>
            {
                if (ShouldCheckForUpdates())
                {
                    CheckForUpdatesAsync();
                }
            };
        }
        
        /// <summary>
        /// Визначає, чи потрібно перевіряти наявність оновлень
        /// </summary>
        private static bool ShouldCheckForUpdates()
        {
            ReadLastCheckTime();
            
            TimeSpan timeSinceLastCheck = DateTime.Now - lastCheckTime;
            return timeSinceLastCheck.TotalHours >= checkIntervalHours;
        }
        
        private static DateTime lastCheckTime;
        
        private static void ReadLastCheckTime()
        {
            if (File.Exists(lastCheckTimePath))
            {
                try
                {
                    var fileContent = File.ReadAllBytes(lastCheckTimePath);
                    long storedTicks = BitConverter.ToInt64(fileContent, 0);
                    lastCheckTime = new DateTime(storedTicks);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to read last check time: {e.Message}");
                    lastCheckTime = DateTime.MinValue;
                }
            }
            else
            {
                lastCheckTime = DateTime.MinValue;
            }
        }
        
        /// <summary>
        /// Перевіряє наявність оновлень
        /// </summary>
        private static void CheckForUpdatesAsync()
        {
            // Зберігаємо час перевірки
            EditorPrefs.SetString(LastCheckTimeKey, DateTime.Now.ToBinary().ToString());
            
            try
            {
                // В реальному використанні тут був би запит до репозиторію
                // та перевірка доступності нової версії
                
                // Для прикладу просто показуємо повідомлення про актуальність
                Debug.Log($"[Unity AI Agent] Поточна версія: {CurrentVersion}");
                
                // Приклад асинхронної перевірки (закоментовано):
                /*
                Task.Run(async () => {
                    try {
                        string latestVersion = await GetLatestVersionAsync();
                        if (IsNewerVersion(latestVersion, CurrentVersion))
                        {
                            EditorApplication.delayCall += () => {
                                ShowUpdateNotification(latestVersion);
                            };
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogWarning($"[Unity AI Agent] Помилка перевірки оновлень: {ex.Message}");
                    }
                });
                */
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Unity AI Agent] Помилка перевірки оновлень: {e.Message}");
            }
        }
        
        /// <summary>
        /// Отримує останню версію пакету з репозиторію
        /// </summary>
        private static async Task<string> GetLatestVersionAsync()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(UpdateCheckUrl))
            {
                // Відправляємо запит і очікуємо завершення запиту
                await request.SendWebRequest();
                
                // Альтернативний підхід через цикл, якщо потрібно більше контролю
                // while (!request.isDone)
                // {
                //     await Task.Delay(100);
                // }
                
                // Перевіряємо помилки
                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception(request.error);
                }
                
                // Парсимо відповідь для отримання версії
                // Зазвичай тут буде парсинг JSON
                string responseText = request.downloadHandler.text;
                
                // Це спрощений код, на практиці тут буде повноцінний парсинг JSON
                // Наприклад: JObject json = JObject.Parse(responseText);
                // string version = json["tag_name"].ToString();
                
                return "1.4.0"; // Заглушка
            }
        }
        
        /// <summary>
        /// Порівнює версії для визначення, чи є доступна версія новішою
        /// </summary>
        private static bool IsNewerVersion(string available, string current)
        {
            try
            {
                // Розбиваємо версії на числові компоненти
                string[] availableParts = available.TrimStart('v').Split('.');
                string[] currentParts = current.TrimStart('v').Split('.');
                
                // Порівнюємо компоненти
                for (int i = 0; i < Math.Min(availableParts.Length, currentParts.Length); i++)
                {
                    int availablePart = int.Parse(availableParts[i]);
                    int currentPart = int.Parse(currentParts[i]);
                    
                    if (availablePart > currentPart)
                    {
                        return true;
                    }
                    else if (availablePart < currentPart)
                    {
                        return false;
                    }
                }
                
                // Якщо всі спільні компоненти однакові, вважаємо довшу версію новішою
                return availableParts.Length > currentParts.Length;
            }
            catch (Exception)
            {
                // У випадку помилки парсингу, вважаємо версії однаковими
                return false;
            }
        }
        
        /// <summary>
        /// Показує сповіщення про наявність оновлення
        /// </summary>
        private static void ShowUpdateNotification(string newVersion)
        {
            if (EditorUtility.DisplayDialog(
                "Доступне оновлення Unity AI Agent",
                $"Доступна нова версія Unity AI Agent: {newVersion}\nПоточна версія: {CurrentVersion}\n\nБажаєте перейти на сторінку оновлень?",
                "Перейти на сторінку оновлень",
                "Нагадати пізніше"))
            {
                // Відкриваємо сторінку з оновленням
                Application.OpenURL("https://github.com/yourcompany/unityaichatagent/releases/latest");
            }
        }
    }
}
