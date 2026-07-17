"""
=============================================================
  EPIC FANTASY CHESS - PHIÊN BẢN VIẾT LẠI HOÀN TOÀN
  Tỷ lệ chuẩn, vũ khí đúng vị trí, trông đẹp hơn nhiều
  Chạy trong Blender Scripting → Alt+P
=============================================================
"""
import bpy, math

# ── Xóa tất cả ───────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for m in list(bpy.data.materials): bpy.data.materials.remove(m)

# ── Tiện ích cơ bản ──────────────────────────────────────
def cyl(r, h, x, y, z, seg=32, name="o"):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=h, vertices=seg,
        location=(x, y, z))
    o = bpy.context.active_object; o.name = name; return o

def sph(r, x, y, z, seg=24, name="o"):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=seg,
        ring_count=seg//2, location=(x, y, z))
    o = bpy.context.active_object; o.name = name; return o

def box(sx, sy, sz, x, y, z, name="o"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z))
    o = bpy.context.active_object; o.name = name
    o.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True); return o

def cone(r, h, x, y, z, seg=16, name="o"):
    bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=0, depth=h,
        vertices=seg, location=(x, y, z))
    o = bpy.context.active_object; o.name = name; return o

def tor(R, r, x, y, z, name="o"):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r,
        major_segments=48, minor_segments=12, location=(x, y, z))
    o = bpy.context.active_object; o.name = name; return o

def sm(o):
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.shade_smooth()
    return o

def asgn(o, m):
    o.data.materials.clear()
    o.data.materials.append(m); return o

