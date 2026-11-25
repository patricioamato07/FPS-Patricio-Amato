using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WeaponSlot { Pistol = 0, Rifle = 1, Sniper = 2 }

public class Gun : MonoBehaviour
{
    [Header("Slot")]
    public WeaponSlot weaponSlot = WeaponSlot.Pistol;
    [Tooltip("true: dispara mientras mantienes el botón. false: un disparo por clic.")]
    public bool automatic = true;

    [Header("Estadísticas")]
    public float reloadTime = 1f;
    [Tooltip("Segundos entre disparos.")]
    public float fireRate = 0.15f;
    public int magSize = 20;

    [Header("Referencias")]
    public GameObject Bullet;
    public Transform BulletSpawnPoint;
    public GameObject weaponFlash;
    public GameObject droppedWeapon;

    [Header("Retroceso")]
    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;

    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    public int CurrentAmmo => currentAmmo;
    public int MagSize => magSize;
    public bool IsReloading => isReloading;

    public event Action<Gun> OnAmmoChanged;
    private bool warnedMissingFlash = false;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new(66, 50, 50);

    protected Transform ownerRoot;
    private static readonly List<Collider> ownerColliders = new List<Collider>();

    protected virtual void Awake()
    {
        currentAmmo = magSize;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        isReloading = false;
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
    }

    public void Initialize(Transform owner)
    {
        ownerRoot = owner;
    }

    public void SetAmmo(int value)
    {
        currentAmmo = Mathf.Clamp(value, 0, magSize);
        OnAmmoChanged?.Invoke(this);
    }

    public void Shoot()
    {
        if (isReloading) return;
        if (Time.time < nextTimeToFire) return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        nextTimeToFire = Time.time + fireRate;
        currentAmmo--;
        OnAmmoChanged?.Invoke(this);

        Fire();

        if (weaponFlash != null && BulletSpawnPoint != null)
        {
            Instantiate(weaponFlash, BulletSpawnPoint.position, BulletSpawnPoint.rotation);
        }
        else if (!warnedMissingFlash)
        {
            warnedMissingFlash = true;
            Debug.LogWarning($"{name}: no se genera el flash porque falta Weapon Flash o Bullet Spawn Point.", this);
        }

        StopCoroutine(nameof(Recoil));
        StartCoroutine(nameof(Recoil));
    }

    protected virtual void Fire()
    {
        if (Bullet == null || BulletSpawnPoint == null) return;

        GameObject bullet = Instantiate(Bullet, BulletSpawnPoint.position, BulletSpawnPoint.rotation);
        IgnoreOwnerCollisions(bullet);
    }

    private void IgnoreOwnerCollisions(GameObject projectile)
    {
        if (ownerRoot == null) return;

        ownerRoot.GetComponentsInChildren(false, ownerColliders);
        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();

        foreach (Collider p in projectileColliders)
        {
            foreach (Collider o in ownerColliders)
            {
                Physics.IgnoreCollision(p, o, true);
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        OnAmmoChanged?.Invoke(this);

        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);
        float halfReload = reloadTime / 2f;
        float t = 0f;

        while (t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t / halfReload);
            yield return null;
        }

        t = 0f;

        while (t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t / halfReload);
            yield return null;
        }

        currentAmmo = magSize;
        isReloading = false;
        OnAmmoChanged?.Invoke(this);
    }

    public void TryReload()
    {
        if (isReloading) return;
        if (currentAmmo == magSize) return;

        StartCoroutine(Reload());
    }

    private IEnumerator Recoil()
    {
        Vector3 recoilTarget = initialPosition + new Vector3(0, 0, recoilDistance);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(initialPosition, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(recoilTarget, initialPosition, t);
            yield return null;
        }

        transform.localPosition = initialPosition;
    }

    public void Drop()
    {
        GameObject dropped = Instantiate(droppedWeapon, transform.position, transform.rotation);

        if (dropped.TryGetComponent(out WeaponPickup pickup))
        {
            pickup.SetAmmo(currentAmmo);
        }

        Destroy(gameObject);
    }
}
