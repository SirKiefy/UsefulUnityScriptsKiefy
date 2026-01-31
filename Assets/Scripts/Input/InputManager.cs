using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Input
{
    /// <summary>
    /// Represents a bindable input action.
    /// </summary>
    [Serializable]
    public class InputAction
    {
        public string actionId;
        public string displayName;
        public InputActionType actionType;
        public KeyCode primaryKey = KeyCode.None;
        public KeyCode secondaryKey = KeyCode.None;
        public MouseButton mouseButton = MouseButton.None;
        public int gamepadButton = -1;
        public string gamepadAxis;
        public float axisDeadzone = 0.1f;
        public bool isRebindable = true;
        public InputCategory category = InputCategory.Gameplay;
        
        public InputAction(string id, string name, KeyCode primary)
        {
            actionId = id;
            displayName = name;
            primaryKey = primary;
            actionType = InputActionType.Button;
        }
        
        public InputAction(string id, string name, string axis)
        {
            actionId = id;
            displayName = name;
            gamepadAxis = axis;
            actionType = InputActionType.Axis;
        }
    }
    
    public enum InputActionType
    {
        Button,
        Axis,
        Composite
    }
    
    public enum MouseButton
    {
        None = -1,
        Left = 0,
        Right = 1,
        Middle = 2
    }
    
    public enum InputCategory
    {
        Gameplay,
        Movement,
        Combat,
        UI,
        Camera,
        Vehicle,
        Social
    }
    
    /// <summary>
    /// Represents a composite axis made from two buttons (e.g., WASD).
    /// </summary>
    [Serializable]
    public class CompositeAxis
    {
        public string axisId;
        public string displayName;
        public string positiveActionId;
        public string negativeActionId;
    }
    
    /// <summary>
    /// Input preset for different control schemes.
    /// </summary>
    [CreateAssetMenu(fileName = "InputPreset", menuName = "UsefulScripts/Input/Input Preset")]
    public class InputPreset : ScriptableObject
    {
        public string presetName;
        public List<InputAction> actions = new List<InputAction>();
        public List<CompositeAxis> compositeAxes = new List<CompositeAxis>();
    }
    
    /// <summary>
    /// Input context that can be enabled/disabled for different game states.
    /// </summary>
    [Serializable]
    public class InputContext
    {
        public string contextId;
        public string displayName;
        public List<string> allowedActionIds = new List<string>();
        public int priority;
        public bool blockLowerPriority = true;
        
        public InputContext(string id, string name, int prio = 0)
        {
            contextId = id;
            displayName = name;
            priority = prio;
        }
    }
    
    /// <summary>
    /// Complete rebindable input system with gamepad support and input contexts.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        
        [Header("Input Settings")]
        [SerializeField] private InputPreset defaultPreset;
        [SerializeField] private float holdThreshold = 0.5f;
        [SerializeField] private float doubleTapThreshold = 0.3f;
        
        [Header("Gamepad Settings")]
        [SerializeField] private bool enableGamepad = true;
        [SerializeField] private float gamepadAxisDeadzone = 0.15f;
        [SerializeField] private float gamepadAxisSensitivity = 1f;
        
        [Header("Mouse Settings")]
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private bool invertMouseY = false;
        
        private Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>();
        private Dictionary<string, CompositeAxis> compositeAxes = new Dictionary<string, CompositeAxis>();
        private Dictionary<string, float> actionHoldTimers = new Dictionary<string, float>();
        private Dictionary<string, float> lastTapTimes = new Dictionary<string, float>();
        private List<InputContext> activeContexts = new List<InputContext>();
        private InputDevice currentDevice = InputDevice.KeyboardMouse;
        private bool isRebinding = false;
        private string rebindingActionId;
        private bool rebindingPrimary = true;
        
        // Events
        public event Action<string> OnActionPressed;
        public event Action<string> OnActionReleased;
        public event Action<string> OnActionHeld;
        public event Action<string> OnActionDoubleTapped;
        public event Action<string, KeyCode> OnRebindComplete;
        public event Action OnRebindCancelled;
        public event Action<InputDevice> OnInputDeviceChanged;
        
        // Properties
        public InputDevice CurrentDevice => currentDevice;
        public bool IsRebinding => isRebinding;
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = value; }
        public bool InvertMouseY { get => invertMouseY; set => invertMouseY = value; }
        public float GamepadSensitivity { get => gamepadAxisSensitivity; set => gamepadAxisSensitivity = value; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (defaultPreset != null)
            {
                LoadPreset(defaultPreset);
            }
        }
        
        private void Update()
        {
            if (isRebinding)
            {
                CheckForRebind();
                return;
            }
            
            DetectInputDevice();
            UpdateActions();
        }
        
        /// <summary>
        /// Loads an input preset.
        /// </summary>
        public void LoadPreset(InputPreset preset)
        {
            actions.Clear();
            compositeAxes.Clear();
            
            foreach (var action in preset.actions)
            {
                actions[action.actionId] = action;
                actionHoldTimers[action.actionId] = 0f;
                lastTapTimes[action.actionId] = -100f;
            }
            
            foreach (var axis in preset.compositeAxes)
            {
                compositeAxes[axis.axisId] = axis;
            }
        }
        
        /// <summary>
        /// Registers a new input action.
        /// </summary>
        public void RegisterAction(InputAction action)
        {
            actions[action.actionId] = action;
            actionHoldTimers[action.actionId] = 0f;
            lastTapTimes[action.actionId] = -100f;
        }
        
        /// <summary>
        /// Unregisters an input action.
        /// </summary>
        public void UnregisterAction(string actionId)
        {
            actions.Remove(actionId);
            actionHoldTimers.Remove(actionId);
            lastTapTimes.Remove(actionId);
        }
        
        private void UpdateActions()
        {
            foreach (var kvp in actions)
            {
                var action = kvp.Value;
                if (!IsActionAllowed(action.actionId)) continue;
                
                bool wasPressed = actionHoldTimers[action.actionId] > 0;
                bool isPressed = IsActionPressedRaw(action);
                
                if (isPressed && !wasPressed)
                {
                    // Just pressed
                    float timeSinceLastTap = Time.time - lastTapTimes[action.actionId];
                    if (timeSinceLastTap <= doubleTapThreshold)
                    {
                        OnActionDoubleTapped?.Invoke(action.actionId);
                    }
                    lastTapTimes[action.actionId] = Time.time;
                    
                    OnActionPressed?.Invoke(action.actionId);
                    actionHoldTimers[action.actionId] = Time.deltaTime;
                }
                else if (isPressed && wasPressed)
                {
                    // Holding
                    actionHoldTimers[action.actionId] += Time.deltaTime;
                    
                    if (actionHoldTimers[action.actionId] >= holdThreshold)
                    {
                        OnActionHeld?.Invoke(action.actionId);
                    }
                }
                else if (!isPressed && wasPressed)
                {
                    // Just released
                    OnActionReleased?.Invoke(action.actionId);
                    actionHoldTimers[action.actionId] = 0f;
                }
            }
        }
        
        private bool IsActionPressedRaw(InputAction action)
        {
            // Check keyboard
            if (action.primaryKey != KeyCode.None && UnityEngine.Input.GetKey(action.primaryKey))
                return true;
            if (action.secondaryKey != KeyCode.None && UnityEngine.Input.GetKey(action.secondaryKey))
                return true;
            
            // Check mouse
            if (action.mouseButton != MouseButton.None && UnityEngine.Input.GetMouseButton((int)action.mouseButton))
                return true;
            
            // Check gamepad
            if (enableGamepad && action.gamepadButton >= 0)
            {
                try
                {
                    if (UnityEngine.Input.GetKey((KeyCode)(350 + action.gamepadButton)))
                        return true;
                }
                catch { }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if an action is currently pressed.
        /// </summary>
        public bool IsPressed(string actionId)
        {
            if (!IsActionAllowed(actionId)) return false;
            return actionHoldTimers.TryGetValue(actionId, out float timer) && timer > 0;
        }
        
        /// <summary>
        /// Checks if an action was just pressed this frame.
        /// </summary>
        public bool WasJustPressed(string actionId)
        {
            if (!IsActionAllowed(actionId)) return false;
            if (!actions.TryGetValue(actionId, out var action)) return false;
            
            if (action.primaryKey != KeyCode.None && UnityEngine.Input.GetKeyDown(action.primaryKey))
                return true;
            if (action.secondaryKey != KeyCode.None && UnityEngine.Input.GetKeyDown(action.secondaryKey))
                return true;
            if (action.mouseButton != MouseButton.None && UnityEngine.Input.GetMouseButtonDown((int)action.mouseButton))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// Checks if an action was just released this frame.
        /// </summary>
        public bool WasJustReleased(string actionId)
        {
            if (!IsActionAllowed(actionId)) return false;
            if (!actions.TryGetValue(actionId, out var action)) return false;
            
            if (action.primaryKey != KeyCode.None && UnityEngine.Input.GetKeyUp(action.primaryKey))
                return true;
            if (action.secondaryKey != KeyCode.None && UnityEngine.Input.GetKeyUp(action.secondaryKey))
                return true;
            if (action.mouseButton != MouseButton.None && UnityEngine.Input.GetMouseButtonUp((int)action.mouseButton))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// Checks if an action is being held (past hold threshold).
        /// </summary>
        public bool IsHeld(string actionId)
        {
            if (!IsActionAllowed(actionId)) return false;
            return actionHoldTimers.TryGetValue(actionId, out float timer) && timer >= holdThreshold;
        }
        
        /// <summary>
        /// Gets the hold duration for an action.
        /// </summary>
        public float GetHoldDuration(string actionId)
        {
            return actionHoldTimers.TryGetValue(actionId, out float timer) ? timer : 0f;
        }
        
        /// <summary>
        /// Gets an axis value from a named axis or composite axis.
        /// </summary>
        public float GetAxis(string axisId)
        {
            // Check for Unity built-in axis
            try
            {
                float value = UnityEngine.Input.GetAxis(axisId) * gamepadAxisSensitivity;
                if (Mathf.Abs(value) > gamepadAxisDeadzone)
                {
                    return value;
                }
            }
            catch { }
            
            // Check for composite axis
            if (compositeAxes.TryGetValue(axisId, out var composite))
            {
                float positive = IsPressed(composite.positiveActionId) ? 1f : 0f;
                float negative = IsPressed(composite.negativeActionId) ? 1f : 0f;
                return positive - negative;
            }
            
            // Check if it's a registered action with an axis
            if (actions.TryGetValue(axisId, out var action) && !string.IsNullOrEmpty(action.gamepadAxis))
            {
                try
                {
                    float value = UnityEngine.Input.GetAxis(action.gamepadAxis) * gamepadAxisSensitivity;
                    if (Mathf.Abs(value) > action.axisDeadzone)
                    {
                        return value;
                    }
                }
                catch { }
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Gets the raw axis value without deadzone.
        /// </summary>
        public float GetAxisRaw(string axisId)
        {
            try
            {
                return UnityEngine.Input.GetAxisRaw(axisId);
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// Gets the mouse delta.
        /// </summary>
        public Vector2 GetMouseDelta()
        {
            float x = UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
            float y = UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity * (invertMouseY ? -1f : 1f);
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Gets the mouse position.
        /// </summary>
        public Vector3 GetMousePosition()
        {
            return UnityEngine.Input.mousePosition;
        }
        
        /// <summary>
        /// Gets movement input as a Vector2.
        /// </summary>
        public Vector2 GetMovementInput()
        {
            float horizontal = GetAxis("Horizontal");
            float vertical = GetAxis("Vertical");
            
            // Also check for WASD composite if set up
            if (Mathf.Approximately(horizontal, 0f))
            {
                horizontal = GetAxis("MoveHorizontal");
            }
            if (Mathf.Approximately(vertical, 0f))
            {
                vertical = GetAxis("MoveVertical");
            }
            
            return new Vector2(horizontal, vertical);
        }
        
        /// <summary>
        /// Starts rebinding an action.
        /// </summary>
        public void StartRebind(string actionId, bool primary = true)
        {
            if (!actions.TryGetValue(actionId, out var action)) return;
            if (!action.isRebindable) return;
            
            isRebinding = true;
            rebindingActionId = actionId;
            rebindingPrimary = primary;
        }
        
        /// <summary>
        /// Cancels the current rebind operation.
        /// </summary>
        public void CancelRebind()
        {
            isRebinding = false;
            rebindingActionId = null;
            OnRebindCancelled?.Invoke();
        }
        
        private void CheckForRebind()
        {
            // Check for escape to cancel
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRebind();
                return;
            }
            
            // Check all keys
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.Escape) continue;
                if (key == KeyCode.None) continue;
                
                if (UnityEngine.Input.GetKeyDown(key))
                {
                    CompleteRebind(key);
                    return;
                }
            }
        }
        
        private void CompleteRebind(KeyCode newKey)
        {
            if (!actions.TryGetValue(rebindingActionId, out var action)) return;
            
            // Check for conflicts
            var conflicting = actions.Values.FirstOrDefault(a => 
                a.actionId != rebindingActionId && 
                (a.primaryKey == newKey || a.secondaryKey == newKey));
            
            if (conflicting != null)
            {
                // Clear the conflicting binding
                if (conflicting.primaryKey == newKey) conflicting.primaryKey = KeyCode.None;
                if (conflicting.secondaryKey == newKey) conflicting.secondaryKey = KeyCode.None;
            }
            
            // Set the new binding
            if (rebindingPrimary)
            {
                action.primaryKey = newKey;
            }
            else
            {
                action.secondaryKey = newKey;
            }
            
            isRebinding = false;
            OnRebindComplete?.Invoke(rebindingActionId, newKey);
            rebindingActionId = null;
        }
        
        /// <summary>
        /// Resets an action to its default binding.
        /// </summary>
        public void ResetBinding(string actionId)
        {
            if (defaultPreset == null) return;
            
            var defaultAction = defaultPreset.actions.FirstOrDefault(a => a.actionId == actionId);
            if (defaultAction != null && actions.TryGetValue(actionId, out var action))
            {
                action.primaryKey = defaultAction.primaryKey;
                action.secondaryKey = defaultAction.secondaryKey;
            }
        }
        
        /// <summary>
        /// Resets all bindings to defaults.
        /// </summary>
        public void ResetAllBindings()
        {
            if (defaultPreset != null)
            {
                LoadPreset(defaultPreset);
            }
        }
        
        /// <summary>
        /// Pushes an input context onto the stack.
        /// </summary>
        public void PushContext(InputContext context)
        {
            activeContexts.Add(context);
            activeContexts = activeContexts.OrderByDescending(c => c.priority).ToList();
        }
        
        /// <summary>
        /// Pops a context from the stack.
        /// </summary>
        public void PopContext(string contextId)
        {
            activeContexts.RemoveAll(c => c.contextId == contextId);
        }
        
        /// <summary>
        /// Clears all contexts.
        /// </summary>
        public void ClearContexts()
        {
            activeContexts.Clear();
        }
        
        private bool IsActionAllowed(string actionId)
        {
            if (activeContexts.Count == 0) return true;
            
            var topContext = activeContexts[0];
            
            // If the top context allows this action
            if (topContext.allowedActionIds.Count == 0 || topContext.allowedActionIds.Contains(actionId))
            {
                return true;
            }
            
            // If blocking lower priority, this action is not allowed
            if (topContext.blockLowerPriority)
            {
                return false;
            }
            
            // Check lower priority contexts
            foreach (var context in activeContexts.Skip(1))
            {
                if (context.allowedActionIds.Count == 0 || context.allowedActionIds.Contains(actionId))
                {
                    return true;
                }
                if (context.blockLowerPriority) break;
            }
            
            return false;
        }
        
        private void DetectInputDevice()
        {
            InputDevice newDevice = currentDevice;
            
            // Check for keyboard/mouse input
            if (UnityEngine.Input.anyKey || 
                Mathf.Abs(UnityEngine.Input.GetAxis("Mouse X")) > 0.1f ||
                Mathf.Abs(UnityEngine.Input.GetAxis("Mouse Y")) > 0.1f)
            {
                // Check if it's a gamepad button
                bool isGamepadButton = false;
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        if (UnityEngine.Input.GetKey((KeyCode)(350 + i)))
                        {
                            isGamepadButton = true;
                            break;
                        }
                    }
                    catch { }
                }
                
                if (!isGamepadButton)
                {
                    newDevice = InputDevice.KeyboardMouse;
                }
            }
            
            // Check for gamepad input
            if (enableGamepad)
            {
                try
                {
                    if (Mathf.Abs(UnityEngine.Input.GetAxis("Horizontal")) > gamepadAxisDeadzone ||
                        Mathf.Abs(UnityEngine.Input.GetAxis("Vertical")) > gamepadAxisDeadzone)
                    {
                        // Could be gamepad, check for D-pad or sticks
                        newDevice = InputDevice.Gamepad;
                    }
                }
                catch { }
            }
            
            if (newDevice != currentDevice)
            {
                currentDevice = newDevice;
                OnInputDeviceChanged?.Invoke(currentDevice);
            }
        }
        
        /// <summary>
        /// Gets an action by ID.
        /// </summary>
        public InputAction GetAction(string actionId)
        {
            return actions.TryGetValue(actionId, out var action) ? action : null;
        }
        
        /// <summary>
        /// Gets all actions.
        /// </summary>
        public List<InputAction> GetAllActions()
        {
            return new List<InputAction>(actions.Values);
        }
        
        /// <summary>
        /// Gets all actions in a category.
        /// </summary>
        public List<InputAction> GetActionsByCategory(InputCategory category)
        {
            return actions.Values.Where(a => a.category == category).ToList();
        }
        
        /// <summary>
        /// Gets the display name for a key.
        /// </summary>
        public string GetKeyDisplayName(KeyCode key)
        {
            return key switch
            {
                KeyCode.LeftControl => "LCtrl",
                KeyCode.RightControl => "RCtrl",
                KeyCode.LeftShift => "LShift",
                KeyCode.RightShift => "RShift",
                KeyCode.LeftAlt => "LAlt",
                KeyCode.RightAlt => "RAlt",
                KeyCode.Space => "Space",
                KeyCode.Return => "Enter",
                KeyCode.Escape => "Esc",
                KeyCode.Backspace => "Backspace",
                KeyCode.Tab => "Tab",
                KeyCode.Mouse0 => "LMB",
                KeyCode.Mouse1 => "RMB",
                KeyCode.Mouse2 => "MMB",
                _ => key.ToString()
            };
        }
        
        /// <summary>
        /// Saves current bindings to PlayerPrefs.
        /// </summary>
        public void SaveBindings()
        {
            foreach (var action in actions.Values)
            {
                PlayerPrefs.SetInt($"Input_{action.actionId}_Primary", (int)action.primaryKey);
                PlayerPrefs.SetInt($"Input_{action.actionId}_Secondary", (int)action.secondaryKey);
            }
            
            PlayerPrefs.SetFloat("Input_MouseSensitivity", mouseSensitivity);
            PlayerPrefs.SetInt("Input_InvertMouseY", invertMouseY ? 1 : 0);
            PlayerPrefs.SetFloat("Input_GamepadSensitivity", gamepadAxisSensitivity);
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loads bindings from PlayerPrefs.
        /// </summary>
        public void LoadBindings()
        {
            foreach (var action in actions.Values)
            {
                if (PlayerPrefs.HasKey($"Input_{action.actionId}_Primary"))
                {
                    action.primaryKey = (KeyCode)PlayerPrefs.GetInt($"Input_{action.actionId}_Primary");
                }
                if (PlayerPrefs.HasKey($"Input_{action.actionId}_Secondary"))
                {
                    action.secondaryKey = (KeyCode)PlayerPrefs.GetInt($"Input_{action.actionId}_Secondary");
                }
            }
            
            if (PlayerPrefs.HasKey("Input_MouseSensitivity"))
            {
                mouseSensitivity = PlayerPrefs.GetFloat("Input_MouseSensitivity");
            }
            if (PlayerPrefs.HasKey("Input_InvertMouseY"))
            {
                invertMouseY = PlayerPrefs.GetInt("Input_InvertMouseY") == 1;
            }
            if (PlayerPrefs.HasKey("Input_GamepadSensitivity"))
            {
                gamepadAxisSensitivity = PlayerPrefs.GetFloat("Input_GamepadSensitivity");
            }
        }
    }
    
    public enum InputDevice
    {
        KeyboardMouse,
        Gamepad
    }
}
