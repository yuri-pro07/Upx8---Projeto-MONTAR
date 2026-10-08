"""Gera os sprites da interface do MontAR em Assets/Textures/UI.

Tudo em branco sobre fundo transparente: a cor vem do Image da Unity (campo Color).
  - ui_arredondado.png: retângulo arredondado para 9-slice (borda de 48 px), base de cartões e botões;
  - ui_circulo.png e ui_check.png: pontos, caixas de marcar e o sinal de concluído;
  - icone_*.png: ícones da apresentação (Aponta, Guia, Valida, Registra) e do modo 3D;
  - peca_*.png: um ícone por peça do catálogo.

Os desenhos são feitos em 4x e reduzidos (antisserrilhado). Depois de gerar, rodar na Unity:
MontAR > App > Reconstruir interface e cena (configura os sprites e o 9-slice).

Uso (a partir da raiz do repositório):
  python tools/gerar_icones_ui.py

Requer: pip install pillow
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

RAIZ = Path(__file__).resolve().parent.parent
PASTA = RAIZ / "Assets" / "Textures" / "UI"
ESCALA = 4
BRANCO = (255, 255, 255, 255)


def tela(tamanho):
    img = Image.new("RGBA", (tamanho * ESCALA, tamanho * ESCALA), (255, 255, 255, 0))
    return img, ImageDraw.Draw(img)


def salvar(img, nome, tamanho):
    img.resize((tamanho, tamanho), Image.LANCZOS).save(PASTA / nome)
    return nome


def s(v):
    """Coordenada do desenho (em px do ícone final) para a tela 4x."""
    return v * ESCALA


def caixa(x0, y0, x1, y1):
    return [s(x0), s(y0), s(x1), s(y1)]


def linha(d, pontos, largura):
    d.line([(s(x), s(y)) for x, y in pontos], fill=BRANCO, width=s(largura), joint="curve")
    for x, y in (pontos[0], pontos[-1]):
        r = largura / 2
        d.ellipse(caixa(x - r, y - r, x + r, y + r), fill=BRANCO)


def contorno(d, x0, y0, x1, y1, raio, largura):
    d.rounded_rectangle(caixa(x0, y0, x1, y1), radius=s(raio), outline=BRANCO, width=s(largura))


def seta_baixo(d, cx, topo, base, largura, ponta):
    linha(d, [(cx, topo), (cx, base - 2)], largura)
    d.polygon([(s(cx - ponta), s(base - ponta)), (s(cx + ponta), s(base - ponta)), (s(cx), s(base + ponta * 0.35))], fill=BRANCO)


# ------------------------------------------------------------------ primitivas da UI

def ui_arredondado():
    img, d = tela(128)
    d.rounded_rectangle(caixa(0, 0, 127.9, 127.9), radius=s(48), fill=BRANCO)
    return salvar(img, "ui_arredondado.png", 128)


def ui_circulo():
    img, d = tela(128)
    d.ellipse(caixa(0, 0, 127.9, 127.9), fill=BRANCO)
    return salvar(img, "ui_circulo.png", 128)


def ui_check():
    img, d = tela(128)
    linha(d, [(26, 66), (52, 92), (102, 38)], 16)
    return salvar(img, "ui_check.png", 128)


# ------------------------------------------------------------------ apresentação

def icone_aponta():
    img, d = tela(256)
    # Celular com cantos do visor e um alvo no centro.
    contorno(d, 70, 22, 186, 234, 22, 10)
    for cx, cy, dx, dy in ((96, 66, 1, 1), (160, 66, -1, 1), (96, 170, 1, -1), (160, 170, -1, -1)):
        linha(d, [(cx, cy + 22 * dy), (cx, cy), (cx + 22 * dx, cy)], 8)
    d.ellipse(caixa(112, 102, 144, 134), outline=BRANCO, width=s(8))
    d.ellipse(caixa(123, 113, 133, 123), fill=BRANCO)
    linha(d, [(110, 212), (146, 212)], 8)
    return salvar(img, "icone_aponta.png", 256)


def cubo(d, cx, cy, r, largura):
    # Cubo isométrico em arame.
    topo = [(cx, cy - r), (cx + r * 0.87, cy - r * 0.5), (cx, cy), (cx - r * 0.87, cy - r * 0.5)]
    linha(d, topo + [topo[0]], largura)
    linha(d, [(cx - r * 0.87, cy - r * 0.5), (cx - r * 0.87, cy + r * 0.5), (cx, cy + r), (cx + r * 0.87, cy + r * 0.5), (cx + r * 0.87, cy - r * 0.5)], largura)
    linha(d, [(cx, cy), (cx, cy + r)], largura)


def icone_guia():
    img, d = tela(256)
    cubo(d, 128, 164, 62, 10)
    seta_baixo(d, 128, 18, 76, 10, 16)
    return salvar(img, "icone_guia.png", 256)


def icone_valida():
    img, d = tela(256)
    d.ellipse(caixa(30, 30, 226, 226), outline=BRANCO, width=s(12))
    linha(d, [(82, 132), (114, 164), (176, 98)], 16)
    return salvar(img, "icone_valida.png", 256)


def icone_registra():
    img, d = tela(256)
    contorno(d, 46, 26, 210, 230, 18, 10)
    for i, y in enumerate((76, 128, 180)):
        linha(d, [(72, y), (88, y + 14), (110, y - 10)], 9)
        linha(d, [(128, y + 2), (184, y + 2)], 9)
    return salvar(img, "icone_registra.png", 256)


def icone_cubo():
    img, d = tela(256)
    cubo(d, 128, 130, 92, 12)
    return salvar(img, "icone_cubo.png", 256)


def icone_camera():
    img, d = tela(256)
    contorno(d, 28, 70, 228, 206, 22, 11)
    d.rounded_rectangle(caixa(88, 46, 168, 76), radius=s(10), fill=BRANCO)
    d.ellipse(caixa(88, 98, 168, 178), outline=BRANCO, width=s(11))
    return salvar(img, "icone_camera.png", 256)


# ------------------------------------------------------------------ peças

def peca_placa_mae():
    img, d = tela(256)
    contorno(d, 30, 22, 226, 234, 12, 10)
    contorno(d, 76, 58, 136, 118, 6, 9)            # soquete
    for x in (160, 176, 192, 208):                    # slots DIMM
        linha(d, [(x, 50), (x, 140)], 7)
    linha(d, [(62, 164), (172, 164)], 9)              # PCIe
    linha(d, [(62, 198), (172, 198)], 9)
    d.rectangle(caixa(36, 52, 50, 150), fill=BRANCO)  # I/O
    return salvar(img, "peca_placa_mae.png", 256)


def peca_cpu():
    img, d = tela(256)
    contorno(d, 58, 58, 198, 198, 14, 11)
    contorno(d, 92, 92, 164, 164, 6, 8)
    for i in range(5):
        t = 82 + i * 23
        linha(d, [(t, 24), (t, 50)], 7)
        linha(d, [(t, 206), (t, 232)], 7)
        linha(d, [(24, t), (50, t)], 7)
        linha(d, [(206, t), (232, t)], 7)
    d.polygon([(s(70), s(186)), (s(70), s(158)), (s(98), s(186))], fill=BRANCO)
    return salvar(img, "peca_cpu.png", 256)


def peca_cooler():
    img, d = tela(256)
    contorno(d, 26, 26, 230, 230, 22, 10)
    d.ellipse(caixa(48, 48, 208, 208), outline=BRANCO, width=s(8))
    d.ellipse(caixa(112, 112, 144, 144), fill=BRANCO)
    for k in range(5):
        a = k * 2 * math.pi / 5
        pts = []
        for t in range(9):
            u = t / 8
            ang = a + u * 1.0
            r = 22 + u * 52
            pts.append((128 + r * math.cos(ang), 128 + r * math.sin(ang)))
        linha(d, pts, 10)
    for x, y in ((44, 44), (212, 44), (44, 212), (212, 212)):
        d.ellipse(caixa(x - 7, y - 7, x + 7, y + 7), fill=BRANCO)
    return salvar(img, "peca_cooler.png", 256)


def peca_ram():
    img, d = tela(256)
    # Pente na horizontal, chanfro fora do centro e contatos embaixo.
    d.rounded_rectangle(caixa(14, 82, 242, 168), radius=s(8), outline=BRANCO, width=s(9))
    for x in (34, 82, 130, 178):
        d.rectangle(caixa(x, 100, x + 36, 134), fill=BRANCO)
    d.rectangle(caixa(150, 156, 166, 176), fill=(255, 255, 255, 0))
    for x in range(26, 236, 12):
        if 146 <= x <= 170:
            continue
        linha(d, [(x, 150), (x, 160)], 4)
    return salvar(img, "peca_ram.png", 256)


def peca_ssd():
    img, d = tela(256)
    d.rounded_rectangle(caixa(34, 96, 222, 160), radius=s(6), outline=BRANCO, width=s(9))
    d.rectangle(caixa(26, 112, 44, 144), fill=BRANCO)           # contatos
    d.rectangle(caixa(26, 124, 40, 132), fill=(255, 255, 255, 0))  # chave M
    d.rectangle(caixa(70, 112, 118, 144), fill=BRANCO)
    d.rectangle(caixa(130, 112, 178, 144), fill=BRANCO)
    d.ellipse(caixa(200, 116, 224, 140), outline=BRANCO, width=s(7))  # meia-lua do parafuso
    return salvar(img, "peca_ssd.png", 256)


def peca_gpu():
    img, d = tela(256)
    contorno(d, 34, 58, 238, 178, 14, 10)
    d.rectangle(caixa(16, 46, 30, 210), fill=BRANCO)            # espelho
    for cx in (92, 180):
        d.ellipse(caixa(cx - 38, 80, cx + 38, 156), outline=BRANCO, width=s(8))
        d.ellipse(caixa(cx - 9, 109, cx + 9, 127), fill=BRANCO)
    d.rectangle(caixa(70, 182, 196, 200), fill=BRANCO)          # contatos PCIe
    return salvar(img, "peca_gpu.png", 256)


def peca_fonte():
    img, d = tela(256)
    contorno(d, 22, 54, 196, 202, 14, 10)
    d.ellipse(caixa(52, 76, 156, 180), outline=BRANCO, width=s(8))
    for i in range(-2, 3):
        linha(d, [(104 + i * 18, 84), (104 + i * 18, 172)], 5)
    linha(d, [(196, 110), (224, 110), (224, 196), (236, 196)], 9)
    d.rectangle(caixa(228, 180, 246, 214), fill=BRANCO)
    return salvar(img, "peca_fonte.png", 256)


def main():
    PASTA.mkdir(parents=True, exist_ok=True)
    gerados = [
        ui_arredondado(), ui_circulo(), ui_check(),
        icone_aponta(), icone_guia(), icone_valida(), icone_registra(), icone_cubo(), icone_camera(),
        peca_placa_mae(), peca_cpu(), peca_cooler(), peca_ram(), peca_ssd(), peca_gpu(), peca_fonte(),
    ]
    for nome in gerados:
        print(PASTA / nome)


if __name__ == "__main__":
    main()
