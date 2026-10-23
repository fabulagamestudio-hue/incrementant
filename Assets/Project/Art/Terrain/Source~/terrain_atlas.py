"""Gera Art/Terrain/TerrainAtlas.png (etapa 5 do documento 40): atlas RGB do terreno só com as cores da paleta mestra.

Formato (CellTileArt / AntFarmGuide, "Atlas RGB"): 128 x 128 texels por linha, uma linha por CellMaterial na ordem do
enum, a parede de fundo e, por último, bloco e fundo de cada sala construída (RoomStyle, REQ-058), empilhadas de baixo
para cima (linha 0 embaixo no PNG). Cada tile repete sem emenda (ruído e Voronoi periódicos em 128). Luz de cima à
esquerda: realce no alto/esquerda das formas, sombra embaixo/direita.
Uso: python -I terrain_atlas.py <MasterPalette.hex> <saída.png> [prévia.png] [salas.png]
"""
import sys
import numpy as np
from PIL import Image, ImageDraw

N = 128
CELL = 16  # texels por célula: os blocos de sala alinham as juntas a esta grade
ROOM_STYLES = ['Queen', 'Nutrient', 'Nursery', 'Waste', 'Dew', 'Farm', 'Store', 'Smelter', 'Forge', 'Armory', 'Wardrobe']  # ordem de RoomStyle
ROWS = 37 + 2 * len(ROOM_STYLES)  # 36 materiais + parede de fundo + bloco e fundo por sala


def load_palette(path):
    cols = [l.strip() for l in open(path) if l.strip()]
    return np.array([[int(c[i:i + 2], 16) for i in (0, 2, 4)] for c in cols], np.uint8)


def rng(seed):
    return np.random.default_rng(seed)


def value_noise(period, seed):
    """Ruído de valor periódico em N, nós a cada N/period texels, interpolação suave, em [0, 1]."""
    g = rng(seed).random((period, period))
    t = (np.arange(N) + 0.5) * period / N - 0.5
    i0 = np.floor(t).astype(int)
    f = t - i0
    f = f * f * (3 - 2 * f)
    i0 %= period
    i1 = (i0 + 1) % period
    a = g[np.ix_(i0, i0)]
    b = g[np.ix_(i0, i1)]
    c = g[np.ix_(i1, i0)]
    d = g[np.ix_(i1, i1)]
    fy = f[:, None]
    fx = f[None, :]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(seed, periods=(4, 8, 16, 32), weights=(0.5, 0.25, 0.15, 0.1)):
    h = sum(w * value_noise(p, seed + k) for k, (p, w) in enumerate(zip(periods, weights)))
    return (h - h.min()) / (h.max() - h.min())


def voronoi(per_side, seed, jitter=0.8):
    """Voronoi periódico: índice da semente mais próxima e diferença de distância para a segunda (0 na borda)."""
    r = rng(seed)
    cell = N / per_side
    sites = []
    for gy in range(per_side):
        for gx in range(per_side):
            sites.append(((gx + 0.5 + jitter * (r.random() - 0.5)) * cell,
                          (gy + 0.5 + jitter * (r.random() - 0.5)) * cell))
    yy, xx = np.mgrid[0:N, 0:N] + 0.5
    best = np.full((N, N), 1e9)
    second = np.full((N, N), 1e9)
    idx = np.zeros((N, N), int)
    for k, (sx, sy) in enumerate(sites):
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                d = np.hypot(xx - sx - ox, yy - sy - oy)
                closer = d < best
                second = np.where(closer, best, np.minimum(second, d))
                idx = np.where(closer, k, idx)
                best = np.where(closer, d, best)
    return idx, second - best


def roll(a, dx, dy):
    """Texel em (x - dx, y - dy): roll(a, 1, 1) traz a vizinha de cima/esquerda (y para baixo na imagem)."""
    return np.roll(np.roll(a, dy, axis=0), dx, axis=1)


def light_edges(h):
    """+1 onde a forma encara a luz (a vizinha de cima/esquerda é mais baixa), -1 na sombra (a de baixo/direita é mais baixa)."""
    h = h.astype(float)
    lit = h > roll(h, 1, 1)
    shade = h > roll(h, -1, -1)
    out = np.zeros(h.shape, int)
    out[shade] = -1
    out[lit] = 1
    return out


def majority(levels, passes=1):
    """Tira texels isolados: o texel sem nenhuma vizinha igual vira o valor mais comum em volta."""
    for _ in range(passes):
        stack = np.stack([roll(levels, dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)])
        same = (stack == levels[None]).sum(0)
        vals = np.unique(levels)
        counts = np.stack([(stack == v).sum(0) for v in vals])
        mode = vals[counts.argmax(0)]
        levels = np.where(same <= 1, mode, levels)
    return levels


def blobs(seed, count, rmin, rmax):
    """Pedrinhas: elipses pequenas periódicas. Máscara booleana."""
    r = rng(seed)
    mask = np.zeros((N, N), bool)
    yy, xx = np.mgrid[0:N, 0:N] + 0.5
    for _ in range(count):
        cx, cy = r.random() * N, r.random() * N
        rx = r.uniform(rmin, rmax)
        ry = rx * r.uniform(0.6, 1.0)
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                mask |= ((xx - cx - ox) / rx) ** 2 + ((yy - cy - oy) / ry) ** 2 <= 1
    return mask


def specks(seed, chance):
    return rng(seed).random((N, N)) < chance


# ---------------------------------------------------------------- padrões (valores = índices da paleta mestra)

