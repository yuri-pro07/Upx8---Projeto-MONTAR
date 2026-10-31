# MontAR

> **Realidade Aumentada com validação sequencial de etapas para a montagem de computadores**
> Projeto da Equipe Yupen na disciplina UPX: Realidade Aumentada (Centro Universitário Facens, 2026.2).

![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow)
![Fase](https://img.shields.io/badge/fase-prova%20de%20conceito-orange)
![Plataforma](https://img.shields.io/badge/plataforma-Android-green)
![RA](https://img.shields.io/badge/RA-AR%20Foundation%20%2B%20ARCore-blue)
![Unity](https://img.shields.io/badge/Unity-6000.6.4f1-black)
![Testes](https://img.shields.io/badge/testes-75%20EditMode%20%2B%201%20PlayMode-brightgreen)

> **Estado atual (31/10/2026):** o app funciona por completo **no Editor da Unity**: apresentação, seleção de peças, montagem guiada em RA ou em 3D, peças virtuais com "Simular encaixe", validação por checklist, conclusão, histórico e registro em CSV. Em 31/10 o repositório foi clonado do GitHub numa pasta nova e passou em todos os testes automatizados (75 EditMode e 1 PlayMode de ponta a ponta). O APK Android é gerado sem erros. **O app ainda não foi testado em celular**: a parte de RA (câmera, marcador e overlay sobre a bancada) está implementada, mas não tem resultado medido. A validação do componente pela câmera, prevista no artigo V4, ainda não foi implementada.

---

## 1. Identificação do Projeto

| Item | Informação |
|---|---|
| **Projeto** | MontAR |
| **Equipe** | Yupen |
| **Curso** | Engenharia de Computação |
| **Disciplina** | UPX: Realidade Aumentada (Usina de Projetos Experimentais) |
| **Professor** | Prof. Daniel Ohata |
| **Instituição** | Centro Universitário Facens, Sorocaba/SP |
| **Semestre** | 2026.2 |

### Integrantes

| Nome | RA | Responsabilidade |
|---|---|---|
| Enzo Zorzetto | 234974 | Pesquisa e experimento: estado da arte, protocolo experimental, instrumentos (checklist de falhas, SUS) e análise de dados |
| Yuri Peruzzo | 248732 | Desenvolvimento Unity e C#: integração com AR Foundation e ARCore, rastreamento do marcador, overlays 3D, fluxo de validação de etapas, métricas, testes automatizados e build Android |
| Pedro Ricci Gomes Nascimento | 234923 | Produto e conteúdo: requisitos, roteiro de montagem, interface, manual técnico e vídeo (condições de controle) e documentação |

---

## 2. Visão Geral

Quem monta um computador pela primeira vez costuma seguir um manual ou um vídeo, que mostram a instrução **fora do equipamento**. O estudante precisa olhar o material, achar o componente certo na bancada e traduzir a figura para a peça real. É nesse ponto que surgem os erros mais caros: processador desalinhado, memória invertida, cabo EPS trocado com o de PCIe.

O **MontAR** é um aplicativo Android que mostra a instrução **em cima da bancada** e **só libera a próxima etapa depois que o aluno confere a atual**. O aluno escolhe as peças do kit, e o app monta o roteiro (9 ou 10 etapas). Em cada etapa aparecem o texto, o alerta do erro mais comum e uma peça "fantasma" animada que mostra onde e em que sentido encaixar. Para avançar, o aluno marca um checklist com os pontos críticos da etapa. Se algum item ficar desmarcado, a etapa volta para correção.

A **Realidade Aumentada** faz a localização e a sobreposição. O ARCore reconhece um marcador impresso no tapete de montagem e calcula sua posição e orientação a cada quadro. O app prende nesse marcador a peça fantasma (e, sem o kit, uma placa-mãe virtual), de modo que a instrução fica sobre o encaixe certo mesmo quando o celular se move. Quando o aparelho não tem ARCore ou o marcador sai de vista, a mesma animação aparece numa bancada 3D.

O app também é o **instrumento de uma pesquisa experimental** (artigo V4): ele será comparado com um manual técnico e com um tutorial em vídeo quanto às falhas de instalação, ao tempo de execução e à usabilidade. Por isso registra os eventos de cada sessão em CSV.

| # | Ação | O que faz |
|---|---|---|
| 1 | **Aponta** | A câmera reconhece o marcador do tapete e localiza o encaixe da etapa na placa |
| 2 | **Guia** | A peça fantasma anima o encaixe sobre a placa, com a orientação correta |
| 3 | **Valida** | A próxima etapa só é liberada depois do checklist da etapa atual |
| 4 | **Registra** | Grava o progresso, o histórico e o CSV da sessão, e permite retomar de onde parou |

---

## 3. Problema

**Contexto.** A montagem de computadores desktop é uma atividade prática da formação técnica em computação. O estudante precisa identificar componentes, entender a posição de cada um e seguir a sequência correta de instalação.

**Situação atual.** O apoio usual é o manual técnico ou o tutorial em vídeo. Os dois apresentam a instrução fora do equipamento, e o estudante alterna a atenção entre o material e a bancada, traduzindo uma representação 2D para o computador real.

**Dificuldade.** Estudantes sem experiência erram principalmente nas etapas que dependem de identificação e orientação espacial. Com componentes reais, alguns erros danificam o equipamento:

- **Pinos do soquete:** CPU mal assentada entorta pinos LGA e inutiliza a placa-mãe.
- **Conector trocado:** EPS de 12 V e PCIe têm o mesmo número de pinos.
- **Orientação errada:** módulo de memória forçado no sentido invertido.

**Público afetado.** Estudantes na primeira montagem, o professor do laboratório (que atende vários alunos ao mesmo tempo) e a instituição (que perde componentes de treinamento).

**Impacto.** Retrabalho, aula mais lenta, peças danificadas e menos tempo de prática real. Na literatura, a RA reduz erros de montagem, mas o efeito sobre o tempo varia. Em Chu, Liao e Lin (2020), com RA em smartphone e confirmação de etapas, os erros caíram de 4,25 para 2,31 por montagem, mas o tempo subiu de 358 s para 782 s, e os autores atribuem parte desse custo ao **reconhecimento da peça**. O MontAR trata esse gargalo como requisito (RNF02).

---

## 4. Objetivos

### 4.1 Objetivo Geral

Desenvolver e avaliar uma solução integrada de Realidade Aumentada com validação sequencial de etapas para orientar a montagem física de computadores desktop, investigando seus efeitos sobre as falhas de instalação e o tempo de execução, em comparação com um manual técnico e um tutorial em vídeo produzidos a partir do mesmo roteiro, e a percepção de usabilidade da aplicação.

### 4.2 Objetivos Técnicos

| Objetivo técnico | Situação em 31/10/2026 |
|---|---|
| Configurar o projeto Unity com AR Foundation e ARCore para Android | ✅ Concluído (05/10) |
| Reconhecer o marcador do tapete e ancorar nele a instrução 3D | 🚧 Implementado, falta testar no celular |
| Implementar o fluxo sequencial de etapas, com avanço bloqueado até a validação | ✅ Implementado e coberto por testes |
| Manter o roteiro e o catálogo de peças como dados editáveis, sem alterar código | ✅ Concluído |
| Permitir a demonstração sem o kit (peças virtuais) e sem ARCore (bancada 3D) | ✅ Implementado e testado no Editor |
| Registrar métricas (tempos por etapa, validações, intervenções, FPS) e exportá-las em CSV | ✅ Implementado e testado no Editor |
| Validar o componente pela câmera, com um marcador por peça (RF05 do artigo V4) | ⬜ Planejado |
| Executar os testes técnicos no celular: detecção, posicionamento, estabilidade, FPS e modo avião | ⬜ Planejado |
| Documentar instalação, execução, testes e limitações | ✅ Este README |

### 4.3 Questões de pesquisa e hipóteses (artigo V4)

| ID | Questão |
|---|---|
| RQ1 | Qual o efeito da solução integrada de RA com validação sequencial sobre as **falhas de instalação** (presentes ao concluir cada etapa e cometidas durante a execução), comparada a manual técnico e tutorial em vídeo de mesmo conteúdo? |
| RQ2 | Qual o efeito sobre o **tempo total de execução** da tarefa? |
| RQ3 | Quais dificuldades de interação serão observadas e como os participantes avaliarão a **usabilidade**? |

| ID | Hipótese |
|---|---|
| H1a | A condição RA terá menor proporção de etapas com falha presente na conclusão que manual e vídeo. |
| H1b | A condição RA terá menos falhas cometidas durante a execução que manual e vídeo. |
| H2 | A condição RA terá tempo total maior que manual e vídeo. |
| H3 | A aplicação terá escore médio igual ou superior a 70 na System Usability Scale (SUS). |

O detalhamento está no artigo V4, em [`docs/documents/artigo-v4-montar.docx`](docs/documents/artigo-v4-montar.docx). Nenhum experimento foi realizado até o momento.

---

## 5. Público-Alvo

| Público | Perfil | Necessidade |
|---|---|---|
| Aluno iniciante | Nunca instalou CPU, memória, placa de vídeo ou fonte | Saber onde e em que sentido encaixar cada peça, sem danificar nada |
| Professor de laboratório | Atende vários alunos ao mesmo tempo em aula prática | Menos pedidos de ajuda repetidos e menos peças danificadas |
| Pesquisador (equipe) | Conduz o experimento | Registro confiável de tempos, validações e intervenções |

- **Contexto de uso:** bancada de laboratório, com componentes reais ou peças virtuais, e o celular na mão ou num suporte articulado (no experimento).
- **Familiaridade tecnológica:** usa smartphone no dia a dia; não precisa conhecer RA nem hardware.
- **Limitações do usuário:** a montagem ocupa as duas mãos, e o iniciante não reconhece de cara o nome nem a função de cada peça. Por isso cada etapa mostra o nome da peça, onde segurar e o erro mais comum.

---

## 6. Funcionalidades

| ID | Funcionalidade | Descrição | Status |
|---|---|---|---|
| RF01 | Iniciar sessão | Inicia a sessão com um código anônimo de participante (ex.: `P01`), sem dados pessoais | ✅ |
| RF02 | Reconhecer o marcador (**Aponta**) | Reconhece o marcador A do tapete e obtém sua pose pelo ARCore | 🚧 |
| RF03 | Sobrepor instrução 3D (**Guia**) | Mostra a peça fantasma animada sobre o encaixe, presa ao marcador | 🚧 |
| RF04 | Mostrar instrução e alerta | Exibe o texto da etapa e o alerta do erro mais comum | ✅ |
| RF05 | Bloquear avanço (**Valida**) | Só libera a próxima etapa depois do checklist completo da atual | ✅ |
| RF06 | Salvar e retomar (**Registra**) | Salva o progresso e o kit e permite retomar de onde parou | ✅ |
| RF07 | Registrar métricas | Grava um CSV por sessão, com um evento por linha | ✅ |
| RF08 | Intervenção do observador | Toque longo (1,2 s) num botão discreto registra a intervenção do pesquisador | ✅ |
| RF09 | Tela de conclusão | Mostra tempo total, etapas, validações reprovadas, intervenções e tempo por etapa | ✅ |
| RF10 | Apresentação | Explica Aponta, Guia, Validação e Registra antes de começar | ✅ |
| RF11 | Seleção de peças | Modelo e quantidade de cada peça (ex.: CPU Intel ou AMD, RAM de 1 a 4 pentes, SSD M.2, SATA ou HD, fonte bivolt ou com chave 110/220 V). O roteiro se ajusta ao kit | ✅ |
| RF12 | Demonstração 3D | Mostra o encaixe numa bancada virtual, sem câmera, com o mesmo overlay da RA | ✅ |
| RF13 | Histórico de montagens | Lista as montagens concluídas no aparelho (nome, tempo, erros apontados e etapas) | ✅ |
| RF14 | Permissão da câmera | Explica para que serve a câmera e abre o pedido do Android antes da RA. Sem permissão, segue no 3D | 🚧 |
| RF15 | Peças virtuais e "Simular encaixe" | Sem o kit, a placa-mãe aparece em 3D e o botão **Simular encaixe** leva a peça da etapa até o lugar, onde ela fica montada | 🚧 |
| RF16 | Interface em vidro | Cartões de vidro fosco com o fundo desfocado e fonte Plus Jakarta Sans | ✅ |
| RF17 | Validar o componente pela câmera | Um marcador por componente: o app confere se a peça mostrada é a da etapa e alerta se for outra (RF05 do artigo V4) | ⬜ |
| RF18 | Liberação pelo pesquisador | Liberar a etapa com senha após 20 s sem validação, registrando o motivo (RF10 do artigo V4) | ⬜ |

**Legenda:** ✅ Implementado · 🚧 Em desenvolvimento · ⬜ Planejado · ❌ Cancelado

**Critério usado:** ✅ quer dizer implementado e verificado no Editor por teste automatizado (seção 22). 🚧 quer dizer que o código existe, mas a funcionalidade depende da câmera ou do ARCore e só pode ser confirmada no celular. RF15 já funciona no modo 3D (o teste PlayMode simula o encaixe em todas as etapas); falta a versão presa ao marcador na RA. **Nenhuma funcionalidade foi testada em celular até 31/10/2026.**

---

## 7. Requisitos Não Funcionais

Os critérios vêm do artigo V4 e são metas provisórias, a confirmar nos testes do celular.

| ID | Categoria | Requisito | Critério de verificação | Situação |
|---|---|---|---|---|
| RNF01 | Compatibilidade | Smartphone Android comum com ARCore, sem óculos nem sensores extras | Execução no aparelho do experimento, com modelo e versão do Android registrados | ⬜ |
| RNF02 | Desempenho | Detecção rápida do marcador | Mediana de até 2 s entre o marcador entrar no campo de visão e a detecção | ⬜ |
| RNF03 | Desempenho | Taxa de quadros adequada durante toda a montagem | Média ≥ 30 FPS na sessão, sem trecho de 5 s abaixo de 20 FPS | ⬜ |
| RNF04 | Estabilidade | Conteúdo alinhado ao encaixe ao mover o celular e com o marcador parcialmente coberto | Desvio ≤ 5 mm; realinha em até 2 s; rastreia com até 1/3 do marcador coberto | ⬜ |
| RNF05 | Disponibilidade | Funcionar sem internet | Sessão completa em modo avião. O app não usa nenhum serviço externo | 🚧 |
| RNF06 | Privacidade (LGPD) | Nenhum dado pessoal coletado | Registros só com o código anônimo. O CSV não tem nome nem RA; falta a busca de dados pessoais nos arquivos | 🚧 |
| RNF07 | Usabilidade | Interface em português, legível na bancada e compreensível por iniciantes | Texto com altura mínima de 3 mm a 45 cm; no piloto, no máximo um pedido de esclarecimento por participante | 🚧 |
| RNF08 | Confiabilidade | Validação do componente sem aceitar peça errada | Nenhum falso positivo em 336 apresentações de marcadores errados | ⬜ (depende do RF17) |
| RNF09 | Desempenho | Overlays leves | Overlays feitos de formas simples (caixas, cilindros, setas) e texturas pequenas | ✅ |

---

## 8. Tecnologias Utilizadas

As versões foram conferidas em `ProjectSettings/ProjectVersion.txt`, `Packages/packages-lock.json` e nas próprias ferramentas, em 31/10/2026.

| Tecnologia | Versão | Utilização |
|---|---|---|
| Unity | 6000.6.4f1 | Motor 3D, cena de RA e lógica das etapas |
| C# | o da Unity 6000.6 | Scripts da aplicação (namespace `MontAR`) |
| Android (Player Settings) | API mínima 29, IL2CPP, ARM64 | Plataforma do APK (`br.facens.yupen.montar`, versão 0.1.0) |
| AR Foundation | 6.6.2 | Camada de RA: sessão, câmera e rastreamento de imagens |
| Google ARCore XR Plugin | 6.6.2 | Provedor de RA no Android |
| XR Plugin Management | 4.7.0 | Ativa o ARCore no Android |
| XR Core Utils | 2.6.0 | `XROrigin` (dependência do AR Foundation) |
| Universal Render Pipeline | 17.6.0 | Renderização (perfil Mobile) e passe de desfoque da interface de vidro |
| Input System | 1.20.0 | Toque, arrastar e pinça |
| uGUI + TextMeshPro | 2.6.0 | Interface |
| Unity Test Framework | 1.8.0 | Testes EditMode e PlayMode |
| Python + Pillow | 3.12.10 / 12.3.0 | Scripts de `tools/`: marcadores, texturas da bancada, ícones e simulador web |
| three.js | 0.160.0 (via jsDelivr) | Simulador web (`docs/simulador`) |
| Mermaid CLI | 11.4.2 | Geração das imagens de `docs/diagrams` |
| Git + Git LFS | 2.55.0 / 3.7.1 | Controle de versão e arquivos binários (PNG, PDF, fontes) |
| GitHub | — | Hospedagem do código e da documentação |

Não usamos Blender: os overlays e a placa genérica são formas simples geradas por código a partir de `spec_montagem.json` (seção 11.5).

---

## 9. Hardware Utilizado

| Equipamento | Fabricante e modelo | Sistema | Características relevantes | Finalidade |
|---|---|---|---|---|
| Computador de desenvolvimento (Yuri) | AMD Ryzen 7 9800X3D, 32 GB RAM, NVIDIA GeForce RTX 5070 Ti | Windows 11 Pro (10.0.26200) | Unity 6000.6.4f1 com Android Build Support | Desenvolvimento, build do APK e todos os testes feitos até agora |
| Marcador A impresso | Folha A4, impressão em tamanho real | — | Moldura de 15 cm, nota 100 no `arcoreimg` | Referência da RA (ainda não usado com celular) |

Equipamentos previstos que **ainda não foram usados**:

- **Smartphone Android com ARCore:** não definido. Até 31/10/2026 a equipe não teve um aparelho com ARCore disponível para os testes (seção 29, problema 6).
- **Kit de montagem:** placa-mãe, CPU, cooler, RAM, SSD, placa de vídeo e fonte do laboratório da Facens, ainda não definidos. A demonstração de usabilidade usa as peças virtuais do app.
- **Suporte articulado para o celular:** previsto no artigo V4, para deixar as mãos livres no experimento.

---

## 10. Arquitetura do Sistema

![Arquitetura do MontAR](docs/diagrams/arquitetura.png)

Fonte do diagrama: [`docs/diagrams/arquitetura.mmd`](docs/diagrams/arquitetura.mmd).

- **Dados separados do código.** O roteiro (`AssemblyProcedure` e etapas `AssemblyStep`) e o catálogo de peças (`PartCatalog`) são assets editáveis (ScriptableObject), porque o roteiro muda com o kit real.
- **Lógica testável.** O fluxo (`StepFlow`), a validação, o CSV e o progresso são C# puro, testados sem a Unity em execução.
- **Ponte com a Unity.** Os componentes de cena (MonoBehaviours) só ligam essa lógica à câmera, à RA e à interface.
- **Tudo no aparelho.** Não há servidor, API nem banco de dados. CSV, progresso e histórico ficam em `Application.persistentDataPath` e o pesquisador copia o CSV com `adb pull`.

O kit escolhido filtra o roteiro (`StepFilter`): sem placa de vídeo, a etapa do PCIe sai; com 1 pente de RAM, a etapa usa o slot A2 em vez de A2 e B2.

### Fluxo resumido

```text
Usuário
   ↓
Interface (6 telas, uGUI)
   ↓
Fluxo de etapas (StepFlowController + StepFlow)
   ↓
Módulo de RA (ImageTrackingController + ARContentManager)
   ↓
AR Foundation 6.6.2 (ARSession, ARTrackedImageManager)
   ↓
ARCore (Google ARCore XR Plugin 6.6.2)
   ↓
Câmera e sensores do celular
   ↓
Marcador do tapete e bancada com os componentes
```

---

## 11. Funcionamento da Realidade Aumentada

### 11.1 Técnica utilizada

**Image Tracking com marcador impresso** (RA baseada em marcador), pelo `ARTrackedImageManager` do AR Foundation, com o ARCore como provedor no Android.

O ARCore rastreia imagens e planos, mas **não reconhece objetos 3D** como uma placa-mãe. Por isso a referência é um **tapete de montagem impresso**, com o contorno da placa-mãe e o marcador A. O marcador define o referencial, e cada encaixe (soquete, slots de memória, PCIe, conectores) fica num deslocamento (offset) conhecido em relação a ele.

O marcador `MontAR_Marcador_A` tem 15 cm, foi gerado por `tools/gerar_marcador.py` e recebeu nota 100 no `arcoreimg` do ARCore (o Google recomenda a partir de 75). A nota mede a imagem, não o desempenho no celular, que ainda será testado.

### 11.2 Como a RA está integrada ao projeto

| Componente | Onde fica | Papel na RA |
|---|---|---|
| `AR Session` | Cena `MontAR.unity` | Ciclo de vida do ARCore. Começa **desligada** e só liga depois da permissão da câmera |
| `XR Origin (Mobile AR)` | Cena `MontAR.unity` | Câmera do celular e pose do aparelho. A imagem da câmera é desenhada pelo `ARBackgroundRendererFeature` do `Mobile_Renderer` |
| `ARTrackedImageManager` | No `XR Origin` | Procura as imagens da biblioteca `MontAR_ReferenceImages`. O marcador A tem tamanho físico declarado de 0,15 m, o que dá a escala certa ao overlay |
| `ImageTrackingController.cs` | `Assets/Scripts/AR` | Ouve `trackablesChanged` e emite `ReferenceTracked` ou `ReferenceLost`. Só o estado `Tracking` conta: `Limited` é tratado como perdido |
| `StepFlowController.cs` + `StepFlow.cs` | `Assets/Scripts/Flow` | Decide a fase da etapa (`Localizando → Guiando → Validando → Concluída`) |
| `ARContentManager.cs` | `Assets/Scripts/AR` | Instancia o overlay da etapa como **filho do `ARTrackedImage`**, no offset da etapa. Com peças virtuais, cria a `VirtualAssembly` (placa-mãe e peças já montadas) no mesmo lugar |
| `AppController.cs` | `Assets/Scripts/UI` | Chama `ARSession.CheckAvailability()` ao abrir. Sem ARCore, o app oferece só a demonstração 3D |
| `CameraPermission.cs` | `Assets/Scripts/AR` | Pede a permissão `CAMERA` do Android. Sem ela, a montagem segue no 3D |

### 11.3 Fluxo técnico

1. Ao abrir, o `AppController` verifica se o aparelho tem ARCore (`ARSession.CheckAvailability`).
2. O usuário inicia a montagem no modo **Câmera (RA)**. O app explica o uso da câmera e abre o pedido de permissão do Android.
3. Com a permissão, o app liga a `ARSession`. O ARCore liga a câmera e começa a rastrear o ambiente.
4. A etapa começa na fase **Localizando** (`step_started`). A cada quadro, o ARCore procura as imagens da biblioteca e o `ARTrackedImageManager` publica as mudanças.
5. Quando o marcador A entra no estado `Tracking`, o `ImageTrackingController` avisa o fluxo, que grava `reference_detected` e passa para **Guiando**.
6. O `ARContentManager` instancia o overlay da etapa como filho do marcador rastreado. Como o overlay é filho do `ARTrackedImage`, ele acompanha a pose do marcador a cada quadro.
7. O usuário executa a etapa (ou toca em **Simular encaixe**), toca em **Validar etapa**, marca o checklist e confirma. Com o checklist completo, o fluxo grava `validation_passed` e `step_completed` e libera a próxima etapa.
8. Se o marcador ficar coberto (`Limited`) ou sair de vista, o overlay continua no último lugar conhecido e a fase não regride: o usuário pode apoiar o celular e usar as duas mãos.

![Fluxo técnico da RA](docs/diagrams/fluxo-ra.png)

Fonte: [`docs/diagrams/fluxo-ra.mmd`](docs/diagrams/fluxo-ra.mmd).

### 11.4 Validação sequencial

<img src="docs/diagrams/estados-etapa.png" alt="Estados de uma etapa" width="380">

Não é possível avançar sem passar pela validação. Hoje, a validação de cada etapa é:

1. **Checklist guiado** (`ChecklistValidator`), com 2 ou 3 pontos críticos da etapa (por exemplo, "o triângulo da CPU coincide com o do soquete"). Com algum item desmarcado, a etapa volta para Guiando e o app mostra em vermelho o que faltou.
2. **Marcador visto** (`ReferenceSeenValidator`), nas etapas que declaram uma referência de validação: a etapa só é aprovada com o marcador rastreado.

Planejado no artigo V4 (RF17 e RF18, ainda não implementados): um marcador impresso por componente, mostrado à câmera antes da instalação; rastreamento estável por 0,5 s para aceitar; alerta se o componente for outro; e liberação pelo pesquisador, com senha, depois de 20 s sem validação.

### 11.5 Elementos virtuais

- peça fantasma translúcida, animada descendo até o encaixe correto;
- contornos amarelos no encaixe certo e vermelhos no errado (ex.: o segundo slot x16, que é x4);
- setas laranja indicando o gesto (empurrar na vertical, prender o parafuso);
- marcas de orientação (triângulo da CPU, chanfro da RAM e chave do slot) e marcadores azuis de onde segurar a peça;
- com peças virtuais: a placa-mãe em 3D e as peças já montadas nas etapas anteriores;
- texto da instrução, alerta do erro comum, checklist, progresso e situação da etapa (procurando o marcador, guiando, conferindo).

Os overlays são gerados a partir de `Assets/Dev/PreviewOverlay/spec_montagem.json`. As posições são de uma placa ATX **genérica** e valem como aproximação até o kit do laboratório ser definido.

### 11.6 Como o app mostra "como faz"

O app guia **em tempo real sobre a bancada**: a peça fantasma mostra o gesto no encaixe certo, o usuário repete com a peça dele e confirma a etapa no checklist. Não há uma etapa separada de "assistir e depois mostrar a peça para o app conferir", porque uma demonstração separada vira um vídeo, que já é uma das condições de controle, e porque o ARCore não reconhece a peça em si.

Quando a câmera não ajuda (marcador fora de vista, aparelho sem ARCore), a **demonstração 3D** mostra a mesma animação numa bancada virtual que dá para girar com o dedo. O botão **Câmera | 3D** troca o modo durante a montagem, e a troca fica registrada no CSV (`mode_changed`). **A condição do experimento é a RA.**

### 11.7 Instrumentação para a pesquisa

O app grava um CSV por sessão em `Application.persistentDataPath/sessions/<session_id>.csv`, com um evento por linha:

```text
session_id, participant_code, app_version, device_model, step_index, step_id, event, t_ms, utc, fps_avg_1s, detail
```

| Grupo | Eventos |
|---|---|
| Sessão | `session_started`, `session_completed` |
| Etapa | `step_started`, `step_completed` |
| Rastreamento | `reference_detected`, `tracking_lost` |
| Validação | `validation_started`, `validation_passed`, `validation_failed`, `validation_cancelled` |
| Observador | `observer_intervention` |
| Modo | `mode_changed` (RA ↔ demonstração 3D) |
| Desempenho | `fps_sample` (a cada 5 s) |

O `detail` do `session_started` registra o modo, o kit e se as peças são virtuais (ex.: `mode=ar;kit=cpu:intel_lga1700:1|…;pecas=virtuais`). Sessões no modo 3D ou com peças virtuais devem ser separadas na análise.

Medidas derivadas: **latência de rastreamento** (`reference_detected − step_started`), **tempo de confirmação** (`validation_passed − validation_started`) e **tempo por etapa** (`step_completed − step_started`). `t_ms` vem de um relógio monotônico e `utc` serve para cruzar com o vídeo da bancada.

---

## 12. Estrutura do Projeto

```text
Upx8---Projeto-MONTAR/
│
├── Assets/
│   ├── Data/                 roteiro (Roteiro_bancada_v0), etapas (Steps/) e catálogo de peças (Pecas/)
│   ├── Dev/PreviewOverlay/   spec da montagem, gerador do conteúdo e cenas de prévia (fora do build)
│   ├── Editor/MontAR/        construtores da interface e da cena, exportador do roteiro, tarefas em linha de comando
│   ├── Fonts/                Plus Jakarta Sans (licença OFL)
│   ├── Materials/            materiais dos overlays, da bancada e do vidro da interface
│   ├── Models/               vazio: os overlays são formas geradas por código
│   ├── Prefabs/              UI/MontAR_UI, Overlays/ (um por etapa), Bancada/ (placa genérica)
│   ├── Scenes/               MontAR.unity (única cena do build)
│   ├── Scripts/              código do app: AR, Data, Flow, Metrics, Persistence, UI, Validation
│   ├── Settings/             perfis do URP (Mobile e PC)
│   ├── Shaders/              vidro da interface e desfoque
│   ├── Tests/                EditMode/ (lógica) e PlayMode/ (fluxo de ponta a ponta)
│   ├── TextMesh Pro/         recursos do TextMeshPro
│   ├── Textures/             ReferenceImages/ (marcador), UI/ (ícones), Bancada/ (tapete e placa)
│   └── XR/                   configurações do XR Plug-in Management (ARCore)
│
├── Packages/                 manifest.json e packages-lock.json
├── ProjectSettings/
│
├── docs/
│   ├── diagrams/             diagramas (.mmd de origem e .png)
│   ├── documents/            artigos V1 e V4, pitch, roteiro de montagem
│   │   └── marcadores/       PDF do marcador para impressão
│   ├── images/               capturas das telas e GIF do fluxo
│   └── simulador/            simulador web da montagem (gerado)
│
├── tools/                    scripts Python (marcadores, texturas, ícones, simulador web)
│
├── .gitattributes            regras do Git LFS
├── .gitignore
├── CHANGELOG.md
├── CLAUDE.md                 contexto do projeto para o assistente de código
├── README.md
└── README-PREENCHIMENTO.md   guia de preenchimento do repositório-modelo
```

### Pastas principais

| Pasta | Finalidade |
|---|---|
| `Assets/Scripts` | Código produzido pela equipe |
| `Assets/Data` | Roteiro, etapas (`Steps/Bancada`) e catálogo de peças (`Pecas`), editáveis sem alterar código |
| `Assets/Editor/MontAR` | Ferramentas de Editor: montam a interface e a cena, exportam o roteiro e rodam tarefas em linha de comando (`BatchTasks`) |
| `Assets/Dev/PreviewOverlay` | `spec_montagem.json`, fonte dos overlays e da placa genérica, e o gerador `MontagemPreviewBuilder` |
| `Assets/Scenes` | `MontAR.unity`, com AR Session, XR Origin, interface e bancada 3D |
| `Assets/Prefabs` | Overlays de cada etapa, placa genérica e interface |
| `Assets/Textures/ReferenceImages` | Marcador A e biblioteca de imagens de referência do ARCore |
| `Assets/Tests` | Testes automatizados: EditMode (lógica) e PlayMode (fluxo completo na cena) |
| `docs/diagrams` | Diagramas técnicos |
| `docs/images` | Capturas das telas e evidências |
| `docs/documents` | Artigos, pitch e roteiro de montagem (base do manual e do vídeo) |
| `docs/documents/marcadores` | Marcador pronto para imprimir (A4, tamanho real) |
| `docs/simulador` | Simulador web da montagem, gerado por `tools/gerar_simulador_web.py` |
| `tools` | `gerar_marcador.py`, `gerar_texturas_previa.py`, `gerar_icones_ui.py` e `gerar_simulador_web.py` |

---

## 13. Principais Scripts

Todos os scripts ficam em `Assets/Scripts/`, no namespace `MontAR`. A coluna "Testes" indica se há teste automatizado (seção 22).

| Script | Pasta | Responsabilidade | Testes |
|---|---|---|---|
| `ImageTrackingController.cs` | `AR/` | Recebe os eventos do `ARTrackedImageManager` e informa referências rastreadas e perdidas | — |
| `ARContentManager.cs` | `AR/` | Instancia o overlay da etapa (e a montagem virtual) como filho da imagem rastreada | — |
| `CameraPermission.cs` | `AR/` | Pede a permissão da câmera do Android | — |
| `VirtualAssembly.cs` / `OverlayPart.cs` | `AR/` | Peças virtuais: placa-mãe em 3D, peças montadas e animação do "Simular encaixe" | ✅ (PlayMode) |
| `OverlayMotion.cs` | `AR/` | Animação da peça fantasma até o encaixe | ✅ |
| `DemoStage.cs` | `AR/` | Bancada virtual da demonstração 3D, com câmera orbital | ✅ (PlayMode) |
| `PocDiagnosticsHud.cs` | `AR/` | Painel de diagnóstico: estado da sessão, FPS e tempo até a detecção | — |
| `AssemblyStep.cs` / `AssemblyProcedure.cs` / `PartCatalog.cs` | `Data/` | Etapa, roteiro e catálogo de peças como dados editáveis | ✅ (via fluxo e seleção) |
| `StepFlow.cs` | `Flow/` | Máquina de estados das etapas, com avanço bloqueado até validar | ✅ |
| `StepFlowController.cs` | `Flow/` | Liga o fluxo à cena: rastreamento, overlay, métricas e progresso | ✅ (PlayMode) |
| `ParticipantCode.cs` | `Flow/` | Validação do código anônimo e id da sessão | ✅ |
| `PartSelection.cs` / `StepFilter.cs` | `Flow/` | Kit escolhido e filtro das etapas pelo kit | ✅ |
| `GuidanceMode.cs` | `Flow/` | Modo RA ou demonstração 3D | ✅ |
| `ChecklistValidator.cs` / `ReferenceSeenValidator.cs` | `Validation/` | Validação por checklist e por marcador visto | ✅ |
| `SessionCsvLog.cs` / `SessionMetricsLogger.cs` / `FpsWindow.cs` | `Metrics/` | CSV de eventos, gravação em disco e média de FPS | ✅ |
| `SessionSummary.cs` | `Metrics/` | Resumo da tela de conclusão | ✅ |
| `ProgressStore.cs` | `Persistence/` | Salva e retoma o progresso e o kit em JSON | ✅ |
| `AssemblyHistory.cs` | `Persistence/` | Histórico de montagens concluídas (`history.json`) | ✅ |
| `AppController.cs` | `UI/` | Navegação entre telas, câmeras, verificação do ARCore e retomada | ✅ (PlayMode) |
| `WelcomeScreen`, `PartsScreen`, `StartScreen`, `AssemblyScreen`, `CompletionScreen`, `HistoryScreen` | `UI/` | As seis telas do app | ✅ (PlayMode) |
| `GlassBlurFeature.cs` | `UI/` | Passe do URP que desfoca o fundo para a interface de vidro | — |
| `AppBuilder.cs` / `RoteiroExporter.cs` / `BatchTasks.cs` | `Assets/Editor/MontAR/` | Geram a interface e a cena, exportam o roteiro e rodam build e testes em linha de comando | — |

---

## 14. Dependências

Pacotes declarados em `Packages/manifest.json`. A Unity baixa todos sozinha ao abrir o projeto (é preciso internet na primeira abertura).

```text
com.unity.xr.arfoundation            6.6.2
com.unity.xr.arcore                  6.6.2
com.unity.render-pipelines.universal 17.6.0
com.unity.inputsystem                1.20.0
com.unity.ugui                       2.6.0
com.unity.test-framework             1.8.0
com.unity.timeline                   6.6.0
com.unity.ai.inference               2.6.1
```

- `com.unity.xr.management` (4.7.0) e `com.unity.xr.core-utils` (2.6.0) entram como dependências do ARCore e do AR Foundation.
- Todos os pacotes acima vêm do **Unity Registry** (Package Manager).
- `com.unity.ai.inference` está instalado, mas o app não o usa: fica reservado para um possível reconhecimento por visão computacional.
- `com.coplaydev.unity-mcp` vem do GitHub ([CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)). É só uma ferramenta de desenvolvimento, que liga o Editor ao assistente de código, e não entra no APK. Exige o Git instalado para a Unity baixar.

Fora da Unity:

- **Google Play Services for AR** no celular, instalado pela Play Store (o Android pede na primeira execução).
- **Python 3 + Pillow** (`pip install pillow`), só para rodar os scripts de `tools/`.
- **three.js 0.160.0**, carregado pelo simulador web direto do jsDelivr.

---

## 15. Configuração do Ambiente

### Pré-requisitos

- Windows 10 ou 11;
- Unity Hub;
- Unity **6000.6.4f1** com o módulo **Android Build Support** (OpenJDK e Android SDK & NDK Tools);
- Git e **Git LFS**;
- para a RA: celular Android com suporte a ARCore ([lista do Google](https://developers.google.com/ar/devices)).

### Procedimento

1. Instale o Unity Hub e, por ele, a Unity 6000.6.4f1 com o módulo **Android Build Support** (marque OpenJDK e Android SDK & NDK Tools).
2. Instale o Git e o Git LFS e execute `git lfs install` uma vez na máquina.
3. Clone o repositório (seção 16), de preferência num caminho curto, como `C:\dev\MontAR`.
4. Abra o projeto pelo Unity Hub e aguarde a importação dos pacotes (a primeira abertura leva alguns minutos).
5. Em **Build Profiles**, confirme a plataforma **Android** (o projeto já é salvo com ela ativa).
6. Em **Project Settings › XR Plug-in Management › Project Validation**, confira se não há pendências na aba Android.

O projeto já vem configurado: ARCore ativo no Android, IL2CPP, ARM64, API mínima 29 e os renderer features do AR no `Mobile_Renderer`.

**Verificação do procedimento (31/10/2026):** o repositório foi clonado do GitHub numa pasta nova, sem a pasta `Library`, e aberto pela Unity 6000.6.4f1 em linha de comando. A importação e os 75 testes EditMode levaram 2 min 18 s, e o teste PlayMode de ponta a ponta passou em seguida (seção 22). O teste foi feito na mesma máquina de desenvolvimento, que já tinha a Unity e o Android Build Support instalados. Com o clone numa pasta de caminho muito longo, a importação mostrou um aviso `DirectoryNotFoundException` num pacote da Unity, sem afetar os testes; por isso a recomendação de caminho curto.

> **Windows com Smart App Control:** nesse modo, o Windows bloqueia a DLL do XR Simulation, então o modo Play no Editor não simula a RA. Ver seção 29.

---

## 16. Como Executar

```bash
git lfs install
git clone https://github.com/yuri-pro07/Upx8---Projeto-MONTAR.git
cd Upx8---Projeto-MONTAR
git lfs pull
```

### No Editor (sem celular)

1. Abra o Unity Hub.
2. Clique em **Add › Add project from disk** e selecione a pasta clonada.
3. Confirme a versão 6000.6.4f1 e abra o projeto.
4. Abra a cena `Assets/Scenes/MontAR.unity`.
5. Clique em **Play**. Na tela de início, escolha **Demonstração 3D** (e **Peças virtuais**, se quiser ver as peças encaixando).

### No celular Android

1. Abra o projeto e a cena `Assets/Scenes/MontAR.unity`.
2. Em **Build Profiles**, confirme a plataforma Android.
3. No celular, ative as **Opções do desenvolvedor** e a **Depuração USB** e conecte-o ao computador.
4. Clique em **Build and Run**.

Sem cabo, dá para gerar o APK com **Build** e copiar o arquivo para o celular; para instalar, abra o APK pelo gerenciador de arquivos e permita "instalar apps desconhecidos".

Também dá para gerar o APK em linha de comando, sem abrir o Editor:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod MontAR.EditorTools.BatchTasks.BuildAndroid -logFile build.log
```

O APK foi gerado com sucesso em 05/10/2026 (prova de conceito, 45 MB) e em 27/10/2026 (app completo, 47,9 MB), mas **ainda não foi instalado em nenhum celular**.

### Rodar os testes automatizados

No Editor: **Window › General › Test Runner**, abas **EditMode** e **PlayMode**, **Run All**. Em linha de comando:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults editmode.xml
```

### Marcador da RA

Imprima `docs/documents/marcadores/MontAR_Marcador_A_15cm_A4.pdf` em **100% (tamanho real)**, sem "ajustar à página", e confira com uma régua: a borda externa da moldura preta deve medir 15 cm. Com outro tamanho, o overlay aparece em escala errada.

### Simulador web (opcional)

`docs/simulador/index.html` mostra a mesma bancada e o mesmo roteiro do app no navegador, com a mesma regra de validação, para quem quer ver a montagem sem celular. Basta abrir o arquivo (precisa de internet para carregar o three.js). Ele **não grava CSV** e não faz parte do experimento. Depois de mudar o spec, o catálogo ou as texturas, gere a página de novo com `python tools/gerar_simulador_web.py`. Foi testado só no Chrome headless (27/10/2026).

---

## 17. Como Utilizar a Aplicação

1. Abra o aplicativo. Se houver uma montagem interrompida, o app pergunta se você quer continuar.
2. Leia a **apresentação** (deslize ou toque em **Próximo**) ou toque em **Pular**.
3. Na tela de **peças**, confira o kit: use ‹ › para trocar o modelo e − + para a quantidade. O rodapé mostra quantas etapas o roteiro terá. Toque em **Continuar**.
4. Na tela de **início**, digite o código do participante (ex.: `P01`) e, se quiser, um nome para a montagem. Escolha **Kit de verdade** ou **Peças virtuais** e **Câmera (RA)** ou **Demonstração 3D**. Toque em **Iniciar montagem**.
5. No modo RA, autorize a câmera e aponte para o marcador A impresso. A peça fantasma aparece sobre o encaixe da etapa. Com peças virtuais, a placa-mãe também aparece em 3D sobre o marcador.
6. Execute a etapa com a peça real, ou toque em **Simular encaixe** para ver a peça virtual entrar no lugar.
7. Toque em **Validar etapa**, marque os itens que conferem e toque em **Confirmar**. Se algum ficar desmarcado, o app mostra o que faltou e a etapa volta para correção.
8. Repita até a última etapa. A tela de **conclusão** mostra o tempo, as etapas, as validações reprovadas e o tempo por etapa. Em **Ver histórico de montagens** ficam as montagens já concluídas no aparelho.

Para o pesquisador:

- o toque longo (1,2 s) no botão do observador, no canto superior direito da montagem, registra uma **intervenção**;
- o toque longo (2 s) na versão, na apresentação, liga o painel de diagnóstico (FPS e tempo até a detecção);
- o CSV da sessão fica em `sessions/` na pasta de dados do app e é copiado com `adb pull`.

![Fluxo do app no modo 3D com peças virtuais](docs/images/fluxo-montagem-3d.gif)

---

## 18. Fluxo de Funcionamento

<img src="docs/diagrams/fluxo-funcionamento.png" alt="Fluxo de funcionamento" width="460">

Fonte: [`docs/diagrams/fluxo-funcionamento.mmd`](docs/diagrams/fluxo-funcionamento.mmd).

---

## 19. Caso de Uso

<img src="docs/diagrams/caso-de-uso.png" alt="Caso de uso" width="420">

Fonte: [`docs/diagrams/caso-de-uso.mmd`](docs/diagrams/caso-de-uso.mmd). Os diagramas são gerados com o Mermaid CLI:

```bash
npx -p @mermaid-js/mermaid-cli@11.4.2 mmdc -i docs/diagrams/caso-de-uso.mmd -o docs/diagrams/caso-de-uso.png -s 2 -b white
```

---

## 20. Fluxo de Navegação

```text
Apresentação (como funciona)           ← pergunta se quer continuar uma montagem interrompida
    ↓
Seleção de peças (modelo e quantidade) → Histórico
    ↓
Início (código, nome opcional, kit real ou peças virtuais, Câmera RA ou Demonstração 3D)
    ↓
Montagem: etapa atual (instrução + alerta) e peça fantasma (RA ou 3D)
    ↓
Checklist de validação  (reprovado → volta para a etapa)
    ↓
Próxima etapa  (repete até a última)
    ↓
Conclusão (resumo da sessão) → Histórico de montagens → Nova montagem
```

---

## 21. Interface

Capturas reais do app **no Editor**, no modo demonstração 3D com peças virtuais, feitas pelo teste automatizado de ponta a ponta (interface de vidro, 27/10/2026). Não são capturas do celular nem da RA, que ainda não foi testada.

| Apresentação | Peças | Início |
|---|---|---|
| <img src="docs/images/tela-apresentacao.png" alt="Apresentação" width="220"> | <img src="docs/images/tela-pecas.png" alt="Seleção de peças" width="220"> | <img src="docs/images/tela-inicio.png" alt="Início" width="220"> |

| CPU (peça fantasma) | CPU encaixada (simulada) | RAM encaixada | Verificação final |
|---|---|---|---|
| <img src="docs/images/montagem-3d-cpu.png" alt="Etapa da CPU" width="180"> | <img src="docs/images/montagem-3d-cpu-encaixada.png" alt="CPU encaixada" width="180"> | <img src="docs/images/montagem-3d-ram.png" alt="RAM encaixada" width="180"> | <img src="docs/images/montagem-3d-completa.png" alt="Verificação final" width="180"> |

| Validação | Erro da validação | Conclusão | Histórico |
|---|---|---|---|
| <img src="docs/images/tela-validacao.png" alt="Checklist" width="180"> | <img src="docs/images/tela-erro-validacao.png" alt="Erro da validação" width="180"> | <img src="docs/images/tela-conclusao.png" alt="Conclusão" width="180"> | <img src="docs/images/tela-historico.png" alt="Histórico" width="180"> |

| Tela | Finalidade | Principais elementos | Ação do usuário |
|---|---|---|---|
| Apresentação | Explicar o app antes de começar | 6 páginas (Aponta, Guia, Validação, Registra), indicador de página, **Pular** | Deslizar ou tocar em **Próximo** |
| Peças | Definir o kit, que define o roteiro | Uma linha por peça com modelo, detalhes e quantidade; total de etapas no rodapé; atalho **Histórico** | Trocar modelo (‹ ›) e quantidade (− +), **Continuar** |
| Início | Preparar a sessão | Código do participante, nome opcional, Kit de verdade × Peças virtuais, Câmera (RA) × Demonstração 3D, lista do roteiro | Preencher e tocar em **Iniciar montagem** |
| Montagem | Guiar a etapa atual | Etapa e progresso, situação da etapa, peça fantasma (RA ou 3D), instrução, alerta, **Simular encaixe**, **Validar etapa**, troca Câmera / 3D, botão do observador | Executar a etapa e tocar em **Validar etapa** |
| Validação | Conferir a etapa antes de liberar a próxima | Checklist com 2 ou 3 pontos críticos, **Voltar**, **Confirmar (n/3)** | Marcar os itens e confirmar |
| Erro da validação | Mostrar o que faltou | Mensagem em vermelho com os itens desmarcados | Corrigir e tocar em **Validar de novo** |
| Conclusão | Resumir a sessão | Tempo total, etapas validadas, validações reprovadas, intervenções, tempo por etapa, local do CSV | **Ver histórico** ou **Nova montagem** |
| Histórico | Rever montagens anteriores | Nome, data, código, modo, tempo, erros apontados e etapas de cada montagem | Abrir ou esconder as etapas, **Nova montagem** |

---

## 22. Testes Técnicos

### 22.1 Testes realizados

| Teste | Procedimento | Resultado esperado | Resultado obtido | Status |
|---|---|---|---|---|
| Testes automatizados (05/10/2026) | Testes EditMode de `MontAR.Tests.EditMode` no Test Runner | Todos passam | 41 de 41 aprovados: fluxo, CSV, validação, progresso, código e FPS | ✅ |
| Build Android (05/10/2026) | APK da prova de conceito pelo Build Profiles (IL2CPP, ARM64) | APK sem erros | APK de 45 MB, 0 erros | ✅ |
| Validação do projeto XR (05/10/2026) | XR Plug-in Management › Project Validation, abas Android e Desktop | Nenhuma pendência | Nenhuma pendência | ✅ |
| Qualidade do marcador A (05/10/2026) | `arcoreimg eval-img` no PNG do marcador | Nota ≥ 75 (recomendação do Google) | Nota 100 | ✅ |
| Testes automatizados (07/10/2026) | Testes EditMode em linha de comando (`-runTests -testPlatform EditMode`) | Todos passam | 62 de 62 aprovados, incluindo kit de peças, filtro de etapas, modo 3D e resumo da sessão | ✅ |
| Build Android do app completo (07/10/2026) | `BatchTasks.BuildAndroid` em linha de comando (IL2CPP, ARM64) | APK sem erros | APK de 46,7 MB, 0 erros, 2 min 26 s | ✅ |
| Fluxo completo no Editor (07/10/2026) | Teste PlayMode na cena `MontAR.unity`, modo 3D, com uma validação reprovada de propósito | Chega à conclusão e o CSV tem o kit, o modo, a reprovação e as 9 etapas | Aprovado (cerca de 13 s) | ✅ |
| Build Android do app completo (27/10/2026) | `BatchTasks.BuildAndroid` em linha de comando | APK sem erros | APK de 47,9 MB, 0 erros | ✅ |
| Simulador web (27/10/2026) | Chrome headless, computador e celular emulado, kit padrão (9 etapas) e kit com AM5, 4 pentes, HD, 12V-2x6 e fonte com chave (10 etapas) | Percorre todas as etapas com a regra de validação | Aprovado nos modos "na tela" e "na sua frente" (câmera simulada) | ✅ |
| Clone limpo + testes EditMode (31/10/2026) | `git clone` do GitHub numa pasta nova, `git lfs pull` e Unity 6000.6.4f1 com `-runTests -testPlatform EditMode` | Projeto importa sem erro de compilação e todos os testes passam | Importação e testes em 2 min 18 s; **75 de 75 aprovados** | ✅ |
| Fluxo completo no clone limpo (31/10/2026) | Teste PlayMode `AppFlowSmokeTests`: apresentação, peças (troca de CPU, RAM de 1 a 4 pentes, SSD, GPU, fonte com chave), código inválido e válido, 9 etapas com **Simular encaixe** e uma validação reprovada, conclusão e histórico | Chega ao fim; o CSV e o `history.json` são gravados com a sessão | **1 de 1 aprovado** em 22,8 s; 19 capturas geradas | ✅ |

### 22.2 Testes planejados em celular (artigo V4)

Serão feitos no smartphone e com o kit definitivos, registrando modelo, versão do Android, commit do app, iluminância, distâncias e ângulos.

| Teste | Procedimento | Resultado esperado | Resultado obtido | Status |
|---|---|---|---|---|
| Inicialização | Abrir o app, aceitar a câmera e iniciar a RA | App abre, pede a permissão e mostra a câmera | — | ⬜ |
| Detecção do marcador | Apontar para o marcador A em luz de laboratório | Mediana de até 2 s até `reference_detected` (RNF02) | — | ⬜ |
| Posicionamento | Peça virtual sobre gabarito impresso, 10 reposicionamentos por etapa | Desvio ≤ 5 mm (RNF04) | — | ⬜ |
| Estabilidade | Mover o celular e cobrir 1/3 do marcador | Realinha em até 2 s (RNF04) | — | ⬜ |
| Taxa de quadros | FPS do CSV numa sessão completa | Média ≥ 30 FPS, sem 5 s abaixo de 20 (RNF03) | — | ⬜ |
| Modo avião | Sessão completa sem internet | Sessão completa e CSV gravado (RNF05) | — | ⬜ |
| Retomada | Fechar e reabrir o app no meio da montagem | Retoma na etapa salva | — | ⬜ |
| Exportação CSV | Concluir uma sessão e copiar com `adb pull` | CSV completo, sem dados pessoais (RNF06) | — | ⬜ |
| Piloto de interação | Participantes fora da amostra | No máximo um pedido de esclarecimento por participante (RNF07) | — | ⬜ |

---

## 23. Dispositivos Testados

| Dispositivo | Sistema | Versão | Resultado |
|---|---|---|---|
| PC de desenvolvimento (Editor Unity 6000.6.4f1) | Windows 11 Pro | 10.0.26200 | Fluxo completo aprovado no modo 3D (testes de 07/10 e 31/10). A RA não roda no Editor desta máquina (seção 29, problema 1) |
| Celular Android | — | — | **Nenhum celular testado até 31/10/2026** |

---

## 24. Limitações Conhecidas

- **Sem teste em celular:** a RA (detecção do marcador, overlay sobre a bancada, permissão da câmera, desfoque da interface sobre a imagem da câmera) não foi executada em nenhum aparelho. Nenhum valor de tempo de detecção, FPS ou estabilidade foi medido.
- **Dependência de marcador:** o ARCore não reconhece objetos 3D no Android, então a RA precisa do marcador impresso em tamanho real (15 cm).
- **Placa genérica:** os encaixes são posicionados para uma placa ATX genérica, com posições aproximadas, até o kit do laboratório ser definido.
- **Validação por autoconfirmação:** o checklist depende do julgamento do próprio usuário e pode aprovar uma etapa com falha. A validação do componente pela câmera (RF17) ainda não existe, e nem ela vai conferir a posição exata da peça.
- **Mãos ocupadas:** a montagem exige as duas mãos. No experimento o celular ficará num suporte articulado; fora dele, o usuário alterna entre segurar o aparelho e montar.
- **Tempo maior:** a validação de cada etapa pode aumentar o tempo total da montagem (hipótese H2).
- **Montagem em bancada:** o roteiro não inclui o gabinete.
- **Sessões fora da medição:** no modo 3D a etapa não espera o marcador, e com peças virtuais não há instalação real. Essas sessões não servem para medir latência de rastreamento nem falhas de instalação.
- **Simulador web:** o giroscópio real e o WebXR não foram testados em aparelho.
- **XR Simulation bloqueado:** no PC de desenvolvimento, o Smart App Control impede simular a RA no Editor.

---

## 25. Evidências de Funcionamento

Todas as evidências até agora são do **Editor**, no modo demonstração 3D. Ainda não há evidência da RA no celular.

- **GIF do fluxo completo** (seção 17): [`docs/images/fluxo-montagem-3d.gif`](docs/images/fluxo-montagem-3d.gif), montado com as 19 capturas geradas pelo teste PlayMode no clone limpo de 31/10/2026.
- **Capturas das telas** (seção 21) em `docs/images/`.
- **Testes automatizados** (seção 22): 75 de 75 EditMode e 1 de 1 PlayMode aprovados a partir de um clone do GitHub.
- **Marcador:** nota 100 no `arcoreimg`.

Vídeo de demonstração: ainda não gravado.

---

## 26. Repositórios e Recursos

| Recurso | Link |
|---|---|
| Código-fonte | https://github.com/yuri-pro07/Upx8---Projeto-MONTAR |
| Repositório-modelo da disciplina | https://github.com/dr-ohata/upx-modelo |
| Artigo V4 (atual) | [`docs/documents/artigo-v4-montar.docx`](docs/documents/artigo-v4-montar.docx) |
| Artigo V1 | [`docs/documents/artigo-v1-montar.docx`](docs/documents/artigo-v1-montar.docx) |
| Pitch | [`docs/documents/pitch-montar.pptx`](docs/documents/pitch-montar.pptx) |
| Roteiro de montagem (base do manual e do vídeo) | [`docs/documents/roteiro-bancada-v0.md`](docs/documents/roteiro-bancada-v0.md) |
| Marcador para impressão | [`docs/documents/marcadores/MontAR_Marcador_A_15cm_A4.pdf`](docs/documents/marcadores/MontAR_Marcador_A_15cm_A4.pdf) |
| Histórico de versões | [`CHANGELOG.md`](CHANGELOG.md) |

---

## 27. Cronograma Técnico

Situação em 31/10/2026.

| Atividade | Responsável | Período | Status |
|---|---|---|---|
| Pesquisa e estado da arte (artigo V1) | Equipe | 10/09 – 05/10 | ✅ |
| Estrutura do repositório e configuração do projeto Unity com ARCore | Yuri | 05/10 | ✅ |
| Lógica do fluxo de etapas, métricas e progresso, com testes | Yuri | 05/10 – 07/10 | ✅ |
| Interface do app (primeira versão) | Pedro | 06/10 – 07/10 | ✅ |
| Roteiro de montagem v0 (9 a 10 etapas, exportado do app) | Pedro | 06/10 – 07/10 | ✅ (ajustar ao kit do laboratório) |
| Teste de usabilidade da interface e ajustes (2 rodadas) | Equipe | 23/10 | ✅ |
| Artigo V4: requisitos, arquitetura e plano de testes | Equipe | até 24/10 | ✅ |
| Peças virtuais, "Simular encaixe" e interface de vidro | Yuri | 27/10 | ✅ |
| Documentação técnica do repositório (este README) | Equipe | 31/10 | ✅ |
| Prova de conceito no celular: rastreamento e sobreposição | Yuri | depende de um aparelho com ARCore | 🚧 |
| Validação do componente pela câmera e liberação do pesquisador (RF17 e RF18) | Yuri | até 09/11 | ⬜ |
| Manual técnico e tutorial em vídeo (condições de controle) | Pedro e Enzo | até 09/11 | ⬜ |
| Protocolo experimental e instrumentos (checklist de falhas, SUS) | Enzo | até 09/11 | 🚧 (definido no artigo V4) |
| **Congelamento do protótipo** | Equipe | 09/11 | ⬜ |
| Experimento: coleta e análise de dados | Equipe (análise: Enzo) | 10/11 – 01/12 | ⬜ |
| Apresentação final | Equipe | 03/12 | ⬜ |

Esforço estimado: 3 integrantes × 16 semanas × 6 h semanais = **288 h**.

---

## 28. Controle de Versão

Os commits seguem o padrão do modelo, com a descrição em português:

```text
feat: adiciona o modo Peças virtuais com Simular encaixe e redesenha as telas em vidro
feat: aplica feedback do teste de usabilidade na interface e no roteiro
chore: configura o projeto Unity com AR Foundation e ARCore para Android
docs: atualiza README, CHANGELOG, CLAUDE.md e capturas
```

- Até agora o desenvolvimento ficou na branch `main`, sem branches de funcionalidade. A `main` sempre compila e passa nos testes.
- Commits separados por assunto (configuração, funcionalidade, documentação), para registrar a evolução do projeto (`git log --oneline`).
- **Tags** marcam os marcos do [CHANGELOG](CHANGELOG.md): `v0.1.0` (estrutura inicial e configuração da RA). As próximas são `v0.2.0` (prova de conceito no celular), `v0.3.0` (fluxo com validação no celular) e `v1.0.0` (protótipo congelado, 09/11).
- PNG, PDF, GIF e fontes passam pelo Git LFS (`.gitattributes`).
- Builds (`.apk`, `.aab`), keystores, `.env` e as pastas geradas pela Unity (`Library`, `Logs`, `UserSettings`, `Builds`) ficam fora do repositório (`.gitignore`). O repositório não tem senhas, tokens nem chaves.

---

## 29. Problemas Encontrados e Soluções

### Problema 1: XR Simulation não funciona no Editor

**Problema:** no modo Play, o console mostra `DllNotFoundException: XRSimulationSubsystem` e a RA não é simulada.
**Causa:** o **Smart App Control** do Windows 11 bloqueia a DLL nativa do XR Simulation e as DLLs geradas pelo Burst em `Library/BurstCache`.
**Solução:** não desligamos o Smart App Control, porque no Windows 11 isso não pode ser revertido sem reinstalar o sistema. Para testar o fluxo sem RA, o app ganhou o modo demonstração 3D, e o teste da RA fica para o celular.

### Problema 2: erro ao rastrear imagem que não está na biblioteca

**Problema:** `ArgumentNullException` em `ImageTrackingController` durante o modo Play.
**Causa:** o provedor de RA entregou uma imagem rastreada sem referência na `XRReferenceImageLibrary`, com nome nulo, e o nome era usado como chave de dicionário.
**Solução:** imagens sem nome agora são ignoradas, com um aviso no console.

### Problema 3: marcador com tamanho físico ambíguo

**Problema:** a primeira versão do marcador tinha uma margem branca por fora da moldura preta. Impresso com 15 cm, a moldura mediria cerca de 13,8 cm.
**Causa:** o ARCore usa o tamanho da imagem inteira, incluindo a margem, que some no papel branco.
**Solução:** a moldura passou a encostar na borda da imagem. A borda preta impressa mede exatamente o tamanho declarado na biblioteca (15 cm).

### Problema 4: botão Validar travado em etapas sem marcador

**Problema:** no teste de ponta a ponta, a partir da 2ª etapa do modo 3D o botão **Validar etapa** ficava desabilitado.
**Causa:** o `StepFlow` emitia `step_started` com a fase "Localizando" e só depois passava para "Guiando", sem novo evento, e a interface ficava com a fase antiga.
**Solução:** a fase é decidida antes do `step_started`. Um teste EditMode cobre o caso.

### Problema 5: referências nulas ao montar a cena por script

**Problema:** o catálogo e o roteiro ficavam vazios no `AppController` e no `StepFlowController` depois de montar a cena pelo construtor.
**Causa:** abrir uma cena pelo `EditorSceneManager` descarrega os assets sem uso, e a referência guardada antes virava nula.
**Solução:** o construtor carrega o catálogo, o roteiro e o prefab pelo caminho, depois de abrir a cena.

### Problema 6: nenhum celular com ARCore para testar

**Problema:** a prova de conceito da RA não pôde ser executada em celular.
**Causa:** o único aparelho disponível em 05/10 (Samsung Galaxy A06) não conectou pela depuração USB, e a equipe não teve outro aparelho com ARCore até 31/10.
**Solução (parcial):** o fluxo inteiro passou a ser testado no Editor, no modo 3D, com testes automatizados. O APK fica pronto para instalar por cabo ou copiando o arquivo. O teste no celular continua pendente e é o próximo passo do cronograma.

### Problema 7: sem o kit, a demonstração não mostrava a montagem

**Problema:** para a demonstração de usabilidade não haveria peças de computador, e a RA mostraria só a peça fantasma sobre o vazio.
**Causa:** o app foi feito para guiar peças reais; sem elas, o avaliador não vê nada encaixar.
**Solução:** modo **Peças virtuais** (27/10): a placa-mãe aparece em 3D sobre o marcador e o botão **Simular encaixe** leva a peça da etapa até o lugar, onde ela fica montada. O checklist continua obrigatório, e o CSV marca essas sessões (`pecas=virtuais`) para ficarem fora da análise.

### Problema 8: toques errados e dúvidas no teste de usabilidade

**Problema:** o teste de usabilidade de 23/10 apontou dúvidas sobre o kit (só havia CPU Intel), sobre onde segurar a memória e o SSD e sobre a tensão da fonte, além do botão "Voltar" ao lado do "Próximo", fácil de tocar por engano.
**Causa:** o roteiro cobria um único kit e não dizia como manusear as peças, e a apresentação tinha os dois botões juntos na base.
**Solução:** "Voltar" foi para o canto superior esquerdo; o catálogo ganhou CPU AMD AM5, RAM de 1 a 4 pentes, SSD SATA e HD, placa de vídeo com 12V-2x6 e fonte com chave 110/220 V (com etapa própria); RAM, SSD e CPU passaram a indicar onde segurar.

---

## 30. Melhorias Futuras

- validação do componente pela câmera, com um marcador por peça (RF17), e liberação do pesquisador (RF18);
- reconhecimento do componente sem marcador (Vuforia Model Targets ou modelo de visão computacional);
- validação visual automática da posição de cada componente;
- roteiros para outros modelos de placa-mãe e para a montagem dentro do gabinete;
- painel para o professor acompanhar vários alunos na mesma aula;
- suporte a iOS (ARKit).

---

## 31. Custos e Recursos

| Recurso | Tipo | Custo/Condição |
|---|---|---|
| Esforço da equipe | Pessoas | 288 h estimadas (3 × 16 semanas × 6 h) |
| Unity | Software | Gratuita para uso acadêmico |
| AR Foundation e ARCore | Software | Gratuitos |
| Python, Pillow, three.js, Mermaid | Software | Gratuitos (código aberto) |
| Computador de desenvolvimento | Hardware | Equipamento da equipe |
| Smartphone Android com ARCore | Hardware | A obter (equipe ou instituição) |
| Suporte articulado para o celular | Hardware | Baixo custo, a obter |
| Kit de componentes para montagem | Hardware | Laboratório da Facens (kit ainda não definido) |
| Impressão do tapete e do marcador | Material | Baixo custo |

---

## 32. Licença

Projeto acadêmico desenvolvido para fins educacionais na disciplina UPX: Realidade Aumentada, do Centro Universitário Facens. A fonte Plus Jakarta Sans é distribuída sob a SIL Open Font License, e o three.js, sob a licença MIT.

---

## 33. Autores

- **Enzo Zorzetto** (RA 234974): pesquisa e experimento
- **Yuri Peruzzo** (RA 248732): desenvolvimento Unity, C# e RA
- **Pedro Ricci Gomes Nascimento** (RA 234923): produto, conteúdo e documentação

**Curso:** Engenharia de Computação
**Professor:** Prof. Daniel Ohata
**Instituição:** Centro Universitário Facens
**Disciplina:** UPX: Realidade Aumentada
**Semestre:** 2026.2

---

## Checklist de Entrega

- [x] Código-fonte atualizado
- [x] README completamente preenchido
- [x] Versões das tecnologias registradas
- [x] Arquitetura apresentada
- [x] Funcionamento da RA explicado
- [x] Estrutura de pastas documentada
- [x] Scripts principais descritos
- [x] Procedimento de instalação testado (clone limpo, 31/10/2026)
- [x] Procedimento de execução testado no Editor (falta no celular)
- [x] Screenshots reais adicionados (do Editor)
- [x] Testes registrados
- [x] Dispositivos testados informados (nenhum celular até agora)
- [x] Limitações conhecidas documentadas
- [x] Evidência de funcionamento adicionada (Editor)
- [x] Commits representam a evolução do projeto
