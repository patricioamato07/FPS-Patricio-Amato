using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerShooting : MonoBehaviour
{
    public const int SlotCount = 3; // 0 = Pistol (1 Key), 1 = Rifle (2 Key), 2 = Sniper (3 Key)

    [FormerlySerializedAs("gun")]
    public Gun startingGun;
    public Transform gunHolder;

    public event System.Action<Gun> OnGunChanged;

    private readonly Gun[] slots = new Gun[SlotCount];
    private int currentSlot = -1;
    private bool isHoldingShoot = false;
    private bool shootPressedThisFrame = false;
    private Transform ownerRoot;
    private PlayerHealth health;

    private bool IsPlayerDead => health != null && health.IsDead;

    public Gun CurrentGun => currentSlot >= 0 ? slots[currentSlot] : null;

    void Awake()
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        ownerRoot = rb != null ? rb.transform : transform.root;
        health = GetComponentInParent<PlayerHealth>();
    }

    void OnEnable()
    {
        isHoldingShoot = false;
        shootPressedThisFrame = false;
    }

    void Start()
    {
        if (startingGun == null) return;

        Gun g = startingGun.gameObject.scene.IsValid()
            ? startingGun
            : Instantiate(startingGun, gunHolder);

        Store(g, -1);
    }

    void OnShoot()
    {
        if (!enabled || IsPlayerDead) return;
        isHoldingShoot = true;
        shootPressedThisFrame = true;
    }

    void OnShootRelease()
    {
        isHoldingShoot = false;
    }

    void OnReload()
    {
        if (!enabled || IsPlayerDead) return;
        Gun g = CurrentGun;
        if (g != null) g.TryReload();
    }

    public void OnDrop()
    {
        if (!enabled || IsPlayerDead) return;
        Gun g = CurrentGun;
        if (g == null) return;

        slots[currentSlot] = null;
        currentSlot = -1;
        g.Drop();

        EquipFirstOccupied();
        OnGunChanged?.Invoke(CurrentGun);
    }

    void Update()
    {
        if (IsPlayerDead)
        {
            shootPressedThisFrame = false;
            return;
        }

        HandleWeaponSwitching();

        Gun g = CurrentGun;
        if (g != null)
        {
            bool fire = g.automatic ? isHoldingShoot : shootPressedThisFrame;
            if (fire) g.Shoot();
        }

        shootPressedThisFrame = false;
    }

    private void HandleWeaponSwitching()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) EquipSlot((int)WeaponSlot.Pistol);
            if (keyboard.digit2Key.wasPressedThisFrame) EquipSlot((int)WeaponSlot.Rifle);
            if (keyboard.digit3Key.wasPressedThisFrame) EquipSlot((int)WeaponSlot.Sniper);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                CycleSlot(scroll > 0f ? -1 : 1);
            }
        }
    }

    public bool TryPickUp(Gun gunPrefab, int ammo)
    {
        if (gunPrefab == null) return false;

        int index = (int)gunPrefab.weaponSlot;
        if (index < 0 || index >= SlotCount) return false;
        if (slots[index] != null) return false;

        Gun g = Instantiate(gunPrefab, gunHolder);
        return Store(g, ammo);
    }

    private bool Store(Gun g, int ammo)
    {
        int index = (int)g.weaponSlot;
        if (index < 0 || index >= SlotCount) return false;
        if (slots[index] != null) return false;

        slots[index] = g;
        g.Initialize(ownerRoot);
        if (ammo >= 0) g.SetAmmo(ammo);

        EquipSlot(index);
        return true;
    }

    private void EquipSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        if (slots[index] == null) return;
        if (index == currentSlot) return;

        if (currentSlot >= 0 && slots[currentSlot] != null)
        {
            slots[currentSlot].gameObject.SetActive(false);
        }

        currentSlot = index;
        slots[currentSlot].gameObject.SetActive(true);
        OnGunChanged?.Invoke(slots[currentSlot]);
    }

    private void CycleSlot(int direction)
    {
        if (currentSlot < 0) return;

        for (int step = 1; step <= SlotCount; step++)
        {
            int i = ((currentSlot + direction * step) % SlotCount + SlotCount) % SlotCount;
            if (slots[i] != null)
            {
                EquipSlot(i);
                return;
            }
        }
    }

    private void EquipFirstOccupied()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null)
            {
                EquipSlot(i);
                return;
            }
        }
    }
}
