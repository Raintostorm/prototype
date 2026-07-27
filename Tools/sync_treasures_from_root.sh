#!/usr/bin/env bash
# Đồng bộ treasures/*.PNG (gốc repo) -> Assets/SpinSquad/Resources/UI/Treasure/Icons/
# Chạy từ gốc repo. Sau đó: python3 Tools/fix_treasure_sprite_metas.py && Reimport trong Unity.

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/treasures"
DEST="$ROOT/Assets/SpinSquad/Resources/UI/Treasure/Icons"

die() { echo "sync_treasures: $*" >&2; exit 1; }

[[ -d "$SRC" ]] || die "Thiếu thư mục $SRC"
mkdir -p "$DEST"

copy_lower() {
  local from="$1" to="$2"
  [[ -f "$SRC/$from" ]] || die "Thiếu $SRC/$from"
  cp -f "$SRC/$from" "$DEST/$to"
  echo "  $from -> Icons/$to"
}

echo "[sync_treasures] ROOT=$ROOT"
for i in $(seq 1 10); do copy_lower "C_${i}.PNG" "c_${i}.png"; done
for i in $(seq 1 7); do copy_lower "R_${i}.PNG" "r_${i}.png"; done
for i in $(seq 1 4); do copy_lower "E_${i}.PNG" "e_${i}.png"; done
for i in $(seq 1 4); do copy_lower "L_${i}.PNG" "l_${i}.png"; done
copy_lower "Table_1.PNG" "table_1.png"
for i in $(seq 1 3); do copy_lower "Type_${i}.PNG" "type_${i}.png"; done

COMBAT="$ROOT/Assets/SpinSquad/Resources/UI/Hud/Combat"
mkdir -p "$COMBAT"
for pair in "Continue.PNG:continue.PNG" "Pause.PNG:pause.PNG"; do
  from="${pair%%:*}"
  to="${pair##*:}"
  cp -f "$SRC/$from" "$COMBAT/$to"
  echo "  $from -> Hud/Combat/$to"
done

python3 "$ROOT/Tools/fix_treasure_sprite_metas.py"
python3 "$ROOT/Tools/gen_sprite_metas.py" 2>/dev/null || true
python3 "$ROOT/Tools/fix_hud_sprite_metas.py"
echo "[sync_treasures] Xong. Reimport Treasure/Icons + Hud/Combat nếu cần."
