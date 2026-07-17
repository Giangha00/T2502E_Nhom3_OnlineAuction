#!/usr/bin/env python3
"""Generate SpreadsheetAuctionCatalog entries from Pokemon/Sports sample folders."""

from __future__ import annotations

import hashlib
import json
import re
import shutil
import ssl
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path

import urllib.error
import urllib.request

SSL_CONTEXT = ssl._create_unverified_context()

ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "wwwroot" / "images" / "cards"
CATALOG_PATH = ROOT / "Data" / "SpreadsheetAuctionCatalog.cs"
MAP_PATH = ROOT / "tools" / "cloudinary_seed_image_map.json"
APPSETTINGS_PATH = ROOT / "appsettings.json"
LOCAL_SETTINGS_PATH = ROOT / "appsettings.Local.json"
FOLDER_PREFIX = "auction-house/seed"
MAX_WORKERS = 4

SOURCE_FOLDERS: list[tuple[Path, str, str]] = [
    (Path("/Users/macbook/Downloads/pokemon cgc (hà)"), "pokemon", "Pokémon"),
    (Path("/Users/macbook/Downloads/sport bgs (vanh)"), "sports", "Sports"),
]


@dataclass
class ProductSeed:
    category_name: str
    category_folder: str
    name: str
    description: str
    primary_local: str
    gallery_local: list[str]
    starting_price: float
    grade_label: str
    set_name: str
    year: int
    language: str
    card_number: str
    end_minutes: int


def slugify(text: str) -> str:
    text = text.replace("_", "-").replace("'", "")
    text = re.sub(r"[^a-zA-Z0-9]+", "-", text)
    text = re.sub(r"-+", "-", text).strip("-").lower()
    return text[:120] or "card"


def parse_grade(stem: str) -> tuple[str, str]:
    patterns = [
        r"CGC\s+AUTH",
        r"BGS\s+10\.?\d*\s+BLACK\s+LABEL",
        r"BGS\s+10\s+PRISTINE",
        r"BGS\s+10\s+GEM\s+MINT",
        r"CGC\s+10\s+PRISTINE",
        r"CGC\s+10\s+GEM\s+MINT",
        r"BGS\s+9\.5\s+GEM\s+MINT",
        r"BGS\s+8\.5\s+NM-MT\+?",
        r"BGS\s+9\s+MINT",
        r"CGC\s+9\s+MINT",
        r"CGC\s+10",
        r"BGS\s+10",
        r"BGS\s+9\.5",
        r"BGS\s+9",
        r"BGS\s+8\.5",
        r"CGC\s+9",
    ]
    upper = stem.upper()
    for pattern in patterns:
        match = re.search(pattern.replace(" ", r"\s+"), upper)
        if match:
            label = re.sub(r"\s+", " ", match.group(0)).strip()
            return label.title().replace("Nm-Mt+", "NM-MT+"), label
    return "Graded", "GRADED"


def parse_card_number(stem: str) -> str:
    match = re.search(r"#([A-Za-z0-9\-]+)", stem)
    return match.group(1) if match else "N/A"


def parse_year(stem: str) -> int:
    match = re.match(r"(\d{4})\b", stem)
    return int(match.group(1)) if match else 2020


def parse_language(category: str, stem: str) -> str:
    if category == "Sports":
        return "English"
    if "Japanese" in stem:
        return "Japanese"
    return "English"


def parse_set_name(stem: str, card_number: str, year: int) -> str:
    cleaned = stem
    cleaned = re.sub(r"^\d{4}\s+", "", cleaned)
    cleaned = re.sub(r"\s+(CGC|BGS)\b.*$", "", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"\s+#" + re.escape(card_number) + r"\b.*$", "", cleaned)
    cleaned = cleaned.strip()
    return cleaned[:120] if cleaned else f"Set {year}"


