"""
=============================================================
  EPIC FANTASY CHESS - White Angels vs Dark Demons
  Blender Python Script
  
  Quân TRẮNG: Hiệp sĩ thiên thần - trắng ngà + vàng
  Quân ĐEN:   Ác quỷ tối - đen + đỏ phát sáng
  
  Chạy trong Blender Scripting tab → Alt+P
=============================================================
"""

import bpy, math
from mathutils import Vector

# ── Xóa scene cũ ──────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for mat in bpy.data.materials: bpy.data.materials.remove(mat)

# ══════════════════════════════════════════════════════════
#  HELPER FUNCTIONS
# ══════════════════════════════════════════════════════════

def cyl(r, h, loc, rot=(0,0,0), segs=32, name="c"):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=h, location=loc, vertices=segs)
    o = bpy.context.active_object; o.name = name
    o.rotation_euler = rot; bpy.ops.object.transform_apply(rotation=True)
    return o

def sph(r, loc, segs=32, name="s"):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=segs, ring_count=segs//2)
    o = bpy.context.active_object; o.name = name; return o

def cube(sx, sy, sz, loc, rot=(0,0,0), name="b"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object; o.name = name
    o.scale = (sx, sy, sz); o.rotation_euler = rot
    bpy.ops.object.transform_apply(scale=True, rotation=True); return o

def cone(r, h, loc, rot=(0,0,0), segs=32, name="k"):
    bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=0, depth=h, location=loc, vertices=segs)
    o = bpy.context.active_object; o.name = name
    o.rotation_euler = rot; bpy.ops.object.transform_apply(rotation=True); return o

def tor(rmaj, rmin, loc, rot=(0,0,0), name="t"):
    bpy.ops.mesh.primitive_torus_add(major_radius=rmaj, minor_radius=rmin,
        major_segments=48, minor_segments=16, location=loc)
    o = bpy.context.active_object; o.name = name
    o.rotation_euler = rot; bpy.ops.object.transform_apply(rotation=True); return o

def smooth(o):
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.shade_smooth()

def asgn(o, m):
    if o.data.materials: o.data.materials[0] = m
    else: o.data.materials.append(m)

