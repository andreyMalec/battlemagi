using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RuntimeDrawFeature : ScriptableRendererFeature {
    public delegate void RenderGraphCommand(RasterCommandBuffer cmd);

    class Pass : ScriptableRenderPass {
        private readonly List<RenderGraphCommand> _rgQueue;
        private readonly ProfilingSampler _sampler = new("RuntimeDraw");

        public Pass(List<RenderGraphCommand> rgQueue) {
            _rgQueue = rgQueue;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        private bool ShouldExecute() {
            return _rgQueue.Count != 0;
        }

        private class RenderGraphPassData {
            public Pass Pass;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            if (!ShouldExecute())
                return;

            var camData = frameData.Get<UniversalCameraData>();
            if (camData.targetTexture != null)
                return;

            var resources = frameData.Get<UniversalResourceData>();
            var color = resources.activeColorTexture;
            var depth = resources.activeDepthTexture;

            using (var builder = renderGraph.AddRasterRenderPass<RenderGraphPassData>("RuntimeDraw", out var passData, _sampler)) {
                passData.Pass = this;

                builder.SetRenderAttachment(color, 0);
                builder.SetRenderAttachmentDepth(depth);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphPassData data, RasterGraphContext ctx) => {
                    for (var i = 0; i < data.Pass._rgQueue.Count; i++) {
                        data.Pass._rgQueue[i]?.Invoke(ctx.cmd);
                    }

                    data.Pass._rgQueue.Clear();
                });
            }
        }
    }

    private static readonly List<RenderGraphCommand> RenderGraphQueue = new();
    private Pass _pass;

    public static void Enqueue(RenderGraphCommand command) {
        if (command == null)
            return;

        RenderGraphQueue.Add(command);
    }

    public override void Create() {
        _pass = new Pass(RenderGraphQueue);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_pass);
    }
}
