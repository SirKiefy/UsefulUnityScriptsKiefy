using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace UsefulScripts.Dialogue
{
    /// <summary>
    /// Dialogue system manager for handling conversations.
    /// </summary>
    public class DialogueManager : Core.Singleton<DialogueManager>
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float defaultTypingSpeed = 0.05f;
        [SerializeField] private bool useTypingEffect = true;
        [SerializeField] private KeyCode advanceKey = KeyCode.Space;
        [SerializeField] private KeyCode skipKey = KeyCode.Return;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource typingSource;
        [SerializeField] private AudioClip typingSound;

        // State
        private DialogueData currentDialogue;
        private int currentNodeIndex;
        private bool isTyping;
        private bool isDialogueActive;
        private Coroutine typingCoroutine;
        private HashSet<string> dialogueFlags = new HashSet<string>();
        private Dictionary<string, int> dialogueVariables = new Dictionary<string, int>();

        // Events
        public event System.Action<DialogueData> OnDialogueStarted;
        public event System.Action OnDialogueEnded;
        public event System.Action<DialogueNode> OnNodeDisplayed;
        public event System.Action<DialogueChoice> OnChoiceSelected;

        // Properties
        public bool IsDialogueActive => isDialogueActive;
        public bool IsTyping => isTyping;

        protected override void OnSingletonAwake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            // Advance or skip dialogue
            if (Input.GetKeyDown(advanceKey))
            {
                if (isTyping)
                {
                    SkipTyping();
                }
                else if (currentNodeIndex >= 0)
                {
                    var node = currentDialogue.GetNode(currentNodeIndex);
                    if (node != null && node.choices.Count == 0)
                    {
                        AdvanceDialogue();
                    }
                }
            }

            if (Input.GetKeyDown(skipKey) && isTyping)
            {
                SkipTyping();
            }
        }

        /// <summary>
        /// Start a dialogue
        /// </summary>
        public void StartDialogue(DialogueData dialogue, int startNodeIndex = 0)
        {
            if (dialogue == null || dialogue.nodes.Count == 0)
            {
                Debug.LogWarning("No dialogue data provided!");
                return;
            }

            currentDialogue = dialogue;
            currentNodeIndex = startNodeIndex;
            isDialogueActive = true;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            OnDialogueStarted?.Invoke(dialogue);
            DisplayNode(currentNodeIndex);
        }

        /// <summary>
        /// Display a dialogue node
        /// </summary>
        private void DisplayNode(int nodeIndex)
        {
            var node = currentDialogue.GetNode(nodeIndex);
            if (node == null)
            {
                EndDialogue();
                return;
            }

            currentNodeIndex = nodeIndex;

            // Set speaker info
            if (speakerNameText != null)
            {
                speakerNameText.text = node.speakerName;
            }

            if (speakerPortrait != null)
            {
                if (node.speakerPortrait != null)
                {
                    speakerPortrait.sprite = node.speakerPortrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            // Play voice clip
            if (voiceSource != null && node.voiceClip != null)
            {
                voiceSource.clip = node.voiceClip;
                voiceSource.Play();
            }

            // Process events
            ProcessEvents(node.events);

            // Display text
            if (useTypingEffect)
            {
                typingCoroutine = StartCoroutine(TypeText(node.dialogueText, node.typingSpeed));
            }
            else
            {
                if (dialogueText != null)
                {
                    dialogueText.text = node.dialogueText;
                }
                DisplayChoices(node);
            }

            OnNodeDisplayed?.Invoke(node);

            // Auto advance
            if (node.autoAdvance && node.choices.Count == 0)
            {
                StartCoroutine(AutoAdvance(node.autoAdvanceDelay));
            }
        }

        private IEnumerator TypeText(string text, float speed)
        {
            isTyping = true;
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }

            // Clear choices while typing
            ClearChoices();

            foreach (char c in text)
            {
                if (!isTyping) break;

                if (dialogueText != null)
                {
                    dialogueText.text += c;
                }

                // Play typing sound
                if (typingSource != null && typingSound != null && c != ' ')
                {
                    typingSource.PlayOneShot(typingSound);
                }

                yield return new WaitForSeconds(speed > 0 ? speed : defaultTypingSpeed);
            }

            isTyping = false;
            
            var node = currentDialogue.GetNode(currentNodeIndex);
            if (node != null)
            {
                DisplayChoices(node);
            }
        }

        private void SkipTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            isTyping = false;

            var node = currentDialogue.GetNode(currentNodeIndex);
            if (node != null && dialogueText != null)
            {
                dialogueText.text = node.dialogueText;
                DisplayChoices(node);
            }
        }

        private void DisplayChoices(DialogueNode node)
        {
            ClearChoices();

            if (node.choices.Count == 0) return;

            foreach (var choice in node.choices)
            {
                // Check if choice should be displayed
                if (!string.IsNullOrEmpty(choice.requiredFlag) && !HasFlag(choice.requiredFlag))
                {
                    continue;
                }

                CreateChoiceButton(choice);
            }
        }

        private void CreateChoiceButton(DialogueChoice choice)
        {
            if (choiceButtonPrefab == null || choicesContainer == null) return;

            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
            
            // Set text
            var textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = choice.choiceText;
            }

            // Set click handler
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectChoice(choice));
            }
        }

        private void ClearChoices()
        {
            if (choicesContainer == null) return;

            foreach (Transform child in choicesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Select a dialogue choice
        /// </summary>
        public void SelectChoice(DialogueChoice choice)
        {
            // Set flag if configured
            if (choice.setFlag && !string.IsNullOrEmpty(choice.flagToSet))
            {
                SetFlag(choice.flagToSet);
            }

            OnChoiceSelected?.Invoke(choice);

            if (choice.nextNodeIndex >= 0)
            {
                DisplayNode(choice.nextNodeIndex);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Advance to the next node
        /// </summary>
        public void AdvanceDialogue()
        {
            var node = currentDialogue.GetNode(currentNodeIndex);
            if (node == null || node.choices.Count > 0)
            {
                return;
            }

            if (node.nextNodeIndex >= 0)
            {
                DisplayNode(node.nextNodeIndex);
            }
            else
            {
                EndDialogue();
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (isDialogueActive && !isTyping)
            {
                AdvanceDialogue();
            }
        }

        /// <summary>
        /// End the current dialogue
        /// </summary>
        public void EndDialogue()
        {
            isDialogueActive = false;
            isTyping = false;
            currentDialogue = null;
            currentNodeIndex = -1;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            ClearChoices();
            OnDialogueEnded?.Invoke();
        }

        private void ProcessEvents(DialogueEvent[] events)
        {
            if (events == null) return;

            foreach (var evt in events)
            {
                switch (evt.type)
                {
                    case DialogueEvent.EventType.SetFlag:
                        SetFlag(evt.eventData);
                        break;
                    case DialogueEvent.EventType.ClearFlag:
                        ClearFlag(evt.eventData);
                        break;
                    case DialogueEvent.EventType.TriggerEvent:
                        Events.SimpleEventManager.Trigger(evt.eventData);
                        break;
                    case DialogueEvent.EventType.PlaySound:
                        Audio.AudioManager.Instance?.PlaySound(evt.eventData);
                        break;
                }
            }
        }

        // Flag management
        public void SetFlag(string flag) => dialogueFlags.Add(flag);
        public void ClearFlag(string flag) => dialogueFlags.Remove(flag);
        public bool HasFlag(string flag) => dialogueFlags.Contains(flag);
        public void ClearAllFlags() => dialogueFlags.Clear();

        // Variable management
        public void SetVariable(string key, int value) => dialogueVariables[key] = value;
        public int GetVariable(string key) => dialogueVariables.TryGetValue(key, out int val) ? val : 0;
        public void ClearVariables() => dialogueVariables.Clear();
    }
}
