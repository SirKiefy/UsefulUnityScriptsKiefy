using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Weapons
{
    #region Enums

    /// <summary>
    /// Defines the type of weapon attachment.
    /// </summary>
    public enum AttachmentType
    {
        Scope,
        Barrel,
        Magazine,
        Stock,
        Trigger,
        TopRail,
        BottomRail,
        LeftRail,
        RightRail
    }

    /// <summary>
    /// Defines the available firing modes.
    /// </summary>
    public enum FiringMode
    {
        SemiAutomatic,
        Burst,
        FullAutomatic,
        BoltAction,
        PumpAction,
        Selective         // Supports multiple modes
    }

    /// <summary>
    /// Defines the type of ammunition.
    /// </summary>
    public enum AmmoType
    {
        // Pistol calibers
        Pistol9mm,
        Pistol45ACP,
        Pistol357Magnum,

        // Rifle calibers
        Rifle556,
        Rifle762,
        Rifle50BMG,

        // Shotgun
        Shotgun12Gauge,
        Shotgun20Gauge,

        // SMG
        SMG9mm,
        SMG45ACP,

        // Special
        Energy,
        Explosive,
        Incendiary,
        ArmorPiercing,
        Hollow,
        Subsonic,
        Tracer
    }

    /// <summary>
    /// Defines the rarity/quality tier of attachments and weapons.
    /// </summary>
    public enum WeaponRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Exotic
    }

    /// <summary>
    /// Defines the weapon category.
    /// </summary>
    public enum WeaponCategory
    {
        Pistol,
        SMG,
        AssaultRifle,
        SniperRifle,
        Shotgun,
        LMG,
        DMR,
        Launcher
    }

    #endregion

    #region Stat Modifiers

    /// <summary>
    /// Represents a stat modifier that can be applied by attachments.
    /// </summary>
    [Serializable]
    public class WeaponStatModifier
    {
        public WeaponStatType statType;
        public float value;
        public bool isPercentage;

        public WeaponStatModifier(WeaponStatType type, float val, bool percentage = false)
        {
            statType = type;
            value = val;
            isPercentage = percentage;
        }

        public float Apply(float baseValue)
        {
            if (isPercentage)
            {
                return baseValue * (1f + value / 100f);
            }
            return baseValue + value;
        }
    }

    /// <summary>
    /// Defines the types of weapon stats that can be modified.
    /// </summary>
    public enum WeaponStatType
    {
        Damage,
        FireRate,
        Range,
        Accuracy,
        RecoilControl,
        ReloadSpeed,
        MagazineSize,
        AimDownSightSpeed,
        MovementSpeed,
        HipFireAccuracy,
        MuzzleVelocity,
        Penetration,
        Stability,
        Handling,
        SwapSpeed,
        SprintToFireTime,
        NoiseReduction
    }

    #endregion

    #region Attachment Data

    /// <summary>
    /// Defines the properties of a weapon attachment.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttachment", menuName = "UsefulScripts/Weapons/Attachment")]
    public class AttachmentData : ScriptableObject
    {
        [Header("Basic Info")]
        public string attachmentId;
        public string attachmentName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public GameObject modelPrefab;

        [Header("Attachment Properties")]
        public AttachmentType attachmentType;
        public WeaponRarity rarity = WeaponRarity.Common;
        public float weight = 0.1f;

        [Header("Stat Modifiers")]
        public List<WeaponStatModifier> statModifiers = new List<WeaponStatModifier>();

        [Header("Compatibility")]
        public List<WeaponCategory> compatibleCategories = new List<WeaponCategory>();
        public List<string> compatibleWeaponIds = new List<string>();
        public List<string> incompatibleAttachmentIds = new List<string>();

        [Header("Visual Attachments")]
        public Vector3 attachOffset = Vector3.zero;
        public Vector3 attachRotation = Vector3.zero;
        public float attachScale = 1f;

        /// <summary>
        /// Checks if this attachment is compatible with a weapon.
        /// </summary>
        public bool IsCompatibleWith(WeaponData weapon)
        {
            if (weapon == null) return false;

            // Check if weapon category is compatible
            if (compatibleCategories.Count > 0 && !compatibleCategories.Contains(weapon.category))
            {
                return false;
            }

            // Check if specific weapon ID is in allowed list
            if (compatibleWeaponIds.Count > 0 && !compatibleWeaponIds.Contains(weapon.weaponId))
            {
                return false;
            }

            // Check if weapon has the required slot for this attachment type
            if (!weapon.HasSlotForAttachment(attachmentType))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if this attachment is compatible with another attachment.
        /// </summary>
        public bool IsCompatibleWith(AttachmentData other)
        {
            if (other == null) return true;
            return !incompatibleAttachmentIds.Contains(other.attachmentId);
        }
    }

    /// <summary>
    /// Represents an installed attachment instance.
    /// </summary>
    [Serializable]
    public class AttachmentInstance
    {
        public AttachmentData data;
        public string instanceId;
        public AttachmentType slotType;
        public GameObject spawnedModel;

        public AttachmentInstance(AttachmentData attachmentData, AttachmentType slot)
        {
            data = attachmentData;
            instanceId = Guid.NewGuid().ToString();
            slotType = slot;
        }
    }

    #endregion

    #region Ammo Data

    /// <summary>
    /// Defines ammunition properties.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAmmo", menuName = "UsefulScripts/Weapons/Ammo Type")]
    public class AmmoData : ScriptableObject
    {
        [Header("Basic Info")]
        public string ammoId;
        public string ammoName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;
        public AmmoType ammoType;

        [Header("Properties")]
        public float damageMultiplier = 1f;
        public float penetrationMultiplier = 1f;
        public float velocityMultiplier = 1f;
        public float recoilMultiplier = 1f;

        [Header("Special Effects")]
        public bool isIncendiary = false;
        public float burnDamage = 0f;
        public float burnDuration = 0f;

        public bool isExplosive = false;
        public float explosionRadius = 0f;
        public float explosionDamage = 0f;

        public bool isArmorPiercing = false;
        public float armorPenetration = 0f;

        public bool isSubsonic = false;
        public float noiseReduction = 0f;

        public bool isTracer = false;
        public Color tracerColor = Color.red;

        [Header("Visual")]
        public GameObject bulletPrefab;
        public GameObject impactEffectPrefab;
        public GameObject muzzleFlashOverride;
    }

    #endregion

    #region Weapon Data

    /// <summary>
    /// Defines the base properties of a weapon.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "UsefulScripts/Weapons/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponId;
        public string weaponName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public GameObject modelPrefab;
        public WeaponCategory category;
        public WeaponRarity rarity = WeaponRarity.Common;

        [Header("Base Stats")]
        public float baseDamage = 25f;
        public float baseFireRate = 600f;          // Rounds per minute
        public float baseRange = 100f;
        public float baseAccuracy = 80f;           // 0-100
        public float baseRecoilControl = 70f;      // 0-100
        public float baseReloadSpeed = 2f;         // Seconds
        public int baseMagazineSize = 30;
        public float baseAimDownSightSpeed = 0.3f; // Seconds
        public float baseMovementSpeed = 1f;       // Multiplier
        public float baseMuzzleVelocity = 700f;    // m/s
        public float basePenetration = 1f;
        public float baseStability = 70f;          // 0-100
        public float baseHandling = 70f;           // 0-100

        [Header("Firing")]
        public FiringMode defaultFiringMode = FiringMode.FullAutomatic;
        public List<FiringMode> supportedFiringModes = new List<FiringMode>();
        public int burstCount = 3;
        public float burstDelay = 0.05f;

        [Header("Ammunition")]
        public AmmoType defaultAmmoType = AmmoType.Rifle556;
        public List<AmmoType> compatibleAmmoTypes = new List<AmmoType>();
        public int reserveAmmoMax = 300;

        [Header("Attachment Slots")]
        public bool hasScope = true;
        public bool hasBarrel = true;
        public bool hasMagazine = true;
        public bool hasStock = true;
        public bool hasTrigger = true;
        public bool hasTopRail = true;
        public bool hasBottomRail = true;
        public bool hasLeftRail = false;
        public bool hasRightRail = false;

        [Header("Visual")]
        public Vector3 scopeMount = Vector3.zero;
        public Vector3 barrelMount = Vector3.zero;
        public Vector3 topRailMount = Vector3.zero;
        public Vector3 bottomRailMount = Vector3.zero;
        public Vector3 leftRailMount = Vector3.zero;
        public Vector3 rightRailMount = Vector3.zero;
        public Vector3 stockMount = Vector3.zero;
        public Vector3 magazineMount = Vector3.zero;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptyClickSound;
        public AudioClip equipSound;
        public AudioClip firingModeChangeSound;

        [Header("Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject ejectedCasingPrefab;

        /// <summary>
        /// Checks if this weapon has a slot for the given attachment type.
        /// </summary>
        public bool HasSlotForAttachment(AttachmentType type)
        {
            return type switch
            {
                AttachmentType.Scope => hasScope,
                AttachmentType.Barrel => hasBarrel,
                AttachmentType.Magazine => hasMagazine,
                AttachmentType.Stock => hasStock,
                AttachmentType.Trigger => hasTrigger,
                AttachmentType.TopRail => hasTopRail,
                AttachmentType.BottomRail => hasBottomRail,
                AttachmentType.LeftRail => hasLeftRail,
                AttachmentType.RightRail => hasRightRail,
                _ => false
            };
        }

        /// <summary>
        /// Gets the mount position for an attachment type.
        /// </summary>
        public Vector3 GetMountPosition(AttachmentType type)
        {
            return type switch
            {
                AttachmentType.Scope => scopeMount,
                AttachmentType.Barrel => barrelMount,
                AttachmentType.TopRail => topRailMount,
                AttachmentType.BottomRail => bottomRailMount,
                AttachmentType.LeftRail => leftRailMount,
                AttachmentType.RightRail => rightRailMount,
                AttachmentType.Stock => stockMount,
                AttachmentType.Magazine => magazineMount,
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// Gets all available attachment slots for this weapon.
        /// </summary>
        public List<AttachmentType> GetAvailableSlots()
        {
            var slots = new List<AttachmentType>();
            if (hasScope) slots.Add(AttachmentType.Scope);
            if (hasBarrel) slots.Add(AttachmentType.Barrel);
            if (hasMagazine) slots.Add(AttachmentType.Magazine);
            if (hasStock) slots.Add(AttachmentType.Stock);
            if (hasTrigger) slots.Add(AttachmentType.Trigger);
            if (hasTopRail) slots.Add(AttachmentType.TopRail);
            if (hasBottomRail) slots.Add(AttachmentType.BottomRail);
            if (hasLeftRail) slots.Add(AttachmentType.LeftRail);
            if (hasRightRail) slots.Add(AttachmentType.RightRail);
            return slots;
        }
    }

    #endregion

    #region Weapon Instance

    /// <summary>
    /// Represents a configured weapon instance with attachments.
    /// </summary>
    [Serializable]
    public class WeaponInstance
    {
        [Header("Weapon Info")]
        public WeaponData baseWeapon;
        public string instanceId;
        public string customName;

        [Header("Attachments")]
        public Dictionary<AttachmentType, AttachmentInstance> attachments = new Dictionary<AttachmentType, AttachmentInstance>();

        [Header("State")]
        public FiringMode currentFiringMode;
        public AmmoType currentAmmoType;
        public int currentMagazine;
        public int reserveAmmo;

        [Header("Runtime")]
        public GameObject spawnedModel;
        private Dictionary<WeaponStatType, float> cachedStats = new Dictionary<WeaponStatType, float>();
        private bool statsCacheDirty = true;

        // Events
        public event Action<AttachmentInstance> OnAttachmentAdded;
        public event Action<AttachmentInstance, AttachmentType> OnAttachmentRemoved;
        public event Action<FiringMode> OnFiringModeChanged;
        public event Action<AmmoType> OnAmmoTypeChanged;
        public event Action<int, int> OnAmmoChanged; // current, reserve

        public WeaponInstance(WeaponData weapon)
        {
            baseWeapon = weapon;
            instanceId = Guid.NewGuid().ToString();
            customName = weapon.weaponName;
            currentFiringMode = weapon.defaultFiringMode;
            currentAmmoType = weapon.defaultAmmoType;
            currentMagazine = weapon.baseMagazineSize;
            reserveAmmo = weapon.reserveAmmoMax;
        }

        #region Attachment Management

        /// <summary>
        /// Attaches an attachment to the weapon.
        /// </summary>
        public bool AttachAttachment(AttachmentData attachment)
        {
            if (attachment == null) return false;
            if (!attachment.IsCompatibleWith(baseWeapon)) return false;

            var slotType = attachment.attachmentType;

            // Check for incompatible attachments already installed
            foreach (var existing in attachments.Values)
            {
                if (!attachment.IsCompatibleWith(existing.data))
                {
                    return false;
                }
            }

            // Remove existing attachment in the same slot
            if (attachments.TryGetValue(slotType, out var existingAttachment))
            {
                DetachAttachment(slotType);
            }

            // Install new attachment
            var instance = new AttachmentInstance(attachment, slotType);
            attachments[slotType] = instance;
            statsCacheDirty = true;

            OnAttachmentAdded?.Invoke(instance);
            return true;
        }

        /// <summary>
        /// Removes an attachment from a specific slot.
        /// </summary>
        public AttachmentInstance DetachAttachment(AttachmentType slotType)
        {
            if (!attachments.TryGetValue(slotType, out var attachment))
            {
                return null;
            }

            attachments.Remove(slotType);
            statsCacheDirty = true;

            // Destroy spawned model if exists
            if (attachment.spawnedModel != null)
            {
                UnityEngine.Object.Destroy(attachment.spawnedModel);
            }

            OnAttachmentRemoved?.Invoke(attachment, slotType);
            return attachment;
        }

        /// <summary>
        /// Gets the attachment in a specific slot.
        /// </summary>
        public AttachmentInstance GetAttachment(AttachmentType slotType)
        {
            return attachments.TryGetValue(slotType, out var attachment) ? attachment : null;
        }

        /// <summary>
        /// Gets all attached attachments.
        /// </summary>
        public List<AttachmentInstance> GetAllAttachments()
        {
            return attachments.Values.ToList();
        }

        /// <summary>
        /// Checks if a slot has an attachment.
        /// </summary>
        public bool HasAttachment(AttachmentType slotType)
        {
            return attachments.ContainsKey(slotType);
        }

        /// <summary>
        /// Gets empty slots that can accept attachments.
        /// </summary>
        public List<AttachmentType> GetEmptySlots()
        {
            return baseWeapon.GetAvailableSlots()
                .Where(slot => !attachments.ContainsKey(slot))
                .ToList();
        }

        #endregion

        #region Firing Mode

        /// <summary>
        /// Cycles to the next available firing mode.
        /// </summary>
        public void CycleFiringMode()
        {
            if (baseWeapon.supportedFiringModes.Count <= 1) return;

            int currentIndex = baseWeapon.supportedFiringModes.IndexOf(currentFiringMode);
            currentIndex = (currentIndex + 1) % baseWeapon.supportedFiringModes.Count;
            SetFiringMode(baseWeapon.supportedFiringModes[currentIndex]);
        }

        /// <summary>
        /// Sets a specific firing mode.
        /// </summary>
        public bool SetFiringMode(FiringMode mode)
        {
            if (!baseWeapon.supportedFiringModes.Contains(mode))
            {
                return false;
            }

            currentFiringMode = mode;
            OnFiringModeChanged?.Invoke(mode);
            return true;
        }

        #endregion

        #region Ammo Type

        /// <summary>
        /// Changes the ammo type.
        /// </summary>
        public bool SetAmmoType(AmmoType ammoType)
        {
            if (!baseWeapon.compatibleAmmoTypes.Contains(ammoType))
            {
                return false;
            }

            currentAmmoType = ammoType;
            OnAmmoTypeChanged?.Invoke(ammoType);
            return true;
        }

        /// <summary>
        /// Reloads the weapon with available reserve ammo.
        /// </summary>
        public int Reload()
        {
            int magazineCapacity = Mathf.RoundToInt(GetFinalStat(WeaponStatType.MagazineSize));
            int needed = magazineCapacity - currentMagazine;
            int toLoad = Mathf.Min(needed, reserveAmmo);

            currentMagazine += toLoad;
            reserveAmmo -= toLoad;

            OnAmmoChanged?.Invoke(currentMagazine, reserveAmmo);
            return toLoad;
        }

        /// <summary>
        /// Consumes ammo when firing.
        /// </summary>
        public bool ConsumeAmmo(int amount = 1)
        {
            if (currentMagazine < amount) return false;

            currentMagazine -= amount;
            OnAmmoChanged?.Invoke(currentMagazine, reserveAmmo);
            return true;
        }

        /// <summary>
        /// Adds ammo to reserve.
        /// </summary>
        public int AddReserveAmmo(int amount)
        {
            int maxAdd = baseWeapon.reserveAmmoMax - reserveAmmo;
            int toAdd = Mathf.Min(amount, maxAdd);
            reserveAmmo += toAdd;
            OnAmmoChanged?.Invoke(currentMagazine, reserveAmmo);
            return toAdd;
        }

        #endregion

        #region Stats Calculation

        /// <summary>
        /// Gets the final calculated stat after all attachment modifiers.
        /// </summary>
        public float GetFinalStat(WeaponStatType statType)
        {
            if (!statsCacheDirty && cachedStats.TryGetValue(statType, out float cached))
            {
                return cached;
            }

            float baseStat = GetBaseStat(statType);
            float flatBonus = 0f;
            float percentBonus = 0f;

            // Apply all attachment modifiers
            foreach (var attachment in attachments.Values)
            {
                foreach (var modifier in attachment.data.statModifiers)
                {
                    if (modifier.statType == statType)
                    {
                        if (modifier.isPercentage)
                        {
                            percentBonus += modifier.value;
                        }
                        else
                        {
                            flatBonus += modifier.value;
                        }
                    }
                }
            }

            float finalValue = (baseStat + flatBonus) * (1f + percentBonus / 100f);
            cachedStats[statType] = finalValue;

            return finalValue;
        }

        private float GetBaseStat(WeaponStatType statType)
        {
            return statType switch
            {
                WeaponStatType.Damage => baseWeapon.baseDamage,
                WeaponStatType.FireRate => baseWeapon.baseFireRate,
                WeaponStatType.Range => baseWeapon.baseRange,
                WeaponStatType.Accuracy => baseWeapon.baseAccuracy,
                WeaponStatType.RecoilControl => baseWeapon.baseRecoilControl,
                WeaponStatType.ReloadSpeed => baseWeapon.baseReloadSpeed,
                WeaponStatType.MagazineSize => baseWeapon.baseMagazineSize,
                WeaponStatType.AimDownSightSpeed => baseWeapon.baseAimDownSightSpeed,
                WeaponStatType.MovementSpeed => baseWeapon.baseMovementSpeed,
                WeaponStatType.MuzzleVelocity => baseWeapon.baseMuzzleVelocity,
                WeaponStatType.Penetration => baseWeapon.basePenetration,
                WeaponStatType.Stability => baseWeapon.baseStability,
                WeaponStatType.Handling => baseWeapon.baseHandling,
                _ => 0f
            };
        }

        /// <summary>
        /// Gets all final stats for the weapon.
        /// </summary>
        public Dictionary<WeaponStatType, float> GetAllStats()
        {
            var stats = new Dictionary<WeaponStatType, float>();
            foreach (WeaponStatType statType in Enum.GetValues(typeof(WeaponStatType)))
            {
                stats[statType] = GetFinalStat(statType);
            }
            statsCacheDirty = false;
            return stats;
        }

        /// <summary>
        /// Gets the time between shots based on fire rate.
        /// </summary>
        public float GetTimeBetweenShots()
        {
            float rpm = GetFinalStat(WeaponStatType.FireRate);
            return 60f / rpm;
        }

        /// <summary>
        /// Gets the total weight of the weapon including attachments.
        /// </summary>
        public float GetTotalWeight()
        {
            float weight = 0f;
            foreach (var attachment in attachments.Values)
            {
                weight += attachment.data.weight;
            }
            return weight;
        }

        #endregion

        /// <summary>
        /// Invalidates the stats cache, forcing recalculation.
        /// </summary>
        public void InvalidateStatsCache()
        {
            statsCacheDirty = true;
            cachedStats.Clear();
        }

        /// <summary>
        /// Creates a clone of this weapon instance.
        /// </summary>
        public WeaponInstance Clone()
        {
            var clone = new WeaponInstance(baseWeapon)
            {
                customName = customName,
                currentFiringMode = currentFiringMode,
                currentAmmoType = currentAmmoType,
                currentMagazine = currentMagazine,
                reserveAmmo = reserveAmmo
            };

            foreach (var kvp in attachments)
            {
                clone.AttachAttachment(kvp.Value.data);
            }

            return clone;
        }
    }

    #endregion

    #region Weapon Blueprint

    /// <summary>
    /// Represents a weapon configuration blueprint for crafting/loadouts.
    /// </summary>
    [Serializable]
    public class WeaponBlueprint
    {
        public string blueprintId;
        public string blueprintName;
        public WeaponData baseWeapon;
        public List<AttachmentData> attachments = new List<AttachmentData>();
        public FiringMode preferredFiringMode;
        public AmmoType preferredAmmoType;

        /// <summary>
        /// Creates a weapon instance from this blueprint.
        /// </summary>
        public WeaponInstance CreateInstance()
        {
            var instance = new WeaponInstance(baseWeapon);

            foreach (var attachment in attachments)
            {
                instance.AttachAttachment(attachment);
            }

            instance.SetFiringMode(preferredFiringMode);
            instance.SetAmmoType(preferredAmmoType);

            return instance;
        }

        /// <summary>
        /// Validates that all attachments are compatible.
        /// </summary>
        public bool Validate(out List<string> issues)
        {
            issues = new List<string>();

            if (baseWeapon == null)
            {
                issues.Add("Base weapon is not set");
                return false;
            }

            // Check attachment compatibility
            var usedSlots = new HashSet<AttachmentType>();
            foreach (var attachment in attachments)
            {
                if (!attachment.IsCompatibleWith(baseWeapon))
                {
                    issues.Add($"Attachment '{attachment.attachmentName}' is not compatible with weapon '{baseWeapon.weaponName}'");
                }

                if (usedSlots.Contains(attachment.attachmentType))
                {
                    issues.Add($"Multiple attachments for slot '{attachment.attachmentType}'");
                }
                usedSlots.Add(attachment.attachmentType);

                // Check cross-attachment compatibility
                foreach (var other in attachments)
                {
                    if (attachment != other && !attachment.IsCompatibleWith(other))
                    {
                        issues.Add($"Attachment '{attachment.attachmentName}' is incompatible with '{other.attachmentName}'");
                    }
                }
            }

            // Check firing mode
            if (!baseWeapon.supportedFiringModes.Contains(preferredFiringMode))
            {
                issues.Add($"Firing mode '{preferredFiringMode}' is not supported by weapon '{baseWeapon.weaponName}'");
            }

            // Check ammo type
            if (!baseWeapon.compatibleAmmoTypes.Contains(preferredAmmoType))
            {
                issues.Add($"Ammo type '{preferredAmmoType}' is not compatible with weapon '{baseWeapon.weaponName}'");
            }

            return issues.Count == 0;
        }
    }

    #endregion

    #region Crafting Recipe

    /// <summary>
    /// Represents materials required for weapon crafting.
    /// </summary>
    [Serializable]
    public class WeaponCraftingMaterial
    {
        public string materialId;
        public string materialName;
        public int quantity;

        public WeaponCraftingMaterial(string id, string name, int qty)
        {
            materialId = id;
            materialName = name;
            quantity = qty;
        }
    }

    /// <summary>
    /// Represents a weapon crafting recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponRecipe", menuName = "UsefulScripts/Weapons/Weapon Recipe")]
    public class WeaponCraftingRecipe : ScriptableObject
    {
        [Header("Recipe Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Output")]
        public WeaponData resultWeapon;
        public List<AttachmentData> includedAttachments = new List<AttachmentData>();

        [Header("Requirements")]
        public List<WeaponCraftingMaterial> materials = new List<WeaponCraftingMaterial>();
        public int requiredCraftingLevel = 1;
        public float craftingTime = 5f;

        [Header("Unlock")]
        public bool isUnlockedByDefault = false;
        public int unlockCost = 0;

        /// <summary>
        /// Checks if the recipe can be crafted.
        /// </summary>
        public bool CanCraft(Dictionary<string, int> availableMaterials, int craftingLevel)
        {
            if (craftingLevel < requiredCraftingLevel) return false;

            foreach (var material in materials)
            {
                if (!availableMaterials.TryGetValue(material.materialId, out int available) || available < material.quantity)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Represents an attachment crafting recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttachmentRecipe", menuName = "UsefulScripts/Weapons/Attachment Recipe")]
    public class AttachmentCraftingRecipe : ScriptableObject
    {
        [Header("Recipe Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Output")]
        public AttachmentData resultAttachment;

        [Header("Requirements")]
        public List<WeaponCraftingMaterial> materials = new List<WeaponCraftingMaterial>();
        public int requiredCraftingLevel = 1;
        public float craftingTime = 3f;

        [Header("Unlock")]
        public bool isUnlockedByDefault = false;
        public int unlockCost = 0;

        /// <summary>
        /// Checks if the recipe can be crafted.
        /// </summary>
        public bool CanCraft(Dictionary<string, int> availableMaterials, int craftingLevel)
        {
            if (craftingLevel < requiredCraftingLevel) return false;

            foreach (var material in materials)
            {
                if (!availableMaterials.TryGetValue(material.materialId, out int available) || available < material.quantity)
                {
                    return false;
                }
            }

            return true;
        }
    }

    #endregion

    #region Weapon Crafting System

    /// <summary>
    /// Configuration for the weapon crafting system.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCraftingConfig", menuName = "UsefulScripts/Weapons/Crafting Config")]
    public class WeaponCraftingConfig : ScriptableObject
    {
        [Header("General Settings")]
        public float baseCraftingTime = 5f;
        public bool allowCraftingWhileMoving = false;

        [Header("Weapon Recipes")]
        public List<WeaponCraftingRecipe> weaponRecipes = new List<WeaponCraftingRecipe>();

        [Header("Attachment Recipes")]
        public List<AttachmentCraftingRecipe> attachmentRecipes = new List<AttachmentCraftingRecipe>();

        [Header("Available Attachments")]
        public List<AttachmentData> allAttachments = new List<AttachmentData>();

        [Header("Available Ammo Types")]
        public List<AmmoData> allAmmoTypes = new List<AmmoData>();
    }

    /// <summary>
    /// Result of a weapon crafting attempt.
    /// </summary>
    [Serializable]
    public class WeaponCraftingResult
    {
        public bool success;
        public WeaponInstance craftedWeapon;
        public AttachmentData craftedAttachment;
        public string failureReason;
        public float craftedAtTime;

        public WeaponCraftingResult()
        {
            craftedAtTime = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// Complete weapon crafting and customization system.
    /// Manages weapon assembly, attachment fitting, and crafting operations.
    /// </summary>
    public class ModularWeaponSystem : MonoBehaviour
    {
        public static ModularWeaponSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private WeaponCraftingConfig config;

        [Header("Current State")]
        [SerializeField] private bool isCrafting;
        [SerializeField] private float craftingProgress;

        // Runtime data
        private List<WeaponInstance> ownedWeapons = new List<WeaponInstance>();
        private List<AttachmentData> ownedAttachments = new List<AttachmentData>();
        private List<string> unlockedWeaponRecipes = new List<string>();
        private List<string> unlockedAttachmentRecipes = new List<string>();
        private Dictionary<string, int> materials = new Dictionary<string, int>();
        private int craftingLevel = 1;
        private int craftingExperience = 0;
        private Coroutine activeCraftingCoroutine;

        // Delegates for inventory integration
        public Func<string, int> GetMaterialCount;
        public Action<string, int> ConsumeMaterial;
        public Action<string, int> AddMaterial;

        // Events
        public event Action<WeaponCraftingResult> OnWeaponCrafted;
        public event Action<WeaponCraftingResult> OnAttachmentCrafted;
        public event Action<WeaponInstance, AttachmentInstance> OnAttachmentInstalled;
        public event Action<WeaponInstance, AttachmentInstance, AttachmentType> OnAttachmentRemoved;
        public event Action<WeaponInstance, FiringMode> OnFiringModeChanged;
        public event Action<WeaponInstance, AmmoType> OnAmmoTypeChanged;
        public event Action<float> OnCraftingProgress;
        public event Action<int, int> OnCraftingLevelUp;
        public event Action<string> OnRecipeUnlocked;

        // Properties
        public bool IsCrafting => isCrafting;
        public float CraftingProgress => craftingProgress;
        public int CraftingLevel => craftingLevel;
        public int CraftingExperience => craftingExperience;
        public IReadOnlyList<WeaponInstance> OwnedWeapons => ownedWeapons.AsReadOnly();
        public IReadOnlyList<AttachmentData> OwnedAttachments => ownedAttachments.AsReadOnly();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaultRecipes();
        }

        private void OnDisable()
        {
            // Clean up any active crafting coroutine
            if (activeCraftingCoroutine != null)
            {
                StopCoroutine(activeCraftingCoroutine);
                activeCraftingCoroutine = null;
            }
            isCrafting = false;
            craftingProgress = 0f;
        }

        private void InitializeDefaultRecipes()
        {
            if (config == null) return;

            // Unlock default recipes
            foreach (var recipe in config.weaponRecipes.Where(r => r.isUnlockedByDefault))
            {
                unlockedWeaponRecipes.Add(recipe.recipeId);
            }

            foreach (var recipe in config.attachmentRecipes.Where(r => r.isUnlockedByDefault))
            {
                unlockedAttachmentRecipes.Add(recipe.recipeId);
            }
        }

        #region Weapon Management

        /// <summary>
        /// Creates a new weapon instance from weapon data.
        /// </summary>
        public WeaponInstance CreateWeapon(WeaponData weaponData)
        {
            if (weaponData == null) return null;

            var weapon = new WeaponInstance(weaponData);
            ownedWeapons.Add(weapon);
            return weapon;
        }

        /// <summary>
        /// Removes a weapon from ownership.
        /// </summary>
        public bool RemoveWeapon(WeaponInstance weapon)
        {
            return ownedWeapons.Remove(weapon);
        }

        /// <summary>
        /// Gets a weapon by instance ID.
        /// </summary>
        public WeaponInstance GetWeapon(string instanceId)
        {
            return ownedWeapons.FirstOrDefault(w => w.instanceId == instanceId);
        }

        #endregion

        #region Attachment Management

        /// <summary>
        /// Adds an attachment to inventory.
        /// </summary>
        public void AddAttachment(AttachmentData attachment)
        {
            if (attachment != null)
            {
                ownedAttachments.Add(attachment);
            }
        }

        /// <summary>
        /// Removes an attachment from inventory.
        /// </summary>
        public bool RemoveAttachment(AttachmentData attachment)
        {
            return ownedAttachments.Remove(attachment);
        }

        /// <summary>
        /// Installs an attachment on a weapon.
        /// </summary>
        public bool InstallAttachment(WeaponInstance weapon, AttachmentData attachment)
        {
            if (weapon == null || attachment == null) return false;
            if (!ownedAttachments.Contains(attachment)) return false;

            if (weapon.AttachAttachment(attachment))
            {
                ownedAttachments.Remove(attachment);
                var instance = weapon.GetAttachment(attachment.attachmentType);
                OnAttachmentInstalled?.Invoke(weapon, instance);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes an attachment from a weapon and returns it to inventory.
        /// </summary>
        public bool UninstallAttachment(WeaponInstance weapon, AttachmentType slotType)
        {
            if (weapon == null) return false;

            var removed = weapon.DetachAttachment(slotType);
            if (removed != null)
            {
                ownedAttachments.Add(removed.data);
                OnAttachmentRemoved?.Invoke(weapon, removed, slotType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all compatible attachments for a weapon slot.
        /// </summary>
        public List<AttachmentData> GetCompatibleAttachments(WeaponInstance weapon, AttachmentType slotType)
        {
            if (weapon == null) return new List<AttachmentData>();

            return ownedAttachments
                .Where(a => a.attachmentType == slotType && a.IsCompatibleWith(weapon.baseWeapon))
                .ToList();
        }

        /// <summary>
        /// Gets all available attachments in the config.
        /// </summary>
        public List<AttachmentData> GetAllAvailableAttachments()
        {
            return config != null ? new List<AttachmentData>(config.allAttachments) : new List<AttachmentData>();
        }

        #endregion

        #region Firing Mode & Ammo

        /// <summary>
        /// Changes the firing mode of a weapon.
        /// </summary>
        public bool SetWeaponFiringMode(WeaponInstance weapon, FiringMode mode)
        {
            if (weapon == null) return false;

            if (weapon.SetFiringMode(mode))
            {
                OnFiringModeChanged?.Invoke(weapon, mode);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the ammo type of a weapon.
        /// </summary>
        public bool SetWeaponAmmoType(WeaponInstance weapon, AmmoType ammoType)
        {
            if (weapon == null) return false;

            if (weapon.SetAmmoType(ammoType))
            {
                OnAmmoTypeChanged?.Invoke(weapon, ammoType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets ammo data for a specific ammo type.
        /// </summary>
        public AmmoData GetAmmoData(AmmoType ammoType)
        {
            return config?.allAmmoTypes.FirstOrDefault(a => a.ammoType == ammoType);
        }

        #endregion

        #region Crafting

        /// <summary>
        /// Starts crafting a weapon from a recipe.
        /// </summary>
        public bool StartCraftWeapon(string recipeId, Action<WeaponCraftingResult> callback = null)
        {
            if (isCrafting) return false;

            var recipe = config?.weaponRecipes.FirstOrDefault(r => r.recipeId == recipeId);
            if (recipe == null) return false;

            if (!unlockedWeaponRecipes.Contains(recipeId)) return false;

            var availableMaterials = GetAvailableMaterials();
            if (!recipe.CanCraft(availableMaterials, craftingLevel)) return false;

            activeCraftingCoroutine = StartCoroutine(CraftWeaponCoroutine(recipe, callback));
            return true;
        }

        private System.Collections.IEnumerator CraftWeaponCoroutine(WeaponCraftingRecipe recipe, Action<WeaponCraftingResult> callback)
        {
            isCrafting = true;
            craftingProgress = 0f;

            // Consume materials
            foreach (var material in recipe.materials)
            {
                ConsumeMaterial?.Invoke(material.materialId, material.quantity);
            }

            float elapsedTime = 0f;
            while (elapsedTime < recipe.craftingTime)
            {
                elapsedTime += Time.deltaTime;
                craftingProgress = elapsedTime / recipe.craftingTime;
                OnCraftingProgress?.Invoke(craftingProgress);
                yield return null;
            }

            // Create the weapon
            var result = new WeaponCraftingResult { success = true };
            var weapon = CreateWeapon(recipe.resultWeapon);

            // Add included attachments
            foreach (var attachment in recipe.includedAttachments)
            {
                weapon.AttachAttachment(attachment);
            }

            result.craftedWeapon = weapon;

            // Add experience
            AddCraftingExperience(recipe.requiredCraftingLevel * 10);

            isCrafting = false;
            craftingProgress = 0f;
            activeCraftingCoroutine = null;

            OnWeaponCrafted?.Invoke(result);
            callback?.Invoke(result);
        }

        /// <summary>
        /// Starts crafting an attachment from a recipe.
        /// </summary>
        public bool StartCraftAttachment(string recipeId, Action<WeaponCraftingResult> callback = null)
        {
            if (isCrafting) return false;

            var recipe = config?.attachmentRecipes.FirstOrDefault(r => r.recipeId == recipeId);
            if (recipe == null) return false;

            if (!unlockedAttachmentRecipes.Contains(recipeId)) return false;

            var availableMaterials = GetAvailableMaterials();
            if (!recipe.CanCraft(availableMaterials, craftingLevel)) return false;

            activeCraftingCoroutine = StartCoroutine(CraftAttachmentCoroutine(recipe, callback));
            return true;
        }

        private System.Collections.IEnumerator CraftAttachmentCoroutine(AttachmentCraftingRecipe recipe, Action<WeaponCraftingResult> callback)
        {
            isCrafting = true;
            craftingProgress = 0f;

            // Consume materials
            foreach (var material in recipe.materials)
            {
                ConsumeMaterial?.Invoke(material.materialId, material.quantity);
            }

            float elapsedTime = 0f;
            while (elapsedTime < recipe.craftingTime)
            {
                elapsedTime += Time.deltaTime;
                craftingProgress = elapsedTime / recipe.craftingTime;
                OnCraftingProgress?.Invoke(craftingProgress);
                yield return null;
            }

            // Create the attachment
            var result = new WeaponCraftingResult { success = true };
            AddAttachment(recipe.resultAttachment);
            result.craftedAttachment = recipe.resultAttachment;

            // Add experience
            AddCraftingExperience(recipe.requiredCraftingLevel * 5);

            isCrafting = false;
            craftingProgress = 0f;
            activeCraftingCoroutine = null;

            OnAttachmentCrafted?.Invoke(result);
            callback?.Invoke(result);
        }

        /// <summary>
        /// Cancels the current crafting operation.
        /// </summary>
        public void CancelCrafting()
        {
            if (activeCraftingCoroutine != null)
            {
                StopCoroutine(activeCraftingCoroutine);
                activeCraftingCoroutine = null;
            }
            isCrafting = false;
            craftingProgress = 0f;
        }

        /// <summary>
        /// Unlocks a weapon recipe.
        /// </summary>
        public bool UnlockWeaponRecipe(string recipeId)
        {
            if (unlockedWeaponRecipes.Contains(recipeId)) return false;

            var recipe = config?.weaponRecipes.FirstOrDefault(r => r.recipeId == recipeId);
            if (recipe == null) return false;

            unlockedWeaponRecipes.Add(recipeId);
            OnRecipeUnlocked?.Invoke(recipeId);
            return true;
        }

        /// <summary>
        /// Unlocks an attachment recipe.
        /// </summary>
        public bool UnlockAttachmentRecipe(string recipeId)
        {
            if (unlockedAttachmentRecipes.Contains(recipeId)) return false;

            var recipe = config?.attachmentRecipes.FirstOrDefault(r => r.recipeId == recipeId);
            if (recipe == null) return false;

            unlockedAttachmentRecipes.Add(recipeId);
            OnRecipeUnlocked?.Invoke(recipeId);
            return true;
        }

        /// <summary>
        /// Gets all available weapon recipes.
        /// </summary>
        public List<WeaponCraftingRecipe> GetAvailableWeaponRecipes()
        {
            return config?.weaponRecipes
                .Where(r => unlockedWeaponRecipes.Contains(r.recipeId))
                .ToList() ?? new List<WeaponCraftingRecipe>();
        }

        /// <summary>
        /// Gets all available attachment recipes.
        /// </summary>
        public List<AttachmentCraftingRecipe> GetAvailableAttachmentRecipes()
        {
            return config?.attachmentRecipes
                .Where(r => unlockedAttachmentRecipes.Contains(r.recipeId))
                .ToList() ?? new List<AttachmentCraftingRecipe>();
        }

        private Dictionary<string, int> GetAvailableMaterials()
        {
            if (GetMaterialCount == null) return materials;

            // Use external inventory if available
            var available = new Dictionary<string, int>();
            foreach (var materialId in materials.Keys)
            {
                available[materialId] = GetMaterialCount(materialId);
            }
            return available;
        }

        #endregion

        #region Crafting Level

        /// <summary>
        /// Adds crafting experience.
        /// </summary>
        public void AddCraftingExperience(int amount)
        {
            craftingExperience += amount;
            int expNeeded = GetExperienceForLevel(craftingLevel + 1);

            while (craftingExperience >= expNeeded && craftingLevel < 100)
            {
                craftingExperience -= expNeeded;
                int previousLevel = craftingLevel;
                craftingLevel++;
                expNeeded = GetExperienceForLevel(craftingLevel + 1);
                OnCraftingLevelUp?.Invoke(previousLevel, craftingLevel);
            }
        }

        private int GetExperienceForLevel(int level)
        {
            return (int)(100 * Mathf.Pow(level, 1.5f));
        }

        #endregion

        #region Blueprints

        /// <summary>
        /// Creates a blueprint from a weapon instance.
        /// </summary>
        public WeaponBlueprint CreateBlueprint(WeaponInstance weapon, string blueprintName)
        {
            if (weapon == null) return null;

            return new WeaponBlueprint
            {
                blueprintId = Guid.NewGuid().ToString(),
                blueprintName = blueprintName,
                baseWeapon = weapon.baseWeapon,
                attachments = weapon.GetAllAttachments().Select(a => a.data).ToList(),
                preferredFiringMode = weapon.currentFiringMode,
                preferredAmmoType = weapon.currentAmmoType
            };
        }

        /// <summary>
        /// Creates a weapon instance from a blueprint.
        /// </summary>
        public WeaponInstance CreateFromBlueprint(WeaponBlueprint blueprint)
        {
            if (blueprint == null) return null;

            if (blueprint.Validate(out var issues))
            {
                var weapon = blueprint.CreateInstance();
                ownedWeapons.Add(weapon);
                return weapon;
            }

            Debug.LogWarning($"Blueprint validation failed: {string.Join(", ", issues)}");
            return null;
        }

        #endregion

        #region Materials

        /// <summary>
        /// Adds materials to the internal inventory.
        /// </summary>
        public void AddMaterials(string materialId, int quantity)
        {
            if (!materials.ContainsKey(materialId))
            {
                materials[materialId] = 0;
            }
            materials[materialId] += quantity;
            AddMaterial?.Invoke(materialId, quantity);
        }

        /// <summary>
        /// Gets the count of a material.
        /// </summary>
        public int GetMaterialsCount(string materialId)
        {
            if (GetMaterialCount != null)
            {
                return GetMaterialCount(materialId);
            }
            return materials.TryGetValue(materialId, out int count) ? count : 0;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a stat comparison between current weapon config and with a new attachment.
        /// </summary>
        public Dictionary<WeaponStatType, float> GetStatComparison(WeaponInstance weapon, AttachmentData newAttachment)
        {
            if (weapon == null || newAttachment == null)
            {
                return new Dictionary<WeaponStatType, float>();
            }

            var comparison = new Dictionary<WeaponStatType, float>();
            var currentStats = weapon.GetAllStats();

            // Clone weapon and add new attachment
            var tempWeapon = weapon.Clone();
            tempWeapon.AttachAttachment(newAttachment);
            var newStats = tempWeapon.GetAllStats();

            foreach (var statType in currentStats.Keys)
            {
                comparison[statType] = newStats[statType] - currentStats[statType];
            }

            return comparison;
        }

        /// <summary>
        /// Checks if an attachment can be installed on a weapon.
        /// </summary>
        public bool CanInstallAttachment(WeaponInstance weapon, AttachmentData attachment)
        {
            if (weapon == null || attachment == null) return false;
            return attachment.IsCompatibleWith(weapon.baseWeapon);
        }

        /// <summary>
        /// Gets all weapons of a specific category.
        /// </summary>
        public List<WeaponInstance> GetWeaponsByCategory(WeaponCategory category)
        {
            return ownedWeapons.Where(w => w.baseWeapon.category == category).ToList();
        }

        #endregion
    }

    #endregion
}
