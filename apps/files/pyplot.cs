#!/usr/bin/env dotnet
embed_python312_win32();
dynamic m = py_module("./python/module_test.py");
using(py_gil())
{
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
