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

var logo2 = clip(
    name: "logo2",
    start: 0,
    frames: -1,
    builder: (ctx, clip) =>
    {
        return
            TextBlock("By Nscript").Align(0, 0).Foreground(SKColors.Red).FontSize(60);
    }
);

var footer = clip(
    name: "footer",
    start: 0,
    frames: -1,
    builder: (ctx, clip) =>
    {
        return
            TextBlock("This is a long long long long long long long long long long long long long long long long long long long long long long long long long long long long long footer", textAlign: 0).Align(0, 1).Margin(20);
    }
);

run(stage(bg: SKColors.Orange), [clip1, logo, logo2, footer]);
