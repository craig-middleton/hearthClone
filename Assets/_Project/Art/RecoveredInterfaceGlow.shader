Shader "HearthClone/Interface/Recovered Highlight Study"
{
    Properties
    {
        _MainTex ("Native highlight mask", 2D) = "black" {}
        _FlareTex ("Native moving flare", 2D) = "white" {}
        _Color ("Source tint", Color) = (.05,.81,0,1)
        _Intensity ("Source intensity", Float) = 1
        _ColorIntensity ("Source colour intensity", Float) = 1.8
        _FlareIntensity ("Source flare intensity", Float) = .72
        _VflareSpeed ("Source flare speed", Float) = 4.67
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FlareTex); SAMPLER(sampler_FlareTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST, _FlareTex_ST; half4 _Color;
            float _Intensity, _ColorIntensity, _FlareIntensity, _VflareSpeed;
            CBUFFER_END
            v2f vert(appdata v)
            {
                v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; return o;
            }
            half4 frag(v2f i):SV_Target
            {
                half3 mask=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,TRANSFORM_TEX(i.uv,_MainTex)).rgb;
                half flare=SAMPLE_TEXTURE2D(_FlareTex,sampler_FlareTex,TRANSFORM_TEX(i.uv,_FlareTex)+float2(0,_Time.y*_VflareSpeed*.1)).r;
                return half4(max(mask.r,max(mask.g,mask.b))*_Color.rgb*_ColorIntensity*_Intensity*(1+flare*_FlareIntensity),0);
            }
            ENDHLSL
        }
    }
}
