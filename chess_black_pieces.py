"""
=============================================================
  QUÂN CỜ ĐEN - BLACK TEAM
  Chạy AFTER chess_pieces_3d.py (quân trắng đã có sẵn)
  
  Quân đen sẽ xuất hiện ở hàng Y = -3 phía sau quân trắng
=============================================================
"""

import bpy
import math

# ── Hàm tiện ích ─────────────────────────────────────────

def add_cylinder(r, h, loc, segs=16, name="cyl"):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=h, location=loc, vertices=segs)
    obj = bpy.context.active_object
    obj.name = name
    return obj

def add_sphere(r, loc, name="sph"):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=24, ring_count=16)
    obj = bpy.context.active_object
    obj.name = name
    return obj

def add_cube(sx, sy, sz, loc, name="cube"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    return obj

def add_cone(r, h, loc, segs=16, name="cone"):
    bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=0, depth=h, location=loc, vertices=segs)
    obj = bpy.context.active_object
    obj.name = name
    return obj

def add_torus(r_maj, r_min, loc, name="tor"):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=r_maj, minor_radius=r_min,
        major_segments=24, minor_segments=12,
        location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj

def assign_mat(obj, mat):
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

def join_objects(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    bpy.context.active_object.name = name
    return bpy.context.active_object

def smooth_obj(obj):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()

# ── Materials quân đen ────────────────────────────────────
def make_material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

MAT_B = {
    "body":   make_material("B_body",   (0.06, 0.05, 0.04), metallic=0.1, roughness=0.4),
    "skin":   make_material("B_skin",   (0.18, 0.10, 0.06), metallic=0.0, roughness=0.7),
    "gold":   make_material("B_gold",   (0.60, 0.45, 0.05), metallic=0.9, roughness=0.2),
    "silver": make_material("B_silver", (0.35, 0.35, 0.38), metallic=0.9, roughness=0.3),
    "red":    make_material("B_red",    (0.50, 0.02, 0.02), metallic=0.0, roughness=0.5),
    "blue":   make_material("B_blue",   (0.02, 0.06, 0.35), metallic=0.0, roughness=0.5),
    "purple": make_material("B_purple", (0.22, 0.02, 0.38), metallic=0.1, roughness=0.4),
    "brown":  make_material("B_brown",  (0.20, 0.09, 0.02), metallic=0.0, roughness=0.6),
    "cape":   make_material("B_cape",   (0.08, 0.06, 0.05), metallic=0.0, roughness=0.8),
    "accent": make_material("B_accent", (0.55, 0.40, 0.02), metallic=0.8, roughness=0.3),
}

# ── Thân người đen ────────────────────────────────────────
def make_base_black(ox, oy, cape=False):
    parts = []

    base = add_cylinder(0.42, 0.08, (ox, oy, 0.04), name="b_base")
    assign_mat(base, MAT_B["body"]); smooth_obj(base); parts.append(base)

    for dx, nm in [(-0.12,"b_fl"),(0.12,"b_fr")]:
        f = add_cylinder(0.10, 0.18, (ox+dx, oy, 0.13), name=nm)
        assign_mat(f, MAT_B["body"]); smooth_obj(f); parts.append(f)

    for dx, nm in [(-0.12,"b_ll"),(0.12,"b_lr")]:
        l = add_cylinder(0.08, 0.28, (ox+dx, oy, 0.32), name=nm)
        assign_mat(l, MAT_B["body"]); smooth_obj(l); parts.append(l)

    hip = add_cylinder(0.22, 0.14, (ox, oy, 0.52), name="b_hip")
    assign_mat(hip, MAT_B["body"]); smooth_obj(hip); parts.append(hip)

    torso = add_cylinder(0.20, 0.30, (ox, oy, 0.74), segs=12, name="b_torso")
    assign_mat(torso, MAT_B["body"]); smooth_obj(torso); parts.append(torso)

    for dx, nm in [(-0.28,"b_sl"),(0.28,"b_sr")]:
        s = add_sphere(0.11, (ox+dx, oy, 0.85), name=nm)
        assign_mat(s, MAT_B["body"]); smooth_obj(s); parts.append(s)

    neck = add_cylinder(0.07, 0.10, (ox, oy, 0.94), name="b_neck")
    assign_mat(neck, MAT_B["skin"]); smooth_obj(neck); parts.append(neck)

    head = add_sphere(0.18, (ox, oy, 1.12), name="b_head")
    assign_mat(head, MAT_B["skin"]); smooth_obj(head); parts.append(head)

    for dx, nm in [(-0.07,"b_el"),(0.07,"b_er")]:
        e = add_sphere(0.03, (ox+dx, oy-0.16, 1.15), name=nm)
        assign_mat(e, MAT_B["gold"]); smooth_obj(e); parts.append(e)

    if cape:
        cp = add_cylinder(0.21, 0.28, (ox, oy+0.04, 0.74), segs=8, name="b_cape")
        cp.scale.y = 0.5
        bpy.ops.object.transform_apply(scale=True)
        assign_mat(cp, MAT_B["cape"]); smooth_obj(cp); parts.append(cp)

    return parts

# ═══════════════════════════════════════════════════════════
#  PAWN ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_pawn(ox, oy):
    parts = make_base_black(ox, oy)

    helm = add_sphere(0.20, (ox, oy, 1.12), name="b_helm")
    assign_mat(helm, MAT_B["silver"]); smooth_obj(helm); parts.append(helm)
    rim = add_torus(0.19, 0.03, (ox, oy, 1.04), name="b_helm_rim")
    assign_mat(rim, MAT_B["gold"]); smooth_obj(rim); parts.append(rim)

    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="b_arm_r")
    assign_mat(arm_r, MAT_B["body"]); smooth_obj(arm_r); parts.append(arm_r)
    spear = add_cylinder(0.025, 0.55, (ox+0.28, oy, 0.50), name="b_spear")
    assign_mat(spear, MAT_B["brown"]); smooth_obj(spear); parts.append(spear)
    tip = add_cone(0.06, 0.12, (ox+0.28, oy, 0.23), name="b_tip")
    assign_mat(tip, MAT_B["silver"]); smooth_obj(tip); parts.append(tip)

    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="b_arm_l")
    assign_mat(arm_l, MAT_B["body"]); smooth_obj(arm_l); parts.append(arm_l)
    shield = add_cylinder(0.18, 0.04, (ox-0.44, oy, 0.72), segs=20, name="b_shield")
    shield.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(shield, MAT_B["silver"]); smooth_obj(shield); parts.append(shield)
    boss = add_sphere(0.05, (ox-0.46, oy, 0.72), name="b_boss")
    assign_mat(boss, MAT_B["gold"]); smooth_obj(boss); parts.append(boss)

    return join_objects(parts, "B_Pawn")

