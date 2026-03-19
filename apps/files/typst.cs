#:package TypstNET@1.0.0-rc1

using Typst.NET;

// Basic compilation
using var compiler = new TypstCompiler(workspaceRoot: ".");
using var result = compiler.Compile("= Hello World");

if (result.Success)
{
    var svg = result.Document.RenderPageToSvg(0);
    Console.WriteLine(svg);
    // File.WriteAllText("output.svg", svg);
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.Message} at {error.Location?.Line}:{error.Location?.Column}");
}