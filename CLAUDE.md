# CLAUDE.md — MontAR

> Contexto permanente para o Claude Code neste repositório. Aqui está **o que o projeto é, como trabalhamos e tudo o que falta fazer**.
> Mantenha este arquivo atualizado: quando algo for decidido ou concluído, edite a seção correspondente (principalmente §14 e §15).
> Fontes: `Artigo V1 MontAR - Yupen.docx` (artigo V1, submetido em 10/09/2026) e `Yupen – MontAR (1).pptx` (pitch). Modelo de repositório exigido: https://github.com/dr-ohata/upx-modelo

---

## 1. O projeto em uma frase

**MontAR** é um app Android de Realidade Aumentada que projeta a instrução sobre o computador real e **só libera a próxima etapa da montagem depois de validar a anterior**. Sem óculos de RA, sem hardware dedicado: funciona no celular que o aluno já tem.

Os quatro verbos do app (slide 7):
1. **Aponta**: a câmera reconhece o componente e destaca o encaixe correto na placa.
2. **Guia**: a instrução 3D fica sobre a peça real, com a orientação certa de encaixe.
3. **Valida**: a próxima etapa só é liberada depois de confirmar a anterior. **Esse é o diferencial técnico.**
4. **Registra**: marca cada etapa concluída e permite retomar de onde parou.

Público-alvo: aluno iniciante (primeira montagem), professor de laboratório e instituição de ensino. Contexto de uso: bancada de laboratório, componentes reais, celular na mão.

## 2. Contexto acadêmico: o app também é instrumento de pesquisa

Disciplina **UPX — Realidade Aumentada**, Centro Universitário Facens (Sorocaba/SP), semestre 2026.2. O app existe para responder às perguntas do artigo, e **isso define requisitos de software** (ver §9).

**Problema:** estudantes sem experiência erram na montagem (pinos LGA tortos, EPS 12V trocado com PCIe, RAM invertida). Na literatura, a RA reduz erros, mas o tempo varia. Em Chu, Liao & Lin (2020), com RA móvel e confirmação de etapas, os erros caíram de 4,25 para 2,31, mas o tempo subiu de 358 s para 782 s, **por causa do reconhecimento da peça e não da sobreposição**. Esse gargalo é tratado como **requisito de projeto**: reconhecimento rápido.

- **RQ1:** efeito da RA com validação sequencial sobre a **taxa de falhas de instalação**, comparada a manual técnico e vídeo.
- **RQ2:** efeito sobre o **tempo de execução**.
- **RQ3:** dificuldades de interação e **usabilidade** (SUS).
- **H1:** RA terá menos falhas que manual e vídeo. **H2:** a validação **pode aumentar** o tempo. **H3:** usabilidade adequada.
- **Variável independente:** tipo de instrução (manual técnico × tutorial em vídeo × app RA).
- **Variáveis dependentes:** falhas, tempo e usabilidade.
- **Controle:** mesma bancada, mesmos componentes, mesma tarefa, mesma instrução inicial e mesmo celular. Desenho entre participantes ainda está em avaliação.
- **Implicação direta:** o manual e o vídeo das condições de controle precisam seguir **exatamente o mesmo roteiro de etapas** do app. A equipe também precisa produzir esses dois materiais.
- **Regra do artigo:** nada que ainda não foi executado pode ser descrito como resultado. Vale também para README e documentação.

## 3. Equipe Yupen

| Integrante | Papel | E-mail (RA) |
|---|---|---|
| Enzo Zorzetto | Pesquisa e experimento: estado da arte, protocolo experimental, análise de dados | 234974@facens.br* |
| Yuri Peruzzo | **Desenvolvimento AR**: rastreamento, sobreposição 3D, fluxo de validação de etapas | 248732@facens.br* |
| Pedro Ricci Gomes Nascimento | Produto e conteúdo: requisitos, interface, roteiro de montagem, documentação | 234923@facens.br* |

\* Associação e-mail ↔ nome inferida pela ordem no artigo. **Confirmar** antes de colocar os RAs no README.
Pendentes para o README: nome do professor e curso.

Esforço planejado (slide 11): 3 pessoas × 16 semanas × 6 h/semana = **288 h**, das quais 4 semanas já foram usadas na pesquisa.

## 4. Cronograma

| Fase | Período | Entrega |
|---|---|---|
| Pesquisa | 10/09 – 05/10 | ✅ Concluída (artigo V1) |
| Prova de conceito (PoC) | 06/10 – ~19/10 (proposta) | Rastreamento + sobreposição funcionando no celular |
| Protótipo | ~20/10 – **09/11** | Fluxo completo de etapas + validação + métricas |
| **Congelamento do protótipo** | **09/11** | A partir daqui, **apenas coleta**: nada de feature nova |
| Experimento | 10/11 – 01/12 | Coleta e análise de dados |
| Apresentação final | **03/12** | — |

> ⚠️ **Inconsistência no slide 10:** as datas estão deslocadas uma coluna para a direita. Do jeito que está, o protótipo aparece em 10/11–01/12, o que contradiz "congelamento em 09/11". A tabela acima é a leitura coerente; a divisão PoC/Protótipo em ~19/10 é uma **proposta**. Confirmar com a equipe e corrigir o slide.

## 5. Estado atual do repositório (07/10/2026)

- **App com interface completa** (07/10): apresentação → seleção de peças → início (código + modo RA/3D) → montagem → conclusão. O fluxo inteiro passa no teste PlayMode no Editor, no **modo demonstração 3D**. **Ainda não rodou em celular.** Capturas em `docs/images/` (do Editor, não da RA).
- **Unity configurada para AR no Android** (§6). Plataforma ativa Android; Editor no nível de qualidade "Mobile"; orientação travada em retrato.
- Cena única do build: `Assets/Scenes/MontAR.unity`. Contém:
  - AR Session, XR Origin (Mobile AR) com `ARTrackedImageManager` (biblioteca `MontAR_ReferenceImages`, 1 imagem móvel) e Directional Light;
  - EventSystem com `InputSystemUIInputModule`;
  - objeto `MontAR` com `ImageTrackingController`, `ARContentManager` (sem `previewStep`), `PocDiagnosticsHud` (desligado; toque longo de 2 s na versão da tela de apresentação liga/desliga), `SessionMetricsLogger`, `StepFlowController` (roteiro `Roteiro_bancada_v0`) e `AppController` (catálogo `Catalogo_pecas_v0`);
  - instância do prefab `Assets/Prefabs/UI/MontAR_UI.prefab` (5 telas + aviso + diálogo);
  - `Demonstração 3D` em y = −100: bancada virtual (mesa, tapete, placa genérica, marcador A virtual) e a câmera da demonstração. A bancada fica desativada fora do modo 3D. Nos menus, essa câmera só limpa o fundo.
