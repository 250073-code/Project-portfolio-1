using UnityEngine;

namespace My_Scripts
{
    public class VanguardAnimationEvents : MonoBehaviour
    {
        // Animation event target for the Vanguard's reload clip.
        public void FinishReloading()
        {
            GunSystem activeGun = WeaponManager.Instance != null ? WeaponManager.Instance.ActiveGun : null;

            if (activeGun == null)
            {
                Debug.LogWarning($"{name}: no active gun to notify of reload finishing.");
                return;
            }

            activeGun.FinishReloading();
        }
    }
}