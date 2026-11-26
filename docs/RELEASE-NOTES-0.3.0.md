# Brown Dust 2 PT-BR 0.3.0

Esta versão corrige o bloqueio que impedia instalar o catálogo após uma atualização do jogo e acrescenta os textos novos e alterados do cliente atual.

## Tradução

- 7.994 entradas novas ou alteradas integradas: 7.349 traduções diretas e 645 reaproveitamentos por correspondência verificada.
- 265.270 entradas efetivas no catálogo consolidado.
- 430 chaves de títulos musicais preservadas no original, correspondentes a 320 fontes únicas protegidas.
- 123 entradas obsoletas retiradas do snapshot atual. As demais traduções anteriores foram preservadas.

## Instalação e atualizações

A referência do catálogo passa a ser informativa. O aplicativo confere as estruturas necessárias para a integração; um catálogo defasado pode ser instalado em um cliente posterior quando essas capacidades permanecem disponíveis. Textos novos ou alterados sem correspondência válida ficam no original. Mudanças estruturais reais ainda podem exigir adaptação.

O catálogo e o aplicativo são atualizados separadamente. **Buscar atualização** consulta ambos; o botão principal aplica o catálogo e **Atualizar aplicativo** aparece quando há um executável novo. A atualização do aplicativo preserva o executável anterior, reinicia a interface e recupera a versão anterior se a nova janela não abrir. Inclui o novo ícone preto e branco aprovado pelo autor.

**Migração inicial:** quem possui o aplicativo 0.2.0 deve baixar o novo **BD2PTBR.exe** uma vez para receber a atualização própria e a checagem estrutural. O aplicativo antigo ainda consegue instalar este novo catálogo no cliente validado, mas não pode adquirir retroativamente um mecanismo de atualização de si próprio. Depois da migração, use o mesmo aplicativo nas próximas atualizações.

## Cliente e validação

- Cliente real: **2.39.10 (FHD-058:312)**, pacote **20261113000**, Windows x64.
- Auditorias públicas de identidade, chaves, Unicode, músicas e marcadores: aprovadas para 265.270 entradas.
- **60 cenários** do atualizador aprovados, incluindo clientes posteriores, recuperação, downloads, atualização do aplicativo e metadados de jogo incompletos.
- Artefatos finais: **13 operações e 46 assertivas** aprovadas, incluindo instalação limpa, verificação, dano/reparo, remoção, rollback e uso do aplicativo anterior.
- Troca real do aplicativo entre processos validada com backup, reinício e confirmação da nova janela.
- Instalação e verificação reais de **26 arquivos**, sem avisos. Plugin, catálogo, fontes e proteções carregados; HUD e menus PT-BR observados sem falhas detectadas no registro.
- Os 3 arquivos oficiais críticos permaneceram intactos. Os 87 arquivos-fonte comparados não mudaram durante a validação.

A tradução continua em beta. A validação técnica e a amostra visual não equivalem a revisão humana integral de todas as frases ou cenas. Elementos incorporados em artes podem permanecer no original. A compatibilidade reconhecida estruturalmente não é uma garantia para toda alteração futura do cliente.

## Downloads

- **BD2PTBR.exe**: aplicativo universal recomendado.
- **BD2PTBR.zip**: contém somente o mesmo aplicativo; extraia antes de abrir.
- **BD2PTBR-payload-0.3.0.zip**: pacote usado pelo atualizador; não extraia manualmente no jogo.
- **SHA256SUMS.txt** e **release-manifest.json**: integridade e identificação desta distribuição.

O código-fonte e as instruções permanentes ficam no repositório. O aplicativo consulta somente o canal e os downloads oficiais do projeto por HTTPS.
