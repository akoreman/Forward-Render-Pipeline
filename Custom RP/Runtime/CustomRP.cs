using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


public class CustomRP : RenderPipeline
{
    CamRenderer renderer = new CamRenderer();
    ShadowSettings shadowSettings;

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, shadowSettings);
            GraphicsSettings.lightsUseLinearIntensity = true;
        }
    }

    public CustomRP(ShadowSettings shadowSettings)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        this.shadowSettings = shadowSettings;
    }
}

public class CamRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    CullingResults cullingResults;
    Lighting lighting = new Lighting();

    //static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId unlitShaderTagId = new ShaderTagId("Unlit");
    //static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    static ShaderTagId litShaderTagId = new ShaderTagId("Lit");

    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer {name = bufferName};

    public void Render (ScriptableRenderContext context, Camera camera, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        if (!Cull(shadowSettings.maxDistance))
            return;

        buffer.BeginSample(bufferName);

        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);

        buffer.EndSample(bufferName);

        Setup();
        DrawGeometry();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);

        ExecuteBuffer();
    }

    void DrawGeometry()
    {
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes
        };

        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        drawingSettings.SetShaderPassName(1, litShaderTagId);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);
    }

    void Submit()
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters param;

        if (camera.TryGetCullingParameters(out param))
        {
            param.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref param);
            return true;
        }

        return false;
    }
}
