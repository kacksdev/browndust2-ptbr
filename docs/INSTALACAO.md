# Instalação do Brown Dust 2 PT-BR

## Método recomendado

1. Abra a seção **Releases** do repositório.
2. Baixe `BD2PTBR-Installer-v0.1.1.exe` e, opcionalmente, o arquivo `.sha256`.
3. Feche Brown Dust 2 e o launcher.
4. Abra o instalador.
5. Confirme a pasta detectada ou clique em **Escolher pasta**.
6. Clique em **Instalar**.
7. Aguarde a mensagem **Operação concluída** e abra o jogo normalmente.

Não é necessário extrair arquivos, abrir terminal ou executar comandos como administrador manualmente. O instalador solicita elevação somente quando o Windows exigir permissão para alterar a pasta do jogo.

## Pasta correta do jogo

A pasta selecionada precisa conter:

```text
Brown Dust II.exe
Brown Dust II_Data\
UnityPlayer.dll
```

O caminho mais comum termina em `BrownDust2_10000002`. Não selecione a pasta `BepInEx` nem crie uma pasta do jogo dentro de outra.

## Ações disponíveis

- **Instalar**: adiciona o carregador e a tradução a um cliente limpo.
- **Reparar**: restaura somente arquivos ausentes ou alterados do mod.
- **Atualizar**: substitui uma versão anterior sem acumular arquivos obsoletos.
- **Verificar**: compara todos os arquivos instalados com o manifesto interno.
- **Remover**: restaura os arquivos originais e retira os componentes pertencentes ao projeto.

Os detalhes técnicos podem ser abertos ou fechados pela seção **Detalhes da operação**. A barra de progresso e a mensagem principal informam claramente quando a operação começa, termina ou precisa de atenção.

## Backup e reversão

Antes de alterar o cliente, o instalador registra o estado existente e cria um ponto de restauração em:

```text
%LOCALAPPDATA%\Kacks\BD2PTBR\instances
```

Se uma etapa falhar ou o processo for interrompido, a próxima abertura recupera a transação pendente e restaura o estado anterior. O instalador nunca usa uma mensagem de sucesso se a verificação final não tiver sido aprovada.

## Como confirmar o carregamento

Depois de abrir o jogo, consulte:

```text
BrownDust2_10000002\BepInEx\LogOutput.log
```

Uma inicialização correta contém mensagens equivalentes a:

```text
Loading [Brown Dust 2 PT-BR 0.1.1]
Traduções carregadas: 223743.
[PTBR-RUNTIME] cobertura de tradução operacional.
[PTBR-CICLO] Awake concluído.
```

Na tela inicial, o comando para iniciar o jogo deve aparecer em português.

## Atualizar o mod

Baixe o novo instalador oficial e abra-o com o jogo fechado. A interface reconhecerá a instalação anterior e oferecerá **Atualizar** ou **Reparar**. O catálogo antigo é substituído de forma controlada e os arquivos de outros mods são preservados.

## Atualizar o jogo

1. Feche o jogo.
2. Execute o **Brown Dust 2 Starter** oficial da NEOWIZ.
3. Aguarde a atualização terminar.
4. Abra o instalador do mod e use **Verificar**.
5. Se necessário, use **Reparar**.

O plugin foi validado no cliente `v2.32.10 (FHD-221:619)`, pacote `20260814000`. Se uma versão posterior alterar pontos essenciais, o mod desfaz os próprios interceptores e mantém os textos originais, registrando o motivo no log.

## Remoção

Feche o jogo, abra o mesmo executável e clique em **Remover**.

- Se o BepInEx foi instalado pelo projeto e nenhum outro mod foi encontrado, o carregador também é removido.
- Se já havia BepInEx ou outro mod, esses arquivos são preservados.
- Arquivos originais substituídos durante a instalação são restaurados.
- O cliente limpo usado na validação voltou exatamente aos 2.164 arquivos originais.

## Segurança e origem

A versão `0.1.1` ainda não possui certificado comercial de assinatura de código. O Windows pode mostrar **Fornecedor desconhecido** ou uma tela do SmartScreen. Isso não significa que o arquivo esteja corrompido, mas confirma que ele não possui uma assinatura paga reconhecida pelo sistema.

Use somente estas origens:

- Release oficial em `github.com/kacksdev/browndust2-ptbr`;
- página oficial do mod em `gamebanana.com/mods/706840`.

Compare o SHA-256 do executável com o arquivo `.sha256` publicado na mesma Release. O instalador não envia telemetria e não baixa componentes durante a operação.

## Problemas comuns

### O jogo não foi encontrado

Clique em **Escolher pasta** e selecione a pasta que contém `Brown Dust II.exe`. Se o arquivo não estiver nela, a seleção será recusada sem alterar nada.

### O jogo continua em inglês

1. Feche o jogo.
2. Abra o instalador e use **Verificar**.
3. Se houver divergência, use **Reparar**.
4. Abra `BepInEx\LogOutput.log` e procure a mensagem de carregamento do plugin `0.1.1`.

### O Windows bloqueou a abertura

Confirme que o arquivo veio de uma das duas páginas oficiais e compare o SHA-256. Na proteção do SmartScreen, use **Mais informações** somente depois dessa verificação.

### A operação foi interrompida

Abra o instalador novamente. A recuperação da transação pendente ocorre antes de qualquer nova ação. Se o problema continuar, abra **Detalhes da operação** e envie o registro pela aba Issues do GitHub ou pelos comentários do GameBanana.
