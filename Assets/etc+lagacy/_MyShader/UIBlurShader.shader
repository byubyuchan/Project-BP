Shader "Custom/UIBlur"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0, 0, 0, 0.5)
        _BlurSize ("Blur Size", Range(0, 10)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100

        GrabPass { "_GrabTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 grabPos : TEXCOORD0;
            };

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float4 _TintColor;
            float _BlurSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w;
                float2 offset = _GrabTexture_TexelSize.xy * _BlurSize;

                fixed4 col = tex2D(_GrabTexture, uv);
                col += tex2D(_GrabTexture, uv + float2(offset.x, offset.y));
                col += tex2D(_GrabTexture, uv + float2(-offset.x, offset.y));
                col += tex2D(_GrabTexture, uv + float2(offset.x, -offset.y));
                col += tex2D(_GrabTexture, uv + float2(-offset.x, -offset.y));
                col += tex2D(_GrabTexture, uv + float2(offset.x, 0));
                col += tex2D(_GrabTexture, uv + float2(-offset.x, 0));
                col += tex2D(_GrabTexture, uv + float2(0, offset.y));
                col += tex2D(_GrabTexture, uv + float2(0, -offset.y));

                col /= 9.0;
                
                return col * _TintColor;
            }
            ENDCG
        }
    }
}