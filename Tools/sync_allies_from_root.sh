#!/usr/bin/env bash
# Đồng bộ allies/*_spine/ (gốc repo) -> Assets/SpinSquad/Data/Spine/Allies/{Blue,Green,Red,White}/
# + copy projectile Thủy -> Resources/Vfx/AllyThuyProjectile.png
# Yêu cầu: chạy từ gốc repo (SpinSquadClone/). Sau đó mở Unity để Reimport Spine.

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ALLIES="$ROOT/allies"
DEST="$ROOT/Assets/SpinSquad/Data/Spine/Allies"
VFX="$ROOT/Assets/SpinSquad/Resources/Vfx"

die() { echo "sync_allies: $*" >&2; exit 1; }

[[ -d "$ALLIES" ]] || die "Thiếu thư mục $ALLIES"
[[ -d "$DEST" ]] || die "Thiếu $DEST"

sync_one() {
  local src_name="$1" dest_color="$2"
  local src="$ALLIES/$src_name"
  local dst="$DEST/$dest_color"
  [[ -d "$src" ]] || die "Thiếu nguồn $src"
  [[ -d "$dst" ]] || die "Thiếu đích $dst"
  for f in skeleton.json skeleton.atlas.txt skeleton.png; do
    [[ -f "$src/$f" ]] || die "Thiếu $src/$f"
    cp -f "$src/$f" "$dst/$f"
    echo "  copied $src_name/$f -> $dest_color/"
  done
}

echo "[sync_allies] ROOT=$ROOT"
sync_one "blue_spine" "Blue"
sync_one "green_spine" "Green"
sync_one "red_spine" "Red"
sync_one "white_spine" "White"

if [[ -f "$ALLIES/blue_spine/projectile.PNG" ]]; then
  mkdir -p "$VFX"
  cp -f "$ALLIES/blue_spine/projectile.PNG" "$VFX/AllyThuyProjectile.png"
  echo "  copied blue_spine/projectile.PNG -> Resources/Vfx/AllyThuyProjectile.png"
fi

echo "[sync_allies] Chuẩn hóa skeleton.json images -> ./ (Spine atlas cùng thư mục)"
export SYNC_ALLIES_ROOT="$ROOT"
python3 << 'PY'
import re, os, pathlib
root = pathlib.Path(os.environ["SYNC_ALLIES_ROOT"])
dest = root / "Assets" / "SpinSquad" / "Data" / "Spine" / "Allies"
for color in ("Blue", "Green", "Red", "White"):
    p = dest / color / "skeleton.json"
    if not p.is_file():
        continue
    text = p.read_text(encoding="utf-8")
    new, n = re.subn(r'"images"\s*:\s*"[^"]*"', '"images":"./"', text, count=1)
    if n:
        p.write_text(new, encoding="utf-8")
        print(f"  normalized images in {color}/skeleton.json")
    else:
        print(f"  warn: no images field match in {color}/skeleton.json")
PY

echo "[sync_allies] Xong. Mở Unity (hoặc AssetDatabase.Refresh) để Spine reimport."
