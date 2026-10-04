using UnityEngine;

namespace My_Scripts
{
    public class GunSystem : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _fireRate = 0.1f; // Delay between shots
        [SerializeField] private int _magazineSize = 30;
        [SerializeField] private float _range = 100f;

        [Header("Physics Settings")] 
        [SerializeField] private float _impactForce = 15f;

        [Header("Effect Lifetimes")] 
        [SerializeField] private float _muzzleFlashLifetime = 1f;

        [SerializeField] private float _impactEffectLifetime = 1f;

        [Header("References")] 
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Animator _fpsAnimator;
        [SerializeField] private Transform _muzzlePoint;

        // Layers the raycast should HIT. Anything not on this mask (e.g. the player) is ignored.
        [SerializeField] private LayerMask _hittableLayers;

        [Header("Visual Prefabs")] 
        [SerializeField] private GameObject _muzzleFlash;
        [SerializeField] private GameObject _impactEffect;
        
        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _shootSound;
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private AudioClip _emptySound; // played when Shoot is triggered with 0 ammo

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
            if (Time.timeScale == 0f) return;
 
            bool canFire = !_isReloading && Time.time >= _nextTimeToFire && _currentAmmo > 0;
            bool wantsToFire = _inputs.GamePlay.Shoot.triggered && canFire;
 
            if (wantsToFire)
            {
                _nextTimeToFire = Time.time + _fireRate;
                Shoot();
            }
            else if (_inputs.GamePlay.Shoot.triggered && !_isReloading && _currentAmmo <= 0)
            {
                PlaySound(_emptySound); // dry-fire click so an empty mag isn't silent
            }
 
            _fpsAnimator.SetBool("isFiring", wantsToFire);
        }

        private void Shoot()
        {
            
            SpawnMuzzleFlash();
            PlaySound(_shootSound);
 
            _currentAmmo--;
            UpdateAmmoUI();

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
            PlaySound(_reloadSound);
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
        
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}