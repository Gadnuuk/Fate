using System;

namespace Fate.Systems.Identity
{
    /// <summary>
    /// A locally-generated, stable identifier for a player profile on this machine.
    ///
    /// This is intentionally provider-agnostic: it is not tied to any online backend,
    /// platform account, or matchmaking service. It exists so a saved <see cref="PlayerProfile"/>
    /// has a stable key, and so that key can travel over the network (see
    /// <see cref="PlayerIdentityBroadcast"/>) as "online presence" for today's direct-connect
    /// party model. A future real backend can map this same id to a platform account without
    /// changing any of the call sites that only need a stable id + display name.
    /// </summary>
    [Serializable]
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        [UnityEngine.SerializeField]
        private readonly string value;

        private PlayerId(string value)
        {
            this.value = value;
        }

        public static PlayerId Empty => new PlayerId(string.Empty);

        public bool IsEmpty => string.IsNullOrEmpty(value);

        public static PlayerId New() => new PlayerId(Guid.NewGuid().ToString("N"));

        /// <summary>
        /// Reconstructs a <see cref="PlayerId"/> from its wire/save-file string form.
        /// Does not validate that the string is a well-formed guid - any non-empty,
        /// stable string is accepted so ids created by a future backend still round-trip.
        /// </summary>
        public static PlayerId FromString(string raw) => new PlayerId(raw ?? string.Empty);

        public override string ToString() => value ?? string.Empty;

        public bool Equals(PlayerId other) => string.Equals(value, other.value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);

        public override int GetHashCode() => value != null ? value.GetHashCode() : 0;

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
