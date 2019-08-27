// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FX/Gem_D"
{
	Properties {
		_AmbientColor ("AmbientColor", Color) = (0.1,0.1,0.1,1)
		_Color ("Color", Color) = (1,1,1,1)
		_Emission ("Emission", Range(0.0,2.0)) = 0.0
		[NoScaleOffset] _RefractTex ("Refraction Texture", Cube) = "" {}
		[Toggle(_FRESNEL)] _UseFresnel("========== Use Fresnel ==========", Float) = 0
	}
	SubShader {
		Tags {
			"RenderType" = "Opaque"
		}

		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#pragma shader_feature _FRESNEL
        
			struct v2f {
				float4 pos : SV_POSITION;
				float3 uv : TEXCOORD0;
				#if _FRESNEL
					half fresnel : TEXCOORD1;
				#endif
			};

			v2f vert (float4 v : POSITION, float3 n : NORMAL)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v);

				// TexGen CubeReflect:
				// reflect view direction along the normal, in view space.
				float3 viewDir = normalize(ObjSpaceViewDir(v));
				o.uv = -reflect(viewDir, n);
				o.uv = mul(unity_ObjectToWorld, float4(o.uv,0));
				#if _FRESNEL
					o.fresnel = 1.0 - saturate(dot(n, viewDir));
				#endif
				return o;
			}

			fixed4 _AmbientColor;
			fixed4 _Color;
			samplerCUBE _RefractTex;
			half _EnvironmentLight;
			half _Emission;
			half4 frag (v2f i) : SV_Target
			{
				half3 refraction = texCUBE(_RefractTex, i.uv).rgb * _Color.rgb * _Emission;
				half3 ambientColor = _AmbientColor.rgb;
				#if _FRESNEL
					ambientColor *= i.fresnel;
				#endif
				return half4(ambientColor.rgb + refraction.rgb, 1.0f);
			}
			ENDCG 
		}

		// Shadow casting & depth texture support -- so that gems can
        // cast shadows
        UsePass "VertexLit/SHADOWCASTER"
	}
}
