Shader "CatPlayer/EyeUnlit" {
 Properties { _BaseMap("Palette",2D)="white" {} }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Back ZWrite Off ZTest LEqual
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 struct A { float4 p:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);}
 ENDHLSL
 } }
}
