# Roteiro de montagem em bancada (bancada_v0)

> Gerado a partir de `Assets/Data/Roteiro_bancada_v0.asset` e `Assets/Data/Pecas/Catalogo_pecas_v0.asset` em 27/10/2026 (menu **MontAR › App › Exportar roteiro**). Não edite este arquivo à mão: mude as etapas na Unity (ou no `spec_montagem.json`) e exporte de novo.

O app, o **manual técnico** e o **tutorial em vídeo** das condições de controle precisam seguir exatamente estas etapas, na mesma ordem e com o mesmo kit (CLAUDE.md, §2).

**Rascunho v0.** Placa ATX genérica com posições aproximadas: o kit do laboratório ainda não foi definido (decisão D5). Montagem em bancada, sem gabinete (decisão D4 em aberto).

## Peças

| Peça | Modelos (o primeiro é o padrão) | Quantidade aceita | Padrão |
|---|---|---|---|
| Placa-mãe (`placa_mae`) | ATX genérica · LGA1700/AM5 (`atx_generica_lga1700`) | 1 | 1 |
| Processador (CPU) (`cpu`) | Intel · soquete LGA1700 (`intel_lga1700`)<br>AMD · soquete AM5 (`amd_am5`) | 1 | 1 |
| Cooler do processador (`cooler`) | Torre a ar · 4 heat pipes · 120 mm (`cooler_ar_4pinos`)<br>Cooler box (de fábrica) · sem heat pipes (`cooler_box`)<br>Torre dupla · 6 heat pipes · 2 fans (`cooler_torre_dupla`) | 1 | 1 |
| Memória RAM (`ram`) | DDR5 · 16 GB · 5600 MT/s · CL40 (`ddr5_dimm`)<br>DDR5 · 8 GB · 4800 MT/s · CL40 (`ddr5_8gb_4800_cl40`)<br>DDR5 · 32 GB · 6000 MT/s · CL30 (`ddr5_32gb_6000_cl30`) | 1 a 4 | 2 |
| Armazenamento (`ssd`) | SSD M.2 2280 · NVMe (`ssd_m2_2280_nvme`)<br>SSD SATA 2,5" (`ssd_sata_25`)<br>HD SATA 3,5" · 7200 rpm (`hd_sata_35`) | 0 a 1 (opcional) | 1 |
| Placa de vídeo (`gpu`) | PCIe x16 · 1 cabo PCIe 8 pinos (6+2) (`gpu_pcie_x16`)<br>PCIe x16 · sem cabo de energia (até 75 W) (`gpu_sem_cabo`)<br>PCIe x16 · conector 12V-2x6 (16 pinos) (`gpu_12v2x6`) | 0 a 1 (opcional) | 1 |
| Fonte de alimentação (`fonte`) | ATX bivolt automática (100–240 V) (`fonte_atx`)<br>ATX com chave 110/220 V (`fonte_atx_chave`) | 1 | 1 |

## Etapas

A coluna "entra quando" diz de qual peça a etapa depende. Com o kit padrão (2 pentes de RAM, SSD e placa de vídeo), o roteiro tem 9 etapas.

