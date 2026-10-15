Shader "CatPlayer/Outline" {
 Properties { _Width("Width",Float)=0.003 _Color("Color",Color)=(0.025,0.018,0.012,1) }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }
 Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Front ZWrite On
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Width; half4 _Color;
 CBUFFER_END
 struct A { float4 p:POSITION; float3 n:NORMAL; };
 struct V { float4 p:SV_POSITION; };
 V vert(A a) { V o; float3 w=TransformObjectToWorld(a.p.xyz); w+=TransformObjectToWorldNormal(a.n)*_Width; w-=GetWorldSpaceNormalizeViewDir(w)*_Width*2; o.p=TransformWorldToHClip(w); return o; }
 half4 frag(V i):SV_Target { return _Color; }
 ENDHLSL
 } }
}
