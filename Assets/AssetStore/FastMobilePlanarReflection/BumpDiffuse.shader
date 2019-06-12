Shader "SupGames/PlanarReflection/Bumped Diffuse"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Main Texture", 2D) = "white" {}
		_BumpTex("Normal Map", 2D) = "bump" {}
		_Distort("Distort Amount", Range(0.01,50)) = 1
	}
	SubShader{
		Tags {"RenderType" = "Opaque"}
		LOD 100
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			# include "UnityCG.cginc"

			sampler2D _ReflectionTex;
			sampler2D _MainTex;
			sampler2D _BumpTex;
			fixed _RefAlpha;
			fixed _Distort;
			fixed4 _MainTex_ST;
			fixed4 _BumpTex_ST;
			fixed4 _Color;
			fixed _ReflectionAlpha;
			fixed4 _LightColor0;

			
			struct input
			{
				fixed4 pos : POSITION;
				fixed4 uv : TEXCOORD0;
				fixed3 normal : NORMAL;
				fixed4 tangent : TANGENT;
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed4 uv : TEXCOORD0;
				fixed2 uv1 : TEXCOORD1;
				fixed3 tangent : TEXCOORD2;
				fixed3 normal : TEXCOORD3;
				fixed3 binormal : TEXCOORD4;
				fixed4 posWorld : TEXCOORD5;
			};

			v2f vert(input i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv = ComputeScreenPos(o.pos);
				o.uv1 = TRANSFORM_TEX(i.uv, _MainTex);
				o.tangent = normalize(mul(unity_ObjectToWorld, fixed4(i.tangent.xyz, 0.0)).xyz);
				o.normal = normalize(mul(fixed4(i.normal, 0.0h), unity_WorldToObject).xyz);
				o.binormal = normalize(cross(o.normal, o.tangent)* i.tangent.w);
				o.posWorld = mul(unity_ObjectToWorld, i.pos);
				return o;
			}

			fixed4 frag(v2f i) : COLOR {

				fixed4 encodedNormal = tex2D(_BumpTex, _BumpTex_ST.xy * i.uv1 + _BumpTex_ST.zw);
				fixed3 localCoords = fixed3(2.0h * encodedNormal.a - 1.0h, 2.0h * encodedNormal.g - 1.0h, 0.0h);
				localCoords.z = sqrt(1.0h - dot(localCoords, localCoords));

				fixed3x3 local2WorldTranspose = fixed3x3(i.tangent, i.binormal, i.normal);
				fixed3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));

				fixed3 viewDirection = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
				fixed3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				fixed tmpDot = max(0.0h, dot(normalDirection, lightDirection));

				fixed4 color = tex2D(_MainTex, i.uv1)*fixed4(UNITY_LIGHTMODEL_AMBIENT.rgb + _LightColor0.rgb * tmpDot,1.0h);
				fixed4 bump = tex2D(_BumpTex, i.uv1)*_Distort;
				i.uv.x += bump - 0.5h*_Distort;
				i.uv.y += 0.5h*_Distort - bump;
				fixed4 reflection = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i.uv));
				return (lerp(color, reflection, _RefAlpha) + lerp(reflection, color, 1 - _RefAlpha))*_Color / 2;
			}
			ENDCG
		}
	}
}