- **Tudo isso é gerado por código** (Editor, menu **MontAR › App › Reconstruir interface e cena**, ou `BatchTasks.BuildContent`). Reconstruir **apaga ajustes manuais** no prefab da UI e na bancada virtual. O catálogo de peças só é criado se não existir.
- Conteúdo da montagem: `Assets/Dev/PreviewOverlay/spec_montagem.json` é a fonte. O `MontagemPreviewBuilder` (menu **MontAR › Prévia › Reconstruir cena de montagem**) gera a placa genérica (`Prefabs/Bancada`), os overlays (`Prefabs/Overlays`), as etapas (`Data/Steps/Bancada`), o roteiro (`Data/Roteiro_bancada_v0.asset`) e a cena de prévia `Preview_Montagem.unity`. As texturas da bancada vêm de `tools/gerar_texturas_previa.py` (→ `Textures/Bancada`); os ícones da UI, de `tools/gerar_icones_ui.py` (→ `Textures/UI`).
- `docs/documents/roteiro-bancada-v0.md` é exportado das etapas (menu **MontAR › App › Exportar roteiro**). É a base do manual e do vídeo. Não editar à mão.
- Template limpo: `TutorialInfo/`, `Readme.asset` e `SampleScene.unity` removidos. `PC_*` mantidos (perfil do Standalone). `SampleSceneProfile` mantido porque os dois RP Assets o referenciam como volume profile.
- `activeInputHandler: 1`, ou seja, **somente o novo Input System** (nada de `UnityEngine.Input`). O IMGUI (`OnGUI`) do painel de diagnóstico funciona mesmo assim.
- Build Android funciona: `Builds/MontAR-0.1.0-poc.apk` (45 MB, 05/10, só a PoC) e `Builds/MontAR-0.1.0-dev.apk` (07/10, app completo, gerado por `BatchTasks.BuildAndroid`). Os dois ficam fora do git. **Nenhum foi instalado em celular ainda.**
- ⚠️ **Smart App Control (Windows 11) na máquina do Yuri** bloqueou a DLL nativa do XR Simulation (`DllNotFoundException: XRSimulationSubsystem` no Play) e as DLLs do `Library/BurstCache` (só avisos) em 05/10. **Não desligar** (é irreversível no Windows 11). Em 07/10, nos testes PlayMode em batchmode, o XR Simulation carregou (`ARSession: Ready`); falta conferir no Editor com interface. Testar RA no celular. Registrado no README §29.
- **Sem celular:** o próprio app tem o modo **demonstração 3D** (bancada virtual, mesmo overlay e mesmos offsets da RA). Prévias de Editor: `Preview_Montagem.unity` (todas as etapas, renderiza imagens com `MontagemPreviewBuilder.RenderShots`) e `Preview_Overlay.unity`, ambas em `Assets/Dev/PreviewOverlay/`, fora do build.
- **Prévia do overlay da PoC:** `Assets/Dev/PreviewOverlay/Preview_Overlay.unity`, fora do build. Tem a folha A4 do marcador deitada numa mesa e um objeto "Imagem rastreada (simulada)" com o `Overlay_PoC` como filho, igual ao `ARContentManager`. Serve para ajustar offsets e overlays no Editor, já que o XR Simulation não roda aqui. A câmera "Camera do celular (prévia)" pode ser renderizada por script para gerar imagens.
- **Celular:** o Galaxy A06 do Yuri não serve para testar (relatado em 05/10). O APK foi copiado para ele via MTP, mas a depuração USB não chegou a conectar. **Falta um aparelho com ARCore** para a PoC; conferir em https://developers.google.com/ar/devices.
- O ARCore grava o banco de imagens dentro de `MontAR_ReferenceImages.asset` (`m_DataStore`) a cada build: é normal esse asset aparecer modificado no git depois de buildar.
- Git: branch `main` **sem nenhum commit**. Remoto: `origin = https://github.com/yuri-pro07/Upx8---Projeto-MONTAR.git`. **Não commitar até o Yuri liberar** (pedido explícito em 05/10).
  - `Assets/Editor/HubForceResolve.cs` está staged mas já foi apagado do disco. Rodar `git add -A` antes do 1º commit para não versioná-lo.
  - `.gitattributes` usa **Git LFS** (fbx, blend, obj, áudio, vídeo…). Todo integrante precisa rodar `git lfs install` (já feito na máquina do Yuri, LFS 3.7.1).
  - Artigo e pitch em `docs/documents/artigo-v1-montar.docx` e `docs/documents/pitch-montar.pptx`.
- `.gitkeep` só nas pastas ainda vazias (`Assets/Models`, `docs/diagrams`). A Unity ignora arquivos que começam com ponto.
- `tools/gerar_marcador.py` (Python + Pillow) gera marcadores reprodutíveis: PNG em `Assets/Textures/ReferenceImages/` e PDF A4 em `docs/documents/marcadores/`. A moldura preta encosta na borda da imagem, então **lado da moldura impressa = tamanho declarado na biblioteca**. Avaliar com `arcoreimg eval-img` (meta ≥ 75; o marcador A tirou 100).
- `com.coplaydev.unity-mcp` ainda aponta para `#main` (D6, pendente).

## 6. Stack e configuração da Unity

| Tecnologia | Versão | Uso |
|---|---|---|
| Unity | **6000.6.4f1** | Motor e lógica das etapas |
| URP | 17.6.0 | Renderização (usar `Mobile_RPAsset` / `Mobile_Renderer` no Android) |
| Input System | 1.20.0 | Toque/UI (`InputSystemUIInputModule`) |
| AR Foundation | **6.6.2** (recomendada para o 6000.6) | Camada de RA |
| Google ARCore XR Plugin | **6.6.2** | Provedor RA Android |
| XR Plugin Management | 4.7.0 (dependência) | Ativar ARCore |
| XR Core Utils | 2.6.0 (dependência) | `XROrigin` |
| uGUI + TextMeshPro | 2.6.0 (TMP vem dentro do uGUI no Unity 6) | Interface |
| Test Framework | 1.8.0 | Testes EditMode da lógica |
| Blender | 4.x | Modelos 3D dos componentes |
| Vuforia | *alternativa em avaliação* (ver D1) | Model Targets, se o rastreamento por imagem não bastar |