def join_all(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    bpy.context.active_object.name = name
    return bpy.context.active_object

# ── Materials ─────────────────────────────────────────────
def mk(name, col, met=0, rgh=0.5, em=None, es=0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value   = (*col, 1)
    b.inputs["Metallic"].default_value     = met
    b.inputs["Roughness"].default_value    = rgh
    if em:
        b.inputs["Emission Color"].default_value    = (*em, 1)
        b.inputs["Emission Strength"].default_value = es
    return m

# Palette Trắng — Paladin Thánh
MW = {
    "ivory": mk("ivory",  (0.95,0.93,0.88), met=0.05, rgh=0.20),
    "gold":  mk("gold",   (0.95,0.78,0.12), met=0.95, rgh=0.10),
    "skin":  mk("skin",   (0.98,0.86,0.74), met=0.00, rgh=0.60),
    "cape":  mk("cape",   (0.92,0.90,0.85), met=0.00, rgh=0.85),
    "gem":   mk("gem",    (0.50,0.85,1.00), met=0.20, rgh=0.05,
                em=(0.4,0.8,1.0), es=2.0),
    "eye":   mk("eye_w",  (0.20,0.60,1.00), met=0.00, rgh=0.05,
                em=(0.2,0.5,1.0), es=3.0),
    "blade": mk("blade",  (0.85,0.88,0.92), met=0.90, rgh=0.10),
}

# Palette Đen — Demon Quỷ
MB = {
    "dark":  mk("dark",   (0.04,0.03,0.02), met=0.30, rgh=0.35),
    "iron":  mk("iron",   (0.10,0.06,0.04), met=0.50, rgh=0.25),
    "gold":  mk("b_gold", (0.55,0.32,0.02), met=0.90, rgh=0.20),
    "skin":  mk("b_skin", (0.10,0.05,0.03), met=0.00, rgh=0.70),
    "red":   mk("red",    (0.80,0.03,0.01), met=0.10, rgh=0.10,
                em=(1.0,0.05,0.0), es=5.0),
    "eye":   mk("eye_b",  (1.00,0.08,0.00), met=0.00, rgh=0.00,
                em=(1.0,0.1,0.0),  es=8.0),
    "spike": mk("spike",  (0.15,0.06,0.03), met=0.60, rgh=0.20),
}

SP  = 1.6   # khoảng cách ngang
YB  = -3.2  # hàng quân đen

# ══════════════════════════════════════════════════════════
#  THÂN NGƯỜI — dùng chung cho cả 2 bên
# ══════════════════════════════════════════════════════════
def humanoid(ox, oy, M, cape_len=0.0):
    """
    Trả về list parts + chiều cao đỉnh đầu (head_top_z)
    ox, oy = vị trí gốc
    M      = dict materials
    cape_len > 0 → thêm áo choàng
    """
    P = []
    # Đế
    sm(asgn(cyl(0.44,0.09, ox,oy,0.045,48,"base"), M["ivory" if "ivory" in M else "dark"]))
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.38,0.030, ox,oy,0.09,"brim"), M["gold"]))
    P.append(bpy.context.active_object)

    # Chân
    for dx in [-0.13, 0.13]:
        sm(asgn(cyl(0.085,0.32, ox+dx,oy,0.25,16,"leg"), M["ivory" if "ivory" in M else "dark"]))
        P.append(bpy.context.active_object)
        sm(asgn(tor(0.085,0.020, ox+dx,oy,0.40,"kn"), M["gold"]))
        P.append(bpy.context.active_object)

    # Hông
    sm(asgn(cyl(0.22,0.16, ox,oy,0.50,24,"hip"), M["ivory" if "ivory" in M else "dark"]))
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.21,0.028, ox,oy,0.57,"belt"), M["gold"]))
    P.append(bpy.context.active_object)

    # Ngực
    sm(asgn(cyl(0.20,0.30, ox,oy,0.73,24,"torso"), M["ivory" if "ivory" in M else "dark"]))
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.19,0.022, ox,oy,0.87,"collar"), M["gold"]))
    P.append(bpy.context.active_object)

    # Vai
    for dx in [-0.29, 0.29]:
        sm(asgn(sph(0.12, ox+dx,oy,0.84,20,"sh"), M["ivory" if "ivory" in M else "dark"]))
        P.append(bpy.context.active_object)
        sm(asgn(tor(0.11,0.020, ox+dx,oy,0.79,"shr"), M["gold"]))
        P.append(bpy.context.active_object)

    # Cổ
    sm(asgn(cyl(0.072,0.11, ox,oy,0.95,16,"neck"), M["skin"]))
    P.append(bpy.context.active_object)

    # Đầu
    sm(asgn(sph(0.18, ox,oy,1.12,32,"head"), M["skin"]))
    P.append(bpy.context.active_object)

    # Mắt
    for dx in [-0.07, 0.07]:
        sm(asgn(sph(0.028, ox+dx,oy-0.16,1.15,12,"eye"), M["eye"]))
        P.append(bpy.context.active_object)

    # Áo choàng
    if cape_len > 0:
        sm(asgn(cone(0.24,cape_len, ox,oy+0.06,0.88-cape_len/2+0.44,16,"cape"),
                M["cape" if "cape" in M else "dark"]))
        P[-1].scale.z = -1
        bpy.ops.object.transform_apply(scale=True)
        P[-1].location.z = 0.88
        P.append(bpy.context.active_object)
        sm(asgn(tor(0.22,0.018, ox,oy+0.05,0.18,"capeh"), M["gold"]))
        P.append(bpy.context.active_object)

    return P

def arms(ox, oy, M, z_arm=0.76):
    """Thêm 2 cánh tay, trả về list parts"""
    P = []
    for dx in [-0.29, 0.29]:
        sm(asgn(cyl(0.065,0.26, ox+dx,oy,z_arm,16,"arm"), M["ivory" if "ivory" in M else "dark"]))
        P.append(bpy.context.active_object)
        sm(asgn(tor(0.065,0.018, ox+dx,oy,z_arm-0.12,"elbow"), M["gold"]))
        P.append(bpy.context.active_object)
    return P

# ══════════════════════════════════════════════════════════
#  QUÂN TRẮNG
# ══════════════════════════════════════════════════════════

