from pathlib import Path
import urllib.request, struct, json, string, zipfile
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent
base='https://raw.githubusercontent.com/eifetx/Pixelify-Sans/main/'
sources={
    'PixelifySans-Regular.ttf':base+'fonts/ttf/PixelifySans-Regular.ttf',
    'PixelifySans-Bold.ttf':base+'fonts/ttf/PixelifySans-Bold.ttf',
    'OFL.txt':base+'OFL.txt',
}
for name,url in sources.items():
    with urllib.request.urlopen(url,timeout=30) as response: (root/name).write_bytes(response.read())

def has_glyph(data,code):
    u16=lambda p:struct.unpack_from('>H',data,p)[0]
    i16=lambda p:struct.unpack_from('>h',data,p)[0]
    u32=lambda p:struct.unpack_from('>I',data,p)[0]
    cmap=None
    for i in range(u16(4)):
        p=12+16*i
        if data[p:p+4]==b'cmap': cmap=u32(p+8);break
    assert cmap is not None
    for i in range(u16(cmap+2)):
        p=cmap+4+8*i; platform=u16(p); enc=u16(p+2)
        if platform not in (0,3):continue
        sub=cmap+u32(p+4);fmt=u16(sub)
        if fmt==12:
            for j in range(u32(sub+12)):
                start,end,glyph=struct.unpack_from('>III',data,sub+16+12*j)
                if start<=code<=end and glyph+code-start!=0:return True
        elif fmt==4 and code<=65535:
            count=u16(sub+6)//2
            for j in range(count):
                end=u16(sub+14+2*j);start=u16(sub+16+2*count+2*j)
                if start<=code<=end:
                    delta=i16(sub+16+4*count+2*j)
                    rp=sub+16+6*count+2*j; offset=u16(rp)
                    if offset==0:glyph=(code+delta)%65536
                    else:
                        glyph=u16(rp+offset+2*(code-start))
                        if glyph:glyph=(glyph+delta)%65536
                    if glyph:return True
    return False

accented='\u00e1\u00e0\u00e2\u00e3\u00e9\u00ea\u00ed\u00f3\u00f4\u00f5\u00fa\u00fc\u00e7'
required=accented+accented.upper()+'\u00aa\u00ba\u00b0\u00d7'
chars=''.join(chr(i) for i in range(32,127))+required
chars=''.join(dict.fromkeys(chars))
(root/'Caracteres-PTBR.txt').write_text(chars,encoding='utf-8')
report=[]
for name in sources:
    if name.endswith('.ttf'):
        data=(root/name).read_bytes()
        missing=[c for c in chars if not has_glyph(data,ord(c))]
        report.append({'file':name,'source':sources[name],'tested_characters':len(chars),'missing':missing})
        assert not missing,(name,missing)
(root/'Verificacao.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')

preview=Image.new('RGB',(640,280),'#f6edcf');draw=ImageDraw.Draw(preview)
samples=[('Col\u00f4nia  |  Evolu\u00e7\u00e3o  |  Ber\u00e7\u00e1rio',16),('Res\u00edduos  |  Configura\u00e7\u00f5es  |  Pr\u00f3xima cria',16),(accented+' '+accented.upper(),16),('428,50 N   +2,40 N/s   18 / 28',16),('COL\u00d4NIA PAUSADA',32)]
y=15
for text,size in samples:
    font=ImageFont.truetype(str(root/('PixelifySans-Bold.ttf' if size==32 else 'PixelifySans-Regular.ttf')),size)
    mask=Image.new('1',preview.size)
    ImageDraw.Draw(mask).text((15,y),text,font=font,fill=1)
    preview.paste('#382819',(0,0),mask)
    y+=44 if size==16 else 50
preview.resize((1280,560),Image.Resampling.NEAREST).save(root/'Previa-Portugues.png')
print(json.dumps(report,ensure_ascii=True))
