using UnityEngine;

public class ForwardBackwardMovement : StateMachineBehaviour
{
    [Header("Animator Parameter Names")]
    public string directionIntParam = "MoveDirection";
    public string speedMultiplierParam = "WalkSpeed";
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int direction = animator.GetInteger(directionIntParam);
        float targetSpeed = (direction < 0) ? -1.0f : 1.0f;
        animator.SetFloat(speedMultiplierParam, targetSpeed);
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetFloat(speedMultiplierParam, 1.0f);
    }
}
