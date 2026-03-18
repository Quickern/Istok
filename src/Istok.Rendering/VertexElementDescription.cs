using Silk.NET.Vulkan;

namespace Istok.Rendering;

/// <summary>
/// Describes a single element of a vertex for constructing VertexInputAttributeDescription
/// </summary>
public readonly record struct VertexElementDescription
{
    public readonly string Name;

    public readonly Format Format = Format.R32Sfloat;

    /// <summary>
    /// Explicit byte offset within the vertex buffer. If null, offset is auto-computed
    /// sequentially from preceding elements' sizes.
    /// </summary>
    public readonly uint? Offset;

    public VertexElementDescription(
        string name,
        Format format,
        uint? offset = null)
    {
        Name = name;
        Format = format;
        Offset = offset;
    }
}
