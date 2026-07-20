#!/usr/bin/env dotnet

var clip1 = clip(
     name: "clip_image",
     frames: 30, 
     builder: (ctx, clip) =>
     {
         return
         VGrid($"*", [
                 Image("./typst/assets/snows.jpg")
                 .Align(0,0).Stretch(Stretch.Fill)
             ]).Background(SKColors.DeepSkyBlue);
     },
     mask: (ctx, clip) =>
     {
         var easing = Easing.SineInOut;
         double v = easing(ctx.progress);
         return
         Panel([
            Panel().Align(null,null).Opacity(0.5).Background(SKColors.Black),
            TextBlock("RUN").Align(0,0).FontSize(400 + (int)(400*v))
        ]);
     }
 );

run(stage(1920, 1080, bg: SKColors.Orange), [clip1]);

