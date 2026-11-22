Shader "HearthClone/UI/Source Hand Silhouette"
{
 Properties
 {
  [PerRendererData] _MainTex("Original card silhouette",2D)="black"{}
  _Color("Source highlight base",Color)=(0.043137256,0.8117647,0,1)
  _HiColor("Source hot color",Color)=(0.3764706,0.7294118,0.32941177,1)
  _ColorIntensity("Source color intensity",Float)=1.5
  _Intensity("Source intensity",Float)=1.3
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
  Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
  Cull Off ZWrite Off ZTest[unity_GUIZTestMode]
  Blend SrcAlpha One
  ColorMask[_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct Input{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Output{float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
   sampler2D _MainTex;float4 _Color,_HiColor,_ClipRect;float _Intensity,_ColorIntensity;
   Output vert(Input i){Output o;o.pos=UnityObjectToClipPos(i.vertex);o.local=i.vertex;o.uv=i.uv;o.color=i.color;return o;}
   fixed4 frag(Output i):SV_Target
   {
    float mask=tex2D(_MainTex,i.uv).r;fixed4 c=fixed4(lerp(_Color.rgb*_ColorIntensity,_HiColor.rgb,mask)*_Intensity,mask*_Color.a)*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
