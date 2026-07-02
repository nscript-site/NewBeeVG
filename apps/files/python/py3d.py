import os
import tempfile
import webbrowser

import imageio.v3 as iio
import numpy as np
import pygfx as gfx
import pylinalg as la
from rendercanvas.offscreen import RenderCanvas

# Create offscreen canvas, renderer and scene
canvas = RenderCanvas(size=(640, 480), pixel_ratio=1)
renderer = gfx.renderers.WgpuRenderer(canvas)
scene = gfx.Scene()
scene.add(gfx.Background.from_color("#00000000"))

im = iio.imread("imageio:astronaut.png").astype("float32") / 255
tex = gfx.Texture(im, dim=2)

geometry = gfx.box_geometry(200, 200, 200)
material = gfx.MeshBasicMaterial(map=tex)
cube = gfx.Mesh(geometry, material)
scene.add(cube)

camera = gfx.PerspectiveCamera(70, 16 / 9)
camera.local.z = 400

rot = la.quat_from_euler((0.5, 1.0), order="XY")
cube.local.rotation = la.quat_mul(rot, cube.local.rotation)

def cvs_draw(localz = 400):
    camera.local.z = localz
    canvas.request_draw(lambda: renderer.render(scene, camera))

def cvs_draw_data(localz = 400):
    camera.local.z = localz
    canvas.request_draw(lambda: renderer.render(scene, camera))
    im_rgba = np.asarray(canvas.draw())
    im_bgra = im_rgba[..., [2, 1, 0, 3]]
    im_data = im_bgra.tobytes()
    im_size = im_rgba.shape
    w = im_size[1]
    h = im_size[0]
    return w,h,im_data

if __name__ == "__main__":
    cvs_draw(200)
    # The offscreen canvas has a draw method that returns a memoryview.
    # Use this to obtain what you normally see on-screen. You should
    # only use an offscreen canvas for e.g. testing or generating images.
    im1 = np.asarray(canvas.draw())
    print("image from canvas.draw():", im1.shape)  # (480, 640, 4)

    # The renderer also has a snapshot utility. With this you get a snapshot
    # of the internal state (might be at a higher resolution).
    # The use of the snapshot method may change and be improved.
    im2 = renderer.snapshot()
    print("Image from renderer.snapshot():", im2.shape)  # (960, 1280, 4)

    filename = os.path.join(tempfile.gettempdir(), "pygfx_offscreen.png")
    iio.imwrite(filename, im1)
    webbrowser.open("file://" + filename)