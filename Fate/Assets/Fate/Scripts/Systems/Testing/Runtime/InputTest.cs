using UnityEngine;
using Fate.Systems.Game;
using Fate.Systems.InputActions;

public class InputTest : MonoBehaviour, IPossessionHandler
{
    [SerializeField]
    private InputAction jumpAction;
    
    [SerializeField]
    private InputAction moveHorizontalAction;
    

    PlayerController controller = null;
    
    public void OnPossessed(PlayerController controller)
    {
        this.controller = controller; 
        controller.ActionPressed += OnPressed;
    }

    public void OnUnpossessed(PlayerController controller)
    {
        controller.ActionPressed -= OnPressed;
        this.controller = null; 
    }

    private void OnPressed(InputAction a) { if (a == jumpAction) Jump(); }
    private void Jump()
    {
        Debug.Log("Jump");
    }

    private void Update()
    {
        if(controller != null)
        {
            float axis = controller.GetAxis(moveHorizontalAction);
            Debug.Log($"{axis}");
        }
    }
}
