using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    private ViewSpaceNormalsPass viewSpaceNormalsPass;
    private OutlinePass _outlinePass;
    private LayerMask outlineLayer = -1; // All layers by default

    [System.Serializable]
    public class Settings
    {
        public Color outlineColour = Color.black;
        [Range(0.5f, 5f)] public float thickness = 1f;
        public Material outlineMaterial;
    }

    
    public override void Create()
    {
        viewSpaceNormalsPass = new ViewSpaceNormalsPass(RenderPassEvent.AfterRenderingPrePasses, outlineLayer);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, 
        ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled) return;
        renderer.EnqueuePass(viewSpaceNormalsPass);
    }

    private class ViewSpaceNormalsPass : ScriptableRenderPass 
    {
        private readonly RenderTargetHandle viewSpaceNormalsBuffer;
        private FilteringSettings filteringSettings;
        private readonly List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();
        private readonly Material normalsMaterial;
        
        public ViewSpaceNormalsPass(RenderPassEvent evt, LayerMask layerMask)
        {
            this.renderPassEvent = evt;
            viewSpaceNormalsBuffer.Init("_SceneViewSpaceNormals");
            shaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("ForwardOnly"),
                new ShaderTagId("Toon3"),
                new ShaderTagId("grass"),
                new ShaderTagId("GrassBack")
            };
            Shader shader = Shader.Find(
                "Hidden/Universal Render Pipeline/NormalsTexture");

            Material normalsMat = CoreUtils.CreateEngineMaterial(shader);

        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!normalsMaterial)
            {
                return;
            }
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("View Space Normals Pass")))
            {
                DrawingSettings dsettings = CreateDrawingSettings(shaderTagIds, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                dsettings.overrideMaterial = normalsMaterial;
                context.DrawRenderers(renderingData.cullResults, ref dsettings, ref filteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor normalsDescriptor = cameraTextureDescriptor;
            normalsDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            normalsDescriptor.depthBufferBits = 0; // No depth buffer needed
            cmd.GetTemporaryRT(viewSpaceNormalsBuffer.id, normalsDescriptor, FilterMode.Point);
            ConfigureTarget(viewSpaceNormalsBuffer.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(viewSpaceNormalsBuffer.id);
        }
        
        public void Cleanup()
        {
        }
    }
    private class OutlinePass : ScriptableRenderPass
    {

        public OutlinePass(RenderPassEvent evt,
            RenderTargetHandle normalsHandle,
            Settings settings)
        {
         
        }

        // Called once per camera just before Execute
        public override void OnCameraSetup(CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Outline Pass");

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

}