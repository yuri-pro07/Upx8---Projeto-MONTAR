# Roteiro de montagem em bancada (bancada_v0)

> Gerado a partir de `Assets/Data/Roteiro_bancada_v0.asset` e `Assets/Data/Pecas/Catalogo_pecas_v0.asset` em 07/10/2026 (menu **MontAR › App › Exportar roteiro**). Não edite este arquivo à mão: mude as etapas na Unity (ou no `spec_montagem.json`) e exporte de novo.

O app, o **manual técnico** e o **tutorial em vídeo** das condições de controle precisam seguir exatamente estas etapas, na mesma ordem e com o mesmo kit (CLAUDE.md, §2).

**Rascunho v0.** Placa ATX genérica com posições aproximadas: o kit do laboratório ainda não foi definido (decisão D5). Montagem em bancada, sem gabinete (decisão D4 em aberto).

## Peças (uma opção de cada, por enquanto)

| Peça | Modelo | Quantidade aceita | Padrão |
|---|---|---|---|
| Placa-mãe (`placa_mae`) | ATX genérica · LGA1700 · DDR5 | 1 | 1 |
| Processador (CPU) (`cpu`) | Intel · soquete LGA1700 | 1 | 1 |
| Cooler do processador (`cooler`) | Cooler a ar · cabo de 4 pinos | 1 | 1 |
| Memória RAM (`ram`) | DDR5 · pente DIMM | 1 a 2 | 2 |
| SSD M.2 (`ssd`) | M.2 2280 · NVMe | 0 a 1 (opcional) | 1 |
| Placa de vídeo (`gpu`) | PCIe x16 · 1 cabo de energia PCIe | 0 a 1 (opcional) | 1 |
| Fonte de alimentação (`fonte`) | ATX · cabos 24 pinos, EPS 4+4 e PCIe 6+2 | 1 | 1 |

## Etapas

A coluna "entra quando" diz de qual peça a etapa depende. Com o kit padrão (2 pentes de RAM, SSD e placa de vídeo), o roteiro tem 9 etapas.

| # | Etapa | Entra quando | ID no CSV |
|---|---|---|---|
| 1 | Coloque a placa-mãe no tapete | Placa-mãe | `placa_tapete` |
| 2 | Encaixe a CPU no soquete | Processador (CPU) | `cpu_soquete` |
| 3 | Instale o cooler e ligue no CPU_FAN | Cooler do processador | `cooler_cpu` |
| 4 | Instale a RAM no slot A2 | Memória RAM: exatamente 1 pente | `ram_slot_a2` |
| 5 | Instale a RAM nos slots A2 e B2 | Memória RAM: exatamente 2 pentes | `ram_slots` |
| 6 | Instale o SSD M.2 | SSD M.2 no kit | `ssd_m2` |
| 7 | Ligue o cabo de 24 pinos | Fonte de alimentação | `atx_24p` |
| 8 | Ligue o cabo EPS 12V da CPU | Fonte de alimentação | `eps_8p` |
| 9 | Encaixe a placa de vídeo no PCIe x16 | Placa de vídeo no kit | `gpu_pcie` |
| 10 | Verificação final | sempre | `verificacao_final` |

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

- **Componente:** Processador (LGA1700)
- **Entra quando:** Processador (CPU)
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Levante a alavanca e abra a moldura. Alinhe o triângulo dourado da CPU com a marca do soquete e apoie a CPU sem empurrar. Feche a moldura e trave a alavanca.

> **Atenção:** Não encoste nos contatos do soquete nem force a CPU: um pino LGA torto inutiliza a placa.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Triângulo da CPU alinhado com o do soquete
- [ ] CPU apoiada sem forçar
- [ ] Moldura fechada e alavanca travada

### 3. Instale o cooler e ligue no CPU_FAN

- **Componente:** Cooler do processador
- **Entra quando:** Cooler do processador
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Confira a pasta térmica. Apoie o cooler sobre a CPU com os 4 pontos de fixação alinhados aos furos em volta do soquete e prenda em diagonal (pinos ou parafusos em X). Depois ligue o cabo do cooler no conector CPU_FAN, acima do soquete.

> **Atenção:** Sem o cabo no CPU_FAN (ou com ele em outro conector), a placa acusa erro ou o processador esquenta demais.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pasta térmica no lugar (já vem no cooler ou uma gota no centro da CPU)
- [ ] Cooler firme, sem balançar, com os 4 pontos presos
- [ ] Cabo do cooler ligado no conector CPU_FAN

### 4. Instale a RAM no slot A2

