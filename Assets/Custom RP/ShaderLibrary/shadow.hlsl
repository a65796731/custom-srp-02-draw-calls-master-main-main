#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#define MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
	float normalbias;
};
struct ShadowData
{
  int cascadeIndex;
  float strength;
 
};
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);
CBUFFER_START(_CustomShadows)
    int _CascadeCount;
	float4 _ShadowDistenceFade;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT*MAX_CASCADE_COUNT];
CBUFFER_END

float SampleDirectionalShadowAtlas (float3 positionSTS)
{
  return  SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}
float GetDirectionalShadowAttenuation (DirectionalShadowData data,ShadowData shadowdata, Surface surfaceWS) {
      if (data.strength <= 0.0) {
		return 1.0;
	   }
	  float3 normalBias=surfaceWS.normal*(data.normalbias*_CascadeData[shadowdata.cascadeIndex].y);
	float3 positionSTS = mul(
		_DirectionalShadowMatrices[data.tileIndex],
		float4(surfaceWS.position+normalBias, 1.0)
	).xyz;
	//positionSTS.z+= 0.001;
	float shadow = SampleDirectionalShadowAtlas(positionSTS);

		return lerp(1.0, shadow, data.strength);
}
#endif