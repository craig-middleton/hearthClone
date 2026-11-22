Shader "HearthClone/Compatibility/Source Menu Surface"
{
 Properties
 {
  _MainTex("Original paint",2D)="white"{}
  _Color("Original tint",Color)=(1,1,1,1)
  _SrcBlend("Source blend",Float)=1
  _DstBlend("Destination blend",Float)=0
  _ZWrite("Depth write",Float)=1
  _UseAlpha("Use paint alpha",Float)=0
  _Cutoff("Original alpha cutoff",Float)=0
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Off
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_ZWrite]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   float4 _MainTex_ST;half4 _Color;half _UseAlpha,_Cutoff;
   Output vert(Input v){Output o;o.vertex=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.color=v.color*_Color;return o;}
   half4 frag(Output i):SV_Target
   {
    half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    half alpha=lerp(1,paint.a,_UseAlpha)*i.color.a;
    clip(alpha-_Cutoff);
    return half4(paint.rgb*i.color.rgb,alpha);
   }
   ENDHLSL
  }
 }
}
