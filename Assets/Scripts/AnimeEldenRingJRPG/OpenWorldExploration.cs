using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the type of region in the open world.
    /// </summary>
    public enum RegionType
    {
        Plains,         // Open grasslands, easy enemies
        Forest,         // Dense woods, moderate encounters
        Mountain,       // High altitude, wind hazards
        Swamp,          // Poison hazards, hidden paths
        Desert,         // Heat hazard, mirages
        Tundra,         // Cold hazard, ice mechanics
        Volcano,        // Fire hazard, lava traversal
        Ruins,          // Ancient structures, puzzles
        Abyss,          // Dark, corrupted, endgame
        SacredGrounds,  // Holy area, divine creatures
        FloatingIsles,  // Aerial exploration, wind currents
        Underworld      // Underground labyrinth
    }

    /// <summary>
    /// Defines the weather conditions in a region.
    /// </summary>
    public enum WeatherCondition
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow,
        Fog,
        Sandstorm,
        AshFall,
        SpiritBloom,    // Magical event, rare creatures appear
        Eclipse,        // Boss-tier enemies roam
        AuroraBorealis  // Buff to spirit creatures
    }

    /// <summary>
    /// Defines the time of day cycle.
    /// </summary>
    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Evening,
        Night,
        Midnight
    }

    /// <summary>
    /// Defines the discovery state of a map area.
    /// </summary>
    public enum DiscoveryState
    {
        Hidden,         // Not yet found
        Revealed,       // Shown on map but not visited
        Explored,       // Visited
        Completed       // All objectives cleared
    }

    /// <summary>
    /// Defines the type of a point of interest.
    /// </summary>
    public enum PointOfInterestType
    {
        SiteOfGrace,        // Rest point, fast travel, level up
        Dungeon,            // Instance with enemies and boss
        Village,            // NPCs, shops, quests
        BossArena,          // Major boss encounter
        TreasureCache,      // Loot location
        CraftingStation,    // Crafting spot
        CreatureNest,       // Tameable creatures spawn
        SpiritSpring,       // Launch point for traversal
        SealedDoor,         // Requires key/quest to open
        AncientAltar,       // Upgrade or ritual location
        Merchant,           // Wandering merchant
        LoreStone           // World lore fragment
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a point of interest on the map.
    /// </summary>
    [Serializable]
    public class PointOfInterest
    {
        public string poiId;
        public string poiName;
        [TextArea(2, 4)]
        public string description;
        public PointOfInterestType poiType;
        public Vector3 worldPosition;
        public Sprite mapIcon;
        public DiscoveryState discoveryState = DiscoveryState.Hidden;
        public bool isInteractable = true;

        [Header("Requirements")]
        public int recommendedLevel;
        public List<string> requiredItemIds = new List<string>();
        public string requiredQuestId;

        [Header("Rewards")]
        public int runeReward;
        public List<string> itemRewards = new List<string>();
        public string unlockedCreatureId;
    }

    /// <summary>
    /// Represents a Site of Grace (rest and fast-travel point).
    /// </summary>
    [Serializable]
    public class SiteOfGrace
    {
        public string siteId;
        public string siteName;
        [TextArea(2, 3)]
        public string description;
        public Vector3 worldPosition;
        public string regionId;
        public bool isDiscovered;
        public bool isRested;
        public Sprite icon;

        [Header("Services")]
        public bool allowsLeveling = true;
        public bool allowsCrafting;
        public bool allowsCreatureManagement = true;
        public bool allowsFastTravel = true;

        [Header("Respawn")]
        public bool respawnsEnemies = true;
        public float enemyRespawnRadius = 100f;
    }

    /// <summary>
    /// Represents a region of the open world.
    /// </summary>
    [Serializable]
    public class WorldRegion
    {
        public string regionId;
        public string regionName;
        [TextArea(3, 5)]
        public string description;
        public RegionType regionType;
        public Sprite regionIcon;
        public Color mapColor = Color.green;

        [Header("Level Range")]
        public int minEnemyLevel = 1;
        public int maxEnemyLevel = 10;
        public int recommendedPlayerLevel = 1;

        [Header("Content")]
        public List<PointOfInterest> pointsOfInterest = new List<PointOfInterest>();
        public List<SiteOfGrace> sitesOfGrace = new List<SiteOfGrace>();
        public List<string> availableCreatureIds = new List<string>();
        public List<string> regionBossIds = new List<string>();

        [Header("Environment")]
        public List<WeatherCondition> possibleWeather = new List<WeatherCondition>();
        public float weatherChangeInterval = 300f;
        public bool hasEnvironmentalHazard;
        public float hazardDamagePerSecond;

        [Header("Discovery")]
        public DiscoveryState discoveryState = DiscoveryState.Hidden;
        public float explorationPercent;

        [Header("Connected Regions")]
        public List<string> connectedRegionIds = new List<string>();
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Configuration for the open world exploration system.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "UsefulScripts/AnimeEldenRingJRPG/World Config")]
    public class WorldConfig : ScriptableObject
    {
        [Header("World")]
        public string worldName = "Shattered Realm";
        [TextArea(3, 5)]
        public string worldDescription;

        [Header("Regions")]
        public List<WorldRegion> regions = new List<WorldRegion>();

        [Header("Day/Night Cycle")]
        public float dayLengthInMinutes = 24f;
        public float startingHour = 8f;

        [Header("Weather")]
        public float baseWeatherChangeInterval = 300f;
        public float specialEventChance = 0.05f;

        [Header("Fast Travel")]
        public float fastTravelLoadTime = 2f;
        public bool fastTravelRequiresGrace = true;
        public int fastTravelRuneCost = 0;

        [Header("Exploration")]
        public int poiDiscoveryExpReward = 50;
        public int regionCompleteExpReward = 500;
        public int siteOfGraceDiscoveryRunes = 200;
    }

    #endregion

    /// <summary>
    /// Manages the open world exploration system with regions, sites of grace,
    /// map discovery, weather, day/night cycles, and fast travel.
    /// Inspired by Elden Ring's open-world design with anime JRPG aesthetics.
    /// </summary>
    public class OpenWorldExploration : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WorldConfig config;

        [Header("Current State")]
        [SerializeField] private string currentRegionId;
        [SerializeField] private string lastRestedSiteId;
        [SerializeField] private WeatherCondition currentWeather = WeatherCondition.Clear;
        [SerializeField] private TimeOfDay currentTimeOfDay = TimeOfDay.Morning;
        [SerializeField] private float currentWorldTime;
        [SerializeField] private float weatherTimer;

        // Events
        public event Action<WorldRegion> OnRegionEntered;
        public event Action<WorldRegion> OnRegionDiscovered;
        public event Action<WorldRegion> OnRegionCompleted;
        public event Action<SiteOfGrace> OnSiteOfGraceDiscovered;
        public event Action<SiteOfGrace> OnSiteOfGraceRested;
        public event Action<PointOfInterest> OnPointOfInterestDiscovered;
        public event Action<PointOfInterest> OnPointOfInterestCompleted;
        public event Action<WeatherCondition> OnWeatherChanged;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<SiteOfGrace, SiteOfGrace> OnFastTravel;

        // Properties
        public string CurrentRegionId => currentRegionId;
        public WeatherCondition CurrentWeather => currentWeather;
        public TimeOfDay CurrentTime => currentTimeOfDay;
        public float WorldTime => currentWorldTime;

        private void Update()
        {
            UpdateWorldTime();
            UpdateWeather();
        }

        #region Time System

        private void UpdateWorldTime()
        {
            if (config == null) return;

            float dayLength = config.dayLengthInMinutes * 60f;
            currentWorldTime += Time.deltaTime;

            if (currentWorldTime >= dayLength)
            {
                currentWorldTime -= dayLength;
            }

            float hourFraction = currentWorldTime / dayLength;
            float currentHour = (config.startingHour + (hourFraction * 24f)) % 24f;

            TimeOfDay newTime = GetTimeOfDay(currentHour);
            if (newTime != currentTimeOfDay)
            {
                currentTimeOfDay = newTime;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            }
        }

        private TimeOfDay GetTimeOfDay(float hour)
        {
            if (hour < 5f) return TimeOfDay.Midnight;
            if (hour < 7f) return TimeOfDay.Dawn;
            if (hour < 10f) return TimeOfDay.Morning;
            if (hour < 13f) return TimeOfDay.Noon;
            if (hour < 16f) return TimeOfDay.Afternoon;
            if (hour < 18f) return TimeOfDay.Dusk;
            if (hour < 21f) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }

        /// <summary>
        /// Gets the current in-game hour (0-24).
        /// </summary>
        public float GetCurrentHour()
        {
            if (config == null) return 12f;
            float dayLength = config.dayLengthInMinutes * 60f;
            float hourFraction = currentWorldTime / dayLength;
            return (config.startingHour + (hourFraction * 24f)) % 24f;
        }

        /// <summary>
        /// Advances the time to the next dawn.
        /// </summary>
        public void WaitUntilDawn()
        {
            if (config == null) return;
            float dayLength = config.dayLengthInMinutes * 60f;
            float dawnHour = 6f;
            float currentHour = GetCurrentHour();
            float hoursToAdvance = currentHour < dawnHour
                ? dawnHour - currentHour
                : 24f - currentHour + dawnHour;
            currentWorldTime += (hoursToAdvance / 24f) * dayLength;
            if (currentWorldTime >= dayLength) currentWorldTime -= dayLength;
            currentTimeOfDay = TimeOfDay.Dawn;
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        #endregion

        #region Weather

        private void UpdateWeather()
        {
            if (config == null) return;

            weatherTimer -= Time.deltaTime;
            if (weatherTimer <= 0)
            {
                ChangeWeather();
                weatherTimer = config.baseWeatherChangeInterval;
            }
        }

        private void ChangeWeather()
        {
            var region = GetCurrentRegion();
            if (region == null || region.possibleWeather.Count == 0) return;

            // Check for special events
            float specialChance = config != null ? config.specialEventChance : 0.05f;
            if (UnityEngine.Random.Range(0f, 1f) < specialChance)
            {
                var specialWeather = new[] { WeatherCondition.SpiritBloom, WeatherCondition.Eclipse, WeatherCondition.AuroraBorealis };
                var available = specialWeather.Where(w => region.possibleWeather.Contains(w)).ToList();
                if (available.Count > 0)
                {
                    currentWeather = available[UnityEngine.Random.Range(0, available.Count)];
                    OnWeatherChanged?.Invoke(currentWeather);
                    return;
                }
            }

            var normalWeather = region.possibleWeather
                .Where(w => w != WeatherCondition.SpiritBloom && w != WeatherCondition.Eclipse && w != WeatherCondition.AuroraBorealis)
                .ToList();

            if (normalWeather.Count > 0)
            {
                currentWeather = normalWeather[UnityEngine.Random.Range(0, normalWeather.Count)];
                OnWeatherChanged?.Invoke(currentWeather);
            }
        }

        #endregion

        #region Region Management

        /// <summary>
        /// Enters a new region. Triggers discovery if first visit.
        /// </summary>
        public void EnterRegion(string regionId)
        {
            if (config == null) return;

            var region = config.regions.FirstOrDefault(r => r.regionId == regionId);
            if (region == null) return;

            currentRegionId = regionId;

            if (region.discoveryState == DiscoveryState.Hidden)
            {
                region.discoveryState = DiscoveryState.Explored;
                OnRegionDiscovered?.Invoke(region);
            }

            OnRegionEntered?.Invoke(region);
        }

        /// <summary>
        /// Gets the current region data.
        /// </summary>
        public WorldRegion GetCurrentRegion()
        {
            if (config == null) return null;
            return config.regions.FirstOrDefault(r => r.regionId == currentRegionId);
        }

        /// <summary>
        /// Gets all discovered regions.
        /// </summary>
        public List<WorldRegion> GetDiscoveredRegions()
        {
            if (config == null) return new List<WorldRegion>();
            return config.regions.Where(r => r.discoveryState != DiscoveryState.Hidden).ToList();
        }

        /// <summary>
        /// Gets all regions connected to the current region.
        /// </summary>
        public List<WorldRegion> GetConnectedRegions()
        {
            var current = GetCurrentRegion();
            if (current == null || config == null) return new List<WorldRegion>();

            return config.regions
                .Where(r => current.connectedRegionIds.Contains(r.regionId))
                .ToList();
        }

        /// <summary>
        /// Marks a region as completed.
        /// </summary>
        public void CompleteRegion(string regionId)
        {
            if (config == null) return;
            var region = config.regions.FirstOrDefault(r => r.regionId == regionId);
            if (region == null) return;

            region.discoveryState = DiscoveryState.Completed;
            region.explorationPercent = 100f;
            OnRegionCompleted?.Invoke(region);
        }

        #endregion

        #region Sites of Grace

        /// <summary>
        /// Discovers a site of grace.
        /// </summary>
        public void DiscoverSiteOfGrace(string siteId)
        {
            var site = FindSiteOfGrace(siteId);
            if (site == null || site.isDiscovered) return;

            site.isDiscovered = true;
            OnSiteOfGraceDiscovered?.Invoke(site);
        }

        /// <summary>
        /// Rests at a site of grace. Heals party and respawns enemies.
        /// </summary>
        public void RestAtSiteOfGrace(string siteId)
        {
            var site = FindSiteOfGrace(siteId);
            if (site == null || !site.isDiscovered) return;

            site.isRested = true;
            lastRestedSiteId = siteId;
            OnSiteOfGraceRested?.Invoke(site);
        }

        /// <summary>
        /// Fast travels to a discovered site of grace.
        /// </summary>
        public bool FastTravel(string targetSiteId)
        {
            if (config != null && config.fastTravelRequiresGrace)
            {
                if (string.IsNullOrEmpty(lastRestedSiteId)) return false;
            }

            var targetSite = FindSiteOfGrace(targetSiteId);
            if (targetSite == null || !targetSite.isDiscovered) return false;

            var originSite = FindSiteOfGrace(lastRestedSiteId);

            // Update region
            if (!string.IsNullOrEmpty(targetSite.regionId))
            {
                EnterRegion(targetSite.regionId);
            }

            lastRestedSiteId = targetSiteId;
            OnFastTravel?.Invoke(originSite, targetSite);
            return true;
        }

        /// <summary>
        /// Gets all discovered sites of grace.
        /// </summary>
        public List<SiteOfGrace> GetDiscoveredSites()
        {
            if (config == null) return new List<SiteOfGrace>();
            return config.regions
                .SelectMany(r => r.sitesOfGrace)
                .Where(s => s.isDiscovered)
                .ToList();
        }

        /// <summary>
        /// Gets the last rested site of grace (respawn point).
        /// </summary>
        public SiteOfGrace GetLastRestedSite()
        {
            return FindSiteOfGrace(lastRestedSiteId);
        }

        private SiteOfGrace FindSiteOfGrace(string siteId)
        {
            if (string.IsNullOrEmpty(siteId) || config == null) return null;
            return config.regions
                .SelectMany(r => r.sitesOfGrace)
                .FirstOrDefault(s => s.siteId == siteId);
        }

        #endregion

        #region Points of Interest

        /// <summary>
        /// Discovers a point of interest on the map.
        /// </summary>
        public void DiscoverPointOfInterest(string poiId)
        {
            var poi = FindPointOfInterest(poiId);
            if (poi == null || poi.discoveryState != DiscoveryState.Hidden) return;

            poi.discoveryState = DiscoveryState.Revealed;
            OnPointOfInterestDiscovered?.Invoke(poi);
        }

        /// <summary>
        /// Marks a point of interest as completed.
        /// </summary>
        public void CompletePointOfInterest(string poiId)
        {
            var poi = FindPointOfInterest(poiId);
            if (poi == null) return;

            poi.discoveryState = DiscoveryState.Completed;
            OnPointOfInterestCompleted?.Invoke(poi);

            UpdateRegionExploration(poi);
        }

        /// <summary>
        /// Gets all points of interest of a specific type in the current region.
        /// </summary>
        public List<PointOfInterest> GetPointsOfInterest(PointOfInterestType type)
        {
            var region = GetCurrentRegion();
            if (region == null) return new List<PointOfInterest>();

            return region.pointsOfInterest.Where(p => p.poiType == type).ToList();
        }

        private PointOfInterest FindPointOfInterest(string poiId)
        {
            if (config == null) return null;
            return config.regions
                .SelectMany(r => r.pointsOfInterest)
                .FirstOrDefault(p => p.poiId == poiId);
        }

        private void UpdateRegionExploration(PointOfInterest poi)
        {
            if (config == null) return;

            foreach (var region in config.regions)
            {
                if (region.pointsOfInterest.Contains(poi))
                {
                    int total = region.pointsOfInterest.Count;
                    int completed = region.pointsOfInterest.Count(p => p.discoveryState == DiscoveryState.Completed);
                    region.explorationPercent = total > 0 ? (completed * 100f / total) : 0f;

                    if (region.explorationPercent >= 100f && region.discoveryState != DiscoveryState.Completed)
                    {
                        CompleteRegion(region.regionId);
                    }
                    break;
                }
            }
        }

        #endregion

        #region Save Data

        [Serializable]
        public class ExplorationSaveData
        {
            public string currentRegionId;
            public string lastRestedSiteId;
            public float currentWorldTime;
            public int currentWeather;
            public List<RegionSaveData> regions = new List<RegionSaveData>();
        }

        [Serializable]
        public class RegionSaveData
        {
            public string regionId;
            public int discoveryState;
            public float explorationPercent;
            public List<string> discoveredSiteIds = new List<string>();
            public List<string> completedPoiIds = new List<string>();
        }

        public ExplorationSaveData GetSaveData()
        {
            var data = new ExplorationSaveData
            {
                currentRegionId = currentRegionId,
                lastRestedSiteId = lastRestedSiteId,
                currentWorldTime = currentWorldTime,
                currentWeather = (int)currentWeather
            };

            if (config != null)
            {
                foreach (var region in config.regions)
                {
                    var regionSave = new RegionSaveData
                    {
                        regionId = region.regionId,
                        discoveryState = (int)region.discoveryState,
                        explorationPercent = region.explorationPercent
                    };

                    foreach (var site in region.sitesOfGrace)
                    {
                        if (site.isDiscovered) regionSave.discoveredSiteIds.Add(site.siteId);
                    }

                    foreach (var poi in region.pointsOfInterest)
                    {
                        if (poi.discoveryState == DiscoveryState.Completed) regionSave.completedPoiIds.Add(poi.poiId);
                    }

                    data.regions.Add(regionSave);
                }
            }

            return data;
        }

        #endregion
    }
}
