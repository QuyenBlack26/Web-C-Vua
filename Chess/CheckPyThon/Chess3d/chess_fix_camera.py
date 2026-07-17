"""
Fix camera: thu hết 6 quân vào khung, căn giữa đẹp
Chạy script này trong Blender Scripting tab
"""
import bpy
import math

scene = bpy.context.scene
cam_obj = scene.camera

# Camera orthographic nhìn thẳng từ phía trước
cam_obj.location = (0, -9, 1.2)
cam_obj.rotation_euler = (math.radians(90), 0, 0)

cam = cam_obj.data
cam.type = 'ORTHO'
cam.ortho_scale = 11.5   # Đủ rộng để thấy hết 6 quân

# Render 1920x600 - ngang rộng vừa đủ 6 quân
scene.render.resolution_x = 1920
scene.render.resolution_y = 600
scene.render.film_transparent = True

print("✓ Camera đã chỉnh xong!")
print("Nhấn F12 để render lại.")