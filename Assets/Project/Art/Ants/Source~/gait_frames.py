"""Caminhada procedural das formigas em 6 quadros: marcha em tripé, balanço do corpo e antenas (2026-10-24).

O corpo de cada papel vem de Gait/<Pasta>_<Walk|Top>_Body.png (os pixels que eram iguais nos quatro quadros antigos);
as patas são redesenhadas aqui, por baixo do corpo (folha, chapéu e gáster grande ficam por cima delas). Na primeira
execução de um papel, sem os masters, eles são extraídos dos Ant_*_0..3 atuais da pasta dele: rode antes de trocar os
quadros, ou restaure os quatro antigos pelo Git.

Todos os papéis têm as patas no mesmo lugar da operária; o que sobra das patas antigas no corpo fixo é tirado pelas
posições dos tocos da operária. As pontas das antenas só balançam nos papéis em que estão onde as da operária estão.

Uso: python -I gait_frames.py [Pasta ...] [--sheet prancha.png]   (sem pasta = todos os papéis)

O balanço sobe o corpo 1 texel nos quadros 1 e 4 da vista lateral; o AntFarmSceneSetup mede isso (linha mais alta
opaca contra o quadro 0) e o AntSwarmView sobe junto o equipamento e o item carregado.
"""
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ANTS = os.path.join(HERE, "..")
MASTERS = os.path.join(HERE, "Gait")
ROLES = ["Operaria", "Jardineira", "Coletora", "Cortadeira", "Escavadora", "Cuidadora", "Soldado", "Cuspideira",
         "Lixeira", "Fazendeira"]
N = 6
INK = (27, 24, 21, 255)
CLEAR = (0, 0, 0, 0)

# Passo de 4 texels (±2): no apoio o pé anda 2 texels para trás por quadro; no balanço volta erguido em 3 quadros.
STEP_DX = [2, 0, -2, -1, 1, 2]
STEP_LIFT = [0, 0, 0, 1, 1, 1]
BOB_FRAMES = (1, 4)  # meio do apoio de cada tripé: patas a prumo, corpo 1 texel mais alto
ANTENNA_SWAY = [0, 0, -1, -1, 0, 0]


def stable(frames):
    """Pixels iguais em todos os quadros (o corpo, sem as patas que se mexem)."""
    w, h = frames[0].size
    out = Image.new("RGBA", (w, h), CLEAR)
    for y in range(h):
        for x in range(w):
            p = frames[0].getpixel((x, y))
            if p[3] and all(f.getpixel((x, y)) == p for f in frames):
                out.putpixel((x, y), p)
    return out


def body(role, view):
    path = os.path.join(MASTERS, f"{role}_{view}_Body.png")
    if not os.path.exists(path):
        os.makedirs(MASTERS, exist_ok=True)
        frames = [Image.open(os.path.join(ANTS, role, f"Ant_{view}_{i}.png")).convert("RGBA") for i in range(4)]
        stable(frames).save(path)
    return Image.open(path).convert("RGBA")


def pixels(img, pred=lambda x, y: True):
    return {(x, y): img.getpixel((x, y)) for y in range(img.height) for x in range(img.width)
            if img.getpixel((x, y))[3] and pred(x, y)}


def top_core(x, y):
    """Contorno do corpo da operária vista de cima (gáster, pecíolo, tórax e cabeça); fora disso, x < 19, são patas."""
    return 6 <= y <= 9 or (y == 5 and 3 <= x <= 6) or (y == 10 and 2 <= x <= 7)


# Referência da operária: tocos das patas antigas que ficaram no corpo fixo e as pontas das antenas.
_ref_walk, _ref_top = body("Operaria", "Walk"), body("Operaria", "Top")
WALK_STUBS = pixels(_ref_walk, lambda x, y: y >= 13 and x >= 8)
TOP_STUBS = pixels(_ref_top, lambda x, y: x < 19 and not top_core(x, y))
WALK_TIPS = pixels(_ref_walk, lambda x, y: y <= 4)
TOP_TIPS = pixels(_ref_top, lambda x, y: x >= 22 and (y <= 5 or y >= 10))


def without(img, stubs):
    """Tira os tocos das patas antigas que estão onde estavam na operária (mesma cor); o resto do papel fica."""
    out = img.copy()
    for (x, y), p in stubs.items():
        if out.getpixel((x, y)) == p:
            out.putpixel((x, y), CLEAR)
    return out


def tips_match(img, tips, region):
    """As pontas estão onde as da operária estão e não há mais nada na região em volta delas (a folha, o chapéu)."""
    have = pixels(img, region)
    return have == tips


