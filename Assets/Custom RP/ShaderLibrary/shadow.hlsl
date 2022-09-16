#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3

#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif
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
  float cascadeBlend;
 
};
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);
CBUFFER_START(_CustomShadows)
    int _CascadeCount;
	float4  _ShadowAtlasSize;
	float4 _ShadowDistenceFade;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT*MAX_CASCADE_COUNT];
CBUFFER_END

float SampleDirectionalShadowAtlas (float3 positionSTS)
{
  return  SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}


float FilterDirectionalShadow(float3 positionSTS)
{
    	#if defined(DIRECTIONAL_FILTER_SETUP)
	   float weights[DIRECTIONAL_FILTER_SAMPLES];
	   float2 positions[DIRECTIONAL_FILTER_SAMPLES];
	   float4 size=_ShadowAtlasSize.yyxx;
	   DIRECTIONAL_FILTER_SETUP(size,positionSTS.xy,weights,positions);
	  float shadow=0;
	  for(int i=0;i<DIRECTIONAL_FILTER_SAMPLES;i++)
	  {
	    shadow+=weights[i]*SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
	  }
	  return shadow;
	 #else
	 return  SampleDirectionalShadowAtlas(positionSTS);
	 #endif
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
	
	  float shadow = FilterDirectionalShadow(positionSTS);
	 if(shadowdata.cascadeBlend<1.0)
	  {
	    normalBias = surfaceWS.normal*(data.normalbias*_CascadeData[shadowdata.cascadeIndex+1].y);
		positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex + 1],float4(surfaceWS.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, shadowdata.cascadeBlend);
	  }
		return lerp(1.0, shadow, data.strength);
}
#endif