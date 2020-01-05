Shader "FrameworkPV/GradientSkybox"
{
    Properties
    {
		_Color1("Color 1", Color) = (1, 1, 1, 0)
		_Color2("Color 2", Color) = (1, 1, 1, 0)
		_UpVector("Up Vector", Vector) = (0, 1, 0, 0)
		_Intensity("Intensity", Float) = 1.0
		_Exponent("Exponent", Float) = 1.0
    }
    SubShader
    {
		Tags { "RenderType"="Background" "Queue"="Background" }
        LOD 100

        Pass
        {
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }

            CGPROGRAM
			#pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 uv : TEXCOORD0;
            };

			half4 _Color1;
			half4 _Color2;
			half4 _UpVector;
			half _Intensity;
			half _Exponent;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : COLOR
            {
				half d = dot(normalize(i.uv), _UpVector) * 0.5f + 0.5f;
				return lerp(_Color1, _Color2, pow(d, _Exponent)) * _Intensity;
            }
            ENDCG
        }
    }
}
