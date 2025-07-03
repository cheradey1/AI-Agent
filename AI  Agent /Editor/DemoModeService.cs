using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace UnityAIAgent
{
    /// <summary>
    /// Сервіс для демо-режиму, який працює без API ключів
    /// Використовує попередньо збережені відповіді для типових запитань
    /// </summary>
    public class DemoModeService : IAIService
    {
        private readonly AIAgentSettings _settings;
        private Dictionary<string, List<string>> _responsesCache;
        private System.Random _random = new System.Random();
        
        // Шлях до файлу з демо відповідями
        private const string DEMO_RESPONSES_PATH = "Packages/com.unity.ai.agent/Editor/Resources/demo_responses.json";
        
        // Дефолтні відповіді для типових запитів (розширена версія)
        private readonly Dictionary<string, List<string>> _defaultResponses = new Dictionary<string, List<string>>
        {
            { 
                "привіт", new List<string> { 
                    "Привіт! Я Демо-версія AI Unity Agent. Чим можу допомогти? Ви можете запитати мене про базові концепції Unity.",
                    "Вітаю! Я демо-версія AI помічника для Unity. У демо-режимі я можу відповідати лише на базові запитання.",
                    "Привіт! Для повноцінної роботи рекомендую налаштувати безкоштовну модель Ollama або API ключ будь-якої моделі."
                } 
            },
            { 
                "unity", new List<string> { 
                    "Unity - це крос-платформний ігровий рушій, який використовується для розробки відеоігор та додатків. Він підтримує 2D і 3D графіку, drag-and-drop функціональність та скриптування на C#.",
                    "Unity - це потужний інструмент для розробки ігор та інтерактивних додатків. Ви можете створювати проєкти для різних платформ: PC, консолей, мобільних пристроїв та VR/AR."
                } 
            },
            { 
                "код", new List<string> { 
                    "Ось простий приклад скрипта для рухомого об'єкта в Unity:\n\n```csharp\nusing UnityEngine;\n\npublic class Movement : MonoBehaviour \n{\n    public float speed = 5f;\n    \n    void Update()\n    {\n        float horizontal = Input.GetAxis(\"Horizontal\");\n        float vertical = Input.GetAxis(\"Vertical\");\n        \n        Vector3 movement = new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime;\n        transform.Translate(movement);\n    }\n}\n```",
                    "У демо-режимі я можу показувати лише базові приклади коду. Для генерації спеціальних скриптів під ваші задачі потрібно налаштувати повну версію AI Agent."
                } 
            },
            {
                "скрипт", new List<string> {
                    "Ось простий скрипт для обертання об'єкта в Unity:\n\n```csharp\nusing UnityEngine;\n\npublic class Rotator : MonoBehaviour\n{\n    public float rotationSpeed = 30f;\n    public Vector3 rotationAxis = Vector3.up;\n    \n    void Update()\n    {\n        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);\n    }\n}\n```",
                    "Базовий скрипт для збирання об'єктів в Unity:\n\n```csharp\nusing UnityEngine;\n\npublic class Collector : MonoBehaviour\n{\n    public int score = 0;\n    \n    void OnTriggerEnter(Collider other)\n    {\n        if (other.CompareTag(\"Collectible\"))\n        {\n            score++;\n            Debug.Log($\"Collected item! Score: {score}\");\n            Destroy(other.gameObject);\n        }\n    }\n}\n```"
                }
            },
            { 
                "помилка", new List<string> { 
                    "Типові помилки в Unity:\n1. NullReferenceException - об'єкт не знайдено (не призначений у інспекторі або не ініціалізований)\n2. Missing component - компонент відсутній\n3. Scene not found - сцена не знайдена\n\nДля точної діагностики вашої помилки потрібно увімкнути повний режим AI Agent.",
                    "Щоб виправити помилки в Unity, спочатку переконайтеся, що всі посилання правильно встановлені в інспекторі. Перевірте, чи всі необхідні компоненти додані до об'єктів. У демо-режимі я можу дати лише загальні поради."
                } 
            },
            {
                "2d", new List<string> {
                    "У Unity для 2D гри вам знадобиться:\n1. SpriteRenderer - для відображення графіки\n2. Rigidbody2D - для фізики\n3. BoxCollider2D/CircleCollider2D - для колізій\n\nПриклад скрипта руху для 2D платформера:\n```csharp\nusing UnityEngine;\n\npublic class PlatformerController : MonoBehaviour\n{\n    public float moveSpeed = 5f;\n    public float jumpForce = 10f;\n    private Rigidbody2D rb;\n    private bool isGrounded;\n    \n    void Start()\n    {\n        rb = GetComponent<Rigidbody2D>();\n    }\n    \n    void Update()\n    {\n        float move = Input.GetAxis(\"Horizontal\");\n        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);\n        \n        if (Input.GetButtonDown(\"Jump\") && isGrounded)\n        {\n            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);\n        }\n    }\n    \n    void OnCollisionEnter2D(Collision2D collision)\n    {\n        if (collision.gameObject.CompareTag(\"Ground\"))\n        {\n            isGrounded = true;\n        }\n    }\n    \n    void OnCollisionExit2D(Collision2D collision)\n    {\n        if (collision.gameObject.CompareTag(\"Ground\"))\n        {\n            isGrounded = false;\n        }\n    }\n}\n```"
                }
            },
            {
                "3d", new List<string> {
                    "Для 3D гри в Unity важливі такі компоненти:\n1. MeshRenderer і MeshFilter - для відображення 3D моделей\n2. Rigidbody - для фізики\n3. Collider (Box, Sphere, Capsule, Mesh) - для колізій\n\nПриклад простого контролера від третьої особи:\n```csharp\nusing UnityEngine;\n\npublic class ThirdPersonController : MonoBehaviour\n{\n    public float moveSpeed = 5f;\n    public float rotationSpeed = 100f;\n    private CharacterController controller;\n    \n    void Start()\n    {\n        controller = GetComponent<CharacterController>();\n    }\n    \n    void Update()\n    {\n        float horizontal = Input.GetAxis(\"Horizontal\");\n        float vertical = Input.GetAxis(\"Vertical\");\n        \n        Vector3 movement = new Vector3(horizontal, 0, vertical);\n        \n        // Застосовуємо гравітацію\n        if (!controller.isGrounded)\n        {\n            movement.y -= 9.8f * Time.deltaTime;\n        }\n        \n        // Обертаємо персонажа у напрямку руху\n        if (movement != Vector3.zero)\n        {\n            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(horizontal, 0, vertical));\n            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);\n        }\n        \n        controller.Move(movement * moveSpeed * Time.deltaTime);\n    }\n}\n```"
                }
            },
            {
                "api", new List<string> {
                    "Для отримання API ключів для AI сервісів, вам потрібно:\n\n1. OpenAI API: зареєструватися на https://platform.openai.com і створити ключ API\n2. Google Gemini API: отримати на https://ai.google.dev/\n3. Claude API: зареєструватися на https://console.anthropic.com\n\nА ще можете використовувати безкоштовні локальні моделі через Ollama: https://ollama.ai/\n\nДля налаштування агента вам потрібно додати ключ у відповідне поле в налаштуваннях."
                }
            },
            { 
                "default", new List<string> { 
                    "Я знаходжуся в демо-режимі і маю обмежений функціонал. Для повноцінної роботи, будь ласка, налаштуйте AI Agent з API ключем або локальними моделями Ollama.",
                    "В даний момент AI Agent працює в демо-режимі. Щоб отримати більше можливостей, налаштуйте безкоштовні або платні моделі в налаштуваннях агента.",
                    "Для повноцінної роботи AI Agent потрібно встановити Ollama для локальних моделей або отримати API ключ для хмарних моделей.",
                    "Ця відповідь згенерована в демо-режимі. Для отримання повної функціональності рекомендую налаштувати один із підтримуваних AI сервісів у налаштуваннях плагіна."
                } 
            }
        };
        
        public DemoModeService(AIAgentSettings settings)
        {
            _settings = settings;
            LoadDemoResponses();
        }
        
        public bool IsConfigured() => true; // Демо режим завжди готовий до роботи
        public string GetServiceName() => "Demo";
        
        public async Task<AIResponse> QueryAI(string prompt, List<string> chatHistory)
        {
            await Task.Delay(500); // Імітуємо затримку мережі
            
            string response = GetDemoResponse(prompt);
            return new AIResponse 
            { 
                Content = response, 
                IsSuccess = true,
                IsDemoMode = true
            };
        }
        
        /// <summary>
        /// Завантажує демо-відповіді з файлу або використовує стандартні
        /// </summary>
        private void LoadDemoResponses()
        {
            try
            {
                _responsesCache = new Dictionary<string, List<string>>();
                
                // Спробуємо завантажити з ресурсів
                TextAsset textAsset = Resources.Load<TextAsset>("demo_responses");
                if (textAsset != null)
                {
                    _responsesCache = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(textAsset.text);
                }
                // Якщо не вдалося, спробуємо завантажити з файлу
                else if (File.Exists(DEMO_RESPONSES_PATH))
                {
                    string json = File.ReadAllText(DEMO_RESPONSES_PATH);
                    _responsesCache = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                }
                
                // Якщо нічого не вдалося, використовуємо стандартні
                if (_responsesCache == null || _responsesCache.Count == 0)
                {
                    _responsesCache = _defaultResponses;
                }
                
                // Додаємо дефолтні відповіді, якщо вони не дублюються
                foreach (var key in _defaultResponses.Keys)
                {
                    if (!_responsesCache.ContainsKey(key))
                    {
                        _responsesCache[key] = _defaultResponses[key];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Помилка при завантаженні демо-відповідей: {ex.Message}");
                _responsesCache = _defaultResponses;
            }
        }
        
        /// <summary>
        /// Отримує демо-відповідь на основі запиту
        /// Покращена версія з кращим розпізнаванням теми запиту
        /// </summary>
        private string GetDemoResponse(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                // Якщо запит порожній, повертаємо привітання
                var greetings = _responsesCache["привіт"];
                return greetings[_random.Next(0, greetings.Count)];
            }
            
            // Нормалізуємо запит
            string normalizedPrompt = prompt.ToLower().Trim();
            
            // Спочатку шукаємо точні співпадіння
            foreach (var key in _responsesCache.Keys)
            {
                if (normalizedPrompt.Contains(key))
                {
                    var responses = _responsesCache[key];
                    return responses[_random.Next(0, responses.Count)];
                }
            }
            
            // Якщо точних співпадінь немає, шукаємо найбільш близькі відповіді
            // за допомогою аналізу ключових слів
            var keywordMatches = new Dictionary<string, int>();
            
            // Ключові слова для категорій
            var keywords = new Dictionary<string, List<string>>()
            {
                { "привіт", new List<string> { "привіт", "вітаю", "добрий день", "добрий вечір", "здрастуйте", "хай" } },
                { "unity", new List<string> { "юніті", "unity", "рушій", "ігровий", "game engine" } },
                { "код", new List<string> { "код", "скрипт", "програма", "функція", "метод", "клас" } },
                { "скрипт", new List<string> { "скрипт", "компонент", "клас", "програма", "реалізація", "монобехейвіор" } },
                { "помилка", new List<string> { "помилка", "баг", "дебаг", "виправити", "проблема", "exception", "помилки" } },
                { "2d", new List<string> { "2d", "спрайт", "2д", "платформер", "2д гра", "2d гра", "плоска гра" } },
                { "3d", new List<string> { "3d", "3д", "3д гра", "3d гра", "простір", "меш", "модель" } },
                { "api", new List<string> { "api", "ключ", "ключі", "openai", "gemini", "claude", "ollama", "налаштування", "безкоштовний" } }
            };
            
            // Аналізуємо запит за ключовими словами
            foreach (var category in keywords)
            {
                int matchScore = 0;
                foreach (var keyword in category.Value)
                {
                    if (normalizedPrompt.Contains(keyword))
                    {
                        matchScore += 1;
                    }
                }
                
                if (matchScore > 0)
                {
                    keywordMatches[category.Key] = matchScore;
                }
            }
            
            // Якщо знайшли співпадіння за ключовими словами, повертаємо найкращу відповідь
            if (keywordMatches.Count > 0)
            {
                var bestMatch = keywordMatches.OrderByDescending(x => x.Value).First().Key;
                var responses = _responsesCache[bestMatch];
                return responses[_random.Next(0, responses.Count)];
            }
            
            // Якщо нічого не знайдено, повертаємо стандартну відповідь
            var defaultResponses = _responsesCache["default"];
            return defaultResponses[_random.Next(0, defaultResponses.Count)];
        }
        
        /// <summary>
        /// Обчислює відстань Левенштейна між двома рядками
        /// </summary>
        private int LevenshteinDistance(string s, string t)
        {
            // Базовий випадок: порожні рядки
            if (string.IsNullOrEmpty(s))
            {
                return string.IsNullOrEmpty(t) ? 0 : t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            // Створення матриці відстаней
            int[,] d = new int[s.Length + 1, t.Length + 1];

            // Ініціалізація
            for (int i = 0; i <= s.Length; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= t.Length; j++)
            {
                d[0, j] = j;
            }

            // Обчислення
            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s.Length, t.Length];
        }
    }
}
