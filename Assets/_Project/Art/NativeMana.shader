Shader "HearthClone/Compatibility/Original Mana Crystal"
{
 Properties
 {
  _MainTex("Original crystal paint",2D)="white"{}
  _ScrollingTex("Original reflection texture",2D)="white"{}
  _Color("Source diffuse colour",Color)=(.62745094,1,.9844338,1)
  _tint("Source energy tint",Color)=(.109803915,.41232172,1,1)
  _Fade("Source fade",Color)=(1,1,1,1)
  _ReflectColor("Source reflection colour",Color)=(1,1,1,.89411765)
  _Blend("Source reflection blend",Float)=.5
  _LRMRimIntensity("Source rim intensity",Float)=.16417909
  _LRMRimPower("Source rim power",Float)=.9471641
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   TEXTURE2D(_ScrollingTex);SAMPLER(sampler_ScrollingTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color,_tint,_Fade,_ReflectColor;float _Blend,_LRMRimIntensity,_LRMRimPower;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   Varyings vert(Attributes i){Varyings o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
   half4 frag(Varyings i):SV_Target
   {
    float4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    float3 n=normalize(i.normalWS),view=normalize(GetWorldSpaceViewDir(i.positionWS));
    float3 reflected=reflect(-view,n);
    // Compatibility reconstruction, not the original compiled shader. All sampled art,
    // source material parameters and state curves are preserved. Reflection is static;
    // do not invent a scrolling clock while the original shader math is unavailable.
    float3 clouds=SAMPLE_TEXTURE2D(_ScrollingTex,sampler_ScrollingTex,reflected.xz*.5+.5).rgb;
    float gray=dot(paint.rgb,float3(.299,.587,.114));
    float3 body=lerp(gray.xxx,paint.rgb*_Color.rgb,_Fade.a)*_Fade.rgb;
    float rim=pow(saturate(1-dot(n,view)),_LRMRimPower)*_LRMRimIntensity;
    float3 colour=body+_tint.rgb*_tint.a*paint.a;
    colour+=(clouds*_ReflectColor.rgb*_ReflectColor.a*_Blend*paint.a+_tint.rgb*rim)*_tint.a;
    return half4(saturate(colour),1);
   }
   ENDHLSL
  }
 }
}
