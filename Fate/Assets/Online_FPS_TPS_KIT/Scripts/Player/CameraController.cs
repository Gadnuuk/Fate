using Fate.Systems.Game;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachinePOVExtension cinemachinePOVExtension;
    [SerializeField] private float sensitivity = 80;

    [SerializeField] private Transform cameraTPVRotater;
    [SerializeField] private Transform cameraTPVRotater2;
    [SerializeField] private Vector3 cameraRotationValue;

    [SerializeField] float maxViewAngle = 80;

    [Tooltip("This instance's local input rig - see PlayerInputRig / PlayerCameraRig. Auto-resolved " +
             "from a parent if left unassigned.")]
    [SerializeField] private PlayerInputRig inputRig;

    private void Awake()
    {
        if (inputRig == null)
            inputRig = GetComponentInParent<PlayerInputRig>();
    }

    private void OnEnable()
    {
        if (inputRig == null)
            return;

        // Escape unlocks the cursor whichever way this slot's rig fires it: Pause (Gameplay
        // context, opening the pause menu) or the UI map's Cancel (Menu context, closing it).
        // Kept separate from PlayerInputRig/LocalInputCoordinator's own context-swap logic - this
        // is purely "should the OS cursor be visible/free", not a gameplay concern.
        inputRig.PausePressed += OnEscapePressed;
        inputRig.MenuCancelPressed += OnEscapePressed;
    }

    private void OnDisable()
    {
        if (inputRig == null)
            return;

        inputRig.PausePressed -= OnEscapePressed;
        inputRig.MenuCancelPressed -= OnEscapePressed;
    }

    void LateUpdate()
    {
        MouseLocker();
        if (Cursor.lockState != CursorLockMode.Locked) return;
        /* SetCameraRotation(Input.GetAxis("Mouse Y") * -sensitivity, Input.GetAxis("Mouse X") * sensitivity);

        cinemachineBrain.ManualUpdate(); */

    }

    public void SetCameraRotation(float vertical, float horizontal)
    {
        if (this.enabled == false) return;

        cameraRotationValue.x += vertical * Time.deltaTime;
        cameraRotationValue.y += horizontal * Time.deltaTime;
        cameraRotationValue.x = Mathf.Clamp(cameraRotationValue.x, -maxViewAngle, maxViewAngle);

        //cameraTPVRotater.rotation = Quaternion.Euler(cameraRotationValue);
        cameraTPVRotater2.rotation = Quaternion.Euler(cameraRotationValue);
        cinemachinePOVExtension.SetCameraRotation(cameraRotationValue);
    }

    void MouseLocker()
    {
        // mouse lock - reuses the Attack action's own press edge (same physical left-mouse
        // binding the kit always used for this).
        if (inputRig != null && inputRig.AttackPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnEscapePressed()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
