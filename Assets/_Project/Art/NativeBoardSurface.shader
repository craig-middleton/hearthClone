Shader "HearthClone/Board/Native Surface"
{
    Properties
    {
        _MainTex ("Native paint", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ClipBackdrop ("Discard atlas backdrop", Float) = 0
        _ReliefLight ("Relief shading", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float3 normal:TEXCOORD1; };
            sampler2D _MainTex; fixed4 _Color; float _ClipBackdrop, _ReliefLight;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed3 paint=tex2D(_MainTex,i.uv).rgb;
                if (_ClipBackdrop > .5)
                {
                    // ORG_Combined uses four different constant backdrops, not opacity alpha.
                    float2 sampleUV=float2(i.uv.x<.5?(i.uv.y<.5?.49:.01):.51, i.uv.y<.5?.49:.99);
                    fixed3 backdrop=tex2D(_MainTex,sampleUV).rgb;
                    clip(length(paint-backdrop)-.022);
                }
                float3 normal=normalize(i.normal); if(normal.z>0)normal=-normal;
                float light=lerp(1,.84+.20*saturate(dot(normal,normalize(float3(-.4,.5,-1)))),_ReliefLight);
                return fixed4(paint*i.color.rgb*light,1);
            }
            ENDCG
        }
    }
}
