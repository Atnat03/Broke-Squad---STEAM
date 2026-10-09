using Bus;
using Gameplay.IA.Scripts;
using UnityEngine;

namespace Gameplay.Other.SuspiciousSound
{
    public class SuspiciousSoundFactory : NetworkBusListener
    {
        private RaycastHit[] _hits = new RaycastHit[128];
        
        public override void OnNetworkSpawn()
        {
            ListenToEvent<OnCreateSuspiciousSound_EVENT>(CreateSS);
        }

        private void CreateSS(OnCreateSuspiciousSound_EVENT data)
        {
            if (!IsServer) return;
            
            Generate(data);
        }
        
        private void Generate(OnCreateSuspiciousSound_EVENT data)
        {
            Transform player = NetworkManager.ConnectedClients[data.FromClientID].PlayerObject.transform;
            
            for (int i = 0; i < 360; i++)
            {
                float angle = i * Mathf.Deg2Rad;

                Vector3 direction = new Vector3(
                    Mathf.Sin(angle),
                    0f,
                    Mathf.Cos(angle)
                );

                direction = player.TransformDirection(direction);
                
                float f = data.Force;
                float d = data.MaxDistance;
                
                int hitCount = Physics.RaycastNonAlloc(data.Position, direction, _hits, d);

                for (int j = 0; j < hitCount; j++)
                {
                    RaycastHit hit = _hits[j];
                    Collider col = hit.collider;

                    Debug.DrawLine(data.Position, hit.point, 
                        Color.Lerp(Color.red, Color.green, 1 - hit.distance / d), 
                        3f);
                    
                    if (col.TryGetComponent(out SoundAttenuation attenuation))
                    {
                        f = attenuation.GetAttenuationForce(f);
                    }
                    
                    if (hit.transform.TryGetComponent(out TestGuard guard))
                    {
                        f = Mathf.Lerp(0, f, 1 - hit.distance / d);

                        GameObject newG = new GameObject
                        {
                            transform =
                            {
                                position = data.Position
                            }
                        };

                        guard.TriggerBySound(f, newG.transform);
                        
                        //Destroy(newG);
                    }
                }
            }
            
            _hits = new RaycastHit[128];
        }
    }
}