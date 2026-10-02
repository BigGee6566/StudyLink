"""Turn the photos in Images/auth into web-ready WebP files in Images/site.

The originals are camera-sized (up to 7000 px, 3 MB). Pages use only the outputs, named
<use>-<width>.webp, and the SiteImage helper builds each <img srcset> from whatever widths exist.

    python tools/optimize_images.py

Drop a new photo into Images/auth and re-run: if it is not in USES below it is still converted,
under a name made from its file name, ready to be wired into a page.
"""
import os
import re
import sys

from PIL import Image, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "Images", "auth")
TARGET = os.path.join(ROOT, "Images", "site")
QUALITY = 78

# use -> (source file, widths). Widths larger than the source are skipped (never upscale).
USES = {
    "hero": ("group tutoring.jpg", [1920, 1280, 800]),
    "tutoring": ("one on one tutoring.jpg", [1280, 800]),
    "online": ("online tutoring.jpg", [1280, 800]),
    "library": ("Library-book-shelf.jpg", [1280, 800]),
    "book-pick": ("picking-a-book-in-a-bookshelf.jpg", [800]),
    "books-stack": ("Books-Stagged.jpg", [800]),
    "campus-alice": ("Alice campus gate.jpeg", [800]),
    "campus-alice-road": ("Alice campus.jpeg", [800]),
    "campus-east-london": ("Fort hare East London Campus.jpeg", [800]),
    "campus-bisho": ("Bisho campus.jpeg", [800]),
    "community": ("ufh students outside alice gate.jpeg", [800]),
    "login-bg": ("login-background.jpg", [1280, 800]),
    "register-bg": ("register-background.jpg", [800]),
}
DEFAULT_WIDTHS = [1280, 800]


def slug(file_name):
    return re.sub(r"[^a-z0-9]+", "-", os.path.splitext(file_name)[0].lower()).strip("-")


def convert(source, use, widths):
    with Image.open(source) as original:
        image = ImageOps.exif_transpose(original).convert("RGB")
    written = []
    for width in sorted({min(w, image.width) for w in widths}, reverse=True):
        height = round(image.height * width / image.width)
        resized = image if width == image.width else image.resize((width, height), Image.LANCZOS)
        path = os.path.join(TARGET, "%s-%d.webp" % (use, width))
        resized.save(path, "WEBP", quality=QUALITY, method=6)
        written.append("%s (%d KB)" % (os.path.basename(path), os.path.getsize(path) // 1024))
    return written


def main():
    os.makedirs(TARGET, exist_ok=True)
    mapped = {source: use for use, (source, _) in USES.items()}
    missing = [source for source in mapped if not os.path.exists(os.path.join(SOURCE, source))]
    if missing:
        print("Missing in Images/auth: " + ", ".join(missing))
        return 1
    for file_name in sorted(os.listdir(SOURCE)):
        if not file_name.lower().endswith((".jpg", ".jpeg", ".png", ".webp")):
            continue
        use = mapped.get(file_name, slug(file_name))
        widths = USES[use][1] if use in USES else DEFAULT_WIDTHS
        print("%-22s <- %-40s %s" % (use, file_name, ", ".join(convert(os.path.join(SOURCE, file_name), use, widths))))
    return 0


if __name__ == "__main__":
    sys.exit(main())
