using Bus;
using TMPro;
using UnityEngine;

public struct FPSCounterChangeVisual_EVENT
{
    public bool show;
};

namespace Gameplay.UI.Scripts
{
    
    
    public class FPSCounter : MonoBusListener
    {
        [SerializeField] TextMeshProUGUI text;

        [SerializeField] Color badColor     = Color.red;
        [SerializeField] Color neutralColor = Color.yellow;
        [SerializeField] Color goodColor    = Color.cyan;

        [SerializeField] float badValue     = 50;
        [SerializeField] float neutralValue = 60;

        [SerializeField] float fps;

        const float updateInterval = 0.1f;

        float frames;
        float timeLeft;
        float accum;

        void Awake()
        {
            ListenToEvent<FPSCounterChangeVisual_EVENT>(ChangeFPSCounterVisual);
            text.gameObject.SetActive(false);
        }
        
        void ChangeFPSCounterVisual(FPSCounterChangeVisual_EVENT evt)
        {
            text.gameObject.SetActive(evt.show);
        }
        
        void Update()
        {
            timeLeft -= Time.deltaTime;
            ++frames;
            accum += Time.timeScale / Time.deltaTime;

            if (timeLeft <= 0)
            {
                fps = accum / frames;

                if (fps < badValue)
                {
                    text.color = badColor;
                }
                else if (fps < neutralValue)
                {
                    text.color = neutralColor;
                }
                else
                {
                    text.color = goodColor;
                }

                text.text = fps.ToString("f1");
                timeLeft  = updateInterval;
                accum     = 0;
                frames    = 0;
            }
        }
    }
}