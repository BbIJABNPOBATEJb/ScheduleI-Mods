"""Builds README screenshots (docs/images) from smoke-test captures and draws the mod icons.

Usage: python tools/make_media.py   (needs Pillow; screenshots come from test-runs/, see run-smoke.ps1)
"""
import pathlib

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNS = ROOT / "test-runs"
OUT = ROOT / "docs" / "images"
FONTS = ROOT / "mods" / "Polyglot" / "fonts"

# (output name, run folder glob, file, crop box on a 1920x1080 capture)
SHOTS = [
    ("polyglot-language-picker.jpg", "Polyglot-inspect-il2cpp-*", "settings_picker_open.png", (480, 260, 1440, 820)),
    ("polyglot-contacts-ru.jpg", "Polyglot-tour-ru-il2cpp-*", "06_contacts.png", (340, 160, 1580, 860)),
    ("polyglot-products-ru.jpg", "Polyglot-tour-ru-il2cpp-*", "08_products.png", (340, 160, 1580, 860)),
    ("polyglot-contacts-zh.jpg", "Polyglot-tour-zh-CN-il2cpp-*", "06_contacts.png", (340, 160, 1580, 860)),
    ("worldrates-experience.jpg", "WorldRates-ui-il2cpp-*", "window_after_x3.png", (480, 260, 1440, 820)),
    ("worldrates-presets.jpg", "WorldRates-ui-il2cpp-*", "window_tab4.png", (480, 260, 1440, 820)),
    ("worldrates-deals.jpg", "WorldRates-ui-il2cpp-*", "window_tab2.png", (480, 260, 1440, 820)),
    ("worldrates-storage-x2.jpg", "WorldRates-ui-il2cpp-*", "storage_x2.png", (560, 430, 1360, 860)),
    ("worldrates-pause-menu.jpg", "WorldRates-ui-il2cpp-*", "pause_with_button.png", (0, 520, 700, 900)),
    ("worldrates-ru.jpg", "Polyglot-tour-ru-il2cpp-*", "12_worldrates.png", (480, 260, 1440, 820)),
    ("damageindicator-hit.jpg", "DamageIndicator-combat-il2cpp-20260930-211749", "npc_hit.png", (440, 180, 1480, 760)),
    ("damageindicator-ko.jpg", "DamageIndicator-combat-il2cpp-20260930-200647", "npc_ko.png", (620, 300, 1660, 880)),
    ("damageindicator-player.jpg", "DamageIndicator-combat-il2cpp-20260930-211749", "player_hit.png", (560, 660, 1600, 1080)),
    ("damageindicator-settings.jpg", "DamageIndicator-combat-il2cpp-20260930-211749", "settings_tab1.png", (480, 260, 1440, 820)),
    ("guidearrows-hud.jpg", "GuideArrows-arrows-il2cpp-20260930-211843", "arrows.png", (560, 0, 1360, 220)),
    ("guidearrows-labels.jpg", "GuideArrows-arrows-il2cpp-20260930-211843", "mode_nearest_labels.png", (460, 0, 1460, 260)),
    ("guidearrows-settings.jpg", "GuideArrows-arrows-il2cpp-20260930-211843", "settings_tab1.png", (480, 260, 1440, 820)),
    ("quietpause-settings.jpg", "QuietPause-audio-il2cpp-20260930-212727", "audio_settings.png", (480, 260, 1440, 820)),
    ("electricscooter-ride.jpg", "ElectricScooter-ride-il2cpp-*", "curb_moving.png", (280, 100, 1380, 870)),
    ("electricscooter-front.jpg", "ElectricScooter-ride-il2cpp-*", "scooter_front.png", (250, 0, 1350, 900)),
    ("electricscooter-shop.jpg", "ElectricScooter-shop-il2cpp-*", "shop_choices.png", (460, 380, 1460, 1080)),
    ("electricscooter-held.jpg", "ElectricScooter-shop-il2cpp-*", "held_after_reload.png", (380, 480, 1920, 1080)),
]


