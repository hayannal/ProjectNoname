Shader "SupGames/PlanarReflection/Diffuse" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_AmbientColor("Ambient Color", Color) = (0,0,0,0)
		_MainTex("Texture For Diffuse Material Color", 2D) = "white" {}
	}
		SubShader{
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

					sampler2D _ReflectionTex;
					fixed _RefAlpha;
					fixed4 _MainTex_ST;
					fixed4 _Color;
					fixed4 _AmbientColor;

					struct appdata {
						fixed4 vertex : POSITION;
						fixed3 normal : NORMAL;
						fixed4 uv : TEXCOORD0;
					};
					struct v2f {
						fixed4 pos : SV_POSITION;
						fixed4 uv : TEXCOORD0;
						fixed2 uv1 : TEXCOORD1;
						fixed4 ref : TEXTCOORD2;

					};

					v2f vert(appdata i)
					{
						v2f o;
						fixed3 normalDirection = normalize(mul(fixed4(i.normal, 0.0h), unity_WorldToObject).xyz);
						fixed3 diffuseReflection = UNITY_LIGHTMODEL_AMBIENT.rgb + _LightColor0.rgb * max(0.0h, dot(normalDirection, normalize(_WorldSpaceLightPos0.xyz)));
						o.ref = fixed4(diffuseReflection, 1.0h);
						o.uv1 = TRANSFORM_TEX(i.uv, _MainTex);
						o.pos = UnityObjectToClipPos(i.vertex);
						o.uv = ComputeScreenPos(o.pos);
						return o;
					}

					fixed4 frag(v2f i) : COLOR
					{
						fixed4 color = tex2D(_MainTex, i.uv1)*i.ref;
						fixed4 reflection = tex2D(_ReflectionTex, i.uv.xy / i.uv.w);
						return (lerp(color, reflection, _RefAlpha) + lerp(reflection, color, 1 - _RefAlpha))*_Color / 2 + _AmbientColor;
					}
					ENDCG
				}
		}
		Fallback "Diffuse"
}