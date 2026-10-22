import sys, numpy as np
from PIL import Image
# Saída bruta do Qwen -> paleta das pedras com difusão de erro, silhueta da máscara e contorno 1B1815.
raw, mask, out, W, H, k = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
PAL = '1B1815 382414 463123 785A40 9A7F5E B9986D CFB691 E4D2AE 504D55 7D7676 A59685 BBB29F'.split()
cols = [tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) for h in PAL]
pimg = Image.new('P', (1, 1)); flat = [v for c in cols for v in c]; pimg.putpalette(flat + flat[:3] * (256 - len(cols)))
src = Image.open(raw).convert('RGB').resize((W, H), Image.BOX)
q = np.array(src.quantize(palette=pimg, dither=Image.Dither.FLOYDSTEINBERG).convert('RGB'))
alpha = np.array(Image.open(mask).convert('RGBA').resize((W, H), Image.NEAREST))[..., 3] > 0
a = np.zeros((H, W, 4), np.uint8); a[..., :3] = q; a[..., 3] = alpha * 255
# contorno de 1 px de arte por dentro da silhueta
op = np.pad(alpha, 1)
edge = (~np.roll(op, 1, 0) | ~np.roll(op, -1, 0) | ~np.roll(op, 1, 1) | ~np.roll(op, -1, 1))[1:-1, 1:-1] & alpha
a[edge] = (0x1B, 0x18, 0x15, 255)
Image.fromarray(a).resize((W * k, H * k), Image.NEAREST).save(out)
