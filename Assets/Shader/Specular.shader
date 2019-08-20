// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "FrameworkNG/Specular"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ColorIntensity ("Color Intensity", Range(0, 20)) = 1.0
		_MaskTex ("Emissive (R) MatCap (G) Cutoff (B)", 2D) = "white" {}
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_SpecPower ("Specular Intensity", Range(0.0, 2)) = 1
		_Shininess ("Shininess", Range(0.01, 1)) = 0.078125
		[Toggle(_NORMAL)] _UseNormal("========== Use NormalMap ==========", Float) = 0
		_NormalMap ("Normal Map", 2D) = "bump" {}
		[Toggle(_RIMLIGHT)] _UseRimLight("========== Use RimLight ==========", Float) = 0
		_RimColor ("Rim Color", Color) = (0.26, 0.19, 0.16, 0.0)
		_RimPower ("Rim Intensity", Range(-1, 1)) = 0.0
		[Toggle(_CUTOFF)] _UseCutoff("========== Use Cutoff ==========", Float) = 0
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
		[KeywordEnum(None, Single, Dual)] _MatCap("========== Use MatCap ==========", Float) = 0
		_MatCapTex ("MatCap (RGB)", 2D) = "white" {}
		[Toggle(_EMISSIVE)] _UseEmissive("======== Use Emissive (Updater) ========", Float) = 0
		_EmissiveColor ("Emissive Color", Color) = (1, 1, 1, 1)
		[Toggle(_FLOW)] _UseFlow("========== Use Flow ==========", Float) = 0
		_FlowTex ("Flow (RGB)", 2D) = "white" {}
		[KeywordEnum(R,G,B,None)] _Flow_Channel ("Flow Mask Channel", Float) = 0
		_FlowSpeed ("Flow Speed (UV)", Vector) = (0, 0, 0, 0)
		_FlowPower ("Flow Intensity", Float) = 1
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
//		cull Off
		
		CGPROGRAM

		#pragma target 3.0
		#pragma exclude_renderers xbox360
		#pragma surface surf BlinnPhong exclude_path:deferred exclude_path:prepass nolightmap noforwardadd interpolateview approxview
		#pragma vertex vert
		#pragma shader_feature _NORMAL
		#pragma shader_feature _RIMLIGHT
		#pragma shader_feature _CUTOFF
		#pragma shader_feature _MATCAP_NONE _MATCAP_SINGLE _MATCAP_DUAL
		#pragma shader_feature _EMISSIVE
		#pragma shader_feature _FLOW
		#pragma shader_feature _FLOW_CHANNEL_R _FLOW_CHANNEL_G _FLOW_CHANNEL_B _FLOW_CHANNEL_NONE

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
	mc = lerp(0.5f, mc, mask.g);	\
	c *= mc * 2.0f;

#define NG_MATCAP_DUAL	\
	IN.matcapUV.x *= 0.5f;	\
	fixed3 mc1 = tex2D(_MatCapTex, IN.matcapUV);	\
	IN.matcapUV.x += 0.5f;	\
	fixed3 mc2 = tex2D(_MatCapTex, IN.matcapUV);	\
	mc1 = lerp(mc1, mc2, mask.g);	\
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


		inline fixed4 LightingMobileBlinnPhong(SurfaceOutput s, half3 lightDir, fixed3 halfDir, fixed atten)
		{
			fixed diff = max(0, dot(s.Normal, lightDir));
			fixed nh = max(0, dot(s.Normal, halfDir));
			fixed spec = pow(nh, s.Specular * 128) * s.Gloss;

			fixed4 c;
			c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * atten;
			c.a = s.Alpha;
			return c;
		}
		
		sampler2D _MainTex;
		half _ColorIntensity;
		#if _CUTOFF || _MATCAP_SINGLE || _MATCAP_DUAL || _EMISSIVE || _FLOW_CHANNEL_R || _FLOW_CHANNEL_G || _FLOW_CHANNEL_B
			sampler2D _MaskTex;
		#endif
		half _Shininess;
		fixed _SpecPower;
		#if _NORMAL
			sampler2D _NormalMap;
		#endif
		#if _RIMLIGHT
			fixed4 _RimColor;
			half _RimPower;
		#endif
		#if _CUTOFF
			fixed _Cutoff;
		#endif
		#if _MATCAP_SINGLE || _MATCAP_DUAL
			sampler2D _MatCapTex;
		#endif
		#if _EMISSIVE
			fixed4 _EmissiveColor;
		#endif
		#if _FLOW
			sampler2D _FlowTex;
			half4 _FlowTex_ST;
			half4 _FlowSpeed;
			half _FlowPower;
		#endif
		
		struct Input
		{
			half2 uv_MainTex : TEXCOORD0;
			#if _RIMLIGHT
				half3 viewDir;	// objSpaceViewDir
			#endif
			#if _MATCAP_SINGLE || _MATCAP_DUAL
				half2 matcapUV;
			#endif
			#if _FLOW
				half2 flowUV;
			#endif
		};
		
		void vert (inout appdata_tan v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);

			#if _MATCAP_SINGLE || _MATCAP_DUAL
				NG_MATCAP_UV;
			#endif
			#if _FLOW
				NG_FLOW_UV;
			#endif
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed3 c = tex2D(_MainTex, IN.uv_MainTex).rgb;
			#if _CUTOFF || _MATCAP_SINGLE || _MATCAP_DUAL || _EMISSIVE || _FLOW_CHANNEL_R || _FLOW_CHANNEL_G || _FLOW_CHANNEL_B
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

			#if _FLOW
				fixed flowMask = 1;
				#if _FLOW_CHANNEL_R || _FLOW_CHANNEL_G || _FLOW_CHANNEL_B
					flowMask = SelectFlowChannel(mask);
				#endif
				NG_FLOW(flowMask);
			#endif

			o.Albedo = c * _ColorIntensity;
			o.Alpha = 1.0f;
			o.Specular = _Shininess;
			o.Gloss = 1.0f * _SpecPower;

			#if _RIMLIGHT
				NG_RIMLIGHT;
			#endif

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
