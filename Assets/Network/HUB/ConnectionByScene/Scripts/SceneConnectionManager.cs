using System;
using UnityEngine;

public class SceneConnectionManager : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(Application.isEditor);
    }
}
