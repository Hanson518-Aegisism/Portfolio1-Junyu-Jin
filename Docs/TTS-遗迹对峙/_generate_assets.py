# -*- coding: utf-8 -*-
"""Generate Tabletop Simulator assets for 遗迹对峙."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
CARDS = ROOT / "cards"
BOARDS = ROOT / "boards"
FONT = "/usr/share/fonts/truetype/wqy/wqy-microhei.ttc"
FONT_FALLBACK = "/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf"

# Card size TTS-friendly (~poker ratio)
CW, CH = 512, 768

ITEMS = [
    (1, "金字塔"),
    (2, "狮身人面"),
    (3, "房子"),
    (4, "大象"),
    (5, "河马"),
    (6, "马"),
    (7, "猫"),
    (8, "鳄鱼"),
    (9, "蛇"),
    (10, "猫头鹰"),
    (11, "鹰"),
    (12, "鱼"),
    (13, "鸟"),
    (14, "树"),
    (15, "棕榈"),
    (16, "船"),
    (17, "车轮"),
    (18, "锤子"),
    (19, "针"),
    (20, "矛"),
    (21, "弓"),
    (22, "斧头"),
    (23, "火把"),
    (24, "面包"),
    (25, "水壶"),
    (26, "金币"),
    (27, "王冠"),
    (28, "镜子"),
    (29, "盾"),
    (30, "铲子"),
]

WORDS = [
    "活的",
    "鸟",
    "虫",
    "鱼",
    "兽",
    "植物",
    "水",
    "火",
    "土",
    "大",
    "小",
    "锋利",
    "危险",
    "工具",
    "贵重",
    "金属",
]

# Simple symbol glyphs paired for colonist language board (demo pairing)
SYMBOLS = [
    "△",
    "▽",
    "◇",
    "○",
    "◎",
    "☆",
    "✦",
    "≈",
    "＋",
    "－",
    "□",
    "◇",
    "丫",
    "◎",
    "☉",
    "⌂",
]

COMBOS = [
    ("巨兽行进", "大象 ＋ 河马 ＋ 马", "+4"),
    ("兵器架", "矛 ＋ 盾 ＋ 弓", "+3"),
    ("工匠袋", "锤子 ＋ 针 ＋ 铲子", "+3"),
    ("王权三宝", "王冠 ＋ 金币 ＋ 金字塔", "+5"),
    ("河上船家", "船 ＋ 鱼 ＋ 水壶", "+3"),
    ("家与火", "房子 ＋ 火把 ＋ 面包", "+3"),
    ("飞与爬", "鹰 ＋ 蛇 ＋ 鳄鱼", "+3"),
    ("守护兽", "猫 ＋ 猫头鹰", "+2"),
    ("巨石文明", "金字塔 ＋ 狮身人面", "+2"),
    ("伐木人", "斧头 ＋ 树", "+2"),
]

GRID_SCORES = [
    ["+2", "+1", "+3", "+1", "+2", "+1"],
    ["+1", "−1", "+2", "−1", "+1", "+2"],
    ["+3", "+2", "0", "+2", "+3", "−1"],
    ["+1", "−1", "+2", "−1", "+1", "+2"],
    ["+2", "+1", "+3", "+1", "+2", "+1"],
]
COLS = ["A", "B", "C", "D", "E", "F"]


def font(size: int) -> ImageFont.FreeTypeFont:
    try:
        return ImageFont.truetype(FONT, size)
    except OSError:
        return ImageFont.truetype(FONT_FALLBACK, size)


def center_text(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill=(30, 30, 30),
) -> None:
    x0, y0, x1, y1 = box
    bbox = draw.textbbox((0, 0), text, font=fnt)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = x0 + (x1 - x0 - tw) // 2
    y = y0 + (y1 - y0 - th) // 2
    draw.text((x, y), text, font=fnt, fill=fill)


def rounded_rect(draw, box, radius, fill, outline=None, width=2):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def make_card_face(num: int, name: str, path: Path) -> None:
    img = Image.new("RGB", (CW, CH), (245, 239, 228))
    d = ImageDraw.Draw(img)
    rounded_rect(d, (24, 24, CW - 24, CH - 24), 28, (252, 248, 240), (90, 60, 40), 4)
    # header bar
    d.rectangle((40, 40, CW - 40, 120), fill=(55, 70, 90))
    center_text(d, (40, 40, CW - 40, 120), f"贡品  {num:02d}", font(36), (255, 255, 255))
    # big name
    center_text(d, (40, 220, CW - 40, 420), name, font(96 if len(name) <= 2 else 72))
    # footer
    d.rectangle((40, CH - 140, CW - 40, CH - 40), fill=(232, 224, 210))
    center_text(d, (40, CH - 140, CW - 40, CH - 40), "遗迹对峙", font(28), (90, 70, 50))
    img.save(path)


def make_card_back(path: Path) -> None:
    img = Image.new("RGB", (CW, CH), (40, 52, 68))
    d = ImageDraw.Draw(img)
    rounded_rect(d, (24, 24, CW - 24, CH - 24), 28, (55, 70, 90), (200, 180, 140), 6)
    for i in range(6):
        y = 160 + i * 80
        d.line((80, y, CW - 80, y), fill=(80, 95, 115), width=2)
    center_text(d, (40, 280, CW - 40, 400), "遗迹对峙", font(56), (245, 230, 190))
    center_text(d, (40, 400, CW - 40, 480), "贡  品", font(42), (210, 200, 170))
    img.save(path)


def make_grid_board(
    path: Path,
    title: str,
    cell_labels: list[list[str]] | None,
    empty: bool = False,
    note: str = "",
) -> None:
    rows, cols = 5, 6
    margin = 80
    header = 100
    cell = 140
    w = margin * 2 + cols * cell
    h = header + margin + rows * cell + (80 if note else 40)
    img = Image.new("RGB", (w, h), (236, 230, 218))
    d = ImageDraw.Draw(img)
    center_text(d, (0, 20, w, 90), title, font(42), (40, 40, 40))
    ox, oy = margin, header
    for r in range(rows):
        for c in range(cols):
            x0 = ox + c * cell
            y0 = oy + r * cell
            x1, y1 = x0 + cell - 4, y0 + cell - 4
            fill = (250, 246, 238) if (r + c) % 2 == 0 else (240, 234, 222)
            d.rectangle((x0, y0, x1, y1), fill=fill, outline=(90, 70, 50), width=2)
            # coord small
            d.text((x0 + 8, y0 + 6), f"{COLS[c]}{r + 1}", font=font(20), fill=(120, 100, 80))
            if not empty and cell_labels is not None:
                center_text(d, (x0, y0 + 20, x1, y1), cell_labels[r][c], font(36), (30, 30, 30))
            elif empty:
                # empty circle for marking
                cx, cy = (x0 + x1) // 2, (y0 + y1) // 2 + 10
                d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), outline=(160, 140, 120), width=2)
    if note:
        center_text(d, (0, h - 70, w, h - 20), note, font(22), (90, 70, 50))
    img.save(path)


def make_word_board(path: Path, title: str, with_symbols: bool) -> None:
    cols, rows = 4, 4
    cell_w, cell_h = 280, 120
    margin = 60
    header = 100
    w = margin * 2 + cols * cell_w
    h = header + margin + rows * cell_h + 40
    img = Image.new("RGB", (w, h), (236, 230, 218))
    d = ImageDraw.Draw(img)
    center_text(d, (0, 20, w, 90), title, font(40))
    for i, word in enumerate(WORDS):
        r, c = divmod(i, cols)
        # wait - 4 cols, index i: row = i // 4, col = i % 4
        r, c = i // cols, i % cols
        x0 = margin + c * cell_w
        y0 = header + r * cell_h
        x1, y1 = x0 + cell_w - 8, y0 + cell_h - 8
        d.rounded_rectangle((x0, y0, x1, y1), 12, fill=(252, 248, 240), outline=(90, 70, 50), width=2)
        if with_symbols:
            d.text((x0 + 16, y0 + 20), SYMBOLS[i], font=font(40), fill=(55, 70, 90))
            d.text((x0 + 90, y0 + 28), f"=  {word}", font=font(36), fill=(30, 30, 30))
        else:
            d.text((x0 + 24, y0 + 20), word, font=font(36), fill=(30, 30, 30))
            # blank line for writing symbol
            d.line((x0 + 24, y1 - 28, x1 - 24, y1 - 28), fill=(180, 160, 140), width=2)
            d.text((x0 + 24, y1 - 55), "符号：", font=font(20), fill=(140, 120, 100))
    img.save(path)


def make_combo_board(path: Path) -> None:
    w, h = 1200, 900
    img = Image.new("RGB", (w, h), (236, 230, 218))
    d = ImageDraw.Draw(img)
    center_text(d, (0, 20, w, 80), "特定贡品组合（额外加分）", font(40))
    d.text((60, 90), "殖民者按盖住的贡品核；原住民按送成功的贡品核。可叠加。", font=font(24), fill=(90, 70, 50))
    y = 140
    # header
    d.rectangle((50, y, w - 50, y + 50), fill=(55, 70, 90))
    d.text((70, y + 12), "组合名", font=font(26), fill=(255, 255, 255))
    d.text((280, y + 12), "需要的贡品", font=font(26), fill=(255, 255, 255))
    d.text((w - 180, y + 12), "额外分", font=font(26), fill=(255, 255, 255))
    y += 50
    for i, (name, need, pts) in enumerate(COMBOS):
        fill = (252, 248, 240) if i % 2 == 0 else (240, 234, 222)
        d.rectangle((50, y, w - 50, y + 60), fill=fill, outline=(180, 160, 140))
        d.text((70, y + 16), name, font=font(26), fill=(30, 30, 30))
        d.text((280, y + 16), need, font=font(24), fill=(30, 30, 30))
        d.text((w - 160, y + 16), pts, font=font(28), fill=(120, 40, 40))
        y += 60
    img.save(path)


def make_score_sheet(path: Path) -> None:
    cols = ["次序", "殖民者甲", "殖民者乙", "原住民1", "原住民2", "原住民3", "原住民4"]
    rows = ["1", "2", "3", "4", "5", "6", "格子小计", "组合加成", "合计"]
    cell_w, cell_h = 160, 70
    margin = 40
    header = 90
    w = margin * 2 + len(cols) * cell_w
    h = header + margin + (len(rows) + 1) * cell_h
    img = Image.new("RGB", (w, h), (236, 230, 218))
    d = ImageDraw.Draw(img)
    center_text(d, (0, 15, w, 75), "计分表 · 遗迹对峙", font(36))
    # header row
    for c, name in enumerate(cols):
        x0 = margin + c * cell_w
        y0 = header
        d.rectangle((x0, y0, x0 + cell_w - 2, y0 + cell_h - 2), fill=(55, 70, 90))
        center_text(d, (x0, y0, x0 + cell_w - 2, y0 + cell_h - 2), name, font(20), (255, 255, 255))
    for r, label in enumerate(rows):
        for c in range(len(cols)):
            x0 = margin + c * cell_w
            y0 = header + (r + 1) * cell_h
            fill = (55, 70, 90) if c == 0 else ((252, 248, 240) if r % 2 == 0 else (240, 234, 222))
            text_fill = (255, 255, 255) if c == 0 else (30, 30, 30)
            d.rectangle((x0, y0, x0 + cell_w - 2, y0 + cell_h - 2), fill=fill, outline=(160, 140, 120))
            if c == 0:
                center_text(d, (x0, y0, x0 + cell_w - 2, y0 + cell_h - 2), label, font(20), text_fill)
    img.save(path)


def make_draw_board(path: Path) -> None:
    w, h = 900, 600
    img = Image.new("RGB", (w, h), (250, 246, 238))
    d = ImageDraw.Draw(img)
    d.rectangle((20, 20, w - 20, h - 20), outline=(90, 70, 50), width=4)
    center_text(d, (0, 30, w, 90), "画符板（殖民者当众画符）", font(36), (55, 70, 90))
    # ruled area
    for i in range(8):
        y = 140 + i * 50
        d.line((80, y, w - 80, y), fill=(210, 200, 185), width=2)
    center_text(d, (0, h - 80, w, h - 30), "用 TTS 笔工具在此书写符号", font(24), (140, 120, 100))
    img.save(path)


def build_manifest() -> dict:
    cards = [{"file": "back.png", "role": "deck_back"}]
    for n, name in ITEMS:
        fname = f"{n:02d}-{name}.png"
        cards.append({"file": fname, "role": "item_face", "id": n, "name": name})
    boards = [
        {"file": "格分图-5x6.png", "role": "score_grid", "note": "全桌可见"},
        {"file": "进贡卡-5x6.png", "role": "offer_board", "note": "每人一张，私下标记后同时亮"},
        {"file": "笔记板-16词.png", "role": "native_notes", "note": "原住民手牌区私藏"},
        {"file": "语言板-殖民者.png", "role": "colonist_language", "note": "殖民者手牌区私藏；符号为示范配对，可每局重洗"},
        {"file": "组合加分表.png", "role": "combo_table"},
        {"file": "计分表.png", "role": "score_sheet"},
        {"file": "画符板-空白.png", "role": "draw_pad"},
    ]
    return {
        "game": "遗迹对峙",
        "version": "tts-assets-1.0",
        "card_size_px": [CW, CH],
        "field": "5x6",
        "items": [{"id": n, "name": name} for n, name in ITEMS],
        "words": WORDS,
        "combos": [{"name": a, "need": b, "bonus": c} for a, b, c in COMBOS],
        "grid_scores": GRID_SCORES,
        "files": {"cards": cards, "boards": boards},
        "tts_tips": {
            "deck": "Custom Deck：Face 用单张导入或逐张 Custom Card；Back 用 cards/back.png",
            "private": "语言板/笔记板建议放进各自手牌区，仅自己可见",
            "tokens": "用 TTS 自带 Colored Chip 两色作殖民者收物标记",
        },
    }


def main() -> None:
    CARDS.mkdir(parents=True, exist_ok=True)
    BOARDS.mkdir(parents=True, exist_ok=True)

    make_card_back(CARDS / "back.png")
    for n, name in ITEMS:
        make_card_face(n, name, CARDS / f"{n:02d}-{name}.png")

    make_grid_board(
        BOARDS / "格分图-5x6.png",
        "格分图（5×6）",
        GRID_SCORES,
        note="已被盖住的格不能再转、不能再进贡",
    )
    make_grid_board(
        BOARDS / "进贡卡-5x6.png",
        "进贡卡（5×6）",
        None,
        empty=True,
        note="屏风后标记 → 全体同时亮出；方向与场地对齐",
    )
    make_word_board(BOARDS / "笔记板-16词.png", "原住民笔记板（16 词）", with_symbols=False)
    make_word_board(BOARDS / "语言板-殖民者.png", "殖民者语言板（示范配对）", with_symbols=True)
    make_combo_board(BOARDS / "组合加分表.png")
    make_score_sheet(BOARDS / "计分表.png")
    make_draw_board(BOARDS / "画符板-空白.png")

    manifest = build_manifest()
    (ROOT / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    # Flat folder for easy TTS import
    import shutil

    flat = ROOT / "全部图片-导入用"
    if flat.exists():
        shutil.rmtree(flat)
    flat.mkdir(parents=True)
    shutil.copy2(CARDS / "back.png", flat / "00-牌背.png")
    for n, name in ITEMS:
        src = CARDS / f"{n:02d}-{name}.png"
        shutil.copy2(src, flat / f"贡品-{n:02d}-{name}.png")
    board_map = {
        "格分图-5x6.png": "版图-格分图-5x6.png",
        "进贡卡-5x6.png": "版图-进贡卡-5x6.png",
        "笔记板-16词.png": "版图-笔记板-16词.png",
        "语言板-殖民者.png": "版图-语言板-殖民者.png",
        "组合加分表.png": "版图-组合加分表.png",
        "计分表.png": "版图-计分表.png",
        "画符板-空白.png": "版图-画符板-空白.png",
    }
    for src_name, dst_name in board_map.items():
        shutil.copy2(BOARDS / src_name, flat / dst_name)

    zip_path = ROOT / "全部图片-导入用.zip"
    if zip_path.exists():
        zip_path.unlink()
    shutil.make_archive(str(ROOT / "全部图片-导入用"), "zip", ROOT, "全部图片-导入用")

    print("OK", ROOT)
    print("cards", len(list(CARDS.glob("*.png"))))
    print("boards", len(list(BOARDS.glob("*.png"))))
    print("flat", len(list(flat.glob("*.png"))))


if __name__ == "__main__":
    main()
