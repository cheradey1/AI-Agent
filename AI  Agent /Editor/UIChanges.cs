// Новий функціонал для UIChanges.cs
// Ці методи можна інтегрувати в AIAgentUnity.cs

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityAIAgent 
{
    // Це допоміжний клас, який містить нові методи для інтерфейсу
    public static class UIExtensions 
    {
        // Статичні поля для обраного прикладу
        private static StringValue s_SelectedExample;
        private static bool s_NeedsRefresh;
        
        /// <summary>
        /// Показує інформацію про API статус та відкриває налаштування (замість дублювання полів вводу ключів)
        /// </summary>
        public static void DrawApiKeysPanel(Rect panelRect, AIAgentSettings settings, SerializedObject serializedSettings, 
            ref string apiKeyInput, ref List<ChatMessage> chatHistory, IAIService currentService, AIServiceFactory serviceFactory)
        {
            GUILayout.BeginArea(panelRect);
            EditorGUILayout.LabelField("🔑 Сервіс AI", EditorStyles.boldLabel);

            // Вибір AI сервісу
            string[] serviceOptions = {"Auto (рекомендовано)", "OpenAI", "Anthropic Claude", "Google Gemini", "Ollama (локально)"};
            int selectedService = System.Array.IndexOf(serviceOptions, currentService?.GetServiceName() ?? "Auto (рекомендовано)");
            selectedService = EditorGUILayout.Popup("AI Сервіс:", selectedService >= 0 ? selectedService : 0, serviceOptions);

            EditorGUILayout.BeginVertical("box");
            
            // Відображаємо інформацію про поточний сервіс
            string serviceStatus = "Не активний";
            Color statusColor = Color.red;
            
            if (currentService != null)
            {
                if (currentService.IsConfigured())
                {
                    serviceStatus = $"Активний: {currentService.GetServiceName()}";
                    statusColor = Color.green;
                }
                else
                {
                    serviceStatus = $"Не налаштований: {currentService.GetServiceName()}";
                    statusColor = Color.yellow;
                }
            }
            
            // Відображаємо інформацію про ключі
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Статус AI: {serviceStatus}", statusStyle);
            
            if (settings?.openAIApiKey?.Length > 0)
                EditorGUILayout.LabelField("✓ OpenAI API ключ: налаштовано");
            else
                EditorGUILayout.LabelField("✗ OpenAI API ключ: відсутній");
                
            if (settings?.anthropicApiKey?.Length > 0)
                EditorGUILayout.LabelField("✓ Claude API ключ: налаштовано");
            else
                EditorGUILayout.LabelField("✗ Claude API ключ: відсутній");
                
            if (settings?.geminiApiKey?.Length > 0)
                EditorGUILayout.LabelField("✓ Gemini API ключ: налаштовано");
            else
                EditorGUILayout.LabelField("✗ Gemini API ключ: відсутній");
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("⚙️ Відкрити повні налаштування", GUILayout.Height(30)))
            {
                // Відкриваємо вікно з повними налаштуваннями
                AIAgentSettingsWindow.ShowWindow();
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Малює панель Asset Store / Marketplace (середня ліва частина)
        /// </summary>
        public static void DrawAssetStorePanel(Rect panelRect, ref string assetSearchQuery, ref List<string> assetSearchResults, ref Vector2 scrollPosition)
        {
            GUILayout.BeginArea(panelRect);
            EditorGUILayout.LabelField("🛒 Asset Store / Marketplace", EditorStyles.boldLabel);
            
            // Поле пошуку ассетів
            EditorGUILayout.BeginHorizontal();
            assetSearchQuery = EditorGUILayout.TextField(assetSearchQuery);
            if (GUILayout.Button("Пошук", GUILayout.Width(70)))
            {
                // Тут код для пошуку в Asset Store
                assetSearchResults.Clear();
                assetSearchResults.Add("⚠️ Інтеграція з Asset Store знаходиться в розробці");
                assetSearchResults.Add("⚠️ Тут будуть показані результати пошуку за запитом: " + assetSearchQuery);
                assetSearchResults.Add("⚠️ Додано для демонстрації UI");
            }
            EditorGUILayout.EndHorizontal();
            
            // Результати пошуку
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (assetSearchResults.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                foreach (var result in assetSearchResults)
                {
                    EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
            } 
            else 
            {
                EditorGUILayout.HelpBox("Введіть пошуковий запит для пошуку в Asset Store", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Малює панель GitHub (середня права частина)
        /// </summary>
        public static void DrawGitHubPanel(Rect panelRect, ref string githubSearchQuery, ref string githubRepoUrl, ref List<string> githubSearchResults, ref Vector2 scrollPosition)
        {
            GUILayout.BeginArea(panelRect);
            EditorGUILayout.LabelField("📂 GitHub Пошук", EditorStyles.boldLabel);
            
            // Поля для пошуку на GitHub
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Пошук:");
            githubSearchQuery = EditorGUILayout.TextField(githubSearchQuery);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Репозиторій:");
            githubRepoUrl = EditorGUILayout.TextField(githubRepoUrl);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Пошук на GitHub", GUILayout.Height(25)))
            {
                // Тут код для пошуку на GitHub
                githubSearchResults.Clear();
                githubSearchResults.Add("⚠️ Інтеграція з GitHub знаходиться в розробці");
                githubSearchResults.Add($"⚠️ Пошук за запитом '{githubSearchQuery}' у репозиторії: {githubRepoUrl}");
                githubSearchResults.Add("⚠️ Додано для демонстрації UI");
            }
            
            // Результати пошуку на GitHub
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (githubSearchResults.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                foreach (var result in githubSearchResults)
                {
                    EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
                    GUILayout.Space(5);
                }
                EditorGUILayout.EndVertical();
            }
            else 
            {
                EditorGUILayout.HelpBox("Введіть пошуковий запит та репозиторій для пошуку на GitHub", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Малює панель чату з голосовим управлінням (нижня частина)
        /// </summary>
        public static void DrawEnhancedChatPanel(Rect panelRect, ref string userInput, ref Vector2 scrollPosition, 
            ref List<ChatMessage> chatHistory, ref bool isWaitingForAIResponse, ref bool isSpeechToTextActive, ref bool isTextToSpeechEnabled, 
            System.Action<string, bool> sendMessageAction, System.Action clearChatAction)
        {
            GUILayout.BeginArea(panelRect);
            
            // Заголовок з кнопками голосового управління
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("💬 Чат", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            
            GUI.backgroundColor = isTextToSpeechEnabled ? Color.green : Color.white;
            if (GUILayout.Button("🔊 TTS " + (isTextToSpeechEnabled ? "ON" : "OFF"), GUILayout.Width(80)))
            {
                isTextToSpeechEnabled = !isTextToSpeechEnabled;
                chatHistory.Add(new ChatMessage("System", $"Озвучування відповідей {(isTextToSpeechEnabled ? "увімкнено" : "вимкнено")}"));
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // Історія повідомлень чату
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box");
            
            foreach (var message in chatHistory)
            {
                GUIStyle messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                messageStyle.richText = true;
                
                string prefix;
                
                if (message.Sender.ToLower() == "user")
                {
                    GUI.backgroundColor = new Color(0.8f, 0.9f, 1f); // Світло-блакитний для користувача
                    prefix = "<b>👤 Ви:</b> ";
                }
                else if (message.Sender.ToLower() == "ai" || message.Sender.ToLower() == "assistant")
                {
                    GUI.backgroundColor = new Color(0.9f, 1f, 0.9f); // Світло-зелений для AI
                    prefix = "<b>🤖 AI:</b> ";
                }
                else
                {
                    GUI.backgroundColor = new Color(0.95f, 0.95f, 0.95f); // Світло-сірий для системних повідомлень
                    prefix = "<b>⚙️ Система:</b> ";
                }
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(prefix + message.Text, messageStyle);
                EditorGUILayout.EndVertical();
                
                GUI.backgroundColor = Color.white; // Повертаємо стандартний колір
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            
            // Поле вводу і кнопки
            EditorGUILayout.BeginHorizontal();
            
            // Кнопка голосового вводу
            GUI.backgroundColor = isSpeechToTextActive ? Color.red : Color.white;
            if (GUILayout.Button(isSpeechToTextActive ? "🎙️ ⏹️" : "🎙️", GUILayout.Width(40), GUILayout.Height(40)))
            {
                isSpeechToTextActive = !isSpeechToTextActive;
                if (isSpeechToTextActive)
                {
                    chatHistory.Add(new ChatMessage("System", "Голосовий ввід активовано. Говоріть..."));
                    // Тут буде код для Speech-to-Text інтеграції
                }
                else
                {
                    chatHistory.Add(new ChatMessage("System", "Голосовий ввід зупинено"));
                }
            }
            GUI.backgroundColor = Color.white;
            
            // Текстове поле для вводу
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.fontSize = 14; // Збільшуємо розмір тексту
            textAreaStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            
            GUI.SetNextControlName("UserInputTextField");
            string newInput = EditorGUILayout.TextArea(userInput, textAreaStyle, GUILayout.Height(40));
            if (newInput != userInput)
            {
                userInput = newInput;
            }
            
            // Перевіряємо, чи вибрано приклад
            if (s_NeedsRefresh && s_SelectedExample != null)
            {
                userInput = s_SelectedExample.Value;
                s_SelectedExample = null;
                s_NeedsRefresh = false;
            }
            
            // Кнопка відправлення
            EditorGUI.BeginDisabledGroup(isWaitingForAIResponse || string.IsNullOrWhiteSpace(userInput));
            if (GUILayout.Button("✉️", GUILayout.Width(40), GUILayout.Height(40)) && !string.IsNullOrEmpty(userInput))
            {
                sendMessageAction?.Invoke(userInput, false);
                userInput = "";
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            // Додаткові кнопки
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Повторити"))
            {
                // Повторення останнього запиту користувача
                if (chatHistory.LastOrDefault(m => m.Sender == "User").Text != null)
                {
                    sendMessageAction?.Invoke(chatHistory.Last(m => m.Sender == "User").Text, true);
                }
            }
            if (GUILayout.Button("🧹 Очистити чат"))
            {
                clearChatAction?.Invoke();
            }
            if (GUILayout.Button("📝 Приклади") && !isWaitingForAIResponse)
            {
                // Меню з прикладами запитів
                GenericMenu menu = new GenericMenu();
                
                // Створюємо приклади запитів
                string seaBattleExample = "Створи базову сцену гри Морський бій з 3D об'єктами та простим меню";
                string characterControllerExample = "Напиши скрипт контролера для гравця в стилі FPS з підтримкою стрибків і прискорення";
                string inventoryExample = "Створи просту систему інвентарю з UI для рольової гри";
                
                // Зберігаємо посилання на поточного власника вікна
                EditorWindow currentWindow = EditorWindow.focusedWindow;
                
                // Додаємо приклади в меню
                menu.AddItem(new GUIContent("Створи сцену Морський бій"), false, () => {
                    // Призначаємо текст прикладу безпосередньо через s_SelectedExample
                    s_SelectedExample = new StringValue(seaBattleExample);
                    s_NeedsRefresh = true;
                    currentWindow.Repaint();
                });
                menu.AddItem(new GUIContent("Згенеруй скрипт руху персонажа"), false, () => {
                    s_SelectedExample = new StringValue(characterControllerExample);
                    s_NeedsRefresh = true;
                    currentWindow.Repaint();
                });
                menu.AddItem(new GUIContent("Створи систему інвентарю"), false, () => {
                    s_SelectedExample = new StringValue(inventoryExample);
                    s_NeedsRefresh = true;
                    currentWindow.Repaint();
                });
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Рядок стану (опціонально)
            if (isWaitingForAIResponse)
            {
                EditorGUILayout.HelpBox("AI обробляє запит...", MessageType.Info);
            }
            
            GUILayout.EndArea();
        }
        
        // Метод SetExample був видалений і замінений на UserInputContainer
    }
    
    /// <summary>
    /// Клас для передачі строкових даних між методами
    /// </summary>
    public class StringValue
    {
        public string Value { get; set; }
        
        public StringValue(string initialValue)
        {
            Value = initialValue;
        }
    }
}
