# Suporte

Relate problemas nas [Issues do projeto](https://github.com/kacksdev/browndust2-ptbr/issues). Para instalação, atualização e recuperação, consulte primeiro o [guia de instalação](docs/INSTALACAO.md). A [Release atual](https://github.com/kacksdev/browndust2-ptbr/releases/latest) e o [canal estável](https://raw.githubusercontent.com/kacksdev/browndust2-ptbr/main/updates/stable.json) são as referências variáveis de versão, compatibilidade e integridade.

## O que informar

- A versão do aplicativo e do mod, quando disponíveis, e a Release utilizada.
- A identificação do cliente do jogo e do pacote de dados, se exibida no aplicativo ou no log.
- A ação executada, os passos para reproduzir, o resultado esperado e o resultado obtido.
- A mensagem completa e os registros de **Detalhes da operação** em falhas de instalação, atualização ou recuperação.
- A tela ou o diálogo afetado em problemas de tradução, com o texto observado e uma captura que ajude a localizar a ocorrência.
- O trecho relevante de `BepInEx\LogOutput.log`, dentro da pasta do jogo, em falhas de carregamento.

Remova nomes de usuário, caminhos pessoais e outras informações privadas antes de publicar registros ou capturas. Não envie arquivos proprietários completos do jogo nem dados de conta.

## Antes de tentar novamente

Conclua a atualização pelo Starter oficial e os downloads adicionais do jogo. Depois, feche o jogo e o launcher e abra o mesmo **BD2PTBR.exe**. Use **Buscar atualização** para conferir se existe pacote compatível. Não é necessário baixar outro instalador para cada atualização do catálogo; uma migração do próprio aplicativo será informada pelo projeto.

O aplicativo usa a internet para consultar o canal e baixar os pacotes por HTTPS do GitHub. Se houver falha de rede, canal indisponível ou ausência de pacote compatível, registre a mensagem e preserve a instalação. Não force um pacote antigo nem altere o feed para contornar o bloqueio.

**Verificar** confere os hashes dos arquivos gerenciados; não certifica compatibilidade com uma atualização nova do jogo. **Reparar / Reinstalar** depende de um pacote compatível disponível no canal. Uma consulta bem-sucedida, um hash correto ou uma compilação não substituem o teste real de uma versão candidata.

## Recuperação e backups

Não apague `%LOCALAPPDATA%\Kacks\BD2PTBR\instances`: essa pasta contém o estado e os backups usados na recuperação. Se uma operação for interrompida, reabra o aplicativo com o jogo fechado para que a transação pendente seja avaliada.

**Restaurar versão anterior** só pode usar um ponto válido para o mesmo cliente, com backups íntegros e arquivos gerenciados sem alterações externas. A ação não reverte uma atualização oficial do jogo. Se o cliente mudou, um backup está ausente ou um hash diverge, preserve os arquivos e envie o bloqueio ao suporte; não copie backups manualmente sobre o novo cliente.

O projeto não promete compatibilidade permanente com futuras atualizações oficiais nem ausência absoluta de erros de tradução. Só considere um cliente validado quando essa condição estiver registrada na Release correspondente.
