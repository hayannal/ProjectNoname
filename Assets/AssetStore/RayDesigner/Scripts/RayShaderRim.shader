// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UltimateRayDesigner/Ray_Rim"
{
	Properties
	{
		_Distortion ("Distortion (RGB)", 2D) = "white" {}
		_AMP ("Amplify", Range(0,3.5)) = 0.0
		_XFREQ ("X Frequency", Range(0.0,5.0)) = 0.5
		_YFREQ ("Y Frequency", Range(0.0,5.0)) = 0.5

		_TintColor("Rim Color", Color) = (0, 0, 0, 1)
		_Falloff("Falloff", Float) = 16
		_Transparency("Transparency", Float) = 1
		_AlignNormals("Align Normals to View", float) = 0.5
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
			//Blend SrcAlpha OneMinusSrcAlpha
			Blend One One
			Zwrite Off
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Distortion;
			half4 _Distortion_ST;
			float _AMP;
			float _XFREQ;
			float _YFREQ;

			uniform fixed4 _TintColor;
			uniform fixed _Falloff;
			uniform fixed _Transparency;
			uniform float _AlignNormals;

			struct v2f
			{
				half4 vertex : POSITION;
				half opacity : TEXCOORD0;
				fixed3 normal : TEXCOORD1;
				fixed3 worldvertpos : TEXCOORD2;
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

				pos.xyz += v.normal*float3(
					displacement.r * _AMP * v.color.r,
					displacement.g * _AMP * v.color.g,
					displacement.b * _AMP * v.color.b) * v.color.r;

				o.opacity = v.color.a;

				o.vertex = UnityObjectToClipPos(pos);
				o.worldvertpos = mul(unity_ObjectToWorld, v.vertex);

				o.normal = v.normal;
				return o;
			}

			fixed4 frag (v2f IN) : COLOR
			{
				fixed3 viewdir = normalize(IN.worldvertpos - _WorldSpaceCameraPos);
				fixed4 color = _TintColor;
				color.a = dot(-viewdir, normalize(lerp(IN.normal, -viewdir, _AlignNormals)));
				color.a = pow(color.a, _Falloff);
				color.a *= _Transparency;
				color.rgb *= color.a;
				return color;
			}
			ENDCG
		}
	}
	Fallback "AlphaBlended"
}
