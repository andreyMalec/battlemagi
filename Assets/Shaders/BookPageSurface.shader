Shader "Custom/BookPageSurface"
{
    Properties
    {
        _LeftPageTex ("Left Page", 2D) = "white" {}
        _RightPageTex ("Right Page", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _LeftPageUV ("Left Page UV (xMin,yMin,xMax,yMax)", Vector) = (0,0,0.5,1)
        _RightPageUV ("Right Page UV (xMin,yMin,xMax,yMax)", Vector) = (0.5,0,1,1)
        _AlphaClip ("Alpha Clip", Range(0,1)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Back
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LeftPageTex;
            sampler2D _RightPageTex;
            float4 _Tint;
            float4 _LeftPageUV;
            float4 _RightPageUV;
            float _AlphaClip;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 SamplePage(float2 uv, float4 uvRect, sampler2D tex)
            {
                float2 size = uvRect.zw - uvRect.xy;
                if (size.x <= 0.0001 || size.y <= 0.0001)
                {
                    return float4(0, 0, 0, 0);
                }

                float2 local = (uv - uvRect.xy) / size;
                if (local.x < 0 || local.x > 1 || local.y < 0 || local.y > 1)
                {
                    return float4(0, 0, 0, 0);
                }

                return tex2D(tex, local);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 left = SamplePage(i.uv, _LeftPageUV, _LeftPageTex);
                float4 right = SamplePage(i.uv, _RightPageUV, _RightPageTex);
                float4 color = left + right;
                color *= _Tint;
                clip(color.a - _AlphaClip);
                return color;
            }
            ENDCG
        }
    }
}

