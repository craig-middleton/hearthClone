Shader "HearthClone/Compatibility/Source Board Particles"
{
 Properties
 {
  _MainTex("Source particle paint",2D)="white"{}
  _TintColor("Source tint",Color)=(.5,.5,.5,.5)
  _SrcBlend("Source blend",Float)=5
  _DstBlend("Destination blend",Float)=10
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Blend [_SrcBlend] [_DstBlend]
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_TintColor;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   Varyings vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
   half4 frag(Varyings i):SV_Target{return 2*i.color*_TintColor*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);}
   ENDHLSL
  }
 }
}
