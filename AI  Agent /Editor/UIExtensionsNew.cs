using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace UnityAIAgent
{
    /// <summary>
    /// Методи розширення для AIAgentUnity інтерфейсу
    /// </summary>
    public static class UIExtensionsNew
    {
        /// <summary>
        /// Малює панель ігрових параметрів з новими функціями генерації базових сцен
        /// </summary>
        public static void DrawGameParametersPanel(Rect panelRect, ref GameType gameType, ref PlayerCount playerCount, 
            ref ArtStyle artStyle, ref MapSize mapSize, ref string gameGoal, ref Vector2 scrollPosition, AIAgentUnity window)
        {
            GUILayout.BeginArea(panelRect);
            EditorGUILayout.LabelField("🎮 Ігрові Параметри", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Основні параметри гри", EditorStyles.boldLabel);
            
            gameType = (GameType)EditorGUILayout.EnumPopup("Тип гри:", gameType);
            playerCount = (PlayerCount)EditorGUILayout.EnumPopup("Кількість гравців:", playerCount);
            artStyle = (ArtStyle)EditorGUILayout.EnumPopup("Художній стиль:", artStyle);
            mapSize = (MapSize)EditorGUILayout.EnumPopup("Розмір карти:", mapSize);
            
            EditorGUILayout.LabelField("Ціль гри:");
            gameGoal = EditorGUILayout.TextArea(gameGoal, GUILayout.Height(60));
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Генерація:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Згенерувати прототип", GUILayout.Height(30)))
            {
                if (window != null)
                {
                    window.GenerateGame();
                }
                else
                {
                    Debug.LogError("Вікно AIAgentUnity не знайдено");
                }
            }
            
            // Нова кнопка для швидкого створення базової сцени
            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // Зелений колір для кнопки
            if (GUILayout.Button("🔧 Створити базову сцену", GUILayout.Height(30)))
            {
                if (window != null)
                {
                    window.GenerateBasicScene();
                }
                else
                {
                    Debug.LogError("Вікно AIAgentUnity не знайдено");
                }
            }
            GUI.backgroundColor = Color.white; // Повертаємо початковий колір
            
            // Кнопка для відкриття Asset Store з результатами пошуку за типом гри
            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f); // Синій колір для кнопки
            if (GUILayout.Button("🔍 Знайти ассети за типом гри", GUILayout.Height(30)))
            {
                if (window != null)
                {
                    window.SearchAssetsForGameType();
                }
                else
                {
                    Debug.LogError("Вікно AIAgentUnity не знайдено");
                }
            }
            GUI.backgroundColor = Color.white; // Повертаємо початковий колір
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// Клас для розширення функціональності AIAgentUnity
    /// </summary>
    public static class AIAgentExtensions
    {
        /// <summary>
        /// Генерує базову сцену
        /// </summary>
        public static void GenerateBasicScene(this AIAgentUnity agentWindow)
        {
            // Отримання параметрів з вікна агента через рефлексію
            var gameTypeField = typeof(AIAgentUnity).GetField("_selectedGameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var artStyleField = typeof(AIAgentUnity).GetField("_selectedArtStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gameGoalField = typeof(AIAgentUnity).GetField("_gameGoal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chatHistoryField = typeof(AIAgentUnity).GetField("_chatHistory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gameTypeField == null || artStyleField == null || gameGoalField == null || chatHistoryField == null)
            {
                Debug.LogError("Не вдалося отримати доступ до полів AIAgentUnity");
                return;
            }
            
            var gameType = (GameType)gameTypeField.GetValue(agentWindow);
            var artStyle = (ArtStyle)artStyleField.GetValue(agentWindow);
            var gameGoal = (string)gameGoalField.GetValue(agentWindow);
            var chatHistory = (List<ChatMessage>)chatHistoryField.GetValue(agentWindow);
            
            try
            {
                chatHistory.Add(new ChatMessage("System", $"Генерую базову {gameType} сцену в стилі {artStyle}..."));
                
                // Створюємо нову сцену з базовими об'єктами
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                // Створюємо землю
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.localScale = new Vector3(10, 1, 10);
                ground.transform.position = Vector3.zero;
                ground.name = "Ground";
                
                // Створюємо гравця
                GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.transform.position = new Vector3(0, 2, 0);
                player.transform.localScale = new Vector3(1, 1, 1);
                player.name = "Player";

                // Налаштовуємо камеру
                Camera cam = Camera.main;
                if (cam == null)
                {
                    GameObject camObj = new GameObject("Main Camera");
                    cam = camObj.AddComponent<Camera>();
                    cam.tag = "MainCamera";
                }
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.LookAt(player.transform);

                // Створюємо скрипт руху гравця
                string scriptContent = @"using UnityEngine;
public class PlayerMovement : MonoBehaviour {
    public float speed = 5f;
    public float jumpForce = 5f;
    private bool isGrounded;
    private Rigidbody rb;
    
    void Start() {
        rb = GetComponent<Rigidbody>();
        if (rb == null) {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    
    void Update() {
        float h = Input.GetAxis(""Horizontal"");
        float v = Input.GetAxis(""Vertical"");
        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
        
        // Перевірка приземлення
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        
        // Стрибок
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}";

                // Створюємо папки для скриптів, якщо вони не існують
                string folderPath = "Assets/Scripts";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Scripts");
                }
                
                string scriptPath = $"{folderPath}/PlayerMovement.cs";
                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.ImportAsset(scriptPath);
                AssetDatabase.Refresh();

                // Створюємо папки для сцен, якщо вони не існують
                string scenesFolderPath = "Assets/Scenes";
                if (!AssetDatabase.IsValidFolder(scenesFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }
                
                // Додаємо компонент до гравця після імпорту скрипту
                EditorApplication.delayCall += () => {
                    var playerMovementType = System.Type.GetType("PlayerMovement, Assembly-CSharp");
                    if (playerMovementType != null) {
                        player.AddComponent(playerMovementType);
                        
                        // Додаємо Rigidbody, якщо його ще немає
                        if (player.GetComponent<Rigidbody>() == null)
                        {
                            Rigidbody rb = player.AddComponent<Rigidbody>();
                            rb.constraints = RigidbodyConstraints.FreezeRotation;
                        }
                    } else {
                        Debug.LogError("Не вдалося знайти тип PlayerMovement");
                    }
                };
                
                // Зберігаємо сцену
                string sceneName = $"{gameType}_{artStyle}";
                if (!string.IsNullOrEmpty(gameGoal))
                {
                    sceneName += $"_{System.IO.Path.GetFileNameWithoutExtension(gameGoal.Replace(" ", "_"))}";
                }
                string sanitizedSceneName = string.Join("_", sceneName.Split(System.IO.Path.GetInvalidFileNameChars()));
                string scenePath = $"{scenesFolderPath}/{sanitizedSceneName}.unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
                
                chatHistory.Add(new ChatMessage("System", $"✅ Сцену успішно створено за адресою {scenePath}"));
                chatHistory.Add(new ChatMessage("System", $"✅ Для пошуку відповідних ресурсів у Asset Store, використайте кнопку 'Знайти ассети за типом гри'"));
                
                string assetStoreSearchUrl = $"https://assetstore.unity.com/search?q={UnityWebRequest.EscapeURL(gameType.ToString())}+{UnityWebRequest.EscapeURL(artStyle.ToString())}";
                chatHistory.Add(new ChatMessage("System", $"[Відкрити Asset Store]({assetStoreSearchUrl})"));
                
                agentWindow.Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Помилка генерації сцени: {ex.Message}");
                chatHistory.Add(new ChatMessage("System", $"❌ Помилка генерації сцени: {ex.Message}"));
                agentWindow.Repaint();
            }
        }
        
        /// <summary>
        /// Відкриває Asset Store з пошуком за типом гри
        /// </summary>
        public static void SearchAssetsForGameType(this AIAgentUnity agentWindow)
        {
            // Отримання параметрів з вікна агента через рефлексію
            var gameTypeField = typeof(AIAgentUnity).GetField("_selectedGameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var artStyleField = typeof(AIAgentUnity).GetField("_selectedArtStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chatHistoryField = typeof(AIAgentUnity).GetField("_chatHistory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gameTypeField == null || artStyleField == null || chatHistoryField == null)
            {
                Debug.LogError("Не вдалося отримати доступ до полів AIAgentUnity");
                return;
            }
            
            var gameType = (GameType)gameTypeField.GetValue(agentWindow);
            var artStyle = (ArtStyle)artStyleField.GetValue(agentWindow);
            var chatHistory = (List<ChatMessage>)chatHistoryField.GetValue(agentWindow);
            
            try
            {
                chatHistory.Add(new ChatMessage("System", $"Відкриваю Asset Store для пошуку ресурсів за типом: {gameType} та стилем: {artStyle}"));
                
                // Формуємо URL для пошуку в Asset Store
                string assetStoreSearchUrl = $"https://assetstore.unity.com/search?q={UnityWebRequest.EscapeURL(gameType.ToString())}+{UnityWebRequest.EscapeURL(artStyle.ToString())}";
                Application.OpenURL(assetStoreSearchUrl);
                
                agentWindow.Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Помилка відкриття Asset Store: {ex.Message}");
                chatHistory.Add(new ChatMessage("System", $"❌ Помилка відкриття Asset Store: {ex.Message}"));
                agentWindow.Repaint();
            }
        }
        
        /// <summary>
        /// Генерує гру на основі обраних параметрів
        /// </summary>
        public static void GenerateGame(this AIAgentUnity agentWindow)
        {
            // Отримання параметрів з вікна агента через рефлексію
            var gameTypeField = typeof(AIAgentUnity).GetField("_selectedGameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var playerCountField = typeof(AIAgentUnity).GetField("_selectedPlayerCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var artStyleField = typeof(AIAgentUnity).GetField("_selectedArtStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mapSizeField = typeof(AIAgentUnity).GetField("_selectedMapSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gameGoalField = typeof(AIAgentUnity).GetField("_gameGoal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chatHistoryField = typeof(AIAgentUnity).GetField("_chatHistory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gameTypeField == null || playerCountField == null || artStyleField == null || 
                mapSizeField == null || gameGoalField == null || chatHistoryField == null)
            {
                Debug.LogError("Не вдалося отримати доступ до полів AIAgentUnity");
                return;
            }
            
            var gameType = (GameType)gameTypeField.GetValue(agentWindow);
            var playerCount = (PlayerCount)playerCountField.GetValue(agentWindow);
            var artStyle = (ArtStyle)artStyleField.GetValue(agentWindow);
            var mapSize = (MapSize)mapSizeField.GetValue(agentWindow);
            var gameGoal = (string)gameGoalField.GetValue(agentWindow);
            var chatHistory = (List<ChatMessage>)chatHistoryField.GetValue(agentWindow);
            
            // Викликаємо метод SendMessageToAI через рефлексію
            var sendMessageMethod = typeof(AIAgentUnity).GetMethod("SendMessageToAI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (sendMessageMethod == null)
            {
                Debug.LogError("Не вдалося знайти метод SendMessageToAI");
                return;
            }
            
            // Формуємо запит до AI на основі обраних параметрів гри
            string prompt = $"Створи базову структуру для Unity гри з наступними параметрами:\n" +
                $"- Тип гри: {gameType}\n" +
                $"- Кількість гравців: {playerCount}\n" +
                $"- Стиль: {artStyle}\n" +
                $"- Розмір карти: {mapSize}\n" +
                $"- Мета гри: {gameGoal}\n\n" +
                "Потрібно: основний структурний опис гри, головні скрипти для контролера гравця, ігрової логіки, " +
                "менеджера рівнів та короткі інструкції щодо створення головних елементів сцени. " +
                "Усі скрипти повинні бути на С# з повними коментарями.";
            
            // Додаємо повідомлення до історії чату
            chatHistory.Add(new ChatMessage("System", "Починаємо генерацію гри на основі обраних параметрів..."));
            
            // Викликаємо метод SendMessageToAI
            sendMessageMethod.Invoke(agentWindow, new object[] { prompt, false });
        }
    }
}
