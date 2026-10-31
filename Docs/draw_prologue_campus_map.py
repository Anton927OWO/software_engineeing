"""Redraw the NIU Shennong campus map (from the official layout) in the prologue's paper style.

Source coordinates are measured on the official 1551x2000 map, whose top is WEST.
We rotate 90 deg counter-clockwise so north is up: board = OFF + S * (y - 230, 1200 - x).

Usage (from Tracking_NIU/):
  python Docs/draw_prologue_campus_map.py Assets/Levels/Prologue/Art/CampusMap.png Assets/Common/Fonts/NotoSansTC-Bold.otf
The printed landmark coordinates are what PrologueCampusMapBuilder.s_places uses.
"""
import math
import random
import sys
from PIL import Image, ImageDraw, ImageFont

out, font_path = sys.argv[1], sys.argv[2]
W, H = 1300, 820
SS = 2
S = 0.67
OX, OY = 100, 72
random.seed(3)


def T(x, y):
    return (OX + S * (y - 230), OY + S * (1200 - x))


def R(x0, y0, x1, y1):
    ax, ay = T(x0, y0)
    bx, by = T(x1, y1)
    return (min(ax, bx), min(ay, by), max(ax, bx), max(ay, by))


img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
d = ImageDraw.Draw(img)


def q(box):
    return [v * SS for v in box]


def font(size):
    return ImageFont.truetype(font_path, int(size * SS))


PAPER = (243, 233, 210, 255)
GRASS = (205, 222, 176, 255)
GRASS_D = (180, 205, 150, 255)
GREEN = (160, 196, 128, 255)
ROAD = (238, 224, 190, 255)
ROAD_EDGE = (205, 184, 140, 255)
WATER = (168, 208, 222, 255)
WATER_EDGE = (120, 170, 190, 255)
BLD = (236, 214, 178, 255)
BLD_EDGE = (140, 100, 70, 255)
ROOF = (176, 104, 78, 255)
ROOF_B = (160, 120, 90, 255)
ROOF_G = (96, 140, 110, 255)
COURT = (172, 198, 214, 255)
TREE = (122, 165, 104, 255)
TREE_D = (88, 134, 82, 255)
MOUNT = (160, 186, 165, 255)
MOUNT_D = (126, 158, 138, 255)
FRAME = (138, 90, 59, 255)
INK = (36, 61, 50, 255)
INK_SOFT = (115, 90, 70, 255)

# --- paper, mountains (decorative), frame ---
d.rounded_rectangle(q((0, 0, W, H)), radius=28 * SS, fill=PAPER)
for cx, h, col in [(120, 70, MOUNT), (330, 90, MOUNT_D), (560, 62, MOUNT), (800, 95, MOUNT_D),
                   (1030, 72, MOUNT), (1230, 60, MOUNT_D)]:
    d.polygon(q((cx - 200, 66, cx, 66 - h + 8, cx + 200, 66)), fill=col)
d.rectangle(q((10, 62, W - 10, 70)), fill=PAPER)

# --- campus ground ---
d.rounded_rectangle(q(R(175, 237, 1155, 1800)), radius=18 * SS, fill=GRASS)
cx0, cy0, cx1, cy1 = R(175, 237, 1155, 1800)
for _ in range(160):
    x, y = random.uniform(cx0 + 10, cx1 - 10), random.uniform(cy0 + 10, cy1 - 10)
    r = random.randint(3, 7)
    d.ellipse(q((x - r, y - r / 2, x + r, y + r / 2)), fill=GRASS_D)


def road(x0, y0, x1, y1):
    b = R(x0, y0, x1, y1)
    d.rectangle(q((b[0] - 2, b[1] - 2, b[2] + 2, b[3] + 2)), fill=ROAD_EDGE)


def road_fill(x0, y0, x1, y1):
    d.rectangle(q(R(x0, y0, x1, y1)), fill=ROAD)


ROADS = [
    (425, 410, 730, 440),     # north connector
    (190, 455, 425, 490),     # west small gate
    (425, 410, 458, 1640),    # 南大路
    (692, 410, 728, 1640),    # 北大路
    (178, 903, 1140, 930),    # middle cross road
    (178, 1285, 1143, 1320),  # 噶瑪蘭文化廊道
    (425, 1615, 955, 1640),   # south road
    (920, 1320, 955, 1640),   # east road
    (905, 930, 930, 1035),
    (527, 1640, 628, 1800),   # gate approach
]
for r in ROADS:
    road(*r)
for r in ROADS:
    road_fill(*r)


def shadow_box(b, radius=6):
    d.rounded_rectangle(q((b[0] + 3, b[1] + 4, b[2] + 3, b[3] + 4)), radius=radius * SS, fill=(0, 0, 0, 38))


def building(rect, roof=ROOF_B):
    b = R(*rect)
    shadow_box(b)
    d.rounded_rectangle(q(b), radius=6 * SS, fill=BLD, outline=BLD_EDGE, width=2 * SS)
    band = max(6, (b[3] - b[1]) * 0.16)
    d.rounded_rectangle(q((b[0], b[1], b[2], b[1] + band)), radius=6 * SS, fill=roof, outline=BLD_EDGE, width=2 * SS)
    return (b[0], b[1] + band, b[2], b[3])


