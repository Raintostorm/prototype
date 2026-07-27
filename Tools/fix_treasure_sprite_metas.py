#!/usr/bin/env python3
"""Generate + fix sprite metas for Resources/UI/Treasure/Icons (single full-rect sprite)."""
import re
import struct
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ICONS = ROOT / "Assets/SpinSquad/Resources/UI/Treasure/Icons"

TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as f:
        if f.read(8) != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"Not a PNG: {path}")
        f.read(4)
        if f.read(4) != b"IHDR":
            raise ValueError(f"No IHDR: {path}")
        return struct.unpack(">II", f.read(8))


def build_sprite_block(name: str, w: int, h: int) -> str:
    sid = uuid.uuid4().hex[:32]
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


def ensure_meta(png: Path) -> None:
    meta = Path(str(png) + ".meta")
    if not meta.is_file():
        meta.write_text(TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8")
        print("created meta:", png.name)


def fix_meta(png: Path) -> bool:
    ensure_meta(png)
    meta = Path(str(png) + ".meta")
    w, h = png_size(png)
    name = png.stem
    text = meta.read_text(encoding="utf-8")
    block = "  spriteSheet:\n    serializedVersion: 2\n" + build_sprite_block(name, w, h)
    patterns = [
        r"  spriteSheet:\n    serializedVersion: 2\n.*?nameFileIdTable:\n(?:      [^\n]+\n)*",
        r"  spriteSheet:\n    serializedVersion: 2\n.*?nameFileIdTable: \{\}\n",
    ]
    new_text = text
    n = 0
    for pat in patterns:
        new_text, n = re.subn(pat, block + "\n", new_text, count=1, flags=re.DOTALL)
        if n:
            break
    if n == 0:
        return False
    meta.write_text(new_text, encoding="utf-8")
    return True


def main():
    if not ICONS.is_dir():
        print("No Icons folder yet — run sync_treasures_from_root.sh first.")
        return
    n = 0
    for png in sorted(ICONS.glob("*.png")):
        if fix_meta(png):
            n += 1
            print("fixed", png.name)
    print(f"Done. Updated {n} treasure icon metas.")


if __name__ == "__main__":
    main()
