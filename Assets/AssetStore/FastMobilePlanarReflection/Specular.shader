Shader "SupGames/PlanarReflection/Specular" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture For Diffuse Material Color", 2D) = "white" {}
		_SpecColor("Specular Color", Color) = (1,1,1,1)
		_Shininess("Shininess", Range(0.01,50)) = 0.03
	}
		SubShader{
			Tags { "Glow" = "True" }
			Pass
				{
					Tags { "LightMode" = "ForwardBase" }
					CGPROGRAM

					#pragma vertex vert  
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest

					#include "UnityCG.cginc" 
					uniform fixed4 _LightColor0;
					uniform sampler2D _MainTex;
					uniform fixed4 _SpecColor;
					uniform fixed _Shininess;

					sampler2D _ReflectionTex;
					fixed _RefAlpha;
					fixed4 _MainTex_ST;
					fixed4 _Color;

					struct appdata {
						fixed4 vertex : POSITION;
						fixed3 normal : NORMAL;
						fixed4 uv : TEXCOORD0;
					};
					struct v2f {
						fixed4 pos : SV_POSITION;
						fixed4 uv : TEXCOORD0;
						fixed2 uv1 : TEXCOORD1;
						fixed3 diff : TEXCOORD2;
						fixed3 spec : TEXCOORD3;
					};

					v2f vert(appdata i)
					{
						v2f o;
						fixed3 normalDirection = normalize(mul(float4(i.normal, 0.0h), unity_WorldToObject).xyz);
						fixed3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
						o.diff = fixed4(UNITY_LIGHTMODEL_AMBIENT.rgb,0.0h) + _LightColor0.rgb * max(0.0h, dot(normalDirection, lightDirection));;
						o.spec = _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0h, dot(reflect(-lightDirection, normalDirection), normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, i.vertex).xyz))), _Shininess);;
						o.uv1 = TRANSFORM_TEX(i.uv, _MainTex);
						o.pos = UnityObjectToClipPos(i.vertex);
						o.uv = ComputeScreenPos(o.pos);
						return o;
					}

					fixed4 frag(v2f i) : COLOR
					{
						fixed4 color = tex2D(_MainTex, i.uv1);
						fixed4 reflection = tex2D(_ReflectionTex, i.uv.xy / i.uv.w);
						color = fixed4(i.spec * (1.0h - color.a) + i.diff * color.rgb, 1.0h);
						return (lerp(color, reflection, _RefAlpha) + lerp(reflection, color, 1 - _RefAlpha))*_Color / 2;
					}
					ENDCG
				}
		}
		Fallback "Specular"
}