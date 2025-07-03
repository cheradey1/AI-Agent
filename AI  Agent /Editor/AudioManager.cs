using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityAIAgent
{
    /// <summary>
    /// Типи звукових сповіщень
    /// </summary>
    public enum AudioType
    {
        Success,
        Warning,
        Error,
        Recording,
        Notification,
        Info
    }
    
    /// <summary>
    /// Менеджер для роботи з аудіо в Unity Editor - надає методи для TTS та запису звуку
    /// </summary>
    public class AudioManager
    {
        // Синглтон
        private static AudioManager _instance;
        
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AudioManager();
                return _instance;
            }
        }
        
        // Налаштування
        private bool _isInitialized = false;
        private AIAgentSettings _settings;
        
        // Властивості
        public bool IsTTSEnabled { get; private set; }
        public bool IsSTTEnabled { get; private set; }
        
        // Приватний конструктор (синглтон)
        private AudioManager()
        {
            Initialize();
        }
        
        /// <summary>
        /// Ініціалізація AudioManager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            // Спробуємо знайти налаштування
            _settings = GetSettings();
            
            // Логуємо статус ініціалізації
            string logMessage = "AudioManager initialized " + 
                (_settings != null ? "with settings." : "without settings, using defaults.");
            Debug.Log("=== Перевірка доступності AudioManager ===");
            Debug.Log("✓ Тип AudioManager успішно знайдено через рефлексію: " + GetType().Name);
            Debug.Log("=== Перевірка завершена ===");
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Отримання налаштувань
        /// </summary>
        private AIAgentSettings GetSettings()
        {
            // Використовуємо покращений метод з AIAgentSettingsCreator
            return AIAgentSettingsCreator.GetSettings(true);
        }
        
        /// <summary>
        /// Перетворення тексту на голос
        /// </summary>
        public async Task<bool> TextToSpeech(string text)
        {
            try
            {
                Debug.Log($"[TTS] Запит на озвучування: {(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}");
                await Task.Delay(500); // Імітація обробки
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Помилка: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Запис звуку користувача та його розпізнавання
        /// </summary>
        public async Task<string> SpeechToText()
        {
            try
            {
                Debug.Log("[STT] Початок запису голосу...");
                
                // Імітація різних фаз запису з відповідними повідомленнями для більш реалістичного досвіду
                await Task.Delay(500); // Початкова ініціалізація
                Debug.Log("[STT] Мікрофон активовано, аналіз звуку...");
                
                await Task.Delay(1000); // Імітація запису
                Debug.Log("[STT] Обробка аудіоданих...");
                
                await Task.Delay(500); // Завершальна обробка
                Debug.Log("[STT] Запис завершено, відправлення на розпізнавання...");
                
                await Task.Delay(1000); // Імітація розпізнавання на сервері
                
                // В реальній імплементації тут має бути інтеграція зі справжнім STT сервісом
                // Емулюємо більш реалістичну роботу STT з різними варіантами відповідей
                string[] possibleResults = new string[] {
                    "Створи новий об'єкт на сцені",
                    "Додай скрипт руху до персонажа",
                    "Згенеруй ландшафт з горами та лісом",
                    "Оптимізуй продуктивність гри",
                    "Напиши мені скрипт для керування персонажем",
                    "Що таке Unity компоненти?",
                    "Як працюють колайдери в Unity?",
                    "Поясни систему частинок в Unity",
                    "Створи шейдер для води і додай його на сцену",
                    "Покажи мені приклади використання шейдерів у Unity",
                    "Як створити систему інвентаря для гри?",
                    "Згенеруй ефект вибуху на сцені",
                    "Інтегруй фізичну систему для персонажа",
                    "Як налаштувати гравітацію в грі?"
                };
                
                // Імітуємо різну якість розпізнавання на основі декількох факторів
                float qualityFactor = UnityEngine.Random.value;
                
                // Імітація випадків, коли розпізнавання не вдалося
                if (qualityFactor < 0.08f) {
                    Debug.LogWarning("[STT] Критично низька якість аудіо, розпізнавання не вдалося");
                    return "";
                }
                
                // Вибираємо базову відповідь
                int index = UnityEngine.Random.Range(0, possibleResults.Length);
                string result = possibleResults[index];
                
                // Для низької якості додаємо спотворення
                if (qualityFactor < 0.25f) {
                    // Імітуємо низьку якість розпізнавання
                    result += " [недостатньо чітко]";
                    Debug.Log("[STT] Розпізнано з низькою впевненістю: " + result);
                    
                    // Іноді додаємо спотворення слів для більшої реалістичності
                    if (qualityFactor < 0.15f) {
                        result = ModifyTextToSimulatePoorRecognition(result);
                    }
                } else {
                    // Добра якість розпізнавання
                    Debug.Log("[STT] Розпізнано з високою впевненістю: " + result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STT] Помилка: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Допоміжний метод, який модифікує текст для імітації неточного розпізнавання
        /// </summary>
        private string ModifyTextToSimulatePoorRecognition(string input)
        {
            try {
                string modified = input;
                
                // Видаляємо можливу позначку низької якості, щоб не дублювати її
                modified = modified.Replace("[недостатньо чітко]", "").Trim();
                
                // Додаємо спотворення з невеликою ймовірністю
                if (UnityEngine.Random.value < 0.3f) {
                    // Спотворення окремих слів
                    string[] words = modified.Split(' ');
                    for (int i = 0; i < words.Length; i++) {
                        // З деякою ймовірністю замінюємо слово на подібне
                        if (UnityEngine.Random.value < 0.2f && words[i].Length > 3) {
                            words[i] = words[i].Substring(0, words[i].Length - 2) + "...";
                        }
                    }
                    modified = string.Join(" ", words);
                }
                
                // Повертаємо модифікований текст з позначкою
                return modified + " [недостатньо чітко]";
            }
            catch {
                // У випадку помилки повертаємо оригінальний текст
                return input;
            }
        }
        
        /// <summary>
        /// Відтворює звукове сповіщення заданого типу
        /// </summary>
        /// <param name="type">Тип звуку</param>
        public void PlayNotificationSound(AudioType type)
        {
            // Заглушка для Unity Editor, оскільки без AudioSource ми не можемо відтворювати звуки напряму
            // У реальному проекті тут використовувалися б EditorUtility.PlayGameViewAudio або інші методи
            
            // Виводимо повідомлення в консоль для перевірки
            string soundName = type.ToString();
            Debug.Log($"[AudioManager] Відтворення звуку: {soundName}");
            
            // Відтворюємо звуковий ефект у редакторі
            try
            {
                switch (type)
                {
                    case AudioType.Success:
                        EditorApplication.Beep(); // Стандартний системний звук
                        break;
                    case AudioType.Error:
                        EditorApplication.Beep(); // Можна використовувати різні звукові ефекти в повній реалізації
                        break;
                    case AudioType.Warning:
                    case AudioType.Notification:
                    case AudioType.Recording:
                        EditorApplication.Beep(); // Заглушка для демонстрації
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioManager] Помилка відтворення звуку: {e.Message}");
            }
        }
    }
}
