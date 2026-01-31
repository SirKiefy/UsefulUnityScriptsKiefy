using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Quest
{
    /// <summary>
    /// Represents a quest definition.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "UsefulScripts/Quest/Quest")]
    public class QuestData : ScriptableObject
    {
        [Header("Basic Info")]
        public string questId;
        public string questName;
        [TextArea(3, 10)]
        public string description;
        public Sprite icon;
        
        [Header("Quest Type")]
        public QuestType questType = QuestType.Main;
        public QuestCategory category = QuestCategory.Story;
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public List<string> prerequisiteQuestIds = new List<string>();
        public bool isRepeatable = false;
        public float repeatCooldown = 0f;
        
        [Header("Objectives")]
        public List<QuestObjective> objectives = new List<QuestObjective>();
        
        [Header("Rewards")]
        public int experienceReward = 100;
        public int currencyReward = 50;
        public List<QuestItemReward> itemRewards = new List<QuestItemReward>();
        public List<string> unlockedQuestIds = new List<string>();
        
        [Header("Dialogue")]
        public string acceptDialogueId;
        public string progressDialogueId;
        public string completeDialogueId;
        
        [Header("Time Limit")]
        public bool hasTimeLimit = false;
        public float timeLimit = 300f; // seconds
        
        [Header("Failure Conditions")]
        public bool failOnDeath = false;
        public bool failOnZoneLeave = false;
    }
    
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Weekly,
        Event,
        Hidden
    }
    
    public enum QuestCategory
    {
        Story,
        Combat,
        Exploration,
        Collection,
        Crafting,
        Social,
        Achievement
    }
    
    /// <summary>
    /// Represents a single objective within a quest.
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType objectiveType;
        public string targetId;
        public int requiredAmount = 1;
        public bool isOptional = false;
        public bool isHidden = false;
        
        [Header("Location")]
        public string locationId;
        public Vector3 markerPosition;
        public float markerRadius = 5f;
    }
    
    public enum ObjectiveType
    {
        // Kill/Combat
        Kill,
        KillBoss,
        DefeatInTime,
        SurviveWaves,
        
        // Collection
        Collect,
        Gather,
        Loot,
        Mine,
        Fish,
        
        // Interaction
        Talk,
        Interact,
        Deliver,
        Escort,
        
        // Exploration
        Discover,
        Reach,
        Explore,
        Scout,
        
        // Crafting
        Craft,
        Upgrade,
        Enchant,
        
        // Misc
        Use,
        Equip,
        Complete,
        Custom
    }
    
    /// <summary>
    /// Represents an item reward from a quest.
    /// </summary>
    [Serializable]
    public class QuestItemReward
    {
        public string itemId;
        public int quantity = 1;
        public float dropChance = 1f;
    }
    
    /// <summary>
    /// Represents the runtime state of an active quest.
    /// </summary>
    [Serializable]
    public class ActiveQuest
    {
        public QuestData questData;
        public QuestStatus status;
        public Dictionary<string, int> objectiveProgress;
        public float startTime;
        public float timeRemaining;
        
        public ActiveQuest(QuestData data)
        {
            questData = data;
            status = QuestStatus.InProgress;
            objectiveProgress = new Dictionary<string, int>();
            startTime = Time.time;
            
            foreach (var objective in data.objectives)
            {
                objectiveProgress[objective.objectiveId] = 0;
            }
            
            if (data.hasTimeLimit)
            {
                timeRemaining = data.timeLimit;
            }
        }
        
        public bool IsObjectiveComplete(string objectiveId)
        {
            var objective = questData.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective == null) return false;
            
            return objectiveProgress.TryGetValue(objectiveId, out int progress) && 
                   progress >= objective.requiredAmount;
        }
        
        public float GetObjectiveProgress(string objectiveId)
        {
            var objective = questData.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective == null || objective.requiredAmount <= 0) return 0f;
            
            objectiveProgress.TryGetValue(objectiveId, out int progress);
            return Mathf.Clamp01((float)progress / objective.requiredAmount);
        }
        
        public bool AreAllRequiredObjectivesComplete()
        {
            return questData.objectives
                .Where(o => !o.isOptional)
                .All(o => IsObjectiveComplete(o.objectiveId));
        }
        
        public int GetCompletedOptionalCount()
        {
            return questData.objectives
                .Count(o => o.isOptional && IsObjectiveComplete(o.objectiveId));
        }
    }
    
    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        ReadyToComplete,
        Completed,
        Failed,
        Abandoned
    }
    
    /// <summary>
    /// Complete quest system managing quest tracking, progress, and rewards.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }
        
        [Header("Quest Settings")]
        [SerializeField] private int maxActiveQuests = 20;
        [SerializeField] private bool autoTrackNewQuests = true;
        
        [Header("Quest Database")]
        [SerializeField] private List<QuestData> allQuests = new List<QuestData>();
        
        private Dictionary<string, ActiveQuest> activeQuests = new Dictionary<string, ActiveQuest>();
        private HashSet<string> completedQuestIds = new HashSet<string>();
        private HashSet<string> failedQuestIds = new HashSet<string>();
        private Dictionary<string, float> repeatableCooldowns = new Dictionary<string, float>();
        private string trackedQuestId;
        private int currentPlayerLevel = 1;
        
        // Events
        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestFailed;
        public event Action<QuestData> OnQuestAbandoned;
        public event Action<QuestData, string, int, int> OnObjectiveProgress;
        public event Action<QuestData, string> OnObjectiveCompleted;
        public event Action<QuestData> OnQuestReadyToComplete;
        public event Action<string> OnQuestTracked;
        
        // Properties
        public int ActiveQuestCount => activeQuests.Count;
        public int CompletedQuestCount => completedQuestIds.Count;
        public ActiveQuest TrackedQuest => trackedQuestId != null && activeQuests.TryGetValue(trackedQuestId, out var q) ? q : null;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            UpdateTimedQuests();
            UpdateRepeatableCooldowns();
        }
        
        private void UpdateTimedQuests()
        {
            var timedQuests = activeQuests.Values.Where(q => q.questData.hasTimeLimit && q.status == QuestStatus.InProgress).ToList();
            
            foreach (var quest in timedQuests)
            {
                quest.timeRemaining -= Time.deltaTime;
                
                if (quest.timeRemaining <= 0)
                {
                    FailQuest(quest.questData.questId);
                }
            }
        }
        
        private void UpdateRepeatableCooldowns()
        {
            var keys = repeatableCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                repeatableCooldowns[key] -= Time.deltaTime;
                if (repeatableCooldowns[key] <= 0)
                {
                    repeatableCooldowns.Remove(key);
                    completedQuestIds.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// Accepts a quest by its ID.
        /// </summary>
        public bool AcceptQuest(string questId)
        {
            var questData = allQuests.FirstOrDefault(q => q.questId == questId);
            if (questData == null) return false;
            
            return AcceptQuest(questData);
        }
        
        /// <summary>
        /// Accepts a quest.
        /// </summary>
        public bool AcceptQuest(QuestData questData)
        {
            if (!CanAcceptQuest(questData)) return false;
            
            var activeQuest = new ActiveQuest(questData);
            activeQuests[questData.questId] = activeQuest;
            
            if (autoTrackNewQuests && string.IsNullOrEmpty(trackedQuestId))
            {
                TrackQuest(questData.questId);
            }
            
            OnQuestAccepted?.Invoke(questData);
            return true;
        }
        
        /// <summary>
        /// Checks if a quest can be accepted.
        /// </summary>
        public bool CanAcceptQuest(QuestData questData)
        {
            if (questData == null) return false;
            if (activeQuests.ContainsKey(questData.questId)) return false;
            if (activeQuests.Count >= maxActiveQuests) return false;
            if (questData.requiredLevel > currentPlayerLevel) return false;
            
            // Check if already completed (and not repeatable or on cooldown)
            if (completedQuestIds.Contains(questData.questId))
            {
                if (!questData.isRepeatable) return false;
                if (repeatableCooldowns.ContainsKey(questData.questId)) return false;
            }
            
            // Check prerequisites
            foreach (var prereqId in questData.prerequisiteQuestIds)
            {
                if (!completedQuestIds.Contains(prereqId)) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Updates progress on an objective.
        /// </summary>
        public void UpdateObjective(ObjectiveType type, string targetId, int amount = 1)
        {
            foreach (var activeQuest in activeQuests.Values.Where(q => q.status == QuestStatus.InProgress))
            {
                foreach (var objective in activeQuest.questData.objectives)
                {
                    if (objective.objectiveType == type && objective.targetId == targetId)
                    {
                        UpdateObjectiveProgress(activeQuest.questData.questId, objective.objectiveId, amount);
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates progress on a specific objective.
        /// </summary>
        public void UpdateObjectiveProgress(string questId, string objectiveId, int amount = 1)
        {
            if (!activeQuests.TryGetValue(questId, out var activeQuest)) return;
            if (activeQuest.status != QuestStatus.InProgress) return;
            
            var objective = activeQuest.questData.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective == null) return;
            
            if (!activeQuest.objectiveProgress.ContainsKey(objectiveId))
            {
                activeQuest.objectiveProgress[objectiveId] = 0;
            }
            
            int previousProgress = activeQuest.objectiveProgress[objectiveId];
            activeQuest.objectiveProgress[objectiveId] = Mathf.Min(
                activeQuest.objectiveProgress[objectiveId] + amount,
                objective.requiredAmount
            );
            
            int newProgress = activeQuest.objectiveProgress[objectiveId];
            
            if (newProgress != previousProgress)
            {
                OnObjectiveProgress?.Invoke(activeQuest.questData, objectiveId, newProgress, objective.requiredAmount);
                
                if (newProgress >= objective.requiredAmount)
                {
                    OnObjectiveCompleted?.Invoke(activeQuest.questData, objectiveId);
                }
                
                // Check if quest is ready to complete
                if (activeQuest.AreAllRequiredObjectivesComplete())
                {
                    activeQuest.status = QuestStatus.ReadyToComplete;
                    OnQuestReadyToComplete?.Invoke(activeQuest.questData);
                }
            }
        }
        
        /// <summary>
        /// Sets an objective to a specific progress value.
        /// </summary>
        public void SetObjectiveProgress(string questId, string objectiveId, int progress)
        {
            if (!activeQuests.TryGetValue(questId, out var activeQuest)) return;
            if (activeQuest.status != QuestStatus.InProgress) return;
            
            var objective = activeQuest.questData.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective == null) return;
            
            int clampedProgress = Mathf.Clamp(progress, 0, objective.requiredAmount);
            activeQuest.objectiveProgress[objectiveId] = clampedProgress;
            
            OnObjectiveProgress?.Invoke(activeQuest.questData, objectiveId, clampedProgress, objective.requiredAmount);
            
            if (clampedProgress >= objective.requiredAmount)
            {
                OnObjectiveCompleted?.Invoke(activeQuest.questData, objectiveId);
            }
            
            if (activeQuest.AreAllRequiredObjectivesComplete())
            {
                activeQuest.status = QuestStatus.ReadyToComplete;
                OnQuestReadyToComplete?.Invoke(activeQuest.questData);
            }
        }
        
        /// <summary>
        /// Completes a quest and grants rewards.
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var activeQuest)) return false;
            if (!activeQuest.AreAllRequiredObjectivesComplete()) return false;
            
            var questData = activeQuest.questData;
            
            // Grant rewards
            GrantRewards(questData, activeQuest.GetCompletedOptionalCount());
            
            // Mark as completed
            activeQuest.status = QuestStatus.Completed;
            completedQuestIds.Add(questId);
            activeQuests.Remove(questId);
            
            // Handle repeatable quests
            if (questData.isRepeatable && questData.repeatCooldown > 0)
            {
                repeatableCooldowns[questId] = questData.repeatCooldown;
            }
            
            // Update tracking
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Keys.FirstOrDefault();
                if (trackedQuestId != null)
                {
                    OnQuestTracked?.Invoke(trackedQuestId);
                }
            }
            
            OnQuestCompleted?.Invoke(questData);
            return true;
        }
        
        private void GrantRewards(QuestData questData, int optionalObjectivesCompleted)
        {
            // Base rewards
            Debug.Log($"Granted {questData.experienceReward} XP from quest: {questData.questName}");
            Debug.Log($"Granted {questData.currencyReward} currency from quest: {questData.questName}");
            
            // Item rewards
            foreach (var itemReward in questData.itemRewards)
            {
                if (UnityEngine.Random.value <= itemReward.dropChance)
                {
                    Debug.Log($"Granted {itemReward.quantity}x {itemReward.itemId} from quest: {questData.questName}");
                }
            }
            
            // Bonus for optional objectives
            if (optionalObjectivesCompleted > 0)
            {
                int bonusXP = questData.experienceReward * optionalObjectivesCompleted / 10;
                int bonusCurrency = questData.currencyReward * optionalObjectivesCompleted / 10;
                Debug.Log($"Bonus rewards for completing {optionalObjectivesCompleted} optional objectives: {bonusXP} XP, {bonusCurrency} currency");
            }
        }
        
        /// <summary>
        /// Fails a quest.
        /// </summary>
        public bool FailQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var activeQuest)) return false;
            
            activeQuest.status = QuestStatus.Failed;
            failedQuestIds.Add(questId);
            activeQuests.Remove(questId);
            
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Keys.FirstOrDefault();
            }
            
            OnQuestFailed?.Invoke(activeQuest.questData);
            return true;
        }
        
        /// <summary>
        /// Abandons a quest.
        /// </summary>
        public bool AbandonQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var activeQuest)) return false;
            
            activeQuest.status = QuestStatus.Abandoned;
            activeQuests.Remove(questId);
            
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Keys.FirstOrDefault();
            }
            
            OnQuestAbandoned?.Invoke(activeQuest.questData);
            return true;
        }
        
        /// <summary>
        /// Tracks a quest for UI display.
        /// </summary>
        public void TrackQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId)) return;
            
            trackedQuestId = questId;
            OnQuestTracked?.Invoke(questId);
        }
        
        /// <summary>
        /// Gets an active quest by ID.
        /// </summary>
        public ActiveQuest GetActiveQuest(string questId)
        {
            return activeQuests.TryGetValue(questId, out var quest) ? quest : null;
        }
        
        /// <summary>
        /// Gets all active quests.
        /// </summary>
        public List<ActiveQuest> GetActiveQuests()
        {
            return new List<ActiveQuest>(activeQuests.Values);
        }
        
        /// <summary>
        /// Gets all active quests of a specific type.
        /// </summary>
        public List<ActiveQuest> GetActiveQuestsByType(QuestType type)
        {
            return activeQuests.Values.Where(q => q.questData.questType == type).ToList();
        }
        
        /// <summary>
        /// Checks if a quest is completed.
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return completedQuestIds.Contains(questId);
        }
        
        /// <summary>
        /// Checks if a quest is currently active.
        /// </summary>
        public bool IsQuestActive(string questId)
        {
            return activeQuests.ContainsKey(questId);
        }
        
        /// <summary>
        /// Gets all available quests that can be accepted.
        /// </summary>
        public List<QuestData> GetAvailableQuests()
        {
            return allQuests.Where(q => CanAcceptQuest(q)).ToList();
        }
        
        /// <summary>
        /// Gets all quests ready to be completed (turn in).
        /// </summary>
        public List<ActiveQuest> GetReadyToCompleteQuests()
        {
            return activeQuests.Values.Where(q => q.status == QuestStatus.ReadyToComplete).ToList();
        }
        
        /// <summary>
        /// Sets the player level for quest availability checks.
        /// </summary>
        public void SetPlayerLevel(int level)
        {
            currentPlayerLevel = Mathf.Max(1, level);
        }
        
        /// <summary>
        /// Handles player death - fails quests that fail on death.
        /// </summary>
        public void OnPlayerDeath()
        {
            var questsToFail = activeQuests.Values
                .Where(q => q.questData.failOnDeath && q.status == QuestStatus.InProgress)
                .Select(q => q.questData.questId)
                .ToList();
            
            foreach (var questId in questsToFail)
            {
                FailQuest(questId);
            }
        }
        
        /// <summary>
        /// Handles zone change - fails quests that fail on zone leave.
        /// </summary>
        public void OnZoneChanged(string newZoneId)
        {
            var questsToFail = activeQuests.Values
                .Where(q => q.questData.failOnZoneLeave && q.status == QuestStatus.InProgress)
                .Select(q => q.questData.questId)
                .ToList();
            
            foreach (var questId in questsToFail)
            {
                FailQuest(questId);
            }
        }
        
        /// <summary>
        /// Registers a quest in the system.
        /// </summary>
        public void RegisterQuest(QuestData questData)
        {
            if (questData != null && !allQuests.Contains(questData))
            {
                allQuests.Add(questData);
            }
        }
        
        /// <summary>
        /// Gets quest completion stats.
        /// </summary>
        public QuestStats GetQuestStats()
        {
            return new QuestStats
            {
                TotalAvailable = allQuests.Count,
                Completed = completedQuestIds.Count,
                Failed = failedQuestIds.Count,
                Active = activeQuests.Count,
                MainCompleted = completedQuestIds.Count(id => allQuests.Any(q => q.questId == id && q.questType == QuestType.Main)),
                SideCompleted = completedQuestIds.Count(id => allQuests.Any(q => q.questId == id && q.questType == QuestType.Side))
            };
        }
    }
    
    [Serializable]
    public class QuestStats
    {
        public int TotalAvailable;
        public int Completed;
        public int Failed;
        public int Active;
        public int MainCompleted;
        public int SideCompleted;
        
        public float CompletionPercentage => TotalAvailable > 0 ? (float)Completed / TotalAvailable * 100f : 0f;
    }
}