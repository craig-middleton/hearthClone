Shader "HearthClone/Compatibility/Original Hero Crack UI"
{
 Properties
 {
  [PerRendererData] _MainTex("Original crack paint",2D)="black"{}
  _Color("Original crack color",Color)=(1,1,1,1)
  _Intensity("Original intensity",Float)=1
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha One
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct Input{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct Output{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
   sampler2D _MainTex;fixed4 _Color;float _Intensity;float4 _ClipRect;
   Output vert(Input i){Output o;o.local=i.vertex;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;return o;}
   fixed4 frag(Output i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.rgb*=_Intensity;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