def short_product_name(grade_label: str, stem: str, year: int, card_number: str) -> str:
    core = stem
    core = re.sub(r"^\d{4}\s+", "", core)
    core = re.sub(r"\s+(CGC|BGS)\b.*$", "", core, flags=re.IGNORECASE)
    if card_number != "N/A":
        core = re.sub(r"\s+#" + re.escape(card_number) + r"\b", f" #{card_number}", core)
    core = re.sub(r"\s+", " ", core).strip()
    if len(core) > 70:
        core = core[:67] + "..."
    return f"{grade_label} {core} {year}"


def estimate_price(grade_label: str, category: str) -> float:
    label = grade_label.upper()
    if "BLACK LABEL" in label or "PRISTINE" in label:
        base = 2400 if category == "pokemon" else 1800
    elif "10" in label and "9" not in label:
        base = 900 if category == "pokemon" else 720
    elif "9.5" in label:
        base = 480 if category == "pokemon" else 420
    elif "9" in label:
        base = 360 if category == "pokemon" else 300
    else:
        base = 280 if category == "pokemon" else 240
    return float(base)


def build_description(
    category_name: str,
    grade_label: str,
    set_name: str,
    year: int,
    language: str,
    card_number: str,
    starting_price: float,
) -> str:
    game = "Pokémon TCG" if category_name == "Pokémon" else "Sports Trading Cards"
    manufacturer = "The Pokémon Company" if category_name == "Pokémon" else "Various"
    market_high = int(starting_price * 1.65)
    return (
        "Item specifics | "
        f"Condition: Graded - {grade_label} | "
        f"Card Number: {card_number} | "
        f"Set: {set_name} | "
        f"Year: {year} | "
        f"Language: {language} | "
        f"Manufacturer: {manufacturer} | "
        f"Game: {game} | "
        f"Certification: {grade_label} | "
        f"Tham khảo thị trường 2026: ~${starting_price:,.0f} - ${market_high:,.0f}"
    )


