Shader "HearthClone/Compatibility/Original Spell Surface"
{
 Properties{_MainTex("Source paint",2D)="white"{} _Color("Source color",Color)=(1,1,1,1) _TintColor("Source particle tint",Color)=(1,1,1,1) _Intensity("Source intensity",Float)=1 _ParticleMultiplier("Source particle convention",Float)=1 _SourceAlpha("Source alpha animation",Float)=1 _SrcBlend("Source blend",Float)=5 _DstBlend("Destination blend",Float)=1}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"} Blend [_SrcBlend] [_DstBlend] Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_Color,_TintColor;float _Intensity,_ParticleMultiplier,_SourceAlpha;
   CBUFFER_END
   struct I{float4 position:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct O{float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   O vert(I i){O o;o.position=TransformObjectToHClip(i.position.xyz);o.color=i.color;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
   half4 frag(O i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color*_Color*_TintColor*_ParticleMultiplier;c.rgb*=_Intensity;c.a*=_SourceAlpha;return c;}
   ENDHLSL
  }
 }
}
