using UnityEngine;

namespace My_Scripts
{
    public class GunSystem : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private float _damage = 10f;
        [SerializeField] private float _fireRate = 0.1f; // Delay between shots
        [SerializeField] private int _magazineSize = 30;
        [SerializeField] private float _range = 100f;

        [Header("Physics Settings")] [SerializeField]
        private float _impactForce = 15f;

        [Header("Effect Lifetimes")] [SerializeField]
        private float _muzzleFlashLifetime = 1f;

        [SerializeField] private float _impactEffectLifetime = 1f;

        [Header("References")] [SerializeField]
        private Transform _firePoint;

        [SerializeField] private Animator _fpsAnimator;

        [SerializeField] private Transform _muzzlePoint;

        // Layers the raycast should HIT. Anything not on this mask (e.g. the player) is ignored.
        [SerializeField] private LayerMask _hittableLayers;

        [Header("Visual Prefabs")] [SerializeField]
        private GameObject _muzzleFlash;

        [SerializeField] private GameObject _impactEffect;

        private int _currentAmmo;
        private float _nextTimeToFire;
        private bool _isReloading;
        private PlayerControls _inputs;

        public int CurrentAmmo => _currentAmmo;
        public int MagazineSize => _magazineSize;
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            _currentAmmo = _magazineSize;
            _inputs = new PlayerControls();
        }

        private void OnEnable() => _inputs.Enable();

        private void OnDisable() => _inputs.Disable();

        private void Start()
        {
            UpdateAmmoUI();
        }

        private void Update()
        {
            // Game over / victory / pause: clicking a UI button must not also fire the gun.
            if (Time.timeScale == 0f) return;

            bool canFire = !_isReloading && Time.time >= _nextTimeToFire && _currentAmmo > 0;
            bool wantsToFire = _inputs.GamePlay.Shoot.triggered && canFire;

            if (wantsToFire)
            {
                _nextTimeToFire = Time.time + _fireRate;
                Shoot();
            }

            _fpsAnimator.SetBool("isFiring", wantsToFire);
        }

        private void Shoot()
        {
            SpawnMuzzleFlash();

            _currentAmmo--;
            UpdateAmmoUI();

            // NOTE: previously this passed a mask of layers to EXCLUDE, but Raycast's
            // layerMask parameter is inclusive - it only hits layers listed in it.
            // Fixed by hitting everything except the excluded layers via the inverted mask
            // baked into _hittableLayers (set it in the Inspector to "everything but Player").
            bool hitSomething = Physics.Raycast(
                _firePoint.position,
                _firePoint.forward,
                out RaycastHit hit,
                _range,
                _hittableLayers);

            if (!hitSomething) return;

            ApplyDamage(hit);
            SpawnImpactEffect(hit);
            ApplyImpactForce(hit);
        }

        private void ApplyDamage(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(_damage);
        }

        private void SpawnMuzzleFlash()
        {
            if (_muzzleFlash == null) return;

            GameObject muzzleInstance =
                Instantiate(_muzzleFlash, _muzzlePoint.position, _muzzlePoint.rotation, _muzzlePoint);
            Destroy(muzzleInstance, _muzzleFlashLifetime);
        }

        private void SpawnImpactEffect(RaycastHit hit)
        {
            if (_impactEffect == null) return;

            GameObject impactInstance = Instantiate(_impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impactInstance, _impactEffectLifetime);
        }

        private void ApplyImpactForce(RaycastHit hit)
        {
            if (hit.collider.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(_firePoint.forward * _impactForce, hit.point, ForceMode.Impulse);
            }
        }

        public void StartReloading()
        {
            // Guard against re-triggering the reload animation/logic mid-reload
            // or topping off an already-full magazine.
            if (_isReloading || _currentAmmo >= _magazineSize) return;

            _isReloading = true;
            _fpsAnimator.SetTrigger("onReload");
        }

        public void FinishReloading()
        {
            _currentAmmo = _magazineSize;
            _isReloading = false;
            UpdateAmmoUI();
        }

        private void UpdateAmmoUI()
        {
            UIManager.Instance?.UpdateAmmoUI(_currentAmmo, _magazineSize);
        }
    }
}