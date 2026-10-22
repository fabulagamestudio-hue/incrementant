"""Gera Art/Terrain/GrassOverhang.png: a grama que transborda do bloco de grama para a célula vazia de cima.

Formato: 128 x 16 texels RGBA (8 células de largura, 1 de altura), linha 0 do PNG em cima. Repete na horizontal sem
emenda. O shader TerrainTiles desenha, numa célula vazia com grama embaixo, o texel em (x do mundo mod 128, altura dentro
da célula); alpha 0 = nada. Só cores da paleta mestra. A arte é só desenho: a célula de cima continua vazia para a lógica.

Direção: uma franja baixa e quase contínua quebra a linha reta do topo do bloco; por cima dela, touceiras de 2 a 5 folhas
em leque, mais altas onde o ruído de densidade sobe e quase ausentes nos vãos, para o topo não virar um pente regular.
Luz de cima à esquerda, como o atlas: folha clara no lado esquerdo e na ponta, base escura.
Uso: python -I grass_overhang.py <MasterPalette.hex> <saída.png> [prévia.png] [TerrainAtlas.png]
"""
import sys
import numpy as np
from PIL import Image

W, H = 128, 16
GRASS_ROW = 2  # linha da grama no atlas (CellMaterial.Grass)
# Índices da paleta mestra (Art/Palette/README.md): verdes 21-28.
BASE, DARK, MID, LIGHT, TIP, SHINE = 22, 23, 24, 25, 26, 27


def load_palette(path):
    cols = [l.strip() for l in open(path) if l.strip()]
    return np.array([[int(c[i:i + 2], 16) for i in (0, 2, 4)] for c in cols], np.uint8)


def periodic_noise(seed, periods=(2, 4, 8, 16), weights=(0.45, 0.3, 0.15, 0.1)):
    """Ruído 1D periódico em W, em [0, 1]."""
    r = np.random.default_rng(seed)
    x = np.arange(W) + 0.5
    h = np.zeros(W)
    for p, w in zip(periods, weights):
        g = r.random(p)
        t = x * p / W - 0.5
        i0 = np.floor(t).astype(int)
        f = t - i0
        f = f * f * (3 - 2 * f)
        h += w * (g[i0 % p] * (1 - f) + g[(i0 + 1) % p] * f)
    return (h - h.min()) / (h.max() - h.min())


