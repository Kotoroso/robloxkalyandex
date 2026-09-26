#!/usr/bin/env python3
"""Generate chunky cartoon game UI icons (Roblox simulator style).

Every icon is described as an ordered list of "parts" (float masks at 2048px plus
fill style). The renderer:
  1. unions all silhouette parts and dilates the union (exact Euclidean distance
     transform) to build a uniform, round-joined dark outline,
  2. paints each part: optional own (thinner) separation outline, vertical
     gradient fill, soft bottom-right shade, and upper-left glossy crescent,
  3. crops to content, fits it into 512x512 with a 6% margin (LANCZOS).
The outline width is chosen from the fill extent so that the final outline is
~5.5% of the icon size for every icon.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

S = 2048
PAD = 256  # extra canvas so outlines never clip at the design edges
CS = S + 2 * PAD
OUT = 512
MARGIN = 0.06
OUTLINE_FRAC = 0.055
DARK = "#141220"
OUT_DIR = "/home/user/robloxkalyandex/Assets/_Game/Resources/Art"
PREVIEW = "/tmp/claude-0/-home-user/86ac1269-ff02-5b52-ae58-50e228fe329f/scratchpad/icons_preview.png"

YY, XX = np.mgrid[0:CS, 0:CS].astype(np.float32)


# ----------------------------------------------------------------- helpers
def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


def P(v):
    return PAD + v * S


def D(v):
    """normalized distance -> pixels"""
    return v * S


def _new():
    img = Image.new("L", (CS, CS), 0)
    return img, ImageDraw.Draw(img)


def _arr(img):
    return np.asarray(img, np.float32) / 255.0


def ellipse(cx, cy, rx, ry=None):
    ry = rx if ry is None else ry
    img, d = _new()
    d.ellipse([P(cx - rx), P(cy - ry), P(cx + rx), P(cy + ry)], fill=255)
    return _arr(img)


def circle(cx, cy, r):
    return ellipse(cx, cy, r, r)


def poly(pts):
    img, d = _new()
    d.polygon([(P(x), P(y)) for x, y in pts], fill=255)
    return _arr(img)


def rect(x0, y0, x1, y1):
    img, d = _new()
    d.rectangle([P(x0), P(y0), P(x1), P(y1)], fill=255)
    return _arr(img)


def rrect(x0, y0, x1, y1, r):
    img, d = _new()
    d.rounded_rectangle([P(x0), P(y0), P(x1), P(y1)], radius=D(r), fill=255)
    return _arr(img)


def rot_ellipse(cx, cy, rx, ry, ang_deg, n=180):
    a = math.radians(ang_deg)
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        x, y = rx * math.cos(t), ry * math.sin(t)
        pts.append((cx + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(a) + y * math.cos(a)))
    return poly(pts)


def stroke(pts, w):
    """Thick polyline with round caps/joins (via distance dilation of a thin line)."""
    img, d = _new()
    d.line([(P(x), P(y)) for x, y in pts], fill=255, width=3)
    return dilate(_arr(img), D(w) / 2)


def star_pts(cx, cy, R, ratio=0.5, n=5, rot=-90):
    pts = []
    for i in range(2 * n):
        r = R if i % 2 == 0 else R * ratio
        a = math.radians(rot + i * 180 / n)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def U(*ms):
    return np.maximum.reduce(ms)


def I(a, b):
    return np.minimum(a, b)


def SUB(a, b):
    return a * (1 - b)


def dilate(m, r):
    d = ndimage.distance_transform_edt(m <= 0.5).astype(np.float32)
    return np.clip(r - d + 1.0, 0, 1)


def erode(m, r):
    d = ndimage.distance_transform_edt(m > 0.5).astype(np.float32)
    return np.clip(d - r, 0, 1)


def round_corners(m, r_convex, r_concave=0):
    """Opening rounds convex corners, closing rounds concave corners (distances in norm units)."""
    if r_concave:
        m = erode(dilate(m, D(r_concave)), D(r_concave))
    if r_convex:
        m = dilate(erode(m, D(r_convex)), D(r_convex))
    return m


def shift(m, dx, dy):
    dx, dy = int(round(dx)), int(round(dy))
    out = np.zeros_like(m)
    h, w = m.shape
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = m[ys, xs]
    return out


def bbox(m):
    ys, xs = np.where(m > 0.5)
    if len(xs) == 0:
        return 0, 0, CS, CS
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def blur(m, r):
    img = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8))
    return _arr(img.filter(ImageFilter.GaussianBlur(float(r))))


# ----------------------------------------------------------------- renderer
class Canvas:
    def __init__(self):
        self.rgb = np.zeros((CS, CS, 3), np.float32)  # premultiplied
        self.a = np.zeros((CS, CS), np.float32)

    def paint(self, color, alpha):
        """color: (3,) or (S,S,3) straight rgb; alpha: (S,S)."""
        alpha = np.clip(alpha, 0, 1)
        col = color if color.ndim == 3 else color[None, None, :]
        self.rgb = col * alpha[..., None] + self.rgb * (1 - alpha[..., None])
        self.a = alpha + self.a * (1 - alpha)

    def image(self):
        a = np.clip(self.a, 0, 1)
        rgb = np.where(a[..., None] > 1e-4, self.rgb / np.maximum(a[..., None], 1e-4), 0)
        arr = np.dstack([np.clip(rgb, 0, 1), a])
        return Image.fromarray((arr * 255 + 0.5).astype(np.uint8), "RGBA")


def gradient(mask, top, bot, box=None):
    x0, y0, x1, y1 = box if box is not None else bbox(mask)
    t = np.clip((YY - y0) / max(1, (y1 - y0)), 0, 1)[..., None]
    return hexrgb(top) * (1 - t) + hexrgb(bot) * t


def gloss_mask(mask, W, strength=0.6, inset=0.42, off=0.10, spot=True):
    x0, y0, x1, y1 = bbox(mask)
    size = max(x1 - x0, y1 - y0)
    E = erode(mask, W * inset)
    if E.max() < 0.5:
        return np.zeros_like(mask)
    dx = size * off
    H = E * (1 - shift(E, dx, dx * 1.05))
    # fade toward the bottom-right so highlight lives on the upper-left
    u = (XX - x0) / max(1, x1 - x0)
    v = (YY - y0) / max(1, y1 - y0)
    w = np.clip(1.25 - 1.35 * (u * 0.55 + v * 0.95), 0, 1)
    H = H * w
    H = (H > 0.35).astype(np.float32)
    rr = max(2, size * 0.018)
    H = dilate(erode(H, rr), rr)  # open: drop thin tails
    out = H
    if spot:
        # small round sparkle just below-right of the crescent's top-left end
        ys, xs = np.where(H > 0.5)
        if len(xs):
            # point of crescent closest to the top-left corner
            k = np.argmin((xs - x0) * 0.6 + (ys - y0))
            sx, sy = xs[k], ys[k]
            cx, cy = sx + size * 0.10, sy + size * 0.05
            sr = size * 0.035
            sp = ((XX - cx) ** 2 + (YY - cy) ** 2 <= sr * sr).astype(np.float32)
            out = np.maximum(out, sp * E)
    return blur(out, max(1.5, size * 0.004)) * strength


def shade_mask(mask, W, strength=0.22, off=0.07):
    x0, y0, x1, y1 = bbox(mask)
    size = max(x1 - x0, y1 - y0)
    d = size * off
    sh = mask * (1 - shift(mask, -d * 0.5, -d))
    return blur(sh, size * 0.01) * mask * strength


def part(mask, top=None, bot=None, outline=0.0, gloss=0.55, shade=0.2, sil=True,
         ocolor=DARK, box=None, alpha=1.0, solid=None, gloss_kw=None, shade_kw=None):
    return dict(mask=mask, top=top, bot=bot, outline=outline, gloss=gloss, shade=shade,
                sil=sil, ocolor=ocolor, box=box, alpha=alpha, solid=solid,
                gloss_kw=gloss_kw or {}, shade_kw=shade_kw or {})


def render(parts, name):
    sil = U(*[p["mask"] for p in parts if p["sil"]])
    x0, y0, x1, y1 = bbox(sil)
    maxdim = max(x1 - x0, y1 - y0)
    # choose W so that after fit-to-(1-2*MARGIN) the outline is OUTLINE_FRAC of the icon
    W = OUTLINE_FRAC * maxdim / ((1 - 2 * MARGIN) - 2 * OUTLINE_FRAC)
    cv = Canvas()
    cv.paint(hexrgb(DARK), dilate(sil, W))
    for p in parts:
        m = p["mask"]
        if m.max() < 0.5:
            continue
        if p.get("onlygloss"):
            cv.paint(np.ones(3, np.float32), gloss_mask(m, W, p["gloss"], **p["gloss_kw"]))
            continue
        if p["outline"] > 0:
            cv.paint(hexrgb(p["ocolor"]), dilate(m, W * p["outline"]) * p["alpha"])
        if p["solid"] is not None:
            col = hexrgb(p["solid"])
        else:
            col = gradient(m, p["top"], p["bot"], p["box"])
        cv.paint(col, m * p["alpha"])
        if p["shade"]:
            cv.paint(np.zeros(3, np.float32), shade_mask(m, W, p["shade"], **p["shade_kw"]) * p["alpha"])
        if p["gloss"]:
            cv.paint(np.ones(3, np.float32), gloss_mask(m, W, p["gloss"], **p["gloss_kw"]) * p["alpha"])
    img = cv.image()
    # fit into OUT x OUT with margin, centered on the alpha bbox
    bx = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    crop = img.crop(bx)
    target = OUT * (1 - 2 * MARGIN)
    sc = target / max(crop.width, crop.height)
    nw, nh = max(1, round(crop.width * sc)), max(1, round(crop.height * sc))
    small = crop.resize((nw, nh), Image.LANCZOS)
    final = Image.new("RGBA", (OUT, OUT), (0, 0, 0, 0))
    final.paste(small, ((OUT - nw) // 2, (OUT - nh) // 2))
    path = os.path.join(OUT_DIR, name)
    final.save(path)
    print(f"{name}: scale 2048->{sc * S / OUT:.3f} of design, outline {W * sc:.1f}px")
    return final


# ----------------------------------------------------------------- icons
def emboss_star(cx, cy, R, ratio, W_hint, light, dark, shadow, rnd=0.012):
    st = round_corners(poly(star_pts(cx, cy, R, ratio)), rnd)
    sh = shift(st, D(0.012), D(0.016))
    return [
        part(sh, solid=shadow, gloss=0, shade=0, sil=False),
        part(st, light, dark, gloss=0.45, shade=0.12, sil=False,
             gloss_kw=dict(inset=0.15, off=0.08, spot=False)),
    ]


def icon_coin():
    cx, cy, r = 0.5, 0.46, 0.38
    face = circle(cx, cy, r)
    edge = U(circle(cx, cy + 0.06, r), rect(cx - r, cy, cx + r, cy + 0.06))
    edge = SUB(edge, face)
    ring_outer = circle(cx, cy, r * 0.80)
    ring_inner = circle(cx, cy, r * 0.72)
    parts = [
        part(U(edge, face), solid="#C27400", gloss=0, shade=0),
        part(edge, "#E89A10", "#A85E00", gloss=0, shade=0, sil=False),
        part(face, "#FFE45C", "#F2A007", outline=0.35, gloss=0, shade=0.15),
        part(SUB(ring_outer, ring_inner), "#E08E00", "#C27400", gloss=0, shade=0, sil=False),
        part(ring_inner, "#FFE97A", "#F5AE15", gloss=0, shade=0, sil=False),
    ]
    parts += emboss_star(cx, cy + 0.005, r * 0.52, 0.48, 0, "#FFF6B0", "#F7B51A", "#C27400")
    # face gloss last (over everything on the face)
    g = part(face, gloss=0.6, sil=False, gloss_kw=dict(inset=0.25, off=0.07))
    g["onlygloss"] = True
    parts.append(g)
    return parts


def icon_bolt():
    pts = [(0.60, 0.04), (0.20, 0.56), (0.45, 0.56), (0.35, 0.96), (0.82, 0.40), (0.56, 0.40), (0.72, 0.04)]
    m = round_corners(poly(pts), 0.025, 0.02)
    return [part(m, "#7FF6FF", "#1A9BFF", gloss=0.6, shade=0.2)]


def icon_star():
    m = round_corners(poly(star_pts(0.5, 0.53, 0.47, 0.52)), 0.055, 0.03)
    return [part(m, "#FF9BE6", "#E23BB5", gloss=0.6, shade=0.2)]


def icon_gear():
    cx, cy = 0.5, 0.5
    body = circle(cx, cy, 0.31)
    teeth = []
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        ca, sa = math.cos(a), math.sin(a)
        px, py = -sa, ca
        r0, r1, w0, w1 = 0.26, 0.44, 0.085, 0.060
        teeth.append(poly([
            (cx + ca * r0 + px * w0, cy + sa * r0 + py * w0),
            (cx + ca * r1 + px * w1, cy + sa * r1 + py * w1),
            (cx + ca * r1 - px * w1, cy + sa * r1 - py * w1),
            (cx + ca * r0 - px * w0, cy + sa * r0 - py * w0),
        ]))
    g = round_corners(U(body, *teeth), 0.022, 0.03)
    hole = circle(cx, cy, 0.105)
    g = SUB(g, hole)
    hub = SUB(circle(cx, cy, 0.19), hole)
    return [
        part(g, "#F2F4FA", "#9AA3B8", gloss=0.6, shade=0.18),
        part(hub, "#C9CFDD", "#8C95AB", outline=0.35, gloss=0.35, shade=0, sil=False,
             gloss_kw=dict(inset=0.1, off=0.12, spot=False)),
    ]


def mini_coin(cx, cy, r):
    c = circle(cx, cy, r)
    inner = circle(cx, cy, r * 0.70)
    st = round_corners(poly(star_pts(cx, cy + r * 0.03, r * 0.52, 0.5)), 0.006)
    return [
        part(c, "#FFE45C", "#F2A007", outline=0.45, gloss=0.55, shade=0.15, sil=False,
             gloss_kw=dict(inset=0.12, off=0.10, spot=False)),
        part(SUB(c, inner), "#F5B21A", "#D98A00", gloss=0, shade=0, sil=False, alpha=0.9),
        part(st, "#FFF3A0", "#F7B51A", outline=0.12, ocolor="#B86E00", gloss=0, shade=0, sil=False),
    ]


def icon_bag():
    handle = SUB(ellipse(0.5, 0.34, 0.20, 0.23), ellipse(0.5, 0.34, 0.115, 0.15))
    handle = I(handle, rect(0, 0, 1, 0.40))
    handle = round_corners(handle, 0.01)
    body = round_corners(poly([(0.22, 0.34), (0.78, 0.34), (0.87, 0.92), (0.13, 0.92)]), 0.06)
    band = I(body, rect(0, 0, 1, 0.44))
    parts = [
        part(handle, "#4FCB47", "#1E8A2E", gloss=0.4, shade=0,
             gloss_kw=dict(inset=0.15, off=0.25, spot=False)),
        part(body, "#7CF06A", "#27A83A", outline=0.6, gloss=0.55, shade=0.2),
        part(band, "#5CD650", "#2E9D38", outline=0.35, gloss=0, shade=0, sil=False),
    ]
    # handle rivets on the band
    for x in (0.385, 0.615):
        parts.append(part(circle(x, 0.39, 0.022), solid="#FFE45C", outline=0.28, gloss=0, shade=0, sil=False))
    parts += mini_coin(0.5, 0.66, 0.145)
    return parts


def egg_mask(cx, cy, rx, ry, k=0.14, n=240):
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        s = math.sin(t)
        x = cx + rx * math.cos(t) * (1 + k * s)
        y = cy + ry * s
        pts.append((x, y))
    return poly(pts)


def icon_egg():
    egg = egg_mask(0.5, 0.52, 0.34, 0.44)
    parts = [part(egg, "#FFD36B", "#FF8A1F", gloss=0.6, shade=0.22,
                  gloss_kw=dict(off=0.09))]
    spots = [(0.64, 0.30, 0.07, 0.06, 20), (0.36, 0.52, 0.09, 0.075, -15), (0.66, 0.60, 0.075, 0.065, 30),
             (0.47, 0.78, 0.085, 0.06, 0), (0.28, 0.73, 0.05, 0.045, 0), (0.52, 0.45, 0.04, 0.035, 0),
             (0.76, 0.44, 0.04, 0.035, 0)]
    for (x, y, rx, ry, a) in spots:
        m = I(rot_ellipse(x, y, rx, ry, a), erode(egg, D(0.02)))
        parts.append(part(m, "#FFF4C2", "#FFD07A", gloss=0, shade=0.18, sil=False,
                          shade_kw=dict(off=0.18)))
    return parts


def icon_dragon():
    # head: cranium + snout + neck
    cranium = circle(0.40, 0.46, 0.27)
    snout = rrect(0.40, 0.40, 0.90, 0.76, 0.17)
    neck = poly([(0.20, 0.60), (0.46, 0.66), (0.44, 0.97), (0.14, 0.97)])
    head = round_corners(U(cranium, snout), 0.02, 0.10)
    neck_r = I(round_corners(U(neck, head), 0.02, 0.08), rect(0, 0, 1, 0.94))
    neck_r = SUB(neck_r, head)
    horn1 = round_corners(poly([(0.20, 0.34), (0.08, 0.06), (0.34, 0.26)]), 0.025)
    horn2 = round_corners(poly([(0.36, 0.24), (0.34, 0.03), (0.52, 0.22)]), 0.025)
    spikes = []
    for (x, y, tx, ty) in [(0.15, 0.50, 0.03, 0.52), (0.14, 0.66, 0.03, 0.74)]:
        spikes.append(round_corners(poly([(x, y - 0.07), (tx, ty), (x + 0.03, y + 0.08)]), 0.015))
    spk = U(*spikes)
    jaw = I(head, ellipse(0.71, 0.80, 0.19, 0.115))
    belly = I(neck_r, ellipse(0.40, 0.95, 0.10, 0.25))
    eye = ellipse(0.50, 0.40, 0.115, 0.13)
    pupil = ellipse(0.545, 0.42, 0.058, 0.075)
    hl = circle(0.525, 0.385, 0.024)
    nostril = rot_ellipse(0.80, 0.50, 0.028, 0.018, -20)
    mouth = stroke([(0.60, 0.605), (0.63, 0.64), (0.69, 0.665), (0.77, 0.67), (0.84, 0.655)], 0.022)
    mouth = I(mouth, head)
    cheek = I(ellipse(0.54, 0.585, 0.06, 0.035), head)
    brow = stroke([(0.41, 0.25), (0.50, 0.23), (0.58, 0.26)], 0.03)
    return [
        part(horn1, "#FFF3CF", "#E0B56A", gloss=0.45, shade=0.15, gloss_kw=dict(inset=0.1, spot=False)),
        part(horn2, "#FFF3CF", "#E0B56A", gloss=0.45, shade=0.15, gloss_kw=dict(inset=0.1, spot=False)),
        part(spk, "#FFD04A", "#E89A1A", gloss=0, shade=0.15),
        part(neck_r, "#F58A30", "#D9531A", gloss=0, shade=0.2),
        part(belly, "#FFE0A8", "#F7B866", gloss=0, shade=0, sil=False),
        part(head, "#FFA23A", "#E8601A", outline=0.55, gloss=0.55, shade=0.18,
             gloss_kw=dict(off=0.07, spot=False)),
        part(jaw, "#FFDDA6", "#F5B060", outline=0.0, gloss=0, shade=0.12, sil=False),
        part(cheek, solid="#FF6F8A", gloss=0, shade=0, sil=False, alpha=0.55),
        part(mouth, solid=DARK, gloss=0, shade=0, sil=False),
        part(eye, "#FFFFFF", "#DDE3F0", outline=0.42, gloss=0, shade=0.12, sil=False),
        part(pupil, solid=DARK, gloss=0, shade=0, sil=False),
        part(hl, solid="#FFFFFF", gloss=0, shade=0, sil=False),
        part(circle(0.565, 0.455, 0.011), solid="#FFFFFF", gloss=0, shade=0, sil=False),
        part(nostril, solid=DARK, gloss=0, shade=0, sil=False),
    ]


def icon_paw():
    pad = round_corners(U(ellipse(0.5, 0.70, 0.25, 0.19), ellipse(0.38, 0.62, 0.14, 0.13),
                          ellipse(0.62, 0.62, 0.14, 0.13)), 0.02, 0.06)
    toes = [(0.20, 0.36, -25), (0.50, 0.22, 0), (0.80, 0.36, 25)]
    parts = []
    claws = []
    toe_masks = []
    for (x, y, a) in toes:
        ra = math.radians(a)
        tipx, tipy = x + math.sin(ra) * 0.20, y - math.cos(ra) * 0.20
        bx, by = x + math.sin(ra) * 0.07, y - math.cos(ra) * 0.07
        px, py = math.cos(ra), math.sin(ra)
        claws.append(round_corners(poly([(bx + px * 0.05, by + py * 0.05), (tipx, tipy),
                                         (bx - px * 0.05, by - py * 0.05)]), 0.012))
        toe_masks.append(rot_ellipse(x, y, 0.10, 0.125, a))
    for c in claws:
        parts.append(part(c, "#FFF6DA", "#D9C08A", gloss=0.4, shade=0.1, gloss_kw=dict(inset=0.05, spot=False)))
    for t in toe_masks:
        parts.append(part(t, "#FFB347", "#F0701A", outline=0.55, gloss=0.55, shade=0.18,
                          gloss_kw=dict(inset=0.25, off=0.14, spot=False)))
    parts.append(part(pad, "#FFB347", "#F0701A", gloss=0.55, shade=0.2))
    return parts


def icon_shoe():
    upper = poly([(0.20, 0.28), (0.24, 0.23), (0.34, 0.27), (0.42, 0.33), (0.47, 0.19), (0.58, 0.15),
                  (0.68, 0.33), (0.80, 0.41), (0.92, 0.49), (0.98, 0.61), (0.97, 0.74), (0.20, 0.76)])
    upper = round_corners(upper, 0.04, 0.05)
    sole = rrect(0.15, 0.67, 1.00, 0.86, 0.09)
    toe = I(upper, ellipse(0.98, 0.74, 0.23, 0.21))
    heel = I(upper, ellipse(0.15, 0.52, 0.12, 0.32))
    swoosh = I(stroke([(0.30, 0.60), (0.50, 0.58), (0.72, 0.47)], 0.05), erode(upper, D(0.03)))
    sole_line = I(stroke([(0.18, 0.77), (0.97, 0.77)], 0.018), erode(sole, D(0.025)))
    lines = []
    for (y, x0, x1) in [(0.38, 0.00, 0.10), (0.50, -0.05, 0.09), (0.62, 0.00, 0.09)]:
        lines.append(stroke([(x0, y), (x1, y)], 0.055))
    parts = [part(l, "#FFFFFF", "#BFE6FF", gloss=0, shade=0.15) for l in lines]
    parts += [
        part(upper, "#6EC8FF", "#1F6FE0", gloss=0.55, shade=0.18, gloss_kw=dict(off=0.06, spot=False)),
        part(heel, "#4AA4F5", "#1B5CC4", outline=0.35, gloss=0, shade=0, sil=False),
        part(toe, "#8AD6FF", "#3A8BEA", outline=0.35, gloss=0.35, shade=0, sil=False,
             gloss_kw=dict(inset=0.2, off=0.12, spot=False)),
        part(swoosh, "#FFFFFF", "#D6ECFF", outline=0.3, gloss=0, shade=0, sil=False),
        part(sole, "#FFFFFF", "#C4D2EA", outline=0.55, gloss=0.35, shade=0.15,
             gloss_kw=dict(inset=0.12, off=0.10, spot=False)),
        part(sole_line, solid="#9FB2D4", gloss=0, shade=0, sil=False),
    ]
    for (x, y) in [(0.585, 0.255), (0.625, 0.315), (0.665, 0.375)]:
        lace = stroke([(x - 0.03, y + 0.025), (x + 0.035, y - 0.03)], 0.032)
        parts.append(part(lace, solid="#FFFFFF", outline=0.25, gloss=0, shade=0, sil=False))
    return parts


def icon_book():
    pages = rrect(0.26, 0.13, 0.86, 0.90, 0.05)
    cover = rrect(0.16, 0.08, 0.78, 0.85, 0.06)
    ribbon = poly([(0.56, 0.80), (0.66, 0.80), (0.66, 0.97), (0.61, 0.92), (0.56, 0.97)])
    spine = I(cover, rect(0, 0, 0.27, 1))
    corners = [I(cover, poly([(0.62, 0.0), (0.80, 0.0), (0.80, 0.18)])),
               I(cover, poly([(0.62, 0.95), (0.80, 0.95), (0.80, 0.77)]))]
    emb = round_corners(poly(star_pts(0.52, 0.46, 0.16, 0.5)), 0.012)
    emb_bg = circle(0.52, 0.465, 0.17)
    parts = [
        part(ribbon, "#FF6B6B", "#D62E3E", gloss=0, shade=0.15),
        part(pages, "#FFFFFF", "#D9DEEA", gloss=0, shade=0.1),
    ]
    for k in range(3):
        o = 0.022 * (k + 1)
        ln = I(stroke([(0.80 + o * 0.0, 0.20), (0.80, 0.84)], 0.008), pages)
    parts += [
        part(I(stroke([(0.815, 0.18), (0.815, 0.86)], 0.010), pages), solid="#B8C0D4", gloss=0, shade=0, sil=False),
        part(I(stroke([(0.60, 0.875), (0.82, 0.875)], 0.010), pages), solid="#B8C0D4", gloss=0, shade=0, sil=False),
        part(cover, "#6FA8FF", "#2A5BD8", outline=0.55, gloss=0.55, shade=0.18,
             gloss_kw=dict(off=0.06, spot=False)),
        part(spine, "#4F86F0", "#1F46B8", outline=0.35, gloss=0, shade=0, sil=False),
    ]
    for c in corners:
        parts.append(part(c, "#FFE45C", "#F2A007", outline=0.35, gloss=0, shade=0, sil=False))
    parts.append(part(emb_bg, "#3F74E6", "#2450C4", outline=0.0, gloss=0, shade=0, sil=False, alpha=0.9))
    parts.append(part(emb, "#FFE45C", "#F2A007", outline=0.35, gloss=0.4, shade=0.1, sil=False,
                      gloss_kw=dict(inset=0.1, off=0.1, spot=False)))
    return parts


def crown_mask(cx, cy, w, h):
    x0, x1 = cx - w / 2, cx + w / 2
    yb, yt = cy + h * 0.45, cy - h * 0.35
    pts = [(x0, yb), (x0 - w * 0.04, yt), (cx - w * 0.25, cy + h * 0.02), (cx, yt - h * 0.1),
           (cx + w * 0.25, cy + h * 0.02), (x1 + w * 0.04, yt), (x1, yb)]
    base = poly(pts)
    balls = U(circle(x0 - w * 0.04, yt, w * 0.075), circle(cx, yt - h * 0.1, w * 0.08),
              circle(x1 + w * 0.04, yt, w * 0.075))
    return round_corners(U(base, balls), 0.008, 0.012)


def icon_yan():
    cx, cy, r = 0.5, 0.46, 0.40
    face = circle(cx, cy, r)
    edge = SUB(U(circle(cx, cy + 0.06, r), rect(cx - r, cy, cx + r, cy + 0.06)), face)
    gold = circle(cx, cy, r * 0.70)
    cr = crown_mask(cx, cy + 0.01, 0.34, 0.28)
    cr_sh = shift(cr, D(0.012), D(0.016))
    parts = [
        part(U(edge, face), solid="#3E1790", gloss=0, shade=0),
        part(edge, "#7A3AE0", "#4A1CA8", gloss=0, shade=0, sil=False),
        part(face, "#B06CFF", "#6A2BD8", outline=0.35, gloss=0, shade=0.15),
    ]
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        rr = r * 0.855
        parts.append(part(circle(cx + math.cos(a) * rr, cy + math.sin(a) * rr, 0.022), "#FFF1A0", "#F5B316",
                          outline=0.0, gloss=0, shade=0, sil=False))
    parts += [
        part(gold, "#FFE45C", "#F2A007", outline=0.4, gloss=0, shade=0.15, sil=False),
        part(cr_sh, solid="#C27400", gloss=0, shade=0, sil=False),
        part(cr, "#FFF6B0", "#F7B51A", outline=0.12, ocolor="#B86E00", gloss=0.35, shade=0.1, sil=False,
             gloss_kw=dict(inset=0.1, off=0.10, spot=False)),
        # rim band on crown
        part(I(cr, rect(0, cy + 0.01 + 0.28 * 0.25, 1, 1)), "#F5B21A", "#D98A00", gloss=0, shade=0, sil=False),
    ]
    for (dx, col) in [(-0.09, "#FF5A8A"), (0.0, "#5AD7FF"), (0.09, "#7CF06A")]:
        parts.append(part(circle(cx + dx, cy + 0.01 + 0.28 * 0.34, 0.018), solid=col, outline=0.1,
                          ocolor="#8A4E00", gloss=0, shade=0, sil=False))
    g = part(face, gloss=0.6, sil=False, gloss_kw=dict(inset=0.25, off=0.07))
    g["onlygloss"] = True
    parts.append(g)
    return parts


def icon_lock():
    outer = U(circle(0.5, 0.33, 0.24), rect(0.26, 0.33, 0.74, 0.55))
    inner = U(circle(0.5, 0.33, 0.12), rect(0.38, 0.33, 0.62, 0.60))
    shackle = SUB(outer, inner)
    body = rrect(0.14, 0.43, 0.86, 0.93, 0.11)
    inset = SUB(rrect(0.19, 0.48, 0.81, 0.88, 0.08), rrect(0.215, 0.505, 0.785, 0.855, 0.065))
    kh = round_corners(U(circle(0.5, 0.63, 0.065), poly([(0.47, 0.64), (0.53, 0.64), (0.555, 0.79), (0.445, 0.79)])), 0.012)
    parts = [
        part(shackle, "#F2F4FA", "#8F99B0", gloss=0.5, shade=0.15, gloss_kw=dict(inset=0.2, off=0.12, spot=False)),
        part(body, "#FFE45C", "#F2A007", outline=0.6, gloss=0.55, shade=0.2),
        part(inset, solid="#D98A00", gloss=0, shade=0, sil=False, alpha=0.55),
        part(kh, solid=DARK, gloss=0, shade=0, sil=False),
    ]
    return parts


ICONS = [
    ("icon_coin.png", icon_coin), ("icon_bolt.png", icon_bolt), ("icon_star.png", icon_star),
    ("icon_gear.png", icon_gear), ("icon_bag.png", icon_bag), ("icon_egg.png", icon_egg),
    ("icon_dragon.png", icon_dragon), ("icon_paw.png", icon_paw), ("icon_shoe.png", icon_shoe),
    ("icon_book.png", icon_book), ("icon_yan.png", icon_yan), ("icon_lock.png", icon_lock),
]


def _render_with_onlygloss(parts, name):
    # parts flagged "onlygloss" add just a gloss crescent (e.g. on coin faces after emboss)
    for p in parts:
        if p.get("onlygloss"):
            p["alpha"] = 1.0
    return render(parts, name)


def contact_sheet(imgs):
    cols, rows, cell, pad = 4, 3, 256, 24
    W_ = cols * cell + (cols + 1) * pad
    H_ = rows * cell + (rows + 1) * pad
    sheet = Image.new("RGBA", (W_, H_), (0x28, 0x32, 0x5A, 255))
    for i, im in enumerate(imgs):
        c, r = i % cols, i // cols
        x = pad + c * (cell + pad)
        y = pad + r * (cell + pad)
        sheet.alpha_composite(im.resize((cell, cell), Image.LANCZOS), (x, y))
    sheet.convert("RGB").save(PREVIEW)
    print("preview:", PREVIEW)


if __name__ == "__main__":
    only = set(sys.argv[1:])
    os.makedirs(OUT_DIR, exist_ok=True)
    imgs = []
    for name, fn in ICONS:
        if only and name not in only:
            imgs.append(Image.open(os.path.join(OUT_DIR, name)).convert("RGBA"))
            continue
        imgs.append(_render_with_onlygloss(fn(), name))
    contact_sheet(imgs)
