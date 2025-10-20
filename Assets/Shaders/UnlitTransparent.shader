Shader "Custom/UnlitTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,1,0.2)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Color [_Color]
        }
    }
}