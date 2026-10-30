# SVG 动画模式与自包含检查

## 1. 画布与透明

`svg-animation-convert` 从根节点的 `width`、`height`、`viewBox` 推断输出分辨率，没有命令行覆盖尺寸的参数。

```svg
<svg xmlns="http://www.w3.org/2000/svg"
     width="512" height="512" viewBox="0 0 512 512"
     fill="none">
```

规则：

- `width:height` 与 `viewBox` 的比例一致，避免浏览器加黑边；
- 需要透明时不要画铺满画布的 `rect`；
- 需要局部底色时，只给具体图元加 `fill`；
- 所有运动元素留在 `viewBox` 内，边缘留 2–4px 余量，避免旋转/缩放时被裁切；
- 半透明叠加用 `fill-opacity` / `stroke-opacity`，不要在根节点设置 `opacity`。

## 2. SMIL 还是 CSS

| 方式 | 优点 | 注意 |
| --- | --- | --- |
| SMIL `<animate>` / `<animateTransform>` | 浏览器原生、时长易推断、适合图标 | 不能用事件触发；`begin` 必须可确定 |
| CSS `@keyframes` | 写法熟悉、缓动丰富 | 需要 `--duration` 更稳妥；注意 `transform-origin` |
| JS / GSAP | 表现力强 | 必须传 `--duration`；外部脚本可能导致空白帧 |

优先 SMIL；只有复杂时间线才用 CSS/JS。

## 3. 常用模式

### 3.1 淡入淡出

```svg
<circle cx="256" cy="256" r="80" fill="#4FA3D1">
  <animate attributeName="opacity"
           values="0;1;1;0"
           keyTimes="0;0.15;0.75;1"
           dur="3s" repeatCount="indefinite"/>
</circle>
```

- `values` 与 `keyTimes` 数量必须一致；
- 首尾值相同，循环才不跳变。

### 3.2 中心缩放脉冲

SMIL 的 `scale` 以坐标原点为中心；把图元放到一个已平移到画面中心的组里：

```svg
<g transform="translate(256 256)">
  <g>
    <circle cx="0" cy="0" r="80" fill="#F0B429"/>
    <animateTransform attributeName="transform"
                      type="scale"
                      values="1;1.08;1"
                      dur="1.6s" repeatCount="indefinite"/>
  </g>
</g>
```

不要直接对 `cx=256,cy=256` 的圆做 `scale`，否则会以左上角为原点放大。

### 3.3 围绕中心旋转

SMIL 显式写旋转中心：

```svg
<g>
  <circle cx="256" cy="256" r="176"
          stroke="#4FA3D1" stroke-width="24" fill="none"
          stroke-dasharray="260 846"/>
  <animateTransform attributeName="transform" type="rotate"
                    from="0 256 256" to="360 256 256"
                    dur="6s" repeatCount="indefinite"/>
</g>
```

CSS 必须加：

```css
#ring {
  transform-box: fill-box;
  transform-origin: center;
  animation: spin 6s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
```

### 3.4 路径生长 / 描边绘制

用 `pathLength="1"` 归一化路径长度，避免手算长度：

```svg
<path d="M80 256 C 160 80, 352 432, 432 256"
      fill="none" stroke="#4FA3D1" stroke-width="16"
      stroke-linecap="round"
      pathLength="1"
      stroke-dasharray="1"
      stroke-dashoffset="1">
  <animate attributeName="stroke-dashoffset"
           from="1" to="0"
           dur="2s" repeatCount="indefinite"/>
</path>
```

需要“画完停住再重来”时，用 `values="1;0;0"` 配合 `keyTimes`，或在末尾加 `fill="freeze"` 做一次性动画。

### 3.5 沿路径运动

```svg
<circle r="12" fill="#F0B429">
  <animateMotion dur="4s" repeatCount="indefinite"
                 path="M80 256 C 160 80, 352 432, 432 256"/>
</circle>
```

`animateMotion` 的 `path` 坐标与当前用户坐标系一致；元素初始 `cx/cy` 会被运动路径覆盖。

### 3.6 进度环

```svg
<circle cx="256" cy="256" r="160"
        fill="none" stroke="#4FA3D1" stroke-width="20"
        stroke-linecap="round"
        pathLength="1"
        stroke-dasharray="1"
        stroke-dashoffset="1">
  <animate attributeName="stroke-dashoffset"
           from="1" to="0"
           dur="3s" repeatCount="indefinite"/>
</circle>
```

### 3.7 多段动画共享周期

多个元素周期不同时，整体循环时长取它们的最小公倍数。例如 2s 和 6s 的动画，整体录 6s：

```bash
npx --yes svg-animation-convert --input ./assets/svgs/demo.svg --output ./assets/images/demo.apng --duration 6s --fps 24 --loop 0
mv ./assets/images/demo.apng ./assets/images/demo.png
```

若不想算公倍数，用一个总时长组包裹所有动画，或把所有 `dur` 改成同一个值。

## 4. 时间轴写法

- `dur`：一次动画时长；循环动画写 `repeatCount="indefinite"`；
- `begin`：默认 `0s`；不要用 `click`、`mouseover` 等事件触发；
- `fill="freeze"`：一次性动画结束后保持末态；
- `keyTimes`：0–1，数量与 `values` 一致；
- 首尾帧一致，循环才平滑；需要停顿就显式加一段相同值。

## 5. 自包含检查

转换前逐项确认：

- [ ] 根节点有 `width`、`height`、`viewBox`；
- [ ] 没有外部 `<image href="http...">`、外部 CSS、外部 JS；
- [ ] 没有依赖本机字体的 `<text>`（或已转成路径）；
- [ ] 没有铺满画布的不透明背景；
- [ ] 所有 `dur`/`repeatCount` 明确，无限循环有可确定的周期；
- [ ] 旋转/缩放中心正确；
- [ ] 运动元素没有超出 `viewBox`；
- [ ] 在浏览器中直接打开 SVG，动画表现与预期一致；
- [ ] 用 `--keep-frames` 抽查过首帧、中间帧、末帧。

## 6. 调试

保留中间帧：

```bash
npx --yes svg-animation-convert --input ./assets/svgs/demo.svg --output ./assets/images/demo.apng --duration 3s --fps 24 --keep-frames --frames ./tmp/demo-frames
mv ./assets/images/demo.apng ./assets/images/demo.png
```

检查 `./tmp/demo-frames`：

- 首帧是否为空；
- 中间帧是否在推进；
- 末帧是否与首帧衔接；
- 是否有元素被裁切或位置偏移。

排查完删除临时目录。
