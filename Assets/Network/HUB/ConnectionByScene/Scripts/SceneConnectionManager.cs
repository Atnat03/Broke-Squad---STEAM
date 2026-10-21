using System;
using UnityEngine;


#if UNITY_EDITOR

public class SceneConnectionManager : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(true);
    }
}

#endif
