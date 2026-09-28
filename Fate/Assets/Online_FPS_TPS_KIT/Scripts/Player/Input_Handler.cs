using System.Collections;
using System.Collections.Generic;
using Fate.Systems.Game;
using UnityEngine;

public class Input_Handler : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponPickup weaponPickUp;
    [SerializeField] private BodySlope_Handler bodySlope_Handler;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private BodyTiltInSprint bodyTiltInSprint;
    [SerializeField] private WeaponSight_hangler weaponSightHandler;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] float maxViewAngle = 80;
    [SerializeField] private float sensitivity = 150;

    [Tooltip("This instance's local input rig - see PlayerInputRig / PlayerCameraRig. Auto-resolved " +
             "from a parent if left unassigned.")]
    [SerializeField] private PlayerInputRig inputRig;

    private void Awake()
    {
        if (inputRig == null)
            inputRig = GetComponentInParent<PlayerInputRig>();
    }

    private void Start()
    {
        weaponController.activeID = 1;
        weaponController.animator.Play("GunPickUp", 1);
    }

    void Update()
    {
        TryShoot();

        bodySlope_Handler.setInput(-inputRig.Lean);

        //bodyTiltInSprint.SetMouseXMove(Input.GetAxis("Mouse X"));

        if (inputRig.WeaponSlot1Pressed)
            weaponController.ToChange(1);
        if (inputRig.WeaponSlot2Pressed)
            weaponController.ToChange(2);
        if (inputRig.WeaponSlot3Pressed)
            weaponController.ToChange(3);
        if (inputRig.WeaponSlot4Pressed)
            weaponController.ToChange(4);


        if (inputRig.InteractPressed && weaponPickUp != null)
        {
            weaponPickUp.PickupCheck();
        }

        if (inputRig.AimViewPressed)
        {
            //cameraSwitcher.AimViewChange();
            weaponSightHandler.AimViewChange();
        }
        if (inputRig.AimSightPressed)
        {
            //cameraSwitcher.AimViewChange();
            weaponSightHandler.AimSightChange();
        }

        if (inputRig.ViewChangePressed)
        {
            cameraSwitcher.ViewChange();
        }

        Vector2 look = inputRig.Look;
        cameraController.SetCameraRotation(look.y * -sensitivity, look.x * sensitivity);
    }


    void TryShoot()
    {
        bool singleshoot = weaponController.GETCurrentWeapon.singleShoot;
        if (singleshoot && inputRig.AttackPressed)
        {
            weaponController.StartShoot();
        }
        else if (!singleshoot && inputRig.AttackHeld)
        {
            weaponController.StartShoot();
        }
    }
}
