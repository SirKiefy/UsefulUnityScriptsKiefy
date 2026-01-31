using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Achievement
{
    /// <summary>
    /// Represents an achievement definition.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "UsefulScripts/Achievement/Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Header("Basic Info")]
        public string achievementId;
        public string title;
        [TextArea(2, 5)]
        public string description;
        public Sprite icon;
        public Sprite lockedIcon;
        
        [Header("Category")]
        public AchievementCategory category = AchievementCategory.General;
        public AchievementRarity rarity = AchievementRarity.Common;
        
        [Header("Progress Settings")]
        public AchievementType achievementType = AchievementType.Single;
        public int requiredProgress = 1;
        public bool showProgress = true;
        
        [Header("Unlock Conditions")]
        public List<AchievementCondition> conditions = new List<AchievementCondition>();
        public List<string> prerequisiteAchievementIds = new List<string>();
        
        [Header("Rewards")]
        public int pointsReward = 10;
        public int currencyReward = 0;
        public List<AchievementItemReward> itemRewards = new List<AchievementItemReward>();
        public List<string> unlockableIds = new List<string>(); // Skins, titles, etc.
        
        [Header("Hidden/Secret")]
        public bool isHidden = false;
        public string hiddenTitle = "???";
        public string hiddenDescription = "Complete certain tasks to unlock.";
        
        [Header("Display")]
        public bool showNotification = true;
        public AudioClip unlockSound;
        public GameObject unlockVFX;
    }
    
    public enum AchievementCategory
    {
        General,
        Story,
        Combat,
        Exploration,
        Collection,
        Skill,
        Social,
        Challenge,
        Secret
    }
    
    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum AchievementType
    {
        Single,     // One-time trigger
        Cumulative, // Track progress over time
        Milestone   // Multiple tiers (10, 50, 100 kills, etc.)
    }
    
    /// <summary>
    /// Represents a condition for achievement unlock.
    /// </summary>
    [Serializable]
    public class AchievementCondition
    {
        public ConditionType conditionType;
        public string targetId;
        public float targetValue;
        public ComparisonType comparison = ComparisonType.GreaterOrEqual;
    }
    
    public enum ConditionType
    {
        // Stats
        TotalKills,
        TotalDeaths,
        TotalPlayTime,
        MaxLevel,
        TotalDamageDealt,
        TotalDamageTaken,
        TotalHealing,
        
        // Collection
        ItemsCollected,
        SpecificItemCollected,
        GoldEarned,
        GoldSpent,
        
        // Combat
        KillSpecificEnemy,
        KillsWithWeapon,
        ConsecutiveKills,
        KillsWithoutDamage,
        BossesDefeated,
        
        // Exploration
        LocationsDiscovered,
        DistanceTraveled,
        SecretsFound,
        
        // Quests
        QuestsCompleted,
        SpecificQuestCompleted,
        SideQuestsCompleted,
        
        // Skills
        AbilitiesUnlocked,
        SkillsMaxed,
        
        // Misc
        GamesPlayed,
        Wins,
        Losses,
        Custom
    }
    
    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual
    }
    
    /// <summary>
    /// Item reward from achievement.
    /// </summary>
    [Serializable]
    public class AchievementItemReward
    {
        public string itemId;
        public int quantity = 1;
    }
    
    /// <summary>
    /// Runtime state of an achievement.
    /// </summary>
    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public int currentProgress;
        public bool isUnlocked;
        public DateTime unlockTime;
        public int currentTier; // For milestone achievements
        
        public AchievementProgress(string id)
        {
            achievementId = id;
            currentProgress = 0;
            isUnlocked = false;
            currentTier = 0;
        }
    }
    
    /// <summary>
    /// Stats tracker for achievement conditions.
    /// </summary>
    [Serializable]
    public class AchievementStats
    {
        public Dictionary<string, float> numericStats = new Dictionary<string, float>();
        public Dictionary<string, bool> flagStats = new Dictionary<string, bool>();
        public Dictionary<string, int> countStats = new Dictionary<string, int>();
        
        public float GetStat(string key, float defaultValue = 0f)
        {
            return numericStats.TryGetValue(key, out float value) ? value : defaultValue;
        }
        
        public void SetStat(string key, float value)
        {
            numericStats[key] = value;
        }
        
        public void IncrementStat(string key, float amount = 1f)
        {
            if (!numericStats.ContainsKey(key))
            {
                numericStats[key] = 0f;
            }
            numericStats[key] += amount;
        }
        
        public bool GetFlag(string key)
        {
            return flagStats.TryGetValue(key, out bool value) && value;
        }
        
        public void SetFlag(string key, bool value)
        {
            flagStats[key] = value;
        }
        
        public int GetCount(string key)
        {
            return countStats.TryGetValue(key, out int value) ? value : 0;
        }
        
        public void IncrementCount(string key, int amount = 1)
        {
            if (!countStats.ContainsKey(key))
            {
                countStats[key] = 0;
            }
            countStats[key] += amount;
        }
    }
    
    /// <summary>
    /// Complete achievement system with progress tracking and notifications.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }
        
        [Header("Achievement Database")]
        [SerializeField] private List<AchievementData> allAchievements = new List<AchievementData>();
        
        [Header("Notification Settings")]
        [SerializeField] private float notificationDuration = 5f;
        [SerializeField] private bool enableNotifications = true;
        
        private Dictionary<string, AchievementProgress> progress = new Dictionary<string, AchievementProgress>();
        private AchievementStats stats = new AchievementStats();
        private Queue<AchievementData> notificationQueue = new Queue<AchievementData>();
        private bool isShowingNotification = false;
        
        // Events
        public event Action<AchievementData> OnAchievementUnlocked;
        public event Action<AchievementData, int, int> OnAchievementProgress;
        public event Action<AchievementData> OnNotificationStart;
        public event Action OnNotificationEnd;
        public event Action<string, float> OnStatChanged;
        
        // Properties
        public int TotalAchievements => allAchievements.Count;
        public int UnlockedCount => progress.Values.Count(p => p.isUnlocked);
        public int TotalPoints => progress.Values.Where(p => p.isUnlocked)
            .Sum(p => allAchievements.FirstOrDefault(a => a.achievementId == p.achievementId)?.pointsReward ?? 0);
        public float CompletionPercentage => TotalAchievements > 0 ? (float)UnlockedCount / TotalAchievements * 100f : 0f;
        public AchievementStats Stats => stats;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeProgress();
        }
        
        private void Update()
        {
            ProcessNotificationQueue();
        }
        
        private void InitializeProgress()
        {
            foreach (var achievement in allAchievements)
            {
                if (!progress.ContainsKey(achievement.achievementId))
                {
                    progress[achievement.achievementId] = new AchievementProgress(achievement.achievementId);
                }
            }
        }
        
        /// <summary>
        /// Registers an achievement in the system.
        /// </summary>
        public void RegisterAchievement(AchievementData achievement)
        {
            if (achievement == null) return;
            
            if (!allAchievements.Contains(achievement))
            {
                allAchievements.Add(achievement);
            }
            
            if (!progress.ContainsKey(achievement.achievementId))
            {
                progress[achievement.achievementId] = new AchievementProgress(achievement.achievementId);
            }
        }
        
        /// <summary>
        /// Reports progress on an achievement.
        /// </summary>
        public void ReportProgress(string achievementId, int amount = 1)
        {
            if (!progress.TryGetValue(achievementId, out var achievementProgress)) return;
            if (achievementProgress.isUnlocked) return;
            
            var achievement = GetAchievement(achievementId);
            if (achievement == null) return;
            
            int previousProgress = achievementProgress.currentProgress;
            achievementProgress.currentProgress += amount;
            
            OnAchievementProgress?.Invoke(achievement, achievementProgress.currentProgress, achievement.requiredProgress);
            
            // Check for unlock
            if (achievementProgress.currentProgress >= achievement.requiredProgress)
            {
                UnlockAchievement(achievement);
            }
        }
        
        /// <summary>
        /// Sets the progress on an achievement to a specific value.
        /// </summary>
        public void SetProgress(string achievementId, int value)
        {
            if (!progress.TryGetValue(achievementId, out var achievementProgress)) return;
            if (achievementProgress.isUnlocked) return;
            
            var achievement = GetAchievement(achievementId);
            if (achievement == null) return;
            
            achievementProgress.currentProgress = value;
            
            OnAchievementProgress?.Invoke(achievement, value, achievement.requiredProgress);
            
            if (value >= achievement.requiredProgress)
            {
                UnlockAchievement(achievement);
            }
        }
        
        /// <summary>
        /// Directly unlocks an achievement.
        /// </summary>
        public void UnlockAchievement(string achievementId)
        {
            var achievement = GetAchievement(achievementId);
            if (achievement != null)
            {
                UnlockAchievement(achievement);
            }
        }
        
        private void UnlockAchievement(AchievementData achievement)
        {
            if (!progress.TryGetValue(achievement.achievementId, out var achievementProgress)) return;
            if (achievementProgress.isUnlocked) return;
            
            // Check prerequisites
            foreach (var prereqId in achievement.prerequisiteAchievementIds)
            {
                if (!IsUnlocked(prereqId)) return;
            }
            
            // Check conditions
            if (!CheckConditions(achievement)) return;
            
            achievementProgress.isUnlocked = true;
            achievementProgress.unlockTime = DateTime.Now;
            achievementProgress.currentProgress = achievement.requiredProgress;
            
            // Grant rewards
            GrantRewards(achievement);
            
            // Queue notification
            if (enableNotifications && achievement.showNotification)
            {
                notificationQueue.Enqueue(achievement);
            }
            
            OnAchievementUnlocked?.Invoke(achievement);
        }
        
        private bool CheckConditions(AchievementData achievement)
        {
            foreach (var condition in achievement.conditions)
            {
                float statValue = GetStatForCondition(condition);
                bool met = condition.comparison switch
                {
                    ComparisonType.Equal => Mathf.Approximately(statValue, condition.targetValue),
                    ComparisonType.GreaterThan => statValue > condition.targetValue,
                    ComparisonType.GreaterOrEqual => statValue >= condition.targetValue,
                    ComparisonType.LessThan => statValue < condition.targetValue,
                    ComparisonType.LessOrEqual => statValue <= condition.targetValue,
                    _ => false
                };
                
                if (!met) return false;
            }
            
            return true;
        }
        
        private float GetStatForCondition(AchievementCondition condition)
        {
            string statKey = condition.conditionType.ToString();
            if (!string.IsNullOrEmpty(condition.targetId))
            {
                statKey += "_" + condition.targetId;
            }
            
            return stats.GetStat(statKey);
        }
        
        private void GrantRewards(AchievementData achievement)
        {
            Debug.Log($"Achievement Unlocked: {achievement.title}");
            Debug.Log($"  Points: +{achievement.pointsReward}");
            
            if (achievement.currencyReward > 0)
            {
                Debug.Log($"  Currency: +{achievement.currencyReward}");
            }
            
            foreach (var itemReward in achievement.itemRewards)
            {
                Debug.Log($"  Item: {itemReward.quantity}x {itemReward.itemId}");
            }
            
            foreach (var unlockableId in achievement.unlockableIds)
            {
                Debug.Log($"  Unlocked: {unlockableId}");
            }
            
            // Play sound
            if (achievement.unlockSound != null)
            {
                AudioSource.PlayClipAtPoint(achievement.unlockSound, Camera.main?.transform.position ?? Vector3.zero);
            }
            
            // Spawn VFX
            if (achievement.unlockVFX != null)
            {
                Instantiate(achievement.unlockVFX, Camera.main?.transform.position ?? Vector3.zero, Quaternion.identity);
            }
        }
        
        private void ProcessNotificationQueue()
        {
            if (isShowingNotification || notificationQueue.Count == 0) return;
            
            var achievement = notificationQueue.Dequeue();
            StartCoroutine(ShowNotification(achievement));
        }
        
        private System.Collections.IEnumerator ShowNotification(AchievementData achievement)
        {
            isShowingNotification = true;
            OnNotificationStart?.Invoke(achievement);
            
            yield return new WaitForSeconds(notificationDuration);
            
            OnNotificationEnd?.Invoke();
            isShowingNotification = false;
        }
        
        /// <summary>
        /// Updates a statistic and checks for related achievements.
        /// </summary>
        public void UpdateStat(ConditionType statType, float value, string targetId = null)
        {
            string statKey = statType.ToString();
            if (!string.IsNullOrEmpty(targetId))
            {
                statKey += "_" + targetId;
            }
            
            stats.SetStat(statKey, value);
            OnStatChanged?.Invoke(statKey, value);
            
            // Check achievements that depend on this stat
            CheckAchievementsForStat(statType, targetId);
        }
        
        /// <summary>
        /// Increments a statistic and checks for related achievements.
        /// </summary>
        public void IncrementStat(ConditionType statType, float amount = 1f, string targetId = null)
        {
            string statKey = statType.ToString();
            if (!string.IsNullOrEmpty(targetId))
            {
                statKey += "_" + targetId;
            }
            
            stats.IncrementStat(statKey, amount);
            OnStatChanged?.Invoke(statKey, stats.GetStat(statKey));
            
            CheckAchievementsForStat(statType, targetId);
        }
        
        private void CheckAchievementsForStat(ConditionType statType, string targetId)
        {
            foreach (var achievement in allAchievements)
            {
                if (IsUnlocked(achievement.achievementId)) continue;
                
                // Check if any condition uses this stat
                bool usesThisStat = achievement.conditions.Any(c => 
                    c.conditionType == statType && 
                    (string.IsNullOrEmpty(c.targetId) || c.targetId == targetId));
                
                if (usesThisStat && CheckConditions(achievement))
                {
                    // For cumulative achievements, update progress
                    if (achievement.achievementType == AchievementType.Cumulative)
                    {
                        string statKey = statType.ToString();
                        if (!string.IsNullOrEmpty(targetId)) statKey += "_" + targetId;
                        
                        int progressValue = Mathf.FloorToInt(stats.GetStat(statKey));
                        SetProgress(achievement.achievementId, progressValue);
                    }
                    else
                    {
                        UnlockAchievement(achievement);
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if an achievement is unlocked.
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return progress.TryGetValue(achievementId, out var p) && p.isUnlocked;
        }
        
        /// <summary>
        /// Gets achievement data by ID.
        /// </summary>
        public AchievementData GetAchievement(string achievementId)
        {
            return allAchievements.FirstOrDefault(a => a.achievementId == achievementId);
        }
        
        /// <summary>
        /// Gets progress for an achievement.
        /// </summary>
        public AchievementProgress GetProgress(string achievementId)
        {
            return progress.TryGetValue(achievementId, out var p) ? p : null;
        }
        
        /// <summary>
        /// Gets all achievements.
        /// </summary>
        public List<AchievementData> GetAllAchievements()
        {
            return new List<AchievementData>(allAchievements);
        }
        
        /// <summary>
        /// Gets all achievements by category.
        /// </summary>
        public List<AchievementData> GetAchievementsByCategory(AchievementCategory category)
        {
            return allAchievements.Where(a => a.category == category).ToList();
        }
        
        /// <summary>
        /// Gets all unlocked achievements.
        /// </summary>
        public List<AchievementData> GetUnlockedAchievements()
        {
            return allAchievements.Where(a => IsUnlocked(a.achievementId)).ToList();
        }
        
        /// <summary>
        /// Gets all locked achievements (visible).
        /// </summary>
        public List<AchievementData> GetLockedAchievements(bool includeHidden = false)
        {
            return allAchievements.Where(a => 
                !IsUnlocked(a.achievementId) && 
                (includeHidden || !a.isHidden)).ToList();
        }
        
        /// <summary>
        /// Gets achievements closest to completion.
        /// </summary>
        public List<AchievementData> GetNearlyCompleteAchievements(int count = 5, float minProgress = 0.5f)
        {
            return allAchievements
                .Where(a => !IsUnlocked(a.achievementId))
                .Select(a => new { Achievement = a, Progress = GetProgressPercentage(a.achievementId) })
                .Where(x => x.Progress >= minProgress)
                .OrderByDescending(x => x.Progress)
                .Take(count)
                .Select(x => x.Achievement)
                .ToList();
        }
        
        /// <summary>
        /// Gets the progress percentage for an achievement.
        /// </summary>
        public float GetProgressPercentage(string achievementId)
        {
            if (!progress.TryGetValue(achievementId, out var p)) return 0f;
            if (p.isUnlocked) return 1f;
            
            var achievement = GetAchievement(achievementId);
            if (achievement == null || achievement.requiredProgress <= 0) return 0f;
            
            return Mathf.Clamp01((float)p.currentProgress / achievement.requiredProgress);
        }
        
        /// <summary>
        /// Gets recently unlocked achievements.
        /// </summary>
        public List<AchievementData> GetRecentlyUnlocked(int count = 5)
        {
            return progress.Values
                .Where(p => p.isUnlocked)
                .OrderByDescending(p => p.unlockTime)
                .Take(count)
                .Select(p => GetAchievement(p.achievementId))
                .Where(a => a != null)
                .ToList();
        }
        
        /// <summary>
        /// Gets achievement stats summary.
        /// </summary>
        public AchievementSummary GetSummary()
        {
            var summary = new AchievementSummary
            {
                TotalAchievements = TotalAchievements,
                UnlockedCount = UnlockedCount,
                TotalPoints = TotalPoints,
                CompletionPercentage = CompletionPercentage
            };
            
            foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
            {
                var categoryAchievements = allAchievements.Where(a => a.category == category).ToList();
                var unlockedInCategory = categoryAchievements.Count(a => IsUnlocked(a.achievementId));
                summary.CategoryProgress[category] = categoryAchievements.Count > 0 
                    ? (float)unlockedInCategory / categoryAchievements.Count 
                    : 1f;
            }
            
            foreach (AchievementRarity rarity in Enum.GetValues(typeof(AchievementRarity)))
            {
                var rarityAchievements = allAchievements.Where(a => a.rarity == rarity).ToList();
                var unlockedOfRarity = rarityAchievements.Count(a => IsUnlocked(a.achievementId));
                summary.RarityProgress[rarity] = rarityAchievements.Count > 0 
                    ? (float)unlockedOfRarity / rarityAchievements.Count 
                    : 1f;
            }
            
            return summary;
        }
        
        /// <summary>
        /// Resets all achievement progress.
        /// </summary>
        public void ResetAllProgress()
        {
            progress.Clear();
            stats = new AchievementStats();
            InitializeProgress();
        }
        
        /// <summary>
        /// Saves achievement progress to PlayerPrefs.
        /// </summary>
        public void SaveProgress()
        {
            foreach (var kvp in progress)
            {
                PlayerPrefs.SetInt($"Achievement_{kvp.Key}_Progress", kvp.Value.currentProgress);
                PlayerPrefs.SetInt($"Achievement_{kvp.Key}_Unlocked", kvp.Value.isUnlocked ? 1 : 0);
                if (kvp.Value.isUnlocked)
                {
                    PlayerPrefs.SetString($"Achievement_{kvp.Key}_UnlockTime", kvp.Value.unlockTime.ToBinary().ToString());
                }
            }
            
            // Save stats (simplified - in production use JSON)
            foreach (var kvp in stats.numericStats)
            {
                PlayerPrefs.SetFloat($"Stat_{kvp.Key}", kvp.Value);
            }
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loads achievement progress from PlayerPrefs.
        /// </summary>
        public void LoadProgress()
        {
            foreach (var achievement in allAchievements)
            {
                string id = achievement.achievementId;
                
                if (PlayerPrefs.HasKey($"Achievement_{id}_Progress"))
                {
                    progress[id].currentProgress = PlayerPrefs.GetInt($"Achievement_{id}_Progress");
                }
                if (PlayerPrefs.HasKey($"Achievement_{id}_Unlocked"))
                {
                    progress[id].isUnlocked = PlayerPrefs.GetInt($"Achievement_{id}_Unlocked") == 1;
                }
                if (PlayerPrefs.HasKey($"Achievement_{id}_UnlockTime"))
                {
                    string timeStr = PlayerPrefs.GetString($"Achievement_{id}_UnlockTime");
                    if (long.TryParse(timeStr, out long ticks))
                    {
                        progress[id].unlockTime = DateTime.FromBinary(ticks);
                    }
                }
            }
            
            // Load stats
            foreach (ConditionType condType in Enum.GetValues(typeof(ConditionType)))
            {
                string key = condType.ToString();
                if (PlayerPrefs.HasKey($"Stat_{key}"))
                {
                    stats.SetStat(key, PlayerPrefs.GetFloat($"Stat_{key}"));
                }
            }
        }
    }
    
    [Serializable]
    public class AchievementSummary
    {
        public int TotalAchievements;
        public int UnlockedCount;
        public int TotalPoints;
        public float CompletionPercentage;
        public Dictionary<AchievementCategory, float> CategoryProgress = new Dictionary<AchievementCategory, float>();
        public Dictionary<AchievementRarity, float> RarityProgress = new Dictionary<AchievementRarity, float>();
    }
}
