using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Inventory
{
    /// <summary>
    /// Represents an item definition that can be stored in inventory.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "UsefulScripts/Inventory/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("Stack Settings")]
        public bool isStackable = true;
        public int maxStackSize = 99;
        
        [Header("Item Properties")]
        public ItemType itemType = ItemType.Misc;
        public ItemRarity rarity = ItemRarity.Common;
        public float weight = 1f;
        public int baseValue = 10;
        
        [Header("Equipment Settings")]
        public EquipmentSlot equipSlot = EquipmentSlot.None;
        public List<ItemStat> stats = new List<ItemStat>();
        
        [Header("Consumable Settings")]
        public bool isConsumable = false;
        public float cooldown = 0f;
        public List<ConsumableEffect> consumeEffects = new List<ConsumableEffect>();
    }
    
    public enum ItemType
    {
        Misc,
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material,
        Quest,
        Currency
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
    
    public enum EquipmentSlot
    {
        None,
        Head,
        Chest,
        Legs,
        Feet,
        Hands,
        MainHand,
        OffHand,
        Ring1,
        Ring2,
        Necklace,
        Back
    }
    
    public enum StatType
    {
        Health,
        Mana,
        Stamina,
        Attack,
        Defense,
        Speed,
        CritChance,
        CritDamage,
        HealthRegen,
        ManaRegen
    }
    
    [Serializable]
    public class ItemStat
    {
        public StatType statType;
        public float value;
        public bool isPercentage;
    }
    
    [Serializable]
    public class ConsumableEffect
    {
        public ConsumableEffectType effectType;
        public float value;
        public float duration;
    }
    
    public enum ConsumableEffectType
    {
        RestoreHealth,
        RestoreMana,
        RestoreStamina,
        BoostAttack,
        BoostDefense,
        BoostSpeed,
        RemoveDebuffs,
        ApplyBuff
    }
    
    /// <summary>
    /// Represents an instance of an item in the inventory with quantity and unique instance data.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public ItemData itemData;
        public int quantity;
        public string instanceId;
        public Dictionary<string, object> customData;
        
        public InventoryItem(ItemData data, int qty = 1)
        {
            itemData = data;
            quantity = qty;
            instanceId = Guid.NewGuid().ToString();
            customData = new Dictionary<string, object>();
        }
        
        public bool CanStack => itemData != null && itemData.isStackable && quantity < itemData.maxStackSize;
        public int AvailableStackSpace => itemData != null ? itemData.maxStackSize - quantity : 0;
        
        public InventoryItem Clone()
        {
            var clone = new InventoryItem(itemData, quantity)
            {
                instanceId = Guid.NewGuid().ToString(),
                customData = new Dictionary<string, object>(customData)
            };
            return clone;
        }
    }
    
    /// <summary>
    /// Represents an inventory slot that can hold an item.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public int slotIndex;
        public InventoryItem item;
        public bool isLocked;
        public SlotType slotType;
        
        public bool IsEmpty => item == null;
        public bool HasItem => item != null;
        
        public InventorySlot(int index, SlotType type = SlotType.General)
        {
            slotIndex = index;
            slotType = type;
            item = null;
            isLocked = false;
        }
        
        public bool CanAcceptItem(ItemData itemData)
        {
            if (isLocked) return false;
            if (slotType == SlotType.General) return true;
            
            return slotType switch
            {
                SlotType.Weapon => itemData.itemType == ItemType.Weapon,
                SlotType.Armor => itemData.itemType == ItemType.Armor,
                SlotType.Consumable => itemData.itemType == ItemType.Consumable,
                SlotType.Material => itemData.itemType == ItemType.Material,
                _ => true
            };
        }
    }
    
    public enum SlotType
    {
        General,
        Weapon,
        Armor,
        Consumable,
        Material,
        Equipment
    }
    
    /// <summary>
    /// Complete inventory management system with slots, stacking, equipment, and events.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }
        
        [Header("Inventory Settings")]
        [SerializeField] private int inventorySize = 30;
        [SerializeField] private float maxWeight = 100f;
        [SerializeField] private bool useWeightLimit = true;
        
        [Header("Equipment Slots")]
        [SerializeField] private bool enableEquipment = true;
        
        private List<InventorySlot> slots = new List<InventorySlot>();
        private Dictionary<EquipmentSlot, InventoryItem> equippedItems = new Dictionary<EquipmentSlot, InventoryItem>();
        private Dictionary<string, float> consumableCooldowns = new Dictionary<string, float>();
        
        // Events
        public event Action<InventoryItem, int> OnItemAdded;
        public event Action<InventoryItem, int> OnItemRemoved;
        public event Action<InventoryItem, int, int> OnItemMoved;
        public event Action<InventoryItem, EquipmentSlot> OnItemEquipped;
        public event Action<InventoryItem, EquipmentSlot> OnItemUnequipped;
        public event Action<InventoryItem> OnItemUsed;
        public event Action OnInventoryChanged;
        
        // Properties
        public int SlotCount => slots.Count;
        public float CurrentWeight => CalculateTotalWeight();
        public float MaxWeight => maxWeight;
        public bool IsOverweight => useWeightLimit && CurrentWeight > maxWeight;
        public int EmptySlotCount => slots.Count(s => s.IsEmpty && !s.isLocked);
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeInventory();
            InitializeEquipmentSlots();
        }
        
        private void Update()
        {
            UpdateCooldowns();
        }
        
        private void InitializeInventory()
        {
            slots.Clear();
            for (int i = 0; i < inventorySize; i++)
            {
                slots.Add(new InventorySlot(i));
            }
        }
        
        private void InitializeEquipmentSlots()
        {
            if (!enableEquipment) return;
            
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot != EquipmentSlot.None)
                {
                    equippedItems[slot] = null;
                }
            }
        }
        
        private void UpdateCooldowns()
        {
            var keys = consumableCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                consumableCooldowns[key] -= Time.deltaTime;
                if (consumableCooldowns[key] <= 0f)
                {
                    consumableCooldowns.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// Adds an item to the inventory. Returns the amount that couldn't be added.
        /// </summary>
        public int AddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return quantity;
            
            int remaining = quantity;
            
            // Try to stack with existing items first
            if (itemData.isStackable)
            {
                foreach (var slot in slots.Where(s => s.HasItem && s.item.itemData == itemData && s.item.CanStack))
                {
                    int toAdd = Mathf.Min(remaining, slot.item.AvailableStackSpace);
                    slot.item.quantity += toAdd;
                    remaining -= toAdd;
                    
                    if (remaining <= 0) break;
                }
            }
            
            // Add to empty slots
            while (remaining > 0)
            {
                var emptySlot = slots.FirstOrDefault(s => s.IsEmpty && !s.isLocked && s.CanAcceptItem(itemData));
                if (emptySlot == null) break;
                
                int toAdd = itemData.isStackable ? Mathf.Min(remaining, itemData.maxStackSize) : 1;
                
                // Check weight limit
                if (useWeightLimit && CurrentWeight + (toAdd * itemData.weight) > maxWeight)
                {
                    toAdd = Mathf.FloorToInt((maxWeight - CurrentWeight) / itemData.weight);
                    if (toAdd <= 0) break;
                }
                
                emptySlot.item = new InventoryItem(itemData, toAdd);
                remaining -= toAdd;
                
                OnItemAdded?.Invoke(emptySlot.item, emptySlot.slotIndex);
            }
            
            if (remaining < quantity)
            {
                OnInventoryChanged?.Invoke();
            }
            
            return remaining;
        }
        
        /// <summary>
        /// Removes an item from the inventory. Returns true if successful.
        /// </summary>
        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return false;
            
            int totalAvailable = GetItemCount(itemData);
            if (totalAvailable < quantity) return false;
            
            int remaining = quantity;
            
            // Remove from slots (prefer smaller stacks first)
            var slotsWithItem = slots.Where(s => s.HasItem && s.item.itemData == itemData)
                                     .OrderBy(s => s.item.quantity)
                                     .ToList();
            
            foreach (var slot in slotsWithItem)
            {
                int toRemove = Mathf.Min(remaining, slot.item.quantity);
                slot.item.quantity -= toRemove;
                remaining -= toRemove;
                
                OnItemRemoved?.Invoke(slot.item, slot.slotIndex);
                
                if (slot.item.quantity <= 0)
                {
                    slot.item = null;
                }
                
                if (remaining <= 0) break;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Removes an item from a specific slot.
        /// </summary>
        public bool RemoveItemAtSlot(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return false;
            
            int toRemove = Mathf.Min(quantity, slot.item.quantity);
            slot.item.quantity -= toRemove;
            
            OnItemRemoved?.Invoke(slot.item, slotIndex);
            
            if (slot.item.quantity <= 0)
            {
                slot.item = null;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Moves an item from one slot to another.
        /// </summary>
        public bool MoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= slots.Count || 
                toSlot < 0 || toSlot >= slots.Count) return false;
            
            var source = slots[fromSlot];
            var dest = slots[toSlot];
            
            if (source.IsEmpty) return false;
            if (dest.isLocked) return false;
            if (!dest.CanAcceptItem(source.item.itemData)) return false;
            
            // If destination is empty, just move
            if (dest.IsEmpty)
            {
                dest.item = source.item;
                source.item = null;
                OnItemMoved?.Invoke(dest.item, fromSlot, toSlot);
            }
            // If same item and stackable, try to merge
            else if (dest.item.itemData == source.item.itemData && dest.item.CanStack)
            {
                int toAdd = Mathf.Min(source.item.quantity, dest.item.AvailableStackSpace);
                dest.item.quantity += toAdd;
                source.item.quantity -= toAdd;
                
                if (source.item.quantity <= 0)
                {
                    source.item = null;
                }
                
                OnItemMoved?.Invoke(dest.item, fromSlot, toSlot);
            }
            // Otherwise swap
            else
            {
                (source.item, dest.item) = (dest.item, source.item);
                OnItemMoved?.Invoke(dest.item, fromSlot, toSlot);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Splits a stack into two.
        /// </summary>
        public bool SplitStack(int slotIndex, int amount)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            
            var slot = slots[slotIndex];
            if (slot.IsEmpty || slot.item.quantity <= amount) return false;
            
            var emptySlot = slots.FirstOrDefault(s => s.IsEmpty && !s.isLocked);
            if (emptySlot == null) return false;
            
            slot.item.quantity -= amount;
            emptySlot.item = new InventoryItem(slot.item.itemData, amount);
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Uses a consumable item if possible.
        /// </summary>
        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return false;
            
            var item = slot.item;
            if (!item.itemData.isConsumable) return false;
            
            // Check cooldown
            if (consumableCooldowns.ContainsKey(item.itemData.itemId))
            {
                return false;
            }
            
            // Apply effects
            foreach (var effect in item.itemData.consumeEffects)
            {
                ApplyConsumableEffect(effect);
            }
            
            // Start cooldown
            if (item.itemData.cooldown > 0)
            {
                consumableCooldowns[item.itemData.itemId] = item.itemData.cooldown;
            }
            
            OnItemUsed?.Invoke(item);
            
            // Remove one from stack
            RemoveItemAtSlot(slotIndex, 1);
            
            return true;
        }
        
        private void ApplyConsumableEffect(ConsumableEffect effect)
        {
            // This would integrate with your HealthSystem, AbilitySystem, etc.
            // For now, we just trigger the effect - implementation depends on your game
            Debug.Log($"Applied effect: {effect.effectType} with value {effect.value} for {effect.duration}s");
        }
        
        /// <summary>
        /// Equips an item from the inventory.
        /// </summary>
        public bool EquipItem(int slotIndex)
        {
            if (!enableEquipment) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return false;
            
            var itemData = slot.item.itemData;
            if (itemData.equipSlot == EquipmentSlot.None) return false;
            
            var equipSlot = itemData.equipSlot;
            
            // Unequip current item if any
            if (equippedItems[equipSlot] != null)
            {
                var previousItem = equippedItems[equipSlot];
                int remaining = AddItem(previousItem.itemData, 1);
                if (remaining > 0) return false; // No space to unequip
                
                OnItemUnequipped?.Invoke(previousItem, equipSlot);
            }
            
            // Equip new item
            equippedItems[equipSlot] = slot.item;
            slot.item = null;
            
            OnItemEquipped?.Invoke(equippedItems[equipSlot], equipSlot);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Unequips an item to the inventory.
        /// </summary>
        public bool UnequipItem(EquipmentSlot equipSlot)
        {
            if (!enableEquipment) return false;
            if (equippedItems[equipSlot] == null) return false;
            
            var item = equippedItems[equipSlot];
            int remaining = AddItem(item.itemData, 1);
            if (remaining > 0) return false;
            
            equippedItems[equipSlot] = null;
            
            OnItemUnequipped?.Invoke(item, equipSlot);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Gets the total count of a specific item in the inventory.
        /// </summary>
        public int GetItemCount(ItemData itemData)
        {
            return slots.Where(s => s.HasItem && s.item.itemData == itemData)
                       .Sum(s => s.item.quantity);
        }
        
        /// <summary>
        /// Checks if the inventory has at least the specified amount of an item.
        /// </summary>
        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            return GetItemCount(itemData) >= quantity;
        }
        
        /// <summary>
        /// Gets the item at a specific slot.
        /// </summary>
        public InventoryItem GetItemAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return null;
            return slots[slotIndex].item;
        }
        
        /// <summary>
        /// Gets all equipped items.
        /// </summary>
        public Dictionary<EquipmentSlot, InventoryItem> GetEquippedItems()
        {
            return new Dictionary<EquipmentSlot, InventoryItem>(equippedItems);
        }
        
        /// <summary>
        /// Gets total stat bonuses from equipped items.
        /// </summary>
        public Dictionary<StatType, float> GetEquipmentStats()
        {
            var stats = new Dictionary<StatType, float>();
            
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                stats[statType] = 0f;
            }
            
            foreach (var equipped in equippedItems.Values)
            {
                if (equipped?.itemData?.stats == null) continue;
                
                foreach (var stat in equipped.itemData.stats)
                {
                    stats[stat.statType] += stat.value;
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// Gets the cooldown remaining for a consumable.
        /// </summary>
        public float GetCooldownRemaining(string itemId)
        {
            return consumableCooldowns.TryGetValue(itemId, out float cd) ? cd : 0f;
        }
        
        /// <summary>
        /// Gets all items in the inventory.
        /// </summary>
        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(slots);
        }
        
        /// <summary>
        /// Sorts the inventory by specified criteria.
        /// </summary>
        public void SortInventory(SortCriteria criteria)
        {
            var items = slots.Where(s => s.HasItem).Select(s => s.item).ToList();
            
            // Clear all slots
            foreach (var slot in slots)
            {
                slot.item = null;
            }
            
            // Sort items
            IEnumerable<InventoryItem> sorted = criteria switch
            {
                SortCriteria.Name => items.OrderBy(i => i.itemData.itemName),
                SortCriteria.Type => items.OrderBy(i => i.itemData.itemType).ThenBy(i => i.itemData.itemName),
                SortCriteria.Rarity => items.OrderByDescending(i => i.itemData.rarity).ThenBy(i => i.itemData.itemName),
                SortCriteria.Value => items.OrderByDescending(i => i.itemData.baseValue),
                SortCriteria.Quantity => items.OrderByDescending(i => i.quantity),
                _ => items
            };
            
            // Reassign to slots
            int slotIndex = 0;
            foreach (var item in sorted)
            {
                while (slotIndex < slots.Count && slots[slotIndex].isLocked)
                {
                    slotIndex++;
                }
                
                if (slotIndex < slots.Count)
                {
                    slots[slotIndex].item = item;
                    slotIndex++;
                }
            }
            
            OnInventoryChanged?.Invoke();
        }
        
        private float CalculateTotalWeight()
        {
            float weight = slots.Where(s => s.HasItem)
                                .Sum(s => s.item.itemData.weight * s.item.quantity);
            
            if (enableEquipment)
            {
                weight += equippedItems.Values.Where(i => i != null)
                                              .Sum(i => i.itemData.weight);
            }
            
            return weight;
        }
        
        /// <summary>
        /// Clears the entire inventory.
        /// </summary>
        public void ClearInventory()
        {
            foreach (var slot in slots)
            {
                slot.item = null;
            }
            
            OnInventoryChanged?.Invoke();
        }
        
        /// <summary>
        /// Expands the inventory by the specified amount.
        /// </summary>
        public void ExpandInventory(int additionalSlots)
        {
            int startIndex = slots.Count;
            for (int i = 0; i < additionalSlots; i++)
            {
                slots.Add(new InventorySlot(startIndex + i));
            }
            
            OnInventoryChanged?.Invoke();
        }
    }
    
    public enum SortCriteria
    {
        Name,
        Type,
        Rarity,
        Value,
        Quantity
    }
}
