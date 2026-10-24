# Histórico de versões

As notas detalhadas de cada versão pública ficam na respectiva Release do GitHub. Este arquivo conserva os marcos principais.

## 0.2.0: catálogo e atualizador universal

Catálogo atualizado para o cliente de outubro, pacotes separados do aplicativo, canal HTTPS, compatibilidade verificada, backup e recuperação. Inclui abertura do Starter oficial e documentação permanente. [Notas e validação](./docs/RELEASE-NOTES-0.2.0.md).

## 0.1.1: instalador gráfico e distribuição corrigida

- Substituição dos scripts públicos por um instalador gráfico único para Windows x64.
- Detecção automática do cliente e seleção manual pela interface.
- Instalação, reparo, atualização, verificação e remoção no mesmo executável.
- Operações transacionais com ponto de restauração, recuperação após interrupção e rollback automático.
- Preservação de carregadores e mods preexistentes.
- Correção da remoção dos arquivos gerados pelo BepInEx após uma execução real.
- Matriz com 10 cenários e 19 operações automatizadas aprovada.
- Instalação e remoção reais validadas contra um cliente limpo com 2.164 arquivos originais.
- Cliente `v2.32.10 (FHD-221:619)`, pacote `20260814000`, iniciado com o plugin `0.1.1` e 223.743 traduções sem erro.
- Primeira varredura real medida em 15 ms; passagens seguintes em 0 ms.
- A versão pública `0.1.0` foi retirada por falha no atalho de instalação.

## 0.1.0: versão retirada

- Primeira beta pública do catálogo e do plugin.
- Distribuição retirada porque o atalho procurava o script principal na pasta errada e podia apresentar uma confirmação falsa de sucesso.
- Substituída integralmente pela versão `0.1.1`.

## Linhagem privada anterior

- `0.9.6`: cobertura de Crônicas e Histórias Cotidianas, correção do diálogo progressivo, proteção do guia e otimização de varreduras.
- `0.9.0`: consolidação do catálogo, proteção de músicas e auditorias estruturais.
- `0.8.0`: base integral recuperada usada como ponto de partida.

Esses números privados não representavam proximidade editorial do marco público `1.0.0`.