def join(obs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.join()
    bpy.context.active_object.name = name
    return bpy.context.active_object

def subdiv(o, levels=2):
    bpy.context.view_layer.objects.active = o
    mod = o.modifiers.new("Subd","SUBSURF")
    mod.levels = levels; mod.render_levels = levels

# ══════════════════════════════════════════════════════════
#  MATERIALS
# ══════════════════════════════════════════════════════════

def mat(name, base, metal=0, rough=0.5, emit=(0,0,0), emit_str=0, spec=0.5, trans=0, ior=1.45):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes = m.node_tree.nodes
    links = m.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value    = (*base, 1)
    bsdf.inputs['Metallic'].default_value      = metal
    bsdf.inputs['Roughness'].default_value     = rough
    bsdf.inputs['Specular IOR Level'].default_value = spec
    if emit_str > 0:
        bsdf.inputs['Emission Color'].default_value = (*emit, 1)
        bsdf.inputs['Emission Strength'].default_value = emit_str
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m

# WHITE team materials
MW = {
    'armor':  mat("W_armor",  (0.92,0.90,0.85), metal=0.15, rough=0.25),
    'gold':   mat("W_gold",   (0.95,0.78,0.15), metal=1.0,  rough=0.12),
    'cape':   mat("W_cape",   (0.97,0.95,0.90), metal=0.0,  rough=0.8),
    'skin':   mat("W_skin",   (0.98,0.88,0.76), metal=0.0,  rough=0.6),
    'eye':    mat("W_eye",    (0.20,0.55,1.00), metal=0.0,  rough=0.1, emit=(0.2,0.6,1.0), emit_str=2.0),
    'gem':    mat("W_gem",    (0.20,0.70,1.00), metal=0.0,  rough=0.05,emit=(0.2,0.7,1.0), emit_str=3.0),
    'blade':  mat("W_blade",  (0.88,0.92,0.98), metal=1.0,  rough=0.05),
    'dark':   mat("W_dark",   (0.20,0.18,0.15), metal=0.3,  rough=0.4),
}

# BLACK team materials  
MB = {
    'armor':  mat("B_armor",  (0.05,0.03,0.02), metal=0.3,  rough=0.3),
    'gold':   mat("B_gold",   (0.60,0.35,0.02), metal=1.0,  rough=0.2),
    'cape':   mat("B_cape",   (0.06,0.02,0.02), metal=0.0,  rough=0.9),
    'skin':   mat("B_skin",   (0.12,0.05,0.03), metal=0.0,  rough=0.7),
    'eye':    mat("B_eye",    (1.00,0.05,0.00), metal=0.0,  rough=0.0, emit=(1.0,0.05,0.0),emit_str=8.0),
    'gem':    mat("B_gem",    (1.00,0.08,0.00), metal=0.0,  rough=0.0, emit=(1.0,0.1,0.0), emit_str=10.0),
    'blade':  mat("B_blade",  (0.15,0.05,0.02), metal=0.8,  rough=0.15),
    'dark':   mat("B_dark",   (0.35,0.03,0.01), metal=0.5,  rough=0.2),
}

# ══════════════════════════════════════════════════════════
#  BASE HUMANOID BUILDER
# ══════════════════════════════════════════════════════════

def humanoid(ox, oy, M, cape_len=0.55, armor_detail=True):
    """Tạo thân người chi tiết cao hơn"""
    P = []

    # -- Đế trang trí --
    base1 = cyl(0.44, 0.06, (ox,oy,0.03), segs=48, name="base1")
    asgn(base1, M['gold']); smooth(base1); P.append(base1)
    base2 = cyl(0.38, 0.08, (ox,oy,0.09), segs=48, name="base2")
    asgn(base2, M['armor']); smooth(base2); P.append(base2)
    base_rim = tor(0.40, 0.025, (ox,oy,0.06), name="brim")
    asgn(base_rim, M['gold']); smooth(base_rim); P.append(base_rim)

    # -- Bàn chân có giày giáp --
    for dx in [-0.13, 0.13]:
        boot = cyl(0.09, 0.20, (ox+dx,oy,0.17), segs=16, name="boot")
        asgn(boot, M['armor']); smooth(boot); P.append(boot)
        boot_toe = sph(0.09, (ox+dx,oy-0.05,0.13), name="toe")
        asgn(boot_toe, M['armor']); smooth(boot_toe); P.append(boot_toe)
        boot_rim = tor(0.085, 0.018, (ox+dx,oy,0.27), name="boot_rim")
        asgn(boot_rim, M['gold']); smooth(boot_rim); P.append(boot_rim)

    # -- Ống chân giáp --
    for dx in [-0.13, 0.13]:
        shin = cyl(0.075, 0.30, (ox+dx,oy,0.40), segs=16, name="shin")
        asgn(shin, M['armor']); smooth(shin); P.append(shin)
        knee = sph(0.10, (ox+dx,oy,0.56), name="knee")
        asgn(knee, M['armor']); smooth(knee); P.append(knee)
        knee_detail = cyl(0.06, 0.04, (ox+dx,oy-0.08,0.56), segs=8, name="kd")
        asgn(knee_detail, M['gold']); smooth(knee_detail); P.append(knee_detail)

    # -- Đùi --
    for dx in [-0.13, 0.13]:
        thigh = cyl(0.10, 0.28, (ox+dx,oy,0.73), segs=16, name="thigh")
        asgn(thigh, M['armor']); smooth(thigh); P.append(thigh)

    # -- Hông + váy giáp --
    hip = cyl(0.26, 0.12, (ox,oy,0.88), segs=20, name="hip")
    asgn(hip, M['armor']); smooth(hip); P.append(hip)
    hip_rim = tor(0.24, 0.025, (ox,oy,0.95), name="hiprim")
    asgn(hip_rim, M['gold']); smooth(hip_rim); P.append(hip_rim)
    # Tấm giáp hông trước
    for i in range(3):
        ang = (i-1) * 0.35
        px = ox + math.sin(ang)*0.20
        py = oy - math.cos(ang)*0.20
        flap = cube(0.10, 0.03, 0.14, (px,py,0.83), name="flap")
        asgn(flap, M['armor']); P.append(flap)

    # -- Thân ngực giáp chi tiết --
    torso = cyl(0.22, 0.32, (ox,oy,1.10), segs=20, name="torso")
    asgn(torso, M['armor']); smooth(torso); P.append(torso)
    # Chi tiết ngực
    chest_l = cube(0.10, 0.04, 0.14, (ox-0.08,oy-0.19,1.14), name="chl")
    asgn(chest_l, M['armor']); P.append(chest_l)
    chest_r = cube(0.10, 0.04, 0.14, (ox+0.08,oy-0.19,1.14), name="chr")
    asgn(chest_r, M['armor']); P.append(chest_r)
    chest_mid = cube(0.04, 0.03, 0.20, (ox,oy-0.20,1.12), name="chm")
    asgn(chest_mid, M['gold']); P.append(chest_mid)
    torso_rim_b = tor(0.22, 0.022, (ox,oy,0.95), name="trb")
    asgn(torso_rim_b, M['gold']); smooth(torso_rim_b); P.append(torso_rim_b)
    torso_rim_t = tor(0.20, 0.022, (ox,oy,1.27), name="trt")
    asgn(torso_rim_t, M['gold']); smooth(torso_rim_t); P.append(torso_rim_t)

    # -- Áo choàng --
    cape = cyl(0.24, cape_len, (ox,oy+0.06,1.05), segs=14, name="cape")
    cape.scale.y = 0.45; bpy.ops.object.transform_apply(scale=True)
    asgn(cape, M['cape']); smooth(cape); P.append(cape)
    cape_bot = cone(0.30, 0.20, (ox,oy+0.06,0.75), segs=14, name="cape_bot")
    cape_bot.scale.y = 0.45; bpy.ops.object.transform_apply(scale=True)
    asgn(cape_bot, M['cape']); smooth(cape_bot); P.append(cape_bot)

    # -- Vai to giáp --
    for dx in [-0.36, 0.36]:
        shoulder = sph(0.14, (ox+dx,oy,1.20), name="sho")
        asgn(shoulder, M['armor']); smooth(shoulder); P.append(shoulder)
        sho_rim = tor(0.12, 0.025, (ox+dx,oy,1.15), name="sho_rim")
        asgn(sho_rim, M['gold']); smooth(sho_rim); P.append(sho_rim)
        sho_spike = cone(0.04, 0.10, (ox+dx,oy,1.30), segs=8, name="sspike")
        asgn(sho_spike, M['gold']); smooth(sho_spike); P.append(sho_spike)

    # -- Cổ + cổ áo giáp --
    neck = cyl(0.08, 0.10, (ox,oy,1.32), segs=16, name="neck")
    asgn(neck, M['armor']); smooth(neck); P.append(neck)
    collar = tor(0.10, 0.035, (ox,oy,1.28), name="collar")
    asgn(collar, M['gold']); smooth(collar); P.append(collar)

    # -- Đầu --
    head = sph(0.20, (ox,oy,1.52), segs=32, name="head")
    asgn(head, M['skin']); smooth(head); P.append(head)

    # -- Mắt phát sáng --
    for dx in [-0.08, 0.08]:
        eye = sph(0.035, (ox+dx,oy-0.17,1.54), name="eye")
        asgn(eye, M['eye']); smooth(eye); P.append(eye)

    return P

# ══════════════════════════════════════════════════════════
#  WHITE PIECES
# ══════════════════════════════════════════════════════════

SP = 1.5  # spacing

# ─── WHITE PAWN ───────────────────────────────────────────
def white_pawn(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.40)

    # Mũ giáp peon đơn giản
    helm = sph(0.21, (ox,oy,1.53), segs=24, name="helm")
    asgn(helm, MW['armor']); smooth(helm); P.append(helm)
    helm_rim = tor(0.20, 0.030, (ox,oy,1.41), name="hrim")
    asgn(helm_rim, MW['gold']); smooth(helm_rim); P.append(helm_rim)
    helm_crest = cube(0.05, 0.04, 0.12, (ox,oy,1.66), name="crest")
    asgn(helm_crest, MW['gold']); P.append(helm_crest)

    # Tay phải: thương ngắn
    arm_r = cyl(0.065, 0.26, (ox+0.30,oy,1.08), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    lance = cyl(0.025, 0.70, (ox+0.30,oy,0.65), name="lance")
    asgn(lance, MW['dark']); smooth(lance); P.append(lance)
    lance_tip = cone(0.05, 0.16, (ox+0.30,oy,0.29), segs=8, name="ltip")
    asgn(lance_tip, MW['blade']); smooth(lance_tip); P.append(lance_tip)
    lance_guard = tor(0.05, 0.018, (ox+0.30,oy,0.70), name="lguard")
    asgn(lance_guard, MW['gold']); smooth(lance_guard); P.append(lance_guard)

    # Tay trái: khiên thánh giá
    arm_l = cyl(0.065, 0.26, (ox-0.30,oy,1.08), name="arml")
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    shield = cyl(0.22, 0.045, (ox-0.50,oy,1.05), segs=24, name="shd")
    shield.rotation_euler = (0,math.radians(90),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(shield, MW['armor']); smooth(shield); P.append(shield)
    shield_rim2 = tor(0.21, 0.020, (ox-0.50,oy,1.05), rot=(0,math.radians(90),0), name="srim")
    asgn(shield_rim2, MW['gold']); smooth(shield_rim2); P.append(shield_rim2)
    cross_v = cube(0.03, 0.03, 0.18, (ox-0.52,oy,1.05), name="cv")
    asgn(cross_v, MW['gold']); P.append(cross_v)
    cross_h = cube(0.14, 0.03, 0.03, (ox-0.52,oy,1.09), name="ch")
    asgn(cross_h, MW['gold']); P.append(cross_h)

    return join(P, "W_Pawn")

# ─── WHITE ROOK ───────────────────────────────────────────
def white_rook(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.50)

    # Mũ tháp + răng cưa
    tower_helm = cyl(0.23, 0.28, (ox,oy,1.60), segs=8, name="thm")
    asgn(tower_helm, MW['armor']); smooth(tower_helm); P.append(tower_helm)
    for i in range(4):
        ang = i * math.pi/2
        mx = ox + 0.19*math.cos(ang); my = oy + 0.19*math.sin(ang)
        m = cube(0.07, 0.07, 0.12, (mx,my,1.82), name=f"m{i}")
        asgn(m, MW['armor']); P.append(m)
    tower_rim = tor(0.22, 0.025, (ox,oy,1.74), name="trim")
    asgn(tower_rim, MW['gold']); smooth(tower_rim); P.append(tower_rim)

    # Tay phải: búa chiến lớn
    arm_r = cyl(0.07, 0.26, (ox+0.32,oy,1.10), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    haft = cyl(0.03, 0.60, (ox+0.42,oy,0.82), name="haft")
    asgn(haft, MW['dark']); smooth(haft); P.append(haft)
    hammer = cube(0.20, 0.10, 0.16, (ox+0.42,oy,0.52), name="hmr")
    asgn(hammer, MW['armor']); P.append(hammer)
    hammer_rim2 = cube(0.22, 0.08, 0.02, (ox+0.42,oy,0.52), name="hrm")
    asgn(hammer_rim2, MW['gold']); P.append(hammer_rim2)
    hammer_cross = cube(0.03, 0.06, 0.12, (ox+0.42,oy-0.06,0.52), name="hcr")
    asgn(hammer_cross, MW['gold']); P.append(hammer_cross)

    # Tay trái: khiên tháp lớn
    arm_l = cyl(0.07, 0.26, (ox-0.32,oy,1.10), name="arml")
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    big_shield = cube(0.28, 0.04, 0.40, (ox-0.55,oy,1.00), name="bshd")
    asgn(big_shield, MW['armor']); P.append(big_shield)
    bs_rim = cube(0.30, 0.03, 0.42, (ox-0.55,oy+0.025,1.00), name="bsrim")
    asgn(bs_rim, MW['gold']); P.append(bs_rim)
    tower_icon = cube(0.08, 0.03, 0.14, (ox-0.57,oy-0.02,1.04), name="tic")
    asgn(tower_icon, MW['gold']); P.append(tower_icon)

    return join(P, "W_Rook")

# ─── WHITE KNIGHT ─────────────────────────────────────────
def white_knight(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.52)

    # Mũ hiệp sĩ có mào lớn
    helm = sph(0.22, (ox,oy,1.55), segs=24, name="helm")
    asgn(helm, MW['armor']); smooth(helm); P.append(helm)
    helm_rim2 = tor(0.21, 0.030, (ox,oy,1.43), name="hrim")
    asgn(helm_rim2, MW['gold']); smooth(helm_rim2); P.append(helm_rim2)
    visor = cube(0.18, 0.05, 0.10, (ox,oy-0.19,1.52), name="vis")
    asgn(visor, MW['gold']); P.append(visor)
    visor_slit = cube(0.14, 0.03, 0.025, (ox,oy-0.21,1.54), name="vslit")
    asgn(visor_slit, MW['dark']); P.append(visor_slit)
    # Mào lớn + lông đỏ
    crest_base = cube(0.06, 0.04, 0.06, (ox,oy,1.70), name="crb")
    asgn(crest_base, MW['gold']); P.append(crest_base)
    for i in range(5):
        fi = cyl(0.018, 0.22+i*0.02, (ox-0.04+i*0.02,oy,1.80+i*0.03), name=f"f{i}")
        fi.rotation_euler = (math.radians(8), 0, math.radians(-12+i*6))
        bpy.ops.object.transform_apply(rotation=True)
        asgn(fi, MW['gem']); smooth(fi); P.append(fi)

    # Tay phải: kiếm dài hai tay
    arm_r = cyl(0.07, 0.26, (ox+0.33,oy,1.10), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    sword_h = cyl(0.03, 0.70, (ox+0.33,oy,0.70), name="swh")
    asgn(sword_h, MW['dark']); smooth(sword_h); P.append(sword_h)
    blade = cube(0.028, 0.022, 0.60, (ox+0.33,oy,0.38), name="blade")
    asgn(blade, MW['blade']); P.append(blade)
    crossguard = cube(0.26, 0.04, 0.04, (ox+0.33,oy,1.04), name="cg")
    asgn(crossguard, MW['gold']); P.append(crossguard)
    pommel = sph(0.06, (ox+0.33,oy,1.38), name="pom")
    asgn(pommel, MW['gold']); smooth(pommel); P.append(pommel)
    blade_gem = sph(0.04, (ox+0.33,oy,0.72), name="bgem")
    asgn(blade_gem, MW['gem']); smooth(blade_gem); P.append(blade_gem)

    # Tay trái: giơ lên
    arm_l = cyl(0.07, 0.30, (ox-0.34,oy,1.14), name="arml")
    arm_l.rotation_euler = (0,math.radians(28),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    gaunt_l = sph(0.10, (ox-0.42,oy,1.24), name="gl")
    asgn(gaunt_l, MW['armor']); smooth(gaunt_l); P.append(gaunt_l)

    return join(P, "W_Knight")

# ─── WHITE BISHOP ─────────────────────────────────────────
def white_bishop(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.65)

    # Áo tu tế dài
    robe = cyl(0.28, 0.78, (ox,oy,0.88), segs=20, name="robe")
    asgn(robe, MW['cape']); smooth(robe); P.append(robe)
    robe_bot = cone(0.34, 0.24, (ox,oy,0.50), segs=20, name="rbot")
    asgn(robe_bot, MW['cape']); smooth(robe_bot); P.append(robe_bot)
    robe_gold_rim = tor(0.26, 0.025, (ox,oy,0.52), name="rgr")
    asgn(robe_gold_rim, MW['gold']); smooth(robe_gold_rim); P.append(robe_gold_rim)
    robe_mid_rim = tor(0.24, 0.018, (ox,oy,0.90), name="rmr")
    asgn(robe_mid_rim, MW['gold']); smooth(robe_mid_rim); P.append(robe_mid_rim)

    # Mũ giám mục nhọn cao
    mitre_b = cyl(0.20, 0.14, (ox,oy,1.68), segs=20, name="mib")
    asgn(mitre_b, MW['armor']); smooth(mitre_b); P.append(mitre_b)
    mitre_rim2 = tor(0.19, 0.028, (ox,oy,1.62), name="mrim")
    asgn(mitre_rim2, MW['gold']); smooth(mitre_rim2); P.append(mitre_rim2)
    mitre_top = cone(0.20, 0.52, (ox,oy,1.94), segs=20, name="mit")
    asgn(mitre_top, MW['armor']); smooth(mitre_top); P.append(mitre_top)
    mitre_gem = sph(0.05, (ox,oy-0.19,1.72), name="mgem")
    asgn(mitre_gem, MW['gem']); smooth(mitre_gem); P.append(mitre_gem)
    cross_v2 = cube(0.03, 0.02, 0.26, (ox,oy-0.19,1.82), name="cv2")
    asgn(cross_v2, MW['gold']); P.append(cross_v2)
    cross_h2 = cube(0.16, 0.02, 0.035, (ox,oy-0.19,1.90), name="ch2")
    asgn(cross_h2, MW['gold']); P.append(cross_h2)

    # Tay phải: gậy phép thánh
    arm_r = cyl(0.065, 0.26, (ox+0.30,oy,1.08), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    staff = cyl(0.028, 1.00, (ox+0.44,oy,0.72), name="stf")
    asgn(staff, MW['gold']); smooth(staff); P.append(staff)
    staff_orb_ring = tor(0.10, 0.028, (ox+0.44,oy,1.26), name="sor")
    asgn(staff_orb_ring, MW['gold']); smooth(staff_orb_ring); P.append(staff_orb_ring)
    staff_orb = sph(0.08, (ox+0.44,oy,1.26), name="sorb")
    asgn(staff_orb, MW['gem']); smooth(staff_orb); P.append(staff_orb)
    for i in range(4):
        ang = i*math.pi/2
        wing = cube(0.04, 0.02, 0.12, (ox+0.44+0.10*math.cos(ang),oy+0.10*math.sin(ang),1.28), name=f"sw{i}")
        asgn(wing, MW['gold']); P.append(wing)

    # Tay trái: sách thánh to
    arm_l = cyl(0.065, 0.26, (ox-0.30,oy,1.08), name="arml")
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    book = cube(0.18, 0.05, 0.24, (ox-0.46,oy,1.05), name="bk")
    asgn(book, MW['dark']); P.append(book)
    book_cover = cube(0.19, 0.04, 0.25, (ox-0.46,oy+0.025,1.05), name="bc")
    asgn(book_cover, MW['gold']); P.append(book_cover)
    book_cross = cube(0.04, 0.03, 0.18, (ox-0.46,oy+0.030,1.05), name="bkc")
    asgn(book_cross, MW['armor']); P.append(book_cross)

    return join(P, "W_Bishop")

# ─── WHITE QUEEN ──────────────────────────────────────────
def white_queen(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.80)

    # Váy nữ hoàng dài
    dress = cone(0.46, 0.82, (ox,oy,0.62), segs=24, name="dress")
    dress.scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    dress.location.z = 1.02
    asgn(dress, MW['cape']); smooth(dress); P.append(dress)
    dress_gold = tor(0.36, 0.025, (ox,oy,0.64), name="dgold")
    asgn(dress_gold, MW['gold']); smooth(dress_gold); P.append(dress_gold)
    dress_mid = tor(0.28, 0.020, (ox,oy,0.84), name="dmid")
    asgn(dress_mid, MW['gold']); smooth(dress_mid); P.append(dress_mid)

    # Áo corset vàng
    corset = cyl(0.22, 0.28, (ox,oy,1.10), segs=20, name="cors")
    asgn(corset, MW['armor']); smooth(corset); P.append(corset)
    corset_rim = tor(0.21, 0.025, (ox,oy,0.97), name="crim")
    asgn(corset_rim, MW['gold']); smooth(corset_rim); P.append(corset_rim)
    corset_detail = cube(0.04, 0.03, 0.20, (ox,oy-0.20,1.10), name="cd")
    asgn(corset_detail, MW['gold']); P.append(corset_detail)

    # Tóc dài (áo choàng phía sau đầu)
    hair = cyl(0.17, 0.55, (ox,oy+0.06,1.30), segs=12, name="hair")
    hair.scale.y = 0.45; bpy.ops.object.transform_apply(scale=True)
    asgn(hair, MW['dark']); smooth(hair); P.append(hair)

    # Vương miện hoa 5 đỉnh lớn
    crown_ring = tor(0.19, 0.038, (ox,oy,1.74), name="crng")
    asgn(crown_ring, MW['gold']); smooth(crown_ring); P.append(crown_ring)
    for i in range(5):
        ang = i*2*math.pi/5
        cx = ox+0.19*math.cos(ang); cy = oy+0.19*math.sin(ang)
        spk = cone(0.045, 0.18, (cx,cy,1.84), segs=8, name=f"cspk{i}")
        asgn(spk, MW['gold']); smooth(spk); P.append(spk)
        jwl = sph(0.04, (cx,cy,1.76), name=f"cjwl{i}")
        asgn(jwl, MW['gem']); smooth(jwl); P.append(jwl)

    # Tay trái: giơ duyên dáng
    arm_l = cyl(0.062, 0.28, (ox-0.33,oy,1.12), name="arml")
    arm_l.rotation_euler = (0,math.radians(-22),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    hand_l = sph(0.09, (ox-0.44,oy,1.24), name="handl")
    asgn(hand_l, MW['skin']); smooth(hand_l); P.append(hand_l)

    # Tay phải: quyền trượng nữ hoàng
    arm_r = cyl(0.062, 0.28, (ox+0.33,oy,1.12), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    scp = cyl(0.024, 0.75, (ox+0.46,oy,0.80), name="scp")
    asgn(scp, MW['gold']); smooth(scp); P.append(scp)
    orb = sph(0.10, (ox+0.46,oy,1.22), name="orb")
    asgn(orb, MW['gem']); smooth(orb); P.append(orb)
    orb_rim = tor(0.10, 0.022, (ox+0.46,oy,1.22), name="orim")
    asgn(orb_rim, MW['gold']); smooth(orb_rim); P.append(orb_rim)
    orb_top = cone(0.04, 0.12, (ox+0.46,oy,1.36), segs=8, name="otop")
    asgn(orb_top, MW['gold']); smooth(orb_top); P.append(orb_top)

    return join(P, "W_Queen")

# ─── WHITE KING ───────────────────────────────────────────
def white_king(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.85)

    # Áo choàng hoàng gia rộng
    royal = cone(0.50, 0.95, (ox,oy+0.05,0.58), segs=22, name="royal")
    royal.scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    royal.location.z = 1.06
    asgn(royal, MW['cape']); smooth(royal); P.append(royal)
    royal_rim = tor(0.38, 0.030, (ox,oy,0.62), name="rrim")
    asgn(royal_rim, MW['gold']); smooth(royal_rim); P.append(royal_rim)
    royal_mid = tor(0.30, 0.022, (ox,oy,0.84), name="rmid")
    asgn(royal_mid, MW['gold']); smooth(royal_mid); P.append(royal_mid)

    # Giáp ngực hoàng gia
    chest_plate = cyl(0.25, 0.30, (ox,oy,1.10), segs=20, name="cplate")
    asgn(chest_plate, MW['armor']); smooth(chest_plate); P.append(chest_plate)
    chest_cross_v = cube(0.05, 0.04, 0.22, (ox,oy-0.21,1.12), name="ccv")
    asgn(chest_cross_v, MW['gold']); P.append(chest_cross_v)
    chest_cross_h = cube(0.14, 0.04, 0.05, (ox,oy-0.21,1.16), name="cch")
    asgn(chest_cross_h, MW['gold']); P.append(chest_cross_h)
    chest_gem = sph(0.05, (ox,oy-0.22,1.12), name="cgem")
    asgn(chest_gem, MW['gem']); smooth(chest_gem); P.append(chest_gem)

    # Vương miện hoàng gia cao 6 đỉnh
    crown_base_c = cyl(0.22, 0.10, (ox,oy,1.72), segs=24, name="crb")
    asgn(crown_base_c, MW['gold']); smooth(crown_base_c); P.append(crown_base_c)
    crown_inner = cyl(0.18, 0.08, (ox,oy,1.72), segs=24, name="cri")
    asgn(crown_inner, MW['armor']); smooth(crown_inner); P.append(crown_inner)
    for i in range(6):
        ang = i*2*math.pi/6
        cx = ox+0.21*math.cos(ang); cy = oy+0.21*math.sin(ang)
        spk = cone(0.05, 0.26, (cx,cy,1.84), segs=8, name=f"kspk{i}")
        asgn(spk, MW['gold']); smooth(spk); P.append(spk)
    for i in range(3):
        ang = i*2*math.pi/3 + math.pi/6
        cx = ox+0.20*math.cos(ang); cy = oy+0.20*math.sin(ang)
        gem = sph(0.045, (cx,cy,1.74), name=f"kgem{i}")
        asgn(gem, MW['gem']); smooth(gem); P.append(gem)
    # Thánh giá đỉnh vương miện
    top_c_v = cube(0.04, 0.03, 0.22, (ox,oy,1.98), name="tcv")
    asgn(top_c_v, MW['gold']); P.append(top_c_v)
    top_c_h = cube(0.14, 0.03, 0.04, (ox,oy,2.08), name="tch")
    asgn(top_c_h, MW['gold']); P.append(top_c_h)

    # Tay phải: chùy hoàng gia
    arm_r = cyl(0.075, 0.28, (ox+0.35,oy,1.12), name="armr")
    asgn(arm_r, MW['armor']); smooth(arm_r); P.append(arm_r)
    mace_haft = cyl(0.032, 0.60, (ox+0.48,oy,0.82), name="mhaft")
    asgn(mace_haft, MW['gold']); smooth(mace_haft); P.append(mace_haft)
    mace_head = sph(0.13, (ox+0.48,oy,1.16), name="mhead")
    asgn(mace_head, MW['armor']); smooth(mace_head); P.append(mace_head)
    for i in range(8):
        ang = i*math.pi/4
        ms = cone(0.035, 0.12, (ox+0.48+0.13*math.cos(ang),oy+0.13*math.sin(ang),1.16), segs=6, name=f"ms{i}")
        ms.rotation_euler = (ang+math.pi/2, 0, 0)
        bpy.ops.object.transform_apply(rotation=True)
        asgn(ms, MW['gold']); smooth(ms); P.append(ms)
    mace_gem = sph(0.05, (ox+0.48,oy,1.16), name="mgem")
    asgn(mace_gem, MW['gem']); smooth(mace_gem); P.append(mace_gem)

    # Tay trái: khiên hoàng gia lớn
    arm_l = cyl(0.075, 0.28, (ox-0.35,oy,1.12), name="arml")
    asgn(arm_l, MW['armor']); smooth(arm_l); P.append(arm_l)
    king_shield = cube(0.30, 0.04, 0.40, (ox-0.56,oy,1.06), name="kshd")
    asgn(king_shield, MW['armor']); P.append(king_shield)
    ks_rim = cube(0.32, 0.03, 0.42, (ox-0.56,oy+0.02,1.06), name="ksrim")
    asgn(ks_rim, MW['gold']); P.append(ks_rim)
    ks_cross_v = cube(0.04, 0.03, 0.26, (ox-0.58,oy-0.01,1.08), name="kscv")
    asgn(ks_cross_v, MW['gold']); P.append(ks_cross_v)
    ks_cross_h = cube(0.20, 0.03, 0.04, (ox-0.58,oy-0.01,1.14), name("ksch"))
    asgn(ks_cross_h, MW['gold']); P.append(ks_cross_h)
    ks_gem = sph(0.05, (ox-0.59,oy-0.02,1.10), name="ksgem")
    asgn(ks_gem, MW['gem']); smooth(ks_gem); P.append(ks_gem)

    return join(P, "W_King")

# ══════════════════════════════════════════════════════════
#  BLACK PIECES  (same structure, dark materials, demon style)
# ══════════════════════════════════════════════════════════

def add_demon_horns(ox, oy, M, P):
    """Thêm sừng quỷ"""
    for dx, rot_z in [(-0.12, -0.3), (0.12, 0.3)]:
        horn = cone(0.045, 0.24, (ox+dx,oy,1.72), segs=8, name="horn")
        horn.rotation_euler = (math.radians(-15), 0, rot_z)
        bpy.ops.object.transform_apply(rotation=True)
        asgn(horn, M['dark']); smooth(horn); P.append(horn)
    return P

def black_pawn(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.40)
    add_demon_horns(ox, oy, MB, P)
    # Mặt nạ quỷ
    mask = sph(0.22, (ox,oy,1.52), segs=24, name="mask")
    asgn(mask, MB['armor']); smooth(mask); P.append(mask)
    mask_rim2 = tor(0.21, 0.028, (ox,oy,1.41), name="mrim")
    asgn(mask_rim2, MB['gold']); smooth(mask_rim2); P.append(mask_rim2)
    # Mắt đỏ phát sáng to hơn
    for dx in [-0.08, 0.08]:
        eye2 = sph(0.04, (ox+dx,oy-0.18,1.52), name="eye2")
        asgn(eye2, MB['eye']); smooth(eye2); P.append(eye2)
    # Tay phải: thương tối
    arm_r = cyl(0.065, 0.26, (ox+0.30,oy,1.08), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    lance = cyl(0.022, 0.72, (ox+0.30,oy,0.62), name="lance")
    asgn(lance, MB['dark']); smooth(lance); P.append(lance)
    lance_tip = cone(0.055, 0.18, (ox+0.30,oy,0.26), segs=4, name="ltip")
    asgn(lance_tip, MB['gem']); smooth(lance_tip); P.append(lance_tip)
    # Rune đỏ trên thương
    rune = tor(0.04, 0.015, (ox+0.30,oy,0.72), name="rune")
    asgn(rune, MB['gem']); smooth(rune); P.append(rune)
    # Tay trái: khiên xương
    arm_l = cyl(0.065, 0.26, (ox-0.30,oy,1.08), name="arml")
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    shield = cyl(0.22, 0.045, (ox-0.50,oy,1.05), segs=6, name="shd")
    shield.rotation_euler = (0,math.radians(90),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(shield, MB['armor']); smooth(shield); P.append(shield)
    shield_gem = sph(0.05, (ox-0.52,oy,1.05), name="sgem")
    asgn(shield_gem, MB['gem']); smooth(shield_gem); P.append(shield_gem)
    return join(P, "B_Pawn")

def black_rook(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.50)
    add_demon_horns(ox, oy, MB, P)
    # Tháp tối 4 cạnh răng cưa
    tower = cyl(0.25, 0.30, (ox,oy,1.62), segs=4, name="twr")
    asgn(tower, MB['armor']); smooth(tower); P.append(tower)
    for i in range(4):
        ang = i*math.pi/2
        mx = ox+0.20*math.cos(ang); my = oy+0.20*math.sin(ang)
        m = cube(0.07,0.07,0.14,(mx,my,1.86), name=f"bm{i}")
        asgn(m, MB['armor']); P.append(m)
    # Rune đỏ trên tháp
    for i in range(4):
        ang = i*math.pi/2
        r_gem = sph(0.04,(ox+0.24*math.cos(ang),oy+0.24*math.sin(ang),1.68), name=f"rg{i}")
        asgn(r_gem, MB['gem']); smooth(r_gem); P.append(r_gem)
    tower_rim = tor(0.24, 0.025, (ox,oy,1.76), name="trim")
    asgn(tower_rim, MB['gold']); smooth(tower_rim); P.append(tower_rim)
    # Tay phải: đại búa tối
    arm_r = cyl(0.07, 0.26, (ox+0.32,oy,1.10), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    haft = cyl(0.03, 0.65, (ox+0.44,oy,0.78), name="haft")
    asgn(haft, MB['dark']); smooth(haft); P.append(haft)
    hm = cube(0.24, 0.10, 0.18, (ox+0.44,oy,0.48), name="hm")
    asgn(hm, MB['armor']); P.append(hm)
    hm_gem = sph(0.06, (ox+0.44,oy-0.06,0.48), name="hmg")
    asgn(hm_gem, MB['gem']); smooth(hm_gem); P.append(hm_gem)
    # Tay trái: khiên xương lớn
    arm_l = cyl(0.07, 0.26, (ox-0.32,oy,1.10), name="arml")
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    bs = cube(0.28, 0.04, 0.40, (ox-0.56,oy,1.00), name="bs")
    asgn(bs, MB['armor']); P.append(bs)
    bs_rim = cube(0.30, 0.03, 0.42, (ox-0.56,oy+0.025,1.00), name="bsrim")
    asgn(bs_rim, MB['gold']); P.append(bs_rim)
    bs_skull = sph(0.07, (ox-0.58,oy-0.01,1.02), name="bsk")
    asgn(bs_skull, MB['gem']); smooth(bs_skull); P.append(bs_skull)
    return join(P, "B_Rook")

def black_knight(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.52)
    add_demon_horns(ox, oy, MB, P)
    helm = sph(0.23, (ox,oy,1.55), segs=24, name="helm")
    asgn(helm, MB['armor']); smooth(helm); P.append(helm)
    helm_rim2 = tor(0.22, 0.030, (ox,oy,1.43), name="hrim")
    asgn(helm_rim2, MB['gold']); smooth(helm_rim2); P.append(helm_rim2)
    visor = cube(0.18, 0.05, 0.08, (ox,oy-0.20,1.52), name="vis")
    asgn(visor, MB['dark']); P.append(visor)
    for dx in [-0.06,0.06]:
        eye_slit = sph(0.04, (ox+dx,oy-0.22,1.52), name="es")
        asgn(eye_slit, MB['eye']); smooth(eye_slit); P.append(eye_slit)
    # Tay phải: kiếm quỷ
    arm_r = cyl(0.07, 0.26, (ox+0.33,oy,1.10), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    sword_h = cyl(0.03, 0.72, (ox+0.33,oy,0.68), name="swh")
    asgn(sword_h, MB['dark']); smooth(sword_h); P.append(sword_h)
    blade = cube(0.028, 0.020, 0.62, (ox+0.33,oy,0.36), name="blade")
    asgn(blade, MB['blade']); P.append(blade)
    cg = cube(0.26, 0.04, 0.04, (ox+0.33,oy,1.02), name="cg")
    asgn(cg, MB['gold']); P.append(cg)
    pom = sph(0.06, (ox+0.33,oy,1.38), name="pom")
    asgn(pom, MB['gem']); smooth(pom); P.append(pom)
    for i in range(3):
        r2 = tor(0.028+i*0.01, 0.012, (ox+0.33,oy,0.55+i*0.15), name=f"br{i}")
        asgn(r2, MB['gem']); smooth(r2); P.append(r2)
    # Tay trái
    arm_l = cyl(0.07, 0.30, (ox-0.34,oy,1.14), name="arml")
    arm_l.rotation_euler = (0,math.radians(28),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    return join(P, "B_Knight")

def black_bishop(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.65)
    add_demon_horns(ox, oy, MB, P)
    robe = cyl(0.28, 0.78, (ox,oy,0.88), segs=20, name="robe")
    asgn(robe, MB['cape']); smooth(robe); P.append(robe)
    robe_bot = cone(0.35, 0.26, (ox,oy,0.50), segs=20, name="rbot")
    asgn(robe_bot, MB['cape']); smooth(robe_bot); P.append(robe_bot)
    for h in [0.52, 0.90]:
        rim = tor(0.25 if h<0.6 else 0.24, 0.022, (ox,oy,h), name=f"rrim{h}")
        asgn(rim, MB['gold']); smooth(rim); P.append(rim)
    # Mũ nhọn tối
    mb2 = cyl(0.20, 0.14, (ox,oy,1.68), segs=20, name="mib")
    asgn(mb2, MB['armor']); smooth(mb2); P.append(mb2)
    mt = cone(0.20, 0.58, (ox,oy,1.96), segs=20, name="mit")
    asgn(mt, MB['armor']); smooth(mt); P.append(mt)
    mgem = sph(0.06, (ox,oy-0.19,1.72), name="mgem")
    asgn(mgem, MB['gem']); smooth(mgem); P.append(mgem)
    mrim = tor(0.19, 0.028, (ox,oy,1.62), name="mrim")
    asgn(mrim, MB['gold']); smooth(mrim); P.append(mrim)
    # Tay phải: gậy tối
    arm_r = cyl(0.065, 0.26, (ox+0.30,oy,1.08), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    stf = cyl(0.028, 1.02, (ox+0.44,oy,0.72), name="stf")
    asgn(stf, MB['dark']); smooth(stf); P.append(stf)
    skull = sph(0.09, (ox+0.44,oy,1.28), name="skull")
    asgn(skull, MB['armor']); smooth(skull); P.append(skull)
    skull_eye_l = sph(0.03, (ox+0.37,oy-0.07,1.30), name="sel")
    asgn(skull_eye_l, MB['gem']); smooth(skull_eye_l); P.append(skull_eye_l)
    skull_eye_r = sph(0.03, (ox+0.51,oy-0.07,1.30), name="ser")
    asgn(skull_eye_r, MB['gem']); smooth(skull_eye_r); P.append(skull_eye_r)
    for i in range(3):
        rune = tor(0.032, 0.012, (ox+0.44,oy,0.72+i*0.22), name=f"rune{i}")
        asgn(rune, MB['gem']); smooth(rune); P.append(rune)
    # Tay trái
    arm_l = cyl(0.065, 0.26, (ox-0.30,oy,1.08), name="arml")
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    tome = cube(0.18, 0.05, 0.24, (ox-0.46,oy,1.05), name="tome")
    asgn(tome, MB['dark']); P.append(tome)
    tome_cover = cube(0.19, 0.04, 0.25, (ox-0.46,oy+0.025,1.05), name="tc")
    asgn(tome_cover, MB['armor']); P.append(tome_cover)
    tome_gem = sph(0.04, (ox-0.48,oy+0.03,1.05), name="tgem")
    asgn(tome_gem, MB['gem']); smooth(tome_gem); P.append(tome_gem)
    return join(P, "B_Bishop")

def black_queen(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.80)
    add_demon_horns(ox, oy, MB, P)
    dress = cone(0.48, 0.86, (ox,oy,0.60), segs=24, name="dress")
    dress.scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    dress.location.z = 1.04
    asgn(dress, MB['cape']); smooth(dress); P.append(dress)
    for h, r in [(0.64,0.38),(0.86,0.28)]:
        dr = tor(r, 0.025, (ox,oy,h), name=f"dr{h}")
        asgn(dr, MB['gold']); smooth(dr); P.append(dr)
    cors = cyl(0.22, 0.28, (ox,oy,1.10), segs=20, name="cors")
    asgn(cors, MB['armor']); smooth(cors); P.append(cors)
    cors_gem = sph(0.05, (ox,oy-0.21,1.12), name="cgem")
    asgn(cors_gem, MB['gem']); smooth(cors_gem); P.append(cors_gem)
    # Tóc đỏ tối
    hair = cyl(0.17, 0.55, (ox,oy+0.06,1.30), segs=12, name="hair")
    hair.scale.y = 0.45; bpy.ops.object.transform_apply(scale=True)
    asgn(hair, MB['dark']); smooth(hair); P.append(hair)
    # Vương miện quỷ
    crng = tor(0.19, 0.038, (ox,oy,1.76), name="crng")
    asgn(crng, MB['gold']); smooth(crng); P.append(crng)
    for i in range(5):
        ang = i*2*math.pi/5
        cx=ox+0.19*math.cos(ang); cy=oy+0.19*math.sin(ang)
        spk = cone(0.04, 0.22, (cx,cy,1.88), segs=4, name=f"qspk{i}")
        asgn(spk, MB['dark']); smooth(spk); P.append(spk)
        jwl = sph(0.04, (cx,cy,1.78), name=f"qjwl{i}")
        asgn(jwl, MB['gem']); smooth(jwl); P.append(jwl)
    # Tay trái
    arm_l = cyl(0.062, 0.28, (ox-0.33,oy,1.12), name="arml")
    arm_l.rotation_euler = (0,math.radians(-22),0)
    bpy.ops.object.transform_apply(rotation=True)
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    # Tay phải: quyền trượng tối phát sáng
    arm_r = cyl(0.062, 0.28, (ox+0.33,oy,1.12), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    scp = cyl(0.024, 0.78, (ox+0.46,oy,0.78), name="scp")
    asgn(scp, MB['dark']); smooth(scp); P.append(scp)
    orb = sph(0.11, (ox+0.46,oy,1.22), name="orb")
    asgn(orb, MB['gem']); smooth(orb); P.append(orb)
    orim = tor(0.11, 0.022, (ox+0.46,oy,1.22), name="orim")
    asgn(orim, MB['gold']); smooth(orim); P.append(orim)
    return join(P, "B_Queen")

def black_king(ox, oy):
    P = humanoid(ox, oy, MB, cape_len=0.85)
    # Sừng vua quỷ to hơn
    for dx, rot_z in [(-0.14,-0.4),(0.14,0.4)]:
        horn = cone(0.055, 0.32, (ox+dx,oy,1.74), segs=8, name="bhorn")
        horn.rotation_euler = (math.radians(-20),0,rot_z)
        bpy.ops.object.transform_apply(rotation=True)
        asgn(horn, MB['dark']); smooth(horn); P.append(horn)
    royal = cone(0.52, 1.00, (ox,oy+0.05,0.56), segs=22, name="royal")
    royal.scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    royal.location.z = 1.08
    asgn(royal, MB['cape']); smooth(royal); P.append(royal)
    for h, r in [(0.62,0.40),(0.86,0.30)]:
        rr = tor(r, 0.028, (ox,oy,h), name=f"rr{h}")
        asgn(rr, MB['gold']); smooth(rr); P.append(rr)
    cplate = cyl(0.25, 0.30, (ox,oy,1.10), segs=20, name="cplate")
    asgn(cplate, MB['armor']); smooth(cplate); P.append(cplate)
    cplate_gem = sph(0.06, (ox,oy-0.22,1.12), name="cpgem")
    asgn(cplate_gem, MB['gem']); smooth(cplate_gem); P.append(cplate_gem)
    # Vương miện quỷ vương
    crb = cyl(0.22, 0.10, (ox,oy,1.74), segs=6, name="crb")
    asgn(crb, MB['dark']); smooth(crb); P.append(crb)
    crb_gold = tor(0.22, 0.025, (ox,oy,1.70), name="crbg")
    asgn(crb_gold, MB['gold']); smooth(crb_gold); P.append(crb_gold)
    for i in range(6):
        ang = i*2*math.pi/6
        cx=ox+0.21*math.cos(ang); cy=oy+0.21*math.sin(ang)
        spk = cone(0.055, 0.30, (cx,cy,1.86), segs=6, name=f"bkspk{i}")
        asgn(spk, MB['dark']); smooth(spk); P.append(spk)
    for i in range(6):
        ang = i*2*math.pi/6+math.pi/6
        cx=ox+0.20*math.cos(ang); cy=oy+0.20*math.sin(ang)
        gem = sph(0.04, (cx,cy,1.76), name=f"bkgem{i}")
        asgn(gem, MB['gem']); smooth(gem); P.append(gem)
    # Tay phải: chùy quỷ
    arm_r = cyl(0.075, 0.28, (ox+0.35,oy,1.12), name="armr")
    asgn(arm_r, MB['armor']); smooth(arm_r); P.append(arm_r)
    mhaft = cyl(0.032, 0.62, (ox+0.48,oy,0.80), name="mhaft")
    asgn(mhaft, MB['dark']); smooth(mhaft); P.append(mhaft)
    mhead = sph(0.14, (ox+0.48,oy,1.16), name="mhead")
    asgn(mhead, MB['armor']); smooth(mhead); P.append(mhead)
    for i in range(8):
        ang = i*math.pi/4
        ms = cone(0.04,0.14,(ox+0.48+0.14*math.cos(ang),oy+0.14*math.sin(ang),1.16),segs=4,name=f"bms{i}")
        ms.rotation_euler=(ang+math.pi/2,0,0); bpy.ops.object.transform_apply(rotation=True)
        asgn(ms, MB['gem']); smooth(ms); P.append(ms)
    # Tay trái: khiên quỷ
    arm_l = cyl(0.075, 0.28, (ox-0.35,oy,1.12), name="arml")
    asgn(arm_l, MB['armor']); smooth(arm_l); P.append(arm_l)
    kshd = cube(0.30, 0.04, 0.42, (ox-0.57,oy,1.06), name="kshd")
    asgn(kshd, MB['armor']); P.append(kshd)
    ksrim = cube(0.32, 0.03, 0.44, (ox-0.57,oy+0.02,1.06), name="ksrim")
    asgn(ksrim, MB['gold']); P.append(ksrim)
    ksgem = sph(0.06, (ox-0.59,oy-0.01,1.08), name="ksgem")
    asgn(ksgem, MB['gem']); smooth(ksgem); P.append(ksgem)
    return join(P, "B_King")

# ══════════════════════════════════════════════════════════
#  TẠO TẤT CẢ QUÂN CỜ
# ══════════════════════════════════════════════════════════
print("Đang tạo 12 quân cờ Epic Fantasy...")

ROW_W =  0.0   # hàng trắng Y=0
ROW_B = -3.5   # hàng đen phía sau

pieces = [
    (white_pawn,   -SP*2.5, ROW_W, "W_Pawn"),
    (white_rook,   -SP*1.5, ROW_W, "W_Rook"),
    (white_knight, -SP*0.5, ROW_W, "W_Knight"),
    (white_bishop,  SP*0.5, ROW_W, "W_Bishop"),
    (white_queen,   SP*1.5, ROW_W, "W_Queen"),
    (white_king,    SP*2.5, ROW_W, "W_King"),
    (black_pawn,   -SP*2.5, ROW_B, "B_Pawn"),
    (black_rook,   -SP*1.5, ROW_B, "B_Rook"),
    (black_knight, -SP*0.5, ROW_B, "B_Knight"),
    (black_bishop,  SP*0.5, ROW_B, "B_Bishop"),
    (black_queen,   SP*1.5, ROW_B, "B_Queen"),
    (black_king,    SP*2.5, ROW_B, "B_King"),
]

for fn, ox, oy, name in pieces:
    print(f"  Tạo {name}...")
    fn(ox, oy)

# ── Lighting hoành tráng ──────────────────────────────────

# Ánh sáng mặt trời chính (ấm)
bpy.ops.object.light_add(type='SUN', location=(5,-8,12))
sun = bpy.context.active_object
sun.data.energy = 4.0
sun.data.color = (1.0, 0.92, 0.80)
sun.rotation_euler = (math.radians(40), 0, math.radians(25))

# Fill nhẹ bên trái (lạnh)
bpy.ops.object.light_add(type='AREA', location=(-6, 3, 8))
fill = bpy.context.active_object
fill.data.energy = 400
fill.data.size = 6
fill.data.color = (0.75, 0.85, 1.0)

# Rim light phía sau (phân biệt 2 team)
bpy.ops.object.light_add(type='SPOT', location=(0, 8, 6))
rim = bpy.context.active_object
rim.data.energy = 1200
rim.data.color = (1.0, 0.3, 0.1)   # Ánh đỏ cho quân đen
rim.rotation_euler = (math.radians(-45), 0, 0)

# Ánh sáng nền ấm (world)
bpy.ops.object.light_add(type='AREA', location=(0, -6, 3))
front = bpy.context.active_object
front.data.energy = 300
front.data.color = (1.0, 0.95, 0.85)
front.data.size = 8

# Camera
bpy.ops.object.camera_add(location=(0, -11, 6))
cam = bpy.context.active_object
cam.rotation_euler = (math.radians(58), 0, 0)
bpy.context.scene.camera = cam

# World background khói tối ấm
world = bpy.data.worlds['World']
world.use_nodes = True
bg = world.node_tree.nodes['Background']
bg.inputs[0].default_value = (0.04, 0.03, 0.02, 1)
bg.inputs[1].default_value = 0.2

# Render settings
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 256
scene.render.resolution_x = 1920
scene.render.resolution_y = 1080
scene.render.film_transparent = False

print("=" * 55)
print("✓ XONG! 12 quân cờ Epic Fantasy đã tạo xong!")
print("")
print("  Hàng TRẮNG (thiên thần vàng): W_Pawn → W_King")
print("  Hàng ĐEN   (ác quỷ đỏ):       B_Pawn → B_King")
print("")
print("Nhấn F12 để render preview toàn cảnh")
print("Sau đó chạy chess_render_all.py để lưu từng con")
print("=" * 55)