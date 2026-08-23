# Status técnico: 0.1.1 beta

## Compatibilidade validada

| Item | Estado |
| --- | --- |
| Plataforma | PC / Windows x64 |
| Versão do mod | 0.1.1 beta |
| Cliente testado | v2.32.10 (FHD-221:619) |
| Pacote do cliente | 20260814000 |
| Carregador | BepInEx 5.4.23.5 |
| Distribuição | Instalador gráfico único |
| Teste de instalação | Aprovado em cliente real limpo |
| Teste de carregamento | Aprovado em execução real |

## Auditoria do catálogo

| Métrica | Resultado |
| --- | ---: |
| Registros físicos | 248.840 |
| Entradas efetivas | 223.743 |
| Substituições efetivas | 25.097 |
| Estado `machine` | 198.773 |
| Estado `reviewed` | 24.873 |
| Estado `draft` | 89 |
| Estado `safety-repaired` | 8 |
| Títulos musicais preservados | 293 |
| Fila editorial automática | 0 |
| Falhas estruturais | 0 |

As classificações são estados internos do processo. `reviewed` identifica tratamento editorial explícito no catálogo; não comprova revisão humana profissional independente de cada frase.

## Verificações do instalador

- 10 cenários e 19 operações automatizadas aprovadas.
- Rejeição de pasta inválida sem alteração do cliente.
- Instalação limpa e verificação criptográfica aprovadas.
- Corrupção detectada e reparada.
- Reinstalação idempotente aprovada.
- Rollback aprovado para instalação e remoção interrompidas.
- Carregadores e mods preexistentes preservados.
- Mods adicionados depois da instalação preservados.
- Artefatos gerados pelo BepInEx removidos sem falso positivo.
- Remoção real com zero arquivo extra, ausente ou hash divergente.

## Teste no cliente real

O ciclo final foi executado sobre um cliente limpo com 2.164 arquivos originais:

1. registro do estado limpo;
2. instalação pelo executável final;
3. verificação integral pelo manifesto interno;
4. inicialização real do jogo;
5. confirmação do plugin e da tradução;
6. remoção e comparação exata com o estado limpo;
7. nova instalação e verificação para o estado entregue ao mantenedor.

O jogo iniciou no cliente `v2.32.10 (FHD-221:619)`. O log confirmou o plugin `0.1.1`, 223.743 traduções, 36 pontos centrais, 11 pontos visuais, proteção do guia, cobertura de caracteres PT-BR e ausência de erro fatal. O comando da tela inicial foi exibido em português.

## Desempenho observado

- Primeira passagem de tradução medida em 15 ms.
- Passagens seguintes medidas em 0 ms no teste final.
- Catálogo carregado uma vez e consultado por mapa em memória.
- Varreduras visuais limitadas à inicialização e à mudança de cena.
- Captura diagnóstica desativada por padrão.

## Proteções de runtime

- Tradução aplicada antes da animação progressiva quando o fluxo passa pelos pontos cobertos.
- Livro de ajuda protegido durante recortes e paginação interna.
- Catálogo externo validado antes da ativação dos interceptores.
- Falha em ponto central interrompe a ativação, remove os patches e preserva o idioma original.
- O mod não altera o sistema oficial de atualização do jogo.

## Limitações conhecidas

- A maior parte do catálogo ainda não recebeu revisão contextual manual independente.
- Uma frase estruturalmente válida pode continuar literal ou inadequada à cena.
- Conteúdo lançado após o pacote testado pode permanecer em inglês.
- Mudanças internas futuras podem exigir nova compilação ou novos pontos de integração.
- O executável não possui certificado comercial de assinatura de código.
- A compatibilidade declarada se limita ao cliente indicado neste documento.

O relatório reproduzível do catálogo está em [`catalog/audit-report.json`](../catalog/audit-report.json). A matriz do instalador pode ser reproduzida com [`tools/Test-Installer.ps1`](../tools/Test-Installer.ps1).
