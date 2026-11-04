using UnityEngine;

namespace Gameplay.IA.Refacto
{
    [CreateAssetMenu(menuName = "GuardSettings")]
    public class GuardSetting_SO : ScriptableObject
    {
        [Header("Patrol")]
        [SerializeField] public float PatrolSpeed = 1f;
        [SerializeField] public float DistanceCloseToChangePoint = 2;
        
        [Header("Vision")]
        [SerializeField] public float Radius = 10f;
        [SerializeField, Range(0f, 360f)] public float Angle = 90f;
        [SerializeField] public float EyeHeight = 1.6f;
        [SerializeField] public float CheckInterval = 0.2f;
        [SerializeField] public LayerMask ObstructionMask;
        
        [Header("Detection Player")]
        [SerializeField] public float DetectionLevel = 2f;
        [SerializeField] public float DetectionSpeed = 2f;
        [SerializeField] public float ForgettingSpeed = 1f;
        [SerializeField] public LayerMask TargetPlayerMask;

        [Header("Aggro")]
        [SerializeField] public float AggroSpeed = 1f;
        [SerializeField] public float AggroCloseRadius = 3f;
        [SerializeField] public float AggroDamageMemory = 4f;
        [SerializeField] public float TimeBeforeLockDownStart = 5f;
        
        [Header("Damage")]
        [SerializeField] public float Damage = 10f;
        [SerializeField] public float AttackCooldown = 1f;
        [SerializeField] public float AttackRange = 2f;
        
    }
}