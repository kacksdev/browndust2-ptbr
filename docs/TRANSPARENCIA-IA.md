# Transparência sobre inteligência artificial

## Ferramenta utilizada

O projeto é desenvolvido com assistência do **[OpenAI Codex](https://developers.openai.com/codex/)**, uma ferramenta de inteligência artificial voltada a trabalho de software. Codex foi utilizado para:

- produzir e revisar traduções em português brasileiro;
- analisar contexto, terminologia e consistência;
- desenvolver e depurar a integração técnica;
- criar auditorias, validadores e processos de empacotamento;
- investigar falhas observadas no jogo e propor correções;
- manter documentação e histórico reproduzível.

## Responsabilidade humana

Kacks define o objetivo, os critérios editoriais, o escopo e as decisões de publicação. Os testes práticos relatados foram executados no cliente do jogo pelo mantenedor, com apoio diagnóstico do Codex.

## Limites que precisam ficar claros

- Este não é um trabalho traduzido integralmente por tradutores humanos profissionais.
- Na versão 0.9.6, 198.773 de 223.743 entradas efetivas ainda possuem classificação interna `machine`.
- As 24.873 entradas classificadas como `reviewed` receberam tratamento editorial explícito no processo, mas isso não deve ser confundido com uma segunda revisão humana independente de cada frase.
- IA pode produzir literalidade excessiva, interpretação errada, inconsistência de voz ou erro factual.
- Auditorias automáticas detectam estruturas e padrões conhecidos; não provam perfeição narrativa.

## Como o risco é reduzido

- Glossário e decisões terminológicas persistentes.
- Preservação automática de placeholders, tags e nomes protegidos.
- Comparação entre camadas e rastreabilidade de substituições.
- Filas editoriais e verificações de idioma residual.
- Testes reais de interface, narrativa, paginação, desempenho e crash.
- Distribuição fechada enquanto os critérios públicos não forem atendidos.

## Compromisso

O projeto não esconderá o uso de IA nem apresentará métricas automáticas como prova de tradução perfeita. Quando houver uma versão pública, limitações conhecidas e compatibilidade serão publicadas junto ao pacote.
