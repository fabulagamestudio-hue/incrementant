"""Gera os quadros adultos das formigas a 16 px por célula, etapa 8 do documento 40.

Ant_Walk_0..3 (de lado, virada para a direita, patas na última linha; pivô do importador a 0,25 da altura = meia célula
acima das patas, na escala do sprite) e Ant_Top_0..3 (de cima, virada para a direita, pivô no centro). Ciclo de tripé
em 4 quadros. Só cores da paleta mestra (Art/Palette): contorno no índice 0 e uma rampa de 3 tons por cor de formiga.

- Operárias e variantes de função: 48 x 32 (3 x 2 células), escala de desenho k = 1.
- Rainha (Rainha/): 96 x 64, k = 2. Em jogo ela é desenhada com Queen Scale 2 (6 x 4 células) e o importador a lê a
  32 px por unidade, então cada pixel continua sendo 1/16 de célula. Gáster maior e listrado, tórax maior e as
  marcas das asas que ela perdeu.

Uso: python -I ant_sprites.py <MasterPalette.hex> <pasta Art/Ants> <grupos> [prévia.png]
Grupos: workers, queen, termite, aphids, brood (crias e itens carregados). Operárias, funções e rainha foram
refinadas depois (REQ-049, Source~/Refined): não grave 'workers' nem 'queen' daqui, ou o refinamento se perde.
"""
import os
import sys
import numpy as np
from PIL import Image

OUTLINE = 0
GLINT = 16

# Rampa (sombra, base, luz) por pasta, em índices da paleta mestra. A raiz e Operaria/ são a base vermelha.
RAMPS = {
    '': (32, 33, 34),
    'Operaria': (32, 33, 34),
    'Soldado': (35, 36, 37),
    'Coletora': (23, 24, 26),
    'Escavadora': (8, 12, 13),
    'Cuidadora': (29, 30, 31),
    'Jardineira': (38, 39, 40),
    'Cortadeira': (41, 42, 43),
}
QUEEN_FOLDER = 'Rainha'
QUEEN_RAMP = (32, 33, 34)


def load_palette(path):
    cols = [l.strip() for l in open(path) if l.strip()]
    return np.array([[int(c[i:i + 2], 16) for i in (0, 2, 4)] for c in cols], np.uint8)


