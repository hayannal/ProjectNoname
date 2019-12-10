// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "FrameworkPV/DiffuseRimNormal" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0)
		_RimNormalTex ("Rim Mask (Normal)", 2D) = "Bump" {}    //  Normal을 저장하고 있는 텍스처
		_RimColor ("Rim Color", Color) = (0.1, 0.3, 0.2)	// 유니티 에디터에서 직접 수정할 수 있도록 합니다.
		_RimPower ("Rim Power", Range(-1.0, 2)) = 1
		_RimDirAdjust ("Rim Dir Adjust", Vector) = (0, 0, 0, 0)

		[Toggle(_CUTOFF)] _UseCutoff("========== Use Cutoff (A) ==========", Float) = 0
		_Cutoff("Alpha cutoff", Range(0, 1)) = 0.5

		[Toggle(_EMISSIVE)] _UseEmissive("========== Use Emissive Updater (A) ==========", Float) = 0
		_EmissiveColor("Emissive Color", Color) = (1, 1, 1, 1)

		[Toggle(_HUE)] _UseHue("========== Use Hue ==========", Float) = 0
		_Hue ("Hue", Range(0.0, 360)) = 0

		[Toggle(_SELECTHUE)] _SelectHue("========== Select Hue Range ==========", Float) = 0
		_V_TA_HueSaturationLightness_Angle("Angle", Range(0, 1)) = 0
		_V_TA_HueSaturationLightness_Range("Range", Range(0, 1)) = 0.25

		[Toggle(_DISSOLVE)] _UseDissolve("========== Use Dissolve ==========", Float) = 0
		[HDR]_EdgeColor1 ("Edge Color", Color) = (1,1,1,1)
		_EdgeSize ("EdgeSize", Range(0,1)) = 0.2
		_Noise ("Noise", 2D) = "white" {}
		_EdgeCutoff ("Edge Cutoff", Range(0, 1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd
		#pragma multi_compile _ _DISSOLVE
		#pragma shader_feature _CUTOFF
		#pragma shader_feature _EMISSIVE
		#pragma shader_feature _HUE
		#pragma shader_feature _SELECTHUE

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// Properties와 동일한 변수명으로 설정합니다.
		sampler2D _MainTex;
		fixed4 _Color;
		sampler2D _RimNormalTex;
		float4 _RimColor;
      	float _RimPower;
      	float4 _RimDirAdjust;

#if _CUTOFF
		half _Cutoff;
#endif

#if _EMISSIVE
		fixed4 _EmissiveColor;
#endif

#if _HUE
		uniform float _Hue;
#endif

#if _SELECTHUE
		//Range( 0.0,  360.0) - default = 0
		float _V_TA_HueSaturationLightness_Angle;

		//Range( 0.0,  1.0) - default = 0.25
		float _V_TA_HueSaturationLightness_Range;
		float _V_TA_HueSaturationLightness_RangeMin;
		float _V_TA_HueSaturationLightness_RangeMax;
#endif

#if _DISSOLVE
		half4 _EdgeColor1;
		half _EdgeSize;
		sampler2D _Noise;
		half _EdgeCutoff;
#endif

		// Vertex를 연산할 때 필요한 데이터를 정합니다.
		// surf 함수로 호출될 때마다 해당 정점의 관련 데이터가 연산에 적용됩니다.
		struct Input {
			float2 uv_MainTex;	// uv는 해당 텍스처의 텍스처 좌표를 의미합니다.
			float2 uv_RimNormalTex;
			float3 viewDir;	// 관찰자의 위치를 향하는 방향 벡타입니다. Normal Vector와 내적하기 위해서 추가합니다.
#if _DISSOLVE
			half2 uv_Noise;
#endif
		};

#if _HUE
		inline float3 applyHue(float3 aColor)
		{
			float angle = radians(_Hue);
			float3 k = float3(0.57735, 0.57735, 0.57735);
			float cosAngle = cos(angle);

			return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
		}
#endif

#if _SELECTHUE
		float RGBtoHUE(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
			float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

			return abs(q.z + (q.w - q.y) / (6.0 * (q.x - min(q.w, q.y)) + 1.0e-10));
		}

		inline float3 SelectiveByRange(float3 _srcColor, float3 hsl)
		{
			//hue range value
			float h = RGBtoHUE(_srcColor.rgb);

			_V_TA_HueSaturationLightness_RangeMin = _V_TA_HueSaturationLightness_Angle - _V_TA_HueSaturationLightness_Range;
			_V_TA_HueSaturationLightness_RangeMax = _V_TA_HueSaturationLightness_Angle + _V_TA_HueSaturationLightness_Range;

			if (_V_TA_HueSaturationLightness_RangeMax > 1.0 && h < _V_TA_HueSaturationLightness_RangeMax - 1.0) h += 1.0;
			if (_V_TA_HueSaturationLightness_RangeMin < 0.0 && h > _V_TA_HueSaturationLightness_RangeMin + 1.0) h -= 1.0;

			float2 smoothStep = smoothstep(float2(_V_TA_HueSaturationLightness_Angle, _V_TA_HueSaturationLightness_RangeMin), float2(_V_TA_HueSaturationLightness_RangeMax, _V_TA_HueSaturationLightness_Angle), h);

			_srcColor.rgb = lerp(lerp(hsl.rgb, _srcColor.rgb, smoothStep.x), lerp(_srcColor.rgb, hsl.rgb, smoothStep.y), step(h, _V_TA_HueSaturationLightness_Angle));

			return _srcColor;
		}
#endif

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
#if _CUTOFF
			clip(c.a - _Cutoff);
#endif
			o.Albedo = c.rgb * _Color;
#if _HUE
	#if _SELECTHUE
			float3 hsl = applyHue(o.Albedo);
			o.Albedo = SelectiveByRange(o.Albedo, hsl);
	#else
			o.Albedo = applyHue(o.Albedo);
	#endif
#endif

#if _EMISSIVE
			o.Albedo = lerp(o.Albedo, _EmissiveColor.rgb, c.a * _EmissiveColor.a);
#endif

#if _DISSOLVE
			half Noise = tex2D(_Noise, IN.uv_Noise).r;
			Noise = lerp(0, 1, Noise);
			_EdgeCutoff = lerp(0, _EdgeCutoff + _EdgeSize, _EdgeCutoff);
			half Edge = smoothstep(_EdgeCutoff + _EdgeSize, _EdgeCutoff, clamp(Noise, _EdgeSize, 1));
			o.Emission = _EdgeColor1 * Edge;
			//o.Albedo += _EdgeColor1 * Edge;
			clip(Noise - _EdgeCutoff);
			//o.Albedo.r = saturate(o.Albedo.r);
			//o.Albedo.g = saturate(o.Albedo.g);
			//o.Albedo.b = saturate(o.Albedo.b);
#else
			float3 rimNormal = UnpackNormal(tex2D(_RimNormalTex, IN.uv_RimNormalTex));
			float3 vNormal = normalize(o.Normal + rimNormal);
			//_RimDirAdjust.xyz = mul(_RimDirAdjust, unity_WorldToObject).xyz;
			float3 viewNormal = normalize(IN.viewDir + _RimDirAdjust.xyz);
			half rim = 1.0 - dot(viewNormal, vNormal);
			o.Emission = _RimColor.rgb * saturate(rim - _RimPower);
			//o.Albedo += _RimColor.rgb * saturate(rim - _RimPower);
			//o.Albedo.r = saturate(o.Albedo.r);
			//o.Albedo.g = saturate(o.Albedo.g);
			//o.Albedo.b = saturate(o.Albedo.b);
#endif
		}
		ENDCG

		// shadow caster rendering pass, implemented manually
		// using macros from UnityCG.cginc
		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile _ _DISSOLVE
			#pragma shader_feature _CUTOFF
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

#if _CUTOFF
			half _Cutoff;
#endif

#if _DISSOLVE
			half _EdgeSize;
			sampler2D _Noise;
			half4 _Noise_ST;
			half _EdgeCutoff;
#endif

			struct v2f {
				V2F_SHADOW_CASTER;
#if _CUTOFF
				half2 uv_MainTex : TEXCOORD0;
#endif
#if _DISSOLVE
				half2 uv_Noise : TEXCOORD1;
#endif
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				TRANSFER_SHADOW_CASTER(o)
#if _CUTOFF
				o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
#endif
#if _DISSOLVE
				o.uv_Noise = TRANSFORM_TEX(v.texcoord, _Noise);
#endif
				return o;
			}

			fixed frag(v2f i) : SV_Target
			{
#if _CUTOFF
				fixed4 c = tex2D(_MainTex, i.uv_MainTex);
				clip(c.a - _Cutoff);
#endif
#if _DISSOLVE
				fixed Noise = tex2D(_Noise, i.uv_Noise).r;
				Noise = lerp(0, 1, Noise);
				_EdgeCutoff = lerp(0, _EdgeCutoff + _EdgeSize, _EdgeCutoff);
				clip(Noise - _EdgeCutoff);
#endif

				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}