"""Cut the StudyLink brand sheet into the web assets used by StudyLink.Web.

Run from the StudyLink folder:  python branding/make_assets.py
Source : branding/source/studylink-brand-sheet.png (1536x1024, logo on top, three icons below)
Output : Images/branding/*.png and favicon.ico (the web project is the repository root)
"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "branding", "source", "studylink-brand-sheet.png")
WEB = ROOT
OUT = os.path.join(WEB, "Images", "branding")
os.makedirs(OUT, exist_ok=True)

sheet = Image.open(SRC).convert("RGB")


def ink_bbox(box, threshold=235):
    """Bounding box of non-background pixels inside box (x0, y0, x1, y1)."""
    x0, y0, x1, y1 = box
    region = sheet.crop(box)
    px = region.load()
    xs, ys = [], []
    for y in range(region.height):
        for x in range(region.width):
            if min(px[x, y]) < threshold:
                xs.append(x)
                ys.append(y)
    return (x0 + min(xs), y0 + min(ys), x0 + max(xs) + 1, y0 + max(ys) + 1)


def pad(box, p):
    x0, y0, x1, y1 = box
    return (max(0, x0 - p), max(0, y0 - p), min(sheet.width, x1 + p), min(sheet.height, y1 + p))


def white_to_alpha(img):
    """Remove the white background while keeping anti-aliased edges (GIMP 'colour to alpha')."""
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, _ = px[x, y]
            a = max(255 - r, 255 - g, 255 - b) / 255.0
            if a <= 0.02:
                px[x, y] = (255, 255, 255, 0)
                continue

            def un(c):
                return max(0, min(255, int(round((c - 255 * (1 - a)) / a))))

            px[x, y] = (un(r), un(g), un(b), int(round(a * 255)))
    return img


def square(img, margin_ratio):
    side = int(max(img.size) * (1 + 2 * margin_ratio))
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(img, ((side - img.width) // 2, (side - img.height) // 2), img)
    return canvas


def masked(img, shape, radius_ratio=0.22, scale=4):
    """Apply an anti-aliased circle or rounded-rectangle mask."""
    img = img.convert("RGBA")
    w, h = img.size
    mask = Image.new("L", (w * scale, h * scale), 0)
    d = ImageDraw.Draw(mask)
    if shape == "circle":
        d.ellipse((0, 0, w * scale - 1, h * scale - 1), fill=255)
    else:
        d.rounded_rectangle((0, 0, w * scale - 1, h * scale - 1),
                            radius=int(min(w, h) * radius_ratio * scale), fill=255)
    img.putalpha(mask.resize((w, h), Image.LANCZOS))
    return img


def to_square(img):
    side = min(img.size)
    return img.resize((side, side), Image.LANCZOS)


def save(img, name, size=None):
    if size:
        img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, name), optimize=True)


# 1. Full horizontal logo (mark + wordmark + tagline), transparent background.
white_to_alpha(sheet.crop(pad(ink_bbox((0, 100, sheet.width, 560)), 10))).save(
    os.path.join(OUT, "studylink-logo.png"), optimize=True)
# Mark only (cap + linked S) for compact headers.
white_to_alpha(sheet.crop(pad(ink_bbox((0, 100, 470, 560)), 6))).save(
    os.path.join(OUT, "studylink-mark.png"), optimize=True)

# 2. Square favicon / app icon: the mark from the white tile, on transparent.
# Threshold 200 skips the tile's soft grey drop shadow, which would otherwise widen the crop.
fav = square(white_to_alpha(sheet.crop(pad(ink_bbox((0, 660, 512, 905), 200), 4))), 0.06)
save(fav, "studylink-favicon.png", 512)
save(fav, "favicon-32.png", 32)
fav.resize((256, 256), Image.LANCZOS).save(os.path.join(WEB, "favicon.ico"),
                                           sizes=[(16, 16), (32, 32), (48, 48), (64, 64)])
# Apple touch icon keeps a white tile so it reads on any home screen.
touch = Image.new("RGBA", fav.size, (255, 255, 255, 255))
touch.paste(fav, (0, 0), fav)
save(masked(touch, "rounded"), "apple-touch-icon.png", 180)

# 3. Dashboard favicon: circular blue version.
save(masked(to_square(sheet.crop(ink_bbox((512, 660, 1024, 905)))), "circle"),
     "studylink-dashboard-icon.png", 256)

# 4. Dark-mode dashboard icon: dark blue rounded tile.
save(masked(to_square(sheet.crop(ink_bbox((1024, 660, sheet.width, 905)))), "rounded", 0.2),
     "studylink-dashboard-dark.png", 256)

for f in sorted(os.listdir(OUT)):
    print(f"{f:34} {Image.open(os.path.join(OUT, f)).size}")
print("favicon.ico written to", WEB)
