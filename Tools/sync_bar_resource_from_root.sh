#!/usr/bin/env bash
# bar_and_resource/*.PNG -> Assets/SpinSquad/Resources/UI/Meta/Resource/
# Bỏ Diamond.PNG (không có currency diamond trong save).

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/bar_and_resource"
DEST="$ROOT/Assets/SpinSquad/Resources/UI/Meta/Resource"

die() { echo "sync_bar_resource: $*" >&2; exit 1; }

[[ -d "$SRC" ]] || die "Thiếu $SRC"
mkdir -p "$DEST"

copy_lower() {
  local from="$1" to="$2"
  [[ -f "$SRC/$from" ]] || die "Thiếu $SRC/$from"
  cp -f "$SRC/$from" "$DEST/$to"
  echo "  $from -> Resource/$to"
}

echo "[sync_bar_resource] ROOT=$ROOT"
copy_lower "Coin.PNG" "coin.png"
copy_lower "Coin_bar.PNG" "coin_bar.png"
copy_lower "Key.PNG" "key.png"
copy_lower "Key_bar.PNG" "key_bar.png"
copy_lower "Energy.PNG" "energy.png"
copy_lower "Energy_bar.PNG" "energy_bar.png"
echo "[sync_bar_resource] Skip Diamond.PNG (không dùng trong meta save)"
python3 "$ROOT/Tools/fix_resource_sprite_metas.py"
echo "[sync_bar_resource] Xong. Reimport UI/Meta/Resource trong Unity nếu cần."
