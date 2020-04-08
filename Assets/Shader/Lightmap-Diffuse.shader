// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "FrameworkNG/Lightmap/Diffuse"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_MaskTex ("Emissive (R) MatCap (G) Cutoff (B)", 2D) = "white" {}
		[Toggle(_NORMAL)] _UseNormal("========== Use NormalMap ==========", Float) = 0
		_NormalMap ("Normal Map", 2D) = "bump" {}
		[Toggle(_CUTOFF)] _UseCutoff("========== Use Cutoff ==========", Float) = 0
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
		[KeywordEnum(None, Single, Dual)] _MatCap("========== Use MatCap ==========", Float) = 0
		_MatCapTex ("MatCap (RGB)", 2D) = "white" {}
		_MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1.0
		[Toggle(_EMISSIVE)] _UseEmissive("======== Use Emissive (Updater) ========", Float) = 0
		_EmissiveColor ("Emissive Color", Color) = (1, 1, 1, 1)
		[KeywordEnum(None, Updater, VertexCos)] _Wind("========== Use Wind ==========", Float) = 0
		_WindParameter ("Axis (XYZ) Frequency (W)", Vector) = (0.15, 0, 0.15, 1.75)
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
//		cull Off
		
		CGPROGRAM

		#pragma exclude_renderers d3d11 xbox360
		#pragma surface surf Lambert exclude_path:deferred exclude_path:prepass nodynlightmap nodirlightmap nometa noforwardadd interpolateview approxview halfasview
		#pragma vertex vert
		#pragma shader_feature _NORMAL
		#pragma shader_feature _CUTOFF
		#pragma shader_feature _MATCAP_NONE _MATCAP_SINGLE _MATCAP_DUAL
		#pragma shader_feature _EMISSIVE
		#pragma shader_feature _WIND_NONE _WIND_UPDATER _WIND_VERTEXCOS

		//#include "NGCore.cginc"
#ifndef NG_CORE_INCLUDED
#define NG_CORE_INCLUDED

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Channel

// 많아지면 2의 승수만큼 느려지기 때문에 Flow에만 사용하는거로 하겠다.
inline fixed SelectFlowChannel(fixed3 mask)
{
#if _FLOW_CHANNEL_R
	return mask.r;
#elif _FLOW_CHANNEL_G
	return mask.g;
#elif _FLOW_CHANNEL_B
	return mask.b;
#endif
	return 1.0f;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#define NG_MATCAP_UV	\
	float3 worldNorm = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z);	\
	worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);	\
	o.matcapUV = worldNorm.xy * 0.5 + 0.5;

#define NG_MATCAP_SINGLE	\
	fixed3 mc = tex2D(_MatCapTex, IN.matcapUV);	\
	mc = lerp(0.5f, mc, (mask.g * _MatCapIntensity));	\
	c *= mc * 2.0f;

#define NG_MATCAP_DUAL	\
	IN.matcapUV.x *= 0.5f;	\
	fixed3 mc1 = tex2D(_MatCapTex, IN.matcapUV);	\
	IN.matcapUV.x += 0.5f;	\
	fixed3 mc2 = tex2D(_MatCapTex, IN.matcapUV);	\
	mc1 = lerp(mc1, mc2, (mask.g * _MatCapIntensity));	\
	c *= mc1 * 2.0f;

#define NG_CUTOFF	\
	clip(mask.b - _Cutoff);

#define NG_EMISSIVE	\
	c.rgb = lerp(c.rgb, _EmissiveColor.rgb, mask.r * _EmissiveColor.a);

#define NG_RIMLIGHT	\
	half rim = 1.0f - saturate(dot(normalize(IN.viewDir), o.Normal));	\
	o.Emission = _RimColor.rgb * saturate(rim - _RimPower);

#define NG_NORMAL	\
	o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));

