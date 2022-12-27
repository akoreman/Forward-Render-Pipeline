using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


public class Lighting
{
    const string bufferName = "Lighting";
    const int maxDirLightCount = 4;
    const int maxOtherLightCount = 64;

    CullingResults cullingResults;
    Shadows shadows = new Shadows();

    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColoursId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
    static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");


    static Vector4[] dirLightColours = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        dirLightColours[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }

    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
    }

    void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        int otherLightCount = 0;

        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];

            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++, i, ref visibleLight);
                    }
                    break;

                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupPointLight(otherLightCount++, i, ref visibleLight);
                    }
                    break;

                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupSpotLight(otherLightCount++, i, ref visibleLight);
                    }
                    break;
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);

        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColoursId, dirLightColours);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);

        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}
