using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace UnityAIAgent
{
    /// <summary>
    /// Допоміжний клас для автоматичного створення налаштувань AI Agent
    /// </summary>
    public class AIAgentSettingsCreator
    {
        private const string SettingsPath = "Assets/Resources/AIAgentSettings.asset";
        private const string ResourcesFolder = "Assets/Resources";
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () => CheckAndCreateSettings();
        }
        
        [MenuItem("Window/AI Assistant/Create Settings")]
        public static AIAgentSettings CreateSettings()
        {
            return CheckAndCreateSettings(true);
        }
        
        /// <summary>
        /// Перевірка наявності та автоматичне створення налаштувань
        /// </summary>
        private static AIAgentSettings CheckAndCreateSettings(bool forceCreate = false)
        {
            // Шукаємо налаштування спочатку
            string[] guids = AssetDatabase.FindAssets("t:AIAgentSettings");
            if (guids.Length > 0 && !forceCreate)
            {
                // Налаштування вже існують
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AIAgentSettings>(path);
            }
            
            // Переконаємось, що папка Resources існує
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                string parentDir = System.IO.Path.GetDirectoryName(ResourcesFolder);
                string folderName = System.IO.Path.GetFileName(ResourcesFolder);
                AssetDatabase.CreateFolder(parentDir, folderName);
                Debug.Log($"Створено папку {ResourcesFolder}");
            }
            
            // Створюємо налаштування
            AIAgentSettings settings = ScriptableObject.CreateInstance<AIAgentSettings>();
            
            // Заповнюємо стандартні значення
            settings.systemPrompt = "Ти агент Unity, який генерує C# код, допомагає створювати сцени, ігрові об'єкти, компоненти, вирішує помилки та шукає корисні скрипти або ассети на GitHub та Asset Store.";
            
            // Налаштування для OpenAI
            settings.modelName = "gpt-3.5-turbo"; // Безкоштовна модель за замовчуванням
            
            // Автоматичне визначення API ключів з середовища
            TrySetAPIKeysFromEnvironment(settings);
            
            // Налаштування для Claude
            settings.claudeModelName = "claude-3-haiku-20240307"; // Найдоступніша модель Claude
            settings.useAnthropicClaudeAPI = !string.IsNullOrEmpty(settings.anthropicApiKey);
            
            // Налаштування для Gemini
            settings.geminiModelName = "gemini-pro";
            settings.useGeminiAPI = !string.IsNullOrEmpty(settings.geminiApiKey);
            
            // Налаштування для Ollama
            settings.ollamaModelName = "llama3";
            settings.ollamaEndpoint = "http://localhost:11434/api/generate";
            settings.useOllamaAPI = CheckOllamaAvailability();
            
            // Налаштування для безкоштовного режиму
            settings.enableFreeModels = true;
            settings.autoDetectAPIKeys = true;
            
            // Оптимізаційні налаштування
            settings.enableRequestCaching = true;
            settings.cacheExpirationHours = 24;
            settings.useAutoRetry = true;
            
            // Загальні налаштування
            settings.maxTokens = 4096;
            settings.maxHistoryLength = 50;
            settings.scriptSavePath = "Scripts/AI_Generated";
            
            // Зберігаємо налаштування
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"AIAgent: Створено налаштування за адресою {SettingsPath}");
            
            // Обираємо нові налаштування в редакторі
            Selection.activeObject = settings;
            
            return settings;
        }
        
        /// <summary>
        /// Перевірка доступності Ollama локально
        /// </summary>
        private static bool CheckOllamaAvailability()
        {
            try
            {
                // Спроба виявити Ollama через запуск процесу командної строки
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                
                // Налаштування процесу залежно від OS
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c curl -s http://localhost:11434/api/version";
                }
                else // macOS, Linux
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = "-c \"curl -s http://localhost:11434/api/version\"";
                }
                
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                // Якщо отримали JSON відповідь з версією - Ollama працює
                return output.Contains("version");
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Спроба автоматично отримати API ключі з середовища
        /// </summary>
        private static void TrySetAPIKeysFromEnvironment(AIAgentSettings settings)
        {
            // Перевіряємо, чи дозволено автоматичне виявлення ключів
            if (!settings.autoDetectAPIKeys)
            {
                Debug.Log("AIAgentSettings: автоматичне виявлення ключів API вимкнено в налаштуваннях.");
                return;
            }
            
            bool anyKeyFound = false;
            
            // Перевірка змінних середовища для OpenAI
            string[] openaiKeyEnvVars = {
                "OPENAI_API_KEY", 
                "OPENAI_KEY", 
                "AZURE_OPENAI_KEY"
            };
            
            foreach (var envVar in openaiKeyEnvVars)
            {
                string value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    settings.openAIApiKey = value;
                    Debug.Log($"AIAgentSettings: Знайдено ключ OpenAI API у змінній середовища {envVar}");
                    anyKeyFound = true;
                    break;
                }
            }
            
            // Перевірка змінних середовища для Google Gemini
            string[] geminiKeyEnvVars = {
                "GEMINI_API_KEY", 
                "GOOGLE_AI_API_KEY", 
                "GOOGLE_GEMINI_KEY"
            };
            
            foreach (var envVar in geminiKeyEnvVars)
            {
                string value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    settings.geminiApiKey = value;
                    settings.useGeminiAPI = true;
                    Debug.Log($"AIAgentSettings: Знайдено ключ Gemini API у змінній середовища {envVar}");
                    anyKeyFound = true;
                    break;
                }
            }
            
            // Перевірка змінних середовища для Claude
            string[] claudeKeyEnvVars = {
                "ANTHROPIC_API_KEY", 
                "CLAUDE_API_KEY", 
                "ANTHROPIC_KEY"
            };
            
            foreach (var envVar in claudeKeyEnvVars)
            {
                string value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    settings.anthropicApiKey = value;
                    settings.useAnthropicClaudeAPI = true;
                    Debug.Log($"AIAgentSettings: Знайдено ключ Claude API у змінній середовища {envVar}");
                    anyKeyFound = true;
                    break;
                }
            }
            
            // Перевірка наявності API ключів у файлах конфігурації
            if (!anyKeyFound)
            {
                TryFindApiKeysInConfigFiles(settings);
            }
        }
        
        /// <summary>
        /// Пошук API ключів у файлах конфігурації в домашній папці користувача
        /// </summary>
        private static void TryFindApiKeysInConfigFiles(AIAgentSettings settings)
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] possibleConfigFiles = {
                System.IO.Path.Combine(homeDir, ".openai"),
                System.IO.Path.Combine(homeDir, ".openai/api_key.txt"),
                System.IO.Path.Combine(homeDir, ".config/openai/api_key"),
                System.IO.Path.Combine(homeDir, ".anthropic/api_key.txt"),
                System.IO.Path.Combine(homeDir, ".config/anthropic/api_key"),
                System.IO.Path.Combine(homeDir, ".gemini_key"),
                System.IO.Path.Combine(homeDir, ".config/google/gemini_key")
            };
            
            foreach (string configPath in possibleConfigFiles)
            {
                if (File.Exists(configPath))
                {
                    try
                    {
                        string content = File.ReadAllText(configPath).Trim();
                        
                        // Визначаємо тип ключа за вмістом або назвою файлу
                        if (configPath.Contains("openai") && string.IsNullOrEmpty(settings.openAIApiKey))
                        {
                            if (content.StartsWith("sk-") || IsValidOpenAIKey(content))
                            {
                                settings.openAIApiKey = content;
                            }
                        }
                        else if ((configPath.Contains("anthropic") || configPath.Contains("claude")) && 
                                string.IsNullOrEmpty(settings.anthropicApiKey))
                        {
                            if (content.StartsWith("sk-ant-") || IsValidAnthropicKey(content))
                            {
                                settings.anthropicApiKey = content;
                            }
                        }
                        else if ((configPath.Contains("gemini") || configPath.Contains("google")) && 
                                string.IsNullOrEmpty(settings.geminiApiKey))
                        {
                            // Gemini ключі не мають стандартного префіксу
                            if (content.Length > 20)
                            {
                                settings.geminiApiKey = content;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Не вдалося прочитати конфігураційний файл {configPath}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Перевірка формату ключа OpenAI
        /// </summary>
        private static bool IsValidOpenAIKey(string key)
        {
            return Regex.IsMatch(key, @"^sk-[a-zA-Z0-9]{32,}$");
        }
        
        /// <summary>
        /// Перевірка формату ключа Claude
        /// </summary>
        private static bool IsValidAnthropicKey(string key)
        {
            return Regex.IsMatch(key, @"^sk-ant-[a-zA-Z0-9]{32,}$");
        }
        
        /// <summary>
        /// Отримання налаштувань з будь-якого місця в коді
        /// </summary>
        public static AIAgentSettings GetSettings(bool createIfMissing = true)
        {
            AIAgentSettings settings = null;
            bool settingsFoundOutsideResources = false;
            string existingSettingsPath = "";
            
            // Спочатку спробуємо завантажити через Resources
            settings = Resources.Load<AIAgentSettings>("AIAgentSettings");
            
            if (settings == null)
            {
                // Якщо не знайдено - шукаємо серед усіх асетів
                string[] guids = AssetDatabase.FindAssets("t:AIAgentSettings");
                if (guids.Length > 0)
                {
                    existingSettingsPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AIAgentSettings>(existingSettingsPath);
                    settingsFoundOutsideResources = true;
                }
                else if (createIfMissing)
                {
                    // Якщо не знайдено взагалі - створюємо
                    settings = CreateSettings();
                }
            }
            
            // Якщо знайдено налаштування не в Resources, переміщуємо їх туди
            if (settingsFoundOutsideResources && settings != null)
            {
                MoveSettingsToResources(existingSettingsPath);
            }
            
            // Оновлюємо автоматично знайдені API ключі, якщо увімкнена функція автовизначення
            if (settings != null && settings.autoDetectAPIKeys)
            {
                TrySetAPIKeysFromEnvironment(settings);
                if (settings.enableFreeModels && string.IsNullOrEmpty(settings.openAIApiKey))
                {
                    settings.useOllamaAPI = CheckOllamaAvailability();
                }
                
                // Перевіряємо, чи змінилися налаштування та зберігаємо їх
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            
            return settings;
        }
        
        /// <summary>
        /// Переміщення існуючих налаштувань в папку Resources
        /// </summary>
        private static void MoveSettingsToResources(string existingPath)
        {
            try
            {
                // Якщо налаштування вже є в Resources, не робимо нічого
                if (existingPath.Contains("/Resources/"))
                {
                    return;
                }
                
                // Переконаємось, що папка Resources існує
                if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                {
                    string parentDir = System.IO.Path.GetDirectoryName(ResourcesFolder);
                    string folderName = System.IO.Path.GetFileName(ResourcesFolder);
                    AssetDatabase.CreateFolder(parentDir, folderName);
                }
                
                // Копіюємо налаштування
                string targetPath = SettingsPath;
                bool success = AssetDatabase.CopyAsset(existingPath, targetPath);
                
                if (success)
                {
                    Debug.Log($"AIAgent: Налаштування скопійовано з '{existingPath}' до '{targetPath}'");
                    // Не видаляємо оригінальний файл, щоб не порушити посилання
                }
                else
                {
                    Debug.LogError($"AIAgent: Не вдалося скопіювати налаштування з '{existingPath}' до '{targetPath}'");
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AIAgent: Помилка при переміщенні налаштувань: {ex.Message}");
            }
        }
    }
}
