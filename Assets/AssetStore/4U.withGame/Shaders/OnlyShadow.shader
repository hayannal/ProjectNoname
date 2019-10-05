Shader "4U.withGame/Lit/OnlyShadow" {
	
    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 0
//ShadowCaster
        pass{
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #include "Lighting.cginc"
            #include "UnityCG.cginc"
            struct v2f {V2F_SHADOW_CASTER;};
            v2f vert (appdata_base v) {
                v2f o = (v2f)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            half4 frag(v2f i) : SV_TARGET {SHADOW_CASTER_FRAGMENT(i)}
            ENDCG
        } 
    }
}
