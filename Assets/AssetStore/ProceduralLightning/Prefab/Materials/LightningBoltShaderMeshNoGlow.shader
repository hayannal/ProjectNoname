﻿Shader "Custom/LightningBoltShaderMesh"
{
	Properties
	{
		_MainTex ("Main Texture (RGBA)", 2D) = "white" {}
		_TintColor ("Tint Color (RGB)", Color) = (1, 1, 1, 1)
		_JitterMultiplier ("Jitter Multiplier (Float)", Float) = 0.0
		_Turbulence ("Turbulence (Float)", Float) = 0.0
		_TurbulenceVelocity ("Turbulence Velocity (Vector)", Vector) = (0, 0, 0, 0)
		_SrcBlendMode("SrcBlendMode (Source Blend Mode)", Int) = 5 // SrcAlpha
		_DstBlendMode("DstBlendMode (Destination Blend Mode)", Int) = 1 // One, change to 10 for alpha blend instead of additive blend
    }

    SubShader
	{
		Tags { "Queue" = "Transparent+1" }
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest LEqual
		ColorMask RGBA
		Blend [_SrcBlendMode] [_DstBlendMode]

		CGINCLUDE
		
		#include "UnityCG.cginc"

		#pragma vertex vert
        #pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma glsl_no_auto_normalization
		#pragma multi_compile_particles
		#pragma multi_compile __ PERSPECTIVE
		#pragma multi_compile __ ORTHOGRAPHIC_XY
		#pragma multi_compile __ ORTHOGRAPHIC_XZ
		#pragma multi_compile __ INTENSITY_FLICKER

		float4 _LightningTime;
		float _JitterMultiplier;
		float _Turbulence;
		float4 _TurbulenceVelocity;
		float4 _IntensityFlicker;
		sampler2D _IntensityFlickerTexture;

		struct appdata_t
		{
			float4 vertex : POSITION;
			float4 dir : TANGENT;
			float4 color : COLOR;
			float3 dir2 : NORMAL;
			
#if UNITY_VERSION <= 529

			float2 texcoord : TEXCOORD0;
			float2 glowModifiers : TEXCOORD1;
			
#if UNITY_VERSION >= 500

			float2 fadeXY : TEXCOORD2;
			float2 fadeZW : TEXCOORD3;
			
#endif

#else

			float4 texcoord : TEXCOORD0;
			float4 fadeLifetime : TEXCOORD1;

#endif

		};

        struct v2f
        {
            float2 texcoord : TEXCOORD0;
            fixed4 color : COLOR0;
            float4 pos : SV_POSITION;
        };

		inline float rand3(float3 pos) { return frac(sin(dot(_LightningTime.xyz * pos, float3(12.9898, 78.233, 45.5432))) * 43758.5453); }

		// float rand(float n) { return frac(sin(n) * 43758.5453123); }

		// float noise(float p) { float fl = floor(p); float fc = frac(p); return lerp(rand(fl), rand(fl + 1.0), fc); }

		inline float lerpColor(float4 c)
		{
			// the vertex will fade in, stay at full color, then fade out
			// r = start time
			// g = peak start time
			// b = peak end time
			// a = end time

			// debug
			// return 1;

			float t = _LightningTime.y;
			float lerpMultiplier = (t < c.g);
			float lerpIn = lerp(0, 1, saturate((t - c.r) / max(0.00001, 0.00001 + c.g - c.r)));
			float lerpOut = lerp(1, 0, saturate((t - c.b) / max(0.00001, c.a - c.b)));

#if defined(INTENSITY_FLICKER)

			//float n = lerp(_IntensityFlicker.x, _IntensityFlicker.y, noise((c.x + _LightningTime.y) * )) + _IntensityFlicker.w;
			float n = tex2Dlod(_IntensityFlickerTexture, float4((_LightningTime.y * _IntensityFlicker.z) + c.x, c.x, 0.0, 0.0)).a + _IntensityFlicker.w;
			n = lerp(_IntensityFlicker.x, _IntensityFlicker.y, n);

#endif

			return saturate((((lerpMultiplier * lerpIn) + ((1.0 - lerpMultiplier) * lerpOut)))
			
#if defined(INTENSITY_FLICKER)

				* n

#endif
			
			);

			/*
			float t = _LightningTime.y;
			if (t < c.g)
			{
				return lerp(0.0, 1.0, ((t - c.r) / (c.g - c.r)));
			}
			return lerp(1.0, 0.0, max(0, ((t - c.b) / (c.a - c.b))));
			*/
		}

		inline fixed4 fragMethod(sampler2D tex, v2f i)
		{
			fixed4 c = tex2D(tex, i.texcoord);
			return (c * i.color);
		}

		ENDCG

		// line pass
		Pass
		{
			Name "LinePass"
			LOD 100

            CGPROGRAM

			fixed4 _TintColor; 
            float4 _MainTex_ST;
 			sampler2D _MainTex;

            v2f vert(appdata_t v)
            {
                v2f o;

				// face the camera
				float4 worldPos = v.vertex;
				float dirModifier = (v.texcoord.x - 0.5) + (v.texcoord.x - 0.5);
				float jitter = 1.0 + (rand3(worldPos) * _JitterMultiplier);
				float t = _LightningTime.y;
				
#if UNITY_VERSION <= 529

#if UNITY_VERSION >= 500

				float elapsed = (t - v.fadeXY.x) / (v.fadeZW.y - v.fadeXY.x);
				
#else
				
				float elapsed = 0.1f;

#endif

#else

				float elapsed = (t - v.fadeLifetime.r) / (v.fadeLifetime.a - v.fadeLifetime.r);
				
#endif

				float turbulence = lerp(0.0f, _Turbulence / max(0.5, abs(v.dir.w)), elapsed);
				float4 turbulenceVelocity = lerp(float4(0, 0, 0, 0), _TurbulenceVelocity, elapsed);
				
#if UNITY_VERSION < 500 || defined(PERSPECTIVE)

				float4 turbulenceDirection = turbulenceVelocity + (float4(normalize(v.dir.xyz), 0) * turbulence);
				float3 directionToCamera = (_WorldSpaceCameraPos - worldPos);
				float3 tangent = cross(v.dir.xyz, directionToCamera);
				float4 offset = float4(normalize(tangent) * v.dir.w, 0);
				o.pos = UnityObjectToClipPos(worldPos + (offset * jitter) + turbulenceDirection);

#elif defined(ORTHOGRAPHIC_XY)
								
				float4 turbulenceDirection = float4(turbulenceVelocity.xy, 0, 0) + (float4(normalize(v.dir).xy, 0, 0) * turbulence);
				float2 tangent = normalize(float2(-v.dir.y, v.dir.x));
				float4 offset = float4(tangent * v.dir.w, 0, 0);
				o.pos = UnityObjectToClipPos(worldPos + (offset * jitter) + turbulenceDirection);

#else

				float2 turbulenceDirection = (turbulenceVelocity.xz + normalize(v.dir.xz)) * turbulence;
				float4 turbulenceDirection4 = float4(turbulenceDirection.x, 0.0f, turbulenceDirection.y, 0.0f);
				float2 tangent = normalize(float2(-v.dir.z, v.dir.x));
				tangent *= v.dir.w;
				float4 offset = float4(tangent.x, 0.0f, tangent.y, 0.0f);
				o.pos = UnityObjectToClipPos(worldPos + (offset * jitter) + turbulenceDirection4);
				
#endif
				
				o.texcoord = v.texcoord.xy;
				
#if UNITY_VERSION < 500

				o.color = _TintColor * fixed4(v.color.rgb, 1);

#elif UNITY_VERSION <= 529

                o.color = (lerpColor(float4(v.fadeXY.x, v.fadeXY.y, v.fadeZW.x, v.fadeZW.y)) * _TintColor * fixed4(v.color.rgb, 1));
                
#else

				o.color = (lerpColor(v.fadeLifetime) * _TintColor * fixed4(v.color.rgb, 1));
                
#endif

				// intensity is divided by 10 when passed in
				o.color.a *= v.color.a * 10.0;
                return o; 
            }
			
            fixed4 frag(v2f i) : SV_Target
			{
				return fragMethod(_MainTex, i);
            }
            ENDCG
        }
    }
 
    Fallback Off
}