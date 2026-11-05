using UnityEngine;

namespace Gameplay.IA
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
        
        [Header("Detection Suspect Object")]
        [SerializeField] public float DetectionSuspectLevel = 2f;
        [SerializeField] public float DetectionSuspectSpeed = 2f;
        [SerializeField] public float ForgettingSuspectSpeed = 1f;
        [SerializeField] public LayerMask TargetSuspectMask;
        
        [Header("Aggro")]
        [SerializeField] public float AggroSpeed = 1f;
        [SerializeField] public float AggroCloseRadius = 3f;
        [SerializeField] public float AggroDamageMemory = 4f;
        [SerializeField] public float TimeBeforeLockDownStart = 5f;
        
        [Header("Investigate")]
        [SerializeField] public float InvestigateSpeed = 1f;
        [SerializeField] public float MiniDistanceToStartInvestigate = 1f;
        [SerializeField] public float TimeBeforeInvestigateScan = 3f;
        [SerializeField] public float TimeBeforeCanInvestigateAgain = 10f;
        [SerializeField] public float RadiusInvestigate = 2f;
        [SerializeField, Range(0f, 360f)] public float AngleInvestigate = 90f;
        
        [Header("Damage")]
        [SerializeField] public float Damage = 10f;
        [SerializeField] public float AttackCooldown = 1f;
        [SerializeField] public float AttackRange = 2f;
        
    }
}