using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the anime archetype of a character.
    /// </summary>
    public enum AnimeArchetype
    {
        Protagonist,        // Classic hero, grows through bonds
        Rival,              // Competitive ally
        Mentor,             // Guides the party
        Mysterious,         // Hidden agenda, late-game reveal
        Berserker,          // Rage-fueled warrior
        Strategist,         // Calculated fighter
        Healer,             // Compassionate support
        Trickster,          // Unpredictable, high luck
        Noble,              // Royal lineage, summon-focused
        Outcast             // Dark past, redemption arc
    }

    /// <summary>
    /// Defines the transformation/awakening state of a character.
    /// </summary>
    public enum AwakeningState
    {
        Normal,             // Base form
        Awakened,           // First power-up (hair glow, aura)
        Transcended,        // Major transformation
        DivineForm,         // Ultimate form, limited duration
        DarkAwakening       // Corrupted form, high power but risky
    }

    /// <summary>
    /// Defines the affinity element for a character's abilities.
    /// </summary>
    public enum CharacterAffinity
    {
        Fire,
        Ice,
        Lightning,
        Earth,
        Wind,
        Water,
        Light,
        Dark,
        Void,
        Spirit,
        None                // No elemental affinity
    }

    /// <summary>
    /// Defines the character's combat style.
    /// </summary>
    public enum CombatStyle
    {
        SwordAndShield,     // Balanced melee
        DualWield,          // Fast attacks
        GreatSword,         // Heavy damage
        Spellcaster,        // Ranged magic
        Summoner,           // Uses tamed creatures
        Archer,             // Ranged physical
        Martial,            // Fists and kicks
        Scythe,             // Dark/death themed
        Staff,              // Support magic
        Katana              // Quick draw, counter style
    }

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
        Rival,
        Romantic,
        Mentor,
        FamilyBond,
        SoulPartner         // Unlocks combo ultimates
    }

    /// <summary>
    /// Defines anime-specific battle traits.
    /// </summary>
    public enum AnimeBattleTrait
    {
        PlotArmor,          // Survive lethal hit once with 1 HP
        FriendshipPower,    // Stronger with more allies alive
        RivalryBoost,       // Bonus damage when rival is in party
        LastStand,          // Damage boost at low HP
        Determination,      // Cannot be one-shot killed
        LimitBreak,         // Ultimate gauge fills faster
        ElementalMastery,   // Bonus elemental damage
        ComboMaster,        // Chain attacks deal more damage
        SpiritLink,         // Share HP regen with bonded creature
        DivineProtection    // Chance to negate status effects
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents the base stat spread of a character.
    /// </summary>
    [Serializable]
    public class AnimeCharacterStats
    {
        public int baseHP = 200;
        public int baseMP = 80;
        public int baseAttack = 25;
        public int baseDefense = 20;
        public int baseMagicAttack = 22;
        public int baseMagicDefense = 18;
        public int baseSpeed = 15;
        public int baseLuck = 10;

        [Header("Growth Rates")]
        public float hpGrowth = 15f;
        public float mpGrowth = 8f;
        public float attackGrowth = 3f;
        public float defenseGrowth = 2.5f;
        public float magicAttackGrowth = 2.8f;
        public float magicDefenseGrowth = 2.2f;
        public float speedGrowth = 1.5f;
        public float luckGrowth = 0.8f;

        public float GetStatAtLevel(string stat, int level)
        {
            return stat switch
            {
                "HP" => baseHP + (hpGrowth * (level - 1)),
                "MP" => baseMP + (mpGrowth * (level - 1)),
                "ATK" => baseAttack + (attackGrowth * (level - 1)),
                "DEF" => baseDefense + (defenseGrowth * (level - 1)),
                "MATK" => baseMagicAttack + (magicAttackGrowth * (level - 1)),
                "MDEF" => baseMagicDefense + (magicDefenseGrowth * (level - 1)),
                "SPD" => baseSpeed + (speedGrowth * (level - 1)),
                "LCK" => baseLuck + (luckGrowth * (level - 1)),
                _ => 0
            };
        }
    }

    /// <summary>
    /// Represents a character ability (anime-style move).
    /// </summary>
    [Serializable]
    public class AnimeAbility
    {
        public string abilityId;
        public string abilityName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public int learnLevel;
        public float mpCost;
        public float cooldown;
        public float power = 1f;
        public CharacterAffinity element;
        public bool isUltimate;
        public float ultimateGaugeCost = 100f;

        [Header("Combo")]
        public bool canComboWithCreature;
        public string comboCreatureElement;
        public float comboPowerMultiplier = 2f;

        [Header("Anime")]
        public bool hasTransformationCutscene;
        public AwakeningState requiredState = AwakeningState.Normal;
        public string voiceLineId;

        [Header("Visuals")]
        public AudioClip abilitySound;
        public GameObject abilityVFX;
        public string animationTrigger;
    }

    /// <summary>
    /// Represents the transformation/awakening configuration.
    /// </summary>
    [Serializable]
    public class AwakeningConfig
    {
        public AwakeningState state;
        public float hpMultiplier = 1.5f;
        public float attackMultiplier = 1.5f;
        public float defenseMultiplier = 1.2f;
        public float speedMultiplier = 1.3f;
        public float duration = 30f;            // Seconds in awakened form, 0 = permanent
        public float cooldown = 120f;

        [Header("Requirements")]
        public int requiredLevel = 20;
        public float requiredUltimateGauge = 100f;
        public float requiredHealthPercent = 0f; // 0 = any health

        [Header("Unlock")]
        public bool isUnlocked;
        public string unlockQuestId;
        public string unlockBossId;

        [Header("Visuals")]
        public RuntimeAnimatorController transformedAnimator;
        public Color auraColor = Color.white;
        public GameObject transformVFX;
        public AudioClip transformSound;
    }

    /// <summary>
    /// Represents a relationship between two characters.
    /// </summary>
    [Serializable]
    public class CharacterRelationship
    {
        public string characterId;
        public string relatedCharacterId;
        public RelationshipType relationshipType = RelationshipType.Stranger;
        public float affinity;
        public float affinityToNext = 100f;
        public int comboAttacksPerformed;
        public bool hasComboUltimate;

        public void AddAffinity(float amount)
        {
            affinity += amount;
            while (affinity >= affinityToNext && relationshipType < RelationshipType.SoulPartner)
            {
                affinity -= affinityToNext;
                relationshipType++;
                affinityToNext *= 1.3f;
            }
        }
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Character definition as a ScriptableObject for the anime JRPG.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimeCharacter", menuName = "UsefulScripts/AnimeEldenRingJRPG/Character Data")]
    public class AnimeCharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string characterName;
        public string title;                // e.g., "Blade of the Shattered Realm"
        [TextArea(3, 5)]
        public string backstory;
        public Sprite portrait;
        public Sprite fullBodyArt;
        public GameObject prefab;
        public AnimeArchetype archetype;

        [Header("Combat")]
        public CombatStyle combatStyle;
        public CharacterAffinity primaryAffinity;
        public List<AnimeBattleTrait> traits = new List<AnimeBattleTrait>();

        [Header("Stats")]
        public AnimeCharacterStats baseStats = new AnimeCharacterStats();
        public int maxLevel = 99;

        [Header("Experience")]
        public int baseExpToLevel = 150;
        public float expScaling = 1.4f;

        [Header("Abilities")]
        public List<AnimeAbility> abilities = new List<AnimeAbility>();
        public int maxEquippedAbilities = 6;

        [Header("Awakening")]
        public List<AwakeningConfig> awakenings = new List<AwakeningConfig>();

        [Header("Voice")]
        public AudioClip[] battleVoiceLines;
        public AudioClip[] victoryVoiceLines;
        public AudioClip awakeningVoiceLine;
    }

    #endregion

    #region Runtime Class

    /// <summary>
    /// Represents a playable anime character instance with stats, abilities,
    /// awakening, relationships, and battle traits.
    /// </summary>
    [Serializable]
    public class AnimeCharacterInstance
    {
        public AnimeCharacterData characterData;
        public int level = 1;
        public int currentExp;
        public float currentHP;
        public float currentMP;
        public bool isAlive = true;

        [Header("Awakening")]
        public AwakeningState currentAwakeningState = AwakeningState.Normal;
        public float ultimateGauge;
        public float maxUltimateGauge = 100f;
        public float awakeningTimeRemaining;

        [Header("Abilities")]
        public List<AnimeAbility> equippedAbilities = new List<AnimeAbility>();
        public List<string> learnedAbilityIds = new List<string>();

        [Header("Relationships")]
        public List<CharacterRelationship> relationships = new List<CharacterRelationship>();

        [Header("Battle Stats")]
        public int battlesWon;
        public int battlesFought;
        public int timesKO;
        public int timesRevived;
        public int plotArmorTriggered;

        // Events
        public event Action<AnimeCharacterInstance, int> OnLevelUp;
        public event Action<AnimeCharacterInstance, AwakeningState> OnAwakeningActivated;
        public event Action<AnimeCharacterInstance> OnAwakeningEnded;
        public event Action<AnimeCharacterInstance, AnimeAbility> OnAbilityLearned;
        public event Action<AnimeCharacterInstance> OnKO;
        public event Action<AnimeCharacterInstance> OnRevived;
        public event Action<AnimeCharacterInstance, AnimeBattleTrait> OnTraitTriggered;

        // Computed Stats
        public float MaxHP => characterData != null ? characterData.baseStats.GetStatAtLevel("HP", level) * GetAwakeningMultiplier("HP") : 200;
        public float MaxMP => characterData != null ? characterData.baseStats.GetStatAtLevel("MP", level) : 80;
        public float Attack => characterData != null ? characterData.baseStats.GetStatAtLevel("ATK", level) * GetAwakeningMultiplier("ATK") : 25;
        public float Defense => characterData != null ? characterData.baseStats.GetStatAtLevel("DEF", level) * GetAwakeningMultiplier("DEF") : 20;
        public float MagicAttack => characterData != null ? characterData.baseStats.GetStatAtLevel("MATK", level) * GetAwakeningMultiplier("MATK") : 22;
        public float MagicDefense => characterData != null ? characterData.baseStats.GetStatAtLevel("MDEF", level) : 18;
        public float Speed => characterData != null ? characterData.baseStats.GetStatAtLevel("SPD", level) * GetAwakeningMultiplier("SPD") : 15;

        public int ExpToNextLevel => characterData != null
            ? Mathf.RoundToInt(characterData.baseExpToLevel * Mathf.Pow(characterData.expScaling, level - 1))
            : 150;

        #region Experience & Level

        public void AddExperience(int amount)
        {
            if (!isAlive || characterData == null) return;
            if (level >= characterData.maxLevel) return;

            currentExp += amount;
            while (currentExp >= ExpToNextLevel && level < characterData.maxLevel)
            {
                currentExp -= ExpToNextLevel;
                level++;
                currentHP = MaxHP;
                currentMP = MaxMP;
                CheckNewAbilities();
                OnLevelUp?.Invoke(this, level);
            }
        }

        private void CheckNewAbilities()
        {
            if (characterData == null) return;
            foreach (var ability in characterData.abilities)
            {
                if (ability.learnLevel == level && !learnedAbilityIds.Contains(ability.abilityId))
                {
                    learnedAbilityIds.Add(ability.abilityId);
                    if (equippedAbilities.Count < characterData.maxEquippedAbilities)
                    {
                        equippedAbilities.Add(ability);
                    }
                    OnAbilityLearned?.Invoke(this, ability);
                }
            }
        }

        #endregion

        #region Combat

        public void TakeDamage(float damage)
        {
            if (!isAlive) return;

            float actualDamage = damage;

            // Plot Armor trait check
            if (characterData != null && characterData.traits.Contains(AnimeBattleTrait.PlotArmor))
            {
                if (currentHP - actualDamage <= 0 && plotArmorTriggered == 0)
                {
                    currentHP = 1;
                    plotArmorTriggered++;
                    OnTraitTriggered?.Invoke(this, AnimeBattleTrait.PlotArmor);
                    return;
                }
            }

            // Determination trait: cannot be one-shot
            if (characterData != null && characterData.traits.Contains(AnimeBattleTrait.Determination))
            {
                if (actualDamage >= currentHP && currentHP == MaxHP)
                {
                    actualDamage = currentHP - 1;
                    OnTraitTriggered?.Invoke(this, AnimeBattleTrait.Determination);
                }
            }

            currentHP = Mathf.Max(0, currentHP - actualDamage);

            // Build ultimate gauge from damage taken
            ultimateGauge = Mathf.Min(maxUltimateGauge, ultimateGauge + (actualDamage * 0.1f));

            // Last Stand trait
            if (characterData != null && characterData.traits.Contains(AnimeBattleTrait.LastStand))
            {
                if (currentHP <= MaxHP * 0.25f)
                {
                    OnTraitTriggered?.Invoke(this, AnimeBattleTrait.LastStand);
                }
            }

            if (currentHP <= 0)
            {
                isAlive = false;
                timesKO++;
                OnKO?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive) return;
            currentHP = Mathf.Min(MaxHP, currentHP + amount);
        }

        public void Revive(float healthPercent = 0.5f)
        {
            if (isAlive) return;
            isAlive = true;
            currentHP = MaxHP * healthPercent;
            timesRevived++;
            OnRevived?.Invoke(this);
        }

        /// <summary>
        /// Uses an ability, consuming MP or ultimate gauge.
        /// </summary>
        public bool UseAbility(string abilityId)
        {
            if (!isAlive) return false;

            var ability = equippedAbilities.FirstOrDefault(a => a.abilityId == abilityId);
            if (ability == null) return false;

            if (ability.requiredState != AwakeningState.Normal && currentAwakeningState < ability.requiredState)
                return false;

            if (ability.isUltimate)
            {
                if (ultimateGauge < ability.ultimateGaugeCost) return false;
                ultimateGauge -= ability.ultimateGaugeCost;
            }
            else
            {
                if (currentMP < ability.mpCost) return false;
                currentMP -= ability.mpCost;
            }

            // Build ultimate gauge from ability use
            if (!ability.isUltimate)
            {
                ultimateGauge = Mathf.Min(maxUltimateGauge, ultimateGauge + 5f);
            }

            return true;
        }

        /// <summary>
        /// Builds ultimate gauge from dealing damage.
        /// </summary>
        public void AddUltimateGauge(float amount)
        {
            ultimateGauge = Mathf.Min(maxUltimateGauge, ultimateGauge + amount);
        }

        #endregion

        #region Awakening

        /// <summary>
        /// Activates an awakening transformation if requirements are met.
        /// </summary>
        public bool ActivateAwakening(AwakeningState targetState)
        {
            if (!isAlive || characterData == null) return false;

            var awakening = characterData.awakenings.FirstOrDefault(a => a.state == targetState);
            if (awakening == null || !awakening.isUnlocked) return false;

            if (level < awakening.requiredLevel) return false;
            if (ultimateGauge < awakening.requiredUltimateGauge) return false;
            if (awakening.requiredHealthPercent > 0 && (currentHP / MaxHP) > awakening.requiredHealthPercent) return false;

            ultimateGauge -= awakening.requiredUltimateGauge;
            currentAwakeningState = targetState;
            awakeningTimeRemaining = awakening.duration;

            // Heal on transformation
            currentHP = Mathf.Min(MaxHP, currentHP + MaxHP * 0.2f);

            OnAwakeningActivated?.Invoke(this, targetState);
            return true;
        }

        /// <summary>
        /// Updates the awakening timer. Call every frame during battle.
        /// </summary>
        public void UpdateAwakening(float deltaTime)
        {
            if (currentAwakeningState == AwakeningState.Normal) return;

            var awakening = characterData?.awakenings.FirstOrDefault(a => a.state == currentAwakeningState);
            if (awakening == null || awakening.duration <= 0) return; // Permanent awakening

            awakeningTimeRemaining -= deltaTime;
            if (awakeningTimeRemaining <= 0)
            {
                DeactivateAwakening();
            }
        }

        /// <summary>
        /// Returns to normal state.
        /// </summary>
        public void DeactivateAwakening()
        {
            if (currentAwakeningState == AwakeningState.Normal) return;

            currentAwakeningState = AwakeningState.Normal;
            awakeningTimeRemaining = 0;
            OnAwakeningEnded?.Invoke(this);
        }

        private float GetAwakeningMultiplier(string stat)
        {
            if (characterData == null || currentAwakeningState == AwakeningState.Normal) return 1f;

            var awakening = characterData.awakenings.FirstOrDefault(a => a.state == currentAwakeningState);
            if (awakening == null) return 1f;

            return stat switch
            {
                "HP" => awakening.hpMultiplier,
                "ATK" => awakening.attackMultiplier,
                "DEF" => awakening.defenseMultiplier,
                "SPD" => awakening.speedMultiplier,
                _ => 1f
            };
        }

        #endregion

        #region Relationships

        /// <summary>
        /// Adds affinity to a relationship with another character.
        /// </summary>
        public void AddRelationshipAffinity(string otherCharacterId, float amount)
        {
            var relationship = relationships.FirstOrDefault(r => r.relatedCharacterId == otherCharacterId);
            if (relationship == null)
            {
                relationship = new CharacterRelationship
                {
                    characterId = characterData != null ? characterData.characterId : "",
                    relatedCharacterId = otherCharacterId
                };
                relationships.Add(relationship);
            }

            relationship.AddAffinity(amount);
        }

        /// <summary>
        /// Gets the relationship type with another character.
        /// </summary>
        public RelationshipType GetRelationship(string otherCharacterId)
        {
            var relationship = relationships.FirstOrDefault(r => r.relatedCharacterId == otherCharacterId);
            return relationship?.relationshipType ?? RelationshipType.Stranger;
        }

        /// <summary>
        /// Checks if a combo ultimate is available with another character.
        /// </summary>
        public bool HasComboUltimate(string otherCharacterId)
        {
            var relationship = relationships.FirstOrDefault(r => r.relatedCharacterId == otherCharacterId);
            return relationship != null && relationship.hasComboUltimate;
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            currentHP = MaxHP;
            currentMP = MaxMP;
            isAlive = true;
            ultimateGauge = 0;
            currentAwakeningState = AwakeningState.Normal;

            if (characterData != null)
            {
                foreach (var ability in characterData.abilities)
                {
                    if (ability.learnLevel <= level)
                    {
                        learnedAbilityIds.Add(ability.abilityId);
                        if (equippedAbilities.Count < characterData.maxEquippedAbilities)
                        {
                            equippedAbilities.Add(ability);
                        }
                    }
                }
            }
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Manages anime JRPG characters with archetypes, awakening transformations,
    /// battle traits, relationships, and ultimate abilities.
    /// Combines Elden Ring's build variety with anime JRPG character depth.
    /// </summary>
    public class AnimeCharacterSystem : MonoBehaviour
    {
        [Header("Party")]
        [SerializeField] private List<AnimeCharacterInstance> partyMembers = new List<AnimeCharacterInstance>();
        [SerializeField] private int maxPartySize = 4;

        // Events
        public event Action<AnimeCharacterInstance> OnCharacterAdded;
        public event Action<AnimeCharacterInstance> OnCharacterRemoved;
        public event Action<AnimeCharacterInstance, int> OnCharacterLevelUp;
        public event Action<AnimeCharacterInstance, AwakeningState> OnCharacterAwakened;
        public event Action<string, string, RelationshipType> OnRelationshipChanged;

        // Properties
        public List<AnimeCharacterInstance> PartyMembers => partyMembers;
        public int PartyCount => partyMembers.Count;

        #region Party Management

        /// <summary>
        /// Adds a character to the party.
        /// </summary>
        public bool AddToParty(AnimeCharacterInstance character)
        {
            if (partyMembers.Count >= maxPartySize) return false;
            if (partyMembers.Contains(character)) return false;

            partyMembers.Add(character);
            OnCharacterAdded?.Invoke(character);
            return true;
        }

        /// <summary>
        /// Removes a character from the party.
        /// </summary>
        public bool RemoveFromParty(AnimeCharacterInstance character)
        {
            if (!partyMembers.Contains(character)) return false;

            partyMembers.Remove(character);
            OnCharacterRemoved?.Invoke(character);
            return true;
        }

        /// <summary>
        /// Gets all alive party members.
        /// </summary>
        public List<AnimeCharacterInstance> GetAlivePartyMembers()
        {
            return partyMembers.Where(p => p.isAlive).ToList();
        }

        /// <summary>
        /// Checks if the party is wiped (all KO'd).
        /// </summary>
        public bool IsPartyWiped()
        {
            return partyMembers.All(p => !p.isAlive);
        }

        #endregion

        #region Battle Support

        /// <summary>
        /// Distributes experience to all alive party members.
        /// </summary>
        public void DistributeExperience(int totalExp)
        {
            var alive = GetAlivePartyMembers();
            if (alive.Count == 0) return;

            int baseExpPerMember = totalExp / alive.Count;
            foreach (var member in alive)
            {
                int expForMember = baseExpPerMember;

                // Friendship Power trait: bonus exp when all allies alive
                if (member.characterData != null &&
                    member.characterData.traits.Contains(AnimeBattleTrait.FriendshipPower) &&
                    alive.Count == partyMembers.Count)
                {
                    expForMember = Mathf.RoundToInt(expForMember * 1.2f);
                }

                member.AddExperience(expForMember);
            }
        }

        /// <summary>
        /// Heals all party members fully (rest at site of grace).
        /// </summary>
        public void FullPartyHeal()
        {
            foreach (var member in partyMembers)
            {
                if (!member.isAlive)
                {
                    member.Revive(1f);
                }
                member.currentHP = member.MaxHP;
                member.currentMP = member.MaxMP;
                member.plotArmorTriggered = 0;
            }
        }

        /// <summary>
        /// Builds relationship affinity between two party members after a battle.
        /// </summary>
        public void BuildBattleBond(string charId1, string charId2, float amount = 5f)
        {
            var char1 = partyMembers.FirstOrDefault(p => p.characterData != null && p.characterData.characterId == charId1);
            var char2 = partyMembers.FirstOrDefault(p => p.characterData != null && p.characterData.characterId == charId2);

            if (char1 != null && char2 != null)
            {
                char1.AddRelationshipAffinity(charId2, amount);
                char2.AddRelationshipAffinity(charId1, amount);
            }
        }

        /// <summary>
        /// Updates all party members' awakening timers.
        /// </summary>
        public void UpdateAwakenings(float deltaTime)
        {
            foreach (var member in partyMembers)
            {
                member.UpdateAwakening(deltaTime);
            }
        }

        #endregion

        #region Save Data

        [Serializable]
        public class CharacterSystemSaveData
        {
            public List<CharacterSave> characters = new List<CharacterSave>();
        }

        [Serializable]
        public class CharacterSave
        {
            public string characterDataId;
            public int level;
            public int currentExp;
            public float currentHP;
            public float currentMP;
            public bool isAlive;
            public float ultimateGauge;
            public List<string> learnedAbilityIds;
            public int battlesWon;
            public int battlesFought;
            public List<RelationshipSave> relationships;
        }

        [Serializable]
        public class RelationshipSave
        {
            public string relatedCharacterId;
            public int relationshipType;
            public float affinity;
        }

        public CharacterSystemSaveData GetSaveData()
        {
            var data = new CharacterSystemSaveData();

            foreach (var member in partyMembers)
            {
                var save = new CharacterSave
                {
                    characterDataId = member.characterData != null ? member.characterData.characterId : "",
                    level = member.level,
                    currentExp = member.currentExp,
                    currentHP = member.currentHP,
                    currentMP = member.currentMP,
                    isAlive = member.isAlive,
                    ultimateGauge = member.ultimateGauge,
                    learnedAbilityIds = new List<string>(member.learnedAbilityIds),
                    battlesWon = member.battlesWon,
                    battlesFought = member.battlesFought,
                    relationships = new List<RelationshipSave>()
                };

                foreach (var rel in member.relationships)
                {
                    save.relationships.Add(new RelationshipSave
                    {
                        relatedCharacterId = rel.relatedCharacterId,
                        relationshipType = (int)rel.relationshipType,
                        affinity = rel.affinity
                    });
                }

                data.characters.Add(save);
            }

            return data;
        }

        #endregion
    }
}
