Shader "HearthClone/Compatibility/Original Hero Death"
{
 Properties
 {
  _MainTex("Original paint",2D)="white"{}
  _Color("Original color",Color)=(1,1,1,1)
  _TintColor("Original particle tint multiplier",Color)=(1,1,1,1)
  _Intensity("Original intensity",Float)=1
  _Strength("Original crack curve (compatibility limit)",Float)=0
  _ZWrite("Depth write",Float)=0
  _ParticleMultiplier("Source particle convention",Float)=1
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
   Cull Off ZWrite [_ZWrite]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_Color,_TintColor;float _Intensity,_Strength,_ParticleMultiplier;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   Varyings vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
   half4 frag(Varyings i):SV_Target{half4 c=i.color*_Color*_TintColor*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);c*= _ParticleMultiplier;c.rgb*=_Intensity;return c;}
   ENDHLSL
  }
 }
}
