using UnityEngine;

namespace My_Scripts
{
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        [Header("Weapons")]
        [SerializeField] private GunSystem _smg;
        [SerializeField] private GunSystem _rifle;

        public bool IsRifleUnlocked { get; private set; }

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
            if (IsRifleUnlocked) SelectWeapon(_rifle);
        }

        private void OnReloadPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            _activeGun?.StartReloading();
        }

        private void SelectWeapon(GunSystem newWeapon)
        {
            if (newWeapon == null || newWeapon == _activeGun) return;

            // Reload finishes via an Animation Event that calls FinishReloading() on
            // whatever gun is ActiveGun AT THAT MOMENT - not whichever gun actually started
            // reloading. Swapping mid-reload would leave the old gun stuck in _isReloading
            // forever (its FinishReloading() call never arrives) while the new gun spuriously
            // "finishes" a reload it never started. Simplest correct fix: disallow the swap.
            if (_activeGun != null && _activeGun.IsReloading) return;

            if (_smg != null) _smg.gameObject.SetActive(false);
            if (_rifle != null) _rifle.gameObject.SetActive(false);

            _activeGun = newWeapon;
            _activeGun.gameObject.SetActive(true);

            UIManager.Instance?.UpdateAmmoUI(_activeGun.CurrentAmmo, _activeGun.MagazineSize);
        }

        public void UnlockRifle()
        {
            IsRifleUnlocked = true;
            SelectWeapon(_rifle);
        }
    }
}