| # | Etapa | Entra quando | ID no CSV |
|---|---|---|---|
| 1 | Coloque a placa-mãe no tapete | Placa-mãe | `placa_tapete` |
| 2 | Encaixe a CPU no soquete | Processador (CPU), modelo Intel · soquete LGA1700 (`intel_lga1700`) | `cpu_soquete` |
| 3 | Encaixe a CPU no soquete | Processador (CPU), modelo AMD · soquete AM5 (`amd_am5`) | `cpu_soquete_am5` |
| 4 | Instale o cooler e ligue no CPU_FAN | Processador (CPU), modelo Intel · soquete LGA1700 (`intel_lga1700`) | `cooler_cpu` |
| 5 | Instale o cooler e ligue no CPU_FAN | Processador (CPU), modelo AMD · soquete AM5 (`amd_am5`) | `cooler_cpu_am5` |
| 6 | Instale a RAM no slot A2 | Memória RAM: exatamente 1 pente | `ram_slot_a2` |
| 7 | Instale a RAM nos slots A2 e B2 | Memória RAM: exatamente 2 pentes | `ram_slots` |
| 8 | Instale a RAM nos slots A2, B2 e A1 | Memória RAM: exatamente 3 pentes | `ram_3_slots` |
| 9 | Instale a RAM nos quatro slots | Memória RAM: exatamente 4 pentes | `ram_4_slots` |
| 10 | Instale o SSD M.2 | Armazenamento no kit, modelo SSD M.2 2280 · NVMe (`ssd_m2_2280_nvme`) | `ssd_m2` |
| 11 | Ligue o cabo de 24 pinos | Fonte de alimentação | `atx_24p` |
| 12 | Ligue o cabo EPS 12V da CPU | Fonte de alimentação | `eps_8p` |
| 13 | Ligue o SSD SATA | Armazenamento no kit, modelo SSD SATA 2,5" (`ssd_sata_25`) | `ssd_sata` |
| 14 | Ligue o HD SATA | Armazenamento no kit, modelo HD SATA 3,5" · 7200 rpm (`hd_sata_35`) | `hd_sata` |
| 15 | Encaixe a placa de vídeo no PCIe x16 | Placa de vídeo no kit, modelo PCIe x16 · 1 cabo PCIe 8 pinos (6+2) (`gpu_pcie_x16`) | `gpu_pcie` |
| 16 | Encaixe a placa de vídeo no PCIe x16 | Placa de vídeo no kit, modelo PCIe x16 · sem cabo de energia (até 75 W) (`gpu_sem_cabo`) | `gpu_pcie_sem_cabo` |
| 17 | Encaixe a placa de vídeo e o cabo 12V-2x6 | Placa de vídeo no kit, modelo PCIe x16 · conector 12V-2x6 (16 pinos) (`gpu_12v2x6`) | `gpu_pcie_12v2x6` |
| 18 | Confira a chave 110/220 V da fonte | Fonte de alimentação, modelo ATX com chave 110/220 V (`fonte_atx_chave`) | `fonte_tensao` |
| 19 | Verificação final | sempre | `verificacao_final` |

### 1. Coloque a placa-mãe no tapete

- **Componente:** Placa-mãe ATX
- **Entra quando:** Placa-mãe
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Descarregue a estática (pulseira antiestática ou toque numa parte metálica aterrada). Tire a placa-mãe do saco, segurando pelas bordas, e deite-a sobre o tapete, dentro do contorno tracejado, com o painel traseiro (I/O) para a esquerda.

> **Atenção:** Nunca monte sobre superfície metálica. E a placa precisa ficar dentro do contorno: o app usa essa posição para mostrar cada encaixe.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Placa dentro do contorno tracejado do tapete
- [ ] Painel traseiro (I/O) virado para a esquerda
- [ ] Marcador A à vista, sem nada por cima

### 2. Encaixe a CPU no soquete

