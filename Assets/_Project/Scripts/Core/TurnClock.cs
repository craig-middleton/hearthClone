using System;

namespace HearthstoneClone.Core
{
    // Match-owned countdown. The composition root decides when a playable turn starts,
    // blocks new input at expiry, and waits for already accepted action presentation.
    public sealed class TurnClock
    {
        public bool Active { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool Expired => Active && RemainingSeconds <= 0;

        public void Start(float durationSeconds)
        {
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            RemainingSeconds = durationSeconds;
            Active = true;
        }
        public void Advance(float elapsedSeconds)
        {
            if (!Active || elapsedSeconds <= 0 || float.IsNaN(elapsedSeconds)) return;
            RemainingSeconds = Math.Max(0, RemainingSeconds - elapsedSeconds);
        }
        public void Stop()
        {
            Active = false;
            RemainingSeconds = 0;
        }
    }
}
