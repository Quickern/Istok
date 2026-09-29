using Silk.NET.Vulkan;

namespace Istok.Rendering;
#nullable enable

public unsafe class RenderPass : IDisposable
{
    public AttachmentDescription? DepthTarget { get; }
    public IReadOnlyList<AttachmentDescription> ColorTargets { get; }

    readonly LogicalDevice _logicalDevice;
    readonly Silk.NET.Vulkan.RenderPass _renderPass;
    public Silk.NET.Vulkan.RenderPass DeviceRenderPass => _renderPass;

    public readonly SampleCountFlags SampleCount;

    public uint AttachmentCount { get; }

    RenderPass(LogicalDevice logicalDevice, Silk.NET.Vulkan.RenderPass renderPass, IReadOnlyList<AttachmentDescription> colorTargets, AttachmentDescription? depthTarget)
    {
        _logicalDevice = logicalDevice;
        _renderPass = renderPass;
        ColorTargets = colorTargets;
        DepthTarget = depthTarget;
        AttachmentCount = (uint)colorTargets.Count + (depthTarget != null ? 1u : 0u);

        SampleCountFlags maxSampleCount = depthTarget?.Samples ?? SampleCountFlags.Count1Bit;
        foreach (AttachmentDescription attachmentDescription in ColorTargets)
        {
            if (attachmentDescription.Samples > maxSampleCount)
                maxSampleCount = attachmentDescription.Samples;
        }

        SampleCount = maxSampleCount;
    }

    public static RenderPass Create(LogicalDevice logicalDevice, IReadOnlyList<AttachmentDescription> colorTargets, AttachmentDescription? depthTarget)
    {
        int colorAttachmentCount = colorTargets.Count;
        int totalAttachmentsCount = colorAttachmentCount + (depthTarget != null ? 1 : 0);


        AttachmentDescription* attachments = stackalloc AttachmentDescription[totalAttachmentsCount];
        AttachmentReference* colorAttachmentRefs = stackalloc AttachmentReference[colorAttachmentCount];

        for (int i = 0; i < colorAttachmentCount; i++)
        {
            attachments[i] = colorTargets[i];
            colorAttachmentRefs[i]= new AttachmentReference((uint)i, ImageLayout.ColorAttachmentOptimal);
        }

        AttachmentReference depthAttachmentRef = new AttachmentReference();
        if (depthTarget != null)
        {
            attachments[colorAttachmentCount] = depthTarget.Value;
            depthAttachmentRef = new AttachmentReference((uint)colorTargets.Count, ImageLayout.DepthStencilAttachmentOptimal);
        }

        SubpassDescription subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = (uint)colorAttachmentCount,
            PColorAttachments = colorAttachmentRefs,
            PDepthStencilAttachment = depthTarget != null ? &depthAttachmentRef : default,
        };

        // A framebuffer is rendered to across several command buffers per frame (one per pipeline pass), so
        // attachments loaded with AttachmentLoadOp.Load must see the previous pass's colour and depth writes:
        // both external dependencies name the fragment-test stages and the depth/stencil accesses, and the
        // incoming one carries a source access mask (without it, prior writes are never made available).
        const PipelineStageFlags attachmentStages =
            PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
        const AccessFlags attachmentWrites = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;
        const AccessFlags attachmentAccess =
            AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
            | AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;

        SubpassDependency* subpassDependencies = stackalloc SubpassDependency[2];
        subpassDependencies[0] = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = attachmentStages,
            SrcAccessMask = attachmentWrites,
            DstStageMask = attachmentStages,
            DstAccessMask = attachmentAccess,
        };
        subpassDependencies[1] = new SubpassDependency
        {
            SrcSubpass = 0,
            DstSubpass = Vk.SubpassExternal,
            SrcStageMask = attachmentStages,
            SrcAccessMask = attachmentWrites,
            DstStageMask = attachmentStages | PipelineStageFlags.FragmentShaderBit,
            DstAccessMask = attachmentAccess | AccessFlags.ShaderReadBit,
        };

        RenderPassCreateInfo renderPassCI = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = (uint)totalAttachmentsCount,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 2,
            PDependencies = subpassDependencies,
        };

        Result creationResult = logicalDevice.CreateRenderPass(in renderPassCI, null, out Silk.NET.Vulkan.RenderPass renderPass);
        Helpers.CheckErrors(creationResult);
        return new RenderPass(logicalDevice, renderPass, colorTargets, depthTarget);
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            _logicalDevice.DestroyRenderPass(_renderPass, null);
            IsDisposed = true;
        }
    }
}