def latest(pattern: str, file: str) -> pathlib.Path:
    runs = sorted((p for p in RUNS.glob(pattern) if (p / file).exists()), key=lambda p: p.name)
    if not runs:
        raise FileNotFoundError(f"{pattern}/{file}")
    return runs[-1] / file


def screenshots() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, pattern, file, box in SHOTS:
        img = Image.open(latest(pattern, file)).convert("RGB").crop(box)
        img.save(OUT / name, quality=86, optimize=True)
        print(f"{name}: {img.size[0]}x{img.size[1]}")


def rounded_bg(size: int, top: tuple, bottom: tuple) -> Image.Image:
    grad = Image.new("RGB", (1, size))
    for y in range(size):
        t = y / (size - 1)
        grad.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    grad = grad.resize((size, size))
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=size // 6, fill=255)
    icon = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    icon.paste(grad, (0, 0), mask)
    return icon


def font(file: str, px: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS / file), px)


def cjk(px: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", px)


def bubble(draw: ImageDraw.ImageDraw, box: tuple, fill: tuple, tail_left: bool) -> None:
    x0, y0, x1, y1 = box
    draw.rounded_rectangle(box, radius=(y1 - y0) // 4, fill=fill)
    if tail_left:
        draw.polygon([(x0 + 40, y1 - 4), (x0 + 30, y1 + 46), (x0 + 95, y1 - 4)], fill=fill)
    else:
        draw.polygon([(x1 - 40, y1 - 4), (x1 - 30, y1 + 46), (x1 - 95, y1 - 4)], fill=fill)


def polyglot_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size, (38, 44, 58), (18, 20, 28))
    d = ImageDraw.Draw(icon)
    s = size / 512
    bubble(d, (int(40 * s), int(60 * s), int(330 * s), int(250 * s)), (84, 231, 23), True)
    bubble(d, (int(180 * s), int(270 * s), int(472 * s), int(452 * s)), (118, 201, 255), False)
    d.text((int(185 * s), int(155 * s)), "Aa Я", font=font("OpenSans-Bold.ttf", int(88 * s)), fill=(18, 20, 28), anchor="mm")
    # Microsoft YaHei has no Hangul: draw each glyph with a font that has it.
    glyphs = [("文", cjk(int(76 * s))), ("あ", cjk(int(76 * s))),
              ("한", ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", int(72 * s)))]
    for i, (ch, f) in enumerate(glyphs):
        d.text((int((246 + i * 80) * s), int(361 * s)), ch, font=f, fill=(18, 20, 28), anchor="mm")
    return icon


def worldrates_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size, (40, 48, 40), (16, 20, 16))
    d = ImageDraw.Draw(icon)
    s = size / 512
    green = (84, 231, 23)
    # three rising bars
    for i, h in enumerate((90, 160, 230)):
        x = int((78 + i * 118) * s)
        d.rounded_rectangle((x, int((440 - h) * s), x + int(84 * s), int(440 * s)), radius=int(14 * s),
                            fill=(60 + i * 12, 150 + i * 30, 40))
    d.text((int(256 * s), int(118 * s)), "×10", font=font("OpenSans-Bold.ttf", int(150 * s)), fill=green, anchor="mm",
           stroke_width=int(6 * s), stroke_fill=(16, 20, 16))
    return icon


def damageindicator_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size, (52, 30, 32), (20, 12, 14))
    d = ImageDraw.Draw(icon)
    s = size / 512
    # health bar: frame, recent-damage trail, red fill, yellow stun bar underneath
    d.rounded_rectangle((int(56 * s), int(268 * s), int(456 * s), int(338 * s)), radius=int(18 * s), fill=(10, 8, 8))
    d.rounded_rectangle((int(66 * s), int(278 * s), int(360 * s), int(328 * s)), radius=int(12 * s), fill=(235, 235, 235))
    d.rounded_rectangle((int(66 * s), int(278 * s), int(270 * s), int(328 * s)), radius=int(12 * s), fill=(230, 51, 46))
    d.rounded_rectangle((int(56 * s), int(362 * s), int(456 * s), int(392 * s)), radius=int(10 * s), fill=(10, 8, 8))
    d.rounded_rectangle((int(64 * s), int(368 * s), int(300 * s), int(386 * s)), radius=int(7 * s), fill=(255, 199, 26))
    d.text((int(256 * s), int(150 * s)), "-25", font=font("OpenSans-Bold.ttf", int(170 * s)), fill=(255, 255, 255),
           anchor="mm", stroke_width=int(9 * s), stroke_fill=(20, 12, 14))
    return icon


def nav_arrow(d: ImageDraw.ImageDraw, cx: float, cy: float, scale: float, angle: float, top: tuple, side: tuple) -> None:
    """A beveled navigation arrow seen from above: lighter top face, darker lower edge."""
    import math
    pts = [(0, -1.0), (0.74, 0.74), (0, 0.32), (-0.74, 0.74)]
    a = math.radians(angle)

    def tr(x, y, dy=0.0):
        rx = x * math.cos(a) - y * math.sin(a)
        ry = x * math.sin(a) + y * math.cos(a)
        return (cx + rx * scale, cy + ry * scale + dy)
    d.polygon([tr(x, y, scale * 0.12) for x, y in pts], fill=side)
    d.polygon([tr(x, y) for x, y in pts], fill=top)
    # highlight on the half facing the light
    d.polygon([tr(0, -1.0), tr(0, 0.32), tr(-0.74, 0.74)], fill=tuple(min(255, c + 40) for c in top))


def guidearrows_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size, (30, 38, 52), (12, 16, 24))
    d = ImageDraw.Draw(icon)
    s = size / 512
    # compass strip
    d.rounded_rectangle((int(40 * s), int(56 * s), int(472 * s), int(96 * s)), radius=int(12 * s), fill=(58, 66, 82))
    for x in (100, 180, 332, 412):
        d.rectangle((int(x * s), int(64 * s), int((x + 6) * s), int(88 * s)), fill=(150, 158, 175))
    d.text((int(256 * s), int(76 * s)), "N", font=font("OpenSans-Bold.ttf", int(34 * s)), fill=(235, 238, 245), anchor="mm")
    nav_arrow(d, 256 * s, 270 * s, 150 * s, 35, (72, 220, 110), (26, 120, 52))
    nav_arrow(d, 118 * s, 408 * s, 62 * s, -60, (170, 90, 245), (90, 40, 150))
    nav_arrow(d, 394 * s, 408 * s, 62 * s, 110, (60, 140, 255), (26, 70, 150))
    return icon


