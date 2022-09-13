using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting : MonoBehaviour
{
    const int maxDirLightCount = 4;
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

    const string bufferName = "Lighting";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };
    CullingResults cullingResults ;
    Shadow shadow = new Shadow();
    static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    static int dirLightDirectionId = Shader.PropertyToID("_DircetionalLightDirection");
    static int dirLightCountId= Shader.PropertyToID("_DircetionalLightCount");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    public void SetUp(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
     
        this.cullingResults = cullingResults; 
        buffer.BeginSample(bufferName);
        shadow.SetUp(context,cullingResults,shadowSettings);
        SetUpLights();
        shadow.Render();
     
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    void SetUpLights()
    {
        NativeArray<VisibleLight> visibleLight = cullingResults.visibleLights;
        int dirLightCount = 0;
        for(int i=0;i< visibleLight.Length;i++)
        {
            if (visibleLight[i].lightType == LightType.Directional)
            {
                VisibleLight light= visibleLight[i];
                SetDirectionalLight(dirLightCount++, ref light);
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
          
        }
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }
    private void SetDirectionalLight(int index,ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadow.ReserveDirectionalShadow(visibleLight.light, index);
      
     

    }
    public void Cleanup()
    {
        shadow.Cleanup(); 
    }
}
