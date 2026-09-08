using HelixToolkit.Nex.Engine.CameraControllers;
using HelixToolkit.Nex.Engine.Cameras;
using HelixToolkit.Nex.Maths;
using System.Numerics;

namespace HelixNexDemo;

public class BaseEngineApp : IDisposable
{
    protected NBEngine Engine;

    // Camera
    protected Camera _camera = new PerspectiveCamera();
    protected OrbitCameraController _orbitController;

    // Custom line material types registered by this demo
    protected string[] _materialTypes = [];

    public BaseEngineApp(int width, int height, PerspectiveCamera? camera = null, Color? backgroundColor = null)
    {
        _camera = camera ?? new PerspectiveCamera
        {
            Position = new Vector3(0, 12, -25),
            Target = Vector3.Zero,
            FarPlane = 500,
        };
        _orbitController = new OrbitCameraController(_camera);
        RegisterCustomMaterials();
        Engine = NB3D.CreateEngine(width, height, backgroundColor ?? Color.Black);
        BuildScene();
    }

    protected virtual void BuildScene()
    {
    }

    protected virtual void RegisterCustomMaterials()
    {
    }

    protected virtual void UpdateData()
    {

    }

    public void Render()
    {
        UpdateData();
        Engine.Update(_camera);
        Engine.Render();
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Engine.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
