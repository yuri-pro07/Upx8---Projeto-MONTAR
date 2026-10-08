"""Gera as texturas da bancada de montagem a partir de Assets/Dev/PreviewOverlay/spec_montagem.json.

Saídas, em Assets/Textures/Bancada (usadas na prévia do Editor e na demonstração 3D do app):
  - Tapete_montagem_demo.png: tapete com o marcador A, o contorno tracejado da placa e os textos impressos;
  - PCB_serigrafia.png: face de cima da placa ATX genérica, com o contorno e o nome de cada componente.

Depois de gerar, rodar na Unity: MontAR > Prévia > Reconstruir cena de montagem (e, se a bancada
do app mudou, MontAR > App > Reconstruir interface e cena).
As posições vêm do spec e são aproximadas (o kit real do laboratório ainda não foi definido).

Uso (a partir da raiz do repositório):
  python tools/gerar_texturas_previa.py

Requer: pip install pillow
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gerar_marcador import desenhar_marcador, fonte  # noqa: E402

RAIZ = Path(__file__).resolve().parent.parent
SPEC = RAIZ / "Assets" / "Dev" / "PreviewOverlay" / "spec_montagem.json"
PASTA = RAIZ / "Assets" / "Textures" / "Bancada"
SEMENTE_MARCADOR_A = 20261005
PX_MM_TAPETE = 4
PX_MM_PLACA = 6
COR_TAPETE = (232, 236, 240)
COR_TEXTO = (52, 60, 70)


def rgb255(cor):
    if max(cor[:3]) > 1:
        return tuple(int(c) for c in cor[:3])
    return tuple(round(c * 255) for c in cor[:3])


def intersecta(a, b, folga=0):
    return not (a[2] + folga <= b[0] or b[2] + folga <= a[0] or a[3] + folga <= b[1] or b[3] + folga <= a[1])


def tracejado(d, x0, y0, x1, y1, cor, largura, traco):
    for x in range(x0, x1, traco * 2):
        d.line([(x, y0), (min(x + traco, x1), y0)], fill=cor, width=largura)
        d.line([(x, y1), (min(x + traco, x1), y1)], fill=cor, width=largura)
    for y in range(y0, y1, traco * 2):
        d.line([(x0, y), (x0, min(y + traco, y1))], fill=cor, width=largura)
        d.line([(x1, y), (x1, min(y + traco, y1))], fill=cor, width=largura)


def gerar_tapete(spec):
    mat = spec["mat"]
    pcb = next(e for e in spec["board_elements"] if e["id"] == "pcb")
    k = PX_MM_TAPETE
    largura, altura = round(mat["width_mm"] * k), round(mat["height_mm"] * k)
    img = Image.new("RGB", (largura, altura), COR_TAPETE)
    d = ImageDraw.Draw(img)

    lado = round(mat["marker_size_mm"] * k)
    mx = round(mat["marker_center_x_mm"] * k - lado / 2)
    my = round(mat["marker_center_y_mm"] * k - lado / 2)
    img.paste(desenhar_marcador(SEMENTE_MARCADOR_A, "MontAR A").resize((lado, lado), Image.LANCZOS), (mx, my))
    marcador = (mx, my, mx + lado, my + lado)

    bx0, by0 = round(mat["board_left_mm"] * k), round(mat["board_top_mm"] * k)
    bx1, by1 = bx0 + round(pcb["w_mm"] * k), by0 + round(pcb["h_mm"] * k)
    tracejado(d, bx0, by0, bx1, by1, (70, 80, 92), 5, 22)
    placa = (bx0, by0, bx1, by1)

    # Textos impressos: cada um na primeira posição livre (fora do marcador, da placa e dos outros textos).
    ocupados = [marcador, placa]
    margem = 10 * k
    for i, texto in enumerate(mat.get("printed_labels_pt") or []):
        f = fonte(44 if i == 0 else 30)
        x0, y0, x1, y1 = d.textbbox((0, 0), texto, font=f)
        w, h = x1 - x0, y1 - y0
        colocado = False
        for y in range(margem, altura - h - margem, 6):
            for x in range(margem, largura - w - margem, 12):
                caixa = (x, y, x + w, y + h)
                if not any(intersecta(caixa, o, folga=4 * k) for o in ocupados):
                    d.text((x - x0, y - y0), texto, fill=COR_TEXTO, font=f)
                    ocupados.append(caixa)
                    colocado = True
                    break
            if colocado:
                break
        if not colocado:
            print(f"Aviso: sem espaço no tapete para o texto: {texto}")

    saida = PASTA / "Tapete_montagem_demo.png"
    img.save(saida)
    return saida


def rotulo_serigrafia(e):
    """Texto curto como nas placas reais: slots DIMM pelo nome do canal, o soquete como CPU.
    O campo opcional "silk" do spec troca o texto (vazio = sem texto)."""
    if "silk" in e:
        return e["silk"]
    if e.get("kind") == "dimm":
        return e["id"].split("_")[-1].upper()
    if e["id"].startswith("soquete"):
        return "CPU" if e["id"] == "soquete_moldura" else ""
    return e["id"].upper()


def gerar_serigrafia(spec):
    pcb = next(e for e in spec["board_elements"] if e["id"] == "pcb")
    k = PX_MM_PLACA
    largura, altura = round(pcb["w_mm"] * k), round(pcb["h_mm"] * k)
    base = rgb255(pcb["color_rgb"])
    img = Image.new("RGB", (largura, altura), base)
    d = ImageDraw.Draw(img)
    clara = tuple(min(255, c + 150) for c in base)
    f = fonte(26)

    # Origem do quadro da placa = canto superior esquerdo da imagem (x para a direita, y para baixo).
    for e in spec["board_elements"]:
        if e["id"] == "pcb":
            continue
        x0 = round((e["x_mm"] - e["w_mm"] / 2) * k) - 6
        y0 = round((e["y_mm"] - e["h_mm"] / 2) * k) - 6
        x1 = round((e["x_mm"] + e["w_mm"] / 2) * k) + 6
        y1 = round((e["y_mm"] + e["h_mm"] / 2) * k) + 6
        d.rectangle([x0, y0, x1, y1], outline=clara, width=2)
        rotulo = rotulo_serigrafia(e)
        if not rotulo:
            continue
        tx0, ty0, tx1, ty1 = d.textbbox((0, 0), rotulo, font=f)
        centro = (x0 + x1) / 2 - (tx1 - tx0) / 2
        tx = max(4, min(largura - (tx1 - tx0) - 4, round(centro)))
        ty = y1 + 4 if y1 + 4 + (ty1 - ty0) < altura else y0 - (ty1 - ty0) - 4
        d.text((tx - tx0, ty - ty0), rotulo, fill=clara, font=f)

    for furo in spec.get("mounting_holes") or []:
        cx, cy = furo["x_mm"] * k, furo["y_mm"] * k
        r = 5 * k
        d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=clara, width=2)

    rodape = "MontAR - placa ATX genérica (prévia, posições aproximadas)"
    fr = fonte(30)
    rx0, ry0, rx1, ry1 = d.textbbox((0, 0), rodape, font=fr)
    d.text((largura - (rx1 - rx0) - 24 - rx0, altura - (ry1 - ry0) - 20 - ry0), rodape, fill=clara, font=fr)

    saida = PASTA / "PCB_serigrafia.png"
    img.save(saida)
    return saida


def main():
    spec = json.loads(SPEC.read_text(encoding="utf-8"))
    PASTA.mkdir(parents=True, exist_ok=True)
    print(gerar_tapete(spec))
    print(gerar_serigrafia(spec))


if __name__ == "__main__":
    main()
