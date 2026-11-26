Shader "HearthClone/UI/Prototype Original Card Surface"
{
    Properties
    {
        [PerRendererData] _MainTex("Original card paint; alpha is an effect mask",2D)="white"{}
        _SurfaceMode("0 unlit, 1 original second UV multiply, 2 source gem mask scroller",Float)=0
        _Color("Original main tint",Color)=(1,1,1,1)
        _SecondTex("Original second texture",2D)="white"{}
        _SecondTint("Original second tint",Color)=(.5,.5,.5,.5)
        _BlendIntensity("Original blend intensity",Float)=2
        _ScrollingTex("Original gem cloud texture",2D)="black"{}
        _TintColor("Original gem tint",Color)=(1,1,1,1)
        _tint("Original gem effect tint",Color)=(1,1,1,1)
        _Fade("Original gem fade",Color)=(1,1,1,1)
        _Intensity("Original gem intensity",Float)=1
        _ScaleX("Original gem FX scale X",Float)=1
        _ScaleY("Original gem FX scale Y",Float)=1
        _XSpeed("Original gem X speed",Float)=0
        _YSpeed("Original gem Y speed",Float)=0
        _SourceClockEnabled("Evaluate compatibility scroller",Float)=1
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
        Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;};
            struct Output{float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;float4 local:TEXCOORD2;};
            sampler2D _MainTex,_SecondTex,_ScrollingTex;
            float4 _SecondTex_ST,_ScrollingTex_ST,_Color,_SecondTint,_TintColor,_tint,_Fade,_ClipRect;
            float _SurfaceMode,_BlendIntensity,_Intensity,_ScaleX,_ScaleY,_XSpeed,_YSpeed,_SourceClockEnabled;
            Output vert(Input i)
            {
                Output o;o.position=UnityObjectToClipPos(i.vertex);o.local=i.vertex;
                // UV0 already includes source material/cropped-art transforms in
                // the recipe. UV2 remains the original independent source channel.
                o.uv=i.uv;o.uv2=TRANSFORM_TEX(i.uv2,_SecondTex);o.color=i.color;return o;
            }
            half4 frag(Output i):SV_Target
            {
                half4 paint=tex2D(_MainTex,i.uv);
                half3 result=paint.rgb*_Color.rgb;
                if(_SurfaceMode>.5&&_SurfaceMode<1.5)
                {
                    // Compatibility reconstruction of source Unlit_2Texture2uv /
                    // Unlit_Portrait. Original BlendIntensity=2 and half-tint make
                    // white second paint neutral; the watermark/vignette multiply.
                    half3 sourceTint=_SecondTint.rgb;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    // Unity converts Color properties to linear on upload. The
                    // original half-tint is a numeric mod2x neutral, not a .214
                    // lighting value. Restore that authored factor explicitly.
                    sourceTint=LinearToGammaSpace(sourceTint);
                    #endif
                    result*=tex2D(_SecondTex,i.uv2).rgb*sourceTint*_BlendIntensity;
                }
                else if(_SurfaceMode>1.5)
                {
                    // Original shader is DiffuseAlphaMaskScroller, not an alpha
                    // cutout. Kernel and legacy _Time.x convention are reconstructed;
                    // cloud artwork, mask, tint, scales and speeds are source data.
                    float2 clock=float2(_XSpeed,_YSpeed)*_Time.x*_SourceClockEnabled;
                    float2 cloudUV=i.uv*float2(_ScaleX,_ScaleY)+clock;
                    cloudUV=cloudUV*_ScrollingTex_ST.xy+_ScrollingTex_ST.zw;
                    half3 clouds=tex2D(_ScrollingTex,cloudUV).rgb;
                    result=(paint.rgb*_TintColor.rgb+clouds*_tint.rgb*paint.a*_Intensity)*_Fade.rgb;
                }
                half4 output=half4(result*i.color.rgb,i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                output.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return output;
            }
            ENDCG
        }
    }
}
