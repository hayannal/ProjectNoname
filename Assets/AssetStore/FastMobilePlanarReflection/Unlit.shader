Shader "SupGames/PlanarReflection/Unlit"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Main Texture", 2D) = "white" {}
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
			fixed _RefAlpha;
			fixed4 _MainTex_ST;
			fixed4 _Color;

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed4 uv : TEXCOORD0;
				fixed2 uv1 : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = ComputeScreenPos(o.pos);
				o.uv1 = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : COLOR {
				fixed4 color = tex2D(_MainTex, i.uv1);
				fixed4 reflection = tex2D(_ReflectionTex, i.uv.xy / i.uv.w);
				return (lerp(color,reflection, _RefAlpha) + lerp(reflection, color, 1-_RefAlpha))*_Color/2;
			}
			ENDCG
		}
	}
}