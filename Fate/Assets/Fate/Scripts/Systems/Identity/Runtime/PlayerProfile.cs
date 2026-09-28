using System;

namespace Fate.Systems.Identity
{
    /// <summary>
    /// A single saved local player profile - console-style "user" that any local slot can pick
    /// before joining a game. Deliberately flat/plain so it round-trips through
    /// <see cref="UnityEngine.JsonUtility"/> without any custom converters.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string Id;
        public string DisplayName;
        public long CreatedUtcTicks;
        public long LastPlayedUtcTicks;

        public PlayerProfile()
        {
        }

        public PlayerProfile(PlayerId id, string displayName)
        {
            Id = id.ToString();
            DisplayName = displayName;
            CreatedUtcTicks = DateTime.UtcNow.Ticks;
            LastPlayedUtcTicks = CreatedUtcTicks;
        }

        public PlayerId PlayerId => PlayerId.FromString(Id);
    }
}
