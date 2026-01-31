using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UsefulScripts.UI
{
    /// <summary>
    /// Fade controller for screen transitions and effects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultFadeDuration = 0.5f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool startFadedOut = true;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Optional")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup canvasGroup;
        private Coroutine currentFade;

        // Events
        public event System.Action OnFadeInComplete;
        public event System.Action OnFadeOutComplete;

        // Properties
        public bool IsFading => currentFade != null;
        public float CurrentAlpha => canvasGroup.alpha;

        // Static instance
        private static FadeController _instance;
        public static FadeController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FadeController>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _instance = this;
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (fadeImage != null)
            {
                fadeImage.color = fadeColor;
            }

            if (startFadedOut)
            {
                canvasGroup.alpha = 1;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Fade in (from black to clear)
        /// </summary>
        public void FadeIn(float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartFade(1, 0, duration);
        }

        /// <summary>
        /// Fade out (from clear to black)
        /// </summary>
        public void FadeOut(float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartFade(0, 1, duration);
        }

        /// <summary>
        /// Fade to a specific alpha value
        /// </summary>
        public void FadeTo(float targetAlpha, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartFade(canvasGroup.alpha, targetAlpha, duration);
        }

        /// <summary>
        /// Fade out and back in with an action in between
        /// </summary>
        public void FadeOutIn(System.Action onMiddle, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartCoroutine(FadeOutInRoutine(onMiddle, duration));
        }

        /// <summary>
        /// Fade out, load scene, fade in
        /// </summary>
        public void FadeToScene(string sceneName, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            FadeOutIn(() => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName), duration);
        }

        /// <summary>
        /// Fade out, load scene by index, fade in
        /// </summary>
        public void FadeToScene(int buildIndex, float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            FadeOutIn(() => UnityEngine.SceneManagement.SceneManager.LoadScene(buildIndex), duration);
        }

        /// <summary>
        /// Set fade color (if using image)
        /// </summary>
        public void SetFadeColor(Color color)
        {
            fadeColor = color;
            if (fadeImage != null)
            {
                fadeImage.color = color;
            }
        }

        /// <summary>
        /// Set alpha immediately
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (currentFade != null)
            {
                StopCoroutine(currentFade);
                currentFade = null;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0.5f;
        }

        private void StartFade(float from, float to, float duration)
        {
            if (currentFade != null)
            {
                StopCoroutine(currentFade);
            }
            currentFade = StartCoroutine(FadeRoutine(from, to, duration));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float elapsed = 0;
            canvasGroup.alpha = from;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = fadeCurve.Evaluate(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
            canvasGroup.blocksRaycasts = to > 0.5f;
            currentFade = null;

            if (to < from) // Faded in
            {
                OnFadeInComplete?.Invoke();
            }
            else // Faded out
            {
                OnFadeOutComplete?.Invoke();
            }
        }

        private IEnumerator FadeOutInRoutine(System.Action onMiddle, float duration)
        {
            // Fade out
            yield return FadeRoutine(0, 1, duration / 2);

            // Execute middle action
            onMiddle?.Invoke();

            // Wait a frame for scene to load
            yield return null;

            // Fade in
            yield return FadeRoutine(1, 0, duration / 2);
        }

        /// <summary>
        /// Static fade in
        /// </summary>
        public static void DoFadeIn(float duration = -1)
        {
            Instance?.FadeIn(duration);
        }

        /// <summary>
        /// Static fade out
        /// </summary>
        public static void DoFadeOut(float duration = -1)
        {
            Instance?.FadeOut(duration);
        }

        /// <summary>
        /// Static fade to scene
        /// </summary>
        public static void DoFadeToScene(string sceneName, float duration = -1)
        {
            Instance?.FadeToScene(sceneName, duration);
        }
    }
}
