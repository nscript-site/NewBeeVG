#!/usr/bin/env dotnet
embed_python312_win32();
dynamic m = py_module("./python/py3d.py");
var clip1 = clip(
    name: "py3d",
    frames: 30,
    builder: (ctx, clip) =>
    {
        SKBitmap bmp;
        using (py_gil())
        {
            var img = m.cvs_draw_data(200 + ctx.progress * 200);
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
