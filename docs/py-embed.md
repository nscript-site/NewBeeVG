# 内嵌 python

NewBeeVG 默认支持 windows 下的 python-3.12.8-embed-amd64 嵌入式 python 环境。如果您将 python-3.12.8-embed-amd64 安装在 ../_lib/python-3.12.8-embed-amd64 目录下，在脚本中，embed_python312_win32(); 一行代码即可加载 python 环境

python 端示例如下:

plot.py - 通过 matplotlib 绘制图像

```python
import numpy as np
import io
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

def plot_3d(angle):
    """
    绘制3D曲面图
    """
    # 1. 生成网格数据
    x = np.linspace(-np.pi, np.pi, 50)
    y = np.linspace(-np.pi, np.pi, 50)
    X, Y = np.meshgrid(x, y)

    # 曲面函数 z = sin(x) * cos(y)
    Z = np.sin(X + angle) * np.cos(Y)

    # 2. 创建3D画布
    fig = plt.figure(figsize=(10, 7))
    fig.patch.set_alpha(0)  # 画布外层背景完全透明
    ax = fig.add_subplot(111, projection="3d")

    # ========= 关键：清除3D面板白色底板 =========
    ax.xaxis.pane.fill = False
    ax.yaxis.pane.fill = False
    ax.zaxis.pane.fill = False

    # 3. 绘制曲面
    surf = ax.plot_surface(
        X, Y, Z,
        cmap="viridis",    # 配色
        linewidth=0,
        antialiased=True
    )
    
    return fig,plt

def plot_3d_data(angle):
    fig,plt = plot_3d(angle)
    
    # 强制渲染画布
    fig.canvas.draw()
    # 直接获取画布RGBA像素数组 (H,W,4)，空白区域Alpha=0透明
    rgba_arr = np.array(fig.canvas.buffer_rgba())
    h, w = rgba_arr.shape[:2]
    raw_bin = rgba_arr.tobytes()

    plt.close(fig)
    return w, h, raw_bin

# if __name__ == "__main__":
    # w, h, raw_bin = plot_3d_data(0)
    # print(f"w={w}, h={h}")
    # print(f"Generated raw data size: {len(raw_bin)} bytes")
    # _, plt = plot_3d(0)
    # plt.savefig("output.png")
    # plt.close()
```

NewBeeVG 封装了 PythonNet，通过 py_module 可以加载 python 模块，通过 dynamic 方式调用其中的方法。注意，调用 python 方法返回值应该是 w,h,raw_data。 py_imdecode 会把返回值解码为 SKBitmap。调用示例如下：

```csharp
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
```

目前仅测试了 windows 下的 python 嵌入，linux 和 mac osx 环境下未测试。