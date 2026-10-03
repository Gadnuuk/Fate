using Fate.Systems.InputActions;
using Fate.Systems.Player;

namespace Fate.Systems.Game
{
    /// <summary>A single device-to-profile pairing handed from the player config screen to <see cref="PlayerControllerManager"/>.</summary>
    public readonly struct PlayerControllerAssignment
    {
        public readonly PlayerId PlayerId;
        public readonly InputDevice Device;

        public PlayerControllerAssignment(PlayerId playerId, InputDevice device)
        {
            PlayerId = playerId;
            Device = device;
        }
    }
}
