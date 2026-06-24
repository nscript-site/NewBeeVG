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

if __name__ == "__main__":
    w, h, raw_bin = plot_3d_data(0)
    print(f"w={w}, h={h}")
    print(f"Generated raw data size: {len(raw_bin)} bytes")
    _, plt = plot_3d(0)
    plt.savefig("output.png")
    plt.close()