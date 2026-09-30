using System;

namespace Fate.Systems.Player
{
    /// <summary>
    /// A single saved local player profile. Currently just an identity; this will grow into the
    /// container for progress and other per-player save data.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public PlayerId PlayerId;

        public PlayerProfile()
        {
        }

        public PlayerProfile(PlayerId playerId)
        {
            PlayerId = playerId;
        }
    }
}
