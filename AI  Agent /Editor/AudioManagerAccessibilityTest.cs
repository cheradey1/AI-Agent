using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace UnityAIAgent
{
    public class AudioManagerAccessibilityTest
    {
        [MenuItem("Window/AI Assistant/Test Audio Manager")]
        public static void Test()
        {
            EditorApplication.delayCall += RunTest;
        }
        
        public static void RunTest()
        {
            Debug.Log("=== Перевірка доступності AudioManager ===");
            
            // Перевіряємо наявність класу AudioManager через рефлексію
            System.Type audioManagerType = typeof(AudioManager);
            if (audioManagerType != null)
            {
                Debug.Log($"✓ Тип AudioManager успішно знайдено через рефлексію: {audioManagerType.Name}");
                
                // Перевіряємо доступ до синглтону
                var property = audioManagerType.GetProperty("Instance", 
                    BindingFlags.Public | BindingFlags.Static);
                var instance = property.GetValue(null);
                
                if (instance != null)
                {
                    Debug.Log($"✓ Екземпляр AudioManager успішно отримано: {instance}");
                }
                else
                {
                    Debug.LogError("✗ Не вдалося отримати екземпляр AudioManager");
                }
            }
            else
            {
                Debug.LogError("✗ Тип AudioManager не знайдено");
            }
            
            Debug.Log("=== Перевірка завершена ===");
        }
    }
}
