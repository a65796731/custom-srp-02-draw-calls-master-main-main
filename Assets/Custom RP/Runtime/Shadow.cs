using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

struct ShadowedDirectionalLight
{
    public int visibleLightIndex;
    public float slopeScaleBias;
    public float nearPlaneOffset;
}

public class Shadow 
{
    int ShadowedDirectionalLightCount = 0;
    const int maxShadowedDirectionalLightCount = 4,maxCascades=4;
    const string bufferName = "Shadow";
    CommandBuffer  buffer=new CommandBuffer { name = bufferName };
    ShadowedDirectionalLight[] shadowedDirectionals = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings shadowSettings;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
               dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
               cascadeCountId = Shader.PropertyToID("_CascadeCount"),
               cacadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
               cascadeDataId=Shader.PropertyToID("_CascadeData"),
               shadowDistenceFadeId= Shader.PropertyToID("_ShadowDistenceFade");
     static Matrix4x4[] dirshadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades],
                     cascadeData = new Vector4[maxCascades];
    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults,
         ShadowSettings shadowSettings)
    {
        ShadowedDirectionalLightCount = 0;
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings= shadowSettings;
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
   public Vector3 ReserveDirectionalShadow(Light light,int visibleLightIndex)
    {
        if(visibleLightIndex<maxShadowedDirectionalLightCount&&
            light.shadows!=LightShadows.None&&light.shadowStrength>0f&&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            shadowedDirectionals[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            return new Vector3(light.shadowStrength, shadowSettings.directional.cascadeCount*ShadowedDirectionalLightCount++,light.shadowNormalBias) ;
        }
        return Vector3.zero;
    }
    public void Render()
    {
        if(ShadowedDirectionalLightCount>0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize, atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true,false,Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        int tiles = ShadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i=0;i<ShadowedDirectionalLightCount;i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        float f = 1 - shadowSettings.directional.cascadeFade;

        buffer.SetGlobalVector(shadowDistenceFadeId,new Vector4( 1f/shadowSettings.maxDistance,1f/shadowSettings.distanceFade,1f/(1f-f*f)));
    
        buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(
            cacadeCullingSpheresId, cascadeCullingSpheres
        );
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirshadowMatrices);
        buffer.EndSample(bufferName);
        ExecuteBuffer();

    }
    private void RenderDirectionalShadows(int index,int split,int tileSize)
    {
         ShadowedDirectionalLight shadowedDirectionalLight = shadowedDirectionals[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, shadowedDirectionalLight.visibleLightIndex);
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                shadowedDirectionalLight.visibleLightIndex, i, cascadeCount, ratios, tileSize, shadowedDirectionalLight.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            shadowDrawingSettings.splitData = splitData;
          
            int tileIndex = tileOffset + i;
            if(index==0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            dirshadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0, shadowedDirectionalLight.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            buffer.SetGlobalDepthBias(0, 0f);
        }
    }
    void SetCascadeData(int index,Vector4 cullingSphere,float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        texelSize = texelSize * 1.4142136f;
        cascadeData[index] = new Vector4(
          1f / cullingSphere.w,
          texelSize
          );
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
    }
    Vector2 SetTileViewport(int index,int split,float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x*tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,Vector2 offset,int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }


        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();

    }
}
