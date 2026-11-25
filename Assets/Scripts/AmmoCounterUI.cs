using UnityEngine;
using TMPro;

public class AmmoCounterUI : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private TMP_Text ammoText;

    [Header("Appearance")]
    [SerializeField] private string reloadingLabel = "Reloading";
    [SerializeField] private string noWeaponLabel = "--";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField, Range(0f, 1f)] private float lowAmmoThreshold = 0.25f;

    private Gun currentGun;

    private void Awake()
    {
        if (shooting == null) shooting = FindFirstObjectByType<PlayerShooting>();
    }

    private void OnEnable()
    {
        if (shooting != null) shooting.OnGunChanged += SetGun;
    }

    private void OnDisable()
    {
        if (shooting != null) shooting.OnGunChanged -= SetGun;
        if (currentGun != null) currentGun.OnAmmoChanged -= Refresh;
    }

    private void Start()
    {
        if (shooting == null)
        {
            Debug.LogWarning("AmmoCounterUI: No PlayerShooting Script found", this);
            return;
        }

        SetGun(shooting.CurrentGun);
    }

    private void SetGun(Gun gun)
    {
        if (currentGun != null) currentGun.OnAmmoChanged -= Refresh;

        currentGun = gun;

        if (currentGun != null) currentGun.OnAmmoChanged += Refresh;

        Refresh();
    }

    private void Refresh(Gun _ = null)
    {
        if (ammoText == null) return;

        if (currentGun == null)
        {
            ammoText.text = noWeaponLabel;
            ammoText.color = normalColor;
            return;
        }

        if (currentGun.IsReloading)
        {
            ammoText.text = reloadingLabel;
            ammoText.color = lowAmmoColor;
            return;
        }

        ammoText.text = $"{currentGun.CurrentAmmo} / {currentGun.MagSize}";

        float ratio = currentGun.MagSize > 0 ? (float)currentGun.CurrentAmmo / currentGun.MagSize : 0f;
        ammoText.color = ratio <= lowAmmoThreshold ? lowAmmoColor : normalColor;
    }
}