def earth(seed, tones, pebble_tones=None, pebbles=0, speck=None, smooth=2, thresholds=(0.38, 0.66)):
    """Terra: manchas em 3 tons (fbm), sem texel solto, com relevo leve e pedrinhas opcionais."""
    h = fbm(seed)
    lv = majority(np.digitize(h, list(thresholds)), smooth)
    tones = np.array(tones)
    out = tones[lv]
    edge = light_edges(lv)
    out = np.where(edge == 1, tones[np.minimum(lv + 1, 2)], np.where(edge == -1, tones[np.maximum(lv - 1, 0)], out))
    if speck:
        dark, light = speck
        out = np.where(specks(seed + 9, 0.018), dark, out)
        out = np.where(specks(seed + 10, 0.010), light, out)
    if pebbles:
        mask = blobs(seed + 20, pebbles, 1.2, 2.6)
        base, hi, lo = pebble_tones
        e = light_edges(mask)
        out = np.where(mask, np.where(e == 1, hi, np.where(e == -1, lo, base)), out)
        cast = roll(mask, 1, 1) & ~mask  # sombra projetada embaixo/direita
        out = np.where(cast, tones[0], out)
    return out


def stones(seed, per_side, gap, base_tones, hi, lo, gap_width=1.2):
    """Pedra: Voronoi com frestas, tom sorteado por pedra, realce e sombra na borda, granulado fino."""
    idx, edge = voronoi(per_side, seed)
    tone_of = rng(seed + 1).integers(0, len(base_tones), idx.max() + 1)
    out = np.array(base_tones)[tone_of[idx]]
    inside = edge >= gap_width
    e = light_edges(inside)
    out = np.where(e == 1, hi, np.where(e == -1, lo, out))
    out = np.where(inside & specks(seed + 3, 0.05), lo, out)
    out = np.where(inside & specks(seed + 4, 0.03), hi, out)
    return np.where(inside, out, gap)


