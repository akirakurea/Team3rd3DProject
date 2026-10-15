Shader "Cat Player/Interaction Overlay"
{
 Properties { _Color("Color", Color)=(1,1,0,0.3) _Extrusion("World width", Float)=0 _Cull("Cull", Float)=2 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull [_Cull]
   Offset -1, -1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color; float _Extrusion;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; };
   Varyings Vert(Attributes i) { Varyings o; float3 p=TransformObjectToWorld(i.positionOS.xyz); p+=TransformObjectToWorldNormal(i.normalOS)*_Extrusion; o.positionCS=TransformWorldToHClip(p); return o; }
   half4 Frag(Varyings i):SV_Target { return _Color; }
   ENDHLSL
  }
 }
}