def line(img, a, b):
    (x0, y0), (x1, y1) = a, b
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        if 0 <= x0 < img.width and 0 <= y0 < img.height:
            img.putpixel((x0, y0), INK)
        if (x0, y0) == (x1, y1):
            return
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def phase(group, f):
    i = (f + (0 if group == 0 else N // 2)) % N
    return STEP_DX[i], STEP_LIFT[i]


def shifted(img, dx, dy, pred):
    """Move por (dx, dy) os pixels que satisfazem pred; os demais ficam."""
    out = img.copy()
    moved = []
    for y in range(img.height):
        for x in range(img.width):
            p = img.getpixel((x, y))
            if p[3] and pred(x, y):
                out.putpixel((x, y), CLEAR)
                moved.append((x + dx, y + dy, p))
    for x, y, p in moved:
        if 0 <= x < img.width and 0 <= y < img.height:
            out.putpixel((x, y), p)
    return out


def side(base, sway, f):
    b = without(base, WALK_STUBS)
    if sway:  # pontas das antenas recuam e voltam; só na horizontal, para não confundir a medida do balanço
        b = shifted(b, ANTENNA_SWAY[f], 0, lambda x, y: (x, y) in WALK_TIPS)
    up = 1 if f in BOB_FRAMES else 0
    b = shifted(b, 0, -up, lambda x, y: True)
    legs = Image.new("RGBA", b.size, CLEAR)
    hip_y = 12 - up
    # (quadril x, pé da de perto e da de longe, tripé da de perto); a de longe, 1 texel atrás, é do outro tripé.
    # O pé de trás fica longe do gáster, para não virar um borrão embaixo dele.
    for hx, feet, g in ((10, (-2, -1), 0), (14, (1, 1), 1), (18, (2, 2), 0)):
        for far in (1, 0):
            dx, lift = phase(g ^ far, f)
            hx2 = hx - far
            fx, fy = hx2 + feet[far] + dx, 15 - lift
            kx = hx2 + round((fx - hx2) * 0.35)
            line(legs, (hx2, hip_y), (kx, hip_y + 1))
            line(legs, (kx, hip_y + 1), (fx, fy))
    legs.alpha_composite(b)
    return legs


def top(base, sway, f):
    b = without(base, TOP_STUBS)
    if sway:  # pontas das antenas recuam 1 texel e voltam (para os lados elas se soltavam da base)
        b = shifted(b, ANTENNA_SWAY[f], 0, lambda x, y: (x, y) in TOP_TIPS)
    legs = Image.new("RGBA", b.size, CLEAR)
    # (quadril x, joelho dx, pé dx) de trás, do meio e da frente; lado de cima (y 6) e de baixo (y 9).
    for side_i, (hip_y, sgn) in enumerate(((6, -1), (9, 1))):
        for li, (hx, kdx, fdx) in enumerate(((11, -2, -4), (13, 0, -1), (15, 2, 4))):
            dx, lift = phase((li % 2) ^ side_i, f)  # tripé: frente e trás de um lado com o meio do outro
            fx, fy = hx + fdx + dx, hip_y + sgn * (5 - lift)
            kx, ky = hx + kdx + (1 if dx > 0 else 0), hip_y + sgn * 3
            line(legs, (hx, hip_y + sgn), (kx, ky))
            line(legs, (kx, ky), (fx, fy))
    legs.alpha_composite(b)
    return legs


def thick_line(img, a, b, width):
    """Pata de `width` texels: a mesma linha repetida para a direita (de lado) ou para baixo (de cima)."""
    for k in range(width):
        line(img, (a[0] + k, a[1]), (b[0] + k, b[1]))


# Rainha (64 × 32, patas de 2 texels): o dobro do passo e da erguida, com as próprias posições de quadril e pé.
QUEEN_STEP = 2


def queen_phase(group, f):
    dx, lift = phase(group, f)
    return dx * QUEEN_STEP, lift * QUEEN_STEP


def side_queen(base, f):
    # Abaixo do tórax só há patas: tira os tocos fixos das antigas (a cabeça, à direita de x 43, fica).
    b = shifted(base, 0, 0, lambda x, y: False)
    for (x, y) in list(pixels(b, lambda x, y: (y >= 22 and x >= 31) or (y == 21 and x >= 39))):
        b.putpixel((x, y), CLEAR)
    up = 1 if f in BOB_FRAMES else 0
    b = shifted(b, 0, -up, lambda x, y: True)
    legs = Image.new("RGBA", b.size, CLEAR)
    hip_y = 21 - up
    # (quadril x, pé x, tripé), só as três de perto, como nos quadros antigos: com as de longe, o tórax curto da
    # rainha virava um feixe de patas.
    for hx, foot, g in ((34, 31, 0), (38, 41, 1), (42, 48, 0)):
        dx, lift = queen_phase(g, f)
        fx, fy = foot + dx, 31 - lift
        kx, ky = hx + round((fx - hx) * 0.4), hip_y + 3
        thick_line(legs, (hx, hip_y), (kx, ky), 2)
        thick_line(legs, (kx, ky), (fx, fy), 2)
    legs.alpha_composite(b)
    return legs


def top_queen(base, f):
    b = base.copy()
    # Patas antigas: tudo do tórax para fora (x 31–40, acima de 10 e abaixo de 19).
    for (x, y) in list(pixels(b, lambda x, y: 31 <= x <= 40 and (y <= 10 or y >= 20))):
        b.putpixel((x, y), CLEAR)
    # Pontas das antenas (x >= 50, à frente da cabeça) recuam 1 texel e voltam.
    b = shifted(b, ANTENNA_SWAY[f], 0, lambda x, y: x >= 50 and (y <= 9 or y >= 21))
    legs = Image.new("RGBA", b.size, CLEAR)
    # (quadril x, joelho dx, pé dx); lado de cima (y 11) e de baixo (y 18), espelhados.
    for side_i, (hip_y, sgn) in enumerate(((11, -1), (18, 1))):
        for li, (hx, kdx, fdx) in enumerate(((33, -3, -6), (37, 0, -1), (41, 3, 6))):
            dx, lift = queen_phase((li % 2) ^ side_i, f)
            fx, fy = hx + fdx + dx, hip_y + sgn * (8 - lift)
            kx, ky = hx + kdx + (1 if dx > 0 else 0), hip_y + sgn * 5
            thick_line(legs, (hx, hip_y), (kx, ky), 2)
            thick_line(legs, (kx, ky), (fx, fy), 2)
    legs.alpha_composite(b)
    return legs


def bake_queen():
    role = "Rainha"
    walk_body, top_body = body(role, "Walk"), body(role, "Top")
    walk = [side_queen(walk_body, f) for f in range(N)]
    tops = [top_queen(top_body, f) for f in range(N)]
    for i in range(N):
        walk[i].save(os.path.join(ANTS, role, f"Ant_Walk_{i}.png"))
        tops[i].save(os.path.join(ANTS, role, f"Ant_Top_{i}.png"))
    print(f"{role}: {N}+{N} quadros")
    return walk, tops


def bake(role):
    if role == "Rainha":
        return bake_queen()
    walk_body, top_body = body(role, "Walk"), body(role, "Top")
    walk_sway = tips_match(without(walk_body, WALK_STUBS), WALK_TIPS, lambda x, y: y <= 4 and x >= 17)
    top_sway = tips_match(without(top_body, TOP_STUBS), TOP_TIPS, lambda x, y: x >= 22 and (y <= 5 or y >= 10))
    walk = [side(walk_body, walk_sway, f) for f in range(N)]
    tops = [top(top_body, top_sway, f) for f in range(N)]
    for i in range(N):
        walk[i].save(os.path.join(ANTS, role, f"Ant_Walk_{i}.png"))
        tops[i].save(os.path.join(ANTS, role, f"Ant_Top_{i}.png"))
    print(f"{role}: {N}+{N} quadros (antenas balançando: lado {'sim' if walk_sway else 'não'}, "
          f"cima {'sim' if top_sway else 'não'})")
    return walk, tops


def main():
    args = sys.argv[1:]
    sheet_path = None
    if "--sheet" in args:
        sheet_path = args[args.index("--sheet") + 1]
        args = args[:args.index("--sheet")] + args[args.index("--sheet") + 2:]
    roles = args or ROLES
    rows = []
    for role in roles:
        walk, tops = bake(role)
        rows += [walk, tops]
    if sheet_path:
        s, pad, bg = 6, 6, (88, 68, 50, 255)
        sheet = Image.new("RGBA", (N * (24 * s + pad) + pad, len(rows) * (16 * s + pad) + pad), bg)
        for r, frames in enumerate(rows):
            for i, im in enumerate(frames):
                sheet.alpha_composite(im.resize((24 * s, 16 * s), Image.NEAREST),
                                      (pad + i * (24 * s + pad), pad + r * (16 * s + pad)))
        sheet.save(sheet_path)


if __name__ == "__main__":
    main()
