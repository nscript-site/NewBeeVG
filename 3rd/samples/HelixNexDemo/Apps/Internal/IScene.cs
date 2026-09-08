using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Scene;

namespace HelixNexDemo;

public interface IScene
{
    int WorldSizeX { get; }
    int WorldSizeZ { get; }
    int MaxTerrainHeight { get; }
    int MinTerrainHeight { get; }
    void RegisterMaterials();
    Node Build(
        IResourceManager resourceManager,
        WorldDataProvider worldDataProvider
    );
}