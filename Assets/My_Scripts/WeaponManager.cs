using UnityEngine;

namespace My_Scripts
{
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        [Header("Weapons")]
        [SerializeField] private GunSystem _smg;
        [SerializeField] private GunSystem _rifle;

        private bool _isRifleUnlocked = false;

        private GunSystem _activeGun;
        public GunSystem ActiveGun => _activeGun;

        private PlayerControls _inputs;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return; // stop here - don't wire up input or state for the duplicate
            }

            Instance = this;

            _inputs = new PlayerControls();
            _inputs.GamePlay.Weapon1.performed += OnWeapon1Performed;
            _inputs.GamePlay.Weapon2.performed += OnWeapon2Performed;
            _inputs.GamePlay.Reload.performed += OnReloadPerformed;
        }

        private void OnEnable() => _inputs?.Enable();

        private void OnDisable() => _inputs?.Disable();

        private void OnDestroy()
        {
            if (_inputs == null) return;

            _inputs.GamePlay.Weapon1.performed -= OnWeapon1Performed;
            _inputs.GamePlay.Weapon2.performed -= OnWeapon2Performed;
            _inputs.GamePlay.Reload.performed -= OnReloadPerformed;
        }

        private void Start()
        {
            SelectWeapon(_smg);
        }

        private void OnWeapon1Performed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            SelectWeapon(_smg);
        }

        private void OnWeapon2Performed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (_isRifleUnlocked) SelectWeapon(_rifle);
        }

        private void OnReloadPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            _activeGun?.StartReloading();
        }

        private void SelectWeapon(GunSystem newWeapon)
        {
            if (newWeapon == null || newWeapon == _activeGun) return;

            if (_smg != null) _smg.gameObject.SetActive(false);
            if (_rifle != null) _rifle.gameObject.SetActive(false);

            _activeGun = newWeapon;
            _activeGun.gameObject.SetActive(true);

            UIManager.Instance?.UpdateAmmoUI(_activeGun.CurrentAmmo, _activeGun.MagazineSize);
        }

        public void UnlockRifle()
        {
            _isRifleUnlocked = true;
            SelectWeapon(_rifle);
        }
    }
}