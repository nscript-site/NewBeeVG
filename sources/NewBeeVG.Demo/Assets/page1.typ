#import "@preview/cetz:0.3.4": canvas, draw

#set page(fill: none)

#set page(
  width: 200pt,
  height: 600pt,
)

= 标题

这是正文。

$ a^2 + b^2 = c^2 $

#let frames = int(sys.inputs.at("frames", default: "0"))

#canvas(length: 20pt, {
  import draw: *

  set-style(stroke: 1pt + blue)
  line((0, 0), (4, 0))
  line((0, 0), (0, 3))
  line((0, 0), (4, 3))
  circle((2, 1.5 ), radius: 0.8 + frames/30.0, fill: aqua.lighten(40%))
})

#image("./snows.jpg", width: 200pt)