def collect_products() -> list[ProductSeed]:
    products: list[ProductSeed] = []
    used_names: set[str] = set()

    for source_dir, folder, category_name in SOURCE_FOLDERS:
        if not source_dir.exists():
            raise FileNotFoundError(f"Missing folder: {source_dir}")

        target_dir = CARDS_ROOT / folder
        target_dir.mkdir(parents=True, exist_ok=True)

        fronts = sorted(
            path
            for path in source_dir.iterdir()
            if path.is_file()
            and (path.suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"} or path.suffix == "")
            and "_BACK" not in path.stem.upper()
        )

        for index, front in enumerate(fronts):
            stem = front.stem
            grade_label, _ = parse_grade(stem)
            card_number = parse_card_number(stem)
            year = parse_year(stem)
            language = parse_language(folder, stem)
            set_name = parse_set_name(stem, card_number, year)
            name = short_product_name(grade_label, stem, year, card_number)
            if name in used_names:
                name = f"{name} ({index + 1})"
            used_names.add(name)

            slug = slugify(f"{year}-{grade_label}-{stem}")[:100]
            ext = front.suffix.lower()
            primary_name = f"{slug}{ext}"
            primary_path = target_dir / primary_name
            shutil.copy2(front, primary_path)

            gallery_local: list[str] = []
            back_candidates = [
                source_dir / f"{stem}_BACK{ext}",
                source_dir / f"{stem}_BACK.jpg",
                source_dir / f"{stem}_BACK.jpeg",
                source_dir / f"{stem}_BACK.png",
            ]
            back_source = next((item for item in back_candidates if item.exists()), None)
            if back_source:
                gallery_name = f"{slug}-back{back_source.suffix.lower()}"
                gallery_path = target_dir / gallery_name
                shutil.copy2(back_source, gallery_path)
                gallery_local.append(f"/images/cards/{folder}/{gallery_name}")

            starting_price = estimate_price(grade_label, folder)
            description = build_description(
                category_name,
                grade_label,
                set_name,
                year,
                language,
                card_number,
                starting_price,
            )

            products.append(
                ProductSeed(
                    category_name=category_name,
                    category_folder=folder,
                    name=name,
                    description=description,
                    primary_local=f"/images/cards/{folder}/{primary_name}",
                    gallery_local=gallery_local,
                    starting_price=starting_price,
                    grade_label=grade_label,
                    set_name=set_name,
                    year=year,
                    language=language,
                    card_number=card_number,
                    end_minutes=12 + (index % 6) * 3,
                )
            )

    return products


def load_cloudinary_settings() -> tuple[str, str, str]:
    settings: dict = {}
    for path in (APPSETTINGS_PATH, LOCAL_SETTINGS_PATH):
        if path.exists():
            payload = json.loads(path.read_text(encoding="utf-8"))
            section = payload.get("CloudinarySettings") or {}
            settings.update({k: v for k, v in section.items() if v})

    cloud_name = settings.get("CloudName", "")
    api_key = settings.get("ApiKey", "")
    api_secret = settings.get("ApiSecret", "")
    if not cloud_name or not api_key or not api_secret:
        raise RuntimeError("CloudinarySettings missing in appsettings.")
    return cloud_name, api_key, api_secret


def sign_params(params: dict[str, str]) -> str:
    to_sign = "&".join(f"{k}={v}" for k, v in sorted(params.items()) if v is not None and v != "")
    return hashlib.sha1((to_sign + API_SECRET).encode("utf-8")).hexdigest()


def upload_file(local_path: Path, category_folder: str) -> tuple[str, str]:
    stem = local_path.stem
    web_path = f"/images/cards/{category_folder}/{local_path.name}"
    timestamp = str(int(time.time()))
    params = {
        "folder": f"{FOLDER_PREFIX}/{category_folder}",
        "public_id": stem,
        "overwrite": "true",
        "timestamp": timestamp,
        "transformation": "c_limit,w_1200,q_auto,f_auto",
    }
    signature = sign_params(params)
    boundary = f"----CloudinaryBoundary{timestamp}"
    body = bytearray()

    def add_field(name: str, value: str) -> None:
        body.extend(f"--{boundary}\r\n".encode())
        body.extend(f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode())
        body.extend(f"{value}\r\n".encode())

    add_field("api_key", API_KEY)
    add_field("timestamp", timestamp)
    add_field("signature", signature)
    add_field("folder", params["folder"])
    add_field("public_id", params["public_id"])
    add_field("overwrite", params["overwrite"])
    add_field("transformation", params["transformation"])

    file_bytes = local_path.read_bytes()
    body.extend(f"--{boundary}\r\n".encode())
    body.extend(
        f'Content-Disposition: form-data; name="file"; filename="{local_path.name}"\r\n'.encode()
    )
    body.extend(b"Content-Type: application/octet-stream\r\n\r\n")
    body.extend(file_bytes)
    body.extend(b"\r\n")
    body.extend(f"--{boundary}--\r\n".encode())

    upload_url = f"https://api.cloudinary.com/v1_1/{CLOUD_NAME}/image/upload"
    request = urllib.request.Request(
        upload_url,
        data=bytes(body),
        headers={"Content-Type": f"multipart/form-data; boundary={boundary}"},
        method="POST",
    )

    with urllib.request.urlopen(request, timeout=120, context=SSL_CONTEXT) as response:
        payload = json.loads(response.read().decode("utf-8"))
    secure_url = payload.get("secure_url")
    if not secure_url:
        raise RuntimeError(f"Missing secure_url for {local_path.name}: {payload}")
    return web_path, secure_url


def upload_all(products: list[ProductSeed]) -> dict[str, str]:
    global CLOUD_NAME, API_KEY, API_SECRET, API_SECRET
    CLOUD_NAME, API_KEY, API_SECRET = load_cloudinary_settings()

    mapping: dict[str, str] = {}
    if MAP_PATH.exists():
        mapping = json.loads(MAP_PATH.read_text(encoding="utf-8"))

    pending_paths: list[tuple[Path, str]] = []
    for product in products:
        local = Path(str(ROOT / "wwwroot" / product.primary_local.lstrip("/")))
        pending_paths.append((local, product.category_folder))
        for gallery in product.gallery_local:
            pending_paths.append((Path(str(ROOT / "wwwroot" / gallery.lstrip("/"))), product.category_folder))

    pending = [(path, folder) for path, folder in pending_paths if f"/images/cards/{folder}/{path.name}" not in mapping]
    print(f"Uploading {len(pending)} images to Cloudinary...")

    with ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
        futures = {executor.submit(upload_file, path, folder): path for path, folder in pending}
        for index, future in enumerate(as_completed(futures), start=1):
            path = futures[future]
            web_path, secure_url = future.result()
            mapping[web_path] = secure_url
            print(f"[{index}/{len(pending)}] {web_path}")
            if index % 10 == 0:
                MAP_PATH.write_text(json.dumps(mapping, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    MAP_PATH.write_text(json.dumps(mapping, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return mapping


def resolve_url(local_path: str, mapping: dict[str, str]) -> str:
    return mapping.get(local_path, local_path)


def render_catalog_entries(products: list[ProductSeed], mapping: dict[str, str]) -> str:
    lines: list[str] = []
    for product in products:
        primary = resolve_url(product.primary_local, mapping)
        gallery = [resolve_url(item, mapping) for item in product.gallery_local]
        gallery_arg = ""
        if gallery:
            gallery_json = ", ".join(f'"{item}"' for item in gallery)
            gallery_arg = f",\n            galleryImages: [{gallery_json}]"

        lines.append(
            f"""        CreateEntry(
            "{product.category_name}",
            "{product.name.replace('"', '\\"')}",
            "{product.description.replace('"', '\\"')}",
            "{primary}",
            {int(product.starting_price)},
            "{product.grade_label.replace('"', '\\"')}",
            "{product.set_name.replace('"', '\\"')}",
            {product.year},
            "{product.language}",
            "{product.card_number}",
            endMinutes: {product.end_minutes}{gallery_arg}),"""
        )
    return "\n".join(lines)


def append_to_catalog(entry_block: str) -> None:
    text = CATALOG_PATH.read_text(encoding="utf-8")
    marker = "    public static IReadOnlyList<Entry> GetAuctionEntries() =>\n    ["
    if marker not in text:
        raise RuntimeError("Could not find GetAuctionEntries array.")

    insert_at = text.rfind("\n    ];")
    if insert_at < 0:
        raise RuntimeError("Could not find end of GetAuctionEntries array.")

    if "CGC 10 Venusaur #15 1999" in text:
        print("Pokemon/Sports entries already present in catalog; skipping append.")
        return

    updated = text[:insert_at] + "\n" + entry_block + text[insert_at:]
    summary = (
        "/// Sample auction catalog: One Piece (Nguyễn Hải), Yu-Gi-Oh!/Pokémon (Nguyễn Hà), Sports (Việt Anh)."
    )
    updated = updated.replace(
        "/// Sample auction catalog: graded One Piece (Nguyễn Hải) and Yu-Gi-Oh! (Nguyễn Hà).",
        summary,
    )
    updated = updated.replace(
        "/// Product images are hosted on Cloudinary under auction-house/seed/.",
        "/// Product images are hosted on Cloudinary under auction-house/seed/ (incl. Pokémon & Sports).",
    )
    CATALOG_PATH.write_text(updated, encoding="utf-8")


def main() -> None:
    products = collect_products()
    print(f"Prepared {len(products)} products from sample folders.")

    mapping = upload_all(products)
    entry_block = render_catalog_entries(products, mapping)
    append_to_catalog(entry_block)
    print(f"Appended {len(products)} entries to {CATALOG_PATH}")


if __name__ == "__main__":
    main()
