using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UsefulScripts.UI
{
    /// <summary>
    /// Central UI manager for handling panels, popups, and transitions.
    /// </summary>
    public class UIManager : Core.Singleton<UIManager>
    {
        [System.Serializable]
        public class UIPanel
        {
            public string panelName;
            public GameObject panelObject;
            public bool startActive = false;
        }

        [Header("Panels")]
        [SerializeField] private List<UIPanel> panels = new List<UIPanel>();

        [Header("Transition Settings")]
        [SerializeField] private float defaultFadeDuration = 0.3f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Dictionary<string, UIPanel> panelDict = new Dictionary<string, UIPanel>();
        private Stack<string> panelHistory = new Stack<string>();
        private string currentPanel;

        // Events
        public event System.Action<string> OnPanelOpened;
        public event System.Action<string> OnPanelClosed;

        protected override void OnSingletonAwake()
        {
            InitializePanels();
        }

        private void InitializePanels()
        {
            panelDict.Clear();
            foreach (var panel in panels)
            {
                if (panel.panelObject != null)
                {
                    panelDict[panel.panelName] = panel;
                    panel.panelObject.SetActive(panel.startActive);
                    
                    if (panel.startActive)
                    {
                        currentPanel = panel.panelName;
                    }
                }
            }
        }

        /// <summary>
        /// Show a panel by name
        /// </summary>
        public void ShowPanel(string panelName, bool hideOthers = true)
        {
            if (!panelDict.TryGetValue(panelName, out var panel))
            {
                Debug.LogWarning($"Panel '{panelName}' not found!");
                return;
            }

            if (hideOthers)
            {
                HideAllPanels();
            }

            if (!string.IsNullOrEmpty(currentPanel) && currentPanel != panelName)
            {
                panelHistory.Push(currentPanel);
            }

            panel.panelObject.SetActive(true);
            currentPanel = panelName;
            OnPanelOpened?.Invoke(panelName);
        }

        /// <summary>
        /// Show a panel with fade transition
        /// </summary>
        public void ShowPanelFade(string panelName, bool hideOthers = true, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartCoroutine(FadePanel(panelName, true, duration, hideOthers));
        }

        /// <summary>
        /// Hide a panel by name
        /// </summary>
        public void HidePanel(string panelName)
        {
            if (!panelDict.TryGetValue(panelName, out var panel))
            {
                Debug.LogWarning($"Panel '{panelName}' not found!");
                return;
            }

            panel.panelObject.SetActive(false);
            OnPanelClosed?.Invoke(panelName);

            if (currentPanel == panelName)
            {
                currentPanel = null;
            }
        }

        /// <summary>
        /// Hide a panel with fade transition
        /// </summary>
        public void HidePanelFade(string panelName, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartCoroutine(FadePanel(panelName, false, duration, false));
        }

        /// <summary>
        /// Hide all panels
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var panel in panelDict.Values)
            {
                if (panel.panelObject.activeSelf)
                {
                    panel.panelObject.SetActive(false);
                    OnPanelClosed?.Invoke(panel.panelName);
                }
            }
            currentPanel = null;
        }

        /// <summary>
        /// Go back to previous panel
        /// </summary>
        public void GoBack()
        {
            if (panelHistory.Count > 0)
            {
                string previousPanel = panelHistory.Pop();
                ShowPanel(previousPanel, true);
            }
        }

        /// <summary>
        /// Toggle a panel's visibility
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (!panelDict.TryGetValue(panelName, out var panel))
            {
                Debug.LogWarning($"Panel '{panelName}' not found!");
                return;
            }

            if (panel.panelObject.activeSelf)
            {
                HidePanel(panelName);
            }
            else
            {
                ShowPanel(panelName, false);
            }
        }

        /// <summary>
        /// Check if a panel is visible
        /// </summary>
        public bool IsPanelVisible(string panelName)
        {
            if (panelDict.TryGetValue(panelName, out var panel))
            {
                return panel.panelObject.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// Get the currently active panel
        /// </summary>
        public string GetCurrentPanel()
        {
            return currentPanel;
        }

        /// <summary>
        /// Register a panel at runtime
        /// </summary>
        public void RegisterPanel(string name, GameObject panelObject)
        {
            if (panelDict.ContainsKey(name))
            {
                Debug.LogWarning($"Panel '{name}' already registered!");
                return;
            }

            var newPanel = new UIPanel
            {
                panelName = name,
                panelObject = panelObject,
                startActive = false
            };

            panels.Add(newPanel);
            panelDict[name] = newPanel;
        }

        private IEnumerator FadePanel(string panelName, bool show, float duration, bool hideOthers)
        {
            if (!panelDict.TryGetValue(panelName, out var panel)) yield break;

            CanvasGroup canvasGroup = panel.panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.panelObject.AddComponent<CanvasGroup>();
            }

            if (show)
            {
                if (hideOthers) HideAllPanels();
                panel.panelObject.SetActive(true);
                canvasGroup.alpha = 0;
            }

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = show ? 1 : 0;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (!show)
            {
                panel.panelObject.SetActive(false);
                OnPanelClosed?.Invoke(panelName);
            }
            else
            {
                currentPanel = panelName;
                OnPanelOpened?.Invoke(panelName);
            }
        }

        /// <summary>
        /// Clear panel history
        /// </summary>
        public void ClearHistory()
        {
            panelHistory.Clear();
        }
    }
}
