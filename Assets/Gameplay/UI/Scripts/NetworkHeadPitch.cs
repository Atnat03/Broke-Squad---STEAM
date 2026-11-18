using Unity.Netcode;
using UnityEngine;
using Gameplay.Controller;

namespace Gameplay.UI.Scripts
{
    public class NetworkHeadPitch : NetworkBehaviour
{
    [Header("References")]
    public PlayerCamera playerCamera;
    public Transform body;
    public Transform neckBone;
    public Transform headBone;
    
    [Header("Head Pitch")]
    [Range(-90, 0)] public float minPitch = -60f;
    [Range(0, 90)] public float maxPitch = 60f;
    [Range(0, 1)] public float neckShare = 0.4f;
    public float remoteSmooth = 15f;

    [Header("Lean")]
    public Transform[] spineBones;
    public float visualLeanAngle = 25f;
    public float leanRemoteSmooth = 15f;

    readonly NetworkVariable<float> _netPitch = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<float> _netLean = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    float _current;
    float _currentLean;

    public override void OnNetworkSpawn()
    {
        if (playerCamera) playerCamera.enabled = IsOwner;
    }

    void Update()
    {
        if (IsOwner && playerCamera != null)
        {
            _netPitch.Value = playerCamera.Pitch;
            _netLean.Value = playerCamera.LeanValue;
        }
    }

    void LateUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            if (playerCamera == null) return;
            _current = playerCamera.Pitch;
            _currentLean = playerCamera.LeanValue;
        }
        else
        {
            float k = 1f - Mathf.Exp(-remoteSmooth * Time.deltaTime);
            _current = Mathf.Lerp(_current, _netPitch.Value, k);

            float kl = 1f - Mathf.Exp(-leanRemoteSmooth * Time.deltaTime);
            _currentLean = Mathf.Lerp(_currentLean, _netLean.Value, kl);
        }
        
        if (spineBones != null && spineBones.Length > 0)
        {
            float share = -_currentLean * visualLeanAngle / spineBones.Length;
            Quaternion leanRot = Quaternion.AngleAxis(share, body.forward);

            foreach (var bone in spineBones)
            {
                if (bone) bone.rotation = leanRot * bone.rotation;
            }
                
        }
        
        float pitch = Mathf.Clamp(_current, minPitch, maxPitch);

        Quaternion neckRot = Quaternion.AngleAxis(pitch * neckShare, body.right);
        Quaternion headRot = Quaternion.AngleAxis(pitch * (1f - neckShare), body.right);

        if (neckBone) neckBone.rotation = neckRot * neckBone.rotation;
        if (headBone) headBone.rotation = headRot * headBone.rotation;
    }
}
}