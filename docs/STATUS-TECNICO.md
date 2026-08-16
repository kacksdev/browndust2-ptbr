# Status técnico — 0.1.0 beta

## Compatibilidade validada

| Item | Estado |
| --- | --- |
| Plataforma | PC / Windows |
| Versão pública do mod | 0.1.0 beta |
| Cliente testado | 2.32.7 |
| Pacote do cliente | 20260814000 |
| Distribuição | Pública por GitHub Releases |
| Teste de instalação | Aprovado em instalação real |
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

## Verificações aprovadas

- JSON válido e traduções não vazias.
- Marcadores, placeholders e variáveis preservados.
- Normalização Unicode NFC.
- Títulos musicais e rótulos associados preservados.
- Ausência de resíduos detectáveis pelas regras automáticas vigentes.
- Hash e quantidade dos seis arquivos do catálogo conferidos após a instalação.
- Plugin 0.1.0 carregado pelo BepInEx 5.4.23.5.
- Pontos centrais, coordenador, varreduras limitadas, fonte PT-BR, guia e cobertura de glifos confirmados pelo log do cliente.
- Nenhuma assinatura de falha encontrada no teste real da versão declarada.

## Proteções de runtime

- Tradução aplicada antes da animação progressiva dos diálogos quando o fluxo do cliente passa pelos pontos cobertos.
- Livro de ajuda protegido durante recortes e paginação interna.
- Varreduras visuais limitadas à inicialização e à mudança de cena.
- Catálogo externo validado antes da ativação dos patches.
- Falha em ponto central interrompe a ativação, remove os patches e preserva o idioma original do jogo.

## Limitações conhecidas

- A maior parte do catálogo ainda não recebeu revisão contextual manual independente.
- Uma frase estruturalmente válida pode continuar literal ou inadequada à cena.
- Conteúdo lançado após o pacote testado pode permanecer em inglês.
- Mudanças internas futuras podem exigir nova compilação ou novos pontos de integração.
- A compatibilidade declarada se limita ao cliente indicado neste documento.

O relatório reproduzível está em [`catalog/audit-report.json`](../catalog/audit-report.json).
