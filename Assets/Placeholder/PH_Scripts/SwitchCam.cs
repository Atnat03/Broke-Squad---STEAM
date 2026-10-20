using UnityEngine;

public class SwitchCam : MonoBehaviour
{
    public GameObject[] cameras;
    public GameObject[] screens;
    private int currentCam = 0;

    void Start()
    {
        for (int i = 0; i < cameras.Length; i++)
            SetCam(i, i == currentCam);
    }

    public void NextCam()
    {
        if (cameras.Length == 0) return;

        SetCam(currentCam, false);
        currentCam = (currentCam + 1) % cameras.Length;
        SetCam(currentCam, true);
    }

    void SetCam(int index, bool active)
    {
        if (cameras[index] != null) cameras[index].SetActive(active);
        if (screens != null && index < screens.Length && screens[index] != null)
            screens[index].SetActive(active);
    }
}