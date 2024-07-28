Shader "Hidden/Custom/Heatblur"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    // #include "UnityCG.cginc"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    float4 _MainTex_TexelSize;

    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);
    float _Blend;
    float4 Frag(VaryingsDefault i) : SV_TARGET
    {
        // return float4(i.texcoord, 0, 1);

        float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo));
        return depth/50;
    }

    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