def strata(seed, tones, band=4):
    """Compactado: faixas horizontais onduladas em tons alternados, com riscos claros. N / band precisa ser múltiplo de
    len(tones), senão a última faixa não emenda com a primeira ao repetir."""
    assert (N // band) % len(tones) == 0 and N % band == 0
    yy = np.arange(N)[:, None].repeat(N, 1).astype(float)
    wave = 3.0 * (value_noise(4, seed) - 0.5) + 1.5 * (value_noise(16, seed + 1) - 0.5)
    k = np.floor((yy + wave * band) / band).astype(int) % len(tones)
    out = np.array(tones)[majority(k, 1)]
    return np.where(specks(seed + 2, 0.02), tones[-1], out)


def sand(seed, tones, grain_dark, grain_light):
    """Areia: dois tons em pontilhado de Bayer pelo fbm, manchas claras e grãos soltos (a areia pede granulado)."""
    h = fbm(seed, periods=(8, 16, 32), weights=(0.5, 0.3, 0.2))
    bayer = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0
    thr = np.tile(bayer, (N // 4, N // 4))
    mix = np.clip((h - 0.25) / 0.5, 0, 1)
    out = np.where(mix > thr, tones[1], tones[0])
    out = np.where(h > 0.82, tones[2], out)
    out = np.where(specks(seed + 5, 0.045), grain_dark, out)
    return np.where(specks(seed + 6, 0.035), grain_light, out)


def fibers(seed, tones, vertical=True, density=0.5):
    """Plantas: riscos curtos na vertical (grama) ou em diagonal (folha) sobre um tom de fundo."""
    r = rng(seed)
    out = np.full((N, N), tones[0])
    for _ in range(int(N * N * density / 6)):
        x, y = r.integers(0, N, 2)
        t = tones[r.integers(1, len(tones))]
        for i in range(r.integers(3, 8)):
            out[(y + i) % N, (x + (0 if vertical else i // 2)) % N] = t
    return out


def clods(seed, base, tones, hi, shadow, count=150):
    """Terra solta: torrões arredondados de tamanhos variados sobre um fundo mais escuro, com luz e sombra projetada."""
    r = rng(seed)
    out = earth(seed + 50, base, smooth=2)
    for k in range(count):
        mask = blobs(seed + 100 + k, 1, 1.5, 3.8)
        e = light_edges(mask)
        tone = tones[r.integers(0, len(tones))]
        cast = roll(mask, 1, 1) & ~mask
        out = np.where(cast, shadow, out)
        out = np.where(mask, np.where(e == 1, hi, tone), out)
    return out


def chunks(seed, per_side, tones, gap, hi, lo, extra=None):
    """Entulho e restos: pedaços (Voronoi) de tons sorteados, com relevo e frestas."""
    idx, edge = voronoi(per_side, seed, jitter=0.95)
    tone_of = rng(seed + 1).integers(0, len(tones), idx.max() + 1)
    out = np.array(tones)[tone_of[idx]]
    if extra is not None:
        out = np.where((tone_of[idx] == 0) & specks(seed + 7, 0.3), extra, out)
    inside = edge >= 1.0
    e = light_edges(inside)
    out = np.where(e == 1, hi, np.where(e == -1, lo, out))
    return np.where(inside, out, gap)


def litter(seed):
    """Serapilheira (REQ-070): folhas secas sobrepostas, com nervura e luz na borda, e gravetos miúdos sobre húmus escuro."""
    r = rng(seed)
    out = earth(seed + 1, [2, 3, 9], smooth=2)
    yy, xx = np.mgrid[0:N, 0:N]
    for k in range(120):
        cx, cy = r.uniform(0, N, 2)
        length, width = r.uniform(14, 26), r.uniform(7, 11)
        angle = r.uniform(0, np.pi)
        ca, sa = np.cos(angle), np.sin(angle)
        # Distância periódica ao centro: a folha que sai de um lado do tile entra no outro.
        dx = (xx - cx + N / 2) % N - N / 2
        dy = (yy - cy + N / 2) % N - N / 2
        u, v = dx * ca + dy * sa, -dx * sa + dy * ca
        # Folha em ponta dos dois lados: a largura cai com o quadrado da posição ao longo dela.
        inside = (np.abs(u) <= length / 2) & (np.abs(v) <= (width / 2) * (1 - (2 * u / length) ** 2))
        tone = [8, 10, 11, 11, 12, 9][r.integers(0, 6)]
        e = light_edges(inside)
        cast = roll(inside, 1, 1) & ~inside
        out = np.where(cast, 1, out)
        out = np.where(inside, np.where(e == 1, min(tone + 1, 13), np.where(e == -1, 3, tone)), out)
        out = np.where(inside & (np.abs(v) < 1.0) & (np.abs(u) < length * 0.42), 3 if tone != 9 else 8, out)
    for k in range(10):
        x, y = r.integers(0, N, 2)
        dx, dy = (1, 0) if r.random() < 0.5 else (1, 1 if r.random() < 0.5 else -1)
        for i in range(r.integers(6, 14)):
            for w in (0, 1):
                out[(y + i * dy + w) % N, (x + i * dx) % N] = 5
            out[(y + i * dy + 2) % N, (x + i * dx) % N] = 3
    return out


def compost(seed):
    """Terra adubada (REQ-070): terra escura e fofa com torrões, palha picada e pontas de folha verde, em traços de 2 texels."""
    r = rng(seed)
    out = earth(seed, [1, 2, 3], smooth=2, thresholds=(0.34, 0.62))
    clods_mask = blobs(seed + 5, 40, 1.6, 3.2)
    e = light_edges(clods_mask)
    out = np.where(clods_mask, np.where(e == 1, 4, np.where(e == -1, 1, 3)), out)
    for k in range(70):
        x, y = r.integers(0, N, 2)
        tone = [7, 6, 6, 25][r.integers(0, 4)]
        dx, dy = [(1, 0), (1, 1), (1, -1), (0, 1)][r.integers(0, 4)]
        for i in range(r.integers(3, 7)):
            for w in (0, 1):
                out[(y + i * dy + w * (dx != 0)) % N, (x + i * dx + w * (dx == 0)) % N] = tone
    return out


def nectar(seed):
    """Néctar (REQ-070): mel âmbar com bolhas de luz; só aparece na mão da formiga e no item guardado."""
    out = earth(seed, [12, 13, 7], smooth=3, thresholds=(0.3, 0.7))
    mask = blobs(seed + 3, 26, 1.8, 3.4)
    e = light_edges(mask)
    return np.where(mask, np.where(e == 1, 16, np.where(e == -1, 12, 15)), out)


def grow(mask, k):
    """Dilata a máscara em k texels (disco sem as quinas), com emenda periódica."""
    out = mask.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            if abs(dx) + abs(dy) <= k + 1 and (dx or dy):
                out |= roll(mask, dx, dy)
    return out


def nuggets(seed, clusters, rmin, rmax):
    """Pepitas: elipse grande com duas ou três menores em volta, periódicas. Máscara booleana."""
    r = rng(seed)
    mask = np.zeros((N, N), bool)
    yy, xx = np.mgrid[0:N, 0:N] + 0.5
    for _ in range(clusters):
        cx, cy = r.random() * N, r.random() * N
        parts = [(cx, cy, r.uniform(rmin, rmax))]
        for _ in range(r.integers(1, 3)):
            ang = r.random() * 6.283
            d = r.uniform(rmax * 0.9, rmax * 1.5)
            parts.append((cx + d * np.cos(ang), cy + d * np.sin(ang) * 0.7, r.uniform(2.6, 3.6)))
        for px, py, pr in parts:
            for ox in (-N, 0, N):
                for oy in (-N, 0, N):
                    mask |= ((xx - px - ox) / pr) ** 2 + ((yy - py - oy) / (pr * 0.8)) ** 2 <= 1
    return mask


def pair(m):
    """Engrossa de 1 para 2 texels (grão que sobrevive à redução a 8 texels por célula)."""
    return m | roll(m, 1, 0) | roll(m, 0, 1) | roll(m, 1, 1)


def ore(seed, body, hi, lo, glint, rust=None):
    """Minério (REQ-071): a pedra com pepitas do metal, em contorno escuro de 2 texels, com luz no alto/esquerda,
    sombra embaixo/direita, facetas em dois tons e brilhos."""
    out = stones(seed, 9, 1, [17, 18, 18, 19], 20, 17)
    mask = nuggets(seed + 7, 18, 6.0, 9.0)
    outline = grow(mask, 2) & ~mask
    lv = majority(np.digitize(fbm(seed + 3), [0.5]), 2)
    tone = np.array(body)[lv]
    lit = mask & ~roll(mask, 2, 2)
    shade = mask & ~roll(mask, -2, -2)
    core = mask & ~lit & ~shade
    fill = np.where(lit, hi, np.where(shade, lo, tone))
    fill = np.where(core & pair(specks(seed + 11, 0.012)), glint, fill)
    if rust is not None:
        fill = np.where(core & pair(specks(seed + 12, 0.012)), rust, fill)
    out = np.where(outline, 0, out)
    return np.where(mask, fill, out)


def copperore(seed):
    return ore(seed, [12, 34], 7, 8, 16)


def ironore(seed):
    return ore(seed, [45, 44], 46, 18, 31)


def goldore(seed):
    return ore(seed, [13, 12], 16, 11, 46)


# Barra: lingote (borda, sombra de baixo, sombra do lado, tons do corpo, topo claro, brilho)
INGOTS = {'Copper': dict(out=32, bottom=8, side=34, body=[12, 11], top=7, glint=16),
          'Iron': dict(out=0, bottom=17, side=18, body=[44, 19], top=45, glint=46),
          'Gold': dict(out=8, bottom=10, side=11, body=[13, 12], top=16, glint=46)}


def ingots(seed, metal):
    """Barra (REQ-071): lingotes de 32 x 16 texels empilhados em fileiras alternadas, com borda escura de 2 texels, topo
    claro, lado e base na sombra, quinas cortadas e um brilho no topo."""
    c = INGOTS[metal]
    uid, ix, iy = grid_units(32, 16, offset=16)
    tone = np.array(c['body'])[rng(seed).integers(0, len(c['body']), uid.max() + 1)[uid]]
    out = np.where(iy < 6, c['top'], np.where(iy >= 14, c['bottom'], tone))
    out = np.where((ix >= 30) & (iy >= 6) & (iy < 14), c['side'], out)
    out = np.where((ix < 4) & (iy >= 6) & (iy < 14), c['top'], out)
    out = np.where((ix >= 6) & (ix < 10) & (iy >= 2) & (iy < 4), c['glint'], out)
    border = (ix < 2) | (iy < 2)
    for cx in (2, 30):  # quinas cortadas em 2 x 2 texels
        for cy in (2, 14):
            border |= (ix >= cx) & (ix < cx + 2) & (iy >= cy) & (iy < cy + 2)
    return np.where(border, c['out'], out)


# Equipamento: um objeto de 8 x 8 por célula (desenhado a 8 e ampliado 2x, para a redução a 8 texels devolver o desenho),
# em fileiras de células, sobre um fundo escuro. l = luz, m = corpo, d = sombra, x e w = cabo de madeira (claro e escuro), . = fundo.
GLYPHS = {
    'Tool': ['.llmmmd.',
             'lm.xw.md',
             'm..xw..d',
             '...xw...',
             '...xw...',
             '...xw...',
             '...xw...',
             '........'],
    'Weapon': ['...ll...',
               '...lm...',
               '...lm...',
               '...lm...',
               '...lm...',
               '.dddddd.',
               '...xw...',
               '...dd...'],
    'Armor': ['llllllld',
              'lmmmmmmd',
              'lmmddmmd',
              'lmmddmmd',
              '.lmmmmd.',
              '.lmmmmd.',
              '..lmmd..',
              '...dd...'],
}
GEAR = {'Copper': dict(bg=1, l=7, m=12, d=8, x=5, w=4), 'Iron': dict(bg=0, l=46, m=44, d=18, x=5, w=4),
        'Gold': dict(bg=2, l=16, m=13, d=11, x=5, w=4)}


def gear(metal, kind):
    """Ferramenta (picareta), arma (espada) ou armadura (peitoral) do metal, um por célula, sobre fundo escuro."""
    c = GEAR[metal]
    unit = np.array([[c['bg'] if ch == '.' else c[ch] for ch in row] for row in GLYPHS[kind]])
    return np.kron(np.tile(unit, (N // 16, N // 16)), np.ones((2, 2), int))


def metal_good(seed, metal, kind):
    """Barra e equipamentos (REQ-071): só aparecem no bloco carregado e no item guardado. A barra são lingotes
    empilhados; ferramenta, arma e armadura são o desenho do objeto, e as cores dizem o metal."""
    if kind == 'Bar':
        return ingots(seed, metal)
    return gear(metal, kind)


# ---------------------------------------------------------------- salas construídas (REQ-058)
# Bloco: unidades regulares alinhadas à grade das células (a junta cai na borda da célula), para o construído se
# distinguir da terra natural, que é orgânica. Uma célula de slot isolada mostra unidades inteiras.
# Fundo: o tom do túnel um pouco mais claro e calmo, com o motivo da família (o shader ainda o suaviza com Back Detail).

def grid_units(w, h, offset=0, jitter_seed=None):
    """Unidades w x h alinhadas à grade; fileiras alternadas deslocadas em offset (ou em deslocamentos sorteados).
    Devolve o id da unidade e a posição do texel dentro dela."""
    assert N % w == 0 and N % h == 0 and (offset == 0 or (N // h) % 2 == 0)
    yy, xx = np.mgrid[0:N, 0:N]
    row = yy // h
    shift = rng(jitter_seed).integers(0, w, N // h)[row] if jitter_seed is not None else (row % 2) * offset
    sx = (xx + shift) % N
    return row * (N // w) + sx // w, sx % w, yy % h


def masonry(seed, w, h, tones, joint, hi, lo, offset=0, bevel=1, rounded=False, jitter=False, flecks=()):
    """Bloco construído: unidades com tom sorteado, junta de 1 texel no alto/esquerda, luz no alto/esquerda e sombra
    embaixo/direita; rounded corta as quinas; flecks = (tom, chance, largura) salpicados no miolo."""
    uid, ix, iy = grid_units(w, h, offset, seed + 5 if jitter else None)
    tone_of = rng(seed).integers(0, len(tones), uid.max() + 1)
    out = np.array(tones)[tone_of[uid]]
    lit = ((iy >= 1) & (iy <= bevel)) | ((ix >= 1) & (ix <= bevel))
    shade = (iy >= h - bevel) | (ix >= w - bevel)
    out = np.where(lit & ~shade, hi, np.where(shade & ~lit, lo, out))
    core = ~lit & ~shade
    for k, (tone, chance, width) in enumerate(flecks):
        m = specks(seed + 10 + k, chance)
        for i in range(1, width):
            m |= roll(m, 1, 0)
        out = np.where(m & core, tone, out)
    j = (ix == 0) | (iy == 0)
    if rounded:
        j |= ((ix == 1) | (ix == w - 1)) & ((iy == 1) | (iy == h - 1))
    return np.where(j, joint, out), ix, iy, uid


def calm_back(seed, tones=(1, 2, 3), thresholds=(0.08, 0.85)):
    """Fundo de sala: quase todo no tom do meio, poucas manchas."""
    return earth(seed, list(tones), smooth=3, thresholds=thresholds)


def hollows(mask, out, shadow, lip):
    """Cavidade (máscara): a borda de cima/esquerda por dentro fica na sombra, a de baixo/direita recebe a luz."""
    out = np.where(mask & ~roll(mask, 1, 1), shadow, out)
    return np.where(mask & ~roll(mask, -1, -1), lip, out)


def walks(seed, count, steps, persistence=0.75):
    """Fios (micélio): passeios aleatórios periódicos de 1 texel com direção que muda pouco."""
    r = rng(seed)
    mask = np.zeros((N, N), bool)
    dirs = [(1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)]
    for _ in range(count):
        x, y = r.integers(0, N, 2)
        d = r.integers(0, 8)
        for _ in range(r.integers(*steps)):
            mask[y % N, x % N] = True
            if r.random() > persistence:
                d = (d + r.choice([-1, 1])) % 8
            x, y = x + dirs[d][0], y + dirs[d][1]
    return mask


def queen_block(seed):
    """Rainha: lajotas grandes de barro claro (uma por célula), chanfro largo e losango dourado embutido."""
    out, ix, iy, _ = masonry(seed, 16, 16, [6, 6, 14], 3, 15, 5, bevel=2, flecks=[(5, 0.02, 1)])
    d = np.abs(ix - 8) + np.abs(iy - 8)
    out = np.where(d == 4, 5, out)  # sulco em volta do losango
    gem = d <= 2
    return np.where(gem, np.where((d == 2) & (ix + iy < 16), 13, np.where((d == 2) & (ix + iy > 16), 11, 12)), out)


def queen_back(seed):
    """Rainha: reboco liso com treliça de losangos em sulco e cravos dourados nos cruzamentos."""
    out = calm_back(seed)
    yy, xx = np.mgrid[0:N, 0:N]
    groove = ((xx + yy) % 32 == 0) | ((xx - yy) % 32 == 0)
    out = np.where(roll(groove, 0, 1) & ~groove, 3, out)  # lábio claro embaixo do sulco
    out = np.where(groove, 1, out)
    stud = ((xx % 32 == 0) & (yy % 32 == 0)) | ((xx % 32 == 16) & (yy % 32 == 16))
    out = np.where(stud | roll(stud, 1, 0) | roll(stud, 0, 1) | roll(stud, 1, 1), 11, out)
    return np.where(stud, 12, out)


def nutrient_block(seed):
    """Cultivo: torrões de húmus escuros, com picadinho de folha e fios de fungo."""
    out, *_ = masonry(seed, 8, 8, [3, 4, 3], 1, 5, 2, offset=4, rounded=True,
                      flecks=[(25, 0.035, 2), (24, 0.035, 2), (15, 0.02, 1)])
    return out


def nutrient_back(seed):
    """Cultivo: parede escura com rede de micélio claro e nós de fungo."""
    out = calm_back(seed, (1, 2, 3), (0.12, 0.88))
    threads = walks(seed + 1, 46, (20, 60))
    out = np.where(threads, 4, out)
    knots = threads & specks(seed + 2, 0.06)
    return np.where(knots, 14, out)


def nursery_block(seed):
    """Berçário: almofadas de barro creme, pequenas e arredondadas, em fileiras desencontradas."""
    out, *_ = masonry(seed, 8, 8, [15, 15, 14], 5, 16, 6, offset=4, rounded=True)
    return out


def nursery_back(seed):
    """Berçário: nichos redondos (berços) em favo, um por célula."""
    out = calm_back(seed)
    yy, xx = np.mgrid[0:N, 0:N] + 0.5
    mask = np.zeros((N, N), bool)
    for j in range(N // CELL):
        for i in range(N // CELL):
            cx, cy = CELL * i + CELL / 2 + (CELL / 2) * (j % 2), CELL * j + CELL / 2
            for ox in (-N, 0, N):
                mask |= np.hypot(xx - cx - ox, yy - cy) <= 4.2
    return hollows(mask, np.where(mask, 1, out), 0, 3)


def waste_block(seed):
    """Resíduos: crosta de refugo socado em fiadas tortas, escura, com ferrugem e cascalho."""
    out, *_ = masonry(seed, 16, 8, [1, 2, 2, 3], 0, 3, 0, jitter=True,
                      flecks=[(8, 0.05, 1), (17, 0.03, 2), (0, 0.04, 1)])
    return out


def waste_back(seed):
    """Resíduos: parede encardida com escorridos escuros e secos."""
    out = calm_back(seed, (0, 1, 2), (0.3, 0.88))
    r = rng(seed + 1)
    for _ in range(70):
        x, y = r.integers(0, N, 2)
        tone = 0 if r.random() < 0.6 else 3
        length = r.integers(4, 18)
        for i in range(length):
            out[(y + i) % N, x] = tone
        out[(y + length) % N, x] = tone  # gota no fim
        out[(y + length) % N, (x + 1) % N] = tone
    return out


def dew_block(seed):
    """Orvalho: ladrilhos de pedra vidrados, um por célula, com reflexo e gotas; junta úmida às vezes azul."""
    out, ix, iy, uid = masonry(seed, 16, 16, [18, 19, 19], 1, 20, 17)
    joint = (ix == 0) | (iy == 0)
    out = np.where(joint & specks(seed + 3, 0.3), 29, out)
    sheen = ((ix + iy == 6) | (ix + iy == 7)) & (ix >= 2) & (iy >= 2)
    out = np.where(sheen, 20, out)
    r = rng(seed + 4)
    drop = r.random(uid.max() + 1) < 0.55
    px, py = r.integers(4, 13, uid.max() + 1), r.integers(4, 12, uid.max() + 1)
    here = drop[uid] & (ix == px[uid])
    out = np.where(here & (iy == py[uid]), 31, out)
    out = np.where(here & (iy == py[uid] + 1), 30, out)
    return np.where(here & (iy == py[uid] + 2), 17, out)


def dew_back(seed):
    """Orvalho: parede com manchas de umidade (mais escuras), escorridos azulados e gotas brilhando."""
    out = calm_back(seed, (1, 2, 3), (0.08, 0.88))
    damp = majority((fbm(seed + 1) > 0.66).astype(int), 2).astype(bool)
    out = np.where(damp, 1, out)
    out = np.where(damp & ~roll(damp, 0, -1), 29, out)  # borda de baixo da mancha, onde a água junta
    r = rng(seed + 2)
    for _ in range(26):
        x, y = r.integers(0, N, 2)
        for i in range(r.integers(4, 12)):
            out[(y + i) % N, x] = 29
    for _ in range(30):
        x, y = r.integers(0, N, 2)
        out[y, x] = 31
        out[(y + 1) % N, x] = 30
        out[(y + 2) % N, x] = 0
    return out


def farm_block(seed):
    """Fazenda: trançado de fibra de planta (cesto), retalhos de 8 x 8 alternando o sentido."""
    uid, ix, iy = grid_units(8, 8)
    yy, xx = np.mgrid[0:N, 0:N]
    across = ((xx // 8) + (yy // 8)) % 2 == 0
    t = np.where(across, iy, ix)  # posição através das fibras
    along = np.where(across, ix, iy)
    tone_of = rng(seed).integers(0, 2, uid.max() + 1)
    base = np.array([23, 24])[tone_of[uid]]
    out = np.where(t % 3 == 0, 25, base)  # lado da fibra voltado para a luz
    out = np.where(t % 3 == 2, 22, out)  # vão entre fibras
    out = np.where((along == 0) | (along == 7), 22, out)  # a fibra some sob a vizinha
    return np.where(specks(seed + 1, 0.02) & (t % 3 == 1), 9, out)


def farm_back(seed):
    """Fazenda: parede com musgo escuro e pingos de melada."""
    out = calm_back(seed, (1, 2, 3), (0.2, 0.85))
    h = fbm(seed + 1)
    moss = majority(np.digitize(h, [0.68, 0.8]), 2)
    out = np.where(moss == 1, 22, np.where(moss == 2, 23, out))
    return np.where(specks(seed + 2, 0.005), 7, out)


def store_block(seed):
    """Depósito: tijolos de barro cozido em amarração corrida (dois por célula)."""
    out, *_ = masonry(seed, 16, 8, [10, 10, 11], 2, 12, 9, offset=8, flecks=[(9, 0.02, 1), (12, 0.01, 1)])
    return out


def store_back(seed):
    """Depósito: prateleiras a cada duas células e esteios a cada quatro."""
    out = calm_back(seed)
    yy, xx = np.mgrid[0:N, 0:N]
    ly, lx = yy % 32, xx % 64
    out = np.where(lx == 63, 4, np.where(lx == 0, 3, np.where(lx == 1, 1, out)))
    out = np.where(ly == 30, 4, np.where(ly == 31, 3, np.where(ly == 0, 0, out)))
    return out


def smelter_block(seed):
    """Fundição (REQ-071): tijolo refratário vermelho-escuro em fiadas, com fuligem nas juntas e na face de baixo."""
    out, ix, iy, _ = masonry(seed, 16, 8, [32, 33, 33], 0, 34, 8, offset=8, flecks=[(1, 0.03, 2)])
    return np.where((iy >= 6) & specks(seed + 3, 0.35), 1, out)


def smelter_back(seed):
    """Fundição: parede escura de fuligem, com brasa nas frestas perto do chão de cada célula (a sala ferve)."""
    out = calm_back(seed, (0, 1, 2), (0.25, 0.85))
    yy, xx = np.mgrid[0:N, 0:N]
    cracks = walks(seed + 1, 30, (6, 22))
    low = (yy % CELL) >= CELL - 6
    out = np.where(cracks, 0, out)
    out = np.where(cracks & low, 34, out)
    return np.where(cracks & low & specks(seed + 2, 0.4), 12, out)


def forge_block(seed):
    """Forja: blocos grandes de pedra escura (um por célula) com cinta de ferro e rebites nas quinas."""
    out, ix, iy, _ = masonry(seed, 16, 16, [17, 17, 1], 0, 18, 0, bevel=2)
    band = (iy == 7) | (iy == 8)
    out = np.where(band & (ix > 0), 18, out)
    out = np.where((iy == 9) & (ix > 0), 0, out)
    rivet = ((ix == 3) | (ix == 12)) & ((iy == 3) | (iy == 12))
    return np.where(rivet, 20, out)


def forge_back(seed):
    """Forja: pedra escura com o brilho alaranjado do fogo subindo pelo meio da parede e ferramentas penduradas em silhueta."""
    out = calm_back(seed, (0, 1, 1), (0.3, 0.9))
    yy, xx = np.mgrid[0:N, 0:N]
    glow = majority((fbm(seed + 1) > 0.62).astype(int), 2).astype(bool)
    out = np.where(glow, 8, out)
    out = np.where(glow & specks(seed + 2, 0.25), 33, out)
    # Pregos a cada duas células, com uma tenaz em silhueta pendurada.
    lx, ly = xx % 32, yy % 32
    hook = (lx == 16) & (ly >= 4) & (ly <= 5)
    tool = ((lx == 15) | (lx == 17)) & (ly >= 6) & (ly <= 13)
    return np.where(hook, 20, np.where(tool, 0, out))


def armory_block(seed):
    """Armazém de ferramentas e armas: tábuas verticais de madeira escura com pregos."""
    uid, ix, iy = grid_units(8, 16, offset=8)
    tone_of = rng(seed).integers(0, 3, uid.max() + 1)
    out = np.array([9, 9, 4])[tone_of[uid]]
    out = np.where(ix == 1, 10, np.where(ix == 7, 3, out))
    out = np.where((iy == 2) & (ix == 4), 18, out)
    return np.where(ix == 0, 2, out)


def armory_back(seed):
    """Armazém de ferramentas e armas: parede de tábuas com um cabideiro (barra e ganchos) a cada duas células."""
    out = calm_back(seed, (1, 2, 3), (0.15, 0.85))
    yy, xx = np.mgrid[0:N, 0:N]
    out = np.where(xx % 8 == 0, 1, out)  # junta das tábuas
    ly = yy % 32
    out = np.where(ly == 26, 10, np.where(ly == 25, 4, out))  # barra do cabideiro
    hook = (ly >= 27) & (ly <= 28) & (xx % 16 == 8)
    return np.where(hook, 18, out)


def wardrobe_block(seed):
    """Armazém de roupas e armaduras: madeira clara em painéis almofadados, um por célula."""
    out, ix, iy, _ = masonry(seed, 16, 16, [5, 5, 6], 3, 14, 4, bevel=1)
    inset = (ix >= 4) & (ix <= 12) & (iy >= 4) & (iy <= 12)
    edge = inset & ((ix == 4) | (iy == 4) | (ix == 12) | (iy == 12))
    return np.where(edge, 4, np.where(inset, 6, out))


def wardrobe_back(seed):
    """Armazém de roupas e armaduras: tecido azul-escuro em trama, com uma faixa clara de costura a cada duas células."""
    out = calm_back(seed, (1, 2, 1), (0.2, 0.85))
    yy, xx = np.mgrid[0:N, 0:N]
    weave = ((xx // 2) + (yy // 2)) % 2 == 0
    out = np.where(weave, 29, out)
    out = np.where(yy % 32 == 16, 30, out)
    return np.where((yy % 32 == 16) & (xx % 4 == 0), 1, out)


ROOM_TILES = {
    'Queen': (queen_block, queen_back), 'Nutrient': (nutrient_block, nutrient_back),
    'Nursery': (nursery_block, nursery_back), 'Waste': (waste_block, waste_back), 'Dew': (dew_block, dew_back),
    'Farm': (farm_block, farm_back), 'Store': (store_block, store_back),
    'Smelter': (smelter_block, smelter_back), 'Forge': (forge_block, forge_back),
    'Armory': (armory_block, armory_back), 'Wardrobe': (wardrobe_block, wardrobe_back),
}

ORDER = ['Empty', 'Dirt', 'Grass', 'Sand', 'Rock', 'Spoil', 'Bedrock', 'PackedSpoil', 'PackedSand', 'Mud',
         'PlantGrass', 'PlantLeaf', 'FallenGrass', 'FallenLeaf', 'Rubble', 'Refuse', 'TermiteRemains',
         'GlowMushroom', 'Litter', 'Compost', 'Nectar', 'CopperOre', 'IronOre', 'GoldOre',
         'CopperBar', 'IronBar', 'GoldBar', 'CopperTool', 'IronTool', 'GoldTool', 'CopperWeapon', 'IronWeapon', 'GoldWeapon',
         'CopperArmor', 'IronArmor', 'GoldArmor', 'Background'] + [s + part for s in ROOM_STYLES for part in ('Block', 'Back')]


def build():
    t = {}
    # Parede de fundo calma: quase toda no tom do túnel, poucas manchas escuras (o shader ainda a suaviza com Back Detail).
    t['Background'] = earth(1800, [0, 1, 2], speck=(0, 2), smooth=3, thresholds=(0.12, 0.72))
    t['Empty'] = t['Background']
    t['Dirt'] = earth(100, [3, 9, 10], pebble_tones=(18, 20, 17), pebbles=22, speck=(2, 4))
    t['Grass'] = earth(200, [23, 24, 25], speck=(9, 26))
    t['Sand'] = sand(300, [6, 7, 15], 5, 16)
    t['Rock'] = stones(400, 9, 1, [17, 18, 18, 19], 20, 17)
    t['Spoil'] = clods(500, [9, 10, 10], [10, 11, 11, 12], 12, 3)
    t['Bedrock'] = stones(600, 4, 0, [0, 0, 1], 17, 0, gap_width=1.6)
    t['PackedSpoil'] = strata(700, [4, 9, 5, 9])
    t['PackedSand'] = strata(800, [6, 5, 6, 7])
    t['Mud'] = earth(900, [1, 2, 3], speck=(1, 4), smooth=3)
    t['PlantGrass'] = fibers(1000, [25, 24, 26, 26, 27])
    t['PlantLeaf'] = fibers(1100, [24, 23, 25, 25], vertical=False)
    t['FallenGrass'] = fibers(1200, [26, 25, 27, 7])
    t['FallenLeaf'] = fibers(1300, [24, 23, 9, 25], vertical=False)
    t['Rubble'] = chunks(1400, 9, [10, 4, 9, 18], 2, 11, 3)
    t['Refuse'] = chunks(1500, 12, [3, 4, 17, 2], 1, 5, 1)
    t['TermiteRemains'] = chunks(1600, 12, [8, 9, 3, 10], 1, 11, 0, extra=5)
    t['GlowMushroom'] = earth(1700, [7, 15, 16], speck=(13, 16), smooth=2)
    t['Litter'] = litter(1750)
    t['Compost'] = compost(1760)
    t['Nectar'] = nectar(1770)
    t['CopperOre'] = copperore(1780)
    t['IronOre'] = ironore(1790)
    t['GoldOre'] = goldore(1800)
    for k, kind in enumerate(('Bar', 'Tool', 'Weapon', 'Armor')):
        for m, metal in enumerate(('Copper', 'Iron', 'Gold')):
            t[metal + kind] = metal_good(1810 + 10 * (3 * k + m), metal, kind)
    for k, style in enumerate(ROOM_STYLES):
        block, back = ROOM_TILES[style]
        t[style + 'Block'] = block(1900 + 200 * k)
        t[style + 'Back'] = back(2000 + 200 * k)
    assert len(ORDER) == ROWS and set(ORDER) == set(t)
    return t


def compose(pal, tiles):
    atlas = np.zeros((N * ROWS, N, 3), np.uint8)
    for row, name in enumerate(ORDER):
        top = N * (ROWS - 1 - row)  # linha 0 embaixo no PNG
        atlas[top:top + N] = pal[tiles[name]]
    return atlas


def preview(pal, tiles, path):
    """Cada material repetido 2x2 (mostra a emenda) e ampliado 2x, na ordem do enum."""
    cols = 5
    cw = N * 4 + 8
    img = Image.new('RGB', (cols * cw, ((len(ORDER) + cols - 1) // cols) * cw), (24, 22, 26))
    for i, name in enumerate(ORDER):
        im = Image.fromarray(pal[np.tile(tiles[name], (2, 2))]).resize((N * 4, N * 4), Image.NEAREST)
        img.paste(im, ((i % cols) * cw, (i // cols) * cw))
    img.save(path)


ROOM_NAMES = {'Queen': 'Rainha', 'Nutrient': 'Cultivo', 'Nursery': 'Berçário', 'Waste': 'Resíduos', 'Dew': 'Orvalho',
              'Farm': 'Fazenda', 'Store': 'Depósito', 'Smelter': 'Fundição', 'Forge': 'Forja', 'Armory': 'Armas',
              'Wardrobe': 'Armaduras'}

# Maquete da sala: 12 x 8 células. '.' = vão da sala, 'B' = terra em contato com o vão (rebocada quando a sala é
# construída: piso, teto, lados, quinas e o torrão dentro da área pintada), 't' = túnel que chega à sala, '#' = terra.
ROOM_LAYOUT = ['############',
               '#BBBBBBBBBB#',
               '#B........B#',
               '#B.........t',
               '#B.........t',
               '#B...B....B#',
               '#BBBBBBBBBB#',
               '############']


def to_linear(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.round(255 * np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)).astype(np.uint8)


def room_scene(pal, tiles, block, back, built=True, back_detail=0.6, shadow=0.45, shadow_width=6, steps=3):
    """Cena como o shader a desenha com luz cheia: arte lida em coordenadas de mundo, fundo com Back Detail sobre a
    média e sombra de canto em degraus perto da terra (distância de xadrez em texels)."""
    rows, cols = len(ROOM_LAYOUT), len(ROOM_LAYOUT[0])
    h, w = rows * CELL, cols * CELL
    solid = np.zeros((h, w), bool)
    lin = np.zeros((h, w, 3))
    for r, line in enumerate(ROOM_LAYOUT):
        for c, ch in enumerate(line):
            if ch == 'B' and not built:
                ch = '#'
            open_cell = ch in '.t'
            name = 'Dirt' if ch == '#' else block if ch == 'B' else 'Background' if ch == 't' else back
            art = to_linear(pal[tiles[name]].astype(float))
            sl = (slice(r * CELL, (r + 1) * CELL), slice(c * CELL, (c + 1) * CELL))
            tile = art[(r * CELL) % N:(r * CELL) % N + CELL, (c * CELL) % N:(c * CELL) % N + CELL]
            if open_cell:
                tile = art.reshape(-1, 3).mean(0) * (1 - back_detail) + tile * back_detail
            lin[sl] = tile
            solid[sl] = not open_cell
    dist = np.where(solid, 0, 99)
    for k in range(1, shadow_width + 1):  # distância de xadrez até o texel sólido mais próximo
        grown = np.zeros_like(solid)
        near = dist == k - 1
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                grown |= np.roll(np.roll(near, dy, 0), dx, 1)
        dist = np.where((dist == 99) & grown, k, dist)
    texels = dist - 1
    level = np.floor(texels * steps / shadow_width)
    light = np.where(~solid & (texels < shadow_width), 1 - shadow * (1 - level / steps), 1.0)
    return to_srgb(lin * light[..., None])


def rooms_preview(pal, tiles, path, scale=2):
    """Uma sala de cada família construída (blocos nos slots e fundo próprio), ao lado do túnel comum."""
    from PIL import ImageFont
    panels = [('Antes do reboco', room_scene(pal, tiles, 'Background', 'Background', built=False))]
    for style in ROOM_STYLES:
        panels.append((ROOM_NAMES[style], room_scene(pal, tiles, style + 'Block', style + 'Back')))
    ph, pw = panels[0][1].shape[0] * scale, panels[0][1].shape[1] * scale
    cols, gap, label = 4, 8, 22
    rows = (len(panels) + cols - 1) // cols
    img = Image.new('RGB', (cols * (pw + gap) + gap, rows * (ph + label + gap) + gap), (24, 22, 26))
    draw = ImageDraw.Draw(img)
    font = None
    for name in ('arial.ttf', 'DejaVuSans.ttf'):  # fonte com acentos; a padrão do Pillow pode não ter
        try:
            font = ImageFont.truetype(name, 16)
            break
        except OSError:
            pass
    font = font or ImageFont.load_default()
    for i, (name, scene) in enumerate(panels):
        x, y = gap + (i % cols) * (pw + gap), gap + (i // cols) * (ph + label + gap)
        draw.text((x, y), name, fill=(228, 210, 174), font=font)
        img.paste(Image.fromarray(scene).resize((pw, ph), Image.NEAREST), (x, y + label))
    img.save(path)


if __name__ == '__main__':
    pal = load_palette(sys.argv[1])
    tiles = build()
    atlas = compose(pal, tiles)
    Image.fromarray(atlas).save(sys.argv[2])
    if len(sys.argv) > 3:
        preview(pal, tiles, sys.argv[3])
    if len(sys.argv) > 4:
        rooms_preview(pal, tiles, sys.argv[4])
    used = np.unique(atlas.reshape(-1, 3), axis=0)
    palset = {tuple(int(v) for v in c) for c in pal}
    assert all(tuple(int(v) for v in c) in palset for c in used), 'cor fora da paleta'
    print('ok', atlas.shape, 'cores usadas:', len(used))
