#!/usr/bin/env dotnet

var math = """
            $ #t1[A] #t2[=] #t3[pi r^2] $
            $ #text(fill: red)[f(x)] = #text(fill: blue)[x^2] + #text(fill: orange)[2x] + 1 $
            $ "area" = pi dot "radius"^2 $
            $ cal(A) :=
                { x in RR | x "is natural" } $
            #let x = 5
            $ #x < 17 $
            """;
VStack([
    TextBlock("TypstMath 直接嵌入数学公式").Margin(10),
    TypstMath(math).Width(800).Ref(out var tm)
        .OnFrame(e=>{
            SKColor color = SKColors.Red;
            byte alpha1 = 255;
            byte alpha2 = (byte)(e.frame < 10 ? 0 : 255);
            byte alpha3 = (byte)(e.frame < 20 ? 0 : 255);
            tm.Seg("t1",color,alpha1);
            tm.Seg("t2",color,alpha2);
            tm.Seg("t3",color,alpha3);
        })
        .Align(0,-1).Margin(100)
]).Margin(100)
.AsClip(out var clip, 30, name: "typstmath");

run(stage(1920,1080,bg: SKColors.White), [clip]);