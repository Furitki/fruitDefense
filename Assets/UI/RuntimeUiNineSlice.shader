Shader "Hidden/FruitDefense/RuntimeUiNineSlice"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Tint;
            float4 _TargetBorder;
            float4 _TargetSize;
            float4 _SourceX;
            float4 _SourceXRight;
            float4 _SourceY;
            float4 _SourceYTop;
            float4 _ClipRectPixels;

            Interpolators Vert(AppData input)
            {
                Interpolators output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float RemapNineSliceAxis(float coordinate, float targetSize,
                float leadingBorder, float trailingBorder,
                float4 sourceLeadingAndCenter, float4 sourceTrailing)
            {
                targetSize = max(targetSize, 1.0);
                float leadingEnd = saturate(leadingBorder / targetSize);
                float trailingStart = saturate(1.0 - trailingBorder / targetSize);

                if (leadingBorder > 0.5 && coordinate < leadingEnd)
                {
                    return lerp(sourceLeadingAndCenter.x,
                        sourceLeadingAndCenter.y,
                        saturate(coordinate / max(leadingEnd, 0.000001)));
                }

                if (trailingBorder > 0.5 && coordinate > trailingStart)
                {
                    return lerp(sourceTrailing.x, sourceTrailing.y,
                        saturate((coordinate - trailingStart)
                            / max(1.0 - trailingStart, 0.000001)));
                }

                return lerp(sourceLeadingAndCenter.z, sourceLeadingAndCenter.w,
                    saturate((coordinate - leadingEnd)
                        / max(trailingStart - leadingEnd, 0.000001)));
            }

            fixed4 Frag(Interpolators input) : SV_Target
            {
                clip(input.vertex.x - _ClipRectPixels.x);
                clip(input.vertex.y - _ClipRectPixels.y);
                clip(_ClipRectPixels.z - input.vertex.x);
                clip(_ClipRectPixels.w - input.vertex.y);
                float2 sourceUv;
                sourceUv.x = RemapNineSliceAxis(input.uv.x, _TargetSize.x,
                    _TargetBorder.x, _TargetBorder.z, _SourceX, _SourceXRight);
                sourceUv.y = RemapNineSliceAxis(input.uv.y, _TargetSize.y,
                    _TargetBorder.y, _TargetBorder.w, _SourceY, _SourceYTop);
                return tex2D(_MainTex, sourceUv) * _Tint;
            }
            ENDCG
        }
    }
}