- **Componente:** Memória DDR5 (1 pente)
- **Entra quando:** Memória RAM: exatamente 1 pente
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Com um pente só, use o slot A2 (2º a partir da CPU; confira no manual da placa). Abra as travas, alinhe o chanfro do pente com a chave do slot e empurre na vertical até as travas fecharem.

> **Atenção:** O pente só entra num sentido: se o chanfro não bater, gire 180°. Nunca force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pente no slot A2
- [ ] Chanfro alinhado com a chave do slot
- [ ] Travas fechadas

### 5. Instale a RAM nos slots A2 e B2

- **Componente:** Memória DDR5 (2 módulos)
- **Entra quando:** Memória RAM: exatamente 2 pentes
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Abra as travas dos slots A2 e B2 (2º e 4º a partir da CPU). Alinhe o chanfro do pente com a chave do slot e empurre na vertical até as travas fecharem.

> **Atenção:** O pente só entra num sentido: se o chanfro não bater, gire 180°. Nunca force.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Pentes nos slots A2 e B2
- [ ] Chanfro alinhado com a chave do slot
- [ ] Travas fechadas nos dois slots

### 6. Instale o SSD M.2

- **Componente:** SSD M.2 2280 (NVMe)
- **Entra quando:** SSD M.2 no kit
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Tire o parafuso do suporte do M.2. Encaixe o SSD inclinado no conector M.2 (entre a CPU e o slot da placa de vídeo), com o chanfro dos contatos alinhado à chave. Abaixe a outra ponta até o suporte e prenda com o parafuso.

> **Atenção:** SSD sem parafuso fica levantado e pode soltar. Aperte só até firmar: forçar entorta o SSD.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] SSD todo dentro do conector, sem contatos dourados aparecendo
- [ ] Ponta do SSD presa pelo parafuso
- [ ] SSD paralelo à placa

### 7. Ligue o cabo de 24 pinos

- **Componente:** Fonte · cabo ATX 24 pinos
- **Entra quando:** Fonte de alimentação
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Com a fonte desligada da tomada, pegue o cabo de 24 pinos (o maior). Alinhe a trava do cabo com a lingueta do conector ATX, na borda direita da placa, e empurre até ouvir o clique.

> **Atenção:** Cabo meio encaixado é a causa mais comum de PC que não liga: empurre até a trava prender.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Cabo de 24 pinos todo dentro do conector
- [ ] Trava do cabo presa na lingueta (clique)

### 8. Ligue o cabo EPS 12V da CPU

- **Componente:** Fonte · cabo EPS 8 pinos (4+4)
- **Entra quando:** Fonte de alimentação
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Pegue o cabo marcado CPU (EPS 4+4); se vier dividido, junte as duas metades. Encaixe no conector de 8 pinos do canto superior esquerdo da placa, perto do painel traseiro, até a trava prender.

> **Atenção:** Não use o cabo PCIe 6+2 (marcado PCI-E ou VGA): ele parece o EPS, mas a pinagem é outra. Forçar o cabo errado pode danificar a placa ou a fonte.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Cabo marcado CPU/EPS (não PCI-E)
- [ ] As 8 vias encaixadas e a trava presa

### 9. Encaixe a placa de vídeo no PCIe x16

- **Componente:** Placa de vídeo (PCIe x16)
- **Entra quando:** Placa de vídeo no kit
- **Na RA:** overlay preso ao marcador `MontAR_Marcador_A`

Use o slot PCIe x16 de cima, o mais perto da CPU. Abra a trava na ponta do slot, alinhe os contatos da placa de vídeo e empurre por igual até a trava fechar. Se a placa tiver conector de energia, ligue o cabo PCIe 6+2 da fonte.

> **Atenção:** O slot de baixo (em vermelho) tem o mesmo tamanho, mas é x4. E a placa de vídeo usa o cabo PCIe 6+2, nunca o EPS da CPU.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Placa no slot PCIe x16 de cima
- [ ] Trava do slot fechada
- [ ] Cabo PCIe ligado (se a placa tiver conector de energia)

### 10. Verificação final

- **Componente:** Montagem completa
- **Entra quando:** sempre
- **Na RA:** sem marcador (só instrução e checklist)

Antes de ligar, revise tudo com calma. Se algo estiver solto ou fora do lugar, corrija agora.

> **Atenção:** Só ligue a fonte na tomada depois desta verificação.

Validação (checklist, todos os itens precisam ser confirmados):

- [ ] Peças instaladas firmes e com as travas fechadas
- [ ] Cabos de energia encaixados até a trava
- [ ] Nenhum parafuso ou ferramenta solto sobre a placa