- **Componente:** Processador Intel (LGA1700)
- **Entra quando:** Processador (CPU), modelo Intel · soquete LGA1700 (`intel_lga1700`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Levante a alavanca e abra a moldura. Segure a CPU pelas bordas, sem tocar nos contatos dourados de baixo. Alinhe o triângulo dourado da CPU com a marca do soquete e apoie a CPU sem empurrar. Feche a moldura e trave a alavanca.

> **Atenção:** Não encoste nos contatos do soquete nem force a CPU: um pino LGA torto inutiliza a placa.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Triângulo da CPU alinhado com o do soquete
- [ ] CPU apoiada sem forçar
- [ ] Moldura fechada e alavanca travada

### 3. Encaixe a CPU no soquete

- **Componente:** Processador AMD (AM5)
- **Entra quando:** Processador (CPU), modelo AMD · soquete AM5 (`amd_am5`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Levante a alavanca até o fim e abra a moldura. Segure a CPU pelas bordas, sem tocar nos contatos dourados de baixo. Alinhe o triângulo dourado do canto da CPU com o triângulo do soquete (os entalhes das laterais também precisam bater) e apoie a CPU sem empurrar. Feche a moldura, abaixe a alavanca e prenda-a embaixo da trava. A capa plástica do soquete solta sozinha: guarde-a.

> **Atenção:** O AM5 também é LGA: os pinos ficam no soquete da placa. Não encoste neles nem force a CPU, porque um pino torto inutiliza a placa.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Triângulo da CPU alinhado com o do soquete
- [ ] CPU apoiada sem forçar
- [ ] Moldura fechada e alavanca presa na trava

### 4. Instale o cooler e ligue no CPU_FAN

- **Componente:** Cooler do processador
- **Entra quando:** Processador (CPU), modelo Intel · soquete LGA1700 (`intel_lga1700`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Confira a pasta térmica. Apoie o cooler sobre a CPU com os 4 pontos de fixação alinhados aos furos em volta do soquete e prenda em diagonal (pinos ou parafusos em X). Depois ligue o cabo do cooler no conector CPU_FAN, acima do soquete.

> **Atenção:** Sem o cabo no CPU_FAN (ou com ele em outro conector), a placa acusa erro ou o processador esquenta demais.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pasta térmica no lugar (já vem no cooler ou uma gota no centro da CPU)
- [ ] Cooler firme, sem balançar, com os 4 pontos presos
- [ ] Cabo do cooler ligado no conector CPU_FAN

### 5. Instale o cooler e ligue no CPU_FAN

- **Componente:** Cooler do processador (AM5)
- **Entra quando:** Processador (CPU), modelo AMD · soquete AM5 (`amd_am5`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Confira a pasta térmica. No AM5, o cooler prende nos 4 furos do suporte AMD em volta do soquete (retângulo de 54 x 90 mm, mais alto do que largo) ou nos ganchos do suporte plástico, conforme o modelo. Apoie o cooler sobre a CPU e prenda em diagonal (parafusos em X). Depois ligue o cabo do cooler no conector CPU_FAN, acima do soquete.

> **Atenção:** Sem o cabo no CPU_FAN (ou com ele em outro conector), a placa acusa erro ou o processador esquenta demais.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pasta térmica no lugar (já vem no cooler ou uma gota no centro da CPU)
- [ ] Cooler firme, sem balançar, com os 4 pontos presos
- [ ] Cabo do cooler ligado no conector CPU_FAN

### 6. Instale a RAM no slot A2

- **Componente:** Memória DDR5 (1 pente)
- **Entra quando:** Memória RAM: exatamente 1 pente
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Segure o pente pelas pontas (as bordas curtas, marcadas em azul), sem encostar nos contatos dourados nem nos chips: a estática da mão pode queimar a memória. Com um pente só, use o slot A2 (2º a partir da CPU; confira no manual da placa). Abra as travas, alinhe o chanfro do pente com a chave do slot e empurre na vertical até as travas fecharem.

> **Atenção:** O pente só entra num sentido: se o chanfro não bater, gire 180°. Nunca force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pente no slot A2
- [ ] Chanfro alinhado com a chave do slot
- [ ] Travas fechadas

### 7. Instale a RAM nos slots A2 e B2

- **Componente:** Memória DDR5 (2 pentes, recomendado)
- **Entra quando:** Memória RAM: exatamente 2 pentes
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Segure o pente pelas pontas (as bordas curtas, marcadas em azul), sem encostar nos contatos dourados nem nos chips: a estática da mão pode queimar a memória. Abra as travas dos slots A2 e B2 (2º e 4º a partir da CPU). Alinhe o chanfro do pente com a chave do slot e empurre na vertical até as travas fecharem.

> **Atenção:** O pente só entra num sentido: se o chanfro não bater, gire 180°. Nunca force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pentes nos slots A2 e B2
- [ ] Chanfro alinhado com a chave do slot
- [ ] Travas fechadas nos dois slots

### 8. Instale a RAM nos slots A2, B2 e A1

- **Componente:** Memória DDR5 (3 pentes)
- **Entra quando:** Memória RAM: exatamente 3 pentes
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Segure o pente pelas pontas (as bordas curtas, marcadas em azul), sem encostar nos contatos dourados nem nos chips: a estática da mão pode queimar a memória. Coloque primeiro dois pentes em A2 e B2 (2º e 4º a partir da CPU), como na montagem recomendada, e o terceiro em A1. Abra as travas, alinhe o chanfro com a chave do slot e empurre na vertical até as travas fecharem. Confira no manual da placa a ordem para 3 pentes.

> **Atenção:** Com 3 pentes os dois canais ficam desiguais e a memória pode render menos: o recomendado é 2 ou 4 pentes iguais. O pente só entra num sentido; nunca force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pentes nos slots A2, B2 e A1
- [ ] Chanfro alinhado com a chave de cada slot
- [ ] Travas fechadas nos três slots

### 9. Instale a RAM nos quatro slots

- **Componente:** Memória DDR5 (4 pentes)
- **Entra quando:** Memória RAM: exatamente 4 pentes
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Segure o pente pelas pontas (as bordas curtas, marcadas em azul), sem encostar nos contatos dourados nem nos chips: a estática da mão pode queimar a memória. Com 4 pentes, todos os slots ficam ocupados. Encaixe primeiro A2 e B2 (2º e 4º a partir da CPU), depois A1 e B1. Abra as travas, alinhe o chanfro com a chave do slot e empurre na vertical até as travas fecharem.

> **Atenção:** O pente só entra num sentido: se o chanfro não bater, gire 180°. Nunca force. Com 4 pentes o primeiro boot demora mais (a placa testa a memória): é normal.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pentes nos 4 slots (A1, A2, B1 e B2)
- [ ] Chanfro alinhado com a chave de cada slot
- [ ] Travas fechadas nos quatro slots

### 10. Instale o SSD M.2

- **Componente:** SSD M.2 2280 (NVMe)
- **Entra quando:** Armazenamento no kit, modelo SSD M.2 2280 · NVMe (`ssd_m2_2280_nvme`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Tire o parafuso do suporte do M.2. Segure o SSD pelas bordas laterais, sem tocar nos contatos dourados nem nos chips. Encaixe o SSD inclinado no conector M.2 (entre a CPU e o slot da placa de vídeo), com o chanfro dos contatos alinhado à chave. Abaixe a outra ponta até o suporte e prenda com o parafuso.

> **Atenção:** SSD sem parafuso fica levantado e pode soltar. Aperte só até firmar: forçar entorta o SSD.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] SSD todo dentro do conector, sem contatos dourados aparecendo
- [ ] Ponta do SSD presa pelo parafuso
- [ ] SSD paralelo à placa

### 11. Ligue o cabo de 24 pinos

- **Componente:** Fonte · cabo ATX 24 pinos
- **Entra quando:** Fonte de alimentação
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Com a fonte desligada da tomada, pegue o cabo de 24 pinos (o maior). Alinhe a trava do cabo com a lingueta do conector ATX, na borda direita da placa, e empurre até ouvir o clique.

> **Atenção:** Cabo meio encaixado é a causa mais comum de PC que não liga: empurre até a trava prender.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Cabo de 24 pinos todo dentro do conector
- [ ] Trava do cabo presa na lingueta (clique)

### 12. Ligue o cabo EPS 12V da CPU

- **Componente:** Fonte · cabo EPS 8 pinos (4+4)
- **Entra quando:** Fonte de alimentação
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Pegue o cabo marcado CPU (EPS 4+4); se vier dividido, junte as duas metades. Encaixe no conector de 8 pinos do canto superior esquerdo da placa, perto do painel traseiro, até a trava prender.

> **Atenção:** Não use o cabo PCIe 6+2 (marcado PCI-E ou VGA): ele parece o EPS, mas a pinagem é outra. Forçar o cabo errado pode danificar a placa ou a fonte.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Cabo marcado CPU/EPS (não PCI-E)
- [ ] As 8 vias encaixadas e a trava presa

### 13. Ligue o SSD SATA

- **Componente:** SSD SATA 2,5"
- **Entra quando:** Armazenamento no kit, modelo SSD SATA 2,5" (`ssd_sata_25`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Na bancada, o SSD fica deitado no tapete, à direita da placa (marcadores azuis: segure pelas laterais). Ligue o cabo de dados SATA (o fino, com conector em L) numa porta SATA da borda direita da placa e a outra ponta no SSD. Depois ligue no SSD o cabo de energia SATA da fonte (o mais largo, também em L).

> **Atenção:** Os conectores SATA têm formato em L e só entram num sentido: virado ao contrário, a lingueta de plástico quebra. Não force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Cabo de dados SATA no SSD e numa porta SATA da placa
- [ ] Cabo de energia SATA da fonte ligado no SSD
- [ ] Os dois conectores encaixados até o fim

### 14. Ligue o HD SATA

- **Componente:** HD SATA 3,5"
- **Entra quando:** Armazenamento no kit, modelo HD SATA 3,5" · 7200 rpm (`hd_sata_35`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

O HD tem peças móveis por dentro: segure pelas laterais (marcadores azuis), sem tocar na placa de circuito de baixo, e apoie devagar no tapete, à direita da placa. Ligue o cabo de dados SATA (o fino, em L) numa porta SATA da borda direita da placa e no HD. Depois ligue no HD o cabo de energia SATA da fonte (o mais largo).

> **Atenção:** Batida ou queda estraga o HD, mesmo desligado. E os conectores em L só entram num sentido: não force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] HD apoiado no tapete, sem bater
- [ ] Cabo de dados SATA no HD e numa porta SATA da placa
- [ ] Cabo de energia SATA da fonte ligado no HD

### 15. Encaixe a placa de vídeo no PCIe x16

- **Componente:** Placa de vídeo com cabo PCIe 6+2
- **Entra quando:** Placa de vídeo no kit, modelo PCIe x16 · 1 cabo PCIe 8 pinos (6+2) (`gpu_pcie_x16`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Use o slot PCIe x16 de cima, o mais perto da CPU. Abra a trava na ponta do slot, alinhe os contatos da placa de vídeo e empurre por igual até a trava fechar. Depois ligue o cabo PCIe 6+2 (8 pinos) da fonte no conector da placa de vídeo.

> **Atenção:** O slot de baixo (em vermelho) tem o mesmo tamanho, mas é x4. E a placa de vídeo usa o cabo PCIe 6+2, nunca o EPS da CPU.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Placa no slot PCIe x16 de cima
- [ ] Trava do slot fechada
- [ ] Cabo PCIe 6+2 ligado na placa de vídeo

### 16. Encaixe a placa de vídeo no PCIe x16

- **Componente:** Placa de vídeo sem cabo de energia
- **Entra quando:** Placa de vídeo no kit, modelo PCIe x16 · sem cabo de energia (até 75 W) (`gpu_sem_cabo`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Use o slot PCIe x16 de cima, o mais perto da CPU. Abra a trava na ponta do slot, alinhe os contatos da placa de vídeo e empurre por igual até a trava fechar. Esta placa recebe toda a energia pelo slot: não tem cabo para ligar.

> **Atenção:** O slot de baixo (em vermelho) tem o mesmo tamanho, mas é x4. Segure a placa pelas bordas, sem tocar nos contatos dourados.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Placa no slot PCIe x16 de cima
- [ ] Trava do slot fechada
- [ ] Placa reta, sem um lado mais alto

### 17. Encaixe a placa de vídeo e o cabo 12V-2x6

- **Componente:** Placa de vídeo com conector 12V-2x6
- **Entra quando:** Placa de vídeo no kit, modelo PCIe x16 · conector 12V-2x6 (16 pinos) (`gpu_12v2x6`)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Use o slot PCIe x16 de cima, o mais perto da CPU. Abra a trava na ponta do slot, alinhe os contatos e empurre por igual até a trava fechar. Depois ligue o cabo de 16 pinos 12V-2x6 (ou o adaptador que veio com a placa) e empurre até não sobrar fresta.

> **Atenção:** Cabo 12V-2x6 meio encaixado esquenta e pode derreter: empurre até o fim e não dobre o cabo logo na saída do conector. O slot de baixo (em vermelho) é x4.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Placa no slot PCIe x16 de cima
- [ ] Trava do slot fechada
- [ ] Cabo 12V-2x6 encaixado até o fim, sem fresta
- [ ] Cabo sem dobra forte perto do conector

### 18. Confira a chave 110/220 V da fonte

- **Componente:** Fonte com chave seletora de tensão
- **Entra quando:** Fonte de alimentação, modelo ATX com chave 110/220 V (`fonte_atx_chave`)
- **Na RA:** sem marcador (só instrução e checklist)

Esta fonte tem uma chave de tensão atrás, perto da entrada do cabo de força (geralmente vermelha). Com a fonte ainda fora da tomada, descubra a tensão da tomada da bancada (etiqueta da tomada ou pergunte ao professor) e deixe a chave no mesmo valor: 110/115 V para tomada de 127 V e 220/230 V para tomada de 220 V.

> **Atenção:** Chave em 110 V numa tomada de 220 V queima a fonte e pode danificar a placa-mãe e as outras peças. Chave em 220 V numa tomada de 127 V: o PC não liga ou desliga sozinho. Na dúvida, não ligue e chame o professor.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Tensão da tomada da bancada confirmada (127 V ou 220 V)
- [ ] Chave da fonte na mesma tensão da tomada
- [ ] Fonte ainda fora da tomada

### 19. Verificação final

- **Componente:** Montagem completa
- **Entra quando:** sempre
- **Na RA:** sem marcador (só instrução e checklist)

Antes de ligar, revise tudo com calma. Se algo estiver solto ou fora do lugar, corrija agora.

> **Atenção:** Só ligue a fonte na tomada depois desta verificação. Confira a tensão: fonte com chave 110/220 V na posição errada pode queimar.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Peças instaladas firmes e com as travas fechadas
- [ ] Cabos de energia encaixados até a trava
- [ ] Nenhum parafuso ou ferramenta solto sobre a placa
- [ ] Tensão da fonte conferida com a da tomada (bivolt automática ou chave na posição certa)

