Shader "HearthClone/Board/Recovered Paint With Shadows"
{
    Properties
    {
        _MainTex ("Original painted texture", 2D) = "white" {}
        _ShadowTint ("Shadow tint", Color) = (.235,.18135,.098,1)
        _ShadowStrength ("Shadow opacity", Range(0,1)) = .45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 shadowCoord:TEXCOORD1; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _ShadowTint; float _ShadowStrength;
            CBUFFER_END
            v2f vert(appdata v)
            {
                v2f o;VertexPositionInputs position=GetVertexPositionInputs(v.vertex.xyz);
                o.pos=position.positionCS;o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.shadowCoord=GetShadowCoord(position);return o;
            }
            half4 frag(v2f i):SV_Target
            {
                half3 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
                half shadow=GetMainLight(i.shadowCoord).shadowAttenuation;
                return half4(paint*lerp(half3(1,1,1),_ShadowTint.rgb,_ShadowStrength*(1-shadow)),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
