#!/usr/bin/env python3
"""Populate spriteSheet for HUD PNG metas (single sprite, full texture rect)."""
import re
import struct
import uuid
from pathlib import Path

HUD_ROOT = Path(__file__).resolve().parent.parent / "Assets/SpinSquad/Resources/UI/Hud"


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as f:
        if f.read(8) != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"Not a PNG: {path}")
        f.read(4)  # IHDR chunk length
        if f.read(4) != b"IHDR":
            raise ValueError(f"No IHDR: {path}")
        w, h = struct.unpack(">II", f.read(8))
    return w, h


def sprite_id() -> str:
    return uuid.uuid4().hex[:32]


def build_sprite_block(name: str, w: int, h: int) -> str:
    sid = sprite_id()
    iid = str(-(abs(hash(name)) % (2**63 - 1)))
    return f"""    sprites:
    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {w}
        height: {h}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {sid}
      internalID: {iid}
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      {name}: {iid}"""


def fix_meta(png: Path) -> bool:
    meta = Path(str(png) + ".meta")
    if not meta.is_file():
        return False
    w, h = png_size(png)
    name = png.stem
    text = meta.read_text(encoding="utf-8")
    block = "  spriteSheet:\n    serializedVersion: 2\n" + build_sprite_block(name, w, h)
    new_text, n = re.subn(
        r"  spriteSheet:\n    serializedVersion: 2\n.*?nameFileIdTable:\n(?:      [^\n]+\n)*",
        block + "\n",
        text,
        count=1,
        flags=re.DOTALL,
    )
    if n == 0:
        return False
    meta.write_text(new_text, encoding="utf-8")
    return True


def main():
    n = 0
    for png in sorted(HUD_ROOT.rglob("*.PNG")):
        if fix_meta(png):
            n += 1
            print("fixed", png.relative_to(HUD_ROOT))
    print(f"Done. Updated {n} meta files.")


if __name__ == "__main__":
    main()
