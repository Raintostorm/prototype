#!/usr/bin/env bash
# backgroundpng/ (ưu tiên) hoặc background/ -> Assets/SpinSquad/Resources/UI/Battle/
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$ROOT/Assets/SpinSquad/Resources/UI/Battle"
HUD_COMBAT="$ROOT/Assets/SpinSquad/Resources/UI/Hud/Combat/Background_combat.PNG"
HUD_META="$ROOT/Assets/SpinSquad/Resources/UI/Hud/Meta/Background.PNG"

die() { echo "sync_background: $*" >&2; exit 1; }

pick_src() {
  if [[ -d "$ROOT/backgroundpng" ]]; then
    echo "$ROOT/backgroundpng"
  elif [[ -d "$ROOT/background" ]]; then
    echo "$ROOT/background"
  else
    die "Thiếu thư mục backgroundpng/ hoặc background/"
  fi
}

SRC="$(pick_src)"
mkdir -p "$DEST"

copy_lower() {
  local from="$1" to="$2"
  [[ -f "$SRC/$from" ]] || return 1
  cp -f "$SRC/$from" "$DEST/$to"
  echo "  $from -> Battle/$to"
  return 0
}

copy_hud() {
  local from="$1" to="$2"
  [[ -f "$from" ]] || return 1
  cp -f "$from" "$DEST/$to"
  echo "  $(basename "$from") (HUD) -> Battle/$to"
  return 0
}

echo "[sync_background] SRC=$SRC"

for n in 1 2 3 4 5 6 7 8 9 10; do
  upper="Background_battle_${n}.PNG"
  lower="background_battle_${n}.png"
  copy_lower "$upper" "$lower" || copy_lower "Background_battle_${n}.png" "$lower" || true
done

# Fallback in-battle theo level khi chưa có art riêng trong backgroundpng/
[[ -f "$DEST/background_battle_2.png" ]] || copy_hud "$HUD_COMBAT" "background_battle_2.png" || true
[[ -f "$DEST/background_battle_3.png" ]] || copy_hud "$HUD_META" "background_battle_3.png" || true
[[ -f "$DEST/background_battle_4.png" ]] || copy_hud "$HUD_COMBAT" "background_battle_4.png" || true

copy_lower "Place_holder.PNG" "place_holder.png" || die "Thiếu Place_holder.PNG"
copy_lower "Roll.PNG" "roll.png" || die "Thiếu Roll.PNG"

echo "[sync_background] Prep=background_battle_1 | Combat L1..3 = background_battle_1..3"
python3 "$ROOT/Tools/fix_battle_sprite_metas.py"
echo "[sync_background] Xong. Thay HUD fallback bằng Background_battle_2/3/4.PNG trong backgroundpng/ khi có art."
