// 用 cetz 逐帧绘制函数动画：y = sin(x - t)，t 随帧推进
// 编译：typst compile anim_plot.typ "frames/f-{0p}.png" --ppi 200
#import "@preview/cetz:0.3.4": canvas, draw

#set page(width: 400pt, height: 240pt, margin: 0pt)
#set text(font: "Microsoft YaHei", size: 8.5pt)

#let N = 60
#let PI = calc.pi
#let xmin = -2 * PI
#let xmax = 2 * PI
#let ymin = -1.75
#let ymax = 1.75
#let W = 400.0
#let H = 240.0
#let L = 46.0
#let R = 22.0
#let T = 22.0
#let Bm = 34.0
#let PW = W - L - R
#let PH = H - T - Bm
#let cx(x) = L + (x - xmin) / (xmax - xmin) * PW
#let cy(y) = Bm + (y - ymin) / (ymax - ymin) * PH

// 采样点：x ∈ [-2π, 2π]，步长 0.02
#let xs = range(-314, 315).map(n => n / 50.0)

#let frame(t) = canvas(length: 1pt, {
  import draw: *

  // 网格（每 π/2 一条竖线，每 0.5 一条横线）
  for gx in range(-4, 5).map(k => k * PI / 2) {
    line(
      (cx(gx), cy(ymin)),
      (cx(gx), cy(ymax)),
      stroke: (paint: rgb("#e8ebf1"), thickness: 0.6pt),
    )
  }
  for gy in (-1.5, -1.0, -0.5, 0.5, 1.0, 1.5) {
    line(
      (cx(xmin), cy(gy)),
      (cx(xmax), cy(gy)),
      stroke: (paint: rgb("#e8ebf1"), thickness: 0.6pt),
    )
  }

  // 参照波形 sin(x)（静止，浅灰虚线）
  let reference = xs.map(x => (cx(x), cy(calc.sin(x))))
  line(..reference, stroke: (paint: rgb("#c2c8d2"), thickness: 1pt, dash: "dashed"))

  // 坐标轴
  line(
    (cx(xmin) - 6, cy(0)),
    (cx(xmax) + 8, cy(0)),
    mark: (end: ">"),
    stroke: (paint: rgb("#1f2937"), thickness: 0.9pt),
  )
  line(
    (cx(0), cy(ymin) - 6),
    (cx(0), cy(ymax) + 8),
    mark: (end: ">"),
    stroke: (paint: rgb("#1f2937"), thickness: 0.9pt),
  )

  // 刻度标签
  content((cx(2 * PI), cy(0) - 6), [$2 pi$], anchor: "north")
  content((cx(PI), cy(0) - 6), [$pi$], anchor: "north")
  content((cx(-PI), cy(0) - 6), [$-pi$], anchor: "north")
  content((cx(-2 * PI), cy(0) - 6), [$-2 pi$], anchor: "north")
  content((cx(0) - 10, cy(1)), [1], anchor: "east")
  content((cx(0) - 10, cy(-1)), [-1], anchor: "east")
  content((cx(xmax) + 12, cy(0) + 2), [$x$], anchor: "west")
  content((cx(0), cy(ymax) + 9), [$y$], anchor: "south")

  // 标题（静止）
  content((L, H - 10), [$y = sin(x - t)$], anchor: "west")

  // 运动波形 sin(x - t)
  let wave = xs.map(x => (cx(x), cy(calc.sin(x - t))))
  line(..wave, stroke: (paint: rgb("#2563eb"), thickness: 2.2pt))

  // 波峰上的小红点（跟着波峰移动，强化位移感）
  for k in range(-3, 4) {
    let xc = t + PI / 2 + 2 * PI * k
    if xc >= xmin and xc <= xmax {
      circle((cx(xc), cy(1)), radius: 2.6, fill: rgb("#ef4444"))
    }
  }
})

// 逐页输出每一帧；weak 页断避免末尾多出空白页
#for i in range(N) {
  let t = 2 * PI * i / N
  [ #frame(t) #pagebreak(weak: true) ]
}