class Canvas:
    """Tom por texel: -1 vazio, 0 sombra, 1 base, 2 luz, 3 contorno, 4 brilho. Coordenadas na escala 1 (48 x 32)."""

    def __init__(self, k=1, size=None, origin=(0, 0)):
        self.k = k
        self.w, self.h = size or (48 * k, 32 * k)
        self.ox, self.oy = origin  # onde cai o (0, 0) do desenho de 48 x 32 dentro do canvas
        self.tone = np.full((self.h, self.w), -1, int)
        self.body = np.zeros((self.h, self.w), bool)  # recebe contorno por fora

    def put(self, x, y, t, body=True):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.tone[y, x] = t
            if body:
                self.body[y, x] = True

    def ellipse(self, cx, cy, rx, ry, light=0.45, shade=0.55, bands=0):
        """Elipse sombreada com luz de cima à esquerda; bands > 0 risca faixas verticais escuras (segmentos do gáster)."""
        k = self.k
        for y in range(self.h):
            for x in range(self.w):
                dx = (x + 0.5 - cx * k - self.ox) / (rx * k)
                dy = (y + 0.5 - cy * k - self.oy) / (ry * k)
                if dx * dx + dy * dy <= 1.0:
                    facing = -0.45 * dx - 0.9 * dy      # > 0: encara a luz
                    t = 2 if facing > light else (0 if facing < -shade else 1)
                    if bands and t == 1 and dx > -0.7 and abs((dx + 1) * bands % 1 - 0.5) < 0.09:
                        t = 0
                    self.put(x, y, t)

    def line(self, x0, y0, x1, y1, t, body=True):
        """Linha de 1 texel na escala 1; na escala k, k texels de espessura."""
        k = self.k
        x0, x1 = (int(round(v * k)) + self.ox for v in (x0, x1))
        y0, y1 = (int(round(v * k)) + self.oy for v in (y0, y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            for ox in range(k):
                for oy in range(k):
                    self.put(x0 + ox, y0 + oy, t, body)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def dot(self, x, y, t):
        """Um texel na escala 1 (k x k na escala k), por cima de tudo."""
        for ox in range(self.k):
            for oy in range(self.k):
                xx, yy = x * self.k + ox + self.ox, y * self.k + oy + self.oy
                if 0 <= xx < self.w and 0 <= yy < self.h:
                    self.tone[yy, xx] = t

    def px(self, x, y, t, body=False):
        """Um texel do canvas (sem escala) na posição (x, y) do desenho de 48 x 32 vezes k."""
        xx, yy = x * self.k + self.ox, y * self.k + self.oy
        if body:
            self.put(xx, yy, t)
        elif 0 <= xx < self.w and 0 <= yy < self.h:
            self.tone[yy, xx] = t

    def outline(self):
        m = self.body
        edge = np.zeros_like(m)
        edge[:, 1:] |= m[:, :-1]
        edge[:, :-1] |= m[:, 1:]
        edge[1:, :] |= m[:-1, :]
        edge[:-1, :] |= m[1:, :]
        edge &= ~m
        self.tone[edge] = 3


# Deslocamento do pé por quadro (tripé) e o texel que o grupo que volta para a frente sobe no quadro de passagem.
STEP = [2, 0, -2, 0]
LIFT = [0, 1, 0, 0]
SPREAD = [-8, -1, 7]  # pata de trás, do meio e da frente: para onde o pé aponta a partir do quadril


def leg(c, hip, knee, foot, t, body):
    c.line(hip[0], hip[1], knee[0], knee[1], t, body)
    c.line(knee[0], knee[1], foot[0], foot[1], t, body)


def side(frame, k=1, queen=False, c=None, legs=(0, 1), eye=3, airborne=False, wings=()):
    """Vista de lado. legs = (tom das patas do outro lado, tom das deste lado); eye = tom do olho; airborne = patas
    recolhidas (voo); wings = asas (ver wing()), as de trás antes do corpo e as da frente por cima."""
    c = c or Canvas(k)
    H = 32
    hips = [(25, 18), (27, 18), (29, 18)]
    a, b = STEP[frame], STEP[(frame + 2) % 4]
    lift_a, lift_b = LIFT[frame], LIFT[(frame + 2) % 4]

    def gait(i, near):
        # tripé: pata da frente e de trás de um lado andam com a do meio do outro
        return (a, lift_a) if (i != 1) == near else (b, lift_b)

    def foot(i, near):
        off, lift = gait(i, near)
        if airborne:
            # no ar: pés soltos, puxados para trás, sem tocar o chão
            return (hips[i][0] + SPREAD[i] * 0.6 - 2 + (0 if near else 1), hips[i][1] + 8)
        return (hips[i][0] + SPREAD[i] + off + (0 if near else 1), H - (2 if near else 1) - lift)

    for w in wings:
        if w[-1]:
            wing(c, *w[:-1])
    # patas do outro lado, atrás do corpo: sombra, sem contorno, 1 texel adiante
    for i, (hx, hy) in enumerate(hips):
        knee = (hx + 1 + SPREAD[i] * 0.6, hy + 4)
        leg(c, (hx + 1, hy), knee, foot(i, False), legs[0], False)
    # corpo: gáster, pecíolo fino, tórax e cabeça, com cintura e pescoço (o contorno marca a separação)
    if queen:
        c.ellipse(11, 16.5, 10, 7.5, bands=4)
        c.ellipse(21.5, 18, 1.6, 1.3)
        c.ellipse(27, 15.5, 5, 4)
    else:
        c.ellipse(11, 17, 8.5, 6.5)
        c.ellipse(21, 18, 1.6, 1.3)
        c.ellipse(27, 16, 4.5, 3)
    c.ellipse(37, 15, 4.5, 4.2)
    # mandíbula
    c.line(41, 17, 43, 18, 1)
    c.px(43, 17, 1, body=True)
    # antena: sobe da cabeça, cotovelo e ponta para a frente
    c.line(37, 10, 38, 5, 1)
    c.line(38, 5, 43, 3, 1)
    # patas deste lado
    for i, (hx, hy) in enumerate(hips):
        knee = (hx + SPREAD[i] * 0.6, hy + 4)
        leg(c, (hx, hy), knee, foot(i, True), legs[1], True)
    c.outline()
    # olho: 2x2 com um brilho
    for x, y in ((38, 13), (39, 13), (38, 14), (39, 14)):
        c.dot(x, y, eye)
    c.px(39, 13, 4)
    for w in wings:
        if not w[-1]:
            wing(c, *w[:-1])
    if queen:
        # marcas das asas no alto do tórax: dois traços claros curtos
        for x in (25, 28):
            for d in (0, 1):
                c.tone[12 * k + 1 + c.oy, x * k + d + c.ox] = 2
    return c


def top(frame, k=1, queen=False, c=None, legs=1, eye=3):
    c = c or Canvas(k)
    cy = 16
    a, b = STEP[frame], STEP[(frame + 2) % 4]
    hips = [25, 27, 29]
    for s in (-1, 1):
        for i, hx in enumerate(hips):
            off = a if (i != 1) == (s < 0) else b
            hip = (hx, cy + s * 2)
            knee = (hx + SPREAD[i] * 0.5 + off * 0.5, cy + s * 7)
            foot = (hx + SPREAD[i] + off, cy + s * 13)
            leg(c, hip, knee, foot, legs, True)
        # antenas: saem da frente da cabeça, cotovelo para fora e ponta para a frente
        c.line(38, cy + s * 3, 40, cy + s * 8, 1)
        c.line(40, cy + s * 8, 44, cy + s * 10, 1)
        # mandíbulas em gancho
        c.line(39, cy + s * 2, 41, cy + s * 2, 1)
        c.px(41, cy + s * 1, 1, body=True)
    if queen:
        c.ellipse(11.5, cy, 10, 7, bands=4)
        c.ellipse(22, cy, 1.6, 1.3)
        c.ellipse(27, cy, 5, 3.4)
    else:
        c.ellipse(12, cy, 8.5, 6)
        c.ellipse(21.5, cy, 1.6, 1.3)
        c.ellipse(27, cy, 4, 2.6)
    c.ellipse(35, cy, 4, 4)
    c.outline()
    for s in (-1, 1):
        y = cy + s * 3 + (0 if s > 0 else -1)
        c.dot(36, y, eye)
        c.dot(37, y, eye)
    c.px(37, cy - 4, 4)
    if queen:
        # marcas das asas: um traço claro de cada lado do tórax
        for s in (-1, 1):
            for x in range(25 * k, 28 * k):
                c.tone[(cy + s * 2) * k + c.oy, x + c.ox] = 2
    return c


def wing(c, base, angle, length, width):
    """Asa: elipse girada que sai de base (coordenadas do desenho) a angle graus acima da horizontal para trás, com borda
    e nervura no tom 8 e miolo no tom 7. Não recebe contorno."""
    a = np.radians(angle)
    d = np.array([-np.cos(a), -np.sin(a)])  # para trás e para cima (y para baixo na imagem)
    n = np.array([-d[1], d[0]])
    bx, by = base[0] * c.k + c.ox, base[1] * c.k + c.oy
    cx, cy = bx + d[0] * length / 2, by + d[1] * length / 2
    yy, xx = np.mgrid[0:c.h, 0:c.w] + 0.5
    u = (xx - cx) * d[0] + (yy - cy) * d[1]
    v = (xx - cx) * n[0] + (yy - cy) * n[1]
    inside = (u / (length / 2)) ** 2 + (v / (width / 2)) ** 2 <= 1
    full = np.roll(inside, 1, 0) & np.roll(inside, -1, 0) & np.roll(inside, 1, 1) & np.roll(inside, -1, 1)
    c.tone[inside] = 7
    c.tone[inside & ~full] = 8
    # nervura: da base até perto da ponta, pelo meio da asa
    for t in np.linspace(0, 1, int(length) * 2):
        x = int(bx + d[0] * (length - 3) * t)
        y = int(by + d[1] * (length - 3) * t)
        if 0 <= x < c.w and 0 <= y < c.h and inside[y, x]:
            c.tone[y, x] = 8


def render(c, pal, ramp, extra=None):
    """ramp = (sombra, base, luz); extra = {tom: índice} para os tons 5 em diante (patas, olho, asas do cupim)."""
    img = np.zeros((c.h, c.w, 4), np.uint8)
    pairs = [(0, ramp[0]), (1, ramp[1]), (2, ramp[2]), (3, OUTLINE), (4, GLINT)] + list((extra or {}).items())
    for t, idx in pairs:
        m = c.tone == t
        img[m, :3] = pal[idx]
        img[m, 3] = 255
    return img


def frames(k=1, queen=False):
    return ([('Ant_Walk_%d.png' % f, side(f, k, queen)) for f in range(4)] +
            [('Ant_Top_%d.png' % f, top(f, k, queen)) for f in range(4)])


# Cupim alado (InimigaAlada/): 64 x 64 (4 x 4 células) a 16 px por célula. O corpo da operária fica onde estava no
# canvas antigo de 144 x 144 (pés meia célula abaixo do pivô de lado; pivô no centro de cima). Carapaça carvão-violeta,
# patas vinho, olho âmbar e asas claras. Tons extras: 5 patas, 6 olho, 7 miolo da asa, 8 borda e nervura, 9 patas do
# outro lado.
TERMITE_FOLDER = 'InimigaAlada'
TERMITE_RAMP = (0, 35, 17)
TERMITE_EXTRA = {5: 38, 6: 12, 7: 16, 8: 20, 9: 0}
TERMITE_SIZE = (64, 64)
SIDE_ORIGIN = (8, 24)
TOP_ORIGIN = (8, 16)
WING_BASE = (26, 13)
FLAP = [75, 45, 12, 45]


def wings_at(angle, base=WING_BASE):
    """Asa da frente (por cima do corpo) e a de trás (atrás dele), mais curta e mais baixa."""
    return [(base, angle, 24, 7, False), ((base[0] + 1, base[1]), angle - 30, 19, 6, True)]


def termite_frames():
    def side_t(frame, **kw):
        return side(frame, c=Canvas(1, TERMITE_SIZE, SIDE_ORIGIN), legs=(9, 5), eye=6, **kw)

    out = []
    for f in range(4):
        out.append(('Ant_Fly_%d.png' % f, side_t(f, airborne=True, wings=wings_at(FLAP[f]))))
    out.append(('Ant_Land_0.png', side_t(0, wings=wings_at(75))))
    out.append(('Ant_Land_1.png', side_t(0, wings=wings_at(8))))
    # asas soltas subindo para trás, longe do corpo
    out.append(('Ant_Land_2.png', side_t(0, wings=[((14, 2), 25, 15, 5, False), ((6, 8), 40, 12, 4, False)])))
    out.append(('Ant_Land_3.png', side_t(0)))
    for f in range(4):
        out.append(('Ant_Walk_%d.png' % f, side_t(f)))
    for f in range(4):
        out.append(('Ant_Top_%d.png' % f, top(f, c=Canvas(1, TERMITE_SIZE, TOP_ORIGIN), legs=5, eye=6)))
    return out


# Pulgão (Art/Aphids/): 40 x 24 (2,5 x 1,5 células) a 16 px por célula, cerca de dois terços da operária como o
# anterior. Corpo em pera, cabeça pequena separada, olho escuro, seis patas, antenas longas varridas para trás e os
# dois cornículos atrás, nos verdes da paleta. Pivô de lado a 0,25 da altura (6 texels acima da base, pés na linha de
# baixo); de cima, no centro.
APHID_FOLDER = os.path.join('..', 'Aphids')
APHID_RAMP = (25, 26, 27)
APHID_EXTRA = {5: 24, 9: 23}
APHID_SIZE = (40, 24)
APHID_STEP = [1, 0, -1, 0]
APHID_SPREAD = [-5, 0, 4]


def aphid_side(frame):
    c = Canvas(1, APHID_SIZE)
    a, b = APHID_STEP[frame], APHID_STEP[(frame + 2) % 4]
    lift = LIFT[frame], LIFT[(frame + 2) % 4]
    hips = [(15, 17), (18, 17), (21, 17)]
    for i, (hx, hy) in enumerate(hips):
        off, up = (b, lift[1]) if i != 1 else (a, lift[0])
        leg(c, (hx + 1, hy), (hx + 1 + APHID_SPREAD[i] * 0.5, hy + 3), (hx + 1 + APHID_SPREAD[i] + off, 23 - up), 9, False)
    c.line(29, 13, 24, 6, 9, False)    # antena do outro lado, atrás
    c.line(24, 6, 17, 4, 9, False)
    c.ellipse(14, 13, 8.5, 6.5)        # abdome em pera, mais cheio atrás
    c.ellipse(21, 15, 5, 4.2)
    c.ellipse(30, 15.5, 3, 2.8)        # cabeça, separada pelo pescoço
    c.line(7, 9, 5, 7, 1)              # cornículo
    c.line(30, 12, 27, 4, 5)           # antena deste lado: sobe e volta por cima do corpo
    c.line(27, 4, 19, 1, 5)
    c.line(32, 17, 33, 19, 5)          # rostro
    for i, (hx, hy) in enumerate(hips):
        off, up = (a, lift[0]) if i != 1 else (b, lift[1])
        leg(c, (hx, hy), (hx + APHID_SPREAD[i] * 0.5, hy + 3), (hx + APHID_SPREAD[i] + off, 22 - up), 5, True)
    c.outline()
    for x, y in ((30, 14), (31, 14), (30, 15), (31, 15)):
        c.px(x, y, 3)
    c.px(31, 14, 4)
    return c


def aphid_top(frame):
    c = Canvas(1, APHID_SIZE)
    cy = 12
    a, b = APHID_STEP[frame], APHID_STEP[(frame + 2) % 4]
    hips = [15, 18, 21]
    for s_ in (-1, 1):
        for i, hx in enumerate(hips):
            off = a if (i != 1) == (s_ < 0) else b
            leg(c, (hx, cy + s_ * 3), (hx + APHID_SPREAD[i] * 0.5, cy + s_ * 7),
                (hx + APHID_SPREAD[i] + off, cy + s_ * 10), 5, True)
        c.line(31, cy + s_ * 2, 33, cy + s_ * 6, 5)   # antenas: para a frente e abrindo para os lados
        c.line(33, cy + s_ * 6, 30, cy + s_ * 10, 5)
        c.line(7, cy + s_ * 3, 5, cy + s_ * 5, 1)     # cornículos
    c.ellipse(14, cy, 9, 6.5)
    c.ellipse(21.5, cy, 4.5, 4.5)
    c.ellipse(29.5, cy, 3, 3)
    c.outline()
    for s_ in (-1, 1):
        y = cy + s_ * 2 + (0 if s_ > 0 else -1)
        c.px(30, y, 3)
        c.px(31, y, 3)
    c.px(31, cy - 3, 4)
    return c


# Crias (etapa 9): ovo, larva e pupa em 48 x 32 (as mesmas 3 x 2 células do 18 x 12 a 6 px), dois quadros de respiração
# cada, apoiadas na linha de baixo como antes. Contorno 0, luz de cima à esquerda. Tons extras: 5 boca/cabeça da larva,
# 8 faixas da pupa.
BROOD_SIZE = (48, 32)
EGG_RAMP = (14, 15, 16)
LARVA_RAMP = (14, 15, 16)
LARVA_EXTRA = {5: 6, 8: 8}
PUPA_RAMP = (6, 7, 15)
PUPA_EXTRA = {8: 8}


def egg(frame):
    c = Canvas(1, BROOD_SIZE)
    c.ellipse(24 - frame * 0.5, 25.5 + frame * 0.3, 7, 5.5 - frame * 0.3, light=0.35)
    c.outline()
    c.px(21 - frame, 22, 4)
    return c


def larva(frame):
    """Larva em gomos: círculos ao longo de um corpo curvado, depois um vinco de sombra entre cada par de gomos; a cabeça
    mais escura à direita, com a boca."""
    import math
    c = Canvas(1, BROOD_SIZE)
    n = 8
    segs = []
    for i in range(n):
        x = 7 + i * 4.3
        y = 24.5 - 2.5 * math.sin((i / (n - 1)) * math.pi) + (0.5 * math.sin(i * 1.3) if frame else 0)
        r = 4.0 + 1.3 * math.sin((i + 0.5) / n * math.pi)
        segs.append((x, y, r))
        c.ellipse(x, y, r, r - 0.4, light=0.3)
    hy = 22.5 + frame * 0.5
    c.ellipse(41.5, hy, 3.4, 3.2, light=0.3)
    c.outline()
    for i in range(n - 1):
        xm = int(round((segs[i][0] + segs[i + 1][0]) / 2 + 0.5))
        for yy in range(c.h):
            if c.tone[yy, xm] in (1, 2) and c.tone[yy - 1, xm] != 3:
                c.tone[yy, xm] = 0
    for yy in range(c.h):
        for xx in range(39, 45):
            if c.tone[yy, xx] in (0, 1, 2) and (xx - 41.5) ** 2 + (yy + 0.5 - hy) ** 2 <= 3.4 ** 2:
                c.tone[yy, xx] = 5
    c.px(44, int(hy) + 2, 3)
    return c


def pupa(frame):
    """Pupa: casulo alongado em três tons, com duas faixas escuras no meio (como o anterior)."""
    c = Canvas(1, BROOD_SIZE)
    c.ellipse(24, 25 + frame * 0.3, 19.5 - frame * 0.4, 6.2 - frame * 0.2, light=0.35)
    c.ellipse(23, 19.5 + frame * 0.3, 9, 2.5, light=0.2)  # dobra de cima, mais clara
    c.outline()
    for bx in (20, 26):
        for y in range(20, 31):
            if c.tone[y, bx] in (0, 1, 2):
                c.tone[y, bx] = 8
                c.tone[y, bx + 1] = 8 if c.tone[y, bx + 1] in (0, 1, 2) else c.tone[y, bx + 1]
    return c


# Itens carregados (etapa 9): 16 x 16 = 1 célula, no lugar do quadrado tingido.
ITEM_SIZE = (16, 16)
NUTRIENT_RAMP = (12, 13, 28)
WATER_RAMP = (29, 30, 31)


def nutrient_item():
    c = Canvas(1, ITEM_SIZE)
    c.ellipse(8, 8.5, 5.5, 4.8, light=0.3)
    c.outline()
    c.px(6, 6, 4)
    c.px(10, 10, 0)
    return c


def water_item():
    c = Canvas(1, ITEM_SIZE)
    c.ellipse(8, 10, 4.8, 4.3, light=0.3)
    for y in range(3, 7):
        half = (y - 3) * 0.8
        for x in range(int(8 - half), int(8 + half) + 1):
            c.put(x, y, 1 if x >= 8 else 2)
    c.outline()
    c.px(6, 8, 4)
    return c


# Minério solto (REQ-071, pedido de 2026-10-23: "a arte dos minérios soltos, igual fizemos com a água"): um torrão de pedra
# com pepitas do metal, o mesmo sprite na boca da formiga e no chão (AntFarmView desenha o item com ele). Desenhado direto em
# 8 x 8 (a redução de um 16 x 16 apagava as pepitas): . vazio, O contorno, S/B/L sombra, base e luz da pedra, d/m/h metal.
ORE_ROCK_RAMP = (17, 18, 19)
ORE_METALS = {'CopperOre': (8, 11, 12), 'IronOre': (32, 33, 34), 'GoldOre': (7, 13, 16)}
ORE_ITEM = (
    '........',
    '..OOO...',
    '.OLhBOO.',
    'OLBBmBBO',
    'OBmBBhSO',
    'OBBdSmSO',
    '.OSSSSO.',
    '..OOOO..',
)
ORE_TONES = {'O': 3, 'S': 0, 'B': 1, 'L': 2, 'm': 5, 'h': 6, 'd': 7}


def ore_item():
    c = Canvas(1, (8, 8))
    for y, row in enumerate(ORE_ITEM):
        for x, ch in enumerate(row):
            if ch != '.':
                c.put(x, y, ORE_TONES[ch])
    return c


def brood_and_items():
    out = []
    for f in range(2):
        out.append(('Egg_Idle_%d.png' % f, egg(f), EGG_RAMP, None))
        out.append(('Larva_Idle_%d.png' % f, larva(f), LARVA_RAMP, LARVA_EXTRA))
        out.append(('Pupa_Idle_%d.png' % f, pupa(f), PUPA_RAMP, PUPA_EXTRA))
    out.append(('Item_Nutrient.png', nutrient_item(), NUTRIENT_RAMP, None))
    out.append(('Item_Water.png', water_item(), WATER_RAMP, None))
    for name, (dark, base, light) in ORE_METALS.items():
        out.append(('Item_%s.png' % name, ore_item(), ORE_ROCK_RAMP, {5: base, 6: light, 7: dark}))
    return out


GROUPS = ('workers', 'queen', 'termite', 'aphids', 'brood')


def main():
    """Grava só os grupos pedidos. As operárias, as funções e a rainha foram refinadas depois (REQ-049, masters em
    Source~/Refined e bake-refined.ps1): gravar 'workers' ou 'queen' daqui apaga esse refinamento."""
    if len(sys.argv) < 4:
        sys.exit('uso: ant_sprites.py <MasterPalette.hex> <pasta Art/Ants> <grupos: %s> [prévia.png]' % ','.join(GROUPS))
    pal = load_palette(sys.argv[1])
    root = sys.argv[2]
    groups = set(sys.argv[3].split(','))
    unknown = groups - set(GROUPS)
    if unknown:
        sys.exit('grupos desconhecidos: %s' % ', '.join(sorted(unknown)))
    written = 0
    workers = frames()
    if 'workers' in groups:
        for folder, ramp in RAMPS.items():
            for name, canvas in workers:
                Image.fromarray(render(canvas, pal, ramp)).save(os.path.join(root, folder, name))
                written += 1
    if 'queen' in groups:
        for name, canvas in frames(2, True):
            Image.fromarray(render(canvas, pal, QUEEN_RAMP)).save(os.path.join(root, QUEEN_FOLDER, name))
            written += 1
    if 'termite' in groups:
        for name, canvas in termite_frames():
            Image.fromarray(render(canvas, pal, TERMITE_RAMP, TERMITE_EXTRA)).save(os.path.join(root, TERMITE_FOLDER, name))
            written += 1
    if 'aphids' in groups:
        aphids = [('Aphid_Walk_%d.png' % f, aphid_side(f)) for f in range(4)] + [('Aphid_Top_%d.png' % f, aphid_top(f)) for f in range(4)]
        for name, canvas in aphids:
            Image.fromarray(render(canvas, pal, APHID_RAMP, APHID_EXTRA)).save(os.path.join(root, APHID_FOLDER, name))
            written += 1
    brood = brood_and_items()
    if 'brood' in groups:
        for name, canvas, ramp, extra in brood:
            Image.fromarray(render(canvas, pal, ramp, extra)).save(os.path.join(root, name))
            written += 1
    if len(sys.argv) > 4:
        # prévia das crias e itens, ampliados 6x, com uma operária (do projeto, refinada) ao lado para comparar
        scale = 6
        sheet = Image.new('RGBA', ((48 + 4) * 4 * scale, (32 + 4) * 3 * scale), (70, 60, 52, 255))
        for i, (name, canvas, ramp, extra) in enumerate(brood):
            im = Image.fromarray(render(canvas, pal, ramp, extra))
            im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
            sheet.alpha_composite(im, ((i % 3) * (48 + 4) * scale, (i // 3) * (32 + 4) * scale))
        worker = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Ant_Walk_0.png')
        w = Image.open(worker).convert('RGBA').resize((48 * scale, 32 * scale), Image.NEAREST)
        sheet.alpha_composite(w, (3 * (48 + 4) * scale, 0))
        sheet.save(sys.argv[4])
    print('ok', written, 'quadros gravados')


if __name__ == '__main__':
    main()
