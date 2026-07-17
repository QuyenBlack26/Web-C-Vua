import bpy
import math
from mathutils import Vector

# ── Xóa scene cũ ──────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

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

def make_material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

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

# ── Materials ────────────────────────────────────────────
MAT = {
    "skin":   make_material("Skin",    (0.95, 0.80, 0.65), metallic=0.0, roughness=0.7),
    "white":  make_material("White",   (0.95, 0.92, 0.85), metallic=0.1, roughness=0.4),
    "black":  make_material("Black",   (0.10, 0.08, 0.06), metallic=0.1, roughness=0.4),
    "gold":   make_material("Gold",    (0.83, 0.67, 0.10), metallic=0.9, roughness=0.2),
    "silver": make_material("Silver",  (0.75, 0.75, 0.78), metallic=0.9, roughness=0.2),
    "red":    make_material("Red",     (0.70, 0.05, 0.05), metallic=0.0, roughness=0.5),
    "blue":   make_material("Blue",    (0.05, 0.15, 0.60), metallic=0.0, roughness=0.5),
    "purple": make_material("Purple",  (0.35, 0.05, 0.55), metallic=0.1, roughness=0.4),
    "brown":  make_material("Brown",   (0.35, 0.18, 0.05), metallic=0.0, roughness=0.6),
    "cape_w": make_material("CapeW",   (0.90, 0.85, 0.70), metallic=0.0, roughness=0.8),
    "cape_b": make_material("CapeB",   (0.08, 0.06, 0.04), metallic=0.0, roughness=0.8),
}

# ── Hàm tạo thân người cơ bản ────────────────────────────

def make_humanoid_base(ox, oy, armor_mat, cape_mat=None):
    """Tạo thân người cơ bản: đế, chân, thân, đầu"""
    parts = []

    # Đế tròn
    base = add_cylinder(0.42, 0.08, (ox, oy, 0.04), name="base")
    assign_mat(base, armor_mat); smooth_obj(base); parts.append(base)

    # Bàn chân trái/phải
    fl = add_cylinder(0.10, 0.18, (ox-0.12, oy, 0.13), name="foot_l")
    assign_mat(fl, armor_mat); smooth_obj(fl); parts.append(fl)
    fr = add_cylinder(0.10, 0.18, (ox+0.12, oy, 0.13), name="foot_r")
    assign_mat(fr, armor_mat); smooth_obj(fr); parts.append(fr)

    # Ống chân
    ll = add_cylinder(0.08, 0.28, (ox-0.12, oy, 0.32), name="leg_l")
    assign_mat(ll, armor_mat); smooth_obj(ll); parts.append(ll)
    lr = add_cylinder(0.08, 0.28, (ox+0.12, oy, 0.32), name="leg_r")
    assign_mat(lr, armor_mat); smooth_obj(lr); parts.append(lr)

    # Hông
    hip = add_cylinder(0.22, 0.14, (ox, oy, 0.52), name="hip")
    assign_mat(hip, armor_mat); smooth_obj(hip); parts.append(hip)

    # Thân (giáp ngực)
    torso = add_cylinder(0.20, 0.30, (ox, oy, 0.74), segs=12, name="torso")
    assign_mat(torso, armor_mat); smooth_obj(torso); parts.append(torso)

    # Vai trái/phải
    sl = add_sphere(0.11, (ox-0.28, oy, 0.85), name="shoulder_l")
    assign_mat(sl, armor_mat); smooth_obj(sl); parts.append(sl)
    sr = add_sphere(0.11, (ox+0.28, oy, 0.85), name="shoulder_r")
    assign_mat(sr, armor_mat); smooth_obj(sr); parts.append(sr)

    # Cổ
    neck = add_cylinder(0.07, 0.10, (ox, oy, 0.94), name="neck")
    assign_mat(neck, MAT["skin"]); smooth_obj(neck); parts.append(neck)

    # Đầu
    head = add_sphere(0.18, (ox, oy, 1.12), name="head")
    assign_mat(head, MAT["skin"]); smooth_obj(head); parts.append(head)

    # Mắt trái/phải
    el = add_sphere(0.03, (ox-0.07, oy-0.16, 1.15), name="eye_l")
    assign_mat(el, MAT["black"]); smooth_obj(el); parts.append(el)
    er = add_sphere(0.03, (ox+0.07, oy-0.16, 1.15), name="eye_r")
    assign_mat(er, MAT["black"]); smooth_obj(er); parts.append(er)

    # Áo khoác / cape
    if cape_mat:
        cape = add_cylinder(0.21, 0.28, (ox, oy+0.04, 0.74), segs=8, name="cape")
        cape.scale.y = 0.5
        bpy.ops.object.transform_apply(scale=True)
        assign_mat(cape, cape_mat); smooth_obj(cape); parts.append(cape)

    return parts

