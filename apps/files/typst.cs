#:package TypstNET@1.0.0-rc1

using Typst.NET;

// Basic compilation
using var compiler = new TypstCompiler(workspaceRoot: "./typst");
using var result = compiler.Compile(File.ReadAllText("./typst/page1.typ"));

if (result.Success)
{
    var svg = result.Document.RenderPageToSvg(0);
    Console.WriteLine(svg);
    File.WriteAllText("output.svg", svg);
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.Message} at {error.Location?.Line}:{error.Location?.Column}");
}