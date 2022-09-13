#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
int _DircetionalLightCount;
float4 _DirectionalLightColor[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DircetionalLightDirection[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END
struct Light
{
	float3 color;
	float3 direction;
	float  attenuation;
};
int GetDirectionalLightCount()
{
	return  _DircetionalLightCount;
}
DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
    DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
	return data;
}
Light GetDirectionalLight(int index,Surface surfaceWS)
{
	Light light;
	light.color = _DirectionalLightColor[index].rgb;
	light.direction = _DircetionalLightDirection[index].xyz;
	DirectionalShadowData data=GetDirectionalShadowData(index);
    light.attenuation=GetDirectionalShadowAttenuation(data,surfaceWS);
	return light;
}

#endif