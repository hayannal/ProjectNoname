// Shader targeted for low end devices. Single Pass Forward Toon Rendering.
//This shader is effective on Unity2018.2.
Shader "4U.withGame/LWRPToon18.2"
{
	Properties
	{
		[Enum(OFF,0,FRONT,1,BACK,2)] _Cull("Cull Mode", int) = 2
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Space]
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}
		[Space]
		[MaterialToggle]_KeepW("Keep White",int) = 0
		_HueC("Hue Change",Range(0, 1)) = 0
		[Space]
		_ShadowColor("Shadow Color", Color) = (0.7,0.7,0.7)
		_ShadeShift("Shade Shift", Range(-1, 1)) = 0
		[Space]
		[HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}//"ForceNoShadowCasting" = "True"   "RenderType"="TransparentCutout" "Queue" = "AlphaTest"
		LOD 300
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "LightweightForward" }
			
			Cull[_Cull]

			HLSLPROGRAM
			// Required to compile gles 3.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
			// -------------------------------------
			// Lightweight Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog
			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex LitPassVertexToon
			#pragma fragment LitPassFragmentToon

			#define LIGHTWEIGHT_SIMPLE_LIT_INPUT_INCLUDED
			#define LIGHTWEIGHT_INPUT_SURFACE_INCLUDED
			#define LIGHTWEIGHT_SIMPLE_LIT_PASS_INCLUDED
			#include "LWRP/ShaderLibrary/Lighting.hlsl"
			#include "LWRP/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			half4 _Color;
			float _HueC;
			half4 _EmissionColor;
			half _KeepW;
			half _Cutoff;
			half3 _ShadowColor;
			half _ShadeShift;

			CBUFFER_END

			
			TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
			TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);

			struct Attributes
			{
				float4 pos		    : POSITION;
				float3 normal		: NORMAL;
				float2 texcoord     : TEXCOORD0;
				float2 lightmapUV   : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings
			{
				float2 uv                       : TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				float3 posWS		            : TEXCOORD2;
				half3  normal                   : TEXCOORD3;
				half4 fogFactorAndVertexLight   : TEXCOORD4; // x: fogFactor, yzw: vertex light
				float4 shadowCoord              : TEXCOORD5;
				float4 positionCS               : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			///////////////////////////////////////////////////////////////////////////////
			//                  Vertex and Fragment functions                            //
			///////////////////////////////////////////////////////////////////////////////

			Varyings LitPassVertexToon(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				//VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);	//2018.3
				//VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);// , input.tangentOS);//2018.3
				
				output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

				output.posWS = TransformObjectToWorld(input.pos.xyz);	//2018.2
				//output.posWS = vertexInput.positionWS;	//2018.3
				output.positionCS = TransformWorldToHClip(output.posWS);	//2018.2
				//output.positionCS = vertexInput.positionCS;	//2018.3
				OUTPUT_NORMAL(input, output);	//2018.2
				//output.normal = normalInput.normalWS; //2018.3

				OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
				OUTPUT_SH(output.normal.xyz, output.vertexSH);
				half3 vertexLight = VertexLighting(output.posWS, output.normal);	//2018.2
				//half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);	//2018.3
				half fogFactor = ComputeFogFactor(output.positionCS.z);	//2018.2
				//half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);	//2018.3
				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
		#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
				output.shadowCoord = ComputeShadowCoord(output.positionCS);	//2018.2
				//output.shadowCoord = GetShadowCoord(vertexInput);	//2018.3
		#else
				output.shadowCoord = TransformWorldToShadowCoord(output.posWS.xyz);	//2018.2
				//output.shadowCoord = float4(0, 0, 0, 0);	//2018.3
		#endif
				return output;
			}

			half3 LightingToon(half3 lightAtten, half3 lightDir, half3 normal)
			{
				half halfLam = dot(normal, lightDir)*0.5 + 0.5;
				half shade = _ShadeShift * 0.5 + 0.5;
				return lerp(_ShadowColor,1,lightAtten *smoothstep(shade, shade+ 0.01,halfLam));
			}		
			inline half3 hueChange(half3 col, half hueC)
			{
				half3 rc = lerp(col, half3(col.g, col.g, col.b), smoothstep(0, 1, hueC));
				rc = lerp(rc, half3(col.g, col.b, col.b), smoothstep(1, 2, hueC));
				rc = lerp(rc, half3(col.g, col.b, col.r), smoothstep(2, 3, hueC));
				rc = lerp(rc, half3(col.b, col.b, col.r), smoothstep(3, 4, hueC));
				rc = lerp(rc, half3(col.b, col.r, col.r), smoothstep(4, 5, hueC));
				rc = lerp(rc, half3(col.b, col.r, col.g), smoothstep(5, 6, hueC));
				rc = lerp(rc, half3(col.r, col.r, col.g), smoothstep(6, 7, hueC));
				rc = lerp(rc, half3(col.r, col.g, col.g), smoothstep(7, 8, hueC));
				rc = lerp(rc, half3(col.r, col.g, col.b), smoothstep(8, 9, hueC));
				return rc;
			}
			
			half4 LitPassFragmentToon(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = input.uv;
				half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				clip(diffuseAlpha.a - _Cutoff);
				
				half3 diffuse = diffuseAlpha.rgb * _Color.rgb;
				diffuse = lerp(diffuse,lerp(diffuse,diffuseAlpha.rgb,diffuseAlpha.rgb),_KeepW);
				diffuse = hueChange(diffuse,_HueC*9);

				half3 normal=FragmentNormalWS(input.normal);
				//half3 normal = NormalizeNormalPerPixel(input.normal);
				half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
				
				Light mainLight = GetMainLight(); //(input.shadowCoord)2018.3
				mainLight.attenuation = MainLightRealtimeShadowAttenuation(input.shadowCoord); //2018.2
				MixRealtimeAndBakedGI(mainLight, normal, bakedGI, half4(0, 0, 0, 0));
				half3 attenuatedLight = mainLight.color*mainLight.attenuation;
				//half3 attenuatedLight = mainLight.color*mainLight.distanceAttenuation * mainLight.shadowAttenuation;	//2018.3
				half3 diffuseColor = bakedGI+LightingToon(attenuatedLight, mainLight.direction, normal);
	#ifdef _ADDITIONAL_LIGHTS
				int pixelLightCount = GetPixelLightCount(); //GetAdditionalLightsCount();	//2018.3
				for (int i = 0; i < pixelLightCount; ++i)
				{
					Light light = GetLight(i, input.posWS);// GetAdditionalLight(i, input.posWS);//2018.3
					light.attenuation *= LocalLightRealtimeShadowAttenuation(light.index, input.posWS); //2018.2
					half3 attenuatedLight = light.color*light.attenuation; //2018.2
					//half3 attenuatedLight = light.color*light.distanceAttenuation * light.shadowAttenuation; //2018.3
					diffuseColor += LightingToon(attenuatedLight, light.direction, normal);
				}
	#endif
	#ifdef _ADDITIONAL_LIGHTS_VERTEX
				diffuseColor += input.fogFactorAndVertexLight.yzw;
	#endif
				half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb*_EmissionColor.rgb;
				half3 color = diffuseColor * diffuse + emission;
				ApplyFog(color.rgb, input.fogFactorAndVertexLight.x); //2018.2
				//color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);	//2018.3
				return half4(color,1);
			}
			ENDHLSL
		}
		
		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}
			ZWrite On
			ZTest LEqual
			Cull[_Cull]
			HLSLPROGRAM
			// Required to compile gles 3.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment
			#define LIGHTWEIGHT_SIMPLE_LIT_INPUT_INCLUDED
			#define LIGHTWEIGHT_INPUT_SURFACE_INCLUDED
			#define LIGHTWEIGHT_SHADOW_CASTER_PASS_INCLUDED
			#include "LWRP/ShaderLibrary/Core.hlsl"
			float4 _ShadowBias; // x: depth bias, y: normal bias
			float3 _LightDirection;
			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
			};
			float4 GetShadowPositionHClip(Attributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldDir(input.normalOS);
				float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
				float scale = invNdotL * _ShadowBias.y;
				// normal bias is negative since we want to apply an inset normal offset
				positionWS = _LightDirection * _ShadowBias.xxx + positionWS;
				positionWS = normalWS * scale.xxx + positionWS;
				float4 positionCS = TransformWorldToHClip(positionWS);

			#if UNITY_REVERSED_Z
				positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
			#else
				positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
			#endif

				return positionCS;
			}
			Varyings ShadowPassVertex(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);

				output.positionCS = GetShadowPositionHClip(input);
				return output;
			}
			half4 ShadowPassFragment(Varyings input) : SV_TARGET
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}
			ZWrite On
			ColorMask 0
			Cull[_Cull]
			HLSLPROGRAM
			// Required to compile gles 3.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment
			#pragma multi_compile_instancing
			#define LIGHTWEIGHT_SIMPLE_LIT_INPUT_INCLUDED
			#define LIGHTWEIGHT_INPUT_SURFACE_INCLUDED
			#define LIGHTWEIGHT_DEPTH_ONLY_PASS_INCLUDED
			#include "LWRP/ShaderLibrary/Core.hlsl"
			struct Attributes
			{
				float4 position     : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			Varyings DepthOnlyVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.position.xyz);
				return output;
			}
			half4 DepthOnlyFragment(Varyings input) : SV_TARGET
			{
				return 0;
			}
			ENDHLSL
		}

		// This pass it not used during regular rendering, only for lightmap baking.
		Pass
		{
			Name "Meta"
			Tags{ "LightMode" = "Meta" }
			Cull Off
			HLSLPROGRAM
			// Required to compile gles 3.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
			#pragma vertex LightweightVertexMeta
			#pragma fragment LightweightFragmentMetaToon
			#define LIGHTWEIGHT_SIMPLE_LIT_INPUT_INCLUDED
			#define LIGHTWEIGHT_INPUT_SURFACE_INCLUDED
			#define LIGHTWEIGHT_LIT_META_PASS_INCLUDED
			#include "LWRP/ShaderLibrary/Lighting.hlsl"
			CBUFFER_START(UnityMetaPass)
			// x = use uv1 as raster position
			// y = use uv2 as raster position
			bool4 unity_MetaVertexControl;
			// x = return albedo
			// y = return normal
			bool4 unity_MetaFragmentControl;
			float4 _MainTex_ST;
			half3 _Color;
			half3 _EmissionColor;
			CBUFFER_END
			float unity_OneOverOutputBoost;
			float unity_MaxOutputValue;
			float unity_UseLinearSpace;
			TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
			TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
			struct MetaInput
			{
				half3 Albedo;
				half3 Emission;
			};
			struct Attributes
			{
				float4 positionOS   : POSITION;
				half3  normalOS     : NORMAL;
				float2 uv           : TEXCOORD0;
				float2 uvLM         : TEXCOORD1;
				float2 uvDLM        : TEXCOORD2;
			};
			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				float2 uv           : TEXCOORD0;
			};
			float4 MetaVertexPosition(float4 positionOS, float2 uvLM, float2 uvDLM, float4 lightmapST)
			{
				if (unity_MetaVertexControl.x)
				{
					positionOS.xy = uvLM * lightmapST.xy + lightmapST.zw;
					// OpenGL right now needs to actually use incoming vertex position,
					// so use it in a very dummy way
					positionOS.z = positionOS.z > 0 ? REAL_MIN : 0.0f;
				}
				return TransformObjectToHClip(positionOS.xyz);
			}
			half4 MetaFragment(MetaInput input)
			{
				half4 res = 0;
				if (unity_MetaFragmentControl.x)
				{
					res = half4(input.Albedo, 1);

					// d3d9 shader compiler doesn't like NaNs and infinity.
					unity_OneOverOutputBoost = saturate(unity_OneOverOutputBoost);

					// Apply Albedo Boost from LightmapSettings.
					res.rgb = clamp(PositivePow(res.rgb, unity_OneOverOutputBoost), 0, unity_MaxOutputValue);
				}
				if (unity_MetaFragmentControl.y)
				{
					res = half4(input.Emission, 1.0);
				}
				return res;
			}
			Varyings LightweightVertexMeta(Attributes input)
			{
				Varyings output;
				output.positionCS = MetaVertexPosition(input.positionOS, input.uvLM, input.uvDLM, unity_LightmapST);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}
			half4 LightweightFragmentMetaToon(Varyings input) : SV_Target
			{
				float2 uv = input.uv;
				MetaInput metaInput;
				metaInput.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
				metaInput.Emission = _EmissionColor.rgb*SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap,uv).rgb;

				return MetaFragment(metaInput);
			}
			ENDHLSL
		}
	}
Fallback "Hidden/InternalErrorShader"
}
