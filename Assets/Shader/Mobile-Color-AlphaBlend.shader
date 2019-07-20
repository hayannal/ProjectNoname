// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FrameworkPV/ColorAlphaBlend"
{
	Properties
	{
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend Mode", Float) = 1

		[KeywordEnum(None, Default, Fmod)] _Flow("========== Use Flow ==========", Float) = 0
		_FlowSpeed ("Main (XY)", Vector) = (0, 0, 0, 0)
		[Toggle(_MASK)] _UseMask("========== Use Mask ==========", Float) = 0
		_MaskTex ("Mask (R)", 2D) = "white" {}
		[Toggle(_SECONDUV)] _UseSecondUV("========== Use Second UV ==========", Float) = 0
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			Cull Off
			Lighting Off
			ZWrite Off
			Fog {Mode Off}

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_fog
			#pragma shader_feature _FLOW_NONE _FLOW_DEFAULT _FLOW_FMOD
			#pragma shader_feature _MASK
			#pragma shader_feature _SECONDUV

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			#if _FLOW_DEFAULT || _FLOW_FMOD
				half4 _FlowSpeed;
			#endif
			#if _MASK
				sampler2D _MaskTex;
				half4 _MaskTex_ST;
			#endif

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				#if _SECONDUV
					float2 texcoord : TEXCOORD1;
				#else
					float2 texcoord : TEXCOORD0;
				#endif
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				#if _MASK
					float2 maskUV : TEXCOORD1;
				#endif
				//UNITY_FOG_COORDS(1)
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				#if _FLOW_DEFAULT
					o.texcoord += _FlowSpeed.xy * _Time.y;
				#elif _FLOW_FMOD
					o.texcoord += fmod(_FlowSpeed.xy * _Time.y, 1.0f);
				#endif
				#if _MASK
					o.maskUV = TRANSFORM_TEX(v.texcoord, _MaskTex);
				#endif
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = i.color * _Color * tex2D(_MainTex, i.texcoord);
				#if _MASK
					col.a *= tex2D(_MaskTex, i.maskUV).r;
				#endif
				//UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
				return col;
			}
			ENDCG
		}
	}
}