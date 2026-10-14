Shader "Hidden/Tutorial/BarrelDistortion"
{
    Properties
    {
        _MainTex ("Game Screen", 2D) = "black" {}
        _BackgroundTex ("Original Background", 2D) = "black" {}
        _Curve ("Vertical Curve", Range(-0.8, 0.8)) = 0
        _HorizontalWarp ("Horizontal Warp", Range(0, 0.5)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BackgroundTex;
            float _Curve;
            float _HorizontalWarp;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // 배경은 원본 UV로 읽기 때문에 절대 휘거나 회전하지 않습니다.
                fixed4 background = tex2D(_BackgroundTex, input.uv);

                // 카메라가 원통 안쪽에 있다고 가정합니다.
                // 좌우 벽이 관찰자 방향으로 감싸 들어오도록 안쪽 원통 투영을 적용합니다.
                float2 centered = input.uv - 0.5;
                float normalizedX = centered.x * 2.0;
                float2 gameUV = input.uv;
                float arc = 1.0;
                float angle = normalizedX * arc;
                float negativeZ = 1.0 - cos(angle);
                float perspective = 1.0 / (1.0 + _Curve * 2.0 * negativeZ);
                float curveBlend = saturate(abs(_Curve) / 0.48);

                // tan 투영은 좌우 벽을 화면 안쪽으로 감싸며,
                // 세로 좌표에 perspective를 곱해 바깥 원통과 반대 깊이를 만듭니다.
                float curvedX = 0.5 + (tan(angle) / tan(arc)) * 0.5;
                gameUV.x = lerp(input.uv.x, curvedX, curveBlend);
                gameUV.y = 0.5 + centered.y * perspective;
                gameUV.x -= centered.x * _HorizontalWarp * negativeZ * 0.15 * curveBlend;

                if (gameUV.x < 0.0 || gameUV.x > 1.0 || gameUV.y < 0.0 || gameUV.y > 1.0)
                    return background;

                fixed4 game = tex2D(_MainTex, gameUV);
                return lerp(background, game, game.a);
            }
            ENDCG
        }
    }
}
