using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine; // For Debug.LogError, Debug.LogWarning, Debug.Log
using System.Linq; // For Skip

namespace UnityAIAgent
{
    public class ChatHistoryService
    {
        private readonly string _filePath;
        private readonly int _maxHistoryLength;
        private readonly JsonSerializerSettings _jsonSettings;

        public ChatHistoryService(string filePath, int maxHistoryLength)
        {
            _filePath = filePath;
            _maxHistoryLength = maxHistoryLength > 0 ? maxHistoryLength : 20; // Default to 20 if invalid
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented // For readable JSON file
            };
        }

        public void SaveHistory(List<string> history)
        {
            try
            {
                // Ensure history does not exceed max length
                var limitedHistory = history.Skip(Math.Max(0, history.Count - _maxHistoryLength)).ToList();
                string json = JsonConvert.SerializeObject(limitedHistory, _jsonSettings);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save chat history to '{_filePath}': {ex.Message}");
            }
        }

        public List<string> LoadHistory()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.Log($"Chat history file '{_filePath}' is empty or whitespace. Returning a new list.");
                        return new List<string>();
                    }
                    var history = JsonConvert.DeserializeObject<List<string>>(json);
                    return history ?? new List<string>(); // Ensure null is handled
                }
                Debug.Log($"Chat history file '{_filePath}' not found. A new history will be started.");
            }
            catch (JsonReaderException jex) // Specific exception for JSON parsing errors
            {
                Debug.LogError($"Failed to parse chat history JSON from '{_filePath}': {jex.Message}. The file might be corrupted. Backing up and creating new history.");
                try 
                {
                    string backupFilePath = _filePath + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}";
                    File.Move(_filePath, backupFilePath);
                    Debug.Log($"Corrupted history file backed up to '{backupFilePath}'.");
                } 
                catch (Exception moveEx) 
                { 
                    Debug.LogError($"Could not backup corrupted history file '{_filePath}': {moveEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load chat history from '{_filePath}': {ex.Message}");
            }
            return new List<string>(); // Return empty list if loading fails or file doesn't exist
        }

        public void ClearHistory()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    Debug.Log($"Chat history file '{_filePath}' has been cleared.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear chat history file '{_filePath}': {ex.Message}");
            }
        }
    }
}
