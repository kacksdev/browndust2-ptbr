# Brown Dust 2 PT-BR 0.2.0 beta

Tradução atualizada para o cliente **2.37.13**, pacote **20261023000**, com atualizador universal para Windows x64.

## Download e uso

1. Baixe **BD2PTBR.exe** uma vez e guarde o aplicativo.
2. Atualize o jogo pelo Starter oficial e feche o jogo e o Starter.
3. Abra BD2PTBR.exe, confirme a pasta e use **Buscar atualização**.
4. Aplique o pacote compatível apresentado e confirme a operação.

Nas próximas atualizações de catálogo, use o mesmo aplicativo. O arquivo **BD2PTBR-payload-0.2.0.zip** é baixado e aplicado por ele; não extraia esse payload manualmente na pasta do jogo. **BD2PTBR.zip** é a alternativa compactada do aplicativo, preparada também para a publicação manual no GameBanana.

## Alterações

- Catálogo consolidado com **257.678 entradas efetivas**: 257.667 provenientes das bases e 11 rótulos adicionais. Foram comparados todos os bancos textuais do cliente, incluindo Intro.
- Integração do lote de 32.103 entradas de outubro e do delta posterior de 4.828 entradas: 4.395 traduzidas diretamente e 433 reaproveitadas por correspondência verificada. Fontes alteradas foram avaliadas como alterações, sem reaproveitamento cego por ID.
- Preservação de **423 chaves musicais**, correspondentes a 314 fontes protegidas normalizadas no runtime.
- Correções pontuais de rótulos confirmadas durante a integração e a amostra visual, com procedência separada; nenhuma reformulação geral de estilo.
- Aplicativo separado dos pacotes, canal HTTPS, compatibilidade por pacote/versão/hashes críticos e downloads verificados por tamanho e SHA-256.
- Backup verificado, recuperação transacional, reparo, remoção e restauração do ponto local anterior quando cliente e backups continuam válidos.
- Migração das camadas legadas conhecidas sem apagar arquivos externos ao projeto.
- Botão **Abrir Starter oficial**, com detecção do protocolo registrado, sem caminho fixo nem necessidade de Win+R.
- Uma única ação **Buscar atualização**; botão principal dedicado à instalação, atualização ou reparo após a seleção compatível.
- README e guias permanentes: os próximos lançamentos concentram alterações e compatibilidade nas Releases e no canal.

## Validação

- Auditoria estrutural e de identidade: 257.678 entradas, zero chaves duplicadas e músicas protegidas preservadas.
- Auditor canônico de marcadores do runtime: 257.678 entradas, zero falhas.
- Suíte do atualizador: 47 cenários aprovados.
- Artefatos finais: 17 execuções headless e 74 verificações aprovadas, incluindo negativos esperados de hash, cliente e recuperação.
- Cliente real: aplicação e verificação dos 26 arquivos gerenciados, inicialização pelo Starter, catálogo novo carregado, fontes PT-BR e textos aplicados em menus observados. Os três arquivos oficiais de identidade permaneceram intactos.

A amostra visual não certifica todas as cenas. Tradução direta com auxílio de IA e verificações estruturais não equivalem a revisão humana integral. O catálogo continua em beta e pode conter escolhas literais ou inconsistências históricas. Os arquivos recebidos do Studio, a memória de tradução e o glossário foram preservados.

## Compatibilidade e recuperação

A compatibilidade declarada limita-se ao cliente acima e às identidades do canal. Atualizações futuras do jogo precisam ser avaliadas; conteúdo novo pode permanecer no idioma original. **Restaurar versão anterior** não desfaz atualizações oficiais. Preserve `%LOCALAPPDATA%\Kacks\BD2PTBR\instances` se pretende usar os backups.

O aplicativo não possui certificado comercial de assinatura de código. Confira a origem e os hashes em **SHA256SUMS.txt**. Os hashes e identidades do pacote também estão em **release-manifest.json**. Não desative a proteção do sistema para instalar.

[Guia de instalação e recuperação](https://github.com/kacksdev/browndust2-ptbr/blob/main/docs/INSTALACAO.md) · [Relatar problema](https://github.com/kacksdev/browndust2-ptbr/issues)