Plataforma-alvo: **Android com ARCore**, smartphone comum, sem tripé e sem óculos. Registrar no README apenas as versões **efetivamente instaladas** (conferir `Packages/packages-lock.json`).

### Checklist de configuração da Unity (feito em 05/10 na máquina do Yuri, salvo onde indicado)

- [x] Unity Hub: módulo **Android Build Support** (OpenJDK + Android SDK & NDK + CMake) no 6000.6.4f1. Instalado via `"Unity Hub.exe" -- --headless install-modules --version 6000.6.4f1 -m android --cm` (no terminal do VS Code, rodar sem `ELECTRON_RUN_AS_NODE`). Instalar com o Editor aberto exige **reiniciar o Editor**, senão os pacotes de Android dão erro de compilação (`AndroidExternalToolsSettings` não encontrado).
- [x] Plataforma ativa **Android**.
- [x] AR Foundation 6.6.2 e Google ARCore XR Plugin 6.6.2.
- [x] XR Plug-in Management: **ARCore** no Android e **XR Simulation** no Desktop (o XR Simulation não roda nesta máquina, ver §5).
- [x] **Project Validation** zerado nas abas Android e Desktop. As correções automáticas aplicaram:
  - min API **29** (ARCore "Required" + Vulkan);
  - `ARCommandBufferSupportRendererFeature` no `Mobile_Renderer`;
  - Run In Background ligado;
  - preferência de recompilar só depois do Play.
- [x] Player Settings:
  - Company `Yupen`, Product `MontAR`, pacote `br.facens.yupen.montar`, versão `0.1.0` (code 1);
  - **IL2CPP**, **ARM64**, Target API automático.
- [x] URP: **AR Background Renderer Feature** + `ARCommandBufferSupportRendererFeature` no `Mobile_Renderer`. O Android usa o nível "Mobile" → `Mobile_RPAsset`, e o Editor também foi colocado em "Mobile".
- [x] Cena `Assets/Scenes/MontAR.unity`:
  - AR Session + XR Origin (Mobile AR); câmera sem pós-processamento, near 0,05;
  - `ARTrackedImageManager` com `MontAR_ReferenceImages`.
- [x] Canvas da UI (Scale With Screen Size, 1080×1920, casa pela largura) + EventSystem com `InputSystemUIInputModule` (07/10, gerados pelo `AppBuilder`).
- [x] TMP Essential Resources importado em `Assets/TextMesh Pro` (07/10). Em linha de comando: `-importPackage "Library/PackageCache/com.unity.ugui@…/Package Resources/TMP Essential Resources.unitypackage"`.
- [x] `MontAR.unity` é a única cena do build; `SampleScene` apagada.
- [x] Build do APK sem erros (05/10).
- [ ] Primeiro **Build and Run** em celular real. Registrar modelo, versão do Android e resultado no §23 do README.

Pegadinhas do AR Foundation 6:
- `ARSessionOrigin` está obsoleto; usar `XROrigin` (`Unity.XR.CoreUtils`).
- `trackedImagesChanged` está obsoleto; usar `trackablesChanged`, em que `removed` vem como pares `TrackableId`/`ARTrackedImage`.
- ARCore **não faz rastreamento de objetos 3D**. No Android, o AR Foundation só reconhece **imagens** e planos, então reconhecer o componente em si exige marcador, Vuforia ou ML (ver D1).
- `ARTrackedImage.trackingState` tem os valores `Tracking`, `Limited` e `None`. Tratar `Limited` como "perdido" para a validação.

## 7. Arquitetura do app

Princípio: **roteiro orientado a dados**. As etapas são ScriptableObjects editáveis sem mexer em código, porque o roteiro vai mudar com o levantamento de falhas e com o kit real. A lógica de fluxo fica em C# puro, testável em EditMode. Os MonoBehaviours só fazem a ponte com Unity/AR.

```text
Câmera do celular → Reconhece a peça → Sobrepõe o modelo 3D → Valida e avança
      (ARCore)       (ImageTracking)     (ARContentManager)     (StepFlowController)
                                                                      ↓
                                                 SessionMetricsLogger (CSV) + ProgressStore (JSON)
```

Assemblies: `Assets/Scripts/MontAR.asmdef` (runtime), `Assets/Tests/EditMode/MontAR.Tests.EditMode.asmdef` e `Assets/Tests/PlayMode/MontAR.Tests.PlayMode.asmdef`. Os scripts de Editor (`Assets/Editor/MontAR/` e `Assets/Dev/PreviewOverlay/Editor/`) caem no `Assembly-CSharp-Editor`. **62 testes EditMode + 1 PlayMode passando** (07/10).

**Decisão de 06–07/10 (D8): como o app mostra "como faz".** A instrução é dada **em tempo real sobre a peça real** (RA): a peça fantasma anima o gesto no encaixe certo, o usuário repete com a peça dele e depois **valida** (checklist) antes de liberar a próxima. Não há a sequência "assiste ao vídeo/demonstração e depois mostra a peça para o app conferir". Motivos: é a proposta do artigo (instrução projetada sobre o computador real + validação sequencial); separar demonstração e execução vira um vídeo, que já é a condição de controle; e o ARCore não reconhece a peça em si (só o marcador do tapete), então "mostrar a peça para o app validar" exigiria marcador por componente ou ML (D1/D2). A **demonstração 3D** existe como apoio: mesma animação numa bancada virtual, para quando o marcador não está à vista ou o aparelho não tem ARCore. Ela também deixa testar o fluxo inteiro no Editor. Fica registrada no CSV (`mode_changed`), e **a condição do experimento é a RA**.

