#!/usr/bin/env dotnet

var math = """
            $ A = pi r^2 $
            $ "area" = pi dot "radius"^2 $
            $ cal(A) :=
                { x in RR | x "is natural" } $
            #let x = 5
            $ #x < 17 $
            """;
VStack([
    TextBlock("TypstMath 直接嵌入数学公式").Margin(10),
    TypstMath(math)
        .Align(0,-1).Margin(100)
]).Margin(100)
.AsClip(out var clip, 30, name: "typstmath");

run(stage(bg: SKColors.Orange), [clip]);