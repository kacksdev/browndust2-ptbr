# Instalação do Brown Dust 2 PT-BR

## Método recomendado

1. Atualize Brown Dust 2 pelo Starter oficial e conclua os downloads adicionais solicitados pelo jogo.
2. Feche o jogo e o launcher.
3. Na primeira instalação, baixe **BD2PTBR.exe** pela [Release oficial](https://github.com/kacksdev/browndust2-ptbr/releases/latest) e confira o SHA-256 publicado.
4. Abra o aplicativo e confirme a pasta detectada ou clique em **Escolher pasta**.
5. Use **Buscar atualização**. O aplicativo consulta o canal oficial e procura um pacote compatível com o cliente instalado.
6. Quando o pacote aparecer, clique em **Instalar**, **Atualizar** ou **Reparar / Reinstalar**, conforme a opção exibida, e confirme o download e a aplicação.
7. Aguarde a confirmação de conclusão e abra o jogo normalmente pelo launcher oficial.

É necessária conexão com a internet para consultar o canal e baixar os pacotes por HTTPS do GitHub. A consulta não inicia o download sem sua confirmação. Não é necessário extrair o pacote nem abrir um terminal. Se o Windows bloquear a gravação na pasta do jogo, o aplicativo informará a necessidade de executá-lo como administrador; ele não solicita elevação automaticamente.

**Code → Download ZIP não é o instalador:** esse botão baixa o código-fonte. Use o aplicativo indicado na Release.

## Pasta correta do jogo

A pasta selecionada precisa conter:

```text
BrownDust II.exe
BrownDust II_Data\
UnityPlayer.dll
```

O caminho mais comum termina em `BrownDust2_10000002`. Não selecione a pasta `BepInEx` nem crie uma pasta do jogo dentro de outra.

## Ações disponíveis

- **Buscar atualização**: consulta o [canal estável](https://raw.githubusercontent.com/kacksdev/browndust2-ptbr/main/updates/stable.json) e seleciona um pacote compatível; não instala por conta própria.
- **Instalar / Atualizar**: baixa e aplica o pacote compatível apresentado após sua confirmação.
- **Reparar / Reinstalar**: reaplica a versão compatível já instalada, quando disponível no canal.
- **Verificar**: compara os arquivos gerenciados da instalação com os hashes registrados. Essa verificação de integridade não certifica, sozinha, compatibilidade com um cliente novo.
- **Remover**: retira os componentes pertencentes ao projeto e restaura os arquivos anteriores preservados pelo aplicativo.
- **Restaurar versão anterior**: recupera o ponto local anterior, somente quando o cliente e os backups continuam válidos.

Os detalhes técnicos podem ser abertos ou fechados pela seção **Detalhes da operação**. A mensagem principal informa quando a operação termina ou precisa de atenção.

## Compatibilidade e integridade

A [Release atual](https://github.com/kacksdev/browndust2-ptbr/releases/latest) informa a versão, os clientes validados, as alterações e os hashes. O canal do atualizador informa os pacotes disponíveis, a compatibilidade exigida e a versão mínima do aplicativo. Não use números de versões antigas deste guia como autorização para instalar em outro cliente.

O aplicativo confere a identidade do cliente antes do download e novamente antes de aplicar o pacote. O tamanho e o SHA-256 do download, o manifesto e os arquivos internos também são verificados. Um cliente candidato só deve ser anunciado como aprovado depois do teste real; compilar o pacote ou conferir hashes não substitui esse teste.

Se não existir um pacote compatível, a instalação é bloqueada. Se a consulta ou o download falhar, não force arquivos antigos nem trate o aplicativo como instalador sem rede: preserve a instalação existente e consulte o [suporte](../SUPPORT.md).

## Backup e reversão

Antes de alterar os arquivos gerenciados, o aplicativo registra o estado anterior, cria backups e verifica seus hashes. O estado e os backups ficam em:

```text
%LOCALAPPDATA%\Kacks\BD2PTBR\instances
```

Não apague essa pasta se pretende usar a recuperação. Os downloads ficam em uma pasta separada, `%LOCALAPPDATA%\Kacks\BD2PTBR\downloads`.

Se uma etapa falhar ou uma operação for interrompida, o aplicativo tenta recuperar a transação pendente antes de prosseguir. A recuperação é bloqueada se o cliente não corresponder ao registrado ou se o backup estiver ausente ou corrompido. Nesse caso, preserve os arquivos e envie os detalhes ao suporte.

**Restaurar versão anterior** exige o mesmo cliente do ponto salvo, backups íntegros e arquivos gerenciados sem alterações externas. Essa ação restaura o estado local do mod; não desfaz uma atualização oficial do jogo e não é um modo de forçar um pacote incompatível. A conclusão só é informada depois das verificações da operação.

## Como confirmar o carregamento

Depois de abrir o jogo, consulte `BepInEx\LogOutput.log` dentro da pasta do jogo. Procure o carregamento de **Brown Dust 2 PT-BR** e mensagens como:

```text
[PTBR-RUNTIME] cobertura de tradução operacional.
[PTBR-CICLO] Awake concluído.
```

Compare a versão e as informações do catálogo exibidas no log com a Release correspondente. Uma mensagem isolada não comprova toda a tradução nem compatibilidade com futuras versões do jogo.

Na tela inicial, o texto `TOUCH TO START` deve aparecer como `TOQUE PARA COMEÇAR`. Se o carregamento falhar ou houver um bloqueio de compatibilidade, não altere os arquivos manualmente para contorná-lo.

## Atualizar o mod

Abra o mesmo **BD2PTBR.exe**, com o jogo e o launcher fechados, e use **Buscar atualização**. Confirme o pacote compatível apresentado. Não é necessário baixar outro instalador a cada atualização do catálogo.

Uma mudança de segurança ou de protocolo do próprio aplicativo poderá exigir uma migração, informada pelo projeto na Release. Arquivos externos ao mod são preservados; alterações externas em arquivos gerenciados podem bloquear uma atualização ou restauração e precisam ser avaliadas antes de prosseguir.

## Atualizar o jogo

1. Feche o jogo e execute o **Brown Dust 2 Starter** oficial da NEOWIZ.
2. Aguarde a atualização e conclua os downloads adicionais solicitados pelo jogo.
3. Feche novamente o jogo e o launcher.
4. Abra **BD2PTBR.exe** e use **Buscar atualização** para conferir se há pacote compatível com o novo cliente.
5. Aplique somente o pacote oferecido para esse cliente. **Verificar** pode conferir a integridade dos arquivos do mod, mas não substitui a checagem de compatibilidade.

O atualizador do mod não substitui o Starter oficial. Novos textos podem continuar em inglês até entrarem no catálogo publicado. Se o cliente mudou e não há pacote compatível, aguarde a avaliação do projeto; restaurar um backup antigo não torna esse cliente compatível.

Você também pode usar **Abrir Starter oficial** no BD2PTBR. O aplicativo localiza o Starter pelo registro do protocolo oficial do jogo, sem depender de uma unidade ou pasta fixa, e o abre com o argumento correto. O próprio Starter baixa a atualização e pode abrir o jogo ao terminar. O botão não instala o mod, não altera o protocolo do Windows e não substitui os controles oficiais. Se o Starter estiver ausente ou o registro estiver inválido, será exibida uma orientação para reparar a instalação oficial.

## Remoção

Feche o jogo e o launcher, abra o mesmo aplicativo e clique em **Remover**.

- Se o BepInEx foi instalado pelo projeto e nenhum outro mod foi encontrado, o carregador também é removido.
- Se já havia BepInEx ou outro mod, o aplicativo preserva os componentes que não pertencem ao projeto.
- Arquivos anteriores preservados durante a instalação são restaurados quando os backups necessários são válidos.

Se a operação indicar um bloqueio ou backup inválido, não apague o estado local; consulte o suporte.

## Segurança e origem

O executável não possui certificado comercial de assinatura de código. O Windows pode mostrar **Fornecedor desconhecido** ou uma tela do SmartScreen. Esse aviso não confirma a integridade do arquivo: confira sua origem e seu SHA-256 antes de abrir. Não desative o antivírus para instalar.

Use somente estas origens:

- [Releases oficiais no GitHub](https://github.com/kacksdev/browndust2-ptbr/releases/latest);
- [página oficial no GameBanana](https://gamebanana.com/mods/706840).

Compare o SHA-256 do executável com o publicado na mesma Release. O aplicativo faz consultas de atualização e downloads por HTTPS, restritos ao canal e às Releases oficiais do projeto, com redirecionamentos permitidos para a infraestrutura de arquivos do GitHub. Antes da aplicação, ele verifica o pacote e a compatibilidade do cliente.

## Problemas comuns

### O jogo não foi encontrado

Clique em **Escolher pasta** e selecione a pasta que contém `BrownDust II.exe`. Não selecione somente a pasta do launcher.

### Não existe pacote compatível ou o canal está indisponível

Confira a conexão e os avisos da Release. Não force um pacote de outro cliente e não substitua manualmente o feed ou os arquivos do jogo. A consulta pode falhar quando o canal ainda não foi disponibilizado ou estiver temporariamente indisponível. Envie a mensagem completa ao suporte se o problema persistir.

### O jogo continua em inglês

1. Feche o jogo e o launcher.
2. Abra o aplicativo e use **Verificar** para conferir a instalação existente.
3. Use **Buscar atualização** e, se houver um pacote compatível, **Reparar / Reinstalar** ou **Instalar**, conforme a opção exibida.
4. Consulte `BepInEx\LogOutput.log` e procure o carregamento do plugin e eventuais bloqueios.

### O Windows bloqueou a abertura

Confirme que o arquivo veio de uma das páginas oficiais e compare o SHA-256. Não ignore alertas de integridade ou desative a proteção do sistema. Se a origem ou o hash não puderem ser confirmados, não execute o arquivo e consulte o suporte.

### A operação foi interrompida

Abra o aplicativo novamente com o jogo fechado. A recuperação da transação pendente é avaliada antes de uma nova operação. Se houver bloqueio, preserve os backups e abra **Detalhes da operação**. Veja quais informações enviar no [guia de suporte](../SUPPORT.md).
