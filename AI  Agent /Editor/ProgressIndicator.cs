using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityAIAgent
{
    /// <summary>
    /// Анімований індикатор прогресу для відображення очікування в редакторі Unity
    /// </summary>
    public static class ProgressIndicator
    {
        private static float _rotationAngle = 0f;
        private static float _lastUpdateTime = 0f;
        private static readonly float RotationSpeed = 180f; // градуси за секунду
        
        /// <summary>
        /// Анімований індикатор завантаження "крутящийся колесо"
        /// </summary>
        /// <param name="size">Розмір індикатора</param>
        public static void DrawSpinner(float size = 30f)
        {
            if (Event.current.type == EventType.Repaint)
            {
                // Оновлюємо кут обертання залежно від часу
                float deltaTime = (float)EditorApplication.timeSinceStartup - _lastUpdateTime;
                _lastUpdateTime = (float)EditorApplication.timeSinceStartup;
                _rotationAngle += RotationSpeed * deltaTime;
                _rotationAngle %= 360f;
                
                // Малюємо індикатор
                Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                
                // Запам'ятовуємо поточну матрицю GUI
                Matrix4x4 orgMatrix = GUI.matrix;
                
                // Налаштовуємо матрицю для обертання
                Vector2 pivotPoint = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                GUIUtility.RotateAroundPivot(_rotationAngle, pivotPoint);
                
                // Малюємо колесо
                Color originalColor = GUI.color;
                GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                
                Texture2D wheelTexture = EditorGUIUtility.FindTexture("d_WaitSpin00");
                if (wheelTexture != null)
                {
                    GUI.DrawTexture(rect, wheelTexture);
                }
                else
                {
                    // Якщо текстура не знайдена, малюємо простий круг
                    DrawSimpleSpinner(rect);
                }
                
                // Відновлюємо матрицю і колір
                GUI.matrix = orgMatrix;
                GUI.color = originalColor;
            }
            else
            {
                // Виділяємо місце для індикатора
                GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            }
            
            // Викликаємо перемальовування вікна
            EditorUtility.SetDirty(EditorWindow.focusedWindow);
        }
        
        /// <summary>
        /// Малює анімований текстовий індикатор з крапками
        /// </summary>
        /// <param name="baseText">Базовий текст повідомлення</param>
        public static void DrawTextProgress(string baseText = "Очікування")
        {
            int dotsCount = Mathf.FloorToInt(((float)EditorApplication.timeSinceStartup * 2) % 4);
            
            string text = baseText;
            for (int i = 0; i < dotsCount; i++)
            {
                text += ".";
            }
            
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            
            // Викликаємо перемальовування вікна
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }
        
        /// <summary>
        /// Малює простий індикатор прогресу без використання текстури
        /// </summary>
        private static void DrawSimpleSpinner(Rect rect)
        {
            int segmentCount = 8;
            float segmentAngle = 360f / segmentCount;
            
            for (int i = 0; i < segmentCount; i++)
            {
                float startAngle = i * segmentAngle;
                float opacity = 0.3f + 0.7f * (1.0f - ((i * 1.0f) / segmentCount));
                
                Color segmentColor = new Color(1, 1, 1, opacity);
                Handles.color = segmentColor;
                
                Vector2 center = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                float radius = rect.width / 2 - 2;
                
                Vector2 start = new Vector2(
                    center.x + Mathf.Cos((startAngle) * Mathf.Deg2Rad) * radius,
                    center.y + Mathf.Sin((startAngle) * Mathf.Deg2Rad) * radius
                );
                
                Vector2 end = new Vector2(
                    center.x + Mathf.Cos((startAngle + segmentAngle * 0.8f) * Mathf.Deg2Rad) * radius,
                    center.y + Mathf.Sin((startAngle + segmentAngle * 0.8f) * Mathf.Deg2Rad) * radius
                );
                
                Handles.DrawLine(start, end);
            }
        }
    }
}
