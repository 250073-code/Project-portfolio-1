using UnityEngine;

namespace My_Scripts
{
    public class DoorController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Animator[] _animators;

        [Header("Audio")]
        [SerializeField] private AudioSource[] _openSounds;
        [SerializeField] private AudioSource[] _closeSounds;

        private static readonly int HashOpen = Animator.StringToHash("Open");
        private static readonly int HashClose = Animator.StringToHash("Close");

        private bool _isOpen;

        // Counts players currently inside the trigger, in case the trigger has
        // multiple colliders/players and could otherwise fire Exit while one is still inside.
        private int _playersInside;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playersInside++;
            if (!_isOpen)
            {
                SetDoorState(open: true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playersInside = Mathf.Max(0, _playersInside - 1);
            if (_playersInside == 0 && _isOpen)
            {
                SetDoorState(open: false);
            }
        }

        private void SetDoorState(bool open)
        {
            _isOpen = open;
            int triggerHash = open ? HashOpen : HashClose;

            foreach (Animator anim in _animators)
            {
                if (anim != null) anim.SetTrigger(triggerHash);
            }

            AudioSource[] soundsToPlay = open ? _openSounds : _closeSounds;
            foreach (AudioSource source in soundsToPlay)
            {
                if (source != null) source.Play();
            }
        }
    }
}