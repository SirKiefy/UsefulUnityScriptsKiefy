using UnityEngine;
using System;

namespace UsefulScripts.Utilities
{
    /// <summary>
    /// Flexible timer utility for countdown and stopwatch functionality.
    /// </summary>
    [Serializable]
    public class Timer
    {
        public enum TimerType
        {
            Countdown,
            Stopwatch
        }

        [SerializeField] private TimerType type = TimerType.Countdown;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool useUnscaledTime = false;

        private float currentTime;
        private bool isRunning;
        private bool isPaused;

        // Events
        public event Action OnTimerStart;
        public event Action OnTimerComplete;
        public event Action OnTimerPaused;
        public event Action OnTimerResumed;
        public event Action<float> OnTimerTick;

        // Properties
        public float CurrentTime => currentTime;
        public float Duration => duration;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public float Progress => type == TimerType.Countdown ? 
            1f - (currentTime / duration) : 
            Mathf.Min(currentTime / duration, 1f);
        public float RemainingTime => type == TimerType.Countdown ? currentTime : duration - currentTime;

        public Timer() { }

        public Timer(float duration, TimerType type = TimerType.Countdown, bool loop = false)
        {
            this.duration = duration;
            this.type = type;
            this.loop = loop;
        }

        /// <summary>
        /// Start or restart the timer
        /// </summary>
        public void Start()
        {
            currentTime = type == TimerType.Countdown ? duration : 0f;
            isRunning = true;
            isPaused = false;
            OnTimerStart?.Invoke();
        }

        /// <summary>
        /// Start with a new duration
        /// </summary>
        public void Start(float newDuration)
        {
            duration = newDuration;
            Start();
        }

        /// <summary>
        /// Stop the timer
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            isPaused = false;
        }

        /// <summary>
        /// Pause the timer
        /// </summary>
        public void Pause()
        {
            if (isRunning && !isPaused)
            {
                isPaused = true;
                OnTimerPaused?.Invoke();
            }
        }

        /// <summary>
        /// Resume the timer
        /// </summary>
        public void Resume()
        {
            if (isRunning && isPaused)
            {
                isPaused = false;
                OnTimerResumed?.Invoke();
            }
        }

        /// <summary>
        /// Reset the timer without starting
        /// </summary>
        public void Reset()
        {
            currentTime = type == TimerType.Countdown ? duration : 0f;
            isRunning = false;
            isPaused = false;
        }

        /// <summary>
        /// Add time to the timer
        /// </summary>
        public void AddTime(float time)
        {
            if (type == TimerType.Countdown)
            {
                currentTime = Mathf.Min(currentTime + time, duration);
            }
            else
            {
                currentTime = Mathf.Max(currentTime - time, 0f);
            }
        }

        /// <summary>
        /// Update the timer. Call this in Update().
        /// </summary>
        public void Tick()
        {
            if (!isRunning || isPaused) return;

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (type == TimerType.Countdown)
            {
                currentTime -= deltaTime;
                OnTimerTick?.Invoke(currentTime);

                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    OnTimerComplete?.Invoke();

                    if (loop)
                    {
                        currentTime = duration;
                    }
                    else
                    {
                        isRunning = false;
                    }
                }
            }
            else // Stopwatch
            {
                currentTime += deltaTime;
                OnTimerTick?.Invoke(currentTime);

                if (currentTime >= duration)
                {
                    OnTimerComplete?.Invoke();

                    if (loop)
                    {
                        currentTime = 0f;
                    }
                    else
                    {
                        currentTime = duration;
                        isRunning = false;
                    }
                }
            }
        }

        /// <summary>
        /// Format time as MM:SS
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Format time as MM:SS:MS
        /// </summary>
        public string GetFormattedTimeWithMilliseconds()
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            int milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);
            return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }
}
