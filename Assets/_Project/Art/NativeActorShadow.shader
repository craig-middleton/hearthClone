Shader "HearthClone/Compatibility/Source White Neutral Shadow"
{
 Properties { _MainTex("Original shadow paint",2D)="white"{} _Opacity("Source opacity",Float)=1 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Blend DstColor Zero
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST;float _Opacity;
   CBUFFER_END
   struct Input{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Output{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float alpha:TEXCOORD1;};
   Output vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.alpha=i.color.a;return o;}
   half4 frag(Output i):SV_Target{half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);return half4(lerp(half3(1,1,1),paint.rgb,saturate(paint.a*i.alpha*_Opacity)),1);}
   ENDHLSL
  }
 }
}
