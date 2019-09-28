// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UltimateRayDesigner/Ray_Alpha"
{
	Properties
	{
		_MainTex ("Main Tex (RGB)", 2D) = "black" {}
		_Mask ("Mask (RGB)", 2D) = "black" {}
		_Distortion ("Distortion (RGB)", 2D) = "white" {}
		_TintColor ("TintColor", Color) = (1,1,1,1)
		_AMP ("Amplify", Range(0,1.5)) = 0.0
		_XFREQ ("X Frequency", Range(0.0,5.0)) = 0.5
		_YFREQ ("Y Frequency", Range(0.0,5.0)) = 0.5
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Pass
		{
			//Cull back
			Cull off
			Lighting Off
			Offset -1, -1
			Fog { Mode Off }
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			zwrite off
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _Distortion;
			sampler2D _Mask;
			half4 _Mask_ST;
			half4 _Distortion_ST;
			half4 _MainTex_ST;
			fixed4 _TintColor;
			float _AMP;
			float _XFREQ;
			float _YFREQ;

			struct v2f
			{
				half4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				half2 texcoord1 : TEXCOORD1;
				half opacity : TEXCOORD2;
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				
				float4 pos = v.vertex;
  				float4 displacementDirection = float4(1.0, 1.0, 1.0, 0);
  				
  				float2 transformedTex = TRANSFORM_TEX( v.texcoord, _Distortion);
  				transformedTex.x *= _XFREQ;
  				transformedTex.y *= _YFREQ;
  				
  				fixed4 displacement = tex2Dlod( _Distortion, float4(transformedTex, 0, 0) );

				pos += float4(
					(displacement.r * 2 - 1) * _AMP * v.color.r,
					(displacement.g * 2 - 1) * _AMP * v.color.g,
					(displacement.b * 2 - 1) * _AMP * v.color.b,
					0) * v.color.r;

				o.opacity = v.color.a;

				o.vertex = UnityObjectToClipPos(pos);

				o.texcoord = TRANSFORM_TEX( v.texcoord, _MainTex);
				o.texcoord1 = TRANSFORM_TEX( v.texcoord, _Mask);
				return o;
			}

			fixed4 frag (v2f IN) : COLOR
			{
				fixed4 base = tex2D(_MainTex, IN.texcoord);
				fixed4 mask = tex2D(_Mask, IN.texcoord1);
				base *= mask * (_TintColor * 2);
				base.a *= _TintColor.a * IN.opacity;
				return base;
			}
			ENDCG
		}
	}
	Fallback "AlphaBlended"
}
