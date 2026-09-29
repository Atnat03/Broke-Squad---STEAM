using System;
using MyPrint;
using UnityEngine;

public interface IBreakable
{
    public void Break(Vector3 center, float force);
}

public class Breakable : MonoBehaviour, IBreakable
{
    [SerializeField] private GameObject _normalMesh;
    [SerializeField] private Rigidbody[] _breakMesh;

    private void Start()
    {
        _normalMesh.SetActive(true);

        foreach (var rb in _breakMesh)
            rb.gameObject.SetActive(false);
    }

    public void Break(Vector3 center, float force)
    {
        _normalMesh.SetActive(false);
        
        ABPrint.Print("Break the wall");
        
        foreach (var rb in _breakMesh)
        {
            rb.gameObject.SetActive(true);
            
            rb.AddExplosionForce(force, transform.position, transform.localScale.magnitude);
        }
    }
}
