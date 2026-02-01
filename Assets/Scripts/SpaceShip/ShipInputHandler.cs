using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Input handler for spaceship controls.
    /// Maps keyboard/mouse/gamepad input to ship systems.
    /// Can be customized or replaced for different input systems.
    /// </summary>
    [RequireComponent(typeof(SpaceShipController))]
    public class ShipInputHandler : MonoBehaviour
    {
        [Header("Flight Controls")]
        [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode flightAssistToggle = KeyCode.Z;
        [SerializeField] private KeyCode fullStopKey = KeyCode.X;

        [Header("Throttle")]
        [SerializeField] private KeyCode throttleUpKey = KeyCode.W;
        [SerializeField] private KeyCode throttleDownKey = KeyCode.S;
        [SerializeField] private KeyCode throttle100Key = KeyCode.None;
        [SerializeField] private KeyCode throttle75Key = KeyCode.None;
        [SerializeField] private KeyCode throttle50Key = KeyCode.None;
        [SerializeField] private KeyCode throttle25Key = KeyCode.None;
        [SerializeField] private KeyCode throttle0Key = KeyCode.None;

        [Header("Strafe/Vertical")]
        [SerializeField] private KeyCode strafeLeftKey = KeyCode.A;
        [SerializeField] private KeyCode strafeRightKey = KeyCode.D;
        [SerializeField] private KeyCode thrustUpKey = KeyCode.Space;
        [SerializeField] private KeyCode thrustDownKey = KeyCode.LeftControl;

        [Header("Rotation")]
        [SerializeField] private KeyCode rollLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rollRightKey = KeyCode.E;
        [SerializeField] private bool useMouseForRotation = true;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertPitch = false;

        [Header("Weapons")]
        [SerializeField] private KeyCode primaryFireKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode secondaryFireKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode cycleFiringGroupKey = KeyCode.N;
        [SerializeField] private KeyCode deployHardpointsKey = KeyCode.U;

        [Header("Targeting")]
        [SerializeField] private KeyCode targetAheadKey = KeyCode.T;
        [SerializeField] private KeyCode nextTargetKey = KeyCode.G;
        [SerializeField] private KeyCode prevTargetKey = KeyCode.None;
        [SerializeField] private KeyCode nearestHostileKey = KeyCode.H;
        [SerializeField] private KeyCode highestThreatKey = KeyCode.None;
        [SerializeField] private KeyCode cycleSubsystemKey = KeyCode.Y;

        [Header("Warp/FTL")]
        [SerializeField] private KeyCode supercruiseKey = KeyCode.J;
        [SerializeField] private KeyCode hyperspaceKey = KeyCode.K;
        [SerializeField] private KeyCode quantumKey = KeyCode.None;
        [SerializeField] private KeyCode cancelWarpKey = KeyCode.Backspace;

        [Header("Defensive")]
        [SerializeField] private KeyCode chaffKey = KeyCode.C;
        [SerializeField] private KeyCode flareKey = KeyCode.V;
        [SerializeField] private KeyCode evasiveKey = KeyCode.None;
        [SerializeField] private KeyCode shieldBoostKey = KeyCode.None;

        [Header("Power Management")]
        [SerializeField] private KeyCode powerToWeaponsKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode powerToShieldsKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode powerToEnginesKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode resetPowerKey = KeyCode.Alpha4;

        [Header("Systems")]
        [SerializeField] private KeyCode toggleEnginesKey = KeyCode.None;
        [SerializeField] private KeyCode silentRunningKey = KeyCode.None;
        [SerializeField] private KeyCode cargoScoopKey = KeyCode.None;
        [SerializeField] private KeyCode landingGearKey = KeyCode.L;

        [Header("Input Mode")]
        [SerializeField] private bool directThrottle = false;
        [SerializeField] private bool relativeMouseMode = true;

        // References
        private SpaceShipController shipController;
        private FlightAssistSystem flightAssist;
        private ShipWeaponSystem weaponSystem;
        private ShipTargetingSystem targetingSystem;
        private WarpDriveSystem warpDrive;
        private ShipCombatManager combatManager;
        private ShipSubsystems subsystems;

        // State
        private bool hardpointsDeployed = false;

        private void Awake()
        {
            shipController = GetComponent<SpaceShipController>();
            flightAssist = GetComponent<FlightAssistSystem>();
            weaponSystem = GetComponent<ShipWeaponSystem>();
            targetingSystem = GetComponent<ShipTargetingSystem>();
            warpDrive = GetComponent<WarpDriveSystem>();
            combatManager = GetComponent<ShipCombatManager>();
            subsystems = GetComponent<ShipSubsystems>();
        }

        private void Update()
        {
            HandleFlightInput();
            HandleThrottleInput();
            HandleRotationInput();
            HandleWeaponInput();
            HandleTargetingInput();
            HandleWarpInput();
            HandleDefensiveInput();
            HandlePowerInput();
            HandleSystemInput();
        }

        #region Flight Input

        private void HandleFlightInput()
        {
            // Thrust input
            float strafe = 0f;
            float vertical = 0f;
            float forward = 0f;

            if (Input.GetKey(strafeLeftKey)) strafe -= 1f;
            if (Input.GetKey(strafeRightKey)) strafe += 1f;
            if (Input.GetKey(thrustUpKey)) vertical += 1f;
            if (Input.GetKey(thrustDownKey)) vertical -= 1f;

            if (!directThrottle)
            {
                if (Input.GetKey(throttleUpKey)) forward += 1f;
                if (Input.GetKey(throttleDownKey)) forward -= 1f;
            }

            shipController.SetThrustInput(strafe, vertical, forward);

            // Boost
            if (Input.GetKeyDown(boostKey))
            {
                shipController.StartBoost();
            }
            else if (Input.GetKeyUp(boostKey))
            {
                shipController.EndBoost();
            }

            // Flight assist toggle
            if (Input.GetKeyDown(flightAssistToggle))
            {
                shipController.CycleFlightAssist();
            }

            // Full stop
            if (Input.GetKeyDown(fullStopKey))
            {
                shipController.FullStop();
            }
        }

        #endregion

        #region Throttle Input

        private void HandleThrottleInput()
        {
            if (directThrottle)
            {
                // Adjust throttle based on key held
                if (Input.GetKey(throttleUpKey))
                {
                    shipController.AdjustThrottle(1f);
                }
                else if (Input.GetKey(throttleDownKey))
                {
                    shipController.AdjustThrottle(-1f);
                }
            }

            // Throttle presets
            if (Input.GetKeyDown(throttle100Key))
                shipController.SetThrottlePreset(ThrottlePreset.Full);
            if (Input.GetKeyDown(throttle75Key))
                shipController.SetThrottlePreset(ThrottlePreset.ThreeQuarter);
            if (Input.GetKeyDown(throttle50Key))
                shipController.SetThrottlePreset(ThrottlePreset.Half);
            if (Input.GetKeyDown(throttle25Key))
                shipController.SetThrottlePreset(ThrottlePreset.Quarter);
            if (Input.GetKeyDown(throttle0Key))
                shipController.SetThrottlePreset(ThrottlePreset.Zero);
        }

        #endregion

        #region Rotation Input

        private void HandleRotationInput()
        {
            float pitch = 0f;
            float yaw = 0f;
            float roll = 0f;

            // Mouse rotation
            if (useMouseForRotation)
            {
                if (relativeMouseMode)
                {
                    yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
                    pitch = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertPitch ? 1f : -1f);
                }
                else
                {
                    // Absolute mouse position mode (center of screen = neutral)
                    Vector3 mousePos = Input.mousePosition;
                    Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                    Vector2 offset = new Vector2(mousePos.x - screenCenter.x, mousePos.y - screenCenter.y);
                    
                    yaw = Mathf.Clamp(offset.x / screenCenter.x, -1f, 1f);
                    pitch = Mathf.Clamp(-offset.y / screenCenter.y, -1f, 1f) * (invertPitch ? -1f : 1f);
                }
            }

            // Roll from keyboard
            if (Input.GetKey(rollLeftKey)) roll += 1f;
            if (Input.GetKey(rollRightKey)) roll -= 1f;

            shipController.SetRotationInput(pitch, yaw, roll);
        }

        #endregion

        #region Weapon Input

        private void HandleWeaponInput()
        {
            if (weaponSystem == null) return;

            // Deploy/retract hardpoints
            if (Input.GetKeyDown(deployHardpointsKey))
            {
                hardpointsDeployed = !hardpointsDeployed;
                // Could add animation/state change here
            }

            if (!hardpointsDeployed) return;

            // Primary fire
            if (Input.GetKeyDown(primaryFireKey))
            {
                weaponSystem.StartFiringGroup(0);
            }
            if (Input.GetKeyUp(primaryFireKey))
            {
                weaponSystem.StopFiringGroup(0);
            }

            // Secondary fire
            if (Input.GetKeyDown(secondaryFireKey))
            {
                weaponSystem.StartFiringGroup(1);
            }
            if (Input.GetKeyUp(secondaryFireKey))
            {
                weaponSystem.StopFiringGroup(1);
            }

            // Cycle firing groups
            if (Input.GetKeyDown(cycleFiringGroupKey))
            {
                weaponSystem.CycleFiringGroup();
            }
        }

        #endregion

        #region Targeting Input

        private void HandleTargetingInput()
        {
            if (targetingSystem == null) return;

            // Target in crosshairs
            if (Input.GetKeyDown(targetAheadKey))
            {
                targetingSystem.TargetInCrosshairs();
            }

            // Next target
            if (Input.GetKeyDown(nextTargetKey))
            {
                targetingSystem.NextTarget();
            }

            // Previous target
            if (Input.GetKeyDown(prevTargetKey))
            {
                targetingSystem.PreviousTarget();
            }

            // Nearest hostile
            if (Input.GetKeyDown(nearestHostileKey))
            {
                targetingSystem.TargetNearestHostile();
            }

            // Highest threat
            if (Input.GetKeyDown(highestThreatKey))
            {
                targetingSystem.TargetHighestThreat();
            }

            // Cycle subsystems
            if (Input.GetKeyDown(cycleSubsystemKey))
            {
                targetingSystem.CycleSubsystem();
            }
        }

        #endregion

        #region Warp Input

        private void HandleWarpInput()
        {
            if (warpDrive == null) return;

            // Supercruise
            if (Input.GetKeyDown(supercruiseKey))
            {
                if (warpDrive.CurrentState == WarpState.Supercruise)
                {
                    warpDrive.DisengageSupercruise();
                }
                else if (warpDrive.CurrentState == WarpState.Normal)
                {
                    warpDrive.StartSupercruiseCharge();
                }
            }

            // Hyperspace jump (would need destination selection UI)
            if (Input.GetKeyDown(hyperspaceKey))
            {
                // Placeholder - would need destination selection
                // warpDrive.StartHyperspaceCharge(destination);
            }

            // Quantum travel
            if (Input.GetKeyDown(quantumKey))
            {
                if (warpDrive.CurrentState == WarpState.QuantumTravel)
                {
                    warpDrive.DisengageQuantumTravel();
                }
                else
                {
                    warpDrive.StartQuantumCharge();
                }
            }

            // Cancel warp charge
            if (Input.GetKeyDown(cancelWarpKey))
            {
                warpDrive.DisengageWarp();
            }
        }

        #endregion

        #region Defensive Input

        private void HandleDefensiveInput()
        {
            if (combatManager == null) return;

            // Chaff
            if (Input.GetKeyDown(chaffKey))
            {
                combatManager.DeployChaff();
            }

            // Flare
            if (Input.GetKeyDown(flareKey))
            {
                combatManager.DeployFlare();
            }

            // Evasive maneuvers
            if (Input.GetKeyDown(evasiveKey))
            {
                combatManager.StartEvasive();
            }
        }

        #endregion

        #region Power Input

        private void HandlePowerInput()
        {
            if (subsystems == null) return;

            // Power to weapons
            if (Input.GetKeyDown(powerToWeaponsKey))
            {
                subsystems.IncreasePower(PowerCategory.Weapons);
            }

            // Power to shields
            if (Input.GetKeyDown(powerToShieldsKey))
            {
                subsystems.IncreasePower(PowerCategory.Shields);
            }

            // Power to engines
            if (Input.GetKeyDown(powerToEnginesKey))
            {
                subsystems.IncreasePower(PowerCategory.Engines);
            }

            // Reset power
            if (Input.GetKeyDown(resetPowerKey))
            {
                subsystems.ResetPowerDistribution();
            }
        }

        #endregion

        #region System Input

        private void HandleSystemInput()
        {
            // Toggle engines
            if (Input.GetKeyDown(toggleEnginesKey))
            {
                shipController.ToggleEngines();
            }

            // Landing gear
            if (Input.GetKeyDown(landingGearKey))
            {
                // Would integrate with landing system
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Max(0.1f, sensitivity);
        }

        /// <summary>
        /// Toggle pitch inversion
        /// </summary>
        public void ToggleInvertPitch()
        {
            invertPitch = !invertPitch;
        }

        /// <summary>
        /// Set invert pitch
        /// </summary>
        public void SetInvertPitch(bool invert)
        {
            invertPitch = invert;
        }

        /// <summary>
        /// Toggle between relative and absolute mouse mode
        /// </summary>
        public void ToggleMouseMode()
        {
            relativeMouseMode = !relativeMouseMode;
        }

        /// <summary>
        /// Toggle between direct throttle and direct forward thrust
        /// </summary>
        public void ToggleThrottleMode()
        {
            directThrottle = !directThrottle;
        }

        /// <summary>
        /// Enable or disable mouse rotation
        /// </summary>
        public void SetMouseRotation(bool enabled)
        {
            useMouseForRotation = enabled;
        }

        #endregion
    }
}
