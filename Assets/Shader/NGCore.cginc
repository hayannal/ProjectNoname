// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

#ifndef NG_CORE_INCLUDED
#define NG_CORE_INCLUDED

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Channel

// 많아지면 2의 승수만큼 느려지기 때문에 Flow에만 사용하는거로 하겠다.
inline fixed SelectFlowChannel(fixed3 mask)
{
#if _FLOW_CHANNEL_R
	return mask.r;
#elif _FLOW_CHANNEL_G
	return mask.g;
#elif _FLOW_CHANNEL_B
	return mask.b;
#endif
	return 1.0f;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#define NG_MATCAP_UV	\
	float3 worldNorm = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z);	\
	worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);	\
	o.matcapUV = worldNorm.xy * 0.5 + 0.5;

#define NG_MATCAP_SINGLE	\
	fixed3 mc = tex2D(_MatCapTex, IN.matcapUV);	\
	mc = lerp(0.5f, mc, (mask.g * _MatCapIntensity));	\
	c *= mc * 2.0f;

#define NG_MATCAP_DUAL	\
	IN.matcapUV.x *= 0.5f;	\
	fixed3 mc1 = tex2D(_MatCapTex, IN.matcapUV);	\
	IN.matcapUV.x += 0.5f;	\
	fixed3 mc2 = tex2D(_MatCapTex, IN.matcapUV);	\
	mc1 = lerp(mc1, mc2, (mask.g * _MatCapIntensity));	\
	c *= mc1 * 2.0f;

#define NG_CUTOFF	\
	clip(mask.b - _Cutoff);

#define NG_EMISSIVE	\
	c.rgb = lerp(c.rgb, _EmissiveColor.rgb, mask.r * _EmissiveColor.a);

#define NG_RIMLIGHT	\
	half rim = 1.0f - saturate(dot(normalize(IN.viewDir), o.Normal));	\
	o.Emission = _RimColor.rgb * saturate(rim - _RimPower);

#define NG_NORMAL	\
	o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));

#define NG_FLOW_UV	\
	o.flowUV = TRANSFORM_TEX(v.texcoord, _FlowTex) + _FlowSpeed.xy * _Time.y;

#define NG_FLOW(flowMask)	\
	fixed3 flow = tex2D(_FlowTex, IN.flowUV).rgb * _FlowPower;	\
	c.rgb += flow * flowMask;

#define NG_WIND_UPDATER	\
	v.vertex.xyz += _WindParameterUpdater.xyz * v.color.r;

#define NG_WIND_VERTEX_COS	\
	float t = (_Time.y + dot(v.vertex.xz, _WindParameter.xz)) * _WindParameter.w;	\
	float wave = cos(t) * v.color.r;	\
	v.vertex.xyz += _WindParameter.xyz * wave;

#endif // NG_CORE_INCLUDED

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////