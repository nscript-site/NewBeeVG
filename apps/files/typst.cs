#:package TypstNET@1.0.0-rc1

using Typst.NET;

// 获取 LocalAppData 路径
string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
// 拼接 typst packages 缓存目录
string typstPackageCache = Path.Combine(localAppData, "typst", "packages");

var options = new TypstCompilerOptions
{
    WorkspaceRoot = "./typst",
    Inputs = new Dictionary<string, string>(),
    CustomFontPaths = ["./fonts", "./assets/typography"],
    IncludeSystemFonts = true,
    PackagePath = typstPackageCache
};

// Basic compilation
using var compiler = new TypstCompiler(options);
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