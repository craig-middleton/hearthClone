Shader "HearthClone/Prototype/Fireball Source Surface"
{
 Properties
 {
  _MainTex("Original paint",2D)="white"{}
  _AlphaNoise("Original alpha-noise paint",2D)="white"{}
  _Color("Original color",Color)=(1,1,1,1)
  _TintColor("Original particle tint",Color)=(1,1,1,1)
  _ColorMul("Original color multiplier",Float)=1
  _Intensity("Original intensity",Float)=1
  _Alpha("Original alpha",Float)=1
  _AlphaIntensity("Original alpha-noise intensity",Float)=1
  _Scale("Original alpha-noise scale",Float)=1
  _USpeed("Original alpha-noise U rate",Float)=0
  _VSpeed("Original alpha-noise V rate",Float)=0
  _offsetU("Original cutout U offset",Float)=0
  _offsetV("Original cutout V offset",Float)=0
  _Cutoff("Original cutout threshold",Float)=0
  _SourceAlpha("Original effect alpha curve",Float)=1
  _Kind("Original shader family",Float)=0
  _SrcBlend("Source blend",Float)=5
  _DstBlend("Destination blend",Float)=1
  _ZWrite("Depth write",Float)=0
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_ZWrite]
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   TEXTURE2D(_AlphaNoise);SAMPLER(sampler_AlphaNoise);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_AlphaNoise_ST,_Color,_TintColor;
   float _ColorMul,_Intensity,_Alpha,_AlphaIntensity,_Scale,_USpeed,_VSpeed;
   float _offsetU,_offsetV,_Cutoff,_SourceAlpha,_Kind;
   CBUFFER_END
   struct Input{float4 position:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Output{float4 position:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;float2 rawUV:TEXCOORD1;};
   Output vert(Input i)
   {
    Output o;o.position=TransformObjectToHClip(i.position.xyz);
    o.color=i.color;o.rawUV=i.uv;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;
   }
   half4 frag(Output i):SV_Target
   {
    // The original GPU kernels are unavailable. Shader-family behavior is a
    // compatibility reconstruction using only active source parameters.
    float2 uv=i.uv+float2(_offsetU,_offsetV);
    half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
    if(_Kind>2.5&&_Kind<3.5)
    {
     // Original Hero/Unlit/Unlit_Cutout is an opaque depth-writing core.
     // Stale ElectricityBrightness/GlowBrightness values are deliberately unused.
     half4 core=paint*_Color;
     clip(core.a-_Cutoff);
     return half4(core.rgb,1);
    }
    half4 c=paint*i.color*_Color;
    if(_Kind<.5||_Kind>4.5)c*=_TintColor;
    // Particle Additive +1 explicitly declares its color multiplier; ordinary
    // particle families retain the conventional doubled vertex/tint convention.
    c.rgb*=_ColorMul*_Intensity;
    c.a*=_Alpha*_SourceAlpha;
    if(_Kind>1.5&&_Kind<2.5)
    {
     float2 noiseUV=i.rawUV*_Scale+_Time.y*float2(_USpeed,_VSpeed);
     noiseUV=noiseUV*_AlphaNoise_ST.xy+_AlphaNoise_ST.zw;
     half noise=SAMPLE_TEXTURE2D(_AlphaNoise,sampler_AlphaNoise,noiseUV).a;
     // Two-layer alpha product is disclosed compatibility math; source rates,
     // scale, paint and alpha intensity are retained, without new authored motion.
     c.a*=saturate(noise*_AlphaIntensity);
    }
    if(_Kind>3.5&&_Kind<4.5)
    {
     // Multiplication requires white-neutral output at zero source intensity.
     return half4(lerp(half3(1,1,1),paint.rgb*_Color.rgb,
       saturate(paint.a*i.color.a*_Color.a*_SourceAlpha*_Intensity)),1);
    }
    return c;
   }
   ENDHLSL
  }
 }
}
