Shader "4U.withGame/UnlitShade" {
	Properties{
        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull_Mode", int) = 2
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Space]
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Main Color", COLOR) = (1,1,1,1)
        [Space]
        _WhiteBalance("White Balance",Range(-1, 1)) = 0
        _HueC("Hue Change",Range(0, 9)) = 0
        [Space]
        _ShadeColor ("Shade Color", Color) = (0.3,0.3,0.3)
        _ShadeShift ("Shade Shift", Range(-1, 1)) = 0
		[Space]
		[NoScaleOffset] _EmissionMap("No Shadow Map", 2D) = "black" {}
        [Space]
        _IndirectLightIntensity ("GI Intensity", Range(0, 1)) = 0.1
    }
    SubShader{
        Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
        LOD 0
        Pass{
			Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            Cull [_CullMode]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            //#pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma target 3.0
            #include "UnityCG.cginc"


            uniform fixed _Cutoff;
            uniform sampler2D _MainTex;uniform half4 _MainTex_ST;
            uniform fixed4 _Color;
            uniform fixed _WhiteBalance;
            uniform fixed _HueC;
            uniform fixed3 _ShadeColor;
            uniform fixed _ShadeShift;
			uniform sampler2D _EmissionMap;
			uniform fixed _EmissionColor;
            uniform fixed _IndirectLightIntensity;

            struct appdata {
                half4 vertex : POSITION;
                half3 normal : NORMAL;
                half2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
                };
            struct v2f {
				half4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                half halfLam : TEXCOORD1;
                half3 gi : TEXCOORD2;
                UNITY_FOG_COORDS(3)
				UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed3 hueChange (fixed3 col , fixed hueC){
                fixed3 rc = lerp(col,fixed3(col.g,col.g,col.b),smoothstep(0,1,hueC));
                rc = lerp(rc,fixed3(col.g,col.b,col.b),smoothstep(1,2,hueC));
                rc = lerp(rc,fixed3(col.g,col.b,col.r),smoothstep(2,3,hueC));
                rc = lerp(rc,fixed3(col.b,col.b,col.r),smoothstep(3,4,hueC));
                rc = lerp(rc,fixed3(col.b,col.r,col.r),smoothstep(4,5,hueC));
                rc = lerp(rc,fixed3(col.b,col.r,col.g),smoothstep(5,6,hueC));
                rc = lerp(rc,fixed3(col.r,col.r,col.g),smoothstep(6,7,hueC));
                rc = lerp(rc,fixed3(col.r,col.g,col.g),smoothstep(7,8,hueC));
                rc = lerp(rc,fixed3(col.r,col.g,col.b),smoothstep(8,9,hueC));
                return rc;
            }    
            v2f vert (appdata v){
				v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o); 
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                half3 normalDir = normalize(UnityObjectToWorldNormal(v.normal));
                o.halfLam = dot(normalDir,normalize(_WorldSpaceLightPos0.xyz))*0.5+0.5;
                o.gi = ShadeSH9(half4(normalDir,1));
                UNITY_TRANSFER_FOG(o, o.pos);
                //TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
				fixed4 col= tex2D(_MainTex,i.uv);
				clip(col.a-_Cutoff);
                fixed3 baseColor = hueChange(((col.rgb+_WhiteBalance)*_Color),_HueC);
                fixed emission = step(0.5,tex2D(_EmissionMap, i.uv));
                baseColor = lerp(lerp(baseColor*_ShadeColor,baseColor,step(_ShadeShift*0.5+0.5,i.halfLam)),baseColor,emission);
                baseColor += lerp(0,i.gi,_IndirectLightIntensity);
                UNITY_APPLY_FOG(i.fogCoord, baseColor);
                return fixed4(baseColor.rgb, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent Cutout"
}
