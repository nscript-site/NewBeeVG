 var clip1 = clip(
    name: "clip1",
    frames: 30,
    builder: (ctx, clip) =>
    {
        return
        Panel([
                 WrapPanel([
                     TextBlock("BBBB").Margin(100),
                     TextBlock("BBBB").Margin(100),
                     TextBlock("BBBB").Margin(100),
                     TextBlock("BBBB").Margin(100),
                     TextBlock("BBBB").Margin(100),
                     TextBlock("BBBB").Margin(100),
                     ])
                 .Background(SKColors.Red)
                 .Align(0,1)
                 .Margin(100)
            ]).Background(SKColors.DeepSkyBlue);
    }
);

 var clip2 = clip(
     name: "clip2",
     frames: 30,
     builder: (ctx, clip) =>
     {
         var easing = Easing.SineInOut;
         double v = easing(ctx.progress);
         double h = 100 + (ctx.height - 100) * v;
         return
         VGrid($"*,{h}",[
                 null,
                 HGrid("200, *", [
                     TextBlock("AAAA").Align(-1,0).Margin(10),
                     TextBlock("BBBB").Align(0,-1).Margin(0,100,0,0)
                     ])
                 .Background(SKColors.Red)
             ]).Background(SKColors.DeepSkyBlue);
     }
 );

 var clip3 = clip(
     name: "clip_image",
     frames: 30,
     builder: (ctx, clip) =>
     {
         var easing = Easing.SineInOut;
         double v = easing(ctx.progress);
         double h = 100 + (ctx.height - 100) * v;
         return
         VGrid($"*,{h}", [
                 null,
                 Image("./Assets/snows.jpg").Align(0,-1).Size(300,200).Stretch(Stretch.Fill)
             ]).Background(SKColors.DeepSkyBlue);
     }
 );

var clip4 = drawing(
    name: "drawing",
    frames: 10,
    builder: (ctx, clip, canvas) =>
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Red,
            IsAntialias = true,
        };
        var radius = 100 + 200 * ctx.progress;
        canvas.DrawCircle(ctx.width / 2, ctx.height / 2, (float)radius, paint);
    }
);

var clip5 = drawing(
    name: "withmask1",
    frames: 10,
    builder: (ctx, clip, canvas) =>
    {
        canvas.Clear(SKColors.Green);
    },
    mask: (ctx, clip, canvas) =>
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black,
            IsAntialias = true,
        };
        var radius = 100 + 200 * ctx.progress;
        canvas.DrawCircle(ctx.width / 2, ctx.height / 2, (float)radius, paint);
    }
);

var clip6 = drawing(
    name: "withmask2",
    frames: 10,
    blend: SKBlendMode.SrcOut,
    builder: (ctx, clip, canvas) =>
    {
        canvas.Clear(SKColors.Green);
    },
    mask: (ctx, clip, canvas) =>
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black,
            IsAntialias = true,
        };
        var radius = 100 + 200 * ctx.progress;
        canvas.DrawCircle(ctx.width / 2, ctx.height / 2, (float)radius, paint);
    }
);

 var svg = $"""
             <svg xmlns="http://www.w3.org/2000/svg" width="300" height="200" viewBox="0 0 300 200">            
                 <rect width="100" height="100" fill="#f5f5f5"/>
                 <text x="140" y="108" font-size="24" fill="#222">Hello SVG</text>
             </svg>
             """;

 var clip7 = clip(
     name: "svg",
     frames: 30,
     builder: (ctx, clip) =>
     {
         return
         VGrid($"*", [
                 SVG(svg).Align(0,0)
             ]).Background(SKColors.DeepSkyBlue);
     }
 );

 var clip8 = clip(
     name: "typst",
     frames: 30,
     builder: (ctx, clip) =>
     {
         return
         HGrid("*", [
             VGrid($"*", [
                     TypstFile("./typst/page1.typ")
                         .Align(0,0).Margin(100)
                         .TypstInputs(x=>{x["frames"] = $"{ctx.frame}"; })
                     ]).Margin(100).Background(SKColors.DeepSkyBlue)
         ]).Background(SKColors.Green);
     }
 );

//  embed_python312_win32();

//  dynamic m = py_module("./python/plot.py");

//  var clip9 = clip(
//      name: "pyclip",
//      frames: 30,
//      builder: (ctx, clip) =>
//      {
//          SKBitmap bmp;
//          using (py_gil())
//          {
//              var img = m.plot_3d_data(ctx.progress * 2 * Math.PI);
//              bmp = py_imdecode(img);
//          }

//          return
//          VGrid($"*", [
//                  Image(bmp)
//                      .Align(0,0)
//                  ]).Background(SKColors.DeepSkyBlue);
//      }
//  );


var logo = clip(
    name: "logo",
    start: 0,
    frames: -1,
    builder: (ctx, clip) =>
    {
        return
            TextBlock("NewBeeVG").Align(1, -1).Margin(20);
    }
);
 run(stage(bg: SKColors.Orange), [clip1, clip2, clip3, clip4, clip5, clip6, clip7, clip8, logo]);