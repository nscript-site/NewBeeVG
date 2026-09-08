namespace NewBeeVG.ThreeD;

public class NBScene3D
{
    public List<NBNode3D> Nodes { get; } = new List<NBNode3D>();

    public void Add(NBNode3D node)
    {
        Nodes.Add(node);
    }

    public void Build(NBEngine engine)
    {
        foreach (var node in Nodes)
        {
            node.Build(engine);
        }
    }

    public void AddMaterials()
    {
        foreach (var node in Nodes)
        {
            node.AddMaterials();
        }
    }

    public void OnFrameUpdated(NBFrameUpdateEvent e)
    {
        foreach (var node in Nodes)
        {
            node.FireOnFrameUpdated(e);
        }
    }
}
