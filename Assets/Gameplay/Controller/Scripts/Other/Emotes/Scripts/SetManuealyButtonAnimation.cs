using UnityEngine;

public class SetManuealyButtonAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    
    public void OnNormal() => _animator.SetTrigger("Normal");
    public void OnPressed() => _animator.SetTrigger("Pressed");
    public void OnHighlight() => _animator.SetTrigger("Highlighted");
}
