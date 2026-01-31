using UnityEngine;

namespace UsefulScripts.Utilities
{
    /// <summary>
    /// Simple tweening utilities for common animations.
    /// </summary>
    public static class Tween
    {
        public enum EaseType
        {
            Linear,
            EaseIn,
            EaseOut,
            EaseInOut,
            Bounce,
            Elastic,
            Back
        }

        /// <summary>
        /// Evaluate an easing function
        /// </summary>
        public static float Evaluate(float t, EaseType easeType)
        {
            t = Mathf.Clamp01(t);

            switch (easeType)
            {
                case EaseType.Linear:
                    return t;

                case EaseType.EaseIn:
                    return t * t * t;

                case EaseType.EaseOut:
                    return 1f - Mathf.Pow(1f - t, 3f);

                case EaseType.EaseInOut:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                case EaseType.Bounce:
                    return EvaluateBounce(t);

                case EaseType.Elastic:
                    return EvaluateElastic(t);

                case EaseType.Back:
                    return EvaluateBack(t);

                default:
                    return t;
            }
        }

        private static float EvaluateBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;
            else if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        private static float EvaluateElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;

            if (t == 0f || t == 1f) return t;

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private static float EvaluateBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Lerp with easing
        /// </summary>
        public static float Lerp(float from, float to, float t, EaseType easeType = EaseType.EaseInOut)
        {
            return Mathf.Lerp(from, to, Evaluate(t, easeType));
        }

        /// <summary>
        /// Lerp Vector3 with easing
        /// </summary>
        public static Vector3 Lerp(Vector3 from, Vector3 to, float t, EaseType easeType = EaseType.EaseInOut)
        {
            return Vector3.Lerp(from, to, Evaluate(t, easeType));
        }

        /// <summary>
        /// Lerp Color with easing
        /// </summary>
        public static Color Lerp(Color from, Color to, float t, EaseType easeType = EaseType.EaseInOut)
        {
            return Color.Lerp(from, to, Evaluate(t, easeType));
        }

        /// <summary>
        /// Slerp Quaternion with easing
        /// </summary>
        public static Quaternion Slerp(Quaternion from, Quaternion to, float t, EaseType easeType = EaseType.EaseInOut)
        {
            return Quaternion.Slerp(from, to, Evaluate(t, easeType));
        }
    }

    /// <summary>
    /// MonoBehaviour component for running tweens
    /// </summary>
    public class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instance;
        
        public static TweenRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("TweenRunner");
                    _instance = go.AddComponent<TweenRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Move transform to position over time
        /// </summary>
        public static System.Collections.IEnumerator MoveTo(Transform target, Vector3 endPosition, float duration, Tween.EaseType ease = Tween.EaseType.EaseInOut, System.Action onComplete = null)
        {
            Vector3 startPosition = target.position;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.position = Tween.Lerp(startPosition, endPosition, elapsed / duration, ease);
                yield return null;
            }

            target.position = endPosition;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Scale transform over time
        /// </summary>
        public static System.Collections.IEnumerator ScaleTo(Transform target, Vector3 endScale, float duration, Tween.EaseType ease = Tween.EaseType.EaseInOut, System.Action onComplete = null)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Tween.Lerp(startScale, endScale, elapsed / duration, ease);
                yield return null;
            }

            target.localScale = endScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Rotate transform over time
        /// </summary>
        public static System.Collections.IEnumerator RotateTo(Transform target, Quaternion endRotation, float duration, Tween.EaseType ease = Tween.EaseType.EaseInOut, System.Action onComplete = null)
        {
            Quaternion startRotation = target.rotation;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.rotation = Tween.Slerp(startRotation, endRotation, elapsed / duration, ease);
                yield return null;
            }

            target.rotation = endRotation;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Fade CanvasGroup over time
        /// </summary>
        public static System.Collections.IEnumerator FadeTo(CanvasGroup target, float endAlpha, float duration, Tween.EaseType ease = Tween.EaseType.EaseInOut, System.Action onComplete = null)
        {
            float startAlpha = target.alpha;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.alpha = Tween.Lerp(startAlpha, endAlpha, elapsed / duration, ease);
                yield return null;
            }

            target.alpha = endAlpha;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Punch scale effect
        /// </summary>
        public static System.Collections.IEnumerator PunchScale(Transform target, float punch, float duration)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + punch * Mathf.Sin(t * Mathf.PI);
                target.localScale = originalScale * scale;
                yield return null;
            }

            target.localScale = originalScale;
        }

        /// <summary>
        /// Shake position effect
        /// </summary>
        public static System.Collections.IEnumerator Shake(Transform target, float magnitude, float duration)
        {
            Vector3 originalPosition = target.localPosition;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float dampingFactor = 1f - (elapsed / duration);
                float x = Random.Range(-1f, 1f) * magnitude * dampingFactor;
                float y = Random.Range(-1f, 1f) * magnitude * dampingFactor;
                target.localPosition = originalPosition + new Vector3(x, y, 0);
                yield return null;
            }

            target.localPosition = originalPosition;
        }
    }
}
