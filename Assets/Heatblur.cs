using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(HeatblurRenderer), PostProcessEvent.AfterStack, "Custom/Heatblur")]
public class Heatblur : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Heatblur effect intensity")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };

    // public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    // {
    //     return enabled.value && blend.value > 0f;
    // }
}
public sealed class HeatblurRenderer : PostProcessEffectRenderer<Heatblur>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Heatblur"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }

    public override DepthTextureMode GetLegacyCameraFlags()
    {
        return DepthTextureMode.Depth;
    }

}
