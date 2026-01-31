using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UsefulScripts.SaveSystem
{
    /// <summary>
    /// Universal save/load system with multiple storage options.
    /// </summary>
    public static class SaveManager
    {
        public enum SaveLocation
        {
            PersistentData,
            StreamingAssets,
            PlayerPrefs
        }

        private static string savePath = Application.persistentDataPath;
        private static string defaultFileName = "savedata.json";

        // Events
        public static event System.Action<string> OnGameSaved;
        public static event System.Action<string> OnGameLoaded;

        /// <summary>
        /// Save data to a JSON file
        /// </summary>
        public static void Save<T>(T data, string fileName = null, SaveLocation location = SaveLocation.PersistentData)
        {
            fileName ??= defaultFileName;
            string json = JsonUtility.ToJson(data, true);

            switch (location)
            {
                case SaveLocation.PersistentData:
                    string fullPath = Path.Combine(savePath, fileName);
                    File.WriteAllText(fullPath, json);
                    break;

                case SaveLocation.PlayerPrefs:
                    PlayerPrefs.SetString(fileName, json);
                    PlayerPrefs.Save();
                    break;

                case SaveLocation.StreamingAssets:
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
                    File.WriteAllText(streamingPath, json);
                    break;
            }

            OnGameSaved?.Invoke(fileName);
        }

        /// <summary>
        /// Load data from a JSON file
        /// </summary>
        public static T Load<T>(string fileName = null, SaveLocation location = SaveLocation.PersistentData) where T : new()
        {
            fileName ??= defaultFileName;
            string json = null;

            switch (location)
            {
                case SaveLocation.PersistentData:
                    string fullPath = Path.Combine(savePath, fileName);
                    if (File.Exists(fullPath))
                    {
                        json = File.ReadAllText(fullPath);
                    }
                    break;

                case SaveLocation.PlayerPrefs:
                    if (PlayerPrefs.HasKey(fileName))
                    {
                        json = PlayerPrefs.GetString(fileName);
                    }
                    break;

                case SaveLocation.StreamingAssets:
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
                    if (File.Exists(streamingPath))
                    {
                        json = File.ReadAllText(streamingPath);
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(json))
            {
                T data = JsonUtility.FromJson<T>(json);
                OnGameLoaded?.Invoke(fileName);
                return data;
            }

            return new T();
        }

        /// <summary>
        /// Check if a save file exists
        /// </summary>
        public static bool SaveExists(string fileName = null, SaveLocation location = SaveLocation.PersistentData)
        {
            fileName ??= defaultFileName;

            switch (location)
            {
                case SaveLocation.PersistentData:
                    return File.Exists(Path.Combine(savePath, fileName));

                case SaveLocation.PlayerPrefs:
                    return PlayerPrefs.HasKey(fileName);

                case SaveLocation.StreamingAssets:
                    return File.Exists(Path.Combine(Application.streamingAssetsPath, fileName));
            }

            return false;
        }

        /// <summary>
        /// Delete a save file
        /// </summary>
        public static void DeleteSave(string fileName = null, SaveLocation location = SaveLocation.PersistentData)
        {
            fileName ??= defaultFileName;

            switch (location)
            {
                case SaveLocation.PersistentData:
                    string fullPath = Path.Combine(savePath, fileName);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                    break;

                case SaveLocation.PlayerPrefs:
                    PlayerPrefs.DeleteKey(fileName);
                    break;

                case SaveLocation.StreamingAssets:
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
                    if (File.Exists(streamingPath))
                    {
                        File.Delete(streamingPath);
                    }
                    break;
            }
        }

        /// <summary>
        /// Get all save files
        /// </summary>
        public static string[] GetAllSaveFiles(string pattern = "*.json")
        {
            if (Directory.Exists(savePath))
            {
                return Directory.GetFiles(savePath, pattern);
            }
            return new string[0];
        }

        /// <summary>
        /// Quick save a value to PlayerPrefs
        /// </summary>
        public static void SaveValue<T>(string key, T value)
        {
            switch (value)
            {
                case int i:
                    PlayerPrefs.SetInt(key, i);
                    break;
                case float f:
                    PlayerPrefs.SetFloat(key, f);
                    break;
                case string s:
                    PlayerPrefs.SetString(key, s);
                    break;
                case bool b:
                    PlayerPrefs.SetInt(key, b ? 1 : 0);
                    break;
                default:
                    PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
                    break;
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Quick load a value from PlayerPrefs
        /// </summary>
        public static T LoadValue<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }

            object result = defaultValue;

            if (typeof(T) == typeof(int))
            {
                result = PlayerPrefs.GetInt(key);
            }
            else if (typeof(T) == typeof(float))
            {
                result = PlayerPrefs.GetFloat(key);
            }
            else if (typeof(T) == typeof(string))
            {
                result = PlayerPrefs.GetString(key);
            }
            else if (typeof(T) == typeof(bool))
            {
                result = PlayerPrefs.GetInt(key) == 1;
            }
            else
            {
                string json = PlayerPrefs.GetString(key);
                if (!string.IsNullOrEmpty(json))
                {
                    result = JsonUtility.FromJson<T>(json);
                }
            }

            return (T)result;
        }

        /// <summary>
        /// Clear all PlayerPrefs
        /// </summary>
        public static void ClearAllPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
