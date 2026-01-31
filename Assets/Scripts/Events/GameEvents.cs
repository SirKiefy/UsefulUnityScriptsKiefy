namespace UsefulScripts.Events
{
    /// <summary>
    /// Common game events for use with EventManager.
    /// Extend this with your own events as structs.
    /// </summary>
    
    // Player Events
    public struct PlayerSpawnedEvent
    {
        public UnityEngine.GameObject Player;
        public UnityEngine.Vector3 Position;
    }

    public struct PlayerDiedEvent
    {
        public UnityEngine.GameObject Player;
        public UnityEngine.GameObject Killer;
        public float DamageAmount;
    }

    public struct PlayerDamagedEvent
    {
        public UnityEngine.GameObject Player;
        public float Damage;
        public float CurrentHealth;
        public float MaxHealth;
    }

    public struct PlayerHealedEvent
    {
        public UnityEngine.GameObject Player;
        public float Amount;
        public float CurrentHealth;
    }

    // Game Events
    public struct GameStartedEvent
    {
        public int Level;
    }

    public struct GameOverEvent
    {
        public bool Won;
        public int Score;
        public float TimePlayed;
    }

    public struct GamePausedEvent
    {
        public bool IsPaused;
    }

    public struct LevelLoadedEvent
    {
        public string LevelName;
        public int LevelIndex;
    }

    public struct LevelCompletedEvent
    {
        public int LevelIndex;
        public float CompletionTime;
        public int Score;
        public int Stars;
    }

    // Score Events
    public struct ScoreChangedEvent
    {
        public int OldScore;
        public int NewScore;
        public int Delta;
    }

    public struct HighScoreEvent
    {
        public int Score;
        public int Rank;
    }

    // Pickup/Item Events
    public struct ItemPickedUpEvent
    {
        public string ItemId;
        public string ItemName;
        public UnityEngine.GameObject Item;
    }

    public struct ItemUsedEvent
    {
        public string ItemId;
        public UnityEngine.GameObject User;
    }

    public struct CurrencyChangedEvent
    {
        public string CurrencyType;
        public int OldAmount;
        public int NewAmount;
    }

    // Combat Events
    public struct EnemyKilledEvent
    {
        public UnityEngine.GameObject Enemy;
        public UnityEngine.GameObject Killer;
        public int PointsAwarded;
    }

    public struct WeaponFiredEvent
    {
        public string WeaponName;
        public UnityEngine.Vector3 Position;
        public UnityEngine.Vector3 Direction;
    }

    // Achievement Events
    public struct AchievementUnlockedEvent
    {
        public string AchievementId;
        public string AchievementName;
        public string Description;
    }

    // Audio Events
    public struct PlaySoundEvent
    {
        public string SoundName;
        public UnityEngine.Vector3 Position;
        public float Volume;
    }

    public struct PlayMusicEvent
    {
        public string TrackName;
        public bool Crossfade;
    }

    // UI Events
    public struct UIOpenedEvent
    {
        public string PanelName;
    }

    public struct UIClosedEvent
    {
        public string PanelName;
    }

    public struct NotificationEvent
    {
        public string Message;
        public NotificationType Type;
        public float Duration;
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    // Dialogue Events
    public struct DialogueStartedEvent
    {
        public string DialogueId;
        public string SpeakerName;
    }

    public struct DialogueEndedEvent
    {
        public string DialogueId;
    }

    public struct DialogueChoiceMadeEvent
    {
        public string DialogueId;
        public int ChoiceIndex;
        public string ChoiceText;
    }
}
