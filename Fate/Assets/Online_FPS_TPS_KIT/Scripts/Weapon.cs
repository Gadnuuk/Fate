using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class Weapon : MonoBehaviour
{
    public enum SlotType
    {
        rifle = 1,
        smg = 2,
        pistol = 3
    }
    public SlotType slotType;
    public int playerDamage = 10;
    //public int slotType; // (1: two slots in the back) (2: chest slot) (3: pistol slot)
    public float shotTemp; // 0 - fast 1 - slow
    private bool _canShoot = true;
    public bool singleShoot; // only single shoot?

    [Header("shotgun parameters")]
    public bool shotgun;
    public int bulletAmount;
    public float accuracy = 1;

    [Header("Components")]
    public Transform aimPoint;
    public GameObject muzzleFlash;
    public GameObject casingPrefab;
    public Transform casingSpawnPoint;
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletForce;
    public float bulletStartSpeed;

    [Header("position and points")]
    public Vector3 inHandsPositionOffset; // offset in hands
    public WeaponPoint[] weaponPoints;
    public List<WeaponSight> weaponSights;

    [Header("View resistance")]
    public float resistanceForce; // view offset rotation
    public float resistanceSmoothing; // view offset rotation speed
    public float collisionDetectionLength;
    public float maxZPositionOffsetCollision;

    [Header("Recoil Parameters")]
    public RecoilParametersModel recoilParametersModel = new RecoilParametersModel();

    [Header("Sound")]
    public AudioClip fireSound;
    private AudioSource _audioSource;
    private BoltAnimation boltAnimation;



    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        boltAnimation = GetComponent<BoltAnimation>();
    }

#if UNITY_EDITOR
    //Dictionary<Vector3, float> hits = new Dictionary<Vector3, float>();
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;

        //Dictionary<Vector3, float> cachedHits = new Dictionary<Vector3, float>(hits);
        //foreach(var keyVal in cachedHits)
        //{
        //    if(Time.realtimeSinceStartup - keyVal.Value > 10)
        //    {
        //        hits.Remove(keyVal.Key);
        //    }
        //    else
        //    {
        //        Gizmos.DrawSphere(keyVal.Key, 0.1f);
        //    }
        //}
    }
#endif

    public bool Shoot()
    {
        if (!_canShoot) return false;
        _canShoot = false;

        if (shotgun)
        {
            for (int i = 0; i < bulletAmount; i++)
            {
                Quaternion bulletSpawnDirection = Quaternion.Euler(bulletSpawnPoint.rotation.eulerAngles + new Vector3(Random.Range(-accuracy, accuracy), Random.Range(-accuracy, accuracy), 0));
                float bulletSpeed = Random.Range(bulletStartSpeed * 0.8f, bulletStartSpeed);
                BulletSpawn(bulletStartSpeed, bulletSpawnDirection);
            }
        }
        else
        {
            // Aaron's Changes, fire a ray cast, if you hit anything, fire the bullet from the spawn point to the hit point
            // TODO: update raycast to use proper layers
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Quaternion spawnRotation = Quaternion.identity;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                Vector3 direction = hit.point - bulletSpawnPoint.position;
                spawnRotation = Quaternion.LookRotation(direction.normalized);

//#if UNITY_EDITOR
//                hits.Add(hit.point, Time.realtimeSinceStartup);
//#endif
            }
            else
            {
                spawnRotation = bulletSpawnPoint.rotation;
            }

            BulletSpawn(bulletStartSpeed, spawnRotation);
        }

        CasingSpaw();

        MuzzleFlashSpawn();

        if (fireSound) _audioSource.PlayOneShot(fireSound);

        if (boltAnimation) boltAnimation.StartAnim(0.05f);
        StartCoroutine(ShootPause());

        return true;
    }

    private IEnumerator ShootPause()
    {
        yield return new WaitForSeconds(shotTemp);
        _canShoot = true;
    }

    private void BulletSpawn(float startSpeed, Quaternion bulletDirection)
    {
        GameObject bulletGO = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletDirection);
        var bulletComponent = bulletGO.GetComponent<BulletBehaviour>();
        bulletComponent.BulletStart(transform);
    }

    private void MuzzleFlashSpawn()
    {
        var muzzleSpawn = Instantiate(muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Destroy(muzzleSpawn, 0.5f);
    }

    private void CasingSpaw()
    {
        if (casingPrefab)
        {
            //Spawn casing
            var cas = Instantiate(casingPrefab, casingSpawnPoint.transform.position, Random.rotation);

            cas.GetComponent<Rigidbody>().AddForce(casingSpawnPoint.transform.forward * 55 + new Vector3(
                Random.Range(-20, 40),
                Random.Range(-20, 40),
                Random.Range(-20, 40)));
            Destroy(cas, 5f);
        }
    }
}
