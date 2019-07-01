float CrossAlpha(float aTime, float aSpeed, float aCrossTime) {
	return abs(1-(fmod(aTime*aSpeed + aCrossTime/2, aCrossTime) / aCrossTime) * 2);
}

half4 CreateFlowUV(float aTime, half2 aUV, fixed4 aColor, float aSpeed, float aCrossTime) {
	half4 result;
	half2 dir = ((aColor.xz-0.5)*2) * aColor.y;
	float t   = aTime*aSpeed;

	result.xy = aUV - dir * (fmod(t + aCrossTime/2, aCrossTime)) - half2(0.5,0.5);
	result.zw = aUV - dir * (fmod(t,                aCrossTime));

	return result;
}

fixed4 FlowSample(sampler2D aTex, half4 aFlowUV, float aCrossAlpha) {
	return lerp(tex2D(aTex, aFlowUV.xy), tex2D(aTex, aFlowUV.zw), aCrossAlpha);
}

fixed4 FlowSample(float aTime, sampler2D aTex, half4 aFlowUV, float aSpeed, float aCrossTime) {
	return FlowSample(aTex, aFlowUV, CrossAlpha(aTime, aSpeed, aCrossTime));
}