def build(seed=11):
    """Matriz H x W de índices da paleta (-1 = transparente), y = 0 na base (encostada no bloco)."""
    r = np.random.default_rng(seed)
    out = np.full((H, W), -1, int)
    density = periodic_noise(seed)

    def put(x, y, c, over=True):
        if 0 <= y < H:
            x %= W
            if over or out[y, x] < 0:
                out[y, x] = c

    # Franja: perfil contínuo de 1 a 4 texels que sobe e desce em ondas curtas (moitas baixas), sem linha escura na base:
    # a base usa o tom médio do bloco para emendar com ele, e o topo da onda clareia (luz de cima).
    # Perfil de moitas: arcos de 3 a 8 texels de raio somados por máximo; entre elas o topo do bloco aparece em vãos curtos.
    profile = np.zeros(W)
    x = 0.0
    while x < W:
        radius = r.uniform(3, 8)
        peak = 2.0 + 4.0 * density[int(x) % W] + r.uniform(-0.5, 1.0)
        for dx in range(-int(radius), int(radius) + 1):
            k = 1 - (dx / radius) ** 2
            profile[(int(x) + dx) % W] = max(profile[(int(x) + dx) % W], peak * np.sqrt(max(k, 0)))
        x += radius * r.uniform(1.4, 2.4)
    fringe = np.clip(np.round(profile + r.normal(0, 0.3, W)), 0, 6).astype(int)
    for x in range(W):
        for y in range(fringe[x]):
            top = y == fringe[x] - 1
            put(x, y, LIGHT if top and fringe[x] > 1 else MID, over=False)
        # Sombra do lado direito da moita (luz de cima à esquerda): onde a vizinha da direita é mais baixa.
        if fringe[(x + 1) % W] < fringe[x]:
            put(x, fringe[x] - 1, MID)

    # Touceiras: posições sorteadas pela densidade, sem encostar demais umas nas outras.
    tufts = []
    for _ in range(600):
        if len(tufts) >= 22:
            break
        x = int(r.integers(W))
        if r.random() > 0.1 + 0.9 * density[x]:
            continue
        if any(min(abs(x - t), W - abs(x - t)) < 4 for t in tufts):
            continue
        tufts.append(x)

    for x0 in tufts:
        tall = 6 + density[x0] * 8 + r.normal(0, 0.8)
        blades = int(np.clip(round(3 + density[x0] * 2 + r.normal(0, 0.5)), 3, 5))
        leans = np.linspace(-1, 1, blades) + r.normal(0, 0.2, blades)
        order = np.argsort(-np.abs(leans))  # as do meio (mais altas) por último, na frente
        for k in order:
            lean = leans[k]
            height = int(np.clip(round(tall * (1 - 0.3 * abs(lean)) + r.normal(0, 0.7)), 4, H - 1))
            spread = 1.0 + 3.0 * abs(lean)
            start = x0 + int(round(0.8 * lean))
            prev = start
            for y in range(height):
                t = (y + 1) / height
                x = start + int(round(np.sign(lean) * spread * t * t))
                c = MID if t < 0.45 else (LIGHT if t < 0.85 else TIP)
                # Folha contínua: preenche o salto lateral entre uma linha e a outra.
                for fx in range(min(prev, x), max(prev, x) + 1):
                    put(fx, y, c)
                # Metade de baixo com 2 texels: o lado da sombra (direita) no tom escuro.
                if t < 0.4:
                    put(x + 1, y, DARK if y >= fringe[(x + 1) % W] else MID, over=False)
                prev = x
            if height >= 9:
                put(prev, height - 1, SHINE)
    return out


def to_rgba(pal, idx):
    rgba = np.zeros((H, W, 4), np.uint8)
    mask = idx >= 0
    rgba[mask, :3] = pal[idx[mask]]
    rgba[mask, 3] = 255
    return rgba[::-1]  # y = 0 embaixo -> linha 0 do PNG em cima


def preview(pal, rgba, atlas_path, path, scale=5):
    """Duas réguas de grama (uma 1 célula mais alta, para ver o degrau) sobre o céu, com o bloco do atlas embaixo."""
    sky = np.array([122, 145, 120], np.uint8)
    cells_w = 24
    canvas = np.tile(sky, (4 * H, cells_w * 16, 1))
    atlas = np.array(Image.open(atlas_path).convert('RGB')) if atlas_path else None
    over = rgba.astype(float)

    def block(x0, y_top):
        for x in range(x0, x0 + 16):
            for y in range(16):
                if atlas is not None:
                    ay = atlas.shape[0] - (GRASS_ROW * 128 + y) - 1
                    canvas[y_top + 15 - y, x] = atlas[ay, x % 128]
                else:
                    canvas[y_top + 15 - y, x] = pal[MID]

    for cx in range(cells_w):
        top_cells = 2 if 9 <= cx < 15 else 1  # degrau no meio
        for k in range(top_cells):
            block(cx * 16, 4 * H - 16 * (k + 1))
        y0 = 4 * H - 16 * (top_cells + 1)
        for x in range(cx * 16, cx * 16 + 16):
            for y in range(16):
                a = over[y, x % W, 3] / 255
                if a > 0:
                    canvas[y0 + y, x] = over[y, x % W, :3]
    img = Image.fromarray(canvas)
    img.resize((img.width * scale, img.height * scale), Image.NEAREST).save(path)


if __name__ == '__main__':
    pal = load_palette(sys.argv[1])
    rgba = to_rgba(pal, build())
    Image.fromarray(rgba, 'RGBA').save(sys.argv[2])
    if len(sys.argv) > 3:
        preview(pal, rgba, sys.argv[4] if len(sys.argv) > 4 else None, sys.argv[3])
