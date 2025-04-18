Shader "FrameworkPV/Wing"
{
    Properties
    {
		_wing("wing", 2D) = "white" {}
		_wing_color("wing_color", Color) = (1,1,1,1)
		_wing_uv("wing_uv", Float) = 1
		_wing_intensity("wing_intensity", Float) = 1
		_wing_speed("wing_speed", Float) = 1
		_dust("dust", 2D) = "white" {}
		_dust_color("dust_color", Color) = (0.5,0.5,0.5,1)
		_dust_UV("dust_UV", Float) = 2
		_dust_intensity("dust_intensity", Float) = 1
		_dust_speed("dust_speed", Float) = 1
		_mask("mask", 2D) = "white" {}

		[Toggle(_SHOWDUST)] _UseShowDust("========== Show Dust ==========", Float) = 0

		_ColorIntensity("Color Intensity", Range(0, 5)) = 1.8
		_MenuColorIntensity("Menu Color Intensity", Range(0, 5)) = 1.7
		_LightIntensity("Light Intensity", Range(0, 1)) = 0.3
		_AmbientIntensity("Ambient Intensity", Range(0, 1)) = 0.2
		_TimeSpeed("Time Speed", Range(0.1, 2)) = 0.5

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
    }
    SubShader
    {
		Tags
		{
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
        LOD 200

		Blend [_SrcBlend] [_DstBlend]
		Cull [_Cull]
		ZWrite Off
		Fog {Mode Off}

		CGPROGRAM
		#pragma surface surf Custom exclude_path:prepass nolightmap noforwardadd keepalpha
		#pragma vertex vert
		#pragma shader_feature_local _SHOWDUST

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		uniform sampler2D _wing; uniform float4 _wing_ST;
		uniform sampler2D _mask; uniform float4 _mask_ST;
		uniform float _wing_intensity;
		uniform sampler2D _dust; uniform float4 _dust_ST;
		uniform float _dust_UV;
		uniform float _wing_speed;
		uniform float _dust_intensity;
		uniform float _dust_speed;
		uniform float4 _dust_color;
		uniform float4 _wing_color;
		uniform float _wing_uv;
		half _ColorIntensity;
		half _MenuColorIntensity;
		half _LightIntensity;
		half _AmbientIntensity;
		half _TimeSpeed;

		inline fixed4 CustomLight(SurfaceOutput s, UnityLight light)
		{
			// ignore s.Normal
			fixed diff = 0.5f;
			fixed4 c;
			c.rgb = s.Albedo * light.color * diff * _LightIntensity;
			c.a = s.Alpha;
			return c;
		}

		inline fixed4 LightingCustom(SurfaceOutput s, UnityGI gi)
		{
			fixed4 c;
			c = CustomLight(s, gi.light);
#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
			c.rgb += s.Albedo * gi.indirect.diffuse * _AmbientIntensity;
#endif
			return c;
		}

		inline void LightingCustom_GI(SurfaceOutput o, UnityGIInput data, inout UnityGI gi)
		{
			LightingLambert_GI(o, data, gi);
		}


		struct Input {
			// Values starting with "uv" are automatically handled internally, so if you need a custom value, never start with "uv".
			//float2 uv_MainTex : TEXCOORD0;
			float2 wingUV : TEXCOORD0;

			// for Menu
			float2 distanceRate : TEXCOORD1;
		};
		
		void vert(inout appdata_base v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.wingUV = v.texcoord;

			// for Menu
			float3 viewPosition = UnityObjectToViewPos(v.vertex);
			o.distanceRate.x = lerp(_ColorIntensity, _MenuColorIntensity, step(-25.0f, viewPosition.z));
			o.distanceRate.y = 0.0f;
		}

		void surf(Input i, inout SurfaceOutput o)
		{
			float node_5205 = _Time.y * _TimeSpeed;
			float2 node_8473 = ((i.wingUV*_wing_uv) + (node_5205*_wing_speed)*float2(-1, 0));
			float4 _wing_var = tex2D(_wing, TRANSFORM_TEX(node_8473, _wing));
			#if _SHOWDUST
				float3 node_3177 = (_wing_var.rgb*_wing_var.a*_wing_color.rgb);
			#else
				float3 node_3177 = (_wing_var.rgb*_wing_color.rgb);
			#endif
			float4 _mask_var = tex2D(_mask, TRANSFORM_TEX(i.wingUV, _mask));
			float2 node_9924 = ((i.wingUV*_dust_UV) + (node_5205*_dust_speed)*float2(-1, 0));
			float4 _dust_var = tex2D(_dust, TRANSFORM_TEX(node_9924, _dust));
			#if _SHOWDUST
				float3 emissive = (((node_3177*_wing_intensity) + (_dust_var.rgb*_dust_intensity*_mask_var.rgb*_dust_color.rgb))*_mask_var.rgb);
			#else
				float3 emissive = ((node_3177*_mask_var.rgb*_wing_intensity) + ((node_3177*_dust_var.rgb*_mask_var.rgb*_dust_intensity)*_dust_color.rgb));
			#endif
			o.Albedo = emissive * i.distanceRate.x;
			#if _SHOWDUST
				o.Alpha = _mask_var.r;
			#else
				o.Alpha = _wing_var.a * _mask_var.r;
			#endif
		}
        ENDCG
    }
}