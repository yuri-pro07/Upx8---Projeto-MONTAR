"""Gera o simulador web da montagem (docs/simulador/index.html), para avaliadores sem celular com ARCore.

O simulador mostra a mesma bancada e os mesmos overlays do app (tapete, placa genérica, peça fantasma,
setas) e percorre o roteiro com o mesmo bloqueio: a próxima etapa só abre depois do checklist.
Para não divergir do app, ele é montado com o mesmo conteúdo:
  - Assets/Dev/PreviewOverlay/spec_montagem.json: bancada, etapas, overlays, textos e checklist;
  - o catálogo de peças do AppBuilder (DefaultParts): peças, modelos e quantidades do kit;
  - Assets/Textures/Bancada: texturas do tapete e da serigrafia da placa (embutidas em base64).
O modelo da página fica em tools/simulador_web/simulador.html.

Rodar de novo sempre que o spec, o catálogo ou as texturas mudarem:
  python tools/gerar_simulador_web.py

Com --artefato, grava também uma cópia sem <html>/<head>/<body>, no formato das páginas do claude.ai:
  python tools/gerar_simulador_web.py --artefato caminho/simulador.html

Só usa a biblioteca padrão do Python. A página carrega o three.js do jsDelivr (precisa de internet).
"""
import argparse
import base64
import json
import re
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent
SPEC = RAIZ / "Assets" / "Dev" / "PreviewOverlay" / "spec_montagem.json"
APP_BUILDER = RAIZ / "Assets" / "Editor" / "MontAR" / "AppBuilder.cs"
TEXTURAS = {
    "tapete": RAIZ / "Assets" / "Textures" / "Bancada" / "Tapete_montagem_demo.png",
    "serigrafia": RAIZ / "Assets" / "Textures" / "Bancada" / "PCB_serigrafia.png",
}
MODELO = RAIZ / "tools" / "simulador_web" / "simulador.html"
SAIDA = RAIZ / "docs" / "simulador" / "index.html"
MARCA = "__DADOS_MONTAGEM__"


def texto_cs(literal):
    """Conteúdo de uma string C# entre aspas (só os escapes que aparecem no AppBuilder)."""
    return literal.replace('\\"', '"').replace("\\\\", "\\")


def ler_catalogo():
    """Lê o DefaultParts do AppBuilder. O primeiro modelo de cada peça é o do kit padrão."""
    fonte = APP_BUILDER.read_text(encoding="utf-8")
    inicio = fonte.find("DefaultParts =")
    fim = fonte.find("};", inicio)
    if inicio < 0 or fim < 0:
        sys.exit(f"DefaultParts não encontrado em {APP_BUILDER}")

    string = r'"((?:[^"\\]|\\.)*)"'
    pecas = []
    for bloco in fonte[inicio:fim].split("new PartSpec")[1:]:
        def campo(nome, padrao=string):
            achado = re.search(rf"\b{nome}\s*=\s*{padrao}", bloco)
            if not achado:
                sys.exit(f"Campo {nome} não encontrado numa peça do DefaultParts")
            return achado.group(1)

        modelos = [
            {"id": texto_cs(m.group(1)), "nome": texto_cs(m.group(2))}
            for m in re.finditer(rf"new ModelSpec\(\s*{string}\s*,\s*{string}", bloco)
        ]
        pecas.append({
            "id": texto_cs(campo("Id")),
            "nome": texto_cs(campo("Name")),
            "unidade": texto_cs(campo("UnitSingular")),
            "unidades": texto_cs(campo("UnitPlural")),
            "min": int(campo("Min", r"(\d+)")),
            "max": int(campo("Max", r"(\d+)")),
            "padrao": int(campo("Default", r"(\d+)")),
            "modelos": modelos,
        })
    if not pecas:
        sys.exit("Nenhuma peça lida do DefaultParts")
    return pecas


def data_uri(caminho):
    return "data:image/png;base64," + base64.b64encode(caminho.read_bytes()).decode("ascii")


def sem_moldura(html):
    """Tira doctype, <html>, <head>, <body> e as metas de charset/viewport (o claude.ai acrescenta as dele)."""
    linhas = []
    for linha in html.splitlines():
        limpa = linha.strip().lower()
        if re.fullmatch(r"<!doctype html>|<html[^>]*>|</html>|<head>|</head>|<body>|</body>", limpa):
            continue
        if limpa.startswith("<meta charset") or limpa.startswith('<meta name="viewport"'):
            continue
        linhas.append(linha)
    return "\n".join(linhas) + "\n"


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--artefato", type=Path, help="grava também a versão sem <html>/<head>/<body> neste arquivo")
    args = p.parse_args()

    spec = json.loads(SPEC.read_text(encoding="utf-8"))
    dados = {
        "spec": spec,
        "catalogo": ler_catalogo(),
        "texturas": {nome: data_uri(caminho) for nome, caminho in TEXTURAS.items()},
    }
    # "</" dentro do JSON fecharia o <script> que o carrega.
    json_embutido = json.dumps(dados, ensure_ascii=False, separators=(",", ":")).replace("</", "<\\/")

    modelo = MODELO.read_text(encoding="utf-8")
    if modelo.count(MARCA) != 1:
        sys.exit(f"{MODELO} precisa ter {MARCA} exatamente uma vez")
    html = modelo.replace(MARCA, json_embutido)

    SAIDA.parent.mkdir(parents=True, exist_ok=True)
    SAIDA.write_text(html, encoding="utf-8", newline="\n")
    etapas = len(spec.get("steps", []))
    print(f"{SAIDA.relative_to(RAIZ)}: {len(html) // 1024} KB, {etapas} etapas, {len(dados['catalogo'])} peças no catálogo")

    if args.artefato:
        args.artefato.parent.mkdir(parents=True, exist_ok=True)
        args.artefato.write_text(sem_moldura(html), encoding="utf-8", newline="\n")
        print(f"Versão para o claude.ai: {args.artefato}")


if __name__ == "__main__":
    main()