def area(rect, fill, outline=None):
    b = R(*rect)
    d.rounded_rectangle(q(b), radius=6 * SS, fill=fill, outline=outline or ROAD_EDGE, width=2 * SS)
    return b


def tree(x, y, r):
    d.ellipse(q((x - r + 2, y - r + 3, x + r + 2, y + r + 3)), fill=(0, 0, 0, 35))
    d.ellipse(q((x - r, y - r, x + r, y + r)), fill=TREE, outline=TREE_D, width=2 * SS)
    d.ellipse(q((x - r * .5, y - r * .6, x, y - r * .1)), fill=(150, 190, 125, 255))


labels = []  # (box, text, size_max, color)

BUILDINGS = [
    ((180, 252, 417, 440), "礪金滋蘭學舍", ROOF_B),
    ((466, 256, 692, 402), "時化學舍", ROOF_B),
    ((737, 256, 935, 400), "延英樓", ROOF_B),
    ((180, 507, 417, 656), "工學院", ROOF),
    ((180, 670, 417, 742), "時習大樓", ROOF),
    ((180, 756, 261, 890), "電資二館", ROOF),
    ((273, 756, 417, 890), "電資學院", ROOF),
    ((180, 1055, 417, 1270), "體育館", ROOF_B),
    ((466, 946, 686, 1164), "教穡大樓", ROOF),
    ((587, 1178, 686, 1275), "小劇場", ROOF_B),
    ((737, 946, 897, 1163), "生資大樓", ROOF),
    ((938, 946, 1136, 1035), "經德大樓", ROOF),
    ((1006, 1048, 1137, 1100), "資源回收屋", ROOF_B),
    ((1059, 1112, 1137, 1198), "實驗動物中心", ROOF_B),
    ((735, 1330, 909, 1410), "人文及管理學院", ROOF),
    ((735, 1423, 808, 1525), "通識中心", ROOF_B),
    ((822, 1423, 908, 1525), "食品系實習工廠", ROOF_B),
    ((735, 1553, 910, 1615), "語言中心", ROOF_B),
    ((965, 1553, 1137, 1633), "校友服務中心", ROOF_B),
    ((728, 1650, 953, 1765), "學生活動中心", ROOF),
]
for rect, name, roof in BUILDINGS:
    labels.append((building(rect, roof), name, 17, INK))

# planned dormitory site (L-shape), drawn as a soft dashed lot
for rect in [(737, 452, 875, 890), (875, 725, 1135, 890)]:
    area(rect, (226, 214, 180, 255), BLD_EDGE)
labels.append((R(737, 452, 875, 890), "第三學生宿舍預定地", 16, INK_SOFT))

# library (L-shape) — green roof like the library in the old map
lib = building((962, 1330, 1138, 1462), ROOF_G)
building((962, 1462, 1090, 1527), ROOF_G)
d.rectangle(q(R(966, 1450, 1086, 1470)), fill=BLD)  # hide the seam between the two halves
labels.append((R(962, 1330, 1138, 1527), "圖資大樓", 20, INK))

# administration U-shape + 萬斌廳
for rect in [(464, 1330, 511, 1560), (640, 1330, 686, 1560), (464, 1560, 686, 1615)]:
    building(rect, ROOF)
labels.append((R(464, 1330, 511, 1560), "綜合教學大樓", 14, INK))
labels.append((R(640, 1330, 686, 1560), "綜合教學大樓", 14, INK))
labels.append((R(464, 1560, 686, 1615), "行政大樓", 17, INK))
wb = R(522, 1425, 628, 1495)
d.ellipse(q(wb), fill=(240, 200, 120, 255), outline=BLD_EDGE, width=2 * SS)
labels.append((wb, "萬斌廳\nB1", 15, INK))

# courts & sports field
for rect, name in [((465, 452, 686, 515), "時化球場"), ((180, 946, 417, 1046), "半戶外球場")]:
    labels.append((area(rect, COURT, (120, 150, 170, 255)), name, 16, INK))
area((180, 1330, 417, 1770), COURT, (120, 150, 170, 255))
track = R(227, 1340, 390, 1605)
d.ellipse(q(track), fill=(196, 110, 92, 255), outline=BLD_EDGE, width=2 * SS)
inset = 12
d.ellipse(q((track[0] + inset, track[1] + inset, track[2] - inset, track[3] - inset)), fill=(150, 190, 120, 255),
          outline=(240, 235, 220, 255), width=2 * SS)
labels.append((R(180, 1610, 417, 1770), "操場", 16, INK))

# green spaces
court = area((465, 528, 686, 890), GREEN)
pond = R(495, 770, 665, 865)
d.ellipse(q(pond), fill=WATER, outline=WATER_EDGE, width=3 * SS)
labels.append((R(465, 528, 686, 760), "景觀中庭", 18, INK))
for x, y in [(520, 580), (560, 640), (620, 600), (650, 680)]:
    tree(*T(x, y), 10)
