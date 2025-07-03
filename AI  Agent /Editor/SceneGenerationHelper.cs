using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System;

namespace UnityAIAgent
{
    /// <summary>
    /// Клас для генерації та налаштування базових сцен
    /// </summary>
    public static class SceneGenerationHelper
    {
        /// <summary>
        /// Генерує базову сцену з гравцем та землею
        /// </summary>
        /// <param name="gameType">Тип гри</param>
        /// <param name="artStyle">Художній стиль</param>
        /// <param name="gameGoal">Мета гри (для назви)</param>
        /// <returns>Шлях до збереженої сцени</returns>
        public static string GenerateBasicScene(GameType gameType = GameType.Platformer, ArtStyle artStyle = ArtStyle.SciFi, string gameGoal = "Base Game")
        {
            Debug.Log($"Генерую базову {gameType} сцену в стилі {artStyle}...");
            
            // Створюємо нову сцену з базовими об'єктами
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Створюємо землю
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.transform.position = Vector3.zero;
            ground.name = "Ground";
            
            // Створюємо різні об'єкти залежно від типу гри
            switch (gameType)
            {
                case GameType.FPS:
                case GameType.TPS:
                    CreateShooterSceneElements();
                    break;
                case GameType.Platformer:
                    CreatePlatformerSceneElements();
                    break;
                case GameType.Puzzle:
                    CreatePuzzleSceneElements();
                    break;
                case GameType.RPG:
                    CreateRPGSceneElements();
                    break;
                default:
                    CreateDefaultSceneElements();
                    break;
            }
            
            // Зберігаємо сцену
            string folderPath = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            string sceneName = $"{gameType}_{artStyle}";
            if (!string.IsNullOrEmpty(gameGoal))
            {
                sceneName += $"_{System.IO.Path.GetFileNameWithoutExtension(gameGoal.Replace(" ", "_"))}";
            }
            string sanitizedSceneName = string.Join("_", sceneName.Split(System.IO.Path.GetInvalidFileNameChars()));
            string scenePath = $"Assets/Scenes/{sanitizedSceneName}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            Debug.Log($"Базову сцену згенеровано та збережено за шляхом {scenePath}");
            
            return scenePath;
        }
        
        /// <summary>
        /// Відкриває Asset Store з пошуком за вказаним ключовим словом
        /// </summary>
        /// <param name="keyword">Ключове слово для пошуку</param>
        public static void OpenAssetStoreSearch(string keyword)
        {
            string assetStoreSearchUrl = $"https://assetstore.unity.com/search?q={UnityWebRequest.EscapeURL(keyword)}";
            Application.OpenURL(assetStoreSearchUrl);
            Debug.Log($"Відкрито Asset Store для пошуку: {keyword}");
        }
        
        /// <summary>
        /// Створює базового гравця з компонентами
        /// </summary>
        /// <returns>Створений об'єкт гравця</returns>
        private static GameObject CreatePlayerWithComponents()
        {
            // Створюємо гравця
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.position = new Vector3(0, 2, 0);
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

            string scriptPath = "Assets/Scripts/PlayerMovement.cs";
            
            // Створюємо папку, якщо не існує
            string directory = System.IO.Path.GetDirectoryName(scriptPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string parentFolder = "Assets";
                string newFolderName = "Scripts";
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
            
            File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.ImportAsset(scriptPath);
            AssetDatabase.Refresh();
            
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
            
            return player;
        }
        
        #region Scene Type Specific Generation
        
        private static void CreateDefaultSceneElements()
        {
            CreatePlayerWithComponents();
        }
        
        private static void CreateShooterSceneElements()
        {
            GameObject player = CreatePlayerWithComponents();
            
            // Додаємо цілі
            for (int i = 0; i < 3; i++)
            {
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = new Vector3(UnityEngine.Random.Range(-5f, 5f), 1.5f, UnityEngine.Random.Range(3f, 8f));
                target.transform.localScale = new Vector3(1f, 3f, 1f);
                target.name = $"Target_{i + 1}";
                
                // Додаємо матеріал червоного кольору
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
            }
            
            // Додаємо перешкоди
            for (int i = 0; i < 5; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.transform.position = new Vector3(UnityEngine.Random.Range(-4f, 4f), 1f, UnityEngine.Random.Range(-4f, 4f));
                obstacle.transform.localScale = new Vector3(2f, 2f, 2f);
                obstacle.name = $"Obstacle_{i + 1}";
                
                // Додаємо матеріал сірого кольору
                var renderer = obstacle.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.grey;
                }
            }
        }
        
