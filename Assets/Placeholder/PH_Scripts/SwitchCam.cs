using UnityEngine;

public class SwitchCam : MonoBehaviour
{
    public GameObject[] cameras;
    private int currentCam = 0;

    public void NextCam()
    {
        if (cameras.Length == 0) return;

        cameras[currentCam].SetActive(false);

        currentCam = (currentCam + 1) % cameras.Length;

        cameras[currentCam].SetActive(true);
    }
}