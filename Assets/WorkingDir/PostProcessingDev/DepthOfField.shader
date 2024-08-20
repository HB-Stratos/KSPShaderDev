// Upgrade NOTE: replaced 'defined KERNELDEBUG' with 'defined (KERNELDEBUG)'

Shader "Hidden/DepthOfField"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex, _CameraDepthTexture, _CoCTex, _DoFTex;
    float4 _MainTex_TexelSize;

    float _BokehRadius, _FocusDistance, _FocusRange;
    
    struct VertexData
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Interpolators
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    Interpolators VertexProgram(VertexData v)
    {
        Interpolators i;
        i.pos = UnityObjectToClipPos(v.vertex);
        i.uv = v.uv;
        return i;
    }

    ENDCG

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        // 0 circleOfConfusionPass
        Pass
        {
            CGPROGRAM
            
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            half FragmentProgram(Interpolators i) : SV_TARGET
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                depth = LinearEyeDepth(depth);
                
                float coc = (depth - _FocusDistance) / _FocusRange;
                coc = clamp(coc, -1, 1) * _BokehRadius;
                return coc;
            }

            ENDCG
        }

        // 1 Prefilter Pass
        Pass
        {
            CGPROGRAM

            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            half Weigh(half3 c)
            {
                return 1 / (1 + max(max(c.r, c.g), c.b));
            }

            half4 FragmentProgram(Interpolators i) : SV_TARGET
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-0.5, 0.5).xxyy;

                half3 s0 = tex2D(_MainTex, i.uv + o.xy).rgb;
                half3 s1 = tex2D(_MainTex, i.uv + o.zy).rgb;
                half3 s2 = tex2D(_MainTex, i.uv + o.xw).rgb;
                half3 s3 = tex2D(_MainTex, i.uv + o.zw).rbg;

                half w0 = Weigh(s0);
                half w1 = Weigh(s1);
                half w2 = Weigh(s2);
                half w3 = Weigh(s3);

                half3 color = s0 * w0 + s1 * w1 + s2 * w2 + s3 * w3;
                color /= max(w0 + w1 + w2 + w3, 0.00001);

                // #ifndef SHADER_API_D3D11
                half coc0 = tex2D(_CoCTex, i.uv + o.xy).r;
                half coc1 = tex2D(_CoCTex, i.uv + o.zy).r;
                half coc2 = tex2D(_CoCTex, i.uv + o.xw).r;
                half coc3 = tex2D(_CoCTex, i.uv + o.zw).r;

                // half coc = (coc0 + coc1 + coc2 + coc3) * 0.25;
                
                half cocMin = min(min(min(coc0, coc1), coc2), coc3);
                half cocMax = max(max(max(coc0, coc1), coc2), coc3);

                half coc = cocMax >= -cocMin ? cocMax : cocMin;

                // #endif
                // #ifdef SHADER_API_D3D11 //This does not quite work
                //     float4 dep = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, i.uv);
                //     coc = (dep.x + dep.y + dep.z + dep.w) * 0.25;
                // #endif
                
                return half4(color, coc);
            }

            ENDCG
        }

        // 2 Bokeh Pass
        Pass
        {
            CGPROGRAM

            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            // #define KERNELDEBUG

            #define BOKEH_KERNEL_MEDIUM

            // From https://github.com/Unity-Technologies/PostProcessing/
            // blob/v2/PostProcessing/Shaders/Builtins/DiskKernels.hlsl
            #if defined(BOKEH_KERNEL_SMALL)
                static const int kernelSampleCount = 16;
                static const float2 kernel[kernelSampleCount] = {
                    …
                };
            #elif defined(BOKEH_KERNEL_MEDIUM)
                static const int kernelSampleCount = 22;
                static const float2 kernel[kernelSampleCount] = {
                    float2(0, 0),
                    float2(0.53333336, 0),
                    float2(0.3325279, 0.4169768),
                    float2(-0.11867785, 0.5199616),
                    float2(-0.48051673, 0.2314047),
                    float2(-0.48051673, -0.23140468),
                    float2(-0.11867763, -0.51996166),
                    float2(0.33252785, -0.4169769),
                    float2(1, 0),
                    float2(0.90096885, 0.43388376),
                    float2(0.6234898, 0.7818315),
                    float2(0.22252098, 0.9749279),
                    float2(-0.22252095, 0.9749279),
                    float2(-0.62349, 0.7818314),
                    float2(-0.90096885, 0.43388382),
                    float2(-1, 0),
                    float2(-0.90096885, -0.43388376),
                    float2(-0.6234896, -0.7818316),
                    float2(-0.22252055, -0.974928),
                    float2(0.2225215, -0.9749278),
                    float2(0.6234897, -0.7818316),
                    float2(0.90096885, -0.43388376),
                };
            #endif

            half Weigh(half coc, half radius)
            {
                // saturate is effectively a clamp between 0 and 1
                return saturate((coc - radius + 2) / 2);
            }

            half4 FragmentProgram(Interpolators i) : SV_TARGET
            {
                half coc = tex2D(_MainTex, i.uv).a;

                half3 bgColor = 0; half3 fgColor = 0;
                half bgWeight = 0; half fgWeight = 0;
                for (int k = 0; k < kernelSampleCount; k++)
                {
                    float2 o = kernel[k] * _BokehRadius;
                    half radius = length(o);
                    o *= _MainTex_TexelSize.xy;
                    half4 s = tex2D(_MainTex, i.uv + o);
                    // if our current pixel is with in the radius of bokeh of the pixel our kernel hit
                    half bgw = Weigh(max(0, min(s.a, coc)), radius);
                    bgColor += s.rgb * bgw;
                    bgWeight += bgw;

                    half fgw = Weigh(-s.a, radius);
                    fgColor += s.rgb * fgw;
                    fgWeight += fgw;
                }
                bgColor *= 1.0 / (bgWeight + (bgWeight == 0));
                fgColor *= 1.0 / (fgWeight + (fgWeight == 0));

                #ifdef KERNELDEBUG
                    for (int n = 0; n < kernelSampleCount; n++)
                    {
                        float2 kernelPoint = kernel[n] * 100 + _MainTex_TexelSize.zw / 2;
                        float2 pixelDist = kernelPoint - (i.uv * _MainTex_TexelSize.zw);
                        if (abs(length(pixelDist)) <= 10)
                        {
                            color = half3(pixelDist * 1000, 0);
                        }
                    }
                #endif
                
                half bgfg = min(1, fgWeight * 3.14159265359 / kernelSampleCount);
                half3 color = lerp(bgColor, fgColor, bgfg);

                return half4(color, bgfg);
            }

            ENDCG
        }

        // 3 PostFilterPass
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            half4 FragmentProgram(Interpolators i) : SV_TARGET
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-0.5, 0.5).xxyy;
                half4 s = tex2D(_MainTex, i.uv + o.xy) +
                tex2D(_MainTex, i.uv + o.zy) +
                tex2D(_MainTex, i.uv + o.xw) +
                tex2D(_MainTex, i.uv + o.zw);
                return s * 0.25;
            }
            ENDCG
        }

        //4 CombinePass
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            half4 FragmentProgram(Interpolators i) : SV_TARGET
            {
                half4 source = tex2D(_MainTex, i.uv);
                half coc = tex2D(_CoCTex, i.uv).r;
                half4 dof = tex2D(_DoFTex, i.uv);

                half dofStrength = smoothstep(0.1, 1, abs(coc));
                half3 color = lerp(source.rgb, dof.rgb, dofStrength + dof.a - dofStrength * dof.a);

                return half4(color, source.a);
            }
            ENDCG
        }
    }
}
