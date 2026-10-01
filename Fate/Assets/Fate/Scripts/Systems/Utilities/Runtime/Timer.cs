using UnityEngine;
using UnityEngine.Events;

namespace Fate.Systems.Utilities
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool useUnscaledTime;

        [SerializeField] private UnityEvent onTimerDone;

        public float Duration => duration;
        public float RemainingTime => Mathf.Max(0f, duration - elapsedTime);
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private float elapsedTime;

        void Update()
        {
            if (!IsRunning || IsPaused)
            {
                return;
            }

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (elapsedTime >= duration)
            {
                StopTimer();
                onTimerDone?.Invoke();
            }
        }

        public void StartTimer()
        {
            StartTimer(duration);
        }

        public void StartTimer(float newDuration)
        {
            duration = newDuration;
            elapsedTime = 0f;
            IsRunning = true;
            IsPaused = false;

        }

        public void StopTimer()
        {
            IsRunning = false;
            IsPaused = false;
            elapsedTime = 0f;
        }

        public void PauseTimer()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        public void ResumeTimer()
        {
            if (IsRunning)
            {
                IsPaused = false;
            }
        }
    }
}
