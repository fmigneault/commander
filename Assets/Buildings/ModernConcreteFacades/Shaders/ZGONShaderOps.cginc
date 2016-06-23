#ifndef ZGON_SHADEROPS_INCLUDED
#define ZGON_SHADEROPS_INCLUDED

float4 CombineNormalmap(float4 normalA, float4 normalB, float normalPower) {
	float4 OUT;
	OUT = normalA + (normalB * float4(normalPower, normalPower, 0.0, 1));
	
	OUT = normalize(OUT);
	
	return OUT;
}

float4 CombineColor(float4 colorA, float4 colorB) {
	float4 OUT;
	OUT = colorA * colorB;
	
	return OUT;
}

float4 CombineOcclusion(float4 colorA, float4 occlusion, float occPower, float4 occColor) {
	float4 OUT;
	float4 diff;

	diff = clamp((float4(1, 1, 1, 1) - occlusion) * occPower, (0, 0, 0, 1), (1, 1, 1, 1));
	OUT = colorA * lerp(float4(1, 1, 1, 1), occColor, diff);
	
	return OUT;
}

float4 ClampUnit(float4 colorA) {
	float4 OUT;
	OUT = clamp(colorA, float4(0, 0, 0, 1), float4(1, 1, 1, 1));
	
	return OUT;
}

float4 ApplyColorContrast(float4 colorA, float contrast) {
	float4 OUT;
	OUT = ((colorA - 0.5f) * max(contrast, 0)) + 0.025f;
	
	return OUT;
}

float4 CombineColorDetail(float4 colorA, float4 detail, float detailPower, half4 detailColor) {
	float4 OUT;
	float4 diff;
	float4 contrast;
	
	contrast = ClampUnit(ApplyColorContrast(detail, detailPower));
	diff = float4(1, 1, 1, 1) - contrast;
	OUT = colorA * lerp(float4(1, 1, 1, 1), detailColor, (diff * detailPower));
	
	return OUT;
}

#endif