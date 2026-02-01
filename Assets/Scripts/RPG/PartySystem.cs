using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines party formation positions.
    /// </summary>
    public enum FormationPosition
    {
        Front,
        FrontLeft,
        FrontRight,
        Middle,
        MiddleLeft,
        MiddleRight,
        Back,
        BackLeft,
        BackRight
    }

    /// <summary>
    /// Defines the row position in battle formations.
    /// </summary>
    public enum BattleRow
    {
        Front,      // Takes more damage, deals more damage
        Middle,     // Balanced
        Back        // Takes less damage, deals less damage (unless ranged)
    }

    /// <summary>
    /// Defines AI behavior types for party members.
    /// </summary>
    public enum PartyAIBehavior
    {
        Manual,         // Player controlled
        Aggressive,     // Focus on attacking
        Defensive,      // Focus on defense and healing
        Balanced,       // Mix of offense and defense
        Support,        // Focus on buffs and healing
        Conserve,       // Use minimal resources
        FullPower       // Use all resources freely
    }

    /// <summary>
    /// Represents a party member with their data and state.
    /// </summary>
    [Serializable]
    public class PartyMember
    {
        public string memberId;
        public string memberName;
        public string characterClass;
        public Sprite portrait;
        public GameObject prefab;
        public CharacterStatsSystem stats;
        
        [Header("Party State")]
        public bool isInActiveParty = true;
        public bool isAvailable = true;
        public FormationPosition formationPosition = FormationPosition.Middle;
        public BattleRow battleRow = BattleRow.Front;
        public PartyAIBehavior aiBehavior = PartyAIBehavior.Manual;
        
        [Header("Relationship")]
        public int trustLevel = 0;
        public int maxTrustLevel = 100;
        public int affinityPoints = 0;
        public List<string> unlockedBondings = new List<string>();
        
        [Header("Temporary States")]
        public bool isKnockedOut;
        public bool isReserve;
        public float switchCooldown;

        public bool CanParticipate => isAvailable && !isKnockedOut;
        public float TrustPercent => (float)trustLevel / maxTrustLevel;

        public PartyMember()
        {
            memberId = Guid.NewGuid().ToString();
        }

        public PartyMember(string name, string className)
        {
            memberId = Guid.NewGuid().ToString();
            memberName = name;
            characterClass = className;
        }
    }

    /// <summary>
    /// Represents a party formation preset.
    /// </summary>
    [Serializable]
    public class FormationPreset
    {
        public string presetName;
        public Dictionary<string, FormationPosition> memberPositions = new Dictionary<string, FormationPosition>();
        public Dictionary<string, BattleRow> memberRows = new Dictionary<string, BattleRow>();

        public FormationPreset(string name)
        {
            presetName = name;
        }
    }

    /// <summary>
    /// Represents bonuses from party synergy.
    /// </summary>
    [Serializable]
    public class PartySynergy
    {
        public string synergyId;
        public string synergyName;
        public string description;
        public List<string> requiredClasses = new List<string>();
        public List<string> requiredMembers = new List<string>();
        public int minimumMembers = 2;
        
        [Header("Bonuses")]
        public float attackBonus;
        public float defenseBonus;
        public float magicBonus;
        public float speedBonus;
        public float healthBonus;
        public float manaBonus;
        public float experienceBonus;
        public float goldBonus;
        public bool enableComboAttacks;
        public List<string> unlockedSkills = new List<string>();
    }

    /// <summary>
    /// Represents a combo attack between party members.
    /// </summary>
    [Serializable]
    public class ComboAttack
    {
        public string comboId;
        public string comboName;
        public string description;
        public List<string> participantIds = new List<string>();
        public List<string> participantClasses = new List<string>();
        public float totalManaCost;
        public float totalStaminaCost;
        public float damageMultiplier = 2f;
        public DamageType damageType = DamageType.Physical;
        public List<CombatEffect> effects = new List<CombatEffect>();
        public AudioClip comboSound;
        public GameObject comboVFX;
        public float animationDuration = 2f;
    }

    /// <summary>
    /// Configuration for party system limits and settings.
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "UsefulScripts/RPG/Party Config")]
    public class PartyConfig : ScriptableObject
    {
        [Header("Party Limits")]
        public int maxActivePartySize = 4;
        public int maxReserveSize = 8;
        public int maxTotalMembers = 20;
        
        [Header("Formation")]
        public bool useFormations = true;
        public float frontRowDamageMultiplier = 1.2f;
        public float frontRowDefenseMultiplier = 0.8f;
        public float backRowDamageMultiplier = 0.8f;
        public float backRowDefenseMultiplier = 1.2f;
        
        [Header("Switching")]
        public bool allowMidBattleSwitch = true;
        public float switchCooldown = 2f;
        public bool switchCostsAction = true;
        
        [Header("Experience")]
        public bool shareExperience = true;
        public float reserveExperiencePercent = 0.5f;
        public float inactiveExperiencePercent = 0f;
        
        [Header("Synergies")]
        public List<PartySynergy> availableSynergies = new List<PartySynergy>();
        public List<ComboAttack> comboAttacks = new List<ComboAttack>();
    }

    /// <summary>
    /// Complete party management system for JRPG-style games.
    /// Handles party composition, formations, AI behavior, and synergies.
    /// </summary>
    public class PartySystem : MonoBehaviour
    {
        public static PartySystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private PartyConfig config;

        [Header("Party Members")]
        [SerializeField] private List<PartyMember> allMembers = new List<PartyMember>();

        // Runtime state
        private List<PartyMember> activeParty = new List<PartyMember>();
        private List<PartyMember> reserveParty = new List<PartyMember>();
        private PartyMember partyLeader;
        private List<FormationPreset> savedFormations = new List<FormationPreset>();
        private List<PartySynergy> activeSynergies = new List<PartySynergy>();
        private Dictionary<string, float> memberSwitchCooldowns = new Dictionary<string, float>();

        // Events
        public event Action<PartyMember> OnMemberJoined;
        public event Action<PartyMember> OnMemberLeft;
        public event Action<PartyMember> OnMemberAddedToActive;
        public event Action<PartyMember> OnMemberRemovedFromActive;
        public event Action<PartyMember, PartyMember> OnMembersSwapped;
        public event Action<PartyMember> OnLeaderChanged;
        public event Action<FormationPosition, PartyMember> OnFormationChanged;
        public event Action<List<PartySynergy>> OnSynergiesUpdated;
        public event Action<PartyMember> OnMemberKnockedOut;
        public event Action<PartyMember> OnMemberRevived;
        public event Action<ComboAttack> OnComboAttackAvailable;

        // Properties
        public IReadOnlyList<PartyMember> AllMembers => allMembers.AsReadOnly();
        public IReadOnlyList<PartyMember> ActiveParty => activeParty.AsReadOnly();
        public IReadOnlyList<PartyMember> ReserveParty => reserveParty.AsReadOnly();
        public PartyMember Leader => partyLeader;
        public int ActivePartyCount => activeParty.Count;
        public int MaxActiveSize => config?.maxActivePartySize ?? 4;
        public int TotalMemberCount => allMembers.Count;
        public IReadOnlyList<PartySynergy> ActiveSynergies => activeSynergies.AsReadOnly();

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
            UpdateSwitchCooldowns(Time.deltaTime);
        }

        private void UpdateSwitchCooldowns(float deltaTime)
        {
            var keys = memberSwitchCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                memberSwitchCooldowns[key] -= deltaTime;
                if (memberSwitchCooldowns[key] <= 0)
                {
                    memberSwitchCooldowns.Remove(key);
                }
            }
        }

        #region Party Management

        /// <summary>
        /// Adds a new member to the party roster.
        /// </summary>
        public bool AddMember(PartyMember member)
        {
            if (member == null) return false;
            if (allMembers.Count >= (config?.maxTotalMembers ?? 20)) return false;
            if (allMembers.Any(m => m.memberId == member.memberId)) return false;

            allMembers.Add(member);

            // Auto-add to active party if there's room
            if (activeParty.Count < MaxActiveSize)
            {
                AddToActiveParty(member);
            }
            else
            {
                AddToReserve(member);
            }

            if (partyLeader == null)
            {
                SetLeader(member);
            }

            OnMemberJoined?.Invoke(member);
            UpdateSynergies();
            return true;
        }

        /// <summary>
        /// Removes a member from the party entirely.
        /// </summary>
        public bool RemoveMember(PartyMember member)
        {
            if (member == null) return false;
            if (!allMembers.Contains(member)) return false;

            // Can't remove if it's the last member
            if (allMembers.Count <= 1) return false;

            // Can't remove the leader without reassigning
            if (member == partyLeader)
            {
                var newLeader = activeParty.FirstOrDefault(m => m != member) ?? 
                                allMembers.FirstOrDefault(m => m != member);
                if (newLeader != null)
                {
                    SetLeader(newLeader);
                }
            }

            activeParty.Remove(member);
            reserveParty.Remove(member);
            allMembers.Remove(member);

            OnMemberLeft?.Invoke(member);
            UpdateSynergies();
            return true;
        }

        /// <summary>
        /// Adds a member to the active party.
        /// </summary>
        public bool AddToActiveParty(PartyMember member)
        {
            if (member == null || !member.isAvailable) return false;
            if (activeParty.Count >= MaxActiveSize) return false;
            if (activeParty.Contains(member)) return false;

            reserveParty.Remove(member);
            activeParty.Add(member);
            member.isInActiveParty = true;

            OnMemberAddedToActive?.Invoke(member);
            UpdateSynergies();
            return true;
        }

        /// <summary>
        /// Removes a member from the active party to reserve.
        /// </summary>
        public bool RemoveFromActiveParty(PartyMember member)
        {
            if (member == null) return false;
            if (!activeParty.Contains(member)) return false;
            
            // Must have at least one active member
            if (activeParty.Count <= 1) return false;

            activeParty.Remove(member);
            member.isInActiveParty = false;
            AddToReserve(member);

            OnMemberRemovedFromActive?.Invoke(member);
            UpdateSynergies();
            return true;
        }

        /// <summary>
        /// Adds a member to the reserve party.
        /// </summary>
        public bool AddToReserve(PartyMember member)
        {
            if (member == null) return false;
            if (reserveParty.Count >= (config?.maxReserveSize ?? 8)) return false;
            if (reserveParty.Contains(member)) return false;

            reserveParty.Add(member);
            member.isReserve = true;
            return true;
        }

        /// <summary>
        /// Swaps two party members between active and reserve.
        /// </summary>
        public bool SwapMembers(PartyMember active, PartyMember reserve)
        {
            if (active == null || reserve == null) return false;
            if (!activeParty.Contains(active)) return false;
            if (!reserveParty.Contains(reserve)) return false;
            if (!reserve.isAvailable || reserve.isKnockedOut) return false;

            // Check cooldown
            if (config?.switchCooldown > 0)
            {
                if (memberSwitchCooldowns.ContainsKey(active.memberId)) return false;
                if (memberSwitchCooldowns.ContainsKey(reserve.memberId)) return false;
            }

            int activeIndex = activeParty.IndexOf(active);
            
            activeParty[activeIndex] = reserve;
            reserve.isInActiveParty = true;
            reserve.isReserve = false;

            reserveParty.Remove(reserve);
            reserveParty.Add(active);
            active.isInActiveParty = false;
            active.isReserve = true;

            // Apply cooldown
            if (config?.switchCooldown > 0)
            {
                memberSwitchCooldowns[active.memberId] = config.switchCooldown;
                memberSwitchCooldowns[reserve.memberId] = config.switchCooldown;
            }

            OnMembersSwapped?.Invoke(active, reserve);
            UpdateSynergies();
            return true;
        }

        /// <summary>
        /// Quick swap during battle - swaps a knocked out member with a reserve.
        /// </summary>
        public bool QuickSwapKnockedOut(PartyMember knockedOut)
        {
            if (!config?.allowMidBattleSwitch ?? false) return false;
            if (knockedOut == null || !knockedOut.isKnockedOut) return false;

            var availableReserve = reserveParty.FirstOrDefault(m => m.CanParticipate);
            if (availableReserve == null) return false;

            return SwapMembers(knockedOut, availableReserve);
        }

        /// <summary>
        /// Sets the party leader.
        /// </summary>
        public bool SetLeader(PartyMember member)
        {
            if (member == null) return false;
            if (!allMembers.Contains(member)) return false;
            if (!member.isAvailable) return false;

            var previousLeader = partyLeader;
            partyLeader = member;

            // Ensure leader is in active party
            if (!activeParty.Contains(member))
            {
                if (activeParty.Count >= MaxActiveSize)
                {
                    // Swap with first non-leader member
                    var toSwap = activeParty.First(m => m != previousLeader);
                    SwapMembers(toSwap, member);
                }
                else
                {
                    AddToActiveParty(member);
                }
            }

            OnLeaderChanged?.Invoke(member);
            return true;
        }

        /// <summary>
        /// Marks a party member as knocked out.
        /// </summary>
        public void KnockOutMember(PartyMember member)
        {
            if (member == null) return;
            
            member.isKnockedOut = true;
            OnMemberKnockedOut?.Invoke(member);

            // Auto-swap if enabled
            if (config?.allowMidBattleSwitch ?? false)
            {
                QuickSwapKnockedOut(member);
            }
        }

        /// <summary>
        /// Revives a knocked out party member.
        /// </summary>
        public void ReviveMember(PartyMember member, float healthPercent = 0.25f)
        {
            if (member == null || !member.isKnockedOut) return;

            member.isKnockedOut = false;
            
            if (member.stats != null)
            {
                float healAmount = member.stats.GetDerivedStat(DerivedStat.MaxHealth) * healthPercent;
                member.stats.ModifyHealth(healAmount);
            }

            OnMemberRevived?.Invoke(member);
        }

        /// <summary>
        /// Fully heals all party members.
        /// </summary>
        public void FullPartyRestore()
        {
            foreach (var member in allMembers)
            {
                member.isKnockedOut = false;
                member.stats?.FullRestore();
            }
        }

        #endregion

        #region Formation

        /// <summary>
        /// Sets a member's formation position.
        /// </summary>
        public void SetFormationPosition(PartyMember member, FormationPosition position)
        {
            if (member == null) return;
            
            // Check if position is already taken
            var existing = activeParty.FirstOrDefault(m => m.formationPosition == position && m != member);
            if (existing != null)
            {
                // Swap positions
                existing.formationPosition = member.formationPosition;
            }

            member.formationPosition = position;
            OnFormationChanged?.Invoke(position, member);
        }

        /// <summary>
        /// Sets a member's battle row.
        /// </summary>
        public void SetBattleRow(PartyMember member, BattleRow row)
        {
            if (member == null) return;
            member.battleRow = row;
        }

        /// <summary>
        /// Gets the damage multiplier based on battle row.
        /// </summary>
        public float GetRowDamageMultiplier(PartyMember member)
        {
            if (config == null) return 1f;

            return member.battleRow switch
            {
                BattleRow.Front => config.frontRowDamageMultiplier,
                BattleRow.Back => config.backRowDamageMultiplier,
                _ => 1f
            };
        }

        /// <summary>
        /// Gets the defense multiplier based on battle row.
        /// </summary>
        public float GetRowDefenseMultiplier(PartyMember member)
        {
            if (config == null) return 1f;

            return member.battleRow switch
            {
                BattleRow.Front => config.frontRowDefenseMultiplier,
                BattleRow.Back => config.backRowDefenseMultiplier,
                _ => 1f
            };
        }

        /// <summary>
        /// Saves the current formation as a preset.
        /// </summary>
        public void SaveFormationPreset(string presetName)
        {
            var preset = new FormationPreset(presetName);

            foreach (var member in activeParty)
            {
                preset.memberPositions[member.memberId] = member.formationPosition;
                preset.memberRows[member.memberId] = member.battleRow;
            }

            // Remove existing preset with same name
            savedFormations.RemoveAll(f => f.presetName == presetName);
            savedFormations.Add(preset);
        }

        /// <summary>
        /// Loads a saved formation preset.
        /// </summary>
        public bool LoadFormationPreset(string presetName)
        {
            var preset = savedFormations.FirstOrDefault(f => f.presetName == presetName);
            if (preset == null) return false;

            foreach (var member in activeParty)
            {
                if (preset.memberPositions.TryGetValue(member.memberId, out var position))
                {
                    member.formationPosition = position;
                }
                if (preset.memberRows.TryGetValue(member.memberId, out var row))
                {
                    member.battleRow = row;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all saved formation presets.
        /// </summary>
        public List<string> GetFormationPresets()
        {
            return savedFormations.Select(f => f.presetName).ToList();
        }

        /// <summary>
        /// Applies an optimal auto-formation based on class roles.
        /// </summary>
        public void AutoFormation()
        {
            // Sort by class - tanks front, healers back
            var tanks = activeParty.Where(m => IsTank(m.characterClass)).ToList();
            var dps = activeParty.Where(m => IsDPS(m.characterClass)).ToList();
            var healers = activeParty.Where(m => IsHealer(m.characterClass)).ToList();
            var others = activeParty.Except(tanks).Except(dps).Except(healers).ToList();

            var frontPositions = new[] { FormationPosition.FrontLeft, FormationPosition.Front, FormationPosition.FrontRight };
            var middlePositions = new[] { FormationPosition.MiddleLeft, FormationPosition.Middle, FormationPosition.MiddleRight };
            var backPositions = new[] { FormationPosition.BackLeft, FormationPosition.Back, FormationPosition.BackRight };

            int frontIndex = 0, middleIndex = 0, backIndex = 0;

            // Assign tanks to front
            foreach (var tank in tanks)
            {
                if (frontIndex < frontPositions.Length)
                {
                    tank.formationPosition = frontPositions[frontIndex++];
                    tank.battleRow = BattleRow.Front;
                }
            }

            // Assign DPS to middle
            foreach (var member in dps)
            {
                if (middleIndex < middlePositions.Length)
                {
                    member.formationPosition = middlePositions[middleIndex++];
                    member.battleRow = BattleRow.Middle;
                }
            }

            // Assign healers to back
            foreach (var healer in healers)
            {
                if (backIndex < backPositions.Length)
                {
                    healer.formationPosition = backPositions[backIndex++];
                    healer.battleRow = BattleRow.Back;
                }
            }

            // Assign others to remaining positions
            foreach (var member in others)
            {
                if (middleIndex < middlePositions.Length)
                {
                    member.formationPosition = middlePositions[middleIndex++];
                    member.battleRow = BattleRow.Middle;
                }
                else if (frontIndex < frontPositions.Length)
                {
                    member.formationPosition = frontPositions[frontIndex++];
                    member.battleRow = BattleRow.Front;
                }
                else if (backIndex < backPositions.Length)
                {
                    member.formationPosition = backPositions[backIndex++];
                    member.battleRow = BattleRow.Back;
                }
            }
        }

        private bool IsTank(string className) => 
            className.Contains("Knight") || className.Contains("Warrior") || 
            className.Contains("Tank") || className.Contains("Guardian");

        private bool IsDPS(string className) => 
            className.Contains("Mage") || className.Contains("Rogue") || 
            className.Contains("Archer") || className.Contains("Assassin");

        private bool IsHealer(string className) => 
            className.Contains("Healer") || className.Contains("Priest") || 
            className.Contains("Cleric") || className.Contains("White Mage");

        #endregion

        #region AI Behavior

        /// <summary>
        /// Sets the AI behavior for a party member.
        /// </summary>
        public void SetAIBehavior(PartyMember member, PartyAIBehavior behavior)
        {
            if (member == null) return;
            member.aiBehavior = behavior;
        }

        /// <summary>
        /// Sets AI behavior for all non-leader party members.
        /// </summary>
        public void SetPartyAIBehavior(PartyAIBehavior behavior)
        {
            foreach (var member in activeParty.Where(m => m != partyLeader))
            {
                member.aiBehavior = behavior;
            }
        }

        /// <summary>
        /// Gets the recommended action for an AI-controlled party member.
        /// </summary>
        public CombatAction GetAIRecommendedAction(PartyMember member, List<ICombatant> allies, List<ICombatant> enemies)
        {
            if (member == null || member.aiBehavior == PartyAIBehavior.Manual)
                return null;

            switch (member.aiBehavior)
            {
                case PartyAIBehavior.Aggressive:
                    return GetAggressiveAction(member, enemies);
                case PartyAIBehavior.Defensive:
                    return GetDefensiveAction(member, allies);
                case PartyAIBehavior.Balanced:
                    return GetBalancedAction(member, allies, enemies);
                case PartyAIBehavior.Support:
                    return GetSupportAction(member, allies);
                case PartyAIBehavior.Conserve:
                    return GetConserveAction(member, enemies);
                case PartyAIBehavior.FullPower:
                    return GetFullPowerAction(member, allies, enemies);
                default:
                    return null;
            }
        }

        private CombatAction GetAggressiveAction(PartyMember member, List<ICombatant> enemies)
        {
            // Prioritize strongest attack
            return new CombatAction
            {
                actionId = "attack",
                actionName = "Attack",
                actionType = CombatActionType.Attack,
                basePower = 1f
            };
        }

        private CombatAction GetDefensiveAction(PartyMember member, List<ICombatant> allies)
        {
            // Check if anyone needs healing
            var lowHealthAlly = allies.FirstOrDefault(a => a.CurrentHealth < a.MaxHealth * 0.3f);
            if (lowHealthAlly != null && member.stats?.HasEnoughMana(20) == true)
            {
                return new CombatAction
                {
                    actionId = "heal",
                    actionName = "Heal",
                    actionType = CombatActionType.Magic,
                    targetsAllies = true,
                    manaCost = 20
                };
            }

            return new CombatAction
            {
                actionId = "defend",
                actionName = "Defend",
                actionType = CombatActionType.Defend
            };
        }

        private CombatAction GetBalancedAction(PartyMember member, List<ICombatant> allies, List<ICombatant> enemies)
        {
            // Check party health first
            var avgHealth = allies.Average(a => a.CurrentHealth / a.MaxHealth);
            if (avgHealth < 0.5f)
            {
                return GetDefensiveAction(member, allies);
            }
            return GetAggressiveAction(member, enemies);
        }

        private CombatAction GetSupportAction(PartyMember member, List<ICombatant> allies)
        {
            // Prioritize buffs and healing
            var needsHeal = allies.FirstOrDefault(a => a.CurrentHealth < a.MaxHealth * 0.7f);
            if (needsHeal != null)
            {
                return new CombatAction
                {
                    actionId = "heal",
                    actionName = "Heal",
                    actionType = CombatActionType.Magic,
                    targetsAllies = true,
                    manaCost = 20
                };
            }

            // Apply buff
            return new CombatAction
            {
                actionId = "buff",
                actionName = "Buff",
                actionType = CombatActionType.Magic,
                targetsAllies = true,
                manaCost = 15
            };
        }

        private CombatAction GetConserveAction(PartyMember member, List<ICombatant> enemies)
        {
            // Only use basic attacks to conserve resources
            return new CombatAction
            {
                actionId = "attack",
                actionName = "Attack",
                actionType = CombatActionType.Attack,
                basePower = 1f
            };
        }

        private CombatAction GetFullPowerAction(PartyMember member, List<ICombatant> allies, List<ICombatant> enemies)
        {
            // Use strongest available skill
            return new CombatAction
            {
                actionId = "skill",
                actionName = "Powerful Skill",
                actionType = CombatActionType.Skill,
                basePower = 2f,
                manaCost = 30
            };
        }

        #endregion

        #region Synergies & Combos

        private void UpdateSynergies()
        {
            if (config == null) return;

            activeSynergies.Clear();
            var activeClasses = activeParty.Select(m => m.characterClass).ToList();
            var activeMemberIds = activeParty.Select(m => m.memberId).ToList();

            foreach (var synergy in config.availableSynergies)
            {
                bool hasRequiredClasses = synergy.requiredClasses.All(c => activeClasses.Contains(c));
                bool hasRequiredMembers = synergy.requiredMembers.All(m => activeMemberIds.Contains(m));
                bool hasMinimumMembers = activeParty.Count >= synergy.minimumMembers;

                if ((hasRequiredClasses || synergy.requiredClasses.Count == 0) &&
                    (hasRequiredMembers || synergy.requiredMembers.Count == 0) &&
                    hasMinimumMembers)
                {
                    activeSynergies.Add(synergy);
                }
            }

            OnSynergiesUpdated?.Invoke(activeSynergies);
        }

        /// <summary>
        /// Gets total synergy bonus for a stat.
        /// </summary>
        public float GetSynergyBonus(string bonusType)
        {
            float total = 0;
            foreach (var synergy in activeSynergies)
            {
                total += bonusType switch
                {
                    "attack" => synergy.attackBonus,
                    "defense" => synergy.defenseBonus,
                    "magic" => synergy.magicBonus,
                    "speed" => synergy.speedBonus,
                    "health" => synergy.healthBonus,
                    "mana" => synergy.manaBonus,
                    "experience" => synergy.experienceBonus,
                    "gold" => synergy.goldBonus,
                    _ => 0
                };
            }
            return total;
        }

        /// <summary>
        /// Gets available combo attacks based on current party.
        /// </summary>
        public List<ComboAttack> GetAvailableComboAttacks()
        {
            if (config == null) return new List<ComboAttack>();

            var available = new List<ComboAttack>();
            var activeMemberIds = activeParty.Where(m => m.CanParticipate).Select(m => m.memberId).ToList();
            var activeClasses = activeParty.Where(m => m.CanParticipate).Select(m => m.characterClass).ToList();

            foreach (var combo in config.comboAttacks)
            {
                bool hasParticipants = combo.participantIds.All(id => activeMemberIds.Contains(id));
                bool hasClasses = combo.participantClasses.All(c => activeClasses.Contains(c));

                if (hasParticipants && hasClasses)
                {
                    available.Add(combo);
                }
            }

            return available;
        }

        /// <summary>
        /// Executes a combo attack.
        /// </summary>
        public bool ExecuteComboAttack(ComboAttack combo, ICombatant target)
        {
            if (combo == null || target == null) return false;

            var participants = activeParty.Where(m => 
                combo.participantIds.Contains(m.memberId) || 
                combo.participantClasses.Contains(m.characterClass)).ToList();

            // Check if all participants can participate
            if (!participants.All(p => p.CanParticipate)) return false;

            // Split costs among participants
            float manaCostEach = combo.totalManaCost / participants.Count;
            float staminaCostEach = combo.totalStaminaCost / participants.Count;

            foreach (var participant in participants)
            {
                if (participant.stats != null)
                {
                    if (!participant.stats.HasEnoughMana(manaCostEach)) return false;
                    if (!participant.stats.HasEnoughStamina(staminaCostEach)) return false;
                }
            }

            // Consume resources
            foreach (var participant in participants)
            {
                participant.stats?.ModifyMana(-manaCostEach);
                participant.stats?.ModifyStamina(-staminaCostEach);
            }

            // Calculate combined damage
            float totalAttack = participants.Sum(p => p.stats?.GetDerivedStat(DerivedStat.PhysicalAttack) ?? 10);
            float damage = totalAttack * combo.damageMultiplier;

            // Apply damage
            target.TakeDamage(damage, combo.damageType);

            // Apply effects
            foreach (var effect in combo.effects)
            {
                target.ApplyCombatEffect(effect);
            }

            return true;
        }

        #endregion

        #region Experience & Trust

        /// <summary>
        /// Distributes experience to the party.
        /// </summary>
        public void DistributeExperience(long totalExp)
        {
            if (config == null || !config.shareExperience)
            {
                // Give full exp to active party only
                foreach (var member in activeParty.Where(m => m.CanParticipate))
                {
                    member.stats?.AddExperience(totalExp);
                }
                return;
            }

            // Share experience based on configuration
            int activeCount = activeParty.Count(m => m.CanParticipate);
            long expPerActive = totalExp / Math.Max(1, activeCount);
            long expPerReserve = (long)(expPerActive * config.reserveExperiencePercent);
            long expPerInactive = (long)(expPerActive * config.inactiveExperiencePercent);

            foreach (var member in activeParty.Where(m => m.CanParticipate))
            {
                member.stats?.AddExperience(expPerActive);
            }

            foreach (var member in reserveParty.Where(m => m.isAvailable))
            {
                member.stats?.AddExperience(expPerReserve);
            }

            foreach (var member in allMembers.Where(m => !m.isInActiveParty && !m.isReserve && m.isAvailable))
            {
                member.stats?.AddExperience(expPerInactive);
            }
        }

        /// <summary>
        /// Increases trust between party members.
        /// </summary>
        public void IncreaseTrust(PartyMember member, int amount)
        {
            if (member == null) return;

            member.trustLevel = Mathf.Min(member.trustLevel + amount, member.maxTrustLevel);
            member.affinityPoints += amount;

            // Check for bonding events
            CheckBondingEvents(member);
        }

        /// <summary>
        /// Increases trust between two specific members.
        /// </summary>
        public void IncreaseMutualTrust(PartyMember member1, PartyMember member2, int amount)
        {
            IncreaseTrust(member1, amount);
            IncreaseTrust(member2, amount);
        }

        private void CheckBondingEvents(PartyMember member)
        {
            // Trigger bonding events at certain trust thresholds
            int[] thresholds = { 10, 25, 50, 75, 100 };

            foreach (int threshold in thresholds)
            {
                if (member.trustLevel >= threshold && 
                    !member.unlockedBondings.Contains($"bond_{threshold}"))
                {
                    member.unlockedBondings.Add($"bond_{threshold}");
                    Debug.Log($"New bonding event unlocked for {member.memberName} at trust level {threshold}!");
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a party member by ID.
        /// </summary>
        public PartyMember GetMemberById(string memberId)
        {
            return allMembers.FirstOrDefault(m => m.memberId == memberId);
        }

        /// <summary>
        /// Gets a party member by name.
        /// </summary>
        public PartyMember GetMemberByName(string name)
        {
            return allMembers.FirstOrDefault(m => m.memberName == name);
        }

        /// <summary>
        /// Gets all members of a specific class.
        /// </summary>
        public List<PartyMember> GetMembersByClass(string className)
        {
            return allMembers.Where(m => m.characterClass == className).ToList();
        }

        /// <summary>
        /// Gets the number of conscious party members.
        /// </summary>
        public int GetConsciousCount()
        {
            return activeParty.Count(m => m.CanParticipate);
        }

        /// <summary>
        /// Checks if the party is wiped (all members knocked out).
        /// </summary>
        public bool IsPartyWiped()
        {
            return activeParty.All(m => m.isKnockedOut) && 
                   reserveParty.All(m => m.isKnockedOut || !m.isAvailable);
        }

        /// <summary>
        /// Gets a summary of the current party state.
        /// </summary>
        public string GetPartySummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Party Summary ===");
            sb.AppendLine($"Leader: {partyLeader?.memberName ?? "None"}");
            sb.AppendLine($"Active: {activeParty.Count}/{MaxActiveSize}");
            sb.AppendLine($"Reserve: {reserveParty.Count}");
            sb.AppendLine($"Total: {allMembers.Count}");
            sb.AppendLine();
            sb.AppendLine("Active Party:");
            foreach (var member in activeParty)
            {
                string status = member.isKnockedOut ? "[KO]" : "[OK]";
                sb.AppendLine($"  {status} {member.memberName} ({member.characterClass}) - {member.battleRow}");
            }
            sb.AppendLine();
            sb.AppendLine($"Active Synergies: {activeSynergies.Count}");
            foreach (var synergy in activeSynergies)
            {
                sb.AppendLine($"  - {synergy.synergyName}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the party.
        /// </summary>
        public PartySaveData CreateSaveData()
        {
            return new PartySaveData
            {
                leaderId = partyLeader?.memberId,
                activeMemberIds = activeParty.Select(m => m.memberId).ToList(),
                reserveMemberIds = reserveParty.Select(m => m.memberId).ToList(),
                memberStates = allMembers.ToDictionary(
                    m => m.memberId,
                    m => new PartyMemberState
                    {
                        isKnockedOut = m.isKnockedOut,
                        trustLevel = m.trustLevel,
                        affinityPoints = m.affinityPoints,
                        formationPosition = m.formationPosition,
                        battleRow = m.battleRow,
                        aiBehavior = m.aiBehavior,
                        unlockedBondings = new List<string>(m.unlockedBondings)
                    }
                )
            };
        }

        /// <summary>
        /// Loads party data from save.
        /// </summary>
        public void LoadSaveData(PartySaveData saveData)
        {
            if (saveData == null) return;

            foreach (var kvp in saveData.memberStates)
            {
                var member = GetMemberById(kvp.Key);
                if (member != null)
                {
                    member.isKnockedOut = kvp.Value.isKnockedOut;
                    member.trustLevel = kvp.Value.trustLevel;
                    member.affinityPoints = kvp.Value.affinityPoints;
                    member.formationPosition = kvp.Value.formationPosition;
                    member.battleRow = kvp.Value.battleRow;
                    member.aiBehavior = kvp.Value.aiBehavior;
                    member.unlockedBondings = new List<string>(kvp.Value.unlockedBondings);
                }
            }

            // Rebuild active party
            activeParty.Clear();
            foreach (var memberId in saveData.activeMemberIds)
            {
                var member = GetMemberById(memberId);
                if (member != null)
                {
                    activeParty.Add(member);
                    member.isInActiveParty = true;
                }
            }

            // Rebuild reserve party
            reserveParty.Clear();
            foreach (var memberId in saveData.reserveMemberIds)
            {
                var member = GetMemberById(memberId);
                if (member != null)
                {
                    reserveParty.Add(member);
                    member.isReserve = true;
                }
            }

            // Set leader
            if (!string.IsNullOrEmpty(saveData.leaderId))
            {
                partyLeader = GetMemberById(saveData.leaderId);
            }

            UpdateSynergies();
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for party system.
    /// </summary>
    [Serializable]
    public class PartySaveData
    {
        public string leaderId;
        public List<string> activeMemberIds;
        public List<string> reserveMemberIds;
        public Dictionary<string, PartyMemberState> memberStates;
    }

    /// <summary>
    /// Serializable state for a party member.
    /// </summary>
    [Serializable]
    public class PartyMemberState
    {
        public bool isKnockedOut;
        public int trustLevel;
        public int affinityPoints;
        public FormationPosition formationPosition;
        public BattleRow battleRow;
        public PartyAIBehavior aiBehavior;
        public List<string> unlockedBondings;
    }
}
