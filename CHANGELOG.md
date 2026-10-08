# Changelog

Todas as mudanças relevantes do MontAR são registradas aqui.
O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o versionamento segue o [SemVer](https://semver.org/lang/pt-BR/), com uma versão por marco do projeto:

| Versão | Marco |
|---|---|
| `0.1.0` | Estrutura inicial do repositório |
| `0.2.0` | Prova de conceito (rastreamento e sobreposição no celular) |
| `0.3.0` | Fluxo de etapas com validação |
| `1.0.0` | Protótipo congelado para o experimento (09/11/2026) |

## [Não lançado]

Prova de conceito em andamento. Ainda não testada em celular.

### Adicionado
- Aplicativo com interface completa (uGUI + TextMeshPro), na cena `MontAR.unity`. O fluxo foi testado no Editor, no modo demonstração 3D. Ainda não foi testado em celular:
  - **Apresentação**: 6 páginas explicando Aponta, Guia, Valida e Registra, com deslize lateral e botão Pular;
  - **Seleção de peças**: placa-mãe, CPU, cooler, RAM (1 ou 2 pentes), SSD M.2 (opcional), placa de vídeo (opcional) e fonte, com uma opção de cada por enquanto. O roteiro se ajusta ao kit;
  - **Início**: código anônimo do participante, escolha entre RA e demonstração 3D e o roteiro gerado pelo kit;
  - **Montagem**: etapa e progresso, situação da etapa (aponte o marcador, siga a peça fantasma, confira), instrução, alerta do erro comum, checklist de validação, troca RA/3D e botão discreto do observador (toque longo de 1,2 s);
  - **Conclusão**: tempo total, etapas, validações reprovadas, intervenções e tempo por etapa.
  - Retomada de montagem interrompida (diálogo ao abrir o app) e botão voltar do Android.
- **Demonstração 3D** (`DemoStage`): bancada virtual com o tapete, a placa genérica e o mesmo overlay da RA, para ver como encaixar sem câmera (aparelho sem ARCore ou marcador fora de vista). Dá para girar e aproximar com os dedos.
- Catálogo de peças (`PartCatalog`, `Assets/Data/Pecas/Catalogo_pecas_v0.asset`), seleção do kit (`PartSelection`) e filtro de etapas por peça (`StepFilter`, campos de peça no `AssemblyStep`).
- Roteiro de montagem em bancada v0 (`Assets/Data/Roteiro_bancada_v0.asset`), com 10 etapas e overlays animados. Com o kit padrão, a montagem tem 9 etapas: placa no tapete, CPU, cooler + CPU_FAN, RAM (A2 ou A2+B2), SSD M.2, cabo de 24 pinos, cabo EPS 8 pinos, placa de vídeo e verificação final. As posições são aproximadas: o kit ainda não foi definido (D5).
- `docs/documents/roteiro-bancada-v0.md`, exportado das etapas, para o manual e o vídeo seguirem o mesmo roteiro.
- Construtores no Editor (menu **MontAR › App**): interface, cena, catálogo e exportação do roteiro, além de `BatchTasks` para rodar tudo em linha de comando.
- Evento `mode_changed` no CSV. O `session_started` passa a trazer `mode=ar|demo_3d` e `kit=peça:modelo:quantidade|…` no `detail`.
- `SessionSummary`, o resumo da sessão para a tela de conclusão.
- `tools/gerar_icones_ui.py`, que gera os ícones e os sprites da interface.
- 21 testes EditMode novos (62 no total) e 1 teste PlayMode de ponta a ponta na cena real, que salva capturas das telas em `Builds/Screens`.
- Capturas do Editor (modo 3D) em `docs/images/`.
- Cena `Assets/Scenes/MontAR.unity` com AR Session, XR Origin (Mobile AR), `ARTrackedImageManager` e o objeto `MontAR` (rastreamento, overlay e painel de diagnóstico).
- Marcador `MontAR_Marcador_A` (15 cm, nota 100 no `arcoreimg`), biblioteca `MontAR_ReferenceImages` e PDF A4 para impressão em `docs/documents/marcadores/`.
- `tools/gerar_marcador.py`: gera marcadores reprodutíveis (PNG para a Unity e PDF para impressão).
- Overlay da prova de conceito (`Overlay_PoC`): contorno de 15 cm, peça "fantasma" e seta de orientação.
- Scripts em `Assets/Scripts/` (namespace `MontAR`):
  - roteiro como dados: `AssemblyStep` e `AssemblyProcedure`;
  - fluxo: `StepFlow` (máquina de estados com avanço bloqueado), `StepFlowController` e `ParticipantCode`;
  - validação: `ChecklistValidator` e `ReferenceSeenValidator`;
  - métricas: `SessionCsvLog`, `SessionMetricsLogger` e `FpsWindow`;
  - progresso: `ProgressStore`;
  - RA: `ImageTrackingController`, `ARContentManager` e `PocDiagnosticsHud`.
- 41 testes EditMode (`Assets/Tests/EditMode/`) para o fluxo, o CSV, a validação, o progresso e o código de participante.

### Alterado
- Assets da prévia de montagem saíram de `Assets/Dev` e foram para as pastas definitivas, com os mesmos GUIDs: a placa genérica está em `Prefabs/Bancada`, os overlays em `Prefabs/Overlays`, os materiais em `Materials/Bancada`, as texturas em `Textures/Bancada` e as etapas em `Data/Steps/Bancada`.
- O spec da montagem (`spec_montagem.json`) agora tem as etapas de todas as peças, um slot M.2 sem dissipador e o formato `cilindro`. Os movimentos dos overlays aceitam deslocamento horizontal.
- O painel de diagnóstico da PoC começa desligado. Para ligar e desligar, toque longo (2 s) na versão, na tela de apresentação.
- Orientação travada em retrato.
- A interface agora usa o TextMeshPro Essential Resources, importado em `Assets/TextMesh Pro`.

### Corrigido
- `ImageTrackingController` não quebra mais quando o ARCore entrega uma imagem sem referência na biblioteca.
- `StepFlow`: a fase da etapa já sai certa no `step_started`. Antes, uma etapa sem marcador (ou no modo 3D) passava para "Guiando" depois do evento, e a interface ficava com o botão Validar travado.

## [0.1.0] - 2026-10-05

### Adicionado
- Projeto Unity 6000.6.4f1 criado a partir do template Universal 3D (URP).
- Estrutura de pastas do repositório-modelo da disciplina: `Assets/{Materials,Models,Prefabs,Scenes,Scripts,Textures}` e `docs/{diagrams,documents,images}`.
- Pastas próprias do MontAR: `Assets/Data/Steps`, `Assets/Textures/ReferenceImages` e `Assets/Tests/EditMode`.
- `README.md` com as 33 seções do modelo, preenchido com os dados do artigo V1 e do pitch.
- `README-PREENCHIMENTO.md` copiado do repositório-modelo.
- Artigo V1 e pitch em `docs/documents/`.
- `CLAUDE.md` com o contexto do projeto para o assistente de código.
- Pacotes AR Foundation 6.6.2 e Google ARCore XR Plugin 6.6.2 (com XR Plug-in Management 4.7.0).
- ARCore ativo no Android e XR Simulation no Desktop (XR Plug-in Management).
- `ARBackgroundRendererFeature` e `ARCommandBufferSupportRendererFeature` no `Mobile_Renderer`.

### Alterado
- Plataforma ativa: Android. Player Settings: Company `Yupen`, Product `MontAR`, pacote `br.facens.yupen.montar`, IL2CPP, ARM64, API mínima 29 (exigida pelo ARCore com Vulkan), versão `0.1.0`.
- Editor no nível de qualidade "Mobile" (o mesmo usado no Android).
- `.gitignore` com regras para IDEs, arquivos de sistema operacional e segredos (`.env`, keystores).

### Removido
- Conteúdo do template: `Assets/TutorialInfo/`, `Assets/Readme.asset` e `Assets/Scenes/SampleScene.unity`.
- Pacotes sem uso no projeto: `com.unity.collab-proxy`, `com.unity.visualscripting`, `com.unity.ai.navigation`, `com.unity.ai.assistant` e `com.unity.pipeline`.
