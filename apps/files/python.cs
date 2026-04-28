#!/usr/bin/env dotnet

using System.Runtime.InteropServices;

void InitPython([CallerFilePath] string filePath = "")
{
    Console.WriteLine($"Caller File Path: {filePath}");

    FileInfo file = new FileInfo(filePath);
    var dir = file.Directory?.FullName??"./";
    
    var dllDir = @"../_lib/python-3.12.8-embed-amd64/";

    var dllDirInfo = new DirectoryInfo(Path.Combine(dir, dllDir));

    string pythonHomePath = dllDirInfo.FullName;
    string pythonDllPath = $"{pythonHomePath}python312.dll";
    // 对应python内的重要路径
    string[] py_paths = {"python312.zip", "lib", "lib/site-packages" };
    string pySearchPath = $"{pythonHomePath};";
    foreach (string p in py_paths)
    {
        pySearchPath += $"{pythonHomePath}/{p};";
    }

    Runtime.PythonDLL = pythonDllPath;
    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
    PythonEngine.PythonHome = pythonHomePath;
    PythonEngine.PythonPath = pySearchPath;
    PythonEngine.Initialize();
}

// InitPython();

void InitPythonFromMacOSXVenv()
{
    string pythonDllPath = $"/opt/homebrew/opt/python@3.12/Frameworks/Python.framework/Versions/3.12/Python";
    Runtime.PythonDLL = pythonDllPath;
    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
    PythonEngine.Initialize();
}

if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    Console.WriteLine("Running on macOS, initializing Python from venv...");
    InitPythonFromMacOSXVenv();
}
else
{
    Console.WriteLine("Running on non-macOS platform, initializing Python from embedded distribution...");
    InitPython();
}

using (Py.GIL())
{
    var code = File.ReadAllText("./python/test.py");
    PythonEngine.Exec(code);

    dynamic sys = Py.Import("sys");
    sys.path.append("./python"); // 加入路径

    dynamic m = Py.Import("module_test");

    // 简单的函数调用
    var f = m.foo();
    Console.WriteLine(f);

    // 函数调用时传值
    var sum = (int)m.add(10, 20);
    Console.WriteLine(sum);

    // 二进制相互传值
    byte[] data = new byte[] {1,2,3};
    var pyBytes = m.process_bytes(data);
    byte[] result = (byte[])pyBytes;
    Console.WriteLine($"return: {result.Length} bytes");
}
