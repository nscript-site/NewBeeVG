#!/usr/bin/env dotnet
embed_python312_win32();
dynamic m = py_module("./python/plot.py");
var clip1 = clip(
    name: "pyplot",
    frames: 30,
    builder: (ctx, clip) =>
    {
        SKBitmap bmp;
        using (py_gil())
        {
            var img = m.plot_3d_data(ctx.progress * 2 * Math.PI);
            bmp = py_imdecode(img);
        }

        return
        VGrid($"*", [
                Image(bmp)
                    .Align(0,0)
                ]).Background(SKColors.DeepSkyBlue);
    }
);

run(stage(bg: SKColors.Orange), [clip1]);