def quietpause_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size, (36, 42, 56), (14, 16, 24))
    d = ImageDraw.Draw(icon)
    s = size / 512
    white = (236, 240, 248)
    # speaker
    d.rectangle((int(70 * s), int(200 * s), int(150 * s), int(312 * s)), fill=white)
    d.polygon([(int(150 * s), int(200 * s)), (int(262 * s), int(110 * s)), (int(262 * s), int(402 * s)), (int(150 * s), int(312 * s))], fill=white)
    # pause bars
    blue = (118, 201, 255)
    d.rounded_rectangle((int(312 * s), int(170 * s), int(360 * s), int(342 * s)), radius=int(12 * s), fill=blue)
    d.rounded_rectangle((int(392 * s), int(170 * s), int(440 * s), int(342 * s)), radius=int(12 * s), fill=blue)
    return icon


def scooter(d: ImageDraw.ImageDraw, ox: float, oy: float, s: float) -> None:
    """Side view of the electric scooter (facing right) in a 512-unit design space placed at (ox, oy)."""
    gold, gold_dark, gold_light = (246, 190, 44), (196, 136, 22), (255, 226, 120)
    dark, tyre, hub = (44, 47, 56), (20, 21, 25), (188, 194, 206)

    def p(x, y):
        return (ox + x * s, oy + y * s)

    def line(a, b, width, fill):
        d.line([p(*a), p(*b)], fill=fill, width=max(1, int(width * s)))
        r = width * s / 2
        for x, y in (p(*a), p(*b)):
            d.ellipse((x - r, y - r, x + r, y + r), fill=fill)

    def disc(c, r, fill):
        x, y = p(*c)
        d.ellipse((x - r * s, y - r * s, x + r * s, y + r * s), fill=fill)

    # rear fender, battery pack under the deck
    d.arc((*p(18, 300), *p(182, 464)), 185, 300, fill=dark, width=int(13 * s))
    d.rounded_rectangle((*p(176, 338), *p(332, 372)), radius=int(10 * s), fill=dark)
    # wheels
    for cx in (100, 404):
        disc((cx, 400), 72, tyre)
        disc((cx, 400), 40, hub)
        disc((cx, 400), 26, dark)
        disc((cx, 400), 11, gold)
    # deck with a highlight, tail and neck
    line((92, 312), (150, 326), 20, gold_dark)
    line((150, 326), (350, 326), 26, gold)
    line((158, 319), (342, 319), 7, gold_light)
    line((350, 326), (398, 306), 24, gold_dark)
    # forks
    line((404, 400), (398, 306), 13, dark)
    line((100, 400), (92, 312), 13, dark)
    # steering column, headlight, handlebar with grips
    line((398, 306), (352, 96), 22, gold)
    line((392, 290), (350, 100), 6, gold_light)
    line((372, 150), (398, 146), 26, dark)
    disc((404, 145), 9, (255, 244, 190))
    line((300, 92), (404, 92), 16, dark)
    line((292, 92), (326, 92), 24, tyre)
    line((378, 92), (412, 92), 24, tyre)


