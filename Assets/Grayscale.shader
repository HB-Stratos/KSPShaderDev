Shader "Hidden/Custom/Grayscale"
{
    HLSLINCLUDE
        #include "Grayscale.hlsl"
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
