using System;

namespace Fate.Systems.Player
{
    /// <summary>
    /// Uniquely identifies a saved <see cref="PlayerProfile"/> on this machine, plus the tag
    /// shown alongside it (e.g. a short display tag distinct from the raw uuid). Plain
    /// public-field struct so it round-trips through <see cref="UnityEngine.JsonUtility"/> as a
    /// nested field on <see cref="PlayerProfile"/> without any custom converters.
    /// </summary>
    [Serializable]
    public struct PlayerId : IEquatable<PlayerId>
    {
        public string Uuid;
        public string Tag;

        public static PlayerId New(string tag) => new PlayerId
        {
            Uuid = Guid.NewGuid().ToString("N"),
            Tag = tag,
        };

        public bool IsEmpty => string.IsNullOrEmpty(Uuid);

        public bool Equals(PlayerId other) => string.Equals(Uuid, other.Uuid, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);

        public override int GetHashCode() => Uuid != null ? Uuid.GetHashCode() : 0;

        public override string ToString() => string.IsNullOrEmpty(Tag) ? Uuid : $"{Tag} ({Uuid})";

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
