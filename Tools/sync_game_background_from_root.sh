#!/usr/bin/env bash
# game_background/ -> Assets/SpinSquad/Resources/UI/Backgrounds/
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/game_background"
DEST="$ROOT/Assets/SpinSquad/Resources/UI/Backgrounds"

die() { echo "sync_game_background: $*" >&2; exit 1; }

[[ -d "$SRC" ]] || die "Thiếu thư mục game_background/"

mkdir -p "$DEST"

copy_one() {
  local from="$1" to="$2"
  [[ -f "$SRC/$from" ]] || die "Thiếu $SRC/$from"
  cp -f "$SRC/$from" "$DEST/$to"
  echo "  $from -> Backgrounds/$to"
}

echo "[sync_game_background] SRC=$SRC"
copy_one "Home.PNG" "home.png"
copy_one "Pre_battle.PNG" "pre_battle.png"
copy_one "Inventory.PNG" "inventory.png"
copy_one "Treasure.PNG" "treasure.png"

python3 "$ROOT/Tools/fix_battle_sprite_metas.py" "$DEST"
echo "[sync_game_background] Xong. Prep duel=pre_battle | Home/Treasure/Inventory cho meta UI."
