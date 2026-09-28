using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fate.Systems.Identity
{
    /// <summary>
    /// Local save/load for every <see cref="PlayerProfile"/> on this machine - the backing store
    /// for a console-style "pick your profile" screen. Any local slot can pick any saved profile;
    /// nothing here is tied to a specific slot or device.
    ///
    /// Uses <see cref="JsonUtility"/> (not FishNet's embedded Newtonsoft.Json - no reason to take
    /// that dependency for a small local save file) with a wrapper class since JsonUtility cannot
    /// serialize a bare root list.
    /// </summary>
    public class PlayerProfileStore
    {
        private const string FileName = "player_profiles.json";

        [Serializable]
        private class ProfileCollection
        {
            public List<PlayerProfile> Profiles = new();
        }

        private readonly string _filePath;
        private ProfileCollection _cache;

        public PlayerProfileStore() : this(Path.Combine(Application.persistentDataPath, FileName))
        {
        }

        /// <summary>Overload for tests - lets callers point the store at a temp file.</summary>
        public PlayerProfileStore(string filePath)
        {
            _filePath = filePath;
        }

        public IReadOnlyList<PlayerProfile> LoadAll()
        {
            _cache = ReadFromDisk();
            return _cache.Profiles;
        }

        public void SaveAll(IEnumerable<PlayerProfile> profiles)
        {
            _cache = new ProfileCollection { Profiles = profiles.ToList() };
            WriteToDisk(_cache);
        }

        public PlayerProfile CreateProfile(string displayName)
        {
            EnsureLoaded();

            var profile = new PlayerProfile(PlayerId.New(), displayName);
            _cache.Profiles.Add(profile);
            WriteToDisk(_cache);
            return profile;
        }

        public bool TryLoad(PlayerId id, out PlayerProfile profile)
        {
            EnsureLoaded();

            profile = _cache.Profiles.Find(p => p.PlayerId == id);
            return profile != null;
        }

        public bool Delete(PlayerId id)
        {
            EnsureLoaded();

            int removed = _cache.Profiles.RemoveAll(p => p.PlayerId == id);
            if (removed > 0)
                WriteToDisk(_cache);

            return removed > 0;
        }

        public void TouchLastPlayed(PlayerId id)
        {
            EnsureLoaded();

            PlayerProfile profile = _cache.Profiles.Find(p => p.PlayerId == id);
            if (profile == null)
                return;

            profile.LastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            WriteToDisk(_cache);
        }

        private void EnsureLoaded()
        {
            if (_cache == null)
                _cache = ReadFromDisk();
        }

        private ProfileCollection ReadFromDisk()
        {
            if (!File.Exists(_filePath))
                return new ProfileCollection();

            try
            {
                string json = File.ReadAllText(_filePath);
                var loaded = JsonUtility.FromJson<ProfileCollection>(json);
                return loaded ?? new ProfileCollection();
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(PlayerProfileStore)}: failed to read '{_filePath}' - starting with an empty profile list. {e}");
                return new ProfileCollection();
            }
        }

        private void WriteToDisk(ProfileCollection collection)
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(collection, prettyPrint: true);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(PlayerProfileStore)}: failed to write '{_filePath}'. {e}");
            }
        }
    }
}
