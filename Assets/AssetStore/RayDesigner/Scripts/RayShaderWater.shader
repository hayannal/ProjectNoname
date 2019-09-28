// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UltimateRayDesigner/Ray_Water"
{
	Properties
	{
		_MainTex ("Main Tex (RGB)", 2D) = "black" {}
		_Mask ("Mask (RGB)", 2D) = "black" {}
		_Distortion ("Distortion (RGB)", 2D) = "white" {}
		_TintColor ("TintColor", Color) = (1,1,1,1)
		
		_AMP ("Amplify", Range(0,3.5)) = 0.0
		_XFREQ ("X Frequency", Range(0.0,5.0)) = 0.5
		_YFREQ ("Y Frequency", Range(0.0,5.0)) = 0.5
		
		_RefractionStrength("Refraction Strength", Range(0.0,1.0)) = 0.5
		_Noise1("Caustics", 2D) = "white" {}
		_Intensity("Intensity", float) = 1
		_Speed("Scroll Speed", float) = 1
		_Contrast("Contrast", Range(0.1,20)) = 1
	}

	SubShader
	{
		

		GrabPass
		{
			"_BackgroundTexture"
		}
		Pass
		{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
		}
			//Cull back
			Cull Back ZWrite Off ZTest LEqual
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
			
			float _RefractionStrength;
			sampler2D _Noise1;
			float4 _Noise1_ST;
			float _Intensity;
			float _Speed;
			float _Contrast;

			sampler2D _BackgroundTexture;

			struct v2f
			{
				half4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				half2 texcoord1 : TEXCOORD1;
				half opacity : TEXCOORD2;
				half2 uvT : TEXCOORD3;
				float4 grabPos : TEXCOORD4;
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

				o.uvT = TRANSFORM_TEX(v.texcoord, _Noise1) + float2(0, -_Time.y*_Speed*5);
				o.texcoord = TRANSFORM_TEX( v.texcoord, _MainTex);
				o.texcoord1 = TRANSFORM_TEX(v.texcoord, _Mask);
				o.grabPos = ComputeGrabScreenPos(o.vertex);
				return o;
			}

			fixed4 frag (v2f IN) : COLOR
			{
				fixed4 Noise = tex2D(_Distortion, IN.texcoord1)*2-1;
				fixed4 BG = tex2D(_BackgroundTexture, IN.grabPos - Noise*_RefractionStrength*IN.opacity);
				fixed Mask = tex2D(_MainTex, IN.texcoord).r;

				float2 uv1 = -IN.uvT * float2(0.329, 0.578) + float2(0.806, 0.952) * -_Time.y * _Speed * 0.535;
				float2 uv2 = IN.uvT * float2(0.806, 0.952) + float2(0.610, 0.845) * _Time.y * _Speed * -0.636;
				float2 uv3 = IN.uvT * float2(0.610, 0.845) + float2(0.629, 0.578) * _Time.y * _Speed * 0.330;

				fixed R = tex2D(_Noise1, uv1).r;
				fixed G = tex2D(_Noise1, uv2).g;
				fixed B = tex2D(_Noise1, uv3).b;
				fixed Result1 = (R * G * B) * _Intensity * IN.opacity*Mask;

				BG.rgb *= _TintColor.rgb*2;
				BG.a *= _TintColor.a * IN.opacity;
				return BG + pow(Result1, _Contrast);
			}
			ENDCG
		}
	}
	Fallback "AlphaBlended"
}
