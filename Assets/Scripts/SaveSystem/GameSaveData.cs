using UnityEngine;
using System;

namespace UsefulScripts.SaveSystem
{
    /// <summary>
    /// Example save data structure. Copy and modify for your game.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        // Player Data
        public string playerName = "Player";
        public int playerLevel = 1;
        public float playerExperience = 0;
        public float currentHealth = 100;
        public float maxHealth = 100;

        // Position
        public SerializableVector3 lastPosition;
        public string lastScene = "";

        // Progress
        public int currentLevelIndex = 0;
        public bool[] levelsCompleted = new bool[10];
        public int[] levelStars = new int[10];
        public float totalPlayTime = 0;

        // Inventory
        public int coins = 0;
        public int gems = 0;
        public string[] inventoryItems = new string[0];

        // Settings (saved separately usually)
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;

        // Timestamps
        public string lastSaveTime;
        public string createdTime;

        public GameSaveData()
        {
            createdTime = DateTime.Now.ToString();
            lastSaveTime = createdTime;
        }

        public void UpdateSaveTime()
        {
            lastSaveTime = DateTime.Now.ToString();
        }
    }

    /// <summary>
    /// Serializable Vector3 for JSON serialization
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(SerializableVector3 sv) => sv.ToVector3();
        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v);
    }

    /// <summary>
    /// Serializable Quaternion for JSON serialization
    /// </summary>
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static implicit operator Quaternion(SerializableQuaternion sq) => sq.ToQuaternion();
        public static implicit operator SerializableQuaternion(Quaternion q) => new SerializableQuaternion(q);
    }

    /// <summary>
    /// Serializable Color for JSON serialization
    /// </summary>
    [Serializable]
    public struct SerializableColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SerializableColor(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }

        public static implicit operator Color(SerializableColor sc) => sc.ToColor();
        public static implicit operator SerializableColor(Color c) => new SerializableColor(c);
    }
}
