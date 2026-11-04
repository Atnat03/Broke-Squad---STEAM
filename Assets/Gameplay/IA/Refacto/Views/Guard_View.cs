using System;
using System.Collections;
using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.IA.Refacto.Views
{
    public class Guard_View : MonoBusListener
    {
        [Header("References")]
        [SerializeField] private GuardBrain _brain;
        [SerializeField] private GuardLineSight _lineSight;
        [SerializeField] private GuardHealth _health;
        [SerializeField] private Animator _animator;
        
        [Header("UI")]
        [Header("Player Detection")]
        [SerializeField] private Image _playerDetectionImage;
        [SerializeField] private Color _playerDetectionColor = Color.grey;
        [SerializeField] private Color _playerHasBeenSeenColor = Color.red;
        
        [Header("HP")]
        [SerializeField] private Image _hpBarImage;
        [SerializeField] private Material _deadMaterial;
        
        [Header("Alerted")]
        [SerializeField] private SkinnedMeshRenderer _meshRenderer;
        [SerializeField] private Material _alertedMaterial;
        private Material _normalMaterial;

        [Header("LockDown")] 
        [SerializeField] private Image _lockDownTimerImage;
        
        [Header("Stunning")]
        [SerializeField] private Material _stunningMaterial;

        private void Awake()
        {
            if(_meshRenderer)
                _normalMaterial = _meshRenderer.material;

            _playerDetectionImage.gameObject.SetActive(false);
            _lockDownTimerImage.transform.parent.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _lineSight.OnDetectionProgressChanged += OnUpdateDetectionPlayer;
            
            _health.OnHealthUpdate += UpdateHP;
            _health.OnGuardStunning += UpdateStun;
            _health.OnGuardDie += UpdateDeath;
            
            _brain.OnAlertedChange += UpdateAlertedState;
            _brain.OnUpdateLockDownTimer += UpdateLockDownTimer;
            _brain.OnAttacking += PerformAttacking;
            _brain.OnGuardIsAggro += UpdateAggro;
        }
        private void OnDisable()
        {
            _lineSight.OnDetectionProgressChanged += OnUpdateDetectionPlayer;
            
            _health.OnHealthUpdate -= UpdateHP;
            _health.OnGuardStunning -= UpdateStun;
            _health.OnGuardDie -= UpdateDeath;
            
            _brain.OnAlertedChange -= UpdateAlertedState;
            _brain.OnUpdateLockDownTimer -= UpdateLockDownTimer;
            _brain.OnAttacking -= PerformAttacking;
            _brain.OnGuardIsAggro -= UpdateAggro;
        }

        private void OnUpdateDetectionPlayer(float ratio)
        {
            _playerDetectionImage.gameObject.SetActive(ratio > 0.05f);
            
            _playerDetectionImage.color = ratio < 0.99f ? _playerDetectionColor : _playerHasBeenSeenColor;
            
            _playerDetectionImage.fillAmount = ratio;
        }
        
        private void UpdateHP(float ratio)
        {
            _hpBarImage.fillAmount = ratio;
        }
        
        private void UpdateAlertedState(bool isAlerted)
        {
            _meshRenderer.material = isAlerted ? _alertedMaterial : _normalMaterial;
        }
        
        private void UpdateLockDownTimer(float ratio)
        {
            _lockDownTimerImage.transform.parent.gameObject.SetActive(ratio is > 0.05f and < 0.99f);
            _lockDownTimerImage.fillAmount = ratio;
        }
        
        private void PerformAttacking()
        {
            _animator.SetTrigger("Attacking");
        }
        
        private void UpdateStun(bool isStun)
        {
            _animator.SetBool("Stunning", isStun);
            _meshRenderer.material = isStun ? _stunningMaterial : _normalMaterial;
        }
        
        private void UpdateDeath(bool isDead)
        {
            _animator.SetTrigger("Dead");
            _meshRenderer.material = _deadMaterial;
        }
        
        private void UpdateAggro(bool isAggro)
        {
            _animator.SetBool("Aggro", isAggro);
        }
    }
}