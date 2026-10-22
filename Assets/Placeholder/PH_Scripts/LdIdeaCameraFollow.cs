using UnityEngine;

public class LdIdeaCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -3f);
    [SerializeField] private float smoothTime = 0.15f;
 
    private Vector3 velocity;
 
    private void LateUpdate()
    {
        if (target == null) return;
 
        Vector3 goal = new Vector3(target.position.x + offset.x, transform.position.y, target.position.z + offset.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}