def w_pawn(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0)
    P += arms(ox, oy, MW)
    # Mũ tròn đơn giản
    sm(asgn(sph(0.20, ox,oy,1.14,32,"helm"), MW["ivory"]))
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.185,0.025, ox,oy,1.02,"helmr"), MW["gold"]))
    P.append(bpy.context.active_object)
    # Kiếm ngắn tay phải — DỌC, gắn sát tay
    sm(asgn(cyl(0.022,0.52, ox+0.29,oy,0.50,8,"sword_h"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.020,0.020,0.52, ox+0.29,oy,0.50,"sword_b"), MW["blade"])
    P.append(bpy.context.active_object)
    asgn(box(0.16,0.025,0.030, ox+0.29,oy,0.76,"guard"), MW["gold"])
    P.append(bpy.context.active_object)
    sm(asgn(cone(0.022,0.08, ox+0.29,oy,0.78,8,"tip"), MW["blade"]))
    P.append(bpy.context.active_object)
    # Khiên tay trái
    asgn(box(0.22,0.040,0.28, ox-0.42,oy,0.76,"shield"), MW["ivory"])
    P.append(bpy.context.active_object)
    asgn(box(0.24,0.030,0.30, ox-0.42,oy+0.025,0.76,"shr"), MW["gold"])
    P.append(bpy.context.active_object)
    sm(asgn(sph(0.04, ox-0.42,oy-0.025,0.80,12,"shgem"), MW["gem"]))
    P.append(bpy.context.active_object)
    return join_all(P, "W_Pawn")

def w_rook(ox, oy):
    P = humanoid(ox, oy, MW)
    P += arms(ox, oy, MW)
    # Mũ tháp vuông
    sm(asgn(cyl(0.21,0.28, ox,oy,1.42,4,"tower"), MW["ivory"]))
    P.append(bpy.context.active_object)
    for dx, dy in [(-0.10,-0.10),(-0.10,0.10),(0.10,-0.10),(0.10,0.10)]:
        asgn(box(0.09,0.09,0.14, ox+dx,oy+dy,1.68,"mt"), MW["ivory"])
        P.append(bpy.context.active_object)
    sm(asgn(tor(0.18,0.022, ox,oy,1.28,"tr"), MW["gold"]))
    P.append(bpy.context.active_object)
    # Búa tay phải
    sm(asgn(cyl(0.025,0.50, ox+0.29,oy,0.52,8,"haft"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.18,0.09,0.12, ox+0.29,oy,0.30,"hhead"), MW["gold"])
    P.append(bpy.context.active_object)
    # Cuộn giấy tay trái
    sm(asgn(cyl(0.055,0.26, ox-0.42,oy,0.76,16,"scroll"), MW["cape"]))
    P.append(bpy.context.active_object)
    return join_all(P, "W_Rook")

def w_knight(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.60)
    P += arms(ox, oy, MW)
    # Mũ hiệp sĩ + lông
    sm(asgn(sph(0.20, ox,oy,1.14,28,"helm"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.14,0.035,0.07, ox,oy-0.17,1.10,"visor"), MW["gold"])
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.185,0.022, ox,oy,1.02,"hr"), MW["gold"]))
    P.append(bpy.context.active_object)
    # Lông chim — gắn TRÊN mũ, thẳng đứng
    for i in range(4):
        sm(asgn(cyl(0.020,0.24+i*0.04, ox-0.03+i*0.02,oy,1.40+i*0.04,6,f"feat{i}"),
                MW["gold"]))
        P.append(bpy.context.active_object)
    # Kiếm dài tay phải
    sm(asgn(cyl(0.022,0.60, ox+0.29,oy,0.48,8,"sh"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.018,0.018,0.60, ox+0.29,oy,0.48,"sb"), MW["blade"])
    P.append(bpy.context.active_object)
    asgn(box(0.18,0.022,0.028, ox+0.29,oy,0.76,"g"), MW["gold"])
    P.append(bpy.context.active_object)
    # Khiên tay trái
    asgn(box(0.20,0.038,0.26, ox-0.42,oy,0.76,"sh2"), MW["ivory"])
    P.append(bpy.context.active_object)
    asgn(box(0.22,0.028,0.28, ox-0.42,oy+0.022,0.76,"shr2"), MW["gold"])
    P.append(bpy.context.active_object)
    return join_all(P, "W_Knight")

def w_bishop(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.70)
    P += arms(ox, oy, MW)
    # Mũ nhọn giám mục
    sm(asgn(cyl(0.19,0.10, ox,oy,1.34,20,"mbase"), MW["gold"]))
    P.append(bpy.context.active_object)
    sm(asgn(cone(0.19,0.52, ox,oy,1.65,20,"mtop"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.028,0.020,0.22, ox,oy-0.17,1.40,"cv"), MW["gold"])
    P.append(bpy.context.active_object)
    asgn(box(0.14,0.020,0.028, ox,oy-0.17,1.44,"ch"), MW["gold"])
    P.append(bpy.context.active_object)
    # Gậy phép tay phải — DỌC sát tay
    sm(asgn(cyl(0.025,0.80, ox+0.29,oy,0.44,8,"staff"), MW["gold"]))
    P.append(bpy.context.active_object)
    sm(asgn(sph(0.07, ox+0.29,oy,0.88,16,"orb"), MW["gem"]))
    P.append(bpy.context.active_object)
    sm(asgn(tor(0.07,0.015, ox+0.29,oy,0.88,"orbr"), MW["gold"]))
    P.append(bpy.context.active_object)
    # Sách tay trái
    asgn(box(0.16,0.045,0.20, ox-0.40,oy,0.76,"book"), MW["ivory"])
    P.append(bpy.context.active_object)
    asgn(box(0.14,0.018,0.18, ox-0.40,oy+0.032,0.76,"pages"), MW["cape"])
    P.append(bpy.context.active_object)
    return join_all(P, "W_Bishop")

def w_queen(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.80)
    P += arms(ox, oy, MW)
    # Váy bồng
    sm(asgn(cone(0.42,0.68, ox,oy,0.54,24,"skirt"), MW["cape"]))
    P[-1].scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    P[-1].location.z = 0.88
    sm(asgn(tor(0.38,0.020, ox,oy,0.20,"shem"), MW["gold"]))
    P.append(bpy.context.active_object)
    # Vương miện 7 đỉnh
    sm(asgn(tor(0.17,0.032, ox,oy,1.54,48,"cr"), MW["gold"]))
    P.append(bpy.context.active_object)
    for i in range(7):
        a = i*2*math.pi/7
        cx, cy = ox+0.17*math.cos(a), oy+0.17*math.sin(a)
        sm(asgn(cone(0.038,0.16, cx,cy,1.61,8,f"qpk{i}"), MW["gold"]))
        P.append(bpy.context.active_object)
        sm(asgn(sph(0.030, cx,cy,1.58,8,f"qjw{i}"), MW["gem"]))
        P.append(bpy.context.active_object)
    sm(asgn(sph(0.055, ox,oy,1.83,12,"qcg"), MW["gem"]))
    P.append(bpy.context.active_object)
    # Gậy quyền tay phải
    sm(asgn(cyl(0.025,0.76, ox+0.29,oy,0.46,8,"sc"), MW["gold"]))
    P.append(bpy.context.active_object)
    sm(asgn(sph(0.075, ox+0.29,oy,0.88,16,"sorb"), MW["gem"]))
    P.append(bpy.context.active_object)
    # Tay trái giơ
    sm(asgn(cyl(0.060,0.26, ox-0.29,oy,0.82,12,"arm_l2"), MW["ivory"]))
    P.append(bpy.context.active_object)
    return join_all(P, "W_Queen")

def w_king(ox, oy):
    P = humanoid(ox, oy, MW, cape_len=0.80)
    P += arms(ox, oy, MW)
    # Giáp ngực bổ sung
    sm(asgn(cyl(0.22,0.30, ox,oy,0.73,20,"chest"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.035,0.025,0.16, ox,oy-0.20,0.78,"chcv"), MW["gold"])
    P.append(bpy.context.active_object)
    asgn(box(0.14,0.025,0.035, ox,oy-0.20,0.84,"chch"), MW["gold"])
    P.append(bpy.context.active_object)
    # Vương miện 8 đỉnh hoành tráng
    sm(asgn(tor(0.19,0.035, ox,oy,1.57,48,"cr"), MW["gold"]))
    P.append(bpy.context.active_object)
    for i in range(8):
        a = i*2*math.pi/8
        cx, cy = ox+0.19*math.cos(a), oy+0.19*math.sin(a)
        sm(asgn(cone(0.048,0.22, cx,cy,1.67,8,f"kpk{i}"), MW["gold"]))
        P.append(bpy.context.active_object)
        if i % 2 == 0:
            sm(asgn(sph(0.036, cx,cy,1.63,8,f"kjw{i}"), MW["gem"]))
            P.append(bpy.context.active_object)
    sm(asgn(sph(0.060, ox,oy,1.92,12,"kcg"), MW["gem"]))
    P.append(bpy.context.active_object)
    # Kiếm lớn tay phải
    sm(asgn(cyl(0.025,0.72, ox+0.29,oy,0.42,8,"ksh"), MW["ivory"]))
    P.append(bpy.context.active_object)
    asgn(box(0.022,0.022,0.72, ox+0.29,oy,0.42,"ksb"), MW["blade"])
    P.append(bpy.context.active_object)
    asgn(box(0.22,0.025,0.032, ox+0.29,oy,0.76,"kg"), MW["gold"])
    P.append(bpy.context.active_object)
    # Khiên tay trái
    asgn(box(0.24,0.040,0.32, ox-0.42,oy,0.76,"ksh2"), MW["ivory"])
    P.append(bpy.context.active_object)
    asgn(box(0.26,0.030,0.34, ox-0.42,oy+0.025,0.76,"kshr2"), MW["gold"])
    P.append(bpy.context.active_object)
    sm(asgn(sph(0.050, ox-0.42,oy-0.028,0.82,12,"ksgem"), MW["gem"]))
    P.append(bpy.context.active_object)
    return join_all(P, "W_King")

# ══════════════════════════════════════════════════════════
#  QUÂN ĐEN — body_black dùng lại humanoid với MB
# ══════════════════════════════════════════════════════════
def humanoid_b(ox, oy):
    """Thân quỷ: giáp tối + mắt đỏ + gai vai"""
    P = []
    # Đế + gai vòng quanh
    sm(asgn(cyl(0.44,0.09, ox,oy,0.045,48,"bbase"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.38,0.030, ox,oy,0.09,"bbrim"), MB["red"])); P.append(bpy.context.active_object)
    for i in range(8):
        a = i*math.pi/4
        sm(asgn(cone(0.038,0.13, ox+0.42*math.cos(a),oy+0.42*math.sin(a),0.16,6,f"bsp{i}"),
                MB["spike"])); P.append(bpy.context.active_object)
    # Chân
    for dx in [-0.13, 0.13]:
        sm(asgn(cyl(0.085,0.32, ox+dx,oy,0.25,8,"bleg"), MB["dark"])); P.append(bpy.context.active_object)
        sm(asgn(tor(0.085,0.020, ox+dx,oy,0.40,"bkn"), MB["red"])); P.append(bpy.context.active_object)
    # Hông
    sm(asgn(cyl(0.22,0.16, ox,oy,0.50,12,"bhip"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.21,0.028, ox,oy,0.57,"bbelt"), MB["red"])); P.append(bpy.context.active_object)
    # Ngực
    sm(asgn(cyl(0.20,0.30, ox,oy,0.73,12,"btorso"), MB["dark"])); P.append(bpy.context.active_object)
    # Biểu tượng ngực đỏ
    sm(asgn(sph(0.055, ox,oy-0.19,0.80,12,"bchest_gem"), MB["red"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.19,0.022, ox,oy,0.87,"bcol"), MB["red"])); P.append(bpy.context.active_object)
    # Vai + gai
    for dx in [-0.29, 0.29]:
        sm(asgn(sph(0.12, ox+dx,oy,0.84,16,"bsh"), MB["dark"])); P.append(bpy.context.active_object)
        sm(asgn(tor(0.11,0.018, ox+dx,oy,0.79,"bshr"), MB["gold"])); P.append(bpy.context.active_object)
        sm(asgn(cone(0.040,0.16, ox+dx,oy,0.98,6,"bvsp"), MB["spike"])); P.append(bpy.context.active_object)
    # Cổ
    sm(asgn(cyl(0.072,0.11, ox,oy,0.95,12,"bneck"), MB["skin"])); P.append(bpy.context.active_object)
    # Đầu + mũ giáp
    sm(asgn(sph(0.18, ox,oy,1.12,24,"bhead"), MB["skin"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.20, ox,oy,1.13,24,"bhelm"), MB["dark"])); P.append(bpy.context.active_object)
    # Mắt đỏ phát sáng
    for dx in [-0.07, 0.07]:
        sm(asgn(sph(0.030, ox+dx,oy-0.17,1.17,10,"beye"), MB["eye"])); P.append(bpy.context.active_object)
    # Choàng tối
    sm(asgn(cone(0.26,0.75, ox,oy+0.06,0.90,16,"bcape"), MB["dark"]))
    P[-1].scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    P[-1].location.z = 0.88; P.append(bpy.context.active_object)
    sm(asgn(tor(0.22,0.018, ox,oy+0.05,0.15,"bcapeh"), MB["red"])); P.append(bpy.context.active_object)
    return P

def b_arms(ox, oy):
    P = []
    for dx in [-0.29, 0.29]:
        sm(asgn(cyl(0.065,0.26, ox+dx,oy,0.76,8,"barm"), MB["dark"])); P.append(bpy.context.active_object)
        sm(asgn(tor(0.065,0.018, ox+dx,oy,0.64,"belbow"), MB["gold"])); P.append(bpy.context.active_object)
    return P

def b_spear(ox, oy):
    """Giáo quỷ tay phải"""
    P = []
    sm(asgn(cyl(0.022,0.80, ox+0.29,oy,0.42,8,"bspsh"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(cone(0.055,0.22, ox+0.29,oy,0.86,6,"bsptip"), MB["red"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.055,0.015, ox+0.29,oy,0.74,"bspring"), MB["red"])); P.append(bpy.context.active_object)
    return P

def b_orb_shield(ox, oy):
    """Khiên tối + cầu đỏ tay trái"""
    P = []
    asgn(box(0.22,0.038,0.28, ox-0.42,oy,0.76,"bsh"), MB["dark"]); P.append(bpy.context.active_object)
    asgn(box(0.24,0.028,0.30, ox-0.42,oy+0.022,0.76,"bshr"), MB["red"]); P.append(bpy.context.active_object)
    sm(asgn(sph(0.048, ox-0.42,oy-0.026,0.80,12,"bshgem"), MB["red"])); P.append(bpy.context.active_object)
    return P

def b_pawn(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Mũ tròn giáp
    sm(asgn(sph(0.21, ox,oy,1.15,24,"bph"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.19,0.022, ox,oy,1.02,"bphr"), MB["red"])); P.append(bpy.context.active_object)
    P += b_spear(ox, oy); P += b_orb_shield(ox, oy)
    return join_all(P, "B_Pawn")

def b_rook(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Tháp tối
    sm(asgn(cyl(0.22,0.32, ox,oy,1.44,8,"btwb"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.20,0.020, ox,oy,1.28,"btwbr"), MB["red"])); P.append(bpy.context.active_object)
    for i in range(4):
        a = i*math.pi/2
        mx, my = ox+0.19*math.cos(a), oy+0.19*math.sin(a)
        sm(asgn(cyl(0.072,0.18, mx,my,1.66,8,f"bmt{i}"), MB["dark"])); P.append(bpy.context.active_object)
        sm(asgn(cone(0.050,0.16, mx,my,1.78,6,f"btsp{i}"), MB["spike"])); P.append(bpy.context.active_object)
    # Búa gai tay phải
    sm(asgn(cyl(0.022,0.55, ox+0.29,oy,0.48,8,"bhaft"), MB["dark"])); P.append(bpy.context.active_object)
    asgn(box(0.17,0.08,0.12, ox+0.29,oy,0.26,"bhhead"), MB["iron"]); P.append(bpy.context.active_object)
    for dx2 in [-0.09, 0.09]:
        sm(asgn(cone(0.030,0.10, ox+0.29+dx2,oy,0.26,6,f"bhsp{dx2}"), MB["spike"])); P.append(bpy.context.active_object)
    P += b_orb_shield(ox, oy)
    return join_all(P, "B_Rook")

def b_knight(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Mũ với sừng
    sm(asgn(sph(0.21, ox,oy,1.15,24,"bkh"), MB["dark"])); P.append(bpy.context.active_object)
    asgn(box(0.13,0.030,0.065, ox,oy-0.18,1.11,"bkv"), MB["iron"]); P.append(bpy.context.active_object)
    sm(asgn(tor(0.19,0.020, ox,oy,1.02,"bkhr"), MB["gold"])); P.append(bpy.context.active_object)
    # Sừng 2 bên
    for dx, rz in [(-0.12, -20), (0.12, 20)]:
        sm(asgn(cone(0.040,0.24, ox+dx,oy,1.36,8,f"horn{dx}"), MB["spike"]))
        P[-1].rotation_euler = (0, 0, math.radians(rz))
        bpy.ops.object.transform_apply(rotation=True)
        P.append(bpy.context.active_object)
    P += b_spear(ox, oy); P += b_orb_shield(ox, oy)
    return join_all(P, "B_Knight")

def b_bishop(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Mũ nhọn quỷ
    sm(asgn(cyl(0.19,0.10, ox,oy,1.34,12,"bbmb"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(cone(0.19,0.56, ox,oy,1.66,12,"bbmt"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.17,0.022, ox,oy,1.36,"bbmr"), MB["red"])); P.append(bpy.context.active_object)
    # Gai 2 bên mũ
    for dx in [-0.12, 0.12]:
        sm(asgn(cone(0.030,0.20, ox+dx,oy,1.58,6,f"bbsp{dx}"), MB["spike"])); P.append(bpy.context.active_object)
    # Gậy quỷ tay phải
    sm(asgn(cyl(0.023,0.82, ox+0.29,oy,0.42,8,"bbstf"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.072, ox+0.29,oy,0.87,16,"bborb"), MB["red"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.072,0.015, ox+0.29,oy,0.87,"bborr"), MB["gold"])); P.append(bpy.context.active_object)
    P += b_orb_shield(ox, oy)
    return join_all(P, "B_Bishop")

def b_queen(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Váy quỷ
    sm(asgn(cone(0.42,0.70, ox,oy,0.54,20,"bqsk"), MB["dark"]))
    P[-1].scale.z = -1; bpy.ops.object.transform_apply(scale=True)
    P[-1].location.z = 0.88; P.append(bpy.context.active_object)
    sm(asgn(tor(0.38,0.020, ox,oy,0.18,"bqskh"), MB["red"])); P.append(bpy.context.active_object)
    # Vương miện gai 6 đỉnh
    sm(asgn(tor(0.17,0.032, ox,oy,1.57,48,"bqcr"), MB["gold"])); P.append(bpy.context.active_object)
    for i in range(6):
        a = i*2*math.pi/6
        cx, cy = ox+0.17*math.cos(a), oy+0.17*math.sin(a)
        sm(asgn(cone(0.046,0.24, cx,cy,1.65,6,f"bqpk{i}"), MB["spike"])); P.append(bpy.context.active_object)
        sm(asgn(sph(0.030, cx,cy,1.61,8,f"bqjw{i}"), MB["red"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.060, ox,oy,1.90,12,"bqcg"), MB["red"])); P.append(bpy.context.active_object)
    # Gậy quỷ tay phải
    sm(asgn(cyl(0.024,0.78, ox+0.29,oy,0.44,8,"bqsc"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.078, ox+0.29,oy,0.88,16,"bqsorb"), MB["red"])); P.append(bpy.context.active_object)
    sm(asgn(tor(0.078,0.016, ox+0.29,oy,0.88,"bqsorbr"), MB["gold"])); P.append(bpy.context.active_object)
    return join_all(P, "B_Queen")

def b_king(ox, oy):
    P = humanoid_b(ox, oy); P += b_arms(ox, oy)
    # Giáp ngực hoàng gia
    sm(asgn(cyl(0.22,0.30, ox,oy,0.73,12,"bkch"), MB["iron"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.058, ox,oy-0.19,0.82,12,"bkchg"), MB["red"])); P.append(bpy.context.active_object)
    # Vương miện quỷ vương
    sm(asgn(tor(0.20,0.035, ox,oy,1.60,48,"bkcr"), MB["gold"])); P.append(bpy.context.active_object)
    for i in range(6):
        a = i*2*math.pi/6
        cx, cy = ox+0.20*math.cos(a), oy+0.20*math.sin(a)
        sm(asgn(cone(0.055,0.28, cx,cy,1.70,8,f"bkpk{i}"), MB["spike"])); P.append(bpy.context.active_object)
    # Sừng quỷ trung tâm
    sm(asgn(cone(0.07,0.38, ox,oy,1.72,8,"bkhorn"), MB["spike"])); P.append(bpy.context.active_object)
    sm(asgn(sph(0.065, ox,oy,1.66,12,"bkcg"), MB["red"])); P.append(bpy.context.active_object)
    # Giáo khổng lồ tay phải
    sm(asgn(cyl(0.026,0.88, ox+0.29,oy,0.40,8,"bkspsh"), MB["dark"])); P.append(bpy.context.active_object)
    sm(asgn(cone(0.065,0.28, ox+0.29,oy,0.90,8,"bksptip"), MB["red"])); P.append(bpy.context.active_object)
    # Khiên vua tay trái
    asgn(box(0.26,0.040,0.34, ox-0.43,oy,0.76,"bksh"), MB["dark"]); P.append(bpy.context.active_object)
    asgn(box(0.28,0.030,0.36, ox-0.43,oy+0.025,0.76,"bkshr"), MB["red"]); P.append(bpy.context.active_object)
    sm(asgn(sph(0.055, ox-0.43,oy-0.028,0.82,12,"bksgem"), MB["red"])); P.append(bpy.context.active_object)
    return join_all(P, "B_King")

# ══════════════════════════════════════════════════════════
#  TẠO TẤT CẢ 12 QUÂN
# ══════════════════════════════════════════════════════════
print("Tạo quân TRẮNG (Paladin)...")
w_pawn  (-SP*2.5, 0)
w_rook  (-SP*1.5, 0)
w_knight(-SP*0.5, 0)
w_bishop( SP*0.5, 0)
w_queen ( SP*1.5, 0)
w_king  ( SP*2.5, 0)

print("Tạo quân ĐEN (Demon)...")
b_pawn  (-SP*2.5, YB)
b_rook  (-SP*1.5, YB)
b_knight(-SP*0.5, YB)
b_bishop( SP*0.5, YB)
b_queen ( SP*1.5, YB)
b_king  ( SP*2.5, YB)

# ══════════════════════════════════════════════════════════
#  CAMERA + LIGHTING
# ══════════════════════════════════════════════════════════
scene = bpy.context.scene

# Camera
bpy.ops.object.camera_add(location=(0, -11, 5.5))
cam = bpy.context.active_object; cam.name = "Cam"
cam.rotation_euler = (math.radians(60), 0, 0)
cam.data.type = 'PERSP'; cam.data.lens = 48
scene.camera = cam

# Key light
bpy.ops.object.light_add(type='SUN', location=(5,-6,12))
sun = bpy.context.active_object
sun.data.energy = 5.0
sun.rotation_euler = (math.radians(38),0,math.radians(25))

# Fill
bpy.ops.object.light_add(type='AREA', location=(-6,4,8))
fl = bpy.context.active_object; fl.data.energy=700; fl.data.size=6

# Rim đỏ quỷ
bpy.ops.object.light_add(type='SPOT', location=(0,6,2))
rim = bpy.context.active_object
rim.data.energy=1800; rim.data.color=(1.0,0.05,0.0)
rim.data.spot_size=math.radians(70)
rim.rotation_euler=(math.radians(-52),0,0)

# World
w = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
scene.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.03,0.02,0.02,1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.12

# Render
scene.render.engine = 'CYCLES'
scene.cycles.samples = 256
scene.render.resolution_x = 1920
scene.render.resolution_y = 720
scene.render.film_transparent = True

print("=" * 55)
print("✓ XONG! 12 quân cờ Epic Fantasy đã tạo!")
print("→ Nhấn F12 để render")
print("=" * 55)
