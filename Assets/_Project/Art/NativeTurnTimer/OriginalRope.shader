Shader "HearthClone/Compatibility/Original Turn Rope"
{
 Properties
 {
  _Diffuse("Original diffuse",2D)="white"{}
  _Mask("Original UV2 mask",2D)="white"{}
  _Color("Original tint",Vector)=(1,1,1,1)
  _Xamount("Original burn offset",Float)=.42
  _Amount("Original mask amount",Float)=1
  _SourceAlpha("Source visibility",Float)=1
  _Multiply("Shadow blend",Float)=0
  _SrcBlend("Source blend",Float)=5
  _DstBlend("Destination blend",Float)=10
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Blend [_SrcBlend] [_DstBlend]
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   sampler2D _Diffuse,_Mask;
   float4 _Diffuse_ST,_Mask_ST,_Color;
   float _Xamount,_Amount,_SourceAlpha,_Multiply;
   struct Input{float4 position:POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;float4 color:COLOR;};
   struct Varying{float4 position:SV_POSITION;float2 uv:TEXCOORD0;float2 mask:TEXCOORD1;float4 color:COLOR;};
   Varying vert(Input v)
   {
    Varying o;o.position=TransformObjectToHClip(v.position.xyz);
    o.uv=v.uv*_Diffuse_ST.xy+_Diffuse_ST.zw;
    // The source endpoint range is1.92 over a normalized UV2 strip. The
    // missing mask kernel's doubled horizontal UV reconstructs that range.
    o.mask=float2(v.uv2.x*2,v.uv2.y)*_Mask_ST.xy+_Mask_ST.zw+float2(_Xamount,0);
    o.color=v.color;return o;
   }
   half4 frag(Varying v):SV_Target
   {
    half4 paint=tex2D(_Diffuse,v.uv)*_Color*v.color;
    half mask=saturate(tex2D(_Mask,v.mask).r*_Amount);
    paint.a*=mask*_SourceAlpha;
    // Original compiled kernels are unavailable. UV channels, texture transforms,
    // gradient texture and authored burn offsets are preserved by this adapter.
    if(_Multiply>.5)return half4(lerp(half3(1,1,1),paint.rgb,paint.a),1);
    return paint;
   }
   ENDHLSL
  }
 }
}
