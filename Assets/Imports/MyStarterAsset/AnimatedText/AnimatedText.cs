using TMPro;
using UnityEngine;

public enum TextAnimationType 
{Writing, Floating, Shaking, 
    ColorPulse, ColorFlash, RainbowWave, GradientReveal,
    ReadingHighlight, MovingGradient, 
}

public interface ITextEffect
{
    void Effect();
}

public class AnimatedText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    [SerializeField] private TextAnimationType textAnimationType;
    [SerializeField] private bool triggerEffectWhenEnabled = true;

    private ITextEffect currentEffect;
    
    //[Header("Writing")]
    [SerializeField] private float duration_Writing = 0.05f;
    
    //[Header("Floating")]
    [SerializeField] private float amplitude_Floating = 5f;
    [SerializeField] private float speed_Floating = 2f;
    
    
    //[Header("Shake")]
    [SerializeField] private bool loopShake = true;
    [SerializeField] private float intensity_Shake = 5f;
    [SerializeField] private float duration_Shake = 0.3f;
    
    //[Header("ColorPulse")]
    [SerializeField] private Color colorA_ColorPulse = Color.white;
    [SerializeField] private Color colorB_ColorPulse = Color.red;
    [SerializeField] private float speed_ColorPulse = 2f;
    
    //[Header("RainbowWave")]
    [SerializeField] private float speed_RainbowWave = 2f;
    [SerializeField] private float hueOffset_RainbowWave = 0.1f;

    //[Header("GradientReveal")] 
    [SerializeField] private Color startColor_GradientReveal = Color.white;
    [SerializeField] private Color endColor_GradientReveal = Color.red;
    [SerializeField] private float revealSpeed_GradientReveal = 30f;

    //[Header("FlashColor")] 
    [SerializeField] private Color flashColor_Flash = Color.yellow;
    [SerializeField] private float duration_Flash = 0.3f;
    
    //[Header("ReadingHighlight")]
    [SerializeField] private Color baseColor_ReadingHighlight = Color.white;
    [SerializeField] private Color highlightColor_ReadingHighlight = Color.yellow;   
    [SerializeField] private float speed_ReadingHighlight = 20f;
    
    //[Header("MovingGradient")]
    [SerializeField] private bool loop_MovingGradient = true;
    [SerializeField] private Color colorA_MovingGradient = Color.cyan;
    [SerializeField] private Color colorB_MovingGradient = Color.magenta;
    [SerializeField] private float speed_MovingGradient = 1f;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if(triggerEffectWhenEnabled)
            ApplyEffect();
    }

    public void ApplyEffect()
    {
        if (currentEffect == null)
        {
            currentEffect = CreateEffect(textAnimationType);
        }

        currentEffect.Effect();
    }

    private ITextEffect CreateEffect(TextAnimationType type)
    {
        ITextEffect effect = null;

        switch (type)
        {
            case TextAnimationType.Writing:
                var writing = gameObject.AddComponent<Writing_TextEffect>();
                writing.Init(textMesh, duration_Writing);
                effect = writing;
                break;

            case TextAnimationType.Floating:
                var floating = gameObject.AddComponent<Floating_TextEffect>();
                floating.Init(textMesh, speed_Floating, amplitude_Floating);
                effect = floating;
                break;

            case TextAnimationType.Shaking:
                var shaking = gameObject.AddComponent<Shaking_TextEffect>();
                shaking.Init(textMesh, loopShake, intensity_Shake, duration_Shake);
                effect = shaking;
                break;
            
            case TextAnimationType.ColorPulse:
                var colorPulse = gameObject.AddComponent<ColorPulse_TextEffect>();
                colorPulse.Init(textMesh, colorA_ColorPulse, colorB_ColorPulse, speed_ColorPulse);
                effect = colorPulse;
                break;
            
            case TextAnimationType.RainbowWave:
                var rainbowWave = gameObject.AddComponent<RainbowWave_TextEffect>();
                rainbowWave.Init(textMesh, speed_RainbowWave, hueOffset_RainbowWave);
                effect = rainbowWave;
                break;
            
            case TextAnimationType.GradientReveal:
                var gradientReveal = gameObject.AddComponent<GradientReveal_TextEffect>();
                gradientReveal.Init(textMesh, startColor_GradientReveal, endColor_GradientReveal, revealSpeed_GradientReveal);
                effect = gradientReveal;
                break;
            
            case TextAnimationType.ColorFlash:
                var flashColor = gameObject.AddComponent<FlashColor_TextEffect>();
                flashColor.Init(textMesh, flashColor_Flash, duration_Flash);
                effect = flashColor;
                break;
            
            case TextAnimationType.ReadingHighlight:
                var readingHighlight = gameObject.AddComponent<ReadingHighlight_TextEffect>();
                readingHighlight.Init(textMesh, baseColor_ReadingHighlight, highlightColor_ReadingHighlight, speed_ReadingHighlight);
                effect = readingHighlight;
                break;
            
            case TextAnimationType.MovingGradient:
                var movingGradient = gameObject.AddComponent<MovingGradient_TextEffect>();
                movingGradient.Init(textMesh, 
                    colorB_MovingGradient, colorA_MovingGradient, speed_MovingGradient, loop_MovingGradient);
                effect = movingGradient;
                break;
        }

        return effect;
    }
}