| Script (namespace `MontAR`) | Status | Responsabilidade |
|---|---|---|
| `Data/AssemblyStep` (ScriptableObject, implementa `IStepDefinition`) | ✅ código | Id, título, componente, instrução, alerta de erro comum, `referenceImageName`, prefab do overlay, offsets de posição/rotação, `StepValidationType` (`Checklist`/`ReferenceSeen`), itens do checklist, marcador da validação visual. Menu `Create › MontAR › Etapa de montagem` |
| `Data/AssemblyProcedure` (ScriptableObject) | ✅ código | Lista ordenada de etapas + `procedureId`. `OnValidate` avisa sobre posição vazia e StepId vazio ou repetido |
| `Flow/StepFlow` (C# puro) | ✅ testado | Máquina de estados `Locating → Guiding → Validating → Completed`, com avanço **bloqueado** até validar. Regras: etapa sem referência começa em Guiding; referência já rastreada no início da etapa emite `reference_detected` na hora (latência zero); **perder o rastreamento não faz a fase regredir** (o usuário pode apoiar o celular); validação reprovada ou cancelada volta para Guiding; `Start(n, detalhe)` retoma da etapa n. A fase já sai certa no `step_started`. `SetMode(Demo3D)` pula o Locating (sem `reference_detected` falso) e grava `mode_changed`. Evento único `EventRaised(FlowEvent)` |
| `Flow/StepFlowController` | ✅ código, na cena | Monta o roteiro do kit (`StepsFor`), liga o fluxo ao rastreamento, ao overlay (RA e `DemoStage`), ao logger e ao `ProgressStore` (que guarda o kit). API: `TryGetResumableProgress`, `StartNewSession(código, kit)`, `ResumeSession`, `LeaveSession`, `SetGuidanceMode`, `RequestValidation`, `ConfirmChecklist`, `CancelValidation`, `RegisterObserverIntervention`. A validação visual é aprovada sozinha quando o marcador aparece. O logger se inscreve **antes** do controlador (para gravar `session_completed` antes do `EndSession`) |
| `Data/PartCatalog` (+ `PartDefinition`, `PartModel`) | ✅ código, asset | Peças da tela de seleção: id, nome, descrição, ícone, unidade, quantidade mín./máx./padrão e modelos (1 por peça por enquanto). `Catalogo_pecas_v0`: placa-mãe, CPU, cooler, RAM (1–2), SSD (0–1), placa de vídeo (0–1), fonte |
| `Flow/PartSelection` + `StepFilter` + `IPartRequirement` | ✅ testado | O kit escolhido (`peça:modelo:quantidade`) e o filtro: a etapa entra se a peça exigida estiver no kit, na faixa de quantidade (ex.: `ram_slot_a2` com 1 pente, `ram_slots` com 2) e no modelo exigido (vazio = qualquer) |
| `Flow/GuidanceMode` | ✅ testado | `AugmentedReality` (`ar`) ou `Demo3D` (`demo_3d`) |
| `Metrics/SessionSummary` | ✅ testado | Resumo da tela de conclusão a partir dos mesmos eventos do CSV: tempo total, tempo e reprovações por etapa, intervenções |
| `AR/DemoStage` | ✅ código, na cena | Bancada virtual da demonstração 3D: instancia o overlay da etapa no marcador virtual, com os mesmos offsets da RA; câmera orbital (enquadra a peça, projeção deslocada para a área livre entre o topo e o painel) |
| `UI/AppController` | ✅ código, na cena | Navegação entre as telas, qual câmera aparece (RA, bancada ou fundo), verificação do ARCore (sem RA → só 3D), diálogo de retomada, botão voltar do Android, liga/desliga do diagnóstico |
| `UI/*Screen`, `PartRowView`, `ChecklistItemView`, `Toast`, `ConfirmDialog`, `LongPressButton`, `DemoOrbitInput`, `SafeAreaFitter`, `UiTheme` | ✅ código, no prefab | Telas e componentes da interface. Observador = toque longo de 1,2 s no círculo do topo. Cores em `UiTheme` |
| `Flow/ParticipantCode` | ✅ testado | Normaliza (trim + maiúsculas) e valida `[A-Z0-9_-]{1,16}`. `CreateSessionId` → `P01_20261020T143000` (UTC) |
| `Validation/IStepValidator`, `ChecklistValidator`, `ReferenceSeenValidator` | ✅ testado | Checklist exige todos os itens (`Describe()` → `checked=2/3;pending=1`). Visual exige o marcador em Tracking |
| `Metrics/SessionCsvLog` (C# puro) + `SessionInfo` + `FpsWindow` | ✅ testado | CSV RFC 4180, cultura invariante (ponto decimal mesmo em pt-BR) |
| `Metrics/SessionMetricsLogger` | ✅ código | Arquivo em `persistentDataPath/sessions/<id>.csv` (anexa sem cabeçalho na retomada), flush a cada 2 s e no `OnApplicationPause`, `fps_sample` a cada 5 s |
| `Persistence/ProgressStore` + `SessionProgress` | ✅ testado | `progress.json` em `persistentDataPath`, uma sessão por aparelho; arquivo corrompido → ignora com aviso |
| `AR/ImageTrackingController` | ✅ código, na cena | `trackablesChanged` → eventos `ReferenceTracked(ARTrackedImage)` / `ReferenceLost(nome)`. Só `Tracking` conta; `Limited` = perdido. Ignora imagens sem nome na biblioteca |
| `AR/ARContentManager` | ✅ código, na cena | Instancia o overlay como filho do `ARTrackedImage`, no offset da etapa. `keepOverlayWhenLimited` (padrão true) mantém o overlay quando as mãos cobrem o marcador. `previewStep` mostra uma etapa sem o fluxo (PoC) |
| `AR/PocDiagnosticsHud` | ✅ código, na cena (desligado) | IMGUI: estado da `ARSession`, FPS (1 s), referências rastreadas e botão "Medir detecção" (toque → apontar → tempo). Loga `[MontAR][PoC]` no logcat. Toque longo (2 s) na versão, na apresentação, liga/desliga. **Manter desligado no experimento** |
| `MarkerPoseValidator` | ⬜ | Marcador do componente na pose esperada ± tolerância (D2, camada 2) |
| `InteractionController` | ⬜ | Toques na cena AR (nome do modelo) |

Ferramentas de Editor (`Assets/Editor/MontAR/`, namespace `MontAR.EditorTools`): `AppBuilder` (catálogo, prefab da UI, bancada virtual, ligações da cena), `UiFactory` (blocos de UI), `RoteiroExporter` (Markdown do roteiro) e `BatchTasks` (linha de comando). Em `Assets/Dev/PreviewOverlay/Editor/`: `MontagemPreviewBuilder` + `MontagemPreviewSpec` (conteúdo a partir do spec).

Assets da PoC (continuam no projeto, fora do roteiro):
- `Data/Steps/PoC_Marcador.asset`: etapa com `referenceImageName = MontAR_Marcador_A`, overlay `Prefabs/Overlay_PoC.prefab` e 3 itens de checklist.
- `Overlay_PoC`: contorno amarelo de 15 cm, peça "fantasma" translúcida e seta laranja apontando para +Z. Usa materiais URP Unlit em `Materials/Overlay_*.mat`.
- **A PoC precisa confirmar no celular se o +Z do `ARTrackedImage` é o topo da imagem**, o lado do triângulo preto. Os offsets do tapete dependem dessa convenção.

## 8. Requisitos

| ID | Funcionalidade | Status |
|---|---|---|
| RF01 | Iniciar sessão com **código anônimo do participante** | 🚧 tela pronta, testada no Editor |
| RF02 | Reconhecer a referência/componente da etapa (**Aponta**) | 🚧 na cena, falta testar no celular |
| RF03 | Sobrepor instrução 3D com a orientação correta de encaixe (**Guia**) | 🚧 overlays das 9 etapas prontos (posições aproximadas), falta testar no celular |
| RF04 | Mostrar instrução textual e alerta do erro comum da etapa | 🚧 tela pronta, testada no Editor |
| RF05 | **Bloquear o avanço até validar a etapa** (**Valida**) | 🚧 checklist na tela, testado no Editor |
| RF06 | Salvar progresso e retomar de onde parou (**Registra**) | 🚧 diálogo de retomada pronto, falta testar fechando o app no celular |
| RF07 | Registrar métricas e exportar CSV (§9) | 🚧 CSV gravado no teste PlayMode do Editor, falta rodar no celular |
| RF08 | Botão discreto "intervenção do observador", que alimenta a taxa de conclusão sem intervenção | 🚧 toque longo no topo da montagem |
| RF09 | Tela de conclusão com resumo da sessão | 🚧 tela pronta, testada no Editor |
| RF10 | Apresentação do app (como funciona: Aponta, Guia, Valida, Registra) | 🚧 tela pronta, testada no Editor |
| RF11 | Seleção das peças do kit (modelo e quantidade), que ajusta o roteiro | 🚧 uma opção por peça; testada no Editor |
| RF12 | Demonstração 3D sem câmera (bancada virtual com o mesmo overlay) | 🚧 testada no Editor |

**Legenda:** ✅ implementado · 🚧 em desenvolvimento · ⬜ planejado · ❌ cancelado. Usar a mesma legenda do README. Critério para ✅: funcionando **no celular**.

| ID | Requisito não funcional |
|---|---|
| RNF01 | Android com ARCore, celular **na mão**: sem óculos, sem tripé, sem hardware extra |
| RNF02 | Reconhecimento rápido da referência (o gargalo de Chu et al.). Meta a definir na PoC, sugestão ≤ 2 s em luz de laboratório |
| RNF03 | Taxa de quadros estável. Meta sugerida ≥ 30 FPS, a medir nos testes técnicos |
| RNF04 | Funciona **offline** |
| RNF05 | **Sem dados pessoais** (LGPD): só código anônimo de participante |
| RNF06 | Overlays leves (low-poly, texturas pequenas) |
| RNF07 | Repositório documentado a ponto de outra pessoa configurar e rodar |

## 9. Instrumentação de pesquisa (obrigatória antes de 09/11)

O Quadro 2 do artigo diz "instrumentação da própria aplicação" para três medidas: **tempo por etapa**, **tempo de confirmação de cada etapa** e **FPS / latência de rastreamento**. O app precisa gravar um log de eventos em formato longo, um CSV por sessão, em `persistentDataPath/sessions/<session_id>.csv`, recuperado via `adb pull`.

Colunas **implementadas** (`SessionCsvLog.Header`). A coluna `utc` foi acrescentada para cruzar com o vídeo da bancada e ordenar sessões retomadas, porque `t_ms` zera quando o app reinicia:

```text
session_id, participant_code, app_version, device_model, step_index, step_id, event, t_ms, utc, fps_avg_1s, detail
```

`step_index` fica vazio nos eventos de sessão. `fps_avg_1s` fica vazio antes da primeira janela de 1 s. Exemplos de `detail`: `resumed_from=3`, `checked=2/3;pending=1`, `seen=<marcador>` e o nome da referência nos eventos de rastreamento.

Eventos implementados:

| Grupo | Eventos |
|---|---|
| Sessão | `session_started`, `session_completed` |
| Etapa | `step_started`, `step_completed` |
| Rastreamento | `reference_detected`, `tracking_lost` (de **qualquer** referência; o nome vai em `detail`) |
| Validação | `validation_started`, `validation_passed`, `validation_failed`, `validation_cancelled` |
| Observador | `observer_intervention` |
| Modo | `mode_changed` (`detail` = `mode=ar` ou `mode=demo_3d`) |
| Desempenho | `fps_sample` (a cada 5 s, configurável no `SessionMetricsLogger`) |

O `detail` do `session_started` traz `[resumed_from=N;]mode=ar|demo_3d;kit=peça:modelo:quantidade|…` (ex.: `mode=ar;kit=placa_mae:atx_generica_lga1700:1|…|gpu:gpu_pcie_x16:0|fonte:fonte_atx:1`). Na análise, **descartar ou separar as sessões com `demo_3d`**: no 3D a etapa não espera o marcador, e a latência de rastreamento não vale.

Como cada medida sai do log:
- **Latência de rastreamento** = `reference_detected − step_started`.
- **Tempo de confirmação** = `validation_passed − validation_started`.

A decomposição do tempo (identificação × execução física × confirmação) depende desses eventos, e é ela que permite discutir H2. Usar `Time.realtimeSinceStartupAsDouble` para `t_ms`, nunca `Time.time`. Gravar com flush periódico para não perder dados se o app fechar.

## 10. Estrutura do repositório (seguir o upx-modelo)

```text
├── Assets/
│   ├── Materials/  Models/  Prefabs/  Scenes/  Textures/   ← mesmas pastas do modelo
│   ├── Scripts/                 ← AR, Data, Flow, Metrics, Persistence, UI, Validation (namespace MontAR)
│   ├── Editor/MontAR/           ← construtores da UI/cena, exportador do roteiro, BatchTasks (adição nossa)
│   ├── Data/                    ← Roteiro_bancada_v0, Steps/Bancada (etapas), Pecas/ (catálogo) (adição nossa)
│   ├── Prefabs/                 ← UI/MontAR_UI, Overlays/, Bancada/ (placa genérica)
│   ├── Textures/                ← ReferenceImages/ (marcadores + biblioteca), UI/ (ícones), Bancada/ (tapete, serigrafia)
│   ├── TextMesh Pro/            ← TMP Essential Resources (importado)
│   ├── Dev/PreviewOverlay/      ← spec da montagem, construtor do conteúdo e cenas de prévia (fora do build)
│   ├── Tests/EditMode/ e PlayMode/ ← testes da lógica e o teste de ponta a ponta (adição nossa)
│   └── XR/                      ← configurações do XR Plug-in Management (geradas pela Unity)
├── Packages/                    ← manifest.json real (não o .example do modelo)
├── ProjectSettings/
├── docs/
│   ├── diagrams/   arquitetura.svg, caso-de-uso.svg, fluxo-ra.svg (do MontAR, não do exemplo)
│   ├── documents/  artigo, pitch, roteiro de montagem, protocolo, persona, relatórios de teste
│   │   └── marcadores/  PDFs A4 dos marcadores, em tamanho real
│   └── images/     screenshots reais (tela-inicial.png, experiencia-ra.png…)
├── tools/          ← gerar_marcador.py, gerar_texturas_previa.py, gerar_icones_ui.py (adição nossa)
├── .gitignore  .gitattributes
├── CHANGELOG.md
├── README.md                    ← as 33 seções do modelo, preenchidas com dados REAIS do MontAR
└── README-PREENCHIMENTO.md      ← guia do professor (copiar do modelo)
```

O `.gitignore` atual (template oficial Unity) já é mais completo que o do modelo. Basta acrescentar `.vscode/`, `.idea/`, `.env`, `*.keystore`, `*.jks`, `.DS_Store` e `Thumbs.db`.

## 11. Convenções

- **Idioma:** documentação, commits, comentários, `Debug.Log` e textos da UI em **português**. Identificadores C# em **inglês** (como nos scripts do modelo). Namespace `MontAR`.
- **C#:** `[SerializeField] private` em vez de campos públicos. XML doc (`/// <summary>`) curto em português nas classes públicas. Lógica testável fora de MonoBehaviour. Nada de `FindObjectOfType` em `Update`.
- **Commits (padrão do modelo):** `feat:`, `fix:`, `docs:`, `test:`, `chore:`, `refactor:`, com descrição no presente em português. Exemplo: `feat: adiciona validação por checklist na etapa da CPU`. **Commits pequenos e frequentes**: o professor avalia a evolução, nunca um commitão único.
- **Branches:** `main` sempre compilando. Features em `feat/<nome-curto>` com merge após testar no Editor.
- **CHANGELOG.md:** SemVer por marco. Proposta: `0.1.0` estrutura · `0.2.0` PoC · `0.3.0` fluxo de etapas · `1.0.0` protótipo congelado (09/11).
- **Binários:** modelos `.fbx`/`.blend` passam pelo LFS. Nunca versionar `.apk`, `.aab` ou keystore. Para builds, usar GitHub Releases ou link externo.

## 12. Regras para o Claude neste projeto

1. **Responder em português.**
2. **Nunca inventar** resultados, versões, compatibilidade de dispositivo ou métricas, seja no README, no CHANGELOG ou no artigo. Registrar só o que foi realmente testado (regra do `README-PREENCHIMENTO.md` e do artigo).
3. **Não editar à mão** o YAML de `.unity`, `.prefab` ou `.asset`. Mudanças de cena e prefab vão pelo Editor (via MCP, §13) ou ficam como instruções para a equipe.
4. Depois de criar ou alterar scripts, **verificar o console da Unity** (erros de compilação) antes de declarar pronto.
5. Ao concluir uma funcionalidade, atualizar a tabela RF (§8 aqui e §6 do README) e, quando fechar um marco, o CHANGELOG.
6. Depois de 09/11 (congelamento), **não adicionar features**. Só correções que não alterem o fluxo nem as métricas, e sempre avisando a equipe, porque mexer muda as condições do experimento.
7. Não commitar nem dar push sem pedido explícito.
8. Manter esta CLAUDE.md atualizada quando decisões de §14 forem tomadas.

## 13. Unity via MCP (MCP for Unity, CoplayDev)

O pacote `com.coplaydev.unity-mcp` (v10.3.0) está instalado e permite ao Claude ler o console, criar e editar objetos de cena, gerenciar assets e scripts e executar itens de menu no Editor aberto.

Conexão (feita em 05/10/2026 na máquina do Yuri):
1. No Editor: **Window › MCP for Unity** › aba **Connect** › Transport `HTTP Local` › **Start Server**. O status deve mostrar "Session Active".
2. Registrar o servidor no Claude Code (escopo local, fica em `~/.claude.json` e não vai para o git):
   `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp`
   - O `claude` **não está no PATH** nesta máquina. Usar o binário da extensão: `%USERPROFILE%\.cursor\extensions\anthropic.claude-code-<versão>-win32-x64\resources\native-binary\claude.exe`.
   - Pelo mesmo motivo, o botão **Configure** da janela do Unity mostra "Claude CLI Path: Not found". Basta apontar o **Browse** para esse executável.
3. Conferir com `claude mcp list` (deve aparecer `UnityMCP … √ Connected`) e **reiniciar a sessão** do Claude Code para as ferramentas carregarem.
4. O servidor só existe com o Editor aberto e o **Start Server** ligado. Sem isso, as ferramentas falham. Na máquina do Yuri, o **Auto-Start on Editor Load** (aba Advanced) foi ligado em 05/10: o servidor sobe sozinho quando o Editor abre e após reinícios.
5. Cada integrante que quiser usar o MCP repete os passos 1–3 na própria máquina.
6. Se as ferramentas `mcp__UnityMCP__*` não aparecerem na sessão (registradas depois de a sessão começar), dá para falar direto com o servidor por HTTP: `POST http://127.0.0.1:8080/mcp` com `Accept: application/json, text/event-stream`, fazendo `initialize` → `notifications/initialized` (com o header `Mcp-Session-Id`) → `tools/call`. Foi assim que a configuração de 05/10 foi feita.

Pegadinhas do `execute_code` (compilador CodeDom, C# 6): sem funções locais (usar `Func<>`), nomes de variáveis não podem colidir com os de lambdas, e `AssetDatabase.DeleteAsset` é bloqueado (usar `manage_asset` com `action: delete`). Strings com acento vão certas para os assets; o YAML guarda como `\xE2` etc.

Boas práticas: salvar a cena antes de operações em lote, conferir `read_console` depois de cada mudança de script e preferir criar scripts como arquivos `.cs` (versionáveis) a gerar lógica pelo MCP.

### Linha de comando (com o Editor **fechado**)

Foi assim que o app foi montado e testado em 07/10. A Unity trava o projeto, então feche o Editor antes. `U="/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe"`, `P` = caminho do projeto no formato Windows:

- Compilar e montar tudo: `"$U" -batchmode -quit -projectPath "$P" -executeMethod MontAR.EditorTools.BatchTasks.BuildContent -logFile <log>`. Erro de compilação aparece como `Scripts have compiler errors` (procurar `error CS` no log).
- Testes: `"$U" -batchmode -projectPath "$P" -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>` (**sem** `-quit`). O PlayMode salva capturas em `Builds/Screens/`.
- APK: `-executeMethod MontAR.EditorTools.BatchTasks.BuildAndroid` (sai com código 1 se falhar).
- Pegadinhas que apareceram:
  - **abrir uma cena descarrega assets sem uso**: carregar catálogo, roteiro e prefab **depois** do `OpenScene`, senão a referência vira nula;
  - `GetComponent<T>() ?? AddComponent<T>()` não funciona no Editor (o "null falso" da Unity);
  - texto TMP some num Canvas Screen Space - Camera a menos de ~0,1 m da câmera (falta precisão do SDF). Nas capturas, a UI vai a 1 m numa câmera própria;
  - `AssetDatabase.ImportPackage` é assíncrono em batchmode: usar `-importPackage`.

## 14. Decisões em aberto (resolver na PoC, até ~19/10)

- **D1. Técnica de reconhecimento.** Recomendação: começar com **AR Foundation Image Tracking** usando um **tapete de montagem impresso**, com o contorno da placa-mãe desenhado e marcadores nos cantos. O tapete define o referencial; soquete, slots DIMM, PCIe e conectores ficam em offsets conhecidos para *aquele* modelo de placa. É barato, rápido e confiável.
  - Para as etapas dentro do gabinete: marcador na lateral ou na tampa.
  - Fazer um spike de **Vuforia Model Targets** só se o marcador se mostrar inviável (licença e marca d'água a verificar).
  - ML com Inference Engine (`com.unity.ai.inference` já está no manifest) só como exploração. Custa tempo e dataset.
- **D2. Mecanismo de validação.** Proposta em camadas:
  1. Garantido no protótipo: **checklist guiado por etapa** ("a alavanca do soquete está travada?", "o chanfro da RAM está alinhado?").
  2. Onde for viável: **validação visual por marcador**, seja escaneando o componente certo antes de instalar, seja checando a pose do marcador do componente.
  3. Medir o custo de tempo de cada camada (é exatamente o que a H2 investiga).
- **D3. Uso com as mãos ocupadas.** A montagem exige as duas mãos e o celular fica na mão, sem tripé (é o diferencial do slide 8). Definir o fluxo "olha → apoia o celular → executa → pega e valida" e se um apoio simples de mesa ainda conta como "sem bancada fixa". Isso afeta diretamente o tempo medido.
- **D4. Escopo da tarefa experimental.** Montagem completa ou subconjunto (ex.: CPU, cooler, RAM, M.2, cabos EPS/ATX/PCIe)? Depende do tempo por participante e do kit disponível.
- **D5. Kit de hardware do laboratório.** Modelos exatos de placa-mãe, CPU, RAM, GPU, fonte e gabinete. Sem isso não há offsets, modelos 3D nem roteiro final.
- **D6. Limpeza de pacotes.** ✅ Decidido em 05/10 (o Yuri liberou):
  - Removidos: `collab-proxy`, `visualscripting`, `ai.navigation`, `ai.assistant` e `pipeline`.
  - Mantido: `com.unity.ai.inference`, para a exploração da D1. Ele puxa o `com.unity.dt.app-ui` e pesa no APK (45 MB); reavaliar se a D1 descartar ML.
  - Pendente: fixar `unity-mcp` numa tag de release em vez de `#main`, ou tirá-lo do manifest compartilhado. Cuidado: trocar a versão recompila o plugin e derruba a conexão MCP no meio da sessão.
- **D7. Ética.** Verificar com o professor se o experimento com participantes exige TCLE/CEP e o que pode ser coletado.
- **D8. Como mostrar "como faz".** ✅ Decidido em 07/10: guia em tempo real sobre a peça real, com a demonstração 3D como apoio (ver §7). Confirmar com a equipe.
- **D9. Seleção de peças no experimento.** Todos os participantes precisam do **mesmo kit** (controle). Definir se o observador configura o kit antes de entregar o celular (proposta) ou se a tela de peças fica escondida no experimento. O kit vai no `session_started` do CSV.

## 15. Backlog por fase

### Fase 0: repositório (agora)
- [ ] `git add -A` (registrar a remoção do `HubForceResolve.cs`) e revisar o que vai no 1º commit.
- [x] Mover o artigo e o pitch para `docs/documents/`.
- [x] Criar as pastas do modelo (`Materials`, `Models`, `Prefabs`, `Scripts`, `Textures`) e `docs/{diagrams,documents,images}`.
- [x] Copiar `README-PREENCHIMENTO.md` do modelo. Criar `CHANGELOG.md` (`0.1.0`).
- [x] Criar o `README.md` com as 33 seções do modelo e preencher já o que o artigo e o pitch respondem. Pendentes no README: curso, professor e RAs (marcados como _a preencher_ / _a confirmar_).
- [x] Ajustar o `.gitignore` (§10). `git lfs install`: feito na máquina do Yuri; falta Enzo e Pedro.
- [x] Via MCP: remover `TutorialInfo/` e `Readme.asset`, renomear Company/Product no Player Settings e confirmar os `.meta` das pastas novas.
- [ ] 1º commit e push para `origin/main`. **Aguardando o Yuri liberar o git.** Sugestão para não virar um commitão: (1) `chore: estrutura inicial do projeto Unity` (template, .gitignore, docs, README, CHANGELOG, CLAUDE.md); (2) `chore: configura AR Foundation e ARCore para Android` (Packages, ProjectSettings, Assets/XR, Settings); (3) `feat: adiciona fluxo de etapas, métricas e progresso` (Scripts menos AR + Tests); (4) `feat: adiciona cena da prova de conceito com marcador A` (AR scripts, cena, marcador, prefab, tools/).
- [x] Conectar o MCP for Unity ao Claude Code (§13): `UnityMCP √ Connected` em 05/10.

### Fase 1: prova de conceito (05/10 – ~19/10)
- [x] Checklist de configuração da Unity (§6) completo e APK gerado. ⬜ Rodar no celular.
- [x] Image Tracking de 1 marcador com overlay (contorno + fantasma + seta). Cena pronta; ⬜ falta confirmar no celular que está estável e alinhado, e anotar a convenção de eixos (+Z). **Agora vale para todas as etapas**: se o +Z não for o topo da imagem, todos os overlays da bancada saem girados.
- [ ] Imprimir `docs/documents/marcadores/MontAR_Marcador_A_15cm_A4.pdf` em 100% e conferir os 15 cm com régua.
- [ ] Tapete de montagem v0 impresso. Overlay da etapa "CPU no soquete" alinhado ao soquete real (depende da D5).
- [ ] Medir tempo de detecção (botão do `PocDiagnosticsHud`) e FPS iniciais, registrar no README §22/§23 e decidir D1 e D2.
- [ ] `docs/diagrams/fluxo-ra.svg` e `arquitetura.svg` do MontAR.
- [ ] CHANGELOG `0.2.0` (quando o rastreamento funcionar no celular).

### Fase 2: protótipo (~20/10 – 09/11, congela em 09/11)
- [x] `AssemblyStep` / `AssemblyProcedure` (código). ⬜ Roteiro completo como dados (depende de D4/D5).
- [x] `StepFlow` com avanço bloqueado + testes EditMode.
- [x] `ChecklistValidator` + `ReferenceSeenValidator`. ⬜ `MarkerPoseValidator` (se a D2 pedir) e alertas por etapa na UI.
- [x] `ProgressStore` (retomar sessão), com testes.
- [x] `SessionMetricsLogger` + CSV. ⬜ Botão de intervenção do observador na UI e validação do CSV no celular (`adb pull`).
- [x] Montar o `StepFlowController` na cena (07/10, com `AppController`).
- [x] Overlays primitivos de todas as etapas da bancada v0 (fantasma translúcido, contornos, setas animadas). ⬜ Modelos 3D low-poly reais dos componentes (Blender → FBX via LFS), se os primitivos não bastarem.
- [x] UI completa (apresentação, peças, início com código e modo, etapa, checklist, progresso, conclusão), testada no Editor em 07/10. ⬜ Testar no celular: toque, teclado do código, área segura, retomada fechando o app.
- [ ] Revisar os textos das etapas e do checklist com a equipe (Pedro) e com o kit real (D5). Exportar de novo o `roteiro-bancada-v0.md`.
- [ ] Decidir D9 (kit fixo no experimento).
- [ ] Testes técnicos: estabilidade do rastreamento, FPS e latência, registrados no README §22 e §23.
- [ ] **Paralelo (Pedro/Enzo):** manual técnico e tutorial em vídeo com o **mesmo roteiro**, checklist de observação de falhas por categoria, protocolo experimental e formulário SUS.
- [ ] Piloto com 1–2 pessoas fora da amostra. Ajustes. Tag `v1.0.0` + APK em Release. **Congelar.**

### Fase 3: experimento (10/11 – 01/12)
- [ ] Coleta nas três condições. Exportar os CSVs (`adb pull`) e organizar os dados de forma anônima.
- [ ] Apoiar a análise com scripts de consolidação dos CSVs, se pedido.
- [ ] README final: screenshots reais, vídeo de demonstração, dispositivos testados, limitações e checklist de entrega do modelo completo.

### Roteiro de montagem: rascunho v0 (validar com o kit real e com o levantamento de falhas)

| # | Etapa | Erro crítico a prevenir |
|---|---|---|
| 1 | Placa-mãe sobre a caixa/espuma antiestática | Montar sobre superfície condutiva |
| 2 | CPU no soquete: alavanca, alinhar triângulo/entalhes, assentar sem forçar | **Pinos LGA tortos** |
| 3 | Pasta térmica + cooler + cabo no `CPU_FAN` | Esquecer o fan ou ligá-lo no header errado |
| 4 | RAM nos slots recomendados (ex.: A2/B2), alinhando o chanfro | **Módulo invertido** ou slot errado |
| 5 | SSD M.2 e trava/parafuso | Slot errado, SSD sem fixação |
| 6 | Espelho de I/O, standoffs e placa-mãe no gabinete | Standoff fora do lugar (curto) |
| 7 | Fonte + cabo ATX 24 pinos | Cabo mal encaixado |
| 8 | Cabo **EPS 12V (CPU 4+4)** | **Trocar com o PCIe 6+2** |
| 9 | GPU no PCIe x16 + cabo PCIe | Slot errado, GPU sem trava, cabo errado |
| 10 | Front panel (PWR_SW, RESET, LEDs) + USB frontal | Polaridade e posição |
| 11 | Verificação final antes de ligar | — |

## 16. Referências rápidas

- Modelo do repositório: https://github.com/dr-ohata/upx-modelo
- Repositório da equipe: https://github.com/yuri-pro07/Upx8---Projeto-MONTAR
- Trabalho mais próximo: Chu, Liao & Lin (2020), *Applied Sciences* 10(10), 3383. Smartphone + confirmação de etapas; menos erros, mais tempo.
- Educação/hardware: Westerfield et al. (2015), placa-mãe com RA adaptativa; Sirakaya & Kilic Cakmak (2018), HardwareAR.
- Métricas da área: Cim & Hounsell (2023); revisão de Wang et al. (2022).
