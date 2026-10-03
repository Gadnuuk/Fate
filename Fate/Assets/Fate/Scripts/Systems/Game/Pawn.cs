using UnityEngine;
namespace Fate.Systems.Game
{
    /// <summary>
    /// This is the link from a PlayerController to anything in game from a player character to a main menu controller
    /// </summary>
    public class Pawn : MonoBehaviour
    {
        [SerializeField]
        private bool isGlobal = false;
        public bool IsGlobal{ get => isGlobal; set => isGlobal = value;}

        private IPossessionHandler[] _possessionHandlers;

        /// <summary>The controller currently possessing this pawn, or null if none.</summary>
        public PlayerController Controller { get; private set; }

        public bool IsPossessed => Controller != null;

        private void Awake()
        {
            _possessionHandlers = GetComponents<IPossessionHandler>();
        }

        private void OnEnable()
        {
            if (isGlobal)
                PlayerControllerManager.Instance.GlobalController.PossessGlobalPawn(this);
        }

        private void OnDisable()
        {
            if (isGlobal)
                PlayerControllerManager.Instance.GlobalController.UnpossessGlobalPawn(this);
        }

        /// <summary>
        /// Called by a <see cref="PlayerController"/> taking possession. Notifies every sibling
        /// <see cref="IPossessionHandler"/> so components can bind to their input actions without
        /// caring which context the controller is filtering through.
        /// </summary>
        public void Possess(PlayerController controller)
        {
            if (Controller == controller)
                return;

            if (Controller != null)
                Unpossess();

            Controller = controller;
            foreach (IPossessionHandler handler in _possessionHandlers)
                handler.OnPossessed(controller);
        }

        /// <summary>Releases the current controller, notifying every sibling <see cref="IPossessionHandler"/> to unbind.</summary>
        public void Unpossess()
        {
            if (Controller == null)
                return;

            PlayerController previous = Controller;
            Controller = null;
            foreach (IPossessionHandler handler in _possessionHandlers)
                handler.OnUnpossessed(previous);
        }
    }
}
