"""Monta as imagens do glossário feitas com os sprites do jogo (objetos e formigas).

Cada imagem é 640x360: o fundo é a foto da sala iluminada (light.png, desta pasta) e os sprites entram ampliados por
um fator inteiro (Point, sem suavizar), centrados e apoiados numa linha de chão. Rode da raiz do projeto:

    python "Assets/Project/Art/UI/Glossary/Source~/compose_sprites.py"

Grava em Assets/Project/Art/UI/Glossary/<entrada>.png (o nome é o id da entrada em GlossaryContent).
"""
import os
from PIL import Image, ImageEnhance

ART = 'Assets/Project/Art'
OUT = os.path.join(ART, 'UI/Glossary')
HERE = os.path.join(OUT, 'Source~')
ATLAS = os.path.join(ART, 'Terrain/TerrainAtlas.png')
ATLAS_ROW = 128  # 8x8 células de 16 px por material, linhas de baixo para cima na ordem de CellMaterial
FLOOR_Y = 300    # linha de chão das imagens (piso da sala da foto de fundo)


def background():
    bg = Image.open(os.path.join(OUT, 'light.png')).convert('RGBA')
    return ImageEnhance.Brightness(bg).enhance(0.85)


def sprite(path, scale, tint=None):
    im = Image.open(os.path.join(ART, path)).convert('RGBA')
    if tint is not None:
        r, g, b, a = im.split()
        r = r.point(lambda v: int(v * tint[0]))
        g = g.point(lambda v: int(v * tint[1]))
        b = b.point(lambda v: int(v * tint[2]))
        im = Image.merge('RGBA', (r, g, b, a))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def atlas_tile(material, cells=2, scale=4):
    """Recorte de cells x cells células da linha do material no atlas, ampliado."""
    atlas = Image.open(ATLAS).convert('RGBA')
    top = atlas.height - (material + 1) * ATLAS_ROW
    tile = atlas.crop((0, top, cells * 16, top + cells * 16))
    return tile.resize((tile.width * scale, tile.height * scale), Image.NEAREST)


def row(images, gap=24, floor=FLOOR_Y, name=None):
    """Põe as imagens lado a lado, centradas, com a base na linha de chão."""
    bg = background()
    width = sum(i.width for i in images) + gap * (len(images) - 1)
    x = (bg.width - width) // 2
    for im in images:
        bg.alpha_composite(im, (x, floor - im.height))
        x += im.width + gap
    bg.convert('RGB').save(os.path.join(OUT, name + '.png'))
    print(name)


# Material -> índice em CellMaterial (World/CellMaterial.cs).
DIRT, SAND, ROCK, PLANT_GRASS, PLANT_LEAF, RUBBLE = 1, 3, 4, 10, 11, 14

ROLES = {
    'worker': 'Operaria', 'carrier': 'Coletora', 'cutter': 'Cortadeira', 'digger': 'Escavadora',
    'gardener': 'Jardineira', 'nurse': 'Cuidadora', 'soldier': 'Soldado', 'spitter': 'Cuspideira',
    'midden': 'Lixeira', 'farmer': 'Fazendeira',
}

if __name__ == '__main__':
    row([sprite('Ants/Item_Nutrient.png', 6)] * 3, gap=40, floor=230, name='nutrients')
    row([sprite('Ants/Egg_Idle_0.png', 3), sprite('Ants/Larva_Idle_0.png', 3), sprite('Ants/Pupa_Idle_0.png', 3)],
        gap=16, floor=245, name='brood')
    for entry, folder in ROLES.items():
        row([sprite('Ants/%s/Ant_Walk_0.png' % folder, 5)], floor=290, name=entry)
    row([sprite('Ants/Rainha/Ant_Walk_0.png', 4)], floor=300, name='queen')
    row([sprite('Ants/InimigaAlada/Ant_Fly_0.png', 4), sprite('Ants/InimigaAlada/Ant_Walk_0.png', 4)],
        gap=16, floor=290, name='raids')
    # Pulgão preto: a mesma arte tingida com a cor do AntSwarmView (_blackAphidColor), ao lado de um saudável.
    row([sprite('Aphids/Aphid_Walk_0.png', 6), sprite('Aphids/Aphid_Walk_0.png', 6, tint=(0.16, 0.14, 0.18))],
        gap=48, floor=290, name='blackaphid')
    row([atlas_tile(DIRT), atlas_tile(SAND), atlas_tile(ROCK)], gap=32, floor=250, name='soil')
    row([sprite('Ants/Item_Water.png', 6)] * 3, gap=40, floor=230, name='water')
    row([atlas_tile(RUBBLE), atlas_tile(RUBBLE)], gap=0, floor=250, name='rubble')
    row([atlas_tile(PLANT_GRASS), atlas_tile(PLANT_LEAF)], gap=32, floor=250, name='plants')
