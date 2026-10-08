"""Gera marcadores de imagem para o rastreamento do MontAR (ARCore Image Tracking).

Produz duas saídas:
  1. PNG 1024x1024, que vai para Assets/Textures/ReferenceImages/ e entra na XRReferenceImageLibrary;
  2. PDF A4 para impressão, com o marcador no tamanho físico exato (padrão 15 cm).

O tamanho físico precisa ser o mesmo informado na XRReferenceImageLibrary ("Specify Size"),
senão o overlay aparece em escala errada. Imprimir sempre em 100%, sem "ajustar à página".

Uso (a partir da raiz do repositório):
  python tools/gerar_marcador.py --semente 20261005 --rotulo "MontAR A" --nome MontAR_Marcador_A

Depois de gerar, avaliar a qualidade com a ferramenta do ARCore (recomendado: nota >= 75):
  Library/PackageCache/com.unity.xr.arcore@<hash>/Tools~/Windows/arcoreimg.exe eval-img --input_image_path=<png>

Requer: pip install pillow
"""
import argparse
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

RAIZ = Path(__file__).resolve().parent.parent
PASTA_PNG = RAIZ / "Assets" / "Textures" / "ReferenceImages"
PASTA_PDF = RAIZ / "docs" / "documents" / "marcadores"
PALETA = ["black", (30, 30, 30), (200, 30, 30), (20, 90, 200), (240, 180, 0), (0, 140, 70)]


def fonte(tamanho):
    try:
        return ImageFont.truetype("arialbd.ttf", tamanho)
    except OSError:
        return ImageFont.load_default()


def desenhar_marcador(semente, rotulo, tamanho=1024):
    """Formas aleatórias (mas reprodutíveis pela semente), moldura, rótulo e triângulo de orientação.

    A moldura preta encosta na borda da imagem: assim o lado da moldura impressa é exatamente
    o tamanho físico declarado na XRReferenceImageLibrary (o ARCore mede a imagem inteira).
    """
    rnd = random.Random(semente)
    img = Image.new("RGB", (tamanho, tamanho), "white")
    d = ImageDraw.Draw(img)

    for _ in range(170):
        x, y = rnd.randint(90, tamanho - 90), rnd.randint(90, tamanho - 90)
        r = rnd.choice([8, 12, 18, 26, 38, 55])
        cor = rnd.choice(PALETA)
        tipo = rnd.random()
        if tipo < 0.3:
            d.ellipse([x - r, y - r, x + r, y + r], fill=cor)
        elif tipo < 0.65:
            n = rnd.randint(3, 6)
            a0 = rnd.random() * math.tau
            pontos = [(x + r * math.cos(a0 + i * math.tau / n) * rnd.uniform(0.6, 1.3),
                       y + r * math.sin(a0 + i * math.tau / n) * rnd.uniform(0.6, 1.3)) for i in range(n)]
            d.polygon(pontos, fill=cor)
        else:
            largura = rnd.randint(4, 12)
            a = rnd.random() * math.tau
            comprimento = r * 2.2
            d.line([x, y, x + comprimento * math.cos(a), y + comprimento * math.sin(a)], fill=cor, width=largura)

    # Moldura na borda da imagem (cobre as formas que encostam nela).
    d.rectangle([0, 0, tamanho - 1, tamanho - 1], outline="black", width=36)

    # Rótulo no canto inferior esquerdo, com caixa ajustada ao texto.
    f = fonte(96)
    x0, y0, x1, y1 = d.textbbox((0, 0), rotulo, font=f)
    pad = 22
    bx, by = 95, tamanho - 95 - (y1 - y0) - 2 * pad
    d.rectangle([bx, by, bx + (x1 - x0) + 2 * pad, by + (y1 - y0) + 2 * pad], fill="white", outline="black", width=10)
    d.text((bx + pad - x0, by + pad - y0), rotulo, fill="black", font=f)

    # Triângulo sólido no canto superior esquerdo: indica a orientação do marcador.
    d.polygon([(70, 70), (250, 70), (70, 250)], fill="black")
    return img


def pagina_a4(marcador, nome, lado_cm, dpi=300):
    """Página A4 com o marcador centralizado no tamanho físico exato e instruções de impressão."""
    a4 = (round(21.0 / 2.54 * dpi), round(29.7 / 2.54 * dpi))
    lado_px = round(lado_cm / 2.54 * dpi)
    pagina = Image.new("RGB", a4, "white")
    x = (a4[0] - lado_px) // 2
    y = round(4.0 / 2.54 * dpi)
    pagina.paste(marcador.resize((lado_px, lado_px), Image.LANCZOS), (x, y))

    d = ImageDraw.Draw(pagina)
    f = fonte(48)
    # Marcas de corte nos cantos, 5 mm afastadas do marcador.
    gap, comp = round(0.5 / 2.54 * dpi), round(1.0 / 2.54 * dpi)
    for cx, cy, sx, sy in [(x, y, -1, -1), (x + lado_px, y, 1, -1), (x, y + lado_px, -1, 1), (x + lado_px, y + lado_px, 1, 1)]:
        d.line([cx + sx * gap, cy, cx + sx * (gap + comp), cy], fill="black", width=3)
        d.line([cx, cy + sy * gap, cx, cy + sy * (gap + comp)], fill="black", width=3)

    texto = [
        f"{nome}  |  lado do marcador: {lado_cm:g} cm (borda externa da moldura preta)",
        "Imprimir em 100% (tamanho real), sem \"ajustar à página\".",
        "Conferir com régua depois de imprimir. Colar sobre superfície plana e fosca.",
    ]
    ty = y + lado_px + gap + comp + 60
    for linha in texto:
        d.text((x, ty), linha, fill="black", font=f)
        ty += 70
    return pagina


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--semente", type=int, required=True, help="semente das formas (mesma semente = mesmo marcador)")
    p.add_argument("--rotulo", required=True, help='texto impresso no marcador, ex.: "MontAR A"')
    p.add_argument("--nome", required=True, help="nome do arquivo e da imagem na biblioteca, ex.: MontAR_Marcador_A")
    p.add_argument("--lado-cm", type=float, default=15.0, help="lado físico do marcador impresso (padrão 15 cm)")
    args = p.parse_args()

    marcador = desenhar_marcador(args.semente, args.rotulo)
    PASTA_PNG.mkdir(parents=True, exist_ok=True)
    PASTA_PDF.mkdir(parents=True, exist_ok=True)
    png = PASTA_PNG / f"{args.nome}.png"
    pdf = PASTA_PDF / f"{args.nome}_{args.lado_cm:g}cm_A4.pdf"
    marcador.save(png)
    pagina_a4(marcador, args.nome, args.lado_cm).save(pdf, resolution=300)
    print(f"PNG: {png}\nPDF: {pdf}")


if __name__ == "__main__":
    main()