def scooter_item_icon(size: int = 256) -> Image.Image:
    """In-game item icon of the Electric Scooter (transparent, like the game's own item icons)."""
    big = Image.new("RGBA", (size * 4, size * 4), (0, 0, 0, 0))
    scooter(ImageDraw.Draw(big), 6 * size / 64, -2 * size / 64, size * 4 / 512 * 0.98)
    return big.resize((size, size), Image.LANCZOS)


def electricscooter_icon(size: int = 512) -> Image.Image:
    icon = rounded_bg(size * 2, (44, 40, 28), (18, 16, 12))
    d = ImageDraw.Draw(icon)
    s = size * 2 / 512
    scooter(d, 34 * s, 4 * s, s * 0.86)
    # lightning bolt: it is electric
    bolt = [(86, 60), (150, 60), (122, 124), (166, 124), (78, 232), (104, 152), (62, 152)]
    d.polygon([(x * s, y * s) for x, y in bolt], fill=(84, 231, 23))
    return icon.resize((size, size), Image.LANCZOS)


def icons() -> None:
    for name, make in (("Polyglot", polyglot_icon), ("WorldRates", worldrates_icon), ("DamageIndicator", damageindicator_icon),
                       ("GuideArrows", guidearrows_icon), ("QuietPause", quietpause_icon),
                       ("ElectricScooter", electricscooter_icon)):
        assets = ROOT / "mods" / name / "assets"
        assets.mkdir(parents=True, exist_ok=True)
        big = make(512)
        big.save(assets / "icon-512.png")
        big.resize((256, 256), Image.LANCZOS).save(assets / "icon.png")  # Thunderstore size
        print(f"{name} icons")
    scooter_item_icon().save(ROOT / "mods" / "ElectricScooter" / "assets" / "item-icon.png")


if __name__ == "__main__":
    screenshots()
    icons()
