using Fate.Systems.Game;
using UnityEngine;

public class ViewingResistance : MonoBehaviour
{
    public WeaponController weaponController;
    public EventsCenter eventsCenter;
    public Transform pivot;
    public float resistanceForce;
    public float resistanceSmoothing;

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
        eventsCenter.OnWeaponChange += WeaponChangeCheck;
    }
    private void OnDisable()
    {
        eventsCenter.OnWeaponChange -= WeaponChangeCheck;
    }

    void WeaponChangeCheck(bool changing)
    {
        if (!changing)
        {
            resistanceForce = weaponController.GETCurrentWeapon.resistanceForce;
            resistanceSmoothing = weaponController.GETCurrentWeapon.resistanceSmoothing;
        }
    }

    private void Update()
    {
        Vector2 look = inputRig.Look;

        pivot.localRotation = Quaternion.Lerp(
            pivot.localRotation,
            Quaternion.Euler(
                -look.y * resistanceForce,
                look.x * resistanceForce,
                0),
                resistanceSmoothing * Time.deltaTime);
    }
}
