using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace My_Scripts
{
    public class IntroSequence : MonoBehaviour
    {
        [Header("GameObjects")]
        [SerializeField] private GameObject[] _parasiteTargets;
        [SerializeField] private GameObject _create1;
        [SerializeField] private GameObject _create2;

        [Header("Player Components")]
        [SerializeField] private MonoBehaviour _playerMovementScript; // Скрипт ходьбы
        [SerializeField] private MonoBehaviour _cameraLookScript;
    
        [Header("Playable Director")]
        [SerializeField] private PlayableDirector _playableDirector;

        private void Start()
        {
            TogglePlayerControl(false);
            PlayIntro2(this.GetCancellationTokenOnDestroy()).Forget();
        }
    
        private async UniTaskVoid PlayIntro2(CancellationToken ct)
        {
            if (_playableDirector != null)
            {
                _playableDirector.Play();
            
                await UniTask.WaitUntil(() => _playableDirector.state != PlayState.Playing, cancellationToken: ct);
            
                foreach (var parasite in _parasiteTargets)
                {
                    Destroy(parasite);
                }
            }
        
            TogglePlayerControl(true);
        }
    
        private void TogglePlayerControl(bool state)
        {
            if (_playerMovementScript != null) _playerMovementScript.enabled = state;
            if (_cameraLookScript != null) _cameraLookScript.enabled = state;
            _create1.SetActive(state);
            _create2.SetActive(state);
        }
    }
}