#define NG_FLOW_UV	\
	o.flowUV = TRANSFORM_TEX(v.texcoord, _FlowTex) + _FlowSpeed.xy * _Time.y;

#define NG_FLOW(flowMask)	\
	fixed3 flow = tex2D(_FlowTex, IN.flowUV).rgb * _FlowPower;	\
	c.rgb += flow * flowMask;

#define NG_WIND_UPDATER	\
	v.vertex.xyz += _WindParameterUpdater.xyz * v.color.r;

#define NG_WIND_VERTEX_COS	\
	float t = (_Time.y + dot(v.vertex.xz, _WindParameter.xz)) * _WindParameter.w;	\
	float wave = cos(t) * v.color.r;	\
	v.vertex.xyz += _WindParameter.xyz * wave;

#endif // NG_CORE_INCLUDED

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		
		sampler2D _MainTex;
		#if _CUTOFF || _MATCAP_SINGLE || _MATCAP_DUAL || _EMISSIVE
			sampler2D _MaskTex;
		#endif
		#if _NORMAL
			sampler2D _NormalMap;
		#endif
		#if _CUTOFF
			fixed _Cutoff;
		#endif
		#if _MATCAP_SINGLE || _MATCAP_DUAL
			sampler2D _MatCapTex;
			half _MatCapIntensity;
		#endif
		#if _EMISSIVE
			fixed4 _EmissiveColor;
		#endif
		#if _WIND_UPDATER
			uniform half4 _WindParameterUpdater;
		#elif _WIND_VERTEXCOS
			half4 _WindParameter;
		#endif
		
		struct Input
		{
			half2 uv_MainTex : TEXCOORD0;
			#if _MATCAP_SINGLE || _MATCAP_DUAL
				half2 matcapUV;
			#endif
		};
		
		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);

			// 이거랑 viewDir은 다른거다.
			// viewDir은 월드상의 뷰벡터를 오브젝트 공간으로 변환시킨거고
			// matcapUV는 모델의 노말을 카메라 공간으로 변환시켜 uv로 사용하는거다.
			// viewDir은 Sphere를 화면 구석에 옮겨두어도 정확하게 테두리에 림이 먹지만
			// matcapUV는 Sphere를 화면 구석에 놓아둘 경우 halfasview처럼 림의 방향이 틀어지게 된다.
			// 즉 환경맵처럼 쓰기엔 matcap이 좋지만 림은 viewDir을 사용해야 제대로 나온다.
			//
			// uv from normal in view space
			//o.matcapUV = half2(dot(UNITY_MATRIX_IT_MV[0].xyz, v.normal), dot(UNITY_MATRIX_IT_MV[1].xyz, v.normal)) * 0.5 + 0.5;

			#if _MATCAP_SINGLE || _MATCAP_DUAL
				NG_MATCAP_UV;
			#endif

			#if _WIND_UPDATER
				NG_WIND_UPDATER;
			#elif _WIND_VERTEXCOS
				NG_WIND_VERTEX_COS;
			#endif
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed3 c = tex2D(_MainTex, IN.uv_MainTex).rgb;
			#if _CUTOFF || _MATCAP_SINGLE || _MATCAP_DUAL || _EMISSIVE
				fixed3 mask = tex2D(_MaskTex, IN.uv_MainTex).rgb;
			#endif

			#if _CUTOFF
				NG_CUTOFF;
			#endif

			#if _MATCAP_SINGLE
				NG_MATCAP_SINGLE;
			#elif _MATCAP_DUAL
				NG_MATCAP_DUAL;
			#endif

			#if _EMISSIVE
				NG_EMISSIVE;
			#endif

			o.Albedo = c;
			o.Alpha = 1.0f;

			#if _NORMAL
				NG_NORMAL;
			#endif
		}
		ENDCG

		// Pass to render object as a shadow caster
		UsePass "Mobile/VertexLit/SHADOWCASTER"
	}
	
	Fallback "Diffuse"
}
