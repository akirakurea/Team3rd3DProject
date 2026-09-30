Shader "Cat Player/Cursor Sphere"
{
 Properties {
  _Color("Color", Color)=(1,1,1,0.48)
  _OutlineColor("Outline Color", Color)=(0.12,0.12,0.12,0.8)
  _OutlineWidth("Outline Width", Range(0,0.5))=0.18
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest Always
   Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color, _OutlineColor;
   float _OutlineWidth;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normal:TEXCOORD0; };
   Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.normal=i.normalOS; return o; }
   half4 Frag(Varyings i):SV_Target {
    float3 n=normalize(i.normal);
    float light=.75+.25*saturate(dot(n,normalize(float3(-.3,.6,-1))));
    float radius=length(n.xy);
    float aa=max(fwidth(radius)*.5,0.001);
    float edge=_OutlineWidth>0 ? smoothstep(1-_OutlineWidth-aa,1-_OutlineWidth+aa,radius) : 0;
    return lerp(half4(_Color.rgb*light,_Color.a),_OutlineColor,edge);
   }
   ENDHLSL
  }
 }
}
