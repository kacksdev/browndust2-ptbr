# Status técnico — v0.9.6

## Compatibilidade

| Item | Estado |
| --- | --- |
| Plataforma | PC / Windows |
| Mod | 0.9.6 — validação privada |
| Cliente validado | v2.31.8(FHD-046:137) |
| Distribuição pública | Fechada |
| Implementação neste repositório | Não incluída |

Uma atualização do Brown Dust 2 pode alterar textos, assemblies ou pontos internos do cliente. Compatibilidade só será declarada depois de nova validação.

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
| Fila editorial | 0 |
| Falhas estruturais | 0 |

As classificações são estados internos do processo. `reviewed` indica tratamento editorial explícito no catálogo; não comprova revisão humana profissional independente.

## Verificações zeradas

- JSON inválido.
- Tradução vazia.
- Falha de placeholders ou marcadores.
- Falha de normalização Unicode NFC.
- Título musical alterado.
- Resíduo de idioma sinalizado pelas regras vigentes.
- Artefato de tradução automática sinalizado pelas regras vigentes.

Zero sinalização automática não é garantia de perfeição semântica. Contexto, intenção e qualidade literária ainda dependem de amostragem e testes reais.

## Correções de runtime

- Aplicação da tradução antes da animação progressiva dos diálogos.
- Proteção do livro de ajuda durante recortes e paginação interna.
- Coordenador independente para preservar patches quando o ciclo do cliente encerra o componente inicial.
- Varreduras visuais limitadas a inicialização e mudança de cena.
- Diagnóstico remoto desativado no uso normal.

## Publicação

Este documento relata a implementação privada, mas não a expõe. Nenhuma informação aqui deve ser interpretada como link, convite ou autorização para obter uma compilação ainda não lançada.
