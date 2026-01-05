Shader "Custom/PreviewTransparent" {
    Properties {
        _Color ("Color", Color) = (0,1,1,0.2)
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass {
            Color [_Color]
        }
    }
}

