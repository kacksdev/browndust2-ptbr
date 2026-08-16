# Arquitetura do mod

## Componentes

| Camada | Responsabilidade |
| --- | --- |
| BepInEx | Carregar o plugin no cliente de PC. |
| Plugin C# | Interceptar os pontos de texto cobertos e coordenar o runtime. |
| Catálogo JSONL | Mapear texto original para português brasileiro. |
| Validador | Conferir estrutura, marcadores, Unicode e integridade antes da publicação. |
| Instalador | Criar backup, instalar, atualizar e remover o pacote. |

## Ativação segura

1. O plugin localiza e valida os seis arquivos do catálogo.
2. O catálogo precisa atingir a quantidade mínima esperada e não pode estar incompleto.
3. Os pontos centrais de tradução são instalados.
4. Coordenador, fonte e proteções auxiliares são habilitados.
5. Se uma etapa essencial falhar, o plugin remove os patches e encerra seus componentes.

O objetivo é que uma incompatibilidade resulte no texto original do jogo, e não em tradução parcial ou falha silenciosa persistente.

## Desempenho

O catálogo é carregado uma vez e consultado por mapa em memória. As varreduras de componentes visuais são finitas e associadas à inicialização ou à mudança de cena; não existe busca global contínua a cada quadro.

## Recompilação

O código é público, mas as DLLs de referência do jogo não são redistribuídas. [`tools/Prepare-References.ps1`](../tools/Prepare-References.ps1) copia as referências necessárias de uma instalação legítima local para `lib/references`, e o script de build gera o pacote auditado.
