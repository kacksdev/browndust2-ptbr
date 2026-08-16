# Brown Dust 2 PT-BR 0.1.0 beta

Primeira versão pública da tradução comunitária para PC.

## Compatibilidade

- Brown Dust 2 `2.32.7`;
- pacote oficial `20260814000`;
- Windows x64;
- BepInEx `5.4.23.5`, incluído no pacote e verificado por SHA-256.

## Cobertura e grau de revisão

- 223.743 entradas efetivas;
- 24.873 entradas com tratamento editorial explícito — 11,12%;
- 198.773 entradas produzidas em escala com auxílio de IA — 88,84%;
- 89 registros em rascunho e 8 reparos estruturais;
- 293 títulos musicais preservados no original;
- zero falha estrutural e zero item na fila automática vigente.

Esta é uma tradução beta de cobertura ampla, não uma revisão integral frase por frase. Literalidade, contexto, gênero, nomes e consistência ainda podem receber correções.

## Estabilidade e desempenho

- Tradução aplicada no ponto central antes do efeito progressivo dos diálogos.
- Proteção de paginação do livro de ajuda.
- Varredura contínua removida; somente passagens finitas na inicialização e em mudanças de cena.
- Primeira passagem medida entre 17 e 18 ms nos testes finais e passagens seguintes em 0 ms.
- Captura diagnóstica desativada por padrão.
- Falha de catálogo ou de ponto essencial remove os interceptores do mod e mantém os textos originais.

## Instalação

1. Baixe `BD2PTBR-v0.1.0.zip` nesta Release.
2. Extraia o arquivo por inteiro.
3. Feche o jogo.
4. Execute `INSTALAR-BD2-PTBR.cmd`.

O instalador valida hashes, detecta a pasta oficial e cria backup antes de atualizar uma instalação existente. Para remover, execute `REMOVER-BD2-PTBR.cmd`.

## Teste da atualização oficial

O lançador assinado pela NEOWIZ atualizou o cliente para o pacote `20260814000` sem ser bloqueado pelo mod. Depois da atualização, BepInEx e o plugin carregaram 223.743 entradas, 36 pontos centrais, 11 pontos visuais, a proteção do guia e a fonte PT-BR sem erro de inicialização.

## Transparência

O projeto é dirigido, testado e publicado por Kacks. O OpenAI Codex foi usado como ferramenta auxiliar na tradução em escala, na engenharia, nas auditorias e na documentação. Consulte [Autoria e ferramentas](https://github.com/kacksdev/browndust2-ptbr/blob/main/docs/AUTORIA-E-FERRAMENTAS.md).
