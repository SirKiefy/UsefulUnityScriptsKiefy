using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the type of relationship between characters.
    /// </summary>
    public enum RelationshipType
    {
        Stranger,
        Acquaintance,
        Friend,
        CloseFriend,
        BestFriend,
        Romantic,
        Engaged,
        Married,
        Rival,
        Enemy,
        Family,
        Mentor,
        Student
    }

    /// <summary>
    /// Defines faction standing levels.
    /// </summary>
    public enum FactionStanding
    {
        Hated,          // -1000 to -600
        Hostile,        // -599 to -300
        Unfriendly,     // -299 to -1
        Neutral,        // 0 to 299
        Friendly,       // 300 to 599
        Honored,        // 600 to 999
        Revered,        // 1000 to 1499
        Exalted         // 1500+
    }

    /// <summary>
    /// Defines the type of social interaction.
    /// </summary>
    public enum InteractionType
    {
        Talk,
        GiveGift,
        CompleteQuest,
        FightTogether,
        Betray,
        Help,
        Trade,
        Train,
        DateEvent,
        SpecialEvent,
        DailyGreeting
    }

    /// <summary>
    /// Represents an NPC's preferences for gifts.
    /// </summary>
    [Serializable]
    public class GiftPreferences
    {
        public List<string> lovedItems = new List<string>();      // +50 affinity
        public List<string> likedItems = new List<string>();      // +25 affinity
        public List<string> dislikedItems = new List<string>();   // -10 affinity
        public List<string> hatedItems = new List<string>();      // -30 affinity
        public List<string> lovedCategories = new List<string>(); // Item types loved
        public List<string> hatedCategories = new List<string>(); // Item types hated

        public int GetGiftAffinity(string itemId, string itemCategory)
        {
            if (lovedItems.Contains(itemId)) return 50;
            if (likedItems.Contains(itemId)) return 25;
            if (hatedItems.Contains(itemId)) return -30;
            if (dislikedItems.Contains(itemId)) return -10;

            if (lovedCategories.Contains(itemCategory)) return 20;
            if (hatedCategories.Contains(itemCategory)) return -15;

            return 5; // Neutral gift
        }
    }

    /// <summary>
    /// Represents a social link/support rank system.
    /// </summary>
    [Serializable]
    public class SocialLink
    {
        public string characterId;
        public string characterName;
        public int currentRank = 0;
        public int maxRank = 10;
        public int currentPoints = 0;
        public int pointsToNextRank = 100;
        public bool isMaxed = false;
        public bool isRomanceable = false;
        public bool isRomancing = false;
        public List<string> unlockedEvents = new List<string>();
        public DateTime lastInteraction;
        public int totalGiftsGiven = 0;
        public int totalTimeSpent = 0;

        public event Action<int> OnRankUp;
        public event Action<string> OnEventUnlocked;
        public event Action OnMaxRankReached;

        public float RankProgress => (float)currentPoints / pointsToNextRank;
        public bool CanRankUp => currentPoints >= pointsToNextRank && !isMaxed;

        /// <summary>
        /// Adds affinity points and handles rank ups.
        /// </summary>
        public int AddPoints(int amount)
        {
            if (isMaxed) return 0;

            currentPoints += amount;
            int ranksGained = 0;

            while (currentPoints >= pointsToNextRank && !isMaxed)
            {
                currentPoints -= pointsToNextRank;
                currentRank++;
                ranksGained++;
                pointsToNextRank = CalculatePointsForRank(currentRank + 1);
                OnRankUp?.Invoke(currentRank);

                if (currentRank >= maxRank)
                {
                    isMaxed = true;
                    OnMaxRankReached?.Invoke();
                }
            }

            lastInteraction = DateTime.Now;
            return ranksGained;
        }

        /// <summary>
        /// Calculates points required for a rank.
        /// </summary>
        public int CalculatePointsForRank(int rank)
        {
            return 100 + (rank * 50) + (int)(rank * rank * 10);
        }

        /// <summary>
        /// Unlocks a social event.
        /// </summary>
        public void UnlockEvent(string eventId)
        {
            if (!unlockedEvents.Contains(eventId))
            {
                unlockedEvents.Add(eventId);
                OnEventUnlocked?.Invoke(eventId);
            }
        }
    }

    /// <summary>
    /// Represents a faction in the game world.
    /// </summary>
    [Serializable]
    public class Faction
    {
        [Header("Basic Info")]
        public string factionId;
        public string factionName;
        [TextArea(2, 4)]
        public string description;
        public Sprite emblem;
        public Color factionColor = Color.gray;

        [Header("Relationships")]
        public List<string> alliedFactions = new List<string>();
        public List<string> enemyFactions = new List<string>();
        public List<string> neutralFactions = new List<string>();

        [Header("Reputation Thresholds")]
        public int hatedThreshold = -600;
        public int hostileThreshold = -300;
        public int unfriendlyThreshold = 0;
        public int neutralThreshold = 300;
        public int friendlyThreshold = 600;
        public int honoredThreshold = 1000;
        public int reveredThreshold = 1500;

        [Header("Rewards")]
        public List<FactionReward> rewards = new List<FactionReward>();

        public FactionStanding GetStanding(int reputation)
        {
            if (reputation >= reveredThreshold) return FactionStanding.Exalted;
            if (reputation >= honoredThreshold) return FactionStanding.Revered;
            if (reputation >= friendlyThreshold) return FactionStanding.Honored;
            if (reputation >= neutralThreshold) return FactionStanding.Friendly;
            if (reputation >= unfriendlyThreshold) return FactionStanding.Neutral;
            if (reputation >= hostileThreshold) return FactionStanding.Unfriendly;
            if (reputation >= hatedThreshold) return FactionStanding.Hostile;
            return FactionStanding.Hated;
        }
    }

    /// <summary>
    /// Represents a reward unlocked at a faction standing.
    /// </summary>
    [Serializable]
    public class FactionReward
    {
        public FactionStanding requiredStanding;
        public string rewardId;
        public string rewardName;
        public string description;
        public FactionRewardType rewardType;
        public string itemId;
        public int discount;
        public string recipeId;
        public string questId;
    }

    public enum FactionRewardType
    {
        Item,
        Discount,
        Recipe,
        Quest,
        Title,
        Mount,
        Companion,
        AccessToArea,
        Skill
    }

    /// <summary>
    /// Represents the player's reputation with a faction.
    /// </summary>
    [Serializable]
    public class FactionReputation
    {
        public string factionId;
        public int currentReputation = 0;
        public int totalReputationEarned = 0;
        public int totalReputationLost = 0;
        public FactionStanding currentStanding = FactionStanding.Neutral;
        public List<string> unlockedRewards = new List<string>();
        public DateTime lastChange;

        public event Action<FactionStanding, FactionStanding> OnStandingChanged;
        public event Action<string> OnRewardUnlocked;

        public void ModifyReputation(int amount, Faction faction)
        {
            var previousStanding = currentStanding;
            currentReputation += amount;

            if (amount > 0)
                totalReputationEarned += amount;
            else
                totalReputationLost -= amount;

            currentStanding = faction.GetStanding(currentReputation);
            lastChange = DateTime.Now;

            if (previousStanding != currentStanding)
            {
                OnStandingChanged?.Invoke(previousStanding, currentStanding);
            }

            // Check for new rewards
            foreach (var reward in faction.rewards)
            {
                if (!unlockedRewards.Contains(reward.rewardId) &&
                    (int)currentStanding >= (int)reward.requiredStanding)
                {
                    unlockedRewards.Add(reward.rewardId);
                    OnRewardUnlocked?.Invoke(reward.rewardId);
                }
            }
        }
    }

    /// <summary>
    /// Represents a romantic partner candidate.
    /// </summary>
    [Serializable]
    public class RomanceCandidate
    {
        public string characterId;
        public string characterName;
        public Sprite portrait;
        public bool isRomanceable = true;
        public bool requiresQuest = false;
        public string unlockQuestId;
        public int minRankForConfession = 8;
        public List<string> rivalCharacters = new List<string>();
        public GiftPreferences giftPreferences = new GiftPreferences();
        
        [Header("Personality")]
        public List<string> personalityTraits = new List<string>();
        public string favoriteLocation;
        public string birthday;

        [Header("Events")]
        public List<string> dateEventIds = new List<string>();
        public string confessionEventId;
        public string weddingEventId;
    }

    /// <summary>
    /// Represents a social event/scene that can be triggered.
    /// </summary>
    [Serializable]
    public class SocialEvent
    {
        public string eventId;
        public string eventName;
        public string characterId;
        public int requiredRank;
        public bool isRomanceEvent;
        public bool isOneTime = true;
        public bool hasBeenViewed;
        public List<string> requiredFlags = new List<string>();
        public int affinityReward = 20;
        public List<SocialEventChoice> choices = new List<SocialEventChoice>();
    }

    /// <summary>
    /// Represents a choice in a social event.
    /// </summary>
    [Serializable]
    public class SocialEventChoice
    {
        public string choiceText;
        public int affinityChange;
        public string resultFlag;
        public bool isCorrectChoice;
        public string responseDialogue;
    }

    /// <summary>
    /// Configuration for the relationship system.
    /// </summary>
    [CreateAssetMenu(fileName = "RelationshipConfig", menuName = "UsefulScripts/RPG/Relationship Config")]
    public class RelationshipConfig : ScriptableObject
    {
        [Header("Affinity Settings")]
        public int maxAffinity = 1000;
        public int minAffinity = -500;
        public int dailyAffinityDecay = 1;
        public int dailyInteractionLimit = 3;

        [Header("Gift Settings")]
        public float birthdayGiftMultiplier = 3f;
        public float duplicateGiftPenalty = 0.5f;
        public int maxGiftsPerDay = 2;

        [Header("Romance Settings")]
        public bool allowMultipleRomances = false;
        public int minLevelForRomance = 10;
        public int daysBeforeProposal = 30;

        [Header("Faction Settings")]
        public float alliedFactionRepBonus = 0.25f;
        public float enemyFactionRepPenalty = 0.5f;

        [Header("Characters")]
        public List<RomanceCandidate> romanceCandidates = new List<RomanceCandidate>();
        public List<SocialEvent> socialEvents = new List<SocialEvent>();

        [Header("Factions")]
        public List<Faction> factions = new List<Faction>();
    }

    /// <summary>
    /// Complete relationship and affinity system for managing NPC relationships,
    /// social links, romance, and faction reputation.
    /// </summary>
    public class RelationshipSystem : MonoBehaviour
    {
        public static RelationshipSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private RelationshipConfig config;

        [Header("Current State")]
        [SerializeField] private string currentPartnerId;
        [SerializeField] private int totalDaysPlayed;
        [SerializeField] private string currentDate;

        // Runtime data
        private Dictionary<string, SocialLink> socialLinks = new Dictionary<string, SocialLink>();
        private Dictionary<string, FactionReputation> factionReputations = new Dictionary<string, FactionReputation>();
        private Dictionary<string, int> dailyInteractionCounts = new Dictionary<string, int>();
        private Dictionary<string, int> dailyGiftCounts = new Dictionary<string, int>();
        private Dictionary<string, List<string>> giftHistory = new Dictionary<string, List<string>>();
        private List<string> activeFlags = new List<string>();
        private List<string> viewedEvents = new List<string>();

        // Events
        public event Action<string, int> OnAffinityChanged;
        public event Action<string, int> OnRankUp;
        public event Action<string> OnMaxRankReached;
        public event Action<string, SocialEvent> OnEventAvailable;
        public event Action<string, SocialEvent> OnEventCompleted;
        public event Action<string, FactionStanding> OnFactionStandingChanged;
        public event Action<string, string> OnFactionRewardUnlocked;
        public event Action<string> OnRomanceStarted;
        public event Action<string> OnEngagement;
        public event Action<string> OnMarriage;
        public event Action<RelationshipType, RelationshipType> OnRelationshipTypeChanged;

        // Properties
        public bool HasPartner => !string.IsNullOrEmpty(currentPartnerId);
        public string CurrentPartnerId => currentPartnerId;
        public int TotalDaysPlayed => totalDaysPlayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeFactions();
        }

        private void InitializeFactions()
        {
            if (config == null) return;

            foreach (var faction in config.factions)
            {
                factionReputations[faction.factionId] = new FactionReputation
                {
                    factionId = faction.factionId
                };

                factionReputations[faction.factionId].OnStandingChanged += (prev, curr) =>
                    OnFactionStandingChanged?.Invoke(faction.factionId, curr);
                
                factionReputations[faction.factionId].OnRewardUnlocked += rewardId =>
                    OnFactionRewardUnlocked?.Invoke(faction.factionId, rewardId);
            }
        }

        #region Social Links

        /// <summary>
        /// Gets or creates a social link with a character.
        /// </summary>
        public SocialLink GetSocialLink(string characterId)
        {
            if (!socialLinks.TryGetValue(characterId, out var link))
            {
                link = new SocialLink { characterId = characterId };
                
                // Check if romanceable
                var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);
                if (candidate != null)
                {
                    link.characterName = candidate.characterName;
                    link.isRomanceable = candidate.isRomanceable;
                }

                link.OnRankUp += rank => OnRankUp?.Invoke(characterId, rank);
                link.OnMaxRankReached += () => OnMaxRankReached?.Invoke(characterId);
                link.OnEventUnlocked += eventId => CheckEventAvailability(characterId, eventId);

                socialLinks[characterId] = link;
            }
            return link;
        }

        /// <summary>
        /// Increases affinity with a character.
        /// </summary>
        public int IncreaseAffinity(string characterId, int amount, InteractionType interactionType = InteractionType.Talk)
        {
            // Check daily limits
            if (interactionType == InteractionType.Talk || interactionType == InteractionType.DailyGreeting)
            {
                if (!CanInteractToday(characterId))
                {
                    return 0;
                }
                IncrementDailyInteraction(characterId);
            }

            var link = GetSocialLink(characterId);
            int ranksBefore = link.currentRank;
            
            // Apply multipliers
            float multiplier = 1f;
            
            // Check birthday
            var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);
            if (candidate != null && IsBirthday(candidate.birthday))
            {
                multiplier *= config?.birthdayGiftMultiplier ?? 3f;
            }

            int adjustedAmount = (int)(amount * multiplier);
            int ranksGained = link.AddPoints(adjustedAmount);

            OnAffinityChanged?.Invoke(characterId, adjustedAmount);

            // Check for new events
            CheckAvailableEvents(characterId);

            return ranksGained;
        }

        /// <summary>
        /// Decreases affinity with a character.
        /// </summary>
        public void DecreaseAffinity(string characterId, int amount)
        {
            var link = GetSocialLink(characterId);
            link.AddPoints(-Mathf.Abs(amount));
            OnAffinityChanged?.Invoke(characterId, -Mathf.Abs(amount));
        }

        /// <summary>
        /// Gets the current relationship type with a character.
        /// </summary>
        public RelationshipType GetRelationshipType(string characterId)
        {
            var link = GetSocialLink(characterId);

            if (characterId == currentPartnerId)
            {
                if (link.currentRank >= 10) return RelationshipType.Married;
                return RelationshipType.Romantic;
            }

            if (link.currentRank >= 10) return RelationshipType.BestFriend;
            if (link.currentRank >= 7) return RelationshipType.CloseFriend;
            if (link.currentRank >= 4) return RelationshipType.Friend;
            if (link.currentRank >= 2) return RelationshipType.Acquaintance;
            return RelationshipType.Stranger;
        }

        /// <summary>
        /// Gets all characters with a specific relationship type.
        /// </summary>
        public List<string> GetCharactersByRelationship(RelationshipType type)
        {
            return socialLinks
                .Where(kvp => GetRelationshipType(kvp.Key) == type)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets the top N characters by affinity.
        /// </summary>
        public List<(string characterId, int rank, int points)> GetTopRelationships(int count = 5)
        {
            return socialLinks
                .OrderByDescending(kvp => kvp.Value.currentRank * 1000 + kvp.Value.currentPoints)
                .Take(count)
                .Select(kvp => (kvp.Key, kvp.Value.currentRank, kvp.Value.currentPoints))
                .ToList();
        }

        private bool CanInteractToday(string characterId)
        {
            if (!dailyInteractionCounts.TryGetValue(characterId, out int count))
            {
                return true;
            }
            return count < (config?.dailyInteractionLimit ?? 3);
        }

        private void IncrementDailyInteraction(string characterId)
        {
            if (!dailyInteractionCounts.ContainsKey(characterId))
            {
                dailyInteractionCounts[characterId] = 0;
            }
            dailyInteractionCounts[characterId]++;
        }

        #endregion

        #region Gifts

        /// <summary>
        /// Gives a gift to a character.
        /// </summary>
        public int GiveGift(string characterId, string itemId, string itemCategory = "")
        {
            // Check daily gift limit
            if (!CanGiftToday(characterId))
            {
                return 0;
            }

            var link = GetSocialLink(characterId);
            var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);

            int baseAffinity = 10; // Default for unknown characters

            if (candidate != null)
            {
                baseAffinity = candidate.giftPreferences.GetGiftAffinity(itemId, itemCategory);
            }

            // Apply duplicate penalty
            if (HasGiftedBefore(characterId, itemId))
            {
                baseAffinity = (int)(baseAffinity * (config?.duplicateGiftPenalty ?? 0.5f));
            }

            // Record gift
            RecordGift(characterId, itemId);
            IncrementDailyGift(characterId);
            link.totalGiftsGiven++;

            return IncreaseAffinity(characterId, baseAffinity, InteractionType.GiveGift);
        }

        /// <summary>
        /// Gets gift recommendations for a character.
        /// </summary>
        public List<string> GetGiftRecommendations(string characterId, int count = 5)
        {
            var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);
            if (candidate == null) return new List<string>();

            var recommendations = new List<string>();
            recommendations.AddRange(candidate.giftPreferences.lovedItems.Take(count));
            
            if (recommendations.Count < count)
            {
                recommendations.AddRange(candidate.giftPreferences.likedItems.Take(count - recommendations.Count));
            }

            // Remove already gifted items (to avoid duplicate penalty)
            if (giftHistory.TryGetValue(characterId, out var history))
            {
                recommendations = recommendations.Except(history).ToList();
            }

            return recommendations.Take(count).ToList();
        }

        /// <summary>
        /// Gets items to avoid gifting to a character.
        /// </summary>
        public List<string> GetGiftsToAvoid(string characterId)
        {
            var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);
            if (candidate == null) return new List<string>();

            return candidate.giftPreferences.hatedItems.Concat(candidate.giftPreferences.dislikedItems).ToList();
        }

        private bool CanGiftToday(string characterId)
        {
            if (!dailyGiftCounts.TryGetValue(characterId, out int count))
            {
                return true;
            }
            return count < (config?.maxGiftsPerDay ?? 2);
        }

        private void IncrementDailyGift(string characterId)
        {
            if (!dailyGiftCounts.ContainsKey(characterId))
            {
                dailyGiftCounts[characterId] = 0;
            }
            dailyGiftCounts[characterId]++;
        }

        private bool HasGiftedBefore(string characterId, string itemId)
        {
            return giftHistory.TryGetValue(characterId, out var history) && history.Contains(itemId);
        }

        private void RecordGift(string characterId, string itemId)
        {
            if (!giftHistory.ContainsKey(characterId))
            {
                giftHistory[characterId] = new List<string>();
            }
            if (!giftHistory[characterId].Contains(itemId))
            {
                giftHistory[characterId].Add(itemId);
            }
        }

        #endregion

        #region Romance

        /// <summary>
        /// Attempts to start a romance with a character.
        /// </summary>
        public bool StartRomance(string characterId)
        {
            var link = GetSocialLink(characterId);
            var candidate = config?.romanceCandidates.FirstOrDefault(r => r.characterId == characterId);

            if (candidate == null || !candidate.isRomanceable)
            {
                Debug.Log($"{characterId} is not romanceable");
                return false;
            }

            if (!config?.allowMultipleRomances ?? false && HasPartner && currentPartnerId != characterId)
            {
                Debug.Log("Already in a relationship");
                return false;
            }

            if (link.currentRank < candidate.minRankForConfession)
            {
                Debug.Log($"Rank too low. Need rank {candidate.minRankForConfession}");
                return false;
            }

            link.isRomancing = true;
            currentPartnerId = characterId;
            OnRomanceStarted?.Invoke(characterId);
            OnRelationshipTypeChanged?.Invoke(GetRelationshipType(characterId), RelationshipType.Romantic);

            return true;
        }

        /// <summary>
        /// Proposes to the current romantic partner.
        /// </summary>
        public bool Propose()
        {
            if (!HasPartner) return false;

            var link = GetSocialLink(currentPartnerId);
            if (link.currentRank < 9) return false;

            OnEngagement?.Invoke(currentPartnerId);
            return true;
        }

        /// <summary>
        /// Gets married to the current partner.
        /// </summary>
        public bool GetMarried()
        {
            if (!HasPartner) return false;

            var link = GetSocialLink(currentPartnerId);
            link.currentRank = link.maxRank;
            link.isMaxed = true;

            OnMarriage?.Invoke(currentPartnerId);
            return true;
        }

        /// <summary>
        /// Ends the current romantic relationship.
        /// </summary>
        public void BreakUp()
        {
            if (!HasPartner) return;

            var link = GetSocialLink(currentPartnerId);
            link.isRomancing = false;
            
            // Reduce affinity
            DecreaseAffinity(currentPartnerId, 100);

            var previousPartner = currentPartnerId;
            currentPartnerId = null;

            OnRelationshipTypeChanged?.Invoke(RelationshipType.Romantic, GetRelationshipType(previousPartner));
        }

        /// <summary>
        /// Gets all available romance candidates.
        /// </summary>
        public List<RomanceCandidate> GetRomanceCandidates()
        {
            if (config == null) return new List<RomanceCandidate>();
            return config.romanceCandidates.Where(r => r.isRomanceable).ToList();
        }

        /// <summary>
        /// Gets romance progress with a candidate.
        /// </summary>
        public (int rank, float progress, bool isRomancing) GetRomanceProgress(string characterId)
        {
            var link = GetSocialLink(characterId);
            return (link.currentRank, link.RankProgress, link.isRomancing);
        }

        #endregion

        #region Social Events

        /// <summary>
        /// Checks and returns available social events for a character.
        /// </summary>
        public List<SocialEvent> GetAvailableEvents(string characterId)
        {
            if (config == null) return new List<SocialEvent>();

            var link = GetSocialLink(characterId);
            return config.socialEvents
                .Where(e => e.characterId == characterId &&
                           e.requiredRank <= link.currentRank &&
                           (!e.isOneTime || !viewedEvents.Contains(e.eventId)) &&
                           e.requiredFlags.All(f => activeFlags.Contains(f)))
                .ToList();
        }

        /// <summary>
        /// Triggers a social event.
        /// </summary>
        public SocialEvent TriggerEvent(string eventId)
        {
            var evt = config?.socialEvents.FirstOrDefault(e => e.eventId == eventId);
            if (evt == null) return null;

            if (evt.isOneTime && viewedEvents.Contains(eventId))
            {
                return null;
            }

            return evt;
        }

        /// <summary>
        /// Completes a social event with a choice.
        /// </summary>
        public void CompleteEvent(string eventId, int choiceIndex)
        {
            var evt = config?.socialEvents.FirstOrDefault(e => e.eventId == eventId);
            if (evt == null) return;

            if (evt.isOneTime)
            {
                viewedEvents.Add(eventId);
                evt.hasBeenViewed = true;
            }

            // Apply choice effects
            if (choiceIndex >= 0 && choiceIndex < evt.choices.Count)
            {
                var choice = evt.choices[choiceIndex];
                IncreaseAffinity(evt.characterId, choice.affinityChange, InteractionType.SpecialEvent);

                if (!string.IsNullOrEmpty(choice.resultFlag))
                {
                    SetFlag(choice.resultFlag);
                }
            }

            // Apply base event reward
            IncreaseAffinity(evt.characterId, evt.affinityReward, InteractionType.SpecialEvent);

            OnEventCompleted?.Invoke(evt.characterId, evt);
        }

        private void CheckAvailableEvents(string characterId)
        {
            var availableEvents = GetAvailableEvents(characterId);
            foreach (var evt in availableEvents)
            {
                if (!viewedEvents.Contains(evt.eventId))
                {
                    OnEventAvailable?.Invoke(characterId, evt);
                }
            }
        }

        private void CheckEventAvailability(string characterId, string eventId)
        {
            var evt = config?.socialEvents.FirstOrDefault(e => e.eventId == eventId);
            if (evt != null)
            {
                OnEventAvailable?.Invoke(characterId, evt);
            }
        }

        #endregion

        #region Factions

        /// <summary>
        /// Modifies reputation with a faction.
        /// </summary>
        public void ModifyFactionReputation(string factionId, int amount)
        {
            var faction = config?.factions.FirstOrDefault(f => f.factionId == factionId);
            if (faction == null) return;

            if (!factionReputations.TryGetValue(factionId, out var rep))
            {
                rep = new FactionReputation { factionId = factionId };
                factionReputations[factionId] = rep;
            }

            rep.ModifyReputation(amount, faction);

            // Propagate to allied/enemy factions
            if (amount > 0)
            {
                // Allied factions get bonus rep
                foreach (var alliedId in faction.alliedFactions)
                {
                    var allied = config.factions.FirstOrDefault(f => f.factionId == alliedId);
                    if (allied != null)
                    {
                        int bonusAmount = (int)(amount * (config?.alliedFactionRepBonus ?? 0.25f));
                        var alliedRep = GetFactionReputation(alliedId);
                        alliedRep.ModifyReputation(bonusAmount, allied);
                    }
                }

                // Enemy factions lose rep
                foreach (var enemyId in faction.enemyFactions)
                {
                    var enemy = config.factions.FirstOrDefault(f => f.factionId == enemyId);
                    if (enemy != null)
                    {
                        int penaltyAmount = -(int)(amount * (config?.enemyFactionRepPenalty ?? 0.5f));
                        var enemyRep = GetFactionReputation(enemyId);
                        enemyRep.ModifyReputation(penaltyAmount, enemy);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the reputation with a faction.
        /// </summary>
        public FactionReputation GetFactionReputation(string factionId)
        {
            if (!factionReputations.TryGetValue(factionId, out var rep))
            {
                rep = new FactionReputation { factionId = factionId };
                factionReputations[factionId] = rep;
            }
            return rep;
        }

        /// <summary>
        /// Gets the standing with a faction.
        /// </summary>
        public FactionStanding GetFactionStanding(string factionId)
        {
            return GetFactionReputation(factionId).currentStanding;
        }

        /// <summary>
        /// Gets all unlocked faction rewards.
        /// </summary>
        public List<FactionReward> GetUnlockedFactionRewards(string factionId)
        {
            var faction = config?.factions.FirstOrDefault(f => f.factionId == factionId);
            if (faction == null) return new List<FactionReward>();

            var rep = GetFactionReputation(factionId);
            return faction.rewards.Where(r => rep.unlockedRewards.Contains(r.rewardId)).ToList();
        }

        /// <summary>
        /// Gets the next reward that can be unlocked with a faction.
        /// </summary>
        public FactionReward GetNextFactionReward(string factionId)
        {
            var faction = config?.factions.FirstOrDefault(f => f.factionId == factionId);
            if (faction == null) return null;

            var rep = GetFactionReputation(factionId);
            return faction.rewards
                .OrderBy(r => (int)r.requiredStanding)
                .FirstOrDefault(r => !rep.unlockedRewards.Contains(r.rewardId));
        }

        /// <summary>
        /// Gets reputation needed to reach next standing.
        /// </summary>
        public int GetReputationToNextStanding(string factionId)
        {
            var faction = config?.factions.FirstOrDefault(f => f.factionId == factionId);
            if (faction == null) return 0;

            var rep = GetFactionReputation(factionId);
            int currentRep = rep.currentReputation;

            int[] thresholds = new[]
            {
                faction.hatedThreshold, faction.hostileThreshold, faction.unfriendlyThreshold,
                faction.neutralThreshold, faction.friendlyThreshold, faction.honoredThreshold,
                faction.reveredThreshold
            };

            foreach (int threshold in thresholds.OrderBy(t => t))
            {
                if (currentRep < threshold)
                {
                    return threshold - currentRep;
                }
            }

            return 0; // Already at max standing
        }

        #endregion

        #region Flags & Daily Reset

        /// <summary>
        /// Sets a story/relationship flag.
        /// </summary>
        public void SetFlag(string flag)
        {
            if (!activeFlags.Contains(flag))
            {
                activeFlags.Add(flag);
            }
        }

        /// <summary>
        /// Clears a story/relationship flag.
        /// </summary>
        public void ClearFlag(string flag)
        {
            activeFlags.Remove(flag);
        }

        /// <summary>
        /// Checks if a flag is set.
        /// </summary>
        public bool HasFlag(string flag)
        {
            return activeFlags.Contains(flag);
        }

        /// <summary>
        /// Called when a new day starts. Resets daily limits and applies decay.
        /// </summary>
        public void OnNewDay(string date)
        {
            currentDate = date;
            totalDaysPlayed++;

            // Reset daily counters
            dailyInteractionCounts.Clear();
            dailyGiftCounts.Clear();

            // Apply affinity decay for characters not interacted with
            if (config?.dailyAffinityDecay > 0)
            {
                foreach (var link in socialLinks.Values)
                {
                    if ((DateTime.Now - link.lastInteraction).TotalDays > 7)
                    {
                        link.AddPoints(-config.dailyAffinityDecay);
                    }
                }
            }
        }

        private bool IsBirthday(string birthday)
        {
            if (string.IsNullOrEmpty(birthday) || string.IsNullOrEmpty(currentDate))
                return false;
            return currentDate.EndsWith(birthday.Substring(birthday.IndexOf('/')));
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a summary of all relationships.
        /// </summary>
        public string GetRelationshipSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Relationship Summary ===");
            
            if (HasPartner)
            {
                var partnerLink = GetSocialLink(currentPartnerId);
                sb.AppendLine($"Partner: {partnerLink.characterName} (Rank {partnerLink.currentRank})");
            }

            sb.AppendLine($"\nTotal Relationships: {socialLinks.Count}");
            sb.AppendLine($"Best Friends: {GetCharactersByRelationship(RelationshipType.BestFriend).Count}");
            sb.AppendLine($"Close Friends: {GetCharactersByRelationship(RelationshipType.CloseFriend).Count}");
            sb.AppendLine($"Friends: {GetCharactersByRelationship(RelationshipType.Friend).Count}");

            sb.AppendLine("\n--- Top Relationships ---");
            foreach (var (charId, rank, points) in GetTopRelationships(5))
            {
                var link = socialLinks[charId];
                sb.AppendLine($"  {link.characterName ?? charId}: Rank {rank} ({points} pts)");
            }

            sb.AppendLine("\n--- Faction Standings ---");
            foreach (var faction in config?.factions ?? new List<Faction>())
            {
                var rep = GetFactionReputation(faction.factionId);
                sb.AppendLine($"  {faction.factionName}: {rep.currentStanding} ({rep.currentReputation} rep)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the relationship system.
        /// </summary>
        public RelationshipSaveData CreateSaveData()
        {
            return new RelationshipSaveData
            {
                currentPartnerId = currentPartnerId,
                totalDaysPlayed = totalDaysPlayed,
                currentDate = currentDate,
                socialLinks = socialLinks.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SocialLinkSaveData
                    {
                        currentRank = kvp.Value.currentRank,
                        currentPoints = kvp.Value.currentPoints,
                        isMaxed = kvp.Value.isMaxed,
                        isRomancing = kvp.Value.isRomancing,
                        totalGiftsGiven = kvp.Value.totalGiftsGiven,
                        unlockedEvents = new List<string>(kvp.Value.unlockedEvents)
                    }
                ),
                factionReputations = factionReputations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new FactionReputationSaveData
                    {
                        currentReputation = kvp.Value.currentReputation,
                        totalReputationEarned = kvp.Value.totalReputationEarned,
                        unlockedRewards = new List<string>(kvp.Value.unlockedRewards)
                    }
                ),
                activeFlags = new List<string>(activeFlags),
                viewedEvents = new List<string>(viewedEvents),
                giftHistory = giftHistory.ToDictionary(kvp => kvp.Key, kvp => new List<string>(kvp.Value))
            };
        }

        /// <summary>
        /// Loads relationship system state from save data.
        /// </summary>
        public void LoadSaveData(RelationshipSaveData saveData)
        {
            if (saveData == null) return;

            currentPartnerId = saveData.currentPartnerId;
            totalDaysPlayed = saveData.totalDaysPlayed;
            currentDate = saveData.currentDate;
            activeFlags = new List<string>(saveData.activeFlags);
            viewedEvents = new List<string>(saveData.viewedEvents);
            giftHistory = saveData.giftHistory.ToDictionary(kvp => kvp.Key, kvp => new List<string>(kvp.Value));

            foreach (var kvp in saveData.socialLinks)
            {
                var link = GetSocialLink(kvp.Key);
                link.currentRank = kvp.Value.currentRank;
                link.currentPoints = kvp.Value.currentPoints;
                link.isMaxed = kvp.Value.isMaxed;
                link.isRomancing = kvp.Value.isRomancing;
                link.totalGiftsGiven = kvp.Value.totalGiftsGiven;
                link.unlockedEvents = new List<string>(kvp.Value.unlockedEvents);
                link.pointsToNextRank = link.CalculatePointsForRank(link.currentRank + 1);
            }

            foreach (var kvp in saveData.factionReputations)
            {
                var rep = GetFactionReputation(kvp.Key);
                rep.currentReputation = kvp.Value.currentReputation;
                rep.totalReputationEarned = kvp.Value.totalReputationEarned;
                rep.unlockedRewards = new List<string>(kvp.Value.unlockedRewards);
                
                var faction = config?.factions.FirstOrDefault(f => f.factionId == kvp.Key);
                if (faction != null)
                {
                    rep.currentStanding = faction.GetStanding(rep.currentReputation);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for relationship system.
    /// </summary>
    [Serializable]
    public class RelationshipSaveData
    {
        public string currentPartnerId;
        public int totalDaysPlayed;
        public string currentDate;
        public Dictionary<string, SocialLinkSaveData> socialLinks;
        public Dictionary<string, FactionReputationSaveData> factionReputations;
        public List<string> activeFlags;
        public List<string> viewedEvents;
        public Dictionary<string, List<string>> giftHistory;
    }

    /// <summary>
    /// Serializable save data for social links.
    /// </summary>
    [Serializable]
    public class SocialLinkSaveData
    {
        public int currentRank;
        public int currentPoints;
        public bool isMaxed;
        public bool isRomancing;
        public int totalGiftsGiven;
        public List<string> unlockedEvents;
    }

    /// <summary>
    /// Serializable save data for faction reputation.
    /// </summary>
    [Serializable]
    public class FactionReputationSaveData
    {
        public int currentReputation;
        public int totalReputationEarned;
        public List<string> unlockedRewards;
    }
}
