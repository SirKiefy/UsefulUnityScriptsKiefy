using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UsefulScripts.MathRoguelike.UI
{
    /// <summary>
    /// Handles player text input for math answers.
    /// Supports a TMP InputField with a Submit button (or Enter key).
    /// </summary>
    public class MathInputUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button         submitButton;

        [Header("Special Character Buttons (optional)")]
        [SerializeField] private Button piButton;
        [SerializeField] private Button infinityButton;
        [SerializeField] private Button sqrtButton;
        [SerializeField] private Button fractionButton;

        public event System.Action<string> OnAnswerSubmitted;

        private void Awake()
        {
            submitButton?.onClick.AddListener(Submit);
            inputField?.onSubmit.AddListener(_ => Submit());

            piButton?.onClick.AddListener(       () => InsertText("π"));
            infinityButton?.onClick.AddListener( () => InsertText("∞"));
            sqrtButton?.onClick.AddListener(     () => InsertText("√"));
            fractionButton?.onClick.AddListener( () => InsertText("/"));
        }

        /// <summary>Clears the input field.</summary>
        public void Clear()
        {
            if (inputField) inputField.text = string.Empty;
        }

        /// <summary>Enables or disables the entire input panel.</summary>
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active && inputField)
                inputField.Select();
        }

        // ─────────────────────────────────────────────────────────────

        private void Submit()
        {
            string answer = inputField ? inputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(answer)) return;
            OnAnswerSubmitted?.Invoke(answer);
        }

        private void InsertText(string text)
        {
            if (!inputField) return;
            int caret = inputField.caretPosition;
            inputField.text = inputField.text.Insert(caret, text);
            inputField.caretPosition = caret + text.Length;
            inputField.Select();
        }
    }
}
