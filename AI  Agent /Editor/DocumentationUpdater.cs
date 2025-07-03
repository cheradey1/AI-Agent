using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityAIAgent
{
    /// <summary>
    /// Клас для автоматичного оновлення документації
    /// </summary>
    public static class DocumentationUpdater
    {
        /// <summary>
        /// Оновлює документацію FreeModels.md з інформацією про нові моделі Gemini
        /// </summary>
        /// <param name="modelList">Список доступних моделей Gemini</param>
        public static void UpdateGeminiModelDocs(List<string> modelList)
        {
            try
            {
                // Шукаємо файл FreeModels.md в проекті
                string[] guids = AssetDatabase.FindAssets("FreeModels t:TextAsset");
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Файл FreeModels.md не знайдено у проекті");
                    return;
                }
                
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string fullPath = System.IO.Path.GetFullPath(assetPath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Файл не існує за шляхом: {fullPath}");
                    return;
                }
                
                // Читаємо вміст файлу
                string content = File.ReadAllText(fullPath);
                
                // Готуємо новий вміст розділу моделей
                StringBuilder modelListContent = new StringBuilder();
                modelListContent.AppendLine("### Доступні моделі:");
                
                // Створюємо опис для кожної моделі
                var modelDescriptions = new Dictionary<string, string>
                {
                    { "gemini-pro", "основна текстова модель, безкоштовна" },
                    { "gemini-pro-vision", "модель для аналізу зображень та тексту (потрібен інший API виклик)" },
                    { "gemini-1.5-pro", "нова покращена модель з підтримкою більших контекстів" },
                    { "gemini-1.5-flash", "швидша та економічніша модель, ідеальна для коротких запитів" },
                    { "gemini-1.5-pro-latest", "найновіша версія моделі з покращеною продуктивністю" }
                };
                
                // Додаємо всі підтверджені моделі з отриманого списку
                HashSet<string> addedModels = new HashSet<string>();
                foreach (var model in modelList)
                {
                    if (string.IsNullOrEmpty(model) || !model.StartsWith("gemini"))
                        continue;
                    
                    string description = "модель з сімейства Gemini";
                    if (modelDescriptions.ContainsKey(model))
                    {
                        description = modelDescriptions[model];
                    }
                    
                    modelListContent.AppendLine($"- `{model}` - {description}");
                    addedModels.Add(model);
                }
                
                // Додаємо моделі з опису, яких немає в отриманому списку
                foreach (var pair in modelDescriptions)
                {
                    if (!addedModels.Contains(pair.Key))
                    {
                        modelListContent.AppendLine($"- `{pair.Key}` - {pair.Value}");
                    }
                }
                
                // Шукаємо розділ з моделями Gemini
                string pattern = @"(### Доступні моделі:\r?\n)([\s\S]*?)(\r?\n### Примітки щодо безкоштовного використання:)";
                Regex regex = new Regex(pattern);
                
                if (regex.IsMatch(content))
                {
                    // Оновлюємо розділ з моделями
                    content = regex.Replace(content, 
                        $"$1{modelListContent.ToString().TrimEnd()}$3");
                    
                    // Оновлюємо дату актуальності
                    content = Regex.Replace(content, 
                        @"(## Автоматичне визначення API ключів та моделей\r?\n)", 
                        $"$1\n> Список моделей оновлено: {DateTime.Now.ToString("d MMMM yyyy")}\n\n");
                    
                    // Зберігаємо оновлений файл
                    File.WriteAllText(fullPath, content);
                    AssetDatabase.Refresh();
                    
                    Debug.Log($"Документацію FreeModels.md успішно оновлено з інформацією про {addedModels.Count} моделей Gemini");
                }
                else
                {
                    Debug.LogWarning("Не вдалося знайти розділ з моделями Gemini у файлі FreeModels.md");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при оновленні документації: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Додає новий ресурс до списку безкоштовних ресурсів Unity
        /// </summary>
        /// <param name="category">Категорія ресурсу (FPS, Platformer, Racing, тощо)</param>
        /// <param name="name">Назва ресурсу</param>
        /// <param name="description">Опис ресурсу</param>
        /// <param name="url">URL-адреса для завантаження ресурсу</param>
        public static bool AddResourceToFreeAssetsList(string category, string name, string description, string url)
        {
            try
            {
                // Шукаємо файл FreeUnityAssets.md в проекті
                string[] guids = AssetDatabase.FindAssets("FreeUnityAssets t:TextAsset");
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Файл FreeUnityAssets.md не знайдено у проекті");
                    return false;
                }
                
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string fullPath = System.IO.Path.GetFullPath(assetPath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Файл не існує за шляхом: {fullPath}");
                    return false;
                }
                
                // Читаємо вміст файлу
                string content = File.ReadAllText(fullPath);
                
                // Шукаємо потрібну категорію
                string categoryPattern = $@"##\s+[^#\n]*{Regex.Escape(category)}[^#\n]*\n(.*?)(?=##|\Z)";
                Match categoryMatch = Regex.Match(content, categoryPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                
                if (!categoryMatch.Success)
                {
                    Debug.LogWarning($"Категорію '{category}' не знайдено у файлі FreeUnityAssets.md");
                    return false;
                }
                
                // Отримуємо вміст категорії
                string categoryContent = categoryMatch.Value;
                
                // Перевіряємо, чи ресурс уже існує в категорії
                if (categoryContent.Contains(name) && categoryContent.Contains(url))
                {
                    Debug.Log($"Ресурс '{name}' вже існує у категорії '{category}'");
                    return false;
                }
                
                // Формуємо опис нового ресурсу у форматі Markdown
                string newResource = $"- [{name}]({url}) - {description}\n";
                
                // Знаходимо індекс, куди вставити новий ресурс (перед наступною секцією або в кінець)
                int insertIndex = categoryMatch.Index + categoryMatch.Length;
                
                // Оновлюємо вміст файлу, додаючи новий ресурс
                StringBuilder sb = new StringBuilder(content);
                
                // Перевіряємо, чи є в категорії підзаголовки
                Match subsectionMatch = Regex.Match(categoryContent, @"###\s+([^\n]+)");
                
                if (subsectionMatch.Success)
                {
                    // Додаємо до відповідної підкатегорії або створюємо нову
                    string lastSubsection = "";
                    int lastSubsectionIndex = 0;
                    
                    MatchCollection subsectionMatches = Regex.Matches(categoryContent, @"###\s+([^\n]+)");
                    foreach (Match subMatch in subsectionMatches)
                    {
                        string subsectionTitle = subMatch.Groups[1].Value.Trim();
                        // Якщо це "3D Моделі", "Шаблони ігор", etc. - відповідна підкатегорія для нашого ресурсу
                        if (subsectionTitle.Contains("3D") && url.Contains("3d") ||
                            subsectionTitle.Contains("Моделі") && (url.Contains("model") || url.Contains("asset")) ||
                            subsectionTitle.Contains("Шаблон") && (url.Contains("template") || url.Contains("project")) ||
                            subsectionTitle.Contains("Текстури") && url.Contains("texture"))
                        {
                            lastSubsection = subsectionTitle;
                            lastSubsectionIndex = categoryMatch.Index + subMatch.Index + subMatch.Length;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(lastSubsection))
                    {
                        // Додаємо до знайденої підкатегорії
                        sb.Insert(lastSubsectionIndex, newResource);
                    }
                    else
                    {
                        // Якщо не знайшли підходящу підкатегорію, додаємо в кінець категорії
                        sb.Insert(insertIndex, newResource);
                    }
                }
                else
                {
                    // Додаємо в кінець категорії, якщо немає підзаголовків
                    sb.Insert(insertIndex, newResource);
                }
                
                // Оновлюємо дату останньої зміни
                string datePattern = @"---\n\n>\s*\*\*Примітки\*\*:";
                string dateReplacement = $"---\n\n> Останнє оновлення: {DateTime.Now.ToString("d MMMM yyyy")}р.\n\n> **Примітки**:";
                
                string updatedContent = Regex.Replace(sb.ToString(), datePattern, dateReplacement);
                
                // Зберігаємо оновлений файл
                File.WriteAllText(fullPath, updatedContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"Ресурс '{name}' успішно додано до категорії '{category}' у файлі FreeUnityAssets.md");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при додаванні ресурсу до списку безкоштовних ресурсів: {ex.Message}");
                return false;
            }
        }
    }
}
