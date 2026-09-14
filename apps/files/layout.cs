#!/usr/bin/env dotnet

HGrid("*,*,*,*,Auto", [
    Rect(200,200,fill:SKColors.Green).Align(0,0),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(null,0),
    Rect(0,0,fill:SKColors.Green).Align(null,null),
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var hgrid, 30, name:"hgrid");

VGrid("*,*,*,*,Auto", [
    Rect(200,200,fill:SKColors.Green).Align(0,0),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(0,null),
    Rect(0,0,fill:SKColors.Green).Align(null,null),
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
])
.Margin(10)
.Background(SKColors.LightCoral)
.AsClip(out var vgrid, 30, name:"vgrid");

HStack([
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
    Rect(200,200,fill:SKColors.Green).Align(0,null),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var hstack, 30, name:"hstack");

VStack([
    Rect(200,200,fill:SKColors.Green).Align(0,-1),
    Rect(200,200,fill:SKColors.Green).Align(null,1),
])
.Spacing(20)
.Background(SKColors.LightCoral)
.AsClip(out var vstack, 30, name:"vstack");

Panel([
    Rect(400,400,fill:SKColors.Red).Align(-1,-1),
    Rect(200,200,fill:SKColors.Green).Align(-1,-1),    
    Rect(200,200,fill:SKColors.Blue).Align(1,-1),    
    Rect(200,200,fill:SKColors.Bisque).Align(null,1),    
])
.AsClip(out var panel, 30, name:"panel");

WrapPanel([
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
    Rect(200,200,fill:SKColors.Green),        
]).Align(0,0)
.AsClip(out var wpanel, 30, name:"wpanel");

run(stage(1920, 1080, bg: SKColors.Orange), [hgrid, vgrid, hstack, vstack, panel, wpanel]);