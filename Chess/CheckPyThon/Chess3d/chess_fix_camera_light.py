"""
Fix camera + lighting cho Epic Fantasy Chess
Chạy script này sau chess_epic_fantasy.py
"""
import bpy, math

scene = bpy.context.scene

# ── Xóa camera/light cũ nếu có ──────────────────────────
for obj in list(bpy.data.objects):
    if obj.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(obj, do_unlink=True)

# ── Thêm Camera mới ──────────────────────────────────────
bpy.ops.object.camera_add(location=(0, -11, 5.5))
cam = bpy.context.active_object
cam.name = "MainCamera"
cam.rotation_euler = (math.radians(60), 0, 0)
cam.data.type = 'PERSP'
cam.data.lens = 45
scene.camera = cam   # ← gán làm camera chính
print("✓ Camera đã thêm và gán xong")

# ── Đèn chính (Key Light) ────────────────────────────────
bpy.ops.object.light_add(type='SUN', location=(5, -6, 12))
sun = bpy.context.active_object
sun.name = "KeyLight"
sun.data.energy = 5.0
sun.rotation_euler = (math.radians(38), 0, math.radians(28))
print("✓ Key light OK")

# ── Đèn fill (Fill Light) ────────────────────────────────
bpy.ops.object.light_add(type='AREA', location=(-6, 4, 7))
fill = bpy.context.active_object
fill.name = "FillLight"
fill.data.energy = 800
fill.data.size = 6
print("✓ Fill light OK")

# ── Rim đỏ cho quân đen ──────────────────────────────────
bpy.ops.object.light_add(type='SPOT', location=(0, 5, 2))
rim = bpy.context.active_object
rim.name = "RimLight"
rim.data.energy = 1500
rim.data.color = (1.0, 0.05, 0.0)
rim.data.spot_size = math.radians(60)
rim.rotation_euler = (math.radians(-50), 0, 0)
print("✓ Rim light (đỏ) OK")

# ── World background tối ─────────────────────────────────
world = bpy.data.worlds.get("World")
if not world:
    world = bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.04, 0.02, 0.02, 1)
    bg.inputs[1].default_value = 0.15
print("✓ World background OK")

# ── Render settings ──────────────────────────────────────
scene.render.engine = 'CYCLES'
scene.cycles.samples = 128
scene.render.resolution_x = 1920
scene.render.resolution_y = 720
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
print("✓ Render settings OK")

print("=" * 50)
print("✓ XONG! Camera + Lighting đã fix!")
print("→ Nhấn F12 để render ngay!")
print("=" * 50)
