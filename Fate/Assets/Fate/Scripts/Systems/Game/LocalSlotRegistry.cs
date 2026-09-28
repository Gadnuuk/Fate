using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Client-local, non-networked bookkeeping that lets any number of owned
    /// <see cref="PlayerCameraRig"/> instances claim a local split-screen slot automatically,
    /// purely from "how many owned instances exist on this machine right now" - no per-instance
    /// inspector setup (<see cref="PlayerCameraRig.localSlotIndex"/> /
    /// <see cref="PlayerCameraRig.totalLocalPlayers"/>) needed. Exists purely so ad-hoc local
    /// testing - drop N player prefab instances into a scene, press Play - gets isolated
    /// camera/input for each with zero manual scaffolding.
    ///
    /// The real join lobby (Phase 3,
    /// Assets/Fate/Docs/SplitScreenNetworkedPlayers.md) replaces this with an explicit
    /// device/quadrant assignment; it calls <see cref="PlayerCameraRig.ApplyLocalSlot"/> directly
    /// and never touches this registry (see <see cref="PlayerCameraRig"/>'s
    /// <c>autoAssignLocalSlot</c> toggle, which opts a given instance out of this entirely).
    /// </summary>
    internal static class LocalSlotRegistry
    {
        private static readonly List<PlayerCameraRig> _slots = new();

        /// <summary>
        /// Claims the lowest free slot index for <paramref name="rig"/> and re-applies every
        /// other already-claimed rig's slot too, so total-player-count-dependent viewport splits
        /// stay correct as players join. Returns -1 if every slot up to
        /// <see cref="PlayerCameraRig.MaxLocalSlots"/> is already taken.
        /// </summary>
        public static int Claim(PlayerCameraRig rig)
        {
            int slotIndex = _slots.IndexOf(null);
            if (slotIndex < 0)
            {
                if (_slots.Count >= PlayerCameraRig.MaxLocalSlots)
                {
                    Debug.LogError($"{nameof(LocalSlotRegistry)}: all {PlayerCameraRig.MaxLocalSlots} local slots " +
                                    "are already claimed on this machine - cannot auto-assign another local player.", rig);
                    return -1;
                }

                slotIndex = _slots.Count;
                _slots.Add(rig);
            }
            else
            {
                _slots[slotIndex] = rig;
            }

            RefreshAll();
            return slotIndex;
        }

        /// <summary>Frees <paramref name="rig"/>'s slot (e.g. on despawn/disconnect) and
        /// re-applies every remaining claimed rig's slot so viewports expand back.</summary>
        public static void Release(PlayerCameraRig rig)
        {
            int slotIndex = _slots.IndexOf(rig);
            if (slotIndex < 0)
                return;

            _slots[slotIndex] = null;

            // Trim trailing empty slots so a later Claim() reuses the lowest free index instead
            // of growing forever.
            while (_slots.Count > 0 && _slots[^1] == null)
                _slots.RemoveAt(_slots.Count - 1);

            RefreshAll();
        }

        private static void RefreshAll()
        {
            int total = Mathf.Max(1, _slots.Count(r => r != null));
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.ApplyLocalSlot(i, total);
        }
    }
}
