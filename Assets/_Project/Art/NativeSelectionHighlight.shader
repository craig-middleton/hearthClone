Shader "HearthClone/Compatibility/Source Selection Highlight"
{
 Properties
 {
  _MainTex("Source-derived silhouette",2D)="black"{}
  _Color("Source base color",Color)=(0.043137256,0.8117647,0,1)
  _HiColor("Source hot color",Color)=(0.3764706,0.7294118,0.32941177,1)
  _Colorize("Source colorize",Color)=(0.25882354,0.7294118,0.1882353,1)
  _ColorIntensity("Source color intensity",Float)=1.5
  _Intensity("Source intensity",Float)=1.3
  _UseUV2("Source mask UV channel",Float)=1
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha One
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_Color,_HiColor,_Colorize;float _ColorIntensity,_Intensity,_UseUV2;
   CBUFFER_END
   struct Input{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;};
   struct Output{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   Output vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=TRANSFORM_TEX(lerp(i.uv,i.uv2,_UseUV2),_MainTex);return o;}
   half4 frag(Output i):SV_Target{half v=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;return half4(lerp(_Color.rgb*_ColorIntensity,_HiColor.rgb,v)*_Intensity,saturate(v)*_Color.a);}
   ENDHLSL
  }
 }
}
