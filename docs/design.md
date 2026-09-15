# 基础概念

## NewBeeVG 基础

NewBeeVG 的基本概念有：

- workspace: 工作空间。每个工作空间可以包含多个工作内容。
- work: 工作内容。每个工作内容，代表一个视频制作项目。
- track: 轨道。每个 work 由多个轨道组成，玩过视频剪辑的都知道啥是轨道。
- clip: 轨道上的视频片段。
- stage：舞台。每个视频制作项目(work)，共享一个 stage 配置，如视频的长、宽、背景色等设置。
目前 NewBeeVG 主要实现了对 track, clip 和 stage 的相关操作。上面例子中 run(stage(width: 1920, height: 1080, bg: SKColors.Black), clips);，意思就是预览背景为黑色、宽 1920, 高 1080 下的 track。track 由 clips 中的具有先后顺序的片段组成。

NewBeeVG.Methods 类下有大量的静态方法，提供了丰富的基础操作。

这些方法分两类：

- 首字母大写的方法，是界面描述与布局相关的方法。
- 首字母小写的方法，是其它方法，比如，创建各种 clip 的方法 …

NewBeeVG 内置了一些 widgets，比如，Grid, Stack, Panel, Canvas 等，支持 AvaloniaUI 的布局逻辑，可以像用 Avalonia 一样使用。界面描述使用在 [NewBeeUI](https://github.com/nscript-site/NewBeeUI) 项目中改良的 markup 语言。

> [!NOTE]
> clip 是最核心的概念。它有三个最核心的参数：int? StartFrame ， int DurationFrames 和 Func<NBBuildContext, NBClip, Control?>? Builder。DurationFrames 表示，这个 clip 长度是多少帧。StartFrame 表示，clip 从第几帧进入。StartFrame 如果是 null，则自动贴近前面最近的 clip。

## Markup 语言

Markup 是用 csharp 语法进行界面描述的强类型描述语言。

受 [Avalonia.Markup.Declarative](https://github.com/AvaloniaCommunity/Avalonia.Markup.Declarative) 项目启发，我们开发了基于 Markup 的 Avalonia UI 界面库 [NewBeeUI](https://github.com/nscript-site/NewBeeUI)。一脉相连，NewBeeVG 也采用同样的方式描述画面中的内容。

下例 [threed.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/threed.cs) 中，首字母大写的部分，为 Markup 描述的界面设置。首字母小写的部分，是其它行为与设置。

```csharp
#!/usr/bin/env dotnet

font("阿里巴巴普惠体 2.0");

float len = 10;
float thickness = 5;

HStack([
    VStack([
        TextBlock("3D动画 (Camera 固定)").Font(40, SKColors.Black),
        Canvas3D(800,800,SKColors.Black)
            .Camera(PerspectiveCamera(Vec3(10, 12, -25)))
            .Nodes([
                GroundGrid(10,1,SKColors.White,1),
                Line3D(Vec3(),Vec3(0,0,len),SKColors.Red,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(0,0,len*e.pf)),
                Line3D(Vec3(),Vec3(0,len,0),SKColors.Green,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(0,len*e.pf,0)),
                Line3D(Vec3(),Vec3(len,0,0),SKColors.Blue,thickness)
                    .OnFrameT(e=>e.Sender.End = Vec3(len*e.pf,0,0)),
            ]).Align(0,-1)
    ]),
    VStack([
        TextBlock("3D动画 (Camera 旋转)").Font(40, SKColors.Black),
        Canvas3D(800,800,SKColors.White).Ref(out var canvas)
            .OnFrame(e=>{ canvas.RotateCamera(2f,0); canvas.ZoomCamera(-0.1f); })
            .Camera(PerspectiveCamera(Vec3(-10, -12, 25)))
            .Nodes([
                GroundGrid(10,1,SKColors.Black,1),
                Line3D(Vec3(),Vec3(0,0,len),SKColors.Red,thickness),
                Line3D(Vec3(),Vec3(0,len,0),SKColors.Green,thickness),
                Line3D(Vec3(),Vec3(len,0,0),SKColors.Blue,thickness),
            ]).Align(0,-1)
    ])
])
.Align(0, 0)
.AsClip(out var clip1, frames: 40, name: "3d");

VStack([
    TypstFile("./typst/code2.typ").MaxHeight(900).Align(0,0),
    TextBlock("生成视频的全部代码").Font(40, SKColors.Black).Align(0,-1),
]).Align(0,0).AsClip(out var clip2, frames: 120, name: "code");

run(stage(1920, 1080, bg: SKColors.Orange), [clip1,clip2]);
```

## dotnet File-Based Apps

自 10.0 版本起，dotnet 实现了 File-Based Apps 运行模式：假如你写了一个 demo.cs 文件，直接在命令行中输入 dotnet demo.cs，可以直接运行，让 csharp 开发，具备 python 一样的使用体验。

最初的 File-Based Apps 运行模式，不支持引用其它的 cs 文件。自 dotnet 10.0.300 版本起，File-Based App 提供了 include 机制，支持直接引用 cs 文件。

File-Based Apps 的详细细节参见 https://learn.microsoft.com/zh-cn/dotnet/core/sdk/file-based-apps。

VSCode 下的 C# 插件对 File-Based Apps 提供了全面支持。

这里不详述语法细节，只补充说说使用技巧。

File-Based Apps 支持 Directory.Build.props。

建立个 Directory.Build.props 文件，放在目录下或上级目录下，则所有的 cs 文件，就直接采用这些 msbuild 设置，可以极大的简化代码！

推荐使用 `File-Based Apps` 模式来使用 NewBeeVG，具体可参考 https://github.com/nscript-site/NewBeeVG/tree/main/apps

[Directory.Build.props](https://github.com/nscript-site/NewBeeVG/blob/main/apps/Directory.Build.props) 文件示例如下:

```xml
<Project>
  <PropertyGroup>
    <PublishAot>false</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <Using Include="System" />
    <Using Include="System.Collections.Generic" />
    <Using Include="System.Linq" />
    <Using Include="System.Runtime.CompilerServices" />
    <Using Include="Python.Runtime" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="NewBeeVG" />
    <Using Include="NewBeeVG.ThreeD" />
    <Using Include="SkiaSharp" />
    <Using Include="NewBeeVG.Methods" Static="True" />
    <Using Include="NewBeeVG.Methods3D" Static="True" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\sources\NewBeeVG\NewBeeVG.csproj" />
    <ProjectReference Include="..\..\sources\NewBeeVG.ThreeD\NewBeeVG.ThreeD.csproj" />
    <!-- <PackageReference Include="NewBeeVG" Version="1.0.1.1" /> -->
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="pythonnet" Version="3.1.0" />
  </ItemGroup>
</Project>
```