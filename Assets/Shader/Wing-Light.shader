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

		_ColorIntensity("Color Intensity", Range(0, 1)) = 0.5
		_TimeSpeed("Time Speed", Range(0.01, 1)) = 0.5
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0

		// Without this, uv is not delivered properly. I think it's the law of the surface shader ...
		// If there are no uv properties in the Input structure among the properties, there is a problem that uvs are all passed to 0.
		// So I don't use it, but I will add it as it is hidden.
		[HideInInspector] _MainTex("Base (RGB)", 2D) = "white" {}
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

		Blend One One
		Cull [_Cull]
		ZWrite Off
		Fog {Mode Off}

		CGPROGRAM
		#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd keepalpha
		//#pragma vertex vert
		#pragma shader_feature_local _SHOWDUST

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		uniform float4 _TimeEditor;
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
		half _TimeSpeed;

		struct Input {
			float2 uv_MainTex : TEXCOORD0;
		};
		
		//void vert(inout appdata_base v, out Input o)
		//{
		//	UNITY_INITIALIZE_OUTPUT(Input, o);
		//}

		void surf(Input i, inout SurfaceOutput o)
		{
			float4 node_5205 = _Time.y * _TimeSpeed;
			float2 node_8473 = ((i.uv_MainTex*_wing_uv) + (node_5205.g*_wing_speed)*float2(-1, 0));
			float4 _wing_var = tex2D(_wing, TRANSFORM_TEX(node_8473, _wing));
			float3 node_3177 = (_wing_var.rgb*_wing_var.a*_wing_color.rgb);
			float4 _mask_var = tex2D(_mask, TRANSFORM_TEX(i.uv_MainTex, _mask));
			float2 node_9924 = ((i.uv_MainTex*_dust_UV) + (node_5205.g*_dust_speed)*float2(-1, 0));
			float4 _dust_var = tex2D(_dust, TRANSFORM_TEX(node_9924, _dust));
			#if _SHOWDUST
				float3 emissive = (((node_3177*_wing_intensity) + (_dust_var.rgb*_dust_intensity*_mask_var.rgb*_dust_color.rgb))*_mask_var.rgb);				
			#else
				float3 emissive = ((node_3177*_mask_var.rgb*_wing_intensity) + ((node_3177*_dust_var.rgb*_mask_var.rgb*_dust_intensity)*_dust_color.rgb));
			#endif
			o.Albedo = emissive * _ColorIntensity;
			o.Alpha = 1.0f;
		}
        ENDCG
    }
}