using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class PixelArtRenderFeature : ScriptableRendererFeature
{
    [SerializeField] string targetTag = "SecurityCamera";

    class PixelArtRenderPass : ScriptableRenderPass
    {
        const string m_PassName = "PixelArtRenderPass";

        static readonly int s_PaletteTexId = Shader.PropertyToID("_PaletteTex");
        static readonly int s_PaletteSizeId = Shader.PropertyToID("_PaletteSize");

        int pixelWidth;
        int pixelHeight;

        public void Setup(int m_PixelWidth, int m_PixelHeight)
        {
            pixelWidth = m_PixelWidth;
            pixelHeight = m_PixelHeight;
            
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("Skipping");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;

            TextureDesc lowResDesc = renderGraph.GetTextureDesc(source);
            lowResDesc.name = $"{m_PassName}-LowRes";
            lowResDesc.width = pixelWidth;
            lowResDesc.height = pixelHeight;
            lowResDesc.filterMode = FilterMode.Point;
            lowResDesc.clearBuffer = false;
            TextureHandle lowResTarget = renderGraph.CreateTexture(lowResDesc);

            renderGraph.AddBlitPass(
                source, lowResTarget, Vector2.one, Vector2.zero,
                filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest,
                passName: $"{m_PassName}_Downsample");
            TextureDesc destDesc = renderGraph.GetTextureDesc(source);
            destDesc.name = $"{m_PassName}-Output";
            destDesc.clearBuffer = true;
            TextureHandle destination = renderGraph.CreateTexture(destDesc);

            renderGraph.AddBlitPass(
                lowResTarget, destination, Vector2.one, Vector2.zero,
                filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest,
                passName: $"{m_PassName}_Upsample");

            resourceData.cameraColor = destination;
        }
    }

    [Header("Pixel Art Resolution")]
    [SerializeField, Min(1)] private int pixelWidth = 400;
    [SerializeField, Min(1)] private int pixelHeight = 225;
    PixelArtRenderPass PixelArtPass;
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;

    public override void Create()
    {
        PixelArtPass = new PixelArtRenderPass
        {
            renderPassEvent = injectionPoint,
            requiresIntermediateTexture = true
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.CompareTag(targetTag))
        {
            PixelArtPass.Setup(pixelWidth, pixelHeight);
            renderer.EnqueuePass(PixelArtPass);
        }
    }
}
