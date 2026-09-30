using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fate.Systems.Player
{
    /// <summary>
    /// Loads/saves <see cref="PlayerProfile"/>s via <see cref="PlayerPrefs"/> and tracks which
    /// ones are currently logged in on this machine.
    ///
    /// Storage shape: each profile is saved under its own PlayerPrefs key
    /// ("PlayerProfile.&lt;uuid&gt;") as a JsonUtility-serialized string. A separate index key
    /// ("PlayerProfile.Index") holds a delimited list of every known uuid, since PlayerPrefs has
    /// no way to enumerate its own keys.
    ///
    /// More than one profile can be logged in at once (local split-screen), but exactly one is
    /// ever the primary - the profile that acts as host. Mapping input devices to logged-in
    /// profiles happens elsewhere and comes later.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        private const string ProfileKeyPrefix = "PlayerProfile.";
        private const string IndexKey = "PlayerProfile.Index";
        private const char IndexDelimiter = ';';

        private readonly List<PlayerProfile> _loggedInProfiles = new();

        public IReadOnlyList<PlayerProfile> LoggedInProfiles => _loggedInProfiles;

        public PlayerProfile Primary { get; private set; }

        /// <summary>Reads every saved profile from PlayerPrefs. Does not log any of them in.</summary>
        public List<PlayerProfile> LoadProfiles()
        {
            var profiles = new List<PlayerProfile>();

            foreach (string uuid in ReadIndex())
            {
                string key = ProfileKeyPrefix + uuid;
                if (!PlayerPrefs.HasKey(key))
                    continue;

                string json = PlayerPrefs.GetString(key);
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
                if (profile != null)
                    profiles.Add(profile);
            }

            return profiles;
        }

        /// <summary>Creates a new profile with a fresh <see cref="PlayerId"/> and saves it.</summary>
        public PlayerProfile CreateProfile(string tag)
        {
            var profile = new PlayerProfile(PlayerId.New(tag));
            SaveProfile(profile);
            return profile;
        }

        /// <summary>Writes a profile to PlayerPrefs, adding it to the index if it's new.</summary>
        public void SaveProfile(PlayerProfile profile)
        {
            string uuid = profile.PlayerId.Uuid;
            PlayerPrefs.SetString(ProfileKeyPrefix + uuid, JsonUtility.ToJson(profile));

            List<string> index = ReadIndex();
            if (!index.Contains(uuid))
            {
                index.Add(uuid);
                WriteIndex(index);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Logs a profile in for this session. The first profile logged in becomes primary/host
        /// automatically; use <see cref="SetPrimary"/> to change that afterwards.
        /// </summary>
        public void LogIn(PlayerProfile profile)
        {
            if (_loggedInProfiles.Any(p => p.PlayerId == profile.PlayerId))
                return;

            _loggedInProfiles.Add(profile);

            if (Primary == null)
                Primary = profile;
        }

        /// <summary>Logs a profile out. If it was primary, the next logged-in profile (if any) takes over.</summary>
        public void LogOut(PlayerProfile profile)
        {
            _loggedInProfiles.Remove(profile);

            if (Primary == profile)
                Primary = _loggedInProfiles.FirstOrDefault();
        }

        /// <summary>Makes an already logged-in profile the primary/host.</summary>
        public void SetPrimary(PlayerProfile profile)
        {
            if (!_loggedInProfiles.Contains(profile))
                throw new InvalidOperationException($"{nameof(ProfileManager)}: cannot make a profile primary before it is logged in.");

            Primary = profile;
        }

        private List<string> ReadIndex()
        {
            string raw = PlayerPrefs.GetString(IndexKey, string.Empty);
            return string.IsNullOrEmpty(raw)
                ? new List<string>()
                : raw.Split(IndexDelimiter).ToList();
        }

        private void WriteIndex(List<string> index)
        {
            PlayerPrefs.SetString(IndexKey, string.Join(IndexDelimiter, index));
        }
    }
}
