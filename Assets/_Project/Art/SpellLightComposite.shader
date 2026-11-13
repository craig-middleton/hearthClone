Shader "HearthClone/UI/Spell Light Composite"
{
    Properties { [PerRendererData] _MainTex ("Effect Render Texture", 2D) = "black" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        // The offscreen particles already contain their alpha. Add their emitted light
        // directly instead of multiplying it by alpha a second time on the UI canvas.
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target { return fixed4(tex2D(_MainTex,i.uv).rgb*i.color.rgb*i.color.a,0); }
            ENDCG
        }
    }
}
