using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Weapon management system for spaceships.
    /// Handles hardpoints, firing groups, and weapon coordination.
    /// Inspired by Elite Dangerous and Star Citizen weapon systems.
    /// </summary>
    public class ShipWeaponSystem : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private List<WeaponHardpoint> hardpoints = new List<WeaponHardpoint>();
        [SerializeField] private int maxFiringGroups = 2;

        [Header("Targeting Integration")]
        [SerializeField] private ShipTargetingSystem targetingSystem;
        [SerializeField] private bool autoAssignTargets = true;

        [Header("Power Management")]
        [SerializeField] private float maxWeaponPower = 100f;
        [SerializeField] private float currentWeaponPower = 100f;
        [SerializeField] private float powerRechargeRate = 10f;
        [SerializeField] private float powerPerShot = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource weaponAudioSource;

        // State
        private Dictionary<int, List<WeaponHardpoint>> firingGroups = new Dictionary<int, List<WeaponHardpoint>>();
        private bool[] groupFiring;
        private int activeFiringGroup = 0;

        // Events
        public event System.Action<int, WeaponHardpoint> OnWeaponAdded;
        public event System.Action<int, WeaponHardpoint> OnWeaponRemoved;
        public event System.Action<int> OnFiringGroupChanged;
        public event System.Action<WeaponHardpoint> OnWeaponFired;
        public event System.Action OnAllWeaponsFired;
        public event System.Action<float, float> OnPowerChanged; // current, max
        public event System.Action OnPowerDepleted;

        // Properties
        public List<WeaponHardpoint> Hardpoints => hardpoints;
        public int ActiveFiringGroup => activeFiringGroup;
        public float CurrentPower => currentWeaponPower;
        public float MaxPower => maxWeaponPower;
        public float PowerPercent => currentWeaponPower / maxWeaponPower;
        public bool HasPower => currentWeaponPower >= powerPerShot;
        public int HardpointCount => hardpoints.Count;

        private void Awake()
        {
            groupFiring = new bool[maxFiringGroups];
            
            // Initialize firing groups
            for (int i = 0; i < maxFiringGroups; i++)
            {
                firingGroups[i] = new List<WeaponHardpoint>();
            }

            if (targetingSystem == null)
            {
                targetingSystem = GetComponent<ShipTargetingSystem>();
            }
        }

        private void Start()
        {
            // Auto-assign hardpoints to default firing groups
            AssignDefaultFiringGroups();
        }

        private void Update()
        {
            UpdatePower();
            UpdateTargeting();
            UpdateFiring();
        }

        #region Power Management

        private void UpdatePower()
        {
            if (currentWeaponPower < maxWeaponPower)
            {
                currentWeaponPower += powerRechargeRate * Time.deltaTime;
                currentWeaponPower = Mathf.Min(currentWeaponPower, maxWeaponPower);
                OnPowerChanged?.Invoke(currentWeaponPower, maxWeaponPower);
            }
        }

        /// <summary>
        /// Consume weapon power
        /// </summary>
        public bool ConsumePower(float amount)
        {
            if (currentWeaponPower < amount)
            {
                OnPowerDepleted?.Invoke();
                return false;
            }

            currentWeaponPower -= amount;
            OnPowerChanged?.Invoke(currentWeaponPower, maxWeaponPower);
            return true;
        }

        /// <summary>
        /// Add power to weapons
        /// </summary>
        public void AddPower(float amount)
        {
            currentWeaponPower = Mathf.Min(currentWeaponPower + amount, maxWeaponPower);
            OnPowerChanged?.Invoke(currentWeaponPower, maxWeaponPower);
        }

        /// <summary>
        /// Set power capacity
        /// </summary>
        public void SetPowerCapacity(float capacity)
        {
            maxWeaponPower = Mathf.Max(1f, capacity);
            currentWeaponPower = Mathf.Min(currentWeaponPower, maxWeaponPower);
            OnPowerChanged?.Invoke(currentWeaponPower, maxWeaponPower);
        }

        #endregion

        #region Targeting

        private void UpdateTargeting()
        {
            if (!autoAssignTargets || targetingSystem == null) return;

            Transform target = targetingSystem.CurrentTarget;
            if (target != null)
            {
                foreach (var hardpoint in hardpoints)
                {
                    if (hardpoint.turret != null)
                    {
                        hardpoint.turret.SetTarget(target);
                    }
                }
            }
        }

        /// <summary>
        /// Aim all weapons at a point
        /// </summary>
        public void AimAt(Vector3 point)
        {
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null)
                {
                    hardpoint.turret.SetAimPoint(point);
                }
            }
        }

        /// <summary>
        /// Aim all weapons at a target
        /// </summary>
        public void AimAt(Transform target)
        {
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null)
                {
                    hardpoint.turret.TrackTarget(target);
                }
            }
        }

        #endregion

        #region Firing

        private void UpdateFiring()
        {
            for (int i = 0; i < maxFiringGroups; i++)
            {
                if (groupFiring[i] && firingGroups.ContainsKey(i))
                {
                    FireGroup(i);
                }
            }
        }

        /// <summary>
        /// Start firing a weapon group
        /// </summary>
        public void StartFiringGroup(int groupIndex)
        {
            if (groupIndex >= 0 && groupIndex < maxFiringGroups)
            {
                groupFiring[groupIndex] = true;
            }
        }

        /// <summary>
        /// Stop firing a weapon group
        /// </summary>
        public void StopFiringGroup(int groupIndex)
        {
            if (groupIndex >= 0 && groupIndex < maxFiringGroups)
            {
                groupFiring[groupIndex] = false;

                // Stop all turrets in group
                if (firingGroups.ContainsKey(groupIndex))
                {
                    foreach (var hardpoint in firingGroups[groupIndex])
                    {
                        if (hardpoint.turret != null)
                        {
                            hardpoint.turret.StopFiring();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fire all weapons in a group once
        /// </summary>
        public void FireGroup(int groupIndex)
        {
            if (!firingGroups.ContainsKey(groupIndex)) return;

            bool anyFired = false;
            foreach (var hardpoint in firingGroups[groupIndex])
            {
                if (!hardpoint.isActive || hardpoint.turret == null) continue;

                // Check power
                if (!ConsumePower(powerPerShot)) continue;

                if (hardpoint.turret.Fire())
                {
                    anyFired = true;
                    OnWeaponFired?.Invoke(hardpoint);
                }
            }

            if (anyFired)
            {
                OnAllWeaponsFired?.Invoke();
            }
        }

        /// <summary>
        /// Fire all weapons
        /// </summary>
        public void FireAllWeapons()
        {
            foreach (var kvp in firingGroups)
            {
                FireGroup(kvp.Key);
            }
        }

        /// <summary>
        /// Stop all weapon firing
        /// </summary>
        public void CeaseFireAll()
        {
            for (int i = 0; i < maxFiringGroups; i++)
            {
                StopFiringGroup(i);
            }
        }

        #endregion

        #region Hardpoint Management

        /// <summary>
        /// Add a hardpoint
        /// </summary>
        public void AddHardpoint(WeaponHardpoint hardpoint)
        {
            if (!hardpoints.Contains(hardpoint))
            {
                hardpoints.Add(hardpoint);
                OnWeaponAdded?.Invoke(hardpoints.Count - 1, hardpoint);
            }
        }

        /// <summary>
        /// Remove a hardpoint
        /// </summary>
        public void RemoveHardpoint(int index)
        {
            if (index >= 0 && index < hardpoints.Count)
            {
                WeaponHardpoint removed = hardpoints[index];
                hardpoints.RemoveAt(index);

                // Remove from firing groups
                foreach (var group in firingGroups.Values)
                {
                    group.Remove(removed);
                }

                OnWeaponRemoved?.Invoke(index, removed);
            }
        }

        /// <summary>
        /// Get hardpoint by index
        /// </summary>
        public WeaponHardpoint GetHardpoint(int index)
        {
            if (index >= 0 && index < hardpoints.Count)
            {
                return hardpoints[index];
            }
            return null;
        }

        /// <summary>
        /// Toggle hardpoint active state
        /// </summary>
        public void ToggleHardpoint(int index)
        {
            if (index >= 0 && index < hardpoints.Count)
            {
                hardpoints[index].isActive = !hardpoints[index].isActive;
            }
        }

        #endregion

        #region Firing Groups

        /// <summary>
        /// Set active firing group
        /// </summary>
        public void SetActiveFiringGroup(int groupIndex)
        {
            if (groupIndex >= 0 && groupIndex < maxFiringGroups)
            {
                activeFiringGroup = groupIndex;
                OnFiringGroupChanged?.Invoke(groupIndex);
            }
        }

        /// <summary>
        /// Cycle to next firing group
        /// </summary>
        public void CycleFiringGroup()
        {
            activeFiringGroup = (activeFiringGroup + 1) % maxFiringGroups;
            OnFiringGroupChanged?.Invoke(activeFiringGroup);
        }

        /// <summary>
        /// Assign hardpoint to firing group
        /// </summary>
        public void AssignToFiringGroup(int hardpointIndex, int groupIndex)
        {
            if (hardpointIndex < 0 || hardpointIndex >= hardpoints.Count) return;
            if (groupIndex < 0 || groupIndex >= maxFiringGroups) return;

            WeaponHardpoint hardpoint = hardpoints[hardpointIndex];

            // Remove from all groups first
            foreach (var group in firingGroups.Values)
            {
                group.Remove(hardpoint);
            }

            // Add to new group
            firingGroups[groupIndex].Add(hardpoint);
            hardpoint.firingGroup = groupIndex;
        }

        /// <summary>
        /// Remove hardpoint from firing group
        /// </summary>
        public void RemoveFromFiringGroup(int hardpointIndex, int groupIndex)
        {
            if (hardpointIndex < 0 || hardpointIndex >= hardpoints.Count) return;
            if (groupIndex < 0 || groupIndex >= maxFiringGroups) return;

            WeaponHardpoint hardpoint = hardpoints[hardpointIndex];
            firingGroups[groupIndex].Remove(hardpoint);
            hardpoint.firingGroup = -1;
        }

        /// <summary>
        /// Get all hardpoints in a firing group
        /// </summary>
        public List<WeaponHardpoint> GetFiringGroup(int groupIndex)
        {
            if (firingGroups.ContainsKey(groupIndex))
            {
                return new List<WeaponHardpoint>(firingGroups[groupIndex]);
            }
            return new List<WeaponHardpoint>();
        }

        private void AssignDefaultFiringGroups()
        {
            int currentGroup = 0;
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.firingGroup >= 0 && hardpoint.firingGroup < maxFiringGroups)
                {
                    firingGroups[hardpoint.firingGroup].Add(hardpoint);
                }
                else
                {
                    firingGroups[currentGroup].Add(hardpoint);
                    hardpoint.firingGroup = currentGroup;
                    currentGroup = (currentGroup + 1) % maxFiringGroups;
                }
            }
        }

        #endregion

        #region Ammunition

        /// <summary>
        /// Reload all weapons
        /// </summary>
        public void ReloadAll()
        {
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null)
                {
                    hardpoint.turret.FullReload();
                }
            }
        }

        /// <summary>
        /// Get total ammunition percentage
        /// </summary>
        public float GetTotalAmmoPercent()
        {
            if (hardpoints.Count == 0) return 1f;

            float totalPercent = 0f;
            int count = 0;

            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null)
                {
                    totalPercent += hardpoint.turret.AmmoPercent;
                    count++;
                }
            }

            return count > 0 ? totalPercent / count : 1f;
        }

        #endregion

        #region Heat Management

        /// <summary>
        /// Get average heat level across all weapons
        /// </summary>
        public float GetAverageHeat()
        {
            if (hardpoints.Count == 0) return 0f;

            float totalHeat = 0f;
            int count = 0;

            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null)
                {
                    totalHeat += hardpoint.turret.HeatPercent;
                    count++;
                }
            }

            return count > 0 ? totalHeat / count : 0f;
        }

        /// <summary>
        /// Check if any weapon is overheated
        /// </summary>
        public bool IsAnyOverheated()
        {
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.turret != null && hardpoint.turret.IsOverheated)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw hardpoint positions
            foreach (var hardpoint in hardpoints)
            {
                if (hardpoint.mountPoint != null)
                {
                    Gizmos.color = hardpoint.isActive ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(hardpoint.mountPoint.position, 1f);
                    Gizmos.DrawRay(hardpoint.mountPoint.position, hardpoint.mountPoint.forward * 5f);
                }
            }
        }
    }

    /// <summary>
    /// Represents a weapon hardpoint on the ship
    /// </summary>
    [System.Serializable]
    public class WeaponHardpoint
    {
        public string name = "Hardpoint";
        public HardpointSize size = HardpointSize.Small;
        public HardpointPosition position = HardpointPosition.Forward;
        public Transform mountPoint;
        public TurretSystem turret;
        public int firingGroup = 0;
        public bool isActive = true;
    }

    /// <summary>
    /// Size categories for weapon hardpoints
    /// </summary>
    public enum HardpointSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    /// <summary>
    /// Position categories for hardpoints
    /// </summary>
    public enum HardpointPosition
    {
        Forward,
        Rear,
        Port,
        Starboard,
        Dorsal,
        Ventral
    }
}