# ═══════════════════════════════════════════════════════════
#  ROOK ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_rook(ox, oy):
    parts = make_base_black(ox, oy, cape=True)

    hb = add_cylinder(0.21, 0.22, (ox, oy, 1.22), segs=4, name="b_tower")
    assign_mat(hb, MAT_B["silver"]); smooth_obj(hb); parts.append(hb)
    for dx, dy in [(-0.10,0),(0.10,0),(0,-0.10),(0,0.10)]:
        m = add_cube(0.06, 0.06, 0.10, (ox+dx, oy+dy, 1.40), name="b_merlon")
        assign_mat(m, MAT_B["silver"]); parts.append(m)

    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="b_arm_r")
    assign_mat(arm_r, MAT_B["body"]); smooth_obj(arm_r); parts.append(arm_r)
    hh = add_cylinder(0.03, 0.42, (ox+0.28, oy, 0.52), name="b_hammer_h")
    assign_mat(hh, MAT_B["brown"]); smooth_obj(hh); parts.append(hh)
    hhead = add_cube(0.14, 0.08, 0.10, (ox+0.28, oy, 0.30), name="b_hammer_head")
    assign_mat(hhead, MAT_B["silver"]); parts.append(hhead)

    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="b_arm_l")
    assign_mat(arm_l, MAT_B["body"]); smooth_obj(arm_l); parts.append(arm_l)
    scroll = add_cylinder(0.05, 0.28, (ox-0.44, oy, 0.72), name="b_scroll")
    scroll.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(scroll, MAT_B["cape"]); smooth_obj(scroll); parts.append(scroll)

    return join_objects(parts, "B_Rook")

# ═══════════════════════════════════════════════════════════
#  KNIGHT ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_knight(ox, oy):
    parts = make_base_black(ox, oy, cape=True)

    helm = add_sphere(0.21, (ox, oy, 1.14), name="b_helm_k")
    assign_mat(helm, MAT_B["silver"]); smooth_obj(helm); parts.append(helm)
    visor = add_cube(0.16, 0.04, 0.08, (ox, oy-0.18, 1.10), name="b_visor")
    assign_mat(visor, MAT_B["gold"]); parts.append(visor)
    for i in range(3):
        f = add_cylinder(0.025, 0.28+i*0.04, (ox-0.04+i*0.04, oy, 1.28+i*0.06), name=f"b_feather_{i}")
        f.rotation_euler = (math.radians(15), 0, math.radians(-10+i*10))
        bpy.ops.object.transform_apply(rotation=True)
        assign_mat(f, MAT_B["red"]); smooth_obj(f); parts.append(f)

    arm_r = add_cylinder(0.06, 0.22, (ox+0.30, oy, 0.78), name="b_arm_r")
    assign_mat(arm_r, MAT_B["body"]); smooth_obj(arm_r); parts.append(arm_r)
    blade = add_cube(0.03, 0.03, 0.55, (ox+0.30, oy, 0.48), name="b_blade")
    assign_mat(blade, MAT_B["silver"]); parts.append(blade)
    guard = add_cube(0.18, 0.03, 0.04, (ox+0.30, oy, 0.74), name="b_guard")
    assign_mat(guard, MAT_B["gold"]); parts.append(guard)
    pommel = add_sphere(0.05, (ox+0.30, oy, 0.24), name="b_pommel")
    assign_mat(pommel, MAT_B["gold"]); smooth_obj(pommel); parts.append(pommel)

    arm_l = add_cylinder(0.06, 0.28, (ox-0.30, oy, 0.80), name="b_arm_l")
    arm_l.rotation_euler = (0, math.radians(25), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(arm_l, MAT_B["body"]); smooth_obj(arm_l); parts.append(arm_l)

    return join_objects(parts, "B_Knight")

# ═══════════════════════════════════════════════════════════
#  BISHOP ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_bishop(ox, oy):
    parts = make_base_black(ox, oy, cape=True)

    robe = add_cylinder(0.25, 0.60, (ox, oy, 0.60), segs=12, name="b_robe")
    assign_mat(robe, MAT_B["cape"]); smooth_obj(robe); parts.append(robe)

    mb = add_cylinder(0.18, 0.12, (ox, oy, 1.24), segs=16, name="b_mitre_base")
    assign_mat(mb, MAT_B["gold"]); smooth_obj(mb); parts.append(mb)
    mt = add_cone(0.18, 0.38, (ox, oy, 1.44), segs=16, name="b_mitre_top")
    assign_mat(mt, MAT_B["body"]); smooth_obj(mt); parts.append(mt)
    cv = add_cube(0.03, 0.02, 0.20, (ox, oy-0.17, 1.32), name="b_cross_v")
    assign_mat(cv, MAT_B["gold"]); parts.append(cv)
    ch = add_cube(0.12, 0.02, 0.03, (ox, oy-0.17, 1.36), name="b_cross_h")
    assign_mat(ch, MAT_B["gold"]); parts.append(ch)

    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="b_arm_r")
    assign_mat(arm_r, MAT_B["body"]); smooth_obj(arm_r); parts.append(arm_r)
    staff = add_cylinder(0.03, 0.80, (ox+0.38, oy, 0.50), name="b_staff")
    assign_mat(staff, MAT_B["gold"]); smooth_obj(staff); parts.append(staff)
    st = add_torus(0.07, 0.025, (ox+0.38, oy, 0.92), name="b_staff_top")
    assign_mat(st, MAT_B["gold"]); smooth_obj(st); parts.append(st)
    gem = add_sphere(0.05, (ox+0.38, oy, 0.92), name="b_gem")
    assign_mat(gem, MAT_B["purple"]); smooth_obj(gem); parts.append(gem)

    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="b_arm_l")
    assign_mat(arm_l, MAT_B["body"]); smooth_obj(arm_l); parts.append(arm_l)
    book = add_cube(0.14, 0.04, 0.18, (ox-0.40, oy, 0.72), name="b_book")
    assign_mat(book, MAT_B["brown"]); parts.append(book)

    return join_objects(parts, "B_Bishop")

# ═══════════════════════════════════════════════════════════
#  QUEEN ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_queen(ox, oy):
    parts = make_base_black(ox, oy, cape=True)

    skirt = add_cone(0.40, 0.55, (ox, oy, 0.50), segs=20, name="b_skirt")
    skirt.scale.z = -1
    bpy.ops.object.transform_apply(scale=True)
    skirt.location.z = 0.78
    assign_mat(skirt, MAT_B["cape"]); smooth_obj(skirt); parts.append(skirt)

    belt = add_torus(0.21, 0.03, (ox, oy, 0.61), name="b_belt")
    assign_mat(belt, MAT_B["gold"]); smooth_obj(belt); parts.append(belt)

    corset = add_cylinder(0.19, 0.24, (ox, oy, 0.74), segs=16, name="b_corset")
    assign_mat(corset, MAT_B["blue"]); smooth_obj(corset); parts.append(corset)

    crown_ring = add_torus(0.17, 0.035, (ox, oy, 1.28), name="b_crown_ring")
    assign_mat(crown_ring, MAT_B["gold"]); smooth_obj(crown_ring); parts.append(crown_ring)
    for i in range(5):
        angle = i * (2 * math.pi / 5)
        cx = ox + 0.17 * math.cos(angle)
        cy = oy + 0.17 * math.sin(angle)
        peak = add_cone(0.04, 0.12, (cx, cy, 1.34), segs=8, name=f"b_peak_{i}")
        assign_mat(peak, MAT_B["gold"]); smooth_obj(peak); parts.append(peak)
        jewel = add_sphere(0.03, (cx, cy, 1.30), name=f"b_jewel_{i}")
        assign_mat(jewel, MAT_B["red"]); smooth_obj(jewel); parts.append(jewel)

    arm_l = add_cylinder(0.055, 0.24, (ox-0.28, oy, 0.78), name="b_arm_l")
    arm_l.rotation_euler = (0, math.radians(-20), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(arm_l, MAT_B["body"]); smooth_obj(arm_l); parts.append(arm_l)

    arm_r = add_cylinder(0.055, 0.24, (ox+0.28, oy, 0.78), name="b_arm_r")
    assign_mat(arm_r, MAT_B["body"]); smooth_obj(arm_r); parts.append(arm_r)
    scepter = add_cylinder(0.025, 0.55, (ox+0.40, oy, 0.58), name="b_scepter")
    assign_mat(scepter, MAT_B["gold"]); smooth_obj(scepter); parts.append(scepter)
    orb = add_sphere(0.07, (ox+0.40, oy, 0.88), name="b_orb")
    assign_mat(orb, MAT_B["purple"]); smooth_obj(orb); parts.append(orb)
    orb_band = add_torus(0.07, 0.015, (ox+0.40, oy, 0.88), name="b_orb_band")
    assign_mat(orb_band, MAT_B["gold"]); smooth_obj(orb_band); parts.append(orb_band)

    return join_objects(parts, "B_Queen")

# ═══════════════════════════════════════════════════════════
#  KING ĐEN
# ═══════════════════════════════════════════════════════════
def make_black_king(ox, oy):
    parts = make_base_black(ox, oy, cape=True)

    chest = add_cylinder(0.23, 0.28, (ox, oy, 0.75), segs=16, name="b_chest")
    assign_mat(chest, MAT_B["silver"]); smooth_obj(chest); parts.append(chest)
    cd = add_cube(0.06, 0.04, 0.14, (ox, oy-0.21, 0.78), name="b_chest_cross")
    assign_mat(cd, MAT_B["gold"]); parts.append(cd)

    cape = add_cone(0.30, 0.65, (ox, oy+0.05, 0.56), segs=16, name="b_royal_cape")
    cape.scale.z = -1
    bpy.ops.object.transform_apply(scale=True)
    cape.location.z = 0.88
    assign_mat(cape, MAT_B["red"]); smooth_obj(cape); parts.append(cape)

    cb = add_cylinder(0.20, 0.08, (ox, oy, 1.26), segs=20, name="b_crown_base")
    assign_mat(cb, MAT_B["gold"]); smooth_obj(cb); parts.append(cb)
    for i in range(6):
        angle = i * (2 * math.pi / 6)
        cx = ox + 0.19 * math.cos(angle)
        cy = oy + 0.19 * math.sin(angle)
        spk = add_cone(0.045, 0.20, (cx, cy, 1.36), segs=8, name=f"b_spike_{i}")
        assign_mat(spk, MAT_B["gold"]); smooth_obj(spk); parts.append(spk)
    for i in range(3):
        angle = i * (2 * math.pi / 3) + math.pi/6
        cx = ox + 0.19 * math.cos(angle)
        cy = oy + 0.19 * math.sin(angle)
        gem = add_sphere(0.04, (cx, cy, 1.28), name=f"b_cgem_{i}")
        assign_mat(gem, [MAT_B["red"], MAT_B["blue"], MAT_B["purple"]][i])
        smooth_obj(gem); parts.append(gem)

    arm_r = add_cylinder(0.07, 0.22, (ox+0.30, oy, 0.78), name="b_arm_r")
    assign_mat(arm_r, MAT_B["silver"]); smooth_obj(arm_r); parts.append(arm_r)
    mh = add_cylinder(0.03, 0.45, (ox+0.40, oy, 0.58), name="b_mace_h")
    assign_mat(mh, MAT_B["gold"]); smooth_obj(mh); parts.append(mh)
    mhead = add_sphere(0.10, (ox+0.40, oy, 0.88), name="b_mace_head")
    assign_mat(mhead, MAT_B["gold"]); smooth_obj(mhead); parts.append(mhead)

    arm_l = add_cylinder(0.07, 0.22, (ox-0.30, oy, 0.78), name="b_arm_l")
    assign_mat(arm_l, MAT_B["silver"]); smooth_obj(arm_l); parts.append(arm_l)
    shield = add_cube(0.24, 0.04, 0.30, (ox-0.46, oy, 0.76), name="b_shield")
    assign_mat(shield, MAT_B["blue"]); parts.append(shield)
    sr = add_cube(0.26, 0.03, 0.32, (ox-0.46, oy+0.02, 0.76), name="b_shield_rim")
    assign_mat(sr, MAT_B["gold"]); parts.append(sr)

    return join_objects(parts, "B_King")

# ── Tạo 6 quân đen ───────────────────────────────────────
print("Đang tạo quân cờ ĐEN...")

SPACING = 1.4
ROW_Y = -3.0  # Hàng đen đặt phía sau hàng trắng

make_black_pawn(  -SPACING*2.5, ROW_Y)
make_black_rook(  -SPACING*1.5, ROW_Y)
make_black_knight(-SPACING*0.5, ROW_Y)
make_black_bishop( SPACING*0.5, ROW_Y)
make_black_queen(  SPACING*1.5, ROW_Y)
make_black_king(   SPACING*2.5, ROW_Y)

print("=" * 50)
print("✓ Xong! 6 quân ĐEN đã được tạo!")
print("Quân đen ở hàng Y = -3, phía sau quân trắng.")
print("")
print("Để render cả 2 hàng:")
print("  Chạy chess_fix_camera_both.py → F12")
print("=" * 50)
