# Arquitetura do mod

## Componentes

| Camada | Responsabilidade |
| --- | --- |
| BepInEx | Carregar o plugin no cliente de PC. |
| Plugin C# | Interceptar os pontos de texto cobertos e coordenar o runtime. |
| Catálogo JSONL | Mapear texto original para português brasileiro. |
| Validador | Conferir estrutura, marcadores, Unicode e integridade antes da publicação. |
| Instalador gráfico | Detectar o cliente, validar o payload, instalar, reparar, atualizar, verificar, remover e reverter falhas. |

## Ativação segura

1. O plugin localiza e valida os seis arquivos do catálogo.
2. O catálogo precisa atingir a quantidade mínima esperada e não pode estar incompleto.
3. Os pontos centrais de tradução são instalados.
4. Coordenador, fonte e proteções auxiliares são habilitados.
5. Se uma etapa essencial falhar, o plugin remove os patches e encerra seus componentes.

O objetivo é que uma incompatibilidade resulte no texto original do jogo, e não em tradução parcial ou falha silenciosa persistente.

## Desempenho

O catálogo é carregado uma vez e consultado por mapa em memória. As varreduras de componentes visuais são finitas e associadas à inicialização ou à mudança de cena; não existe busca global contínua a cada quadro.

## Instalação transacional

O instalador `0.1.1` é um executável único para Windows x64. O payload fica incorporado e é conferido antes da cópia. Cada operação registra o estado anterior, cria um ponto de restauração e só confirma sucesso após a verificação final.

Uma falha durante instalação ou remoção aciona rollback. Se o processo for interrompido, a próxima abertura recupera a transação pendente antes de aceitar uma nova ação. Arquivos pertencentes a outros mods e carregadores preexistentes são identificados e preservados.

## Recompilação

O código é público, mas as DLLs de referência do jogo não são redistribuídas. [`tools/Prepare-References.ps1`](../tools/Prepare-References.ps1) copia as referências necessárias de uma instalação legítima local para `lib/references`. [`tools/Build-Release.ps1`](../tools/Build-Release.ps1) audita o catálogo, compila o plugin e o instalador e executa a matriz automatizada antes de concluir a distribuição.
