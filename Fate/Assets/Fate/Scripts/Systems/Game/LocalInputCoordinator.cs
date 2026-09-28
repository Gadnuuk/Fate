using System.Collections.Generic;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Scene-level, per-machine pause coordinator for local split-screen players. Deliberately
    /// just a plain <see cref="MonoBehaviour"/> - not a <c>NetworkBehaviour</c>, and never touches
    /// <see cref="Time.timeScale"/>. Pause here means "this one local slot's own
    /// <see cref="PlayerInputRig"/> switches to <see cref="InputContext.Menu"/>", nothing more.
    ///
    /// Every other local slot's rig is left completely alone: if slot 0 pauses, slot 1 keeps
    /// receiving Gameplay input and keeps moving/looking/firing normally. This mirrors
    /// <see cref="PlayerCameraRig"/>'s "each local slot is independent" design on the input side,
    /// and matches the project's explicit requirement that pausing is scoped per-player rather
    /// than global.
    ///
    /// Rigs register themselves (typically from <c>PlayerCameraRig.ApplyLocalSlot</c>, right after
    /// <see cref="PlayerInputRig.Initialize"/>) rather than this coordinator hunting for them, so
    /// it works the same whether all local players' rigs already exist in the scene or spawn in
    /// one at a time.
    /// </summary>
    public class LocalInputCoordinator : MonoBehaviour
    {
        private readonly List<PlayerInputRig> _rigs = new();

        public void Register(PlayerInputRig rig)
        {
            if (rig == null || _rigs.Contains(rig))
                return;

            _rigs.Add(rig);
            rig.PausePressed += () => OnPausePressed(rig);
            rig.MenuCancelPressed += () => OnMenuCancelPressed(rig);
        }

        public void Unregister(PlayerInputRig rig)
        {
            _rigs.Remove(rig);
        }

        /// <summary>
        /// Only the pausing rig switches to Menu - every other registered rig's context is
        /// untouched, so other local players keep playing uninterrupted.
        /// </summary>
        private void OnPausePressed(PlayerInputRig rig)
        {
            if (rig.Context != InputContext.Gameplay)
                return;

            rig.SetContext(InputContext.Menu);
        }

        /// <summary>Mirror of <see cref="OnPausePressed"/> - only the cancelling rig resumes.</summary>
        private void OnMenuCancelPressed(PlayerInputRig rig)
        {
            if (rig.Context != InputContext.Menu)
                return;

            rig.SetContext(InputContext.Gameplay);
        }
    }
}