labels.append((area((468, 1178, 587, 1275), GREEN), "綠草坪", 15, INK))
forest = area((737, 1175, 998, 1272), (150, 190, 118, 255))
labels.append((area((1006, 1210, 1137, 1272), (226, 214, 180, 255), BLD_EDGE), "機車停車場", 13, INK_SOFT))
old = area((963, 1647, 1137, 1767), GREEN)
for x, y in [(990, 1680), (1050, 1700), (1110, 1675), (1010, 1745), (1100, 1745)]:
    tree(*T(x, y), 10)
labels.append((old, "老樹區", 17, INK))
for rect in [(471, 1647, 511, 1767), (643, 1647, 683, 1767)]:
    area(rect, GREEN)
for x, y in [(920, 520), (960, 560), (1000, 520), (1090, 820), (1120, 860)]:
    tree(*T(x, y), 11)

# gate, guard room, red house
gate = R(500, 1792, 655, 1806)
d.rounded_rectangle(q(gate), radius=3 * SS, fill=ROOF, outline=BLD_EDGE, width=2 * SS)
labels.append((building((938, 1810, 1150, 1852), ROOF), "宜大紅樓", 14, INK))
labels.append((building((735, 1810, 925, 1852), (90, 80, 70, 255)), "警衛室", 14, INK))

# gate markers along the fence (小門)
for x, y0, y1 in [(170, 450, 498), (170, 897, 942), (170, 1282, 1325), (1160, 895, 942), (1160, 1272, 1325),
                  (1160, 1520, 1555)]:
    d.rectangle(q(R(x - 6, y0, x + 6, y1)), fill=BLD_EDGE)


# --- labels ---
def fit_text(box, text, size_max, color):
    x0, y0, x1, y1 = box
    w, h = (x1 - x0) - 6, (y1 - y0) - 6
    candidates = [text]
    if "\n" not in text and h > w * 1.2:
        candidates.append("\n".join(text))  # stack vertically in tall boxes
    best = None
    for t in candidates:
        for size in range(size_max, 9, -1):
            f = font(size)
            bb = d.multiline_textbbox((0, 0), t, font=f, spacing=1 * SS, align="center")
            tw, th = (bb[2] - bb[0]) / SS, (bb[3] - bb[1]) / SS
            if tw <= w and th <= h:
                if best is None or size > best[1]:
                    best = (t, size)
                break
    if best is None:
        best = (candidates[-1], 10)
    t, size = best
    f = font(size)
    cx, cy = (x0 + x1) / 2 * SS, (y0 + y1) / 2 * SS
    d.multiline_text((cx, cy), t, font=f, fill=color, anchor="mm", align="center", spacing=1 * SS)


for box, text, size, color in labels:
    fit_text(box, text, size, color)

# road names
def road_label(x, y, text, size=14, vertical=False):
    bx, by = T(x, y)
    t = "\n".join(text) if vertical else text
    d.multiline_text((bx * SS, by * SS), t, font=font(size), fill=INK_SOFT, anchor="mm", align="center", spacing=0)


road_label(441, 600, "南大路")
road_label(710, 600, "北大路")
road_label(441, 1460, "南大路")
road_label(710, 1460, "北大路")
road_label(300, 1302, "噶瑪蘭文化廊道", 13, vertical=True)
road_label(1000, 1302, "噶瑪蘭文化廊道", 13, vertical=True)

# streets outside the campus
road_label(1185, 1000, "農權路", 18)
road_label(140, 1000, "女中路", 18)
road_label(578, 1870, "神農路", 18, vertical=True)

# compass (north up)
ccx, ccy = W - 52, H - 56
d.polygon(q((ccx, ccy - 26, ccx + 8, ccy, ccx, ccy + 26, ccx - 8, ccy)), fill=(120, 80, 55, 255))
d.polygon(q((ccx, ccy - 26, ccx + 8, ccy, ccx - 8, ccy)), fill=(176, 104, 78, 255))
d.text((ccx * SS, (ccy - 36) * SS), "N", font=font(14), fill=INK, anchor="mm")

# frame on top (keeps everything inside the paper)
ring = Image.new("L", img.size, 0)
rd = ImageDraw.Draw(ring)
rd.rounded_rectangle(q((0, 0, W, H)), radius=28 * SS, fill=255)
rd.rounded_rectangle(q((10, 10, W - 10, H - 10)), radius=22 * SS, fill=0)
img.paste(Image.new("RGBA", img.size, FRAME), (0, 0), ring)
outside = Image.new("L", img.size, 255)
ImageDraw.Draw(outside).rounded_rectangle(q((0, 0, W, H)), radius=28 * SS, fill=0)
img.paste(Image.new("RGBA", img.size, (0, 0, 0, 0)), (0, 0), outside)

img = img.resize((W, H), Image.LANCZOS)
img.save(out)

# landmark coordinates for the Unity builder (board pixels, top-left origin)
for name, (x, y) in {"gate": (577, 1800), "corridor": (250, 1302), "wanbin": (575, 1460),
                     "forest": (867, 1224)}.items():
    bx, by = T(x, y)
    print(f"{name}: ({bx:.0f}, {by:.0f})")
