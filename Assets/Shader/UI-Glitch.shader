// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FrameworkNG/UI/Glitch"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		[Toggle(_JITTER)] _UseJitter("========== Use Jitter ==========", Float) = 0
		_ScanLineJitter ("Jitter (disp, thresh)", Vector) = (0, 0, 0, 0)
		[Toggle(_SHAKE)] _UseShakke("========== Use Shake ==========", Float) = 0
		_HorizontalShake ("Shake", Float) = 0
		[Toggle(_COLORDRIFT)] _UseColorDrift("========== Use Color Drift ==========", Float) = 0
		_ColorDrift ("Color Drift (amount, time)", Vector) = (0, 0, 0, 0)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			#pragma shader_feature _JITTER
			#pragma shader_feature _SHAKE
			#pragma shader_feature _COLORDRIFT
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			#if _JITTER
			float2 _ScanLineJitter;	// (displacement, threshold)
			#endif
			#if _SHAKE
			float _HorizontalShake;
			#endif
			#if _COLORDRIFT
			float2 _ColorDrift;	// (amount, time)
			#endif

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
				#endif
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;

			float nrand(float x, float y)
			{
				return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				#if _JITTER || _SHAKE || _COLORDRIFT
				float u = IN.texcoord.x;
				float v = IN.texcoord.y;
				#endif
				
				#if _JITTER
				// Scan line jitter
				float jitter = nrand(v, _Time.x) * 2 - 1;
				jitter *= step(_ScanLineJitter.y, abs(jitter)) * _ScanLineJitter.x;
				u += jitter;
				#endif
				#if _SHAKE
				// Horizontal shake
				float shake = (nrand(_Time.x, 2) - 0.5) * _HorizontalShake;
				u += shake;
				#endif
				#if _COLORDRIFT
				// Color drift
				float drift = sin(v + _ColorDrift.y) * _ColorDrift.x;
				#endif

				#if _COLORDRIFT
				half4 src1 = tex2D(_MainTex, frac(float2(u, v)));
				u += drift;
				half4 src2 = tex2D(_MainTex, frac(float2(u, v)));
				half4 color = half4(src1.r, src2.g, src1.b, (src1.a + src2.a) * 0.5f);
				color = (color + _TextureSampleAdd) * IN.color;
				#elif _JITTER || _SHAKE
				half4 color = (tex2D(_MainTex, frac(float2(u, v))) + _TextureSampleAdd) * IN.color;
				#else
				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				#endif

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
}
