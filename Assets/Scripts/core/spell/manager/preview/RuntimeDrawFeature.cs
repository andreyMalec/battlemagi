using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RuntimeDrawFeature : ScriptableRendererFeature {
    public delegate void RenderCommand(CommandBuffer cmd);
    public delegate void RenderGraphCommand(RasterCommandBuffer cmd);

    class Pass : ScriptableRenderPass {
        private readonly List<RenderCommand> _queue;
        private readonly List<RenderGraphCommand> _rgQueue;
        private readonly ProfilingSampler _sampler = new("RuntimeDraw");

        public Pass(List<RenderCommand> queue, List<RenderGraphCommand> rgQueue) {
            _queue = queue;
            _rgQueue = rgQueue;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        private bool ShouldExecute() {
            return _queue.Count != 0 || _rgQueue.Count != 0;
        }

        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!ShouldExecute())
                return;

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _sampler)) {
                for (var i = 0; i < _queue.Count; i++) {
                    _queue[i]?.Invoke(cmd);
                }

                _queue.Clear();
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class RenderGraphPassData {
            public Pass pass;
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
                passData.pass = this;

                builder.SetRenderAttachment(color, 0);
                builder.SetRenderAttachmentDepth(depth);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphPassData data, RasterGraphContext ctx) => {
                    for (var i = 0; i < data.pass._rgQueue.Count; i++) {
                        data.pass._rgQueue[i]?.Invoke(ctx.cmd);
                    }

                    data.pass._rgQueue.Clear();
                });
            }
        }
    }

    private static readonly List<RenderCommand> Queue = new();
    private static readonly List<RenderGraphCommand> RenderGraphQueue = new();
    private Pass _pass;

    public static void Enqueue(RenderCommand command) {
        if (command == null)
            return;

        Queue.Add(command);
    }

    public static void Enqueue(RenderGraphCommand command) {
        if (command == null)
            return;

        RenderGraphQueue.Add(command);
    }

    public override void Create() {
        _pass = new Pass(Queue, RenderGraphQueue);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_pass);
    }
}