        private static void CreatePlatformerSceneElements()
        {
            GameObject player = CreatePlayerWithComponents();
            
            // Створюємо платформи
            for (int i = 0; i < 5; i++)
            {
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.transform.position = new Vector3(i * 3 - 6, i * 1.5f, 0);
                platform.transform.localScale = new Vector3(2.5f, 0.5f, 2.5f);
                platform.name = $"Platform_{i + 1}";
                
                // Додаємо матеріал синього кольору
                var renderer = platform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.2f, 0.4f, 0.8f);
                }
            }
            
            // Додаємо колекційні предмети
            for (int i = 0; i < 3; i++)
            {
                GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                collectible.transform.position = new Vector3(i * 3 - 6, i * 1.5f + 1.5f, 0);
                collectible.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                collectible.name = $"Collectible_{i + 1}";
                
                // Додаємо матеріал жовтого кольору
                var renderer = collectible.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.yellow;
                }
            }
        }
        
        private static void CreatePuzzleSceneElements()
        {
            GameObject player = CreatePlayerWithComponents();
            
            // Створюємо змінні об'єкти для головоломки
            for (int i = 0; i < 3; i++)
            {
                GameObject puzzleElement = GameObject.CreatePrimitive(PrimitiveType.Cube);
                puzzleElement.transform.position = new Vector3(i * 3 - 3, 1, 3);
                puzzleElement.transform.localScale = new Vector3(1f, 1f, 1f);
                puzzleElement.name = $"PuzzleElement_{i + 1}";
                
                // Додаємо різні кольори
                var renderer = puzzleElement.GetComponent<Renderer>();
                if (renderer != null)
                {
                    switch (i)
                    {
                        case 0: renderer.material.color = Color.red; break;
                        case 1: renderer.material.color = Color.green; break;
                        case 2: renderer.material.color = Color.blue; break;
                    }
                }
            }
            
            // Створюємо цільові зони
            for (int i = 0; i < 3; i++)
            {
                GameObject targetZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetZone.transform.position = new Vector3(i * 3 - 3, 0.1f, -3);
                targetZone.transform.localScale = new Vector3(1f, 0.1f, 1f);
                targetZone.name = $"TargetZone_{i + 1}";
                
                // Додаємо різні кольори з прозорістю
                var renderer = targetZone.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color targetColor = Color.white;
                    switch (i)
                    {
                        case 0: targetColor = new Color(1, 0, 0, 0.5f); break;
                        case 1: targetColor = new Color(0, 1, 0, 0.5f); break;
                        case 2: targetColor = new Color(0, 0, 1, 0.5f); break;
                    }
                    renderer.material.color = targetColor;
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    renderer.material.EnableKeyword("_ALPHABLEND_ON");
                    renderer.material.renderQueue = 3000;
                }
            }
        }
        
        private static void CreateRPGSceneElements()
        {
            GameObject player = CreatePlayerWithComponents();
            
            // Створюємо НІП
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.transform.position = new Vector3(3, 1, 3);
            npc.transform.localScale = new Vector3(1f, 1f, 1f);
            npc.name = "NPC_Quest";
            
            // Додаємо матеріал зеленого кольору
            var npcRenderer = npc.GetComponent<Renderer>();
            if (npcRenderer != null)
            {
                npcRenderer.material.color = new Color(0, 0.8f, 0.2f);
            }
            
            // Створюємо ворога
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.position = new Vector3(-3, 1, 3);
            enemy.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            enemy.name = "Enemy";
            
            // Додаємо матеріал червоного кольору
            var enemyRenderer = enemy.GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = new Color(0.8f, 0.2f, 0.2f);
            }
            
            // Створюємо скриню зі скарбами
            GameObject treasureChest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            treasureChest.transform.position = new Vector3(0, 0.5f, 6);
            treasureChest.transform.localScale = new Vector3(1f, 0.5f, 0.7f);
            treasureChest.name = "TreasureChest";
            
            // Додаємо матеріал коричневого кольору
            var chestRenderer = treasureChest.GetComponent<Renderer>();
            if (chestRenderer != null)
            {
                chestRenderer.material.color = new Color(0.6f, 0.4f, 0.2f);
            }
            
            // Додаємо кришку скрині
            GameObject chestLid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestLid.transform.position = new Vector3(0, 0.75f, 5.8f);
            chestLid.transform.localScale = new Vector3(1f, 0.1f, 0.3f);
            chestLid.name = "ChestLid";
            chestLid.transform.parent = treasureChest.transform;
            
            // Додаємо матеріал коричневого кольору
            var lidRenderer = chestLid.GetComponent<Renderer>();
            if (lidRenderer != null)
            {
                lidRenderer.material.color = new Color(0.6f, 0.4f, 0.2f);
            }
        }
        
        #endregion
    }
}
