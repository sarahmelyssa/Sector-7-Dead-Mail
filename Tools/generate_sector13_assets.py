import os
import random
import struct
import zlib


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sector13")


def chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_png(path, width, height, pixels):
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            raw.extend(pixels[y][x])

    data = b"\x89PNG\r\n\x1a\n"
    data += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    data += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    data += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(data)


def clamp(value):
    return max(0, min(255, int(value)))


def rgba(color):
    return tuple(clamp(c) for c in color)


def noise_color(base, spread):
    return rgba((base[0] + random.randint(-spread, spread), base[1] + random.randint(-spread, spread), base[2] + random.randint(-spread, spread), 255))


def meta_guid(name):
    random.seed(name)
    return "".join(random.choice("0123456789abcdef") for _ in range(32))


def write_meta(path, name):
    with open(path + ".meta", "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {meta_guid(name)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 11
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
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
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
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")


def write_folder_meta(path, name):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {meta_guid(name)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")


def texture(name, width, height, painter):
    random.seed(name)
    pixels = []
    for y in range(height):
        row = []
        for x in range(width):
            row.append(rgba(painter(x, y, width, height)))
        pixels.append(row)

    path = os.path.join(OUT, name + ".png")
    write_png(path, width, height, pixels)
    write_meta(path, name)


def vignette(x, y, w, h):
    dx = abs(x / max(1, w - 1) - 0.5) * 2
    dy = abs(y / max(1, h - 1) - 0.5) * 2
    return 1.0 - min(0.55, (dx * dx + dy * dy) * 0.22)


def wall(x, y, w, h):
    base = [31, 34, 32]
    if x % 64 in (0, 1) or y % 64 in (0, 1):
        base = [18, 20, 20]
    if random.random() < 0.025:
        base = [72, 42, 31]
    if random.random() < 0.015:
        base = [83, 70, 42]
    v = vignette(x, y, w, h)
    return (base[0] * v + random.randint(-12, 10), base[1] * v + random.randint(-12, 10), base[2] * v + random.randint(-12, 10), 255)


def desk(x, y, w, h):
    base = [38, 42, 39]
    if y % 52 < 3:
        base = [22, 24, 23]
    if random.random() < 0.04:
        base = [82, 47, 36]
    return noise_color(base, 14)


def floor(x, y, w, h):
    tile = 64
    base = [39, 42, 36]
    if x % tile < 2 or y % tile < 2:
        base = [17, 19, 18]
    if random.random() < 0.02:
        base = [76, 49, 33]
    return noise_color(base, 12)


def conveyor(x, y, w, h):
    stripe = ((x + y * 2) // 18) % 2
    base = [18, 19, 20] if stripe == 0 else [30, 31, 31]
    return noise_color(base, 5)


def paper(x, y, w, h):
    edge = min(x, y, w - 1 - x, h - 1 - y)
    base = [174, 159, 128] if edge > 10 else [116, 96, 78]
    if random.random() < 0.018:
        base = [89, 128, 113]
    if random.random() < 0.025:
        base = [95, 71, 62]
    return noise_color(base, 9)


def label_green(x, y, w, h):
    edge = min(x, y, w - 1 - x, h - 1 - y)
    base = [93, 168, 91] if edge > 5 else [31, 78, 42]
    if x % 17 < 4 and 10 < y < h - 10:
        base = [24, 42, 26]
    return noise_color(base, 7)


def cardboard(x, y, w, h):
    grain = int(18 * ((x % 23) / 23.0)) + int(10 * ((y % 41) / 41.0))
    base = [128 + grain, 86 + grain // 2, 48 + grain // 4]
    if random.random() < 0.035:
        base = [88, 56, 35]
    if random.random() < 0.018:
        base = [166, 118, 66]
    return noise_color(base, 10)


def button(color):
    def paint(x, y, w, h):
        dx = abs(x / max(1, w - 1) - 0.5) * 2
        dy = abs(y / max(1, h - 1) - 0.5) * 2
        edge = max(dx, dy)
        shade = 1.15 - edge * 0.55
        if x < 8 or y < 8 or x > w - 9 or y > h - 9:
            shade *= 0.45
        if 30 < x < w - 30 and 30 < y < h - 30:
            shade += 0.10
        return (color[0] * shade + random.randint(-8, 8), color[1] * shade + random.randint(-8, 8), color[2] * shade + random.randint(-8, 8), 255)
    return paint


def stripes(x, y, w, h):
    if ((x + y) // 24) % 2 == 0:
        base = [142, 116, 40]
    else:
        base = [31, 31, 26]
    return noise_color(base, 7)


def monitor(x, y, w, h):
    scan = 16 if y % 8 < 2 else 0
    base = [24 + scan, 27 + scan, 18]
    if random.random() < 0.01:
        base = [110, 106, 63]
    return noise_color(base, 5)


def main():
    os.makedirs(OUT, exist_ok=True)
    write_folder_meta(os.path.join(ROOT, "Assets", "Resources"), "Resources")
    write_folder_meta(OUT, "Sector13")
    texture("wall_grime", 256, 256, wall)
    texture("desk_metal", 256, 256, desk)
    texture("floor_tiles", 256, 256, floor)
    texture("conveyor_belt", 256, 256, conveyor)
    texture("paper_manifest", 512, 768, paper)
    texture("package_cardboard", 256, 256, cardboard)
    texture("package_label_green", 256, 128, label_green)
    texture("button_red", 128, 128, button([154, 39, 28]))
    texture("button_green", 128, 128, button([30, 142, 54]))
    texture("button_purple", 128, 128, button([97, 95, 169]))
    texture("hazard_stripes", 256, 256, stripes)
    texture("monitor_scanlines", 256, 128, monitor)
    print("Generated Sector13 assets in", OUT)


if __name__ == "__main__":
    main()
