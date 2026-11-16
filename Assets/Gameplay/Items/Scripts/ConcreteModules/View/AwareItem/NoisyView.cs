using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class NoisyView : AwareItemView
    {
        [SerializeField] private string _eventKey = "NOISY_EVENT";

        [Header("References")]
        [SerializeField] private AudioSource _source;

        private void OnEnable() => ListenToEvent<OnModuleDoAction_EVENT>(UpdateSound);

        private void UpdateSound(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _eventKey) return;
            if (data.HostId != GetHostId()) return;

            if (data.ValueB)
            {
                if (!_source.isPlaying)
                {
                    if (_source.clip != null)
                        _source.time = data.ValueF % _source.clip.length;
                    _source.Play();
                }
            }
            else
            {
                _source.Stop();
            }
        }
    }
}