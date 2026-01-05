Shader "Custom/PreviewWireframe" {
    Properties {
        _Color ("Color", Color) = (0,1,1,1)
        _Thickness ("Thickness", Range(0.1, 5)) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Thickness;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float2 bary : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            Varyings vert(Attributes input) {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.bary = float3(input.bary.x, input.bary.y, 1.0 - input.bary.x - input.bary.y);
                return o;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 d = fwidth(input.bary);
                float3 a = input.bary / max(d, 1e-5);
                float edge = 1.0 - saturate(min(min(a.x, a.y), a.z) * _Thickness);
                edge = saturate(edge);
                return half4(_Color.rgb, _Color.a * edge);
            }
            ENDHLSL
        }
    }
}
