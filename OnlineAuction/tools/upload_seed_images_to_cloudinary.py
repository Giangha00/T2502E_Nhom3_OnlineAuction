#!/usr/bin/env python3
"""Upload wwwroot seed card images to Cloudinary and rewrite SpreadsheetAuctionCatalog.cs."""

from __future__ import annotations

import hashlib
import json
import re
import ssl
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

import urllib.error
import urllib.request

# macOS Python builds often miss system CA bundle; Cloudinary HTTPS still needs a context.
SSL_CONTEXT = ssl._create_unverified_context()

ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "wwwroot" / "images" / "cards"
CATALOG_PATH = ROOT / "Data" / "SpreadsheetAuctionCatalog.cs"
MAP_PATH = ROOT / "tools" / "cloudinary_seed_image_map.json"
APPSETTINGS_PATH = ROOT / "appsettings.json"
LOCAL_SETTINGS_PATH = ROOT / "appsettings.Local.json"

FOLDER_PREFIX = "auction-house/seed"
MAX_WORKERS = 4


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
        raise RuntimeError("CloudinarySettings missing CloudName/ApiKey/ApiSecret in appsettings.")
    return cloud_name, api_key, api_secret


CLOUD_NAME, API_KEY, API_SECRET = load_cloudinary_settings()
UPLOAD_URL = f"https://api.cloudinary.com/v1_1/{CLOUD_NAME}/image/upload"


def sign_params(params: dict[str, str]) -> str:
    to_sign = "&".join(f"{k}={v}" for k, v in sorted(params.items()) if v is not None and v != "")
    return hashlib.sha1((to_sign + API_SECRET).encode("utf-8")).hexdigest()


def upload_file(local_path: Path, category_folder: str) -> tuple[str, str]:
    """Returns (web_path, secure_url)."""
    stem = local_path.stem
    public_id = f"{FOLDER_PREFIX}/{category_folder}/{stem}"
    web_path = f"/images/cards/{category_folder}/{local_path.name}"

    timestamp = str(int(time.time()))
    params = {
        "folder": f"{FOLDER_PREFIX}/{category_folder}",
        "public_id": stem,
        "overwrite": "true",
        "timestamp": timestamp,
        # Keep full slab visible; avoid aggressive crop used by listing uploads.
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

    request = urllib.request.Request(
        UPLOAD_URL,
        data=bytes(body),
        headers={"Content-Type": f"multipart/form-data; boundary={boundary}"},
        method="POST",
    )

    for attempt in range(3):
        try:
            with urllib.request.urlopen(request, timeout=120, context=SSL_CONTEXT) as response:
                payload = json.loads(response.read().decode("utf-8"))
            secure_url = payload.get("secure_url")
            if not secure_url:
                raise RuntimeError(f"Missing secure_url for {local_path.name}: {payload}")
            return web_path, secure_url
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            if attempt == 2:
                raise RuntimeError(f"Upload failed for {local_path}: {exc.code} {detail}") from exc
            time.sleep(1.5 * (attempt + 1))
        except Exception:
            if attempt == 2:
                raise
            time.sleep(1.5 * (attempt + 1))

    raise RuntimeError(f"Upload failed for {local_path}")


def collect_files() -> list[tuple[Path, str]]:
    files: list[tuple[Path, str]] = []
    for category in ("one-piece", "yu-gi-oh"):
        folder = CARDS_ROOT / category
        if not folder.exists():
            continue
        for path in sorted(folder.iterdir()):
            if path.suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"}:
                files.append((path, category))
    return files


def rewrite_catalog(mapping: dict[str, str]) -> int:
    text = CATALOG_PATH.read_text(encoding="utf-8")
    replacements = 0
    updated = text

    # Longest paths first to avoid partial overlaps.
    for local, cloud in sorted(mapping.items(), key=lambda item: len(item[0]), reverse=True):
        count = updated.count(local)
        if count:
            updated = updated.replace(local, cloud)
            replacements += count

    CATALOG_PATH.write_text(updated, encoding="utf-8")
    return replacements


def main() -> None:
    files = collect_files()
    print(f"Found {len(files)} images under {CARDS_ROOT}")

    mapping: dict[str, str] = {}
    if MAP_PATH.exists():
        mapping = json.loads(MAP_PATH.read_text(encoding="utf-8"))
        print(f"Loaded existing map with {len(mapping)} entries")

    pending = [(path, category) for path, category in files
               if f"/images/cards/{category}/{path.name}" not in mapping]

    print(f"Uploading {len(pending)} new/missing images...")
    failures: list[str] = []

    with ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
        futures = {
            executor.submit(upload_file, path, category): (path, category)
            for path, category in pending
        }
        done = 0
        for future in as_completed(futures):
            path, category = futures[future]
            done += 1
            try:
                web_path, secure_url = future.result()
                mapping[web_path] = secure_url
                print(f"[{done}/{len(pending)}] OK {web_path}")
            except Exception as exc:  # noqa: BLE001
                failures.append(f"{path}: {exc}")
                print(f"[{done}/{len(pending)}] FAIL {path.name}: {exc}")

            if done % 10 == 0:
                MAP_PATH.write_text(json.dumps(mapping, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    MAP_PATH.write_text(json.dumps(mapping, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Saved map -> {MAP_PATH} ({len(mapping)} entries)")

    if failures:
        print(f"\n{len(failures)} upload failures:")
        for item in failures:
            print(f"  - {item}")
        raise SystemExit(1)

    replaced = rewrite_catalog(mapping)
    remaining = len(re.findall(r"/images/cards/(?:one-piece|yu-gi-oh)/", CATALOG_PATH.read_text(encoding="utf-8")))
    print(f"Catalog updated. Replacements applied: {replaced}. Remaining local paths: {remaining}")


if __name__ == "__main__":
    main()
