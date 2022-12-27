using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[System.Serializable]
public class ShadowSettings
{
    [Min(0.001f)]
    public float maxDistance = 100f;

    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;

    public enum TextureSize
    {
        _256 = 256, _512 = 512, _1024 = 1024,
        _2048 = 2048, _4096 = 4096, _8192 = 8192
    }

    public enum FilterMode
    {
        PCF2x2, PCF3x3, PCF5x5, PCF7x7
    }


    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;

        [Range(1, 4)]
        public int cascadeCount;

        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

        [Range(0.001f, 1f)]
        public float cascadeFade;

        public FilterMode filter;
    }

    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        filter = FilterMode.PCF2x2,
    };

    [System.Serializable]
    public struct Other
    {
        public TextureSize atlasSize;
        public FilterMode filter;
    }

    public Other other = new Other
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2
    };
}

public class Shadows
{
    const string bufferName = "Shadows";
    const int maxShadowedDirectionalLightCount = 4;
    const int maxShadowedOtherLightCount = 16;
    const int maxCascades = 4;

    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    struct ShadowedDirectionalLight { public int visibleLightIndex; }
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    struct ShadowedOtherLight { public int visibleLightIndex; }
    //{
        //public int visibleLightIndex;
        //public float normalBias;
    //}

    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    ScriptableRenderContext context;
    CullingResults cullingResults;

    ShadowSettings settings;

    int ShadowedDirectionalLightCount;
    int shadowedOtherLightCount;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    //static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
    static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
    static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");


    static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] otherFilterKeywords = {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    Vector4 atlasSizes;

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    static Vector4[] cascadeData = new Vector4[maxCascades];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        ShadowedDirectionalLightCount = 0;
        shadowedOtherLightCount = 0;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex };

            int counter = ShadowedDirectionalLightCount;
            ShadowedDirectionalLightCount++;

            return new Vector2(light.shadowStrength, settings.directional.cascadeCount * counter);
        }

        return Vector2.zero;
    }

    public Vector2 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (shadowedOtherLightCount < maxShadowedOtherLightCount && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
            {
                visibleLightIndex = visibleLightIndex,
                //slopeScaleBias = light.shadowBias,
                //normalBias = light.shadowNormalBias
            };

            int counter = shadowedOtherLightCount;
            shadowedOtherLightCount++;

            return new Vector2(light.shadowStrength, counter);
        }

        return Vector2.zero;
    }



    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
            RenderDirectionalShadows();
        else
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        if (shadowedOtherLightCount > 0)
            RenderOtherShadows();
        else
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
    }

    void SetKeywords()
    {
        int enabledIndex = (int)settings.directional.filter - 1;
        for (int i = 0; i < directionalFilterKeywords.Length; i++)
        {
            if (i == enabledIndex)
                buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
            else
                buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
        }
    }

    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
                buffer.EnableShaderKeyword(keywords[i]);
            else
                buffer.DisableShaderKeyword(keywords[i]);
        }
    }


    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;

        Vector3 ratios = settings.directional.CascadeRatios;

        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            shadowSettings.splitData = splitData;

            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
                cascadeCullingSpheres[i] = splitData.cullingSphere;
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;

                cascadeCullingSpheres[i] = cullingSphere;
            }

            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;

        int split;

        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;

        if (tiles <= 1)
            split = 1;
        else if (tiles <= 4)
            split = 2;
        else
            split = 4;

        int tileSize = atlasSize / split;

        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        buffer.ClearRenderTarget(true, false, Color.clear);
        ExecuteBuffer();

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //buffer.SetGlobalFloat(shadowDistanceId, settings.maxDistance);

        float f = 1f - settings.directional.cascadeFade;

        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);

        SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
        //buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        ExecuteBuffer();
    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives( light.visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

        shadowSettings.splitData = splitData;

        otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(index, split, tileSize), split);

        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

        ExecuteBuffer();

        context.DrawShadows(ref shadowSettings);
        buffer.SetGlobalDepthBias(0f, 0f);
    }

    void RenderOtherShadows()
    {
        int atlasSize = (int)settings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        buffer.ClearRenderTarget(true, false, Color.clear);
        //buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < shadowedOtherLightCount; i++)
        {
            RenderSpotShadows(i, split, tileSize);
        }

        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        SetKeywords( otherFilterKeywords, (int)settings.other.filter - 1);

        //buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);

        if (shadowedOtherLightCount > 0) { buffer.ReleaseTemporaryRT(otherShadowAtlasId); }

        ExecuteBuffer();
    }

    // Conversion to atlas coordinates from CatLikeCoding.
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
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

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);

        offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));

        return offset;
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);

        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

}