# ═══════════════════════════════════════════════════════════
#   1. PAWN — Tốt: chiến binh đơn giản với khiên tròn
# ═══════════════════════════════════════════════════════════
def make_pawn(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"])

    # Mũ giáp đơn giản
    helm = add_sphere(0.20, (ox, oy, 1.12), name="helm")
    assign_mat(helm, MAT["silver"]); smooth_obj(helm); parts.append(helm)
    helm_rim = add_torus(0.19, 0.03, (ox, oy, 1.04), name="helm_rim")
    assign_mat(helm_rim, MAT["gold"]); smooth_obj(helm_rim); parts.append(helm_rim)

    # Tay phải cầm giáo ngắn
    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="arm_r")
    assign_mat(arm_r, MAT["white"]); smooth_obj(arm_r); parts.append(arm_r)
    spear = add_cylinder(0.025, 0.55, (ox+0.28, oy, 0.50), name="spear")
    assign_mat(spear, MAT["brown"]); smooth_obj(spear); parts.append(spear)
    spear_tip = add_cone(0.06, 0.12, (ox+0.28, oy, 0.23), name="spear_tip")
    assign_mat(spear_tip, MAT["silver"]); smooth_obj(spear_tip); parts.append(spear_tip)

    # Tay trái cầm khiên tròn
    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="arm_l")
    assign_mat(arm_l, MAT["white"]); smooth_obj(arm_l); parts.append(arm_l)
    shield = add_cylinder(0.18, 0.04, (ox-0.44, oy, 0.72), segs=20, name="shield")
    shield.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(shield, MAT["silver"]); smooth_obj(shield); parts.append(shield)
    shield_boss = add_sphere(0.05, (ox-0.46, oy, 0.72), name="shield_boss")
    assign_mat(shield_boss, MAT["gold"]); smooth_obj(shield_boss); parts.append(shield_boss)

    return join_objects(parts, "Pawn")

# ═══════════════════════════════════════════════════════════
#   2. ROOK — Xe: kiến trúc sư cầm bản đồ và búa
# ═══════════════════════════════════════════════════════════
def make_rook(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"], MAT["cape_w"])

    # Mũ tháp (castle helmet)
    helm_body = add_cylinder(0.21, 0.22, (ox, oy, 1.22), segs=4, name="helm_tower")
    assign_mat(helm_body, MAT["silver"]); smooth_obj(helm_body); parts.append(helm_body)
    for dx, dy in [(-0.10,0),(0.10,0),(0,-0.10),(0,0.10)]:
        merlon = add_cube(0.06, 0.06, 0.10, (ox+dx, oy+dy, 1.40), name="merlon")
        assign_mat(merlon, MAT["silver"]); parts.append(merlon)

    # Tay phải: búa xây dựng
    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="arm_r")
    assign_mat(arm_r, MAT["white"]); smooth_obj(arm_r); parts.append(arm_r)
    hammer_h = add_cylinder(0.03, 0.42, (ox+0.28, oy, 0.52), name="hammer_h")
    assign_mat(hammer_h, MAT["brown"]); smooth_obj(hammer_h); parts.append(hammer_h)
    hammer_head = add_cube(0.14, 0.08, 0.10, (ox+0.28, oy, 0.30), name="hammer_head")
    assign_mat(hammer_head, MAT["silver"]); parts.append(hammer_head)

    # Tay trái: cuộn bản đồ
    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="arm_l")
    assign_mat(arm_l, MAT["white"]); smooth_obj(arm_l); parts.append(arm_l)
    scroll = add_cylinder(0.05, 0.28, (ox-0.44, oy, 0.72), name="scroll")
    scroll.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(scroll, MAT["cape_w"]); smooth_obj(scroll); parts.append(scroll)

    return join_objects(parts, "Rook")

# ═══════════════════════════════════════════════════════════
#   3. KNIGHT — Mã: hiệp sĩ với mũ lông chim và kiếm
# ═══════════════════════════════════════════════════════════
def make_knight(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"], MAT["cape_w"])

    # Mũ hiệp sĩ với lông chim
    helm = add_sphere(0.21, (ox, oy, 1.14), name="helm_knight")
    assign_mat(helm, MAT["silver"]); smooth_obj(helm); parts.append(helm)
    visor = add_cube(0.16, 0.04, 0.08, (ox, oy-0.18, 1.10), name="visor")
    assign_mat(visor, MAT["gold"]); parts.append(visor)
    # Lông chim trang trí
    for i in range(3):
        feather = add_cylinder(0.025, 0.28+i*0.04, (ox-0.04+i*0.04, oy, 1.28+i*0.06), name=f"feather_{i}")
        feather.rotation_euler = (math.radians(15), 0, math.radians(-10+i*10))
        bpy.ops.object.transform_apply(rotation=True)
        assign_mat(feather, MAT["red"]); smooth_obj(feather); parts.append(feather)

    # Tay phải: kiếm dài
    arm_r = add_cylinder(0.06, 0.22, (ox+0.30, oy, 0.78), name="arm_r")
    assign_mat(arm_r, MAT["white"]); smooth_obj(arm_r); parts.append(arm_r)
    sword_blade = add_cube(0.03, 0.03, 0.55, (ox+0.30, oy, 0.48), name="sword_blade")
    assign_mat(sword_blade, MAT["silver"]); parts.append(sword_blade)
    sword_guard = add_cube(0.18, 0.03, 0.04, (ox+0.30, oy, 0.74), name="sword_guard")
    assign_mat(sword_guard, MAT["gold"]); parts.append(sword_guard)
    sword_pommel = add_sphere(0.05, (ox+0.30, oy, 0.24), name="sword_pommel")
    assign_mat(sword_pommel, MAT["gold"]); smooth_obj(sword_pommel); parts.append(sword_pommel)

    # Tay trái: giơ lên
    arm_l = add_cylinder(0.06, 0.28, (ox-0.30, oy, 0.80), name="arm_l")
    arm_l.rotation_euler = (0, math.radians(25), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(arm_l, MAT["white"]); smooth_obj(arm_l); parts.append(arm_l)

    return join_objects(parts, "Knight")

# ═══════════════════════════════════════════════════════════
#   4. BISHOP — Tượng: tu sĩ với gậy phép và mũ nhọn
# ═══════════════════════════════════════════════════════════
def make_bishop(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"], MAT["cape_w"])

    # Áo tu sĩ dài che thân
    robe = add_cylinder(0.25, 0.60, (ox, oy, 0.60), segs=12, name="robe")
    assign_mat(robe, MAT["cape_w"]); smooth_obj(robe); parts.append(robe)

    # Mũ giám mục nhọn (mitre)
    mitre_base = add_cylinder(0.18, 0.12, (ox, oy, 1.24), segs=16, name="mitre_base")
    assign_mat(mitre_base, MAT["gold"]); smooth_obj(mitre_base); parts.append(mitre_base)
    mitre_top = add_cone(0.18, 0.38, (ox, oy, 1.44), segs=16, name="mitre_top")
    assign_mat(mitre_top, MAT["white"]); smooth_obj(mitre_top); parts.append(mitre_top)
    cross_v = add_cube(0.03, 0.02, 0.20, (ox, oy-0.17, 1.32), name="cross_v")
    assign_mat(cross_v, MAT["gold"]); parts.append(cross_v)
    cross_h = add_cube(0.12, 0.02, 0.03, (ox, oy-0.17, 1.36), name="cross_h")
    assign_mat(cross_h, MAT["gold"]); parts.append(cross_h)

    # Tay phải: gậy phép (crozier)
    arm_r = add_cylinder(0.06, 0.22, (ox+0.28, oy, 0.72), name="arm_r")
    assign_mat(arm_r, MAT["white"]); smooth_obj(arm_r); parts.append(arm_r)
    staff = add_cylinder(0.03, 0.80, (ox+0.38, oy, 0.50), name="staff")
    assign_mat(staff, MAT["gold"]); smooth_obj(staff); parts.append(staff)
    staff_top = add_torus(0.07, 0.025, (ox+0.38, oy, 0.92), name="staff_top")
    assign_mat(staff_top, MAT["gold"]); smooth_obj(staff_top); parts.append(staff_top)
    gem = add_sphere(0.05, (ox+0.38, oy, 0.92), name="gem")
    assign_mat(gem, MAT["purple"]); smooth_obj(gem); parts.append(gem)

    # Tay trái: sách thánh
    arm_l = add_cylinder(0.06, 0.22, (ox-0.28, oy, 0.72), name="arm_l")
    assign_mat(arm_l, MAT["white"]); smooth_obj(arm_l); parts.append(arm_l)
    book = add_cube(0.14, 0.04, 0.18, (ox-0.40, oy, 0.72), name="book")
    assign_mat(book, MAT["brown"]); parts.append(book)
    book_pages = add_cube(0.12, 0.02, 0.16, (ox-0.40, oy+0.03, 0.72), name="pages")
    assign_mat(book_pages, MAT["cape_w"]); parts.append(book_pages)

    return join_objects(parts, "Bishop")

# ═══════════════════════════════════════════════════════════
#   5. QUEEN — Hậu: nữ hoàng với váy và vương miện hoa
# ═══════════════════════════════════════════════════════════
def make_queen(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"], MAT["cape_w"])

    # Váy bồng (flared skirt)
    skirt = add_cone(0.40, 0.55, (ox, oy, 0.50), segs=20, name="skirt")
    skirt.scale.z = -1
    bpy.ops.object.transform_apply(scale=True)
    skirt.location.z = 0.78
    assign_mat(skirt, MAT["cape_w"]); smooth_obj(skirt); parts.append(skirt)

    # Thắt lưng vàng
    belt = add_torus(0.21, 0.03, (ox, oy, 0.61), name="belt")
    assign_mat(belt, MAT["gold"]); smooth_obj(belt); parts.append(belt)

    # Áo corset
    corset = add_cylinder(0.19, 0.24, (ox, oy, 0.74), segs=16, name="corset")
    assign_mat(corset, MAT["blue"]); smooth_obj(corset); parts.append(corset)

    # Vương miện hoa
    crown_ring = add_torus(0.17, 0.035, (ox, oy, 1.28), name="crown_ring")
    assign_mat(crown_ring, MAT["gold"]); smooth_obj(crown_ring); parts.append(crown_ring)
    for i in range(5):
        angle = i * (2 * math.pi / 5)
        cx = ox + 0.17 * math.cos(angle)
        cy = oy + 0.17 * math.sin(angle)
        peak = add_cone(0.04, 0.12, (cx, cy, 1.34), segs=8, name=f"crown_peak_{i}")
        assign_mat(peak, MAT["gold"]); smooth_obj(peak); parts.append(peak)
        jewel = add_sphere(0.03, (cx, cy, 1.30), name=f"jewel_{i}")
        assign_mat(jewel, MAT["red"]); smooth_obj(jewel); parts.append(jewel)

    # Tay trái: giơ duyên dáng
    arm_l = add_cylinder(0.055, 0.24, (ox-0.28, oy, 0.78), name="arm_l")
    arm_l.rotation_euler = (0, math.radians(-20), 0)
    bpy.ops.object.transform_apply(rotation=True)
    assign_mat(arm_l, MAT["white"]); smooth_obj(arm_l); parts.append(arm_l)

    # Tay phải: cầm quyền trượng
    arm_r = add_cylinder(0.055, 0.24, (ox+0.28, oy, 0.78), name="arm_r")
    assign_mat(arm_r, MAT["white"]); smooth_obj(arm_r); parts.append(arm_r)
    scepter = add_cylinder(0.025, 0.55, (ox+0.40, oy, 0.58), name="scepter")
    assign_mat(scepter, MAT["gold"]); smooth_obj(scepter); parts.append(scepter)
    orb = add_sphere(0.07, (ox+0.40, oy, 0.88), name="orb")
    assign_mat(orb, MAT["purple"]); smooth_obj(orb); parts.append(orb)
    orb_band = add_torus(0.07, 0.015, (ox+0.40, oy, 0.88), name="orb_band")
    assign_mat(orb_band, MAT["gold"]); smooth_obj(orb_band); parts.append(orb_band)

    return join_objects(parts, "Queen")

# ═══════════════════════════════════════════════════════════
#   6. KING — Vua: vua áo giáp hoàng gia + ngai vàng nhỏ
# ═══════════════════════════════════════════════════════════
def make_king(ox, oy):
    parts = make_humanoid_base(ox, oy, MAT["white"], MAT["cape_w"])

    # Áo giáp ngực hoành tráng
    chest = add_cylinder(0.23, 0.28, (ox, oy, 0.75), segs=16, name="chest_armor")
    assign_mat(chest, MAT["silver"]); smooth_obj(chest); parts.append(chest)
    chest_detail = add_cube(0.06, 0.04, 0.14, (ox, oy-0.21, 0.78), name="chest_cross")
    assign_mat(chest_detail, MAT["gold"]); parts.append(chest_detail)

    # Áo khoác hoàng gia
    cape_l = add_cone(0.30, 0.65, (ox, oy+0.05, 0.56), segs=16, name="royal_cape")
    cape_l.scale.z = -1
    bpy.ops.object.transform_apply(scale=True)
    cape_l.location.z = 0.88
    assign_mat(cape_l, MAT["red"]); smooth_obj(cape_l); parts.append(cape_l)

    # Vương miện hoàng gia cao
    crown_base = add_cylinder(0.20, 0.08, (ox, oy, 1.26), segs=20, name="crown_base")
    assign_mat(crown_base, MAT["gold"]); smooth_obj(crown_base); parts.append(crown_base)
    for i in range(6):
        angle = i * (2 * math.pi / 6)
        cx = ox + 0.19 * math.cos(angle)
        cy = oy + 0.19 * math.sin(angle)
        spike = add_cone(0.045, 0.20, (cx, cy, 1.36), segs=8, name=f"spike_{i}")
        assign_mat(spike, MAT["gold"]); smooth_obj(spike); parts.append(spike)
    # Đá quý trên vương miện
    for i in range(3):
        angle = i * (2 * math.pi / 3) + math.pi/6
        cx = ox + 0.19 * math.cos(angle)
        cy = oy + 0.19 * math.sin(angle)
        gem = add_sphere(0.04, (cx, cy, 1.28), name=f"crown_gem_{i}")
        assign_mat(gem, [MAT["red"], MAT["blue"], MAT["purple"]][i])
        smooth_obj(gem); parts.append(gem)

    # Tay phải: chùy quyền lực
    arm_r = add_cylinder(0.07, 0.22, (ox+0.30, oy, 0.78), name="arm_r")
    assign_mat(arm_r, MAT["silver"]); smooth_obj(arm_r); parts.append(arm_r)
    mace_h = add_cylinder(0.03, 0.45, (ox+0.40, oy, 0.58), name="mace_handle")
    assign_mat(mace_h, MAT["gold"]); smooth_obj(mace_h); parts.append(mace_h)
    mace_head = add_sphere(0.10, (ox+0.40, oy, 0.88), name="mace_head")
    assign_mat(mace_head, MAT["gold"]); smooth_obj(mace_head); parts.append(mace_head)
    for i in range(6):
        ang = i * (math.pi / 3)
        sx = ox+0.40 + 0.10 * math.cos(ang)
        sy = oy + 0.10 * math.sin(ang)
        spike_m = add_cone(0.03, 0.08, (sx, sy, 0.88), segs=6, name=f"mace_spike_{i}")
        spike_m.rotation_euler = (math.radians(90) * math.sin(ang), math.radians(90) * math.cos(ang), 0)
        bpy.ops.object.transform_apply(rotation=True)
        assign_mat(spike_m, MAT["silver"]); smooth_obj(spike_m); parts.append(spike_m)

    # Tay trái: khiên hoàng gia
    arm_l = add_cylinder(0.07, 0.22, (ox-0.30, oy, 0.78), name="arm_l")
    assign_mat(arm_l, MAT["silver"]); smooth_obj(arm_l); parts.append(arm_l)
    shield = add_cube(0.24, 0.04, 0.30, (ox-0.46, oy, 0.76), name="king_shield")
    assign_mat(shield, MAT["blue"]); parts.append(shield)
    shield_rim = add_cube(0.26, 0.03, 0.32, (ox-0.46, oy+0.02, 0.76), name="shield_rim")
    assign_mat(shield_rim, MAT["gold"]); parts.append(shield_rim)
    shield_cross_v = add_cube(0.03, 0.03, 0.18, (ox-0.46, oy-0.02, 0.76), name="sc_v")
    assign_mat(shield_cross_v, MAT["gold"]); parts.append(shield_cross_v)
    shield_cross_h = add_cube(0.14, 0.03, 0.03, (ox-0.46, oy-0.02, 0.80), name="sc_h")
    assign_mat(shield_cross_h, MAT["gold"]); parts.append(shield_cross_h)

    return join_objects(parts, "King")

# ── Tạo tất cả quân cờ ─────────────────────────────────────
print("Đang tạo quân cờ 3D...")

SPACING = 1.4  # khoảng cách giữa các quân

pawn   = make_pawn(  -SPACING*2.5, 0)
rook   = make_rook(  -SPACING*1.5, 0)
knight = make_knight(-SPACING*0.5, 0)
bishop = make_bishop( SPACING*0.5, 0)
queen  = make_queen(  SPACING*1.5, 0)
king   = make_king(   SPACING*2.5, 0)

# ── Camera & ánh sáng ──────────────────────────────────────

# Camera nhìn tổng thể
bpy.ops.object.camera_add(location=(0, -8, 5))
cam = bpy.context.active_object
cam.rotation_euler = (math.radians(55), 0, 0)
bpy.context.scene.camera = cam

# Ánh sáng chính (Sun)
bpy.ops.object.light_add(type='SUN', location=(3, -4, 8))
sun = bpy.context.active_object
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(45), 0, math.radians(30))

# Ánh sáng fill
bpy.ops.object.light_add(type='AREA', location=(-4, 2, 5))
fill = bpy.context.active_object
fill.data.energy = 500
fill.data.size = 4

# Ánh sáng rim
bpy.ops.object.light_add(type='SPOT', location=(0, 6, 4))
rim = bpy.context.active_object
rim.data.energy = 800
rim.rotation_euler = (math.radians(-50), 0, 0)

# ── Render settings ────────────────────────────────────────
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 128
scene.render.resolution_x = 1920
scene.render.resolution_y = 1080
scene.render.film_transparent = True  # nền trong suốt

# World background
world = bpy.data.worlds['World']
world.use_nodes = True
bg = world.node_tree.nodes['Background']
bg.inputs[0].default_value = (0.05, 0.05, 0.08, 1)
bg.inputs[1].default_value = 0.3

print("=" * 50)
print("✓ Tạo xong 6 quân cờ 3D humanoid!")
print("")
print("Các quân cờ (trái → phải):")
print("  Tốt   | Xe   | Mã   | Tượng | Hậu  | Vua")
print("")
print("Cách render:")
print("  F12 = Render toàn cảnh")
print("  Hoặc: Render > Render Image")
print("")
print("Cách xuất file .blend:")
print("  File > Save As > chess_pieces.blend")
print("=" * 50)