"""
Camera nhìn cả 2 hàng: trắng + đen
Chạy sau khi có đủ cả 2 bộ quân cờ
"""
import bpy, math

scene = bpy.context.scene
cam_obj = scene.camera

# Camera từ trên chéo xuống để thấy 2 hàng
cam_obj.location = (0, -9, 5)
cam_obj.rotation_euler = (math.radians(62), 0, 0)

cam = cam_obj.data
cam.type = 'ORTHO'
cam.ortho_scale = 14.0

# Render đủ rộng thấy 2 hàng
scene.render.resolution_x = 1920
scene.render.resolution_y = 720
scene.render.film_transparent = True

print("✓ Camera chỉnh xong! Nhấn F12 để render cả 2 hàng.")
