using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Moves the render camera assigned to this pawn's possessing <see cref="PlayerController"/>
    /// (see <see cref="PlayerControllerManager.GetCameraFor"/>) to <see cref="viewAnchor"/>'s
    /// position/rotation every frame. The pawn's own camera (if it has one) is never a render
    /// camera itself - it's purely a transform anchor for where the view should be.
    /// </summary>
    public class PawnCameraComponent : MonoBehaviour, IPossessionHandler
    {
        [SerializeField]
        private Transform viewAnchor;

        private Camera _targetCamera;

        public void OnPossessed(PlayerController controller)
        {
            _targetCamera = PlayerControllerManager.Instance.GetCameraFor(controller);
        }

        public void OnUnpossessed(PlayerController controller)
        {
            _targetCamera = null;
        }

        // Unity's == null on a destroyed UnityEngine.Object reference returns true, so losing the
        // split-screen camera (e.g. via DisableSplitScreen) stops updates cleanly with no event wiring needed.
        private void LateUpdate()
        {
            if (_targetCamera == null)
                return;

            _targetCamera.transform.SetPositionAndRotation(viewAnchor.position, viewAnchor.rotation);
        }
    }
}
