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
- **Documentação técnica para a entrega de 31/10** (atividade "Documentação técnica do projeto no GitHub"): README revisado seção por seção contra o modelo `dr-ohata/upx-modelo`, com curso, professor e RAs; requisitos, hipóteses e testes planejados alinhados ao artigo V4; explicação técnica da RA (componentes do AR Foundation e fluxo do rastreamento ao overlay); descrição de cada tela; problemas 6 a 8; cronograma em 31/10. Diagramas em `docs/diagrams/` (arquitetura, fluxo técnico da RA, estados da etapa, fluxo de funcionamento e caso de uso), com a fonte `.mmd` e o PNG gerado pelo Mermaid CLI 11.4.2. GIF do fluxo completo em `docs/images/fluxo-montagem-3d.gif`. Artigo V4 em `docs/documents/artigo-v4-montar.docx`.
- Verificação do procedimento de instalação em 31/10: clone limpo do GitHub, importação pela Unity 6000.6.4f1 e 75 de 75 testes EditMode e 1 de 1 PlayMode aprovados.
- **Peças virtuais e "Simular encaixe"** (para a demonstração de usabilidade sem o kit). Na tela de início, escolha "Kit de verdade" ou "Peças virtuais" (fica salvo no aparelho). Com peças virtuais, a placa-mãe genérica aparece em 3D presa ao marcador A na RA (na bancada 3D ela já existia), e o botão **Simular encaixe**, ao lado do "Validar etapa", anima a peça da etapa no caminho da peça fantasma até o lugar. A peça fica montada nas etapas seguintes; validar sem simular também encaixa. O checklist continua obrigatório. O `session_started` do CSV ganha `;pecas=virtuais`. Novos: `VirtualAssembly`, `OverlayPart` (papel de cada elemento do overlay pela cor do spec, posto pelo `MontagemPreviewBuilder`). Testado só no Editor (modo 3D); a RA não rodou em celular.
- **Interface em vidro (glassmorphism)**: cartões de vidro fosco que mostram desfocado o que está atrás (a câmera da RA ou a bancada 3D). O desfoque é feito por um passe do URP (`GlassBlurFeature`, shader `Hidden/MontAR/GlassBlur`, instalado nos renderers pelo `AppBuilder`), e os cartões usam o shader `MontAR/UI Glass`. Fundo escuro com luzes verde, azul-esverdeada e violeta atrás dos menus e da bancada 3D (`fundo_aurora.png`), contorno e reflexo nos cartões, botão principal verde com brilho e fonte Plus Jakarta Sans (OFL, em `Assets/Fonts`, assets do TextMeshPro gerados pelo `FontBuilder`). Capturas novas em `docs/images/`, do Editor.
- **Simulador web para avaliadores** (`docs/simulador/index.html`, gerado por `tools/gerar_simulador_web.py`): a mesma bancada, os mesmos overlays e o mesmo roteiro do app, lidos do `spec_montagem.json`, do catálogo do `AppBuilder` e das texturas da bancada. A próxima etapa só abre depois do checklist, e a peça validada fica instalada na bancada. Modos **na tela**, **na sua frente** (câmera do celular + giroscópio, sem rastreamento) e **WebXR** (RA na mesa ou óculos). Não grava CSV e não faz parte do experimento. Testado só no Chrome headless (computador e celular emulado); giroscópio real e WebXR ainda não foram testados em aparelho.
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
- Feedback do teste de usabilidade (23/10):
  - **Mais tipos de processador**: a CPU agora tem dois modelos, Intel LGA1700 e AMD AM5, cada um com etapas próprias de CPU (`cpu_soquete`, `cpu_soquete_am5`) e cooler (`cooler_cpu`, `cooler_cpu_am5`, com a furação AMD de 54 x 90 mm). A placa genérica é a mesma.
  - **Onde segurar a peça**: RAM, SSD e CPU dizem para segurar pelas bordas, sem tocar nos contatos dourados nem nos chips. Na RAM, marcadores azuis no overlay mostram as pontas do pente onde segurar.
  - **Tensão da fonte**: a fonte ganhou o modelo "com chave 110/220 V", que acrescenta a etapa `fonte_tensao` (conferir a chave antes de ligar, com alerta de que a chave errada queima a fonte). A verificação final passa a pedir a conferência da tensão para qualquer fonte.
  - Página "3. Valida" da apresentação renomeada para "3. Validação".
- Feedback do teste de usabilidade, 2ª rodada (23/10). Testado só no Editor (modo 3D); nada disso rodou em celular ainda:
  - **Apresentação**: "‹ Voltar" foi para o canto superior esquerdo (o "Próximo" ocupa a base sozinho, sem toque errado) e o ícone do Guia virou uma peça tracejada descendo no encaixe.
  - **Peças**: RAM de 1 a 4 pentes (recomendado 2) com capacidade, frequência e latência; armazenamento SSD M.2, SSD SATA ou HD (etapas `ssd_sata` e `hd_sata`); placa de vídeo sem cabo, com PCIe 6+2 ou com 12V-2x6 (etapas próprias); cooler com heat pipes; detalhes do modelo na linha (áudio, rede, Wi-Fi, Bluetooth e USB-C da placa-mãe). Etapas `ram_3_slots` e `ram_4_slots`.
  - **Câmera**: antes da RA, um aviso explica o uso da câmera e abre o pedido do Android ("Durante o uso do app", "Só desta vez", "Não permitir"). A `ARSession` começa desligada e só liga com a permissão; sem ela, segue no 3D.
  - **Montagem**: a situação da etapa entrou no cartão do topo, com ícone (antes era um cartão solto); "Confira antes de avançar" aparece só no painel de baixo; cadeado fechado/aberto diz se o 3D está travado (validação) ou livre; a peça fica centralizada no espaço entre o topo e o painel; o painel abre e fecha arrastando para cima/baixo; o erro da validação aparece em vermelho logo acima do botão, cita os itens que faltaram e não some ao esconder a instrução; o aviso "Etapa concluída" aparece acima do painel, sem cobrir os cartões; o botão do observador ganhou ícone, legenda e explicação no toque curto.
  - **Histórico de montagens** (`HistoryScreen`, `AssemblyHistoryStore`, `history.json`): a conclusão leva ao histórico (não volta para a apresentação), com nome, data, tempo, erros apontados e etapas de cada montagem. Nome opcional da montagem na tela de início (fica só no histórico, não vai para o CSV). Também abre pelo "Histórico ›" da tela de peças.
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

Tag `v0.1.0` no commit `bf58d79` (configuração do projeto Unity com AR Foundation e ARCore).

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
