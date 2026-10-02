"""Download the real covers of the demo catalogue's textbooks once, into Images/covers/<isbn>.jpg.

Covers come from the Open Library Covers API by ISBN. ISBNs it does not know are skipped and keep the
styled placeholder jacket. At start-up the site copies any cover found here into books that have none
(Infrastructure/CoverLibrary.cs), so the files are only the starting point: managers upload covers
for every new title.

    python tools/fetch_covers.py            (ISBNs are read from Infrastructure/DemoSeeder.cs)
    python tools/fetch_covers.py 9780134093413 ...
"""
import io
import os
import re
import sys
import urllib.error
import urllib.request

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET = os.path.join(ROOT, "Images", "covers")
SEEDER = os.path.join(ROOT, "Infrastructure", "DemoSeeder.cs")
URL = "https://covers.openlibrary.org/b/isbn/%s-L.jpg?default=false"
MAX_WIDTH = 600


def isbns_from_seeder():
    with open(SEEDER, encoding="utf-8-sig") as f:
        return sorted(set(re.findall(r'"(97[89]\d{10})"', f.read())))


def fetch(isbn):
    request = urllib.request.Request(URL % isbn, headers={"User-Agent": "StudyLink cover fetch (UFH student project)"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            data = response.read()
    except urllib.error.HTTPError as e:
        return "not found (%d)" % e.code
    image = Image.open(io.BytesIO(data)).convert("RGB")
    if image.width < 250:  # thumbnails look blurred on a catalogue card; the placeholder jacket reads better
        return "too small (%dx%d), skipped" % image.size
    if image.width > MAX_WIDTH:
        image = image.resize((MAX_WIDTH, round(image.height * MAX_WIDTH / image.width)), Image.LANCZOS)
    path = os.path.join(TARGET, isbn + ".jpg")
    image.save(path, "JPEG", quality=82, optimize=True, progressive=True)
    return "saved %dx%d, %d KB" % (image.width, image.height, os.path.getsize(path) // 1024)


def main():
    os.makedirs(TARGET, exist_ok=True)
    for isbn in sys.argv[1:] or isbns_from_seeder():
        print(isbn, fetch(isbn))
    return 0


if __name__ == "__main__":
    sys.exit(main())
