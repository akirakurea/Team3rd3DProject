Shader "Cat Player/Shot Spread Ring"
{
    Properties
    {
        _Color("Ring Color", Color) = (1,1,1,.8)
        _OutlineColor("Edge Color", Color) = (.05,.05,.05,.65)
        _Radius("Ring Radius", Float) = .9
        _HalfWidth("Half Line Width", Float) = .01
        _BorderWidth("Edge Width", Float) = .005
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+99" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _OutlineColor;
                float _Radius, _HalfWidth, _BorderWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float distanceToLine = abs(radius - _Radius);
                float aa = max(fwidth(radius) * .5, .00001);
                float core = 1.0 - smoothstep(_HalfWidth - aa, _HalfWidth + aa, distanceToLine);
                float outer = _HalfWidth + _BorderWidth;
                float coverage = 1.0 - smoothstep(outer - aa, outer + aa, distanceToLine);
                half4 color = lerp(_OutlineColor, _Color, core);
                color.a *= coverage;
                return color;
            }
            ENDHLSL
        }
    }
}
