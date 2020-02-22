// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FrameworkNG/Particle/Shadow"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest Mode", Float) = 4
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
		_ShadowCutoff("Shadow Cutoff", Range(0, 1)) = 0.05
		[KeywordEnum(A,R)] _Shadow_Channel("Shadow Channel", Float) = 0

		[Toggle(_CUTOFF)] _UseCutoff("========== Use Cutoff ==========", Float) = 0
		_Cutoff("Cutoff", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZTest [_ZTest]
			Cull [_Cull]
			Lighting Off
			ZWrite Off
			Fog {Mode Off}

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_fog
			#pragma shader_feature _CUTOFF

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TintColor;
			#if _CUTOFF
				fixed _Cutoff;
			#endif

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
				#if _CUTOFF
					clip(col.a - _Cutoff);
				#endif
				//UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
				return col;
			}
			ENDCG
		}

		// shadow caster rendering pass, implemented manually
	    // using macros from UnityCG.cginc
		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma shader_feature _SHADOW_CHANNEL_A _SHADOW_CHANNEL_R
			#include "UnityCG.cginc"
				
			inline fixed SelectShadowChannel(fixed4 col)
			{
				#if _SHADOW_CHANNEL_A
				return col.a;
				#elif _SHADOW_CHANNEL_R
				return col.r;
				#endif
				return 1.0f;
			}

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TintColor;
			fixed _ShadowCutoff;

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				V2F_SHADOW_CASTER;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD1;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				TRANSFER_SHADOW_CASTER(o)
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.texcoord);
				fixed a = SelectShadowChannel(col);
				a = 2.0f * i.color.a * _TintColor.a * a;
				clip(a - _ShadowCutoff);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}