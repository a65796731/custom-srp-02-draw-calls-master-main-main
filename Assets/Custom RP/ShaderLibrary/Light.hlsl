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
float FadedShadowStrength(float distence,float scale,float fade)
{
 return saturate((1-scale*distence)*fade);
}
ShadowData GetShadowData(Surface surfaceWS)
{
  ShadowData data;
  data.strength=FadedShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
  int i;
  //找到position属于哪个联级里
  for(int i=0;i<_CascadeCount;i++)
  {
    float4 cullingSpheres= _CascadeCullingSpheres[i];
	float disSqr=DistanceSquared(surfaceWS.position,cullingSpheres.xyz);
	if(disSqr<cullingSpheres.w)
	{
	
	   break;
	}
  }
  if(i==_CascadeCount)
  {
     data.strength=0.0;
  }
  data.cascadeIndex=i;
  return data;
}
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadowData)
{
    DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x*shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y+ shadowData.cascadeIndex;
	return data;
}

Light GetDirectionalLight(int index,Surface surfaceWS,ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColor[index].rgb;
	light.direction = _DircetionalLightDirection[index].xyz;

	DirectionalShadowData data=GetDirectionalShadowData(index,shadowData);
    light.attenuation=GetDirectionalShadowAttenuation(data,surfaceWS);
	//  light.attenuation=  shadowData.cascadeIndex*0.25;
	return light;
}

#endif