using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityAIAgent
{
    /// <summary>
    /// Новий спрощений інтерфейс для AIAgentUnity
    /// </summary>
    public static class SimplifiedChatUI
    {
        // Відступи для елементів інтерфейсу
        private const float PADDING = 10f;
        private const float BUTTON_HEIGHT = 30f;
        private const float SPECIAL_BUTTON_HEIGHT = 40f;
        
        /// <summary>
        /// Малює спрощений інтерфейс чату з основними функціями (з діагностичними логами)
        /// </summary>
        public static void DrawSimplifiedChatInterface(
            Rect windowRect,
            ref string userInput, 
            ref Vector2 chatScrollPosition, 
            ref List<ChatMessage> chatHistory, 
            ref bool isWaitingForAIResponse, 
            bool isTextToSpeechEnabled,
            System.Action<string, bool> sendMessageAction, 
            System.Action clearChatAction,
            System.Action openSettingsAction,
            System.Action<string> saveMessageAction = null,
            bool showAPITroubleshooterButton = false)
        {
            // Додаємо логи для відлагодження
            if (openSettingsAction == null)
                Debug.LogWarning("SimplifiedChatUI: openSettingsAction is null");
            
            if (clearChatAction == null)
                Debug.LogWarning("SimplifiedChatUI: clearChatAction is null");
                
            if (saveMessageAction == null)
                Debug.Log("SimplifiedChatUI: saveMessageAction is null (це нормально, якщо функція збереження не потрібна)");
            
            bool verticalStarted = false;
            bool horizontalStarted = false;
            
            try
            {
                // Основна область
                EditorGUILayout.BeginVertical();
                verticalStarted = true;
                
                // Верхня панель з кнопками
                DrawTopButtonsPanel(openSettingsAction, clearChatAction);
                
                // Панель чату (займає більшу частину вікна)
                DrawChatHistoryPanel(ref chatScrollPosition, chatHistory, saveMessageAction);
                
                // Панель вводу повідомлення
                DrawInputPanel(ref userInput, isWaitingForAIResponse, sendMessageAction);
                
                // Статусний рядок (якщо очікуємо відповідь)
                if (isWaitingForAIResponse)
                {
                    EditorGUILayout.BeginHorizontal();
                    horizontalStarted = true;
                    
                    ProgressIndicator.DrawSpinner(20f);
                    ProgressIndicator.DrawTextProgress("AI обробляє запит");
                    
                    EditorGUILayout.EndHorizontal();
                    horizontalStarted = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка під час відображення інтерфейсу: {ex.Message}\n{ex.StackTrace}");
                EditorGUILayout.HelpBox($"Сталася помилка: {ex.Message}", MessageType.Error);
            }
            finally
            {
                // Гарантуємо, що всі групи закриті навіть у випадку помилок
                if (horizontalStarted)
                {
                    try { EditorGUILayout.EndHorizontal(); } catch (Exception) { }
                }
                
                if (verticalStarted)
                {
                    try { EditorGUILayout.EndVertical(); } catch (Exception) { }
                }
            }
        }
        
        /// <summary>
        /// Малює верхню панель з кнопками керування
        /// </summary>
        private static void DrawTopButtonsPanel(System.Action openSettingsAction, System.Action clearChatAction = null)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Кнопка очищення чату
            if (clearChatAction != null)
            {
                if (GUILayout.Button("🗑️ Очистити чат", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    try
                    {
                        if (EditorUtility.DisplayDialog("Підтвердження", 
                            "Ви впевнені, що хочете очистити історію чату?", 
                            "Так", "Скасувати"))
                        {
                            clearChatAction.Invoke();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Помилка при очищенні чату: {ex.Message}");
                    }
                }
            }
            
            // Кнопка налаштувань (API/settings)
            if (openSettingsAction != null)
            {
                if (GUILayout.Button(new GUIContent("⚙️", "Відкрити налаштування API/AI Agent"), EditorStyles.toolbarButton, GUILayout.Width(32)))
                {
                    openSettingsAction.Invoke();
                }
            }

            GUILayout.FlexibleSpace();
            
            // Кнопка налаштувань була видалена - тепер використовується тільки нижня кнопка з інформаційної панелі
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Малює історію повідомлень чату
        /// </summary>
        private static void DrawChatHistoryPanel(ref Vector2 scrollPosition, List<ChatMessage> chatHistory, System.Action<string> saveMessageAction = null)
        {
            // Історія повідомлень чату
            bool shouldScrollToBottom = false;
            float viewHeight = 0;
            float scrollViewHeight = 0;
            bool scrollViewStarted = false;
            bool verticalStarted = false;
            
            // Зберігаємо поточну позицію прокрутки
            float previousScrollY = scrollPosition.y;
            
            // Визначаємо, чи потрібно прокручувати до останнього повідомлення
            if (Event.current.type == EventType.Layout)
            {
                // Прокручуємо вниз, якщо користувач знаходиться в кінці скролу
                shouldScrollToBottom = (scrollPosition.y >= viewHeight - scrollViewHeight - 50) || 
                                       (scrollViewHeight <= viewHeight);
            }
            
            try
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
                scrollViewStarted = true;
                
                EditorGUILayout.BeginVertical("box");
                verticalStarted = true;
            
            foreach (var message in chatHistory)
            {
                GUIStyle messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                messageStyle.richText = true;
                messageStyle.margin = new RectOffset(5, 5, 5, 5);
                
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
                
                // Форматуємо текст повідомлення з підсвіткою коду, якщо це повідомлення AI
                string displayText = message.Text;
                if (message.Sender.ToLower() == "ai" || message.Sender.ToLower() == "assistant")
                {
                    displayText = FormatMessageWithCodeHighlighting(message.Text);
                }
                
                EditorGUILayout.LabelField(prefix + displayText, messageStyle);
                
                // Кнопки контексту повідомлення
                if (saveMessageAction != null)
                {
                    DrawMessageContextButtons(message, saveMessageAction);
                }
                else if (message.Sender.ToLower() == "ai" || message.Sender.ToLower() == "assistant")
                {
                    // Якщо saveMessageAction == null, але це повідомлення від AI, показуємо тільки кнопку копіювання
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    
                    // Кнопка для копіювання тексту
                    if (GUILayout.Button(new GUIContent("📋", "Копіювати текст"), 
                        GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        EditorGUIUtility.systemCopyBuffer = message.Text;
                        Debug.Log("Текст скопійовано у буфер обміну");
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Якщо повідомлення містить помилку API, додаємо кнопку для усунення проблем
                    if (message.Text.Contains("Помилка:") && (
                        message.Text.Contains("API key") || 
                        message.Text.Contains("API_KEY") ||
                        message.Text.Contains("ключ") ||
                        message.Text.Contains("quota") ||
                        message.Text.Contains("доступ")))
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        
                        // Створюємо кнопку для відкриття вікна усунення проблем
                        GUI.backgroundColor = new Color(1f, 0.8f, 0.2f); // Оранжевий колір для кнопки
                        if (GUILayout.Button("🛠️ Усунення проблем з API", GUILayout.Height(24)))
                        {
                            APITroubleshooterWindow.ShowWindow();
                        }
                        GUI.backgroundColor = Color.white;
                        
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                GUI.backgroundColor = Color.white; // Повертаємо стандартний колір
            }
            
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при відображенні історії чату: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Гарантуємо закриття вертикальної групи
                if (verticalStarted)
                {
                    try { EditorGUILayout.EndVertical(); } catch { }
                }
                
                // Оновлюємо інформацію про розмір контенту
                if (Event.current.type == EventType.Repaint)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    viewHeight = lastRect.height;
                    scrollViewHeight = EditorGUIUtility.currentViewWidth; // Використовуємо доступну ширину як приблизне значення
                    
                    // Якщо потрібно, прокручуємо до останнього повідомлення
                    if (shouldScrollToBottom || chatHistory.Count <= 1)
                    {
                        scrollPosition.y = viewHeight;
                    }
                }
                
                // Гарантуємо закриття прокрутки
                if (scrollViewStarted)
                {
                    try { EditorGUILayout.EndScrollView(); } catch { }
                }
            }
        }
        
        /// <summary>
        /// Малює панель вводу повідомлення
        /// </summary>
        private static void DrawInputPanel(ref string userInput, bool isWaitingForAIResponse, System.Action<string, bool> sendMessageAction)
        {
            bool horizontalStarted = false;
            bool disabledGroupStarted = false;
            
            try
            {
                EditorGUILayout.BeginHorizontal();
                horizontalStarted = true;
                
                // Кнопка "Голосовий чат" зліва від поля вводу
                GUIStyle voiceButtonStyle = new GUIStyle(GUI.skin.button);
                voiceButtonStyle.fontSize = 12;
                voiceButtonStyle.alignment = TextAnchor.MiddleCenter;
                
                // Отримання посилання на AIAgentUnity для управління голосовим режимом
                UnityAIAgent.AIAgentUnity aiAgent = EditorWindow.GetWindow<UnityAIAgent.AIAgentUnity>("AI Agent");
                bool isSpeechActive = aiAgent != null ? aiAgent.IsSpeechToTextActive : false;
                
                // Змінюємо колір і стиль кнопки залежно від стану
                Color buttonColor;
                GUIStyle recordingStyle = new GUIStyle(voiceButtonStyle);
                
                if (isSpeechActive) {
                    // Пульсуючий ефект під час запису
                    float pulseValue = Mathf.PingPong((float)EditorApplication.timeSinceStartup * 2.5f, 1f);
                    buttonColor = Color.Lerp(Color.red, new Color(1f, 0.5f, 0.5f), pulseValue);
                    
                    // Для активного запису додаємо жирний шрифт і чіткішу анімацію
                    recordingStyle.fontStyle = FontStyle.Bold;
                    GUI.backgroundColor = Color.Lerp(new Color(1f, 0.8f, 0.8f), new Color(1f, 0.4f, 0.4f), pulseValue);
                } else {
                    // Звичайний колір в неактивному стані
                    buttonColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.6f, 1f) : new Color(0.1f, 0.5f, 0.9f);
                }
                
                recordingStyle.normal.textColor = buttonColor;
                
                // Використовуємо створений стиль для кнопки
                voiceButtonStyle = recordingStyle;
                
                // Додаємо анімацію для індикації запису
                string buttonText;
                if (isSpeechActive)
                {
                    // Отримуємо дані про запис з AI Agent
                    float recordingStartTime = aiAgent != null ? aiAgent.RecordingStartTime : (float)EditorApplication.timeSinceStartup;
                    float recordingDuration = (float)EditorApplication.timeSinceStartup - recordingStartTime;
                    
                    // Анімація індикатора з покращеним відображенням
                    float time = (float)(EditorApplication.timeSinceStartup % 1.0);
                    string animDots = time < 0.25f ? "•" : (time < 0.5f ? "••" : (time < 0.75f ? "•••" : ""));
                    
                    // Максимальний час запису в секундах
                    int maxRecordTime = 10; // Має відповідати константі MAX_RECORDING_TIME_SEC в AIAgentUnity
                    
                    // Обчислюємо відсоток виконання для індикатора прогресу
                    int progressPercentage = (int)Mathf.Min(100, (recordingDuration / maxRecordTime) * 100);
                    
                    // Форматуємо тривалість запису та індикатор прогресу
                    int seconds = (int)recordingDuration;
                    int remainingTime = maxRecordTime - seconds;
                    
                    // Формуємо текст з візуальним індикатором прогресу
                    string progressBar = "";
                    for (int i = 0; i < 10; i++) {
                        if (i < progressPercentage / 10) {
                            progressBar += "◼";
                        } else {
                            progressBar += "◻";
                        }
                    }
                    
                    buttonText = $"◉ {seconds}с\n{progressBar}";
                    
                    // Перемальовуємо інтерфейс для анімації
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (aiAgent != null)
                        aiAgent.Repaint();
                }
                else
                {
                    buttonText = "🎤"; // Лише іконка мікрофона
                }
                
                if (GUILayout.Button(buttonText, voiceButtonStyle, GUILayout.Width(50), GUILayout.Height(isSpeechActive ? 50 : 40)))
                {
                    // Інвертуємо стан і викликаємо метод для запуску розпізнавання мови
                    if (aiAgent != null)
                    {
                        bool newState = !isSpeechActive;
                        aiAgent.IsSpeechToTextActive = newState;
                        
                        if (newState)
                        {
                            // Викликаємо метод ConvertSpeechToText напряму
                            aiAgent.ConvertSpeechToText();
                        }
                        else
                        {
                            // Якщо вимикаємо активний запис, додаємо повідомлення про скасування
                            aiAgent.AddSystemMessage("⏹️ Запис скасовано користувачем");
                        }
                    }
                }
                
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
                     // Перевіряємо натискання клавіші Enter
            bool enterPressed = false;
            if (Event.current.type == EventType.KeyDown && 
                Event.current.keyCode == KeyCode.Return && 
                !Event.current.shift && 
                GUI.GetNameOfFocusedControl() == "UserInputTextField")
            {
                enterPressed = true;
                Event.current.Use(); // Запобігаємо обробці події іншими елементами
            }
                
                // Кнопка відправлення
                EditorGUI.BeginDisabledGroup(isWaitingForAIResponse || string.IsNullOrWhiteSpace(userInput));
                disabledGroupStarted = true;
                
                if ((GUILayout.Button("➤", GUILayout.Width(50), GUILayout.Height(40)) || enterPressed) && 
                    !string.IsNullOrEmpty(userInput))
                {
                    try
                    {
                        if (sendMessageAction != null)
                        {
                            sendMessageAction.Invoke(userInput, false);
                            userInput = "";
                            
                            // Сфокусувати знову текстове поле після відправлення
                            EditorGUI.FocusTextInControl("UserInputTextField");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Помилка при відправленні повідомлення: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка при відображенні панелі вводу: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Гарантуємо закриття груп навіть у випадку помилки
                if (disabledGroupStarted)
                {
                    try { EditorGUI.EndDisabledGroup(); } catch { }
                }
                
                if (horizontalStarted)
                {
                    try { EditorGUILayout.EndHorizontal(); } catch { }
                }
            }
        }
        
        // Метод DrawActionButtonsPanel видалено, оскільки всі дії тепер доступні через команди в чаті
        
        /// <summary>
        /// Малює кнопки контексту повідомлення
        /// </summary>
        private static void DrawMessageContextButtons(ChatMessage message, System.Action<string> saveMessageAction)
        {
            // Відображаємо кнопки контексту тільки для повідомлень від AI
            if (message.Sender.ToLower() != "ai" && message.Sender.ToLower() != "assistant")
                return;
                
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace(); // Розташовуємо кнопки праворуч
            
            // Кнопка для збереження в файл
            if (saveMessageAction != null && GUILayout.Button(new GUIContent("💾", "Зберегти результат у файл"), 
                GUILayout.Width(25), GUILayout.Height(20)))
            {
                saveMessageAction.Invoke(message.Text);
            }
            
            // Кнопка для копіювання тексту
            if (GUILayout.Button(new GUIContent("📋", "Копіювати текст"), 
                GUILayout.Width(25), GUILayout.Height(20)))
            {
                EditorGUIUtility.systemCopyBuffer = message.Text;
                // Покажемо тимчасову підказку через консоль (замість спливаючого повідомлення)
                Debug.Log("Текст скопійовано у буфер обміну");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Форматує повідомлення, підсвічуючи блоки коду і спеціальні команди
        /// </summary>
        private static string FormatMessageWithCodeHighlighting(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
                
            // Шукаємо блоки коду з потрійними зворотними лапками
            System.Text.RegularExpressions.Regex codeBlockRegex = 
                new System.Text.RegularExpressions.Regex(@"```(.*?)\r?\n([\s\S]*?)\r?\n```");
            
            string result = codeBlockRegex.Replace(message, match =>
            {
                string language = match.Groups[1].Value.Trim();
                string code = match.Groups[2].Value;
                
                // Форматуємо блок коду зі стилізацією
                return $"\n<color=#269926><b>// Код {language}</b></color>\n<color=#0077CC><i>{code}</i></color>\n";
            });
            
            // Підсвічуємо команди у форматі #command:value
            System.Text.RegularExpressions.Regex commandRegex = 
                new System.Text.RegularExpressions.Regex(@"#(\w+):(\w+)");
                
            result = commandRegex.Replace(result, match =>
            {
                return $"<color=#CC5500><b>#{match.Groups[1].Value}:{match.Groups[2].Value}</b></color>";
            });
            
            return result;
        }
    }
}
