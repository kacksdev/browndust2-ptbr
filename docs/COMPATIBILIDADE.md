# Compatibilidade

A [Release atual](https://github.com/kacksdev/browndust2-ptbr/releases/latest) registra o cliente efetivamente testado e a referência do catálogo. O [canal estável](../updates/stable.json) publica os pacotes, o perfil de integração e as atualizações do aplicativo.

## Catálogo e integração

A versão do jogo até a qual os textos foram atualizados é uma referência de cobertura, não um prazo de validade do mod. O aplicativo permite instalar o catálogo existente em clientes posteriores quando reconhece a estrutura necessária para a integração. Textos novos ou alterados sem correspondência válida continuam no idioma original.

O perfil atual verifica o cliente Windows x64 com Mono e as estruturas de tabelas, central de localização, renderização, fontes e guia usadas pelo plugin. Essa inspeção lê metadados sem executar as bibliotecas do jogo. Mudanças de número de versão, pacote ou conteúdo sem impacto nessas capacidades não bloqueiam a instalação. Se uma capacidade essencial mudar, o aplicativo informa o impedimento; isso pode exigir uma nova integração.

Os hashes exatos do cliente continuam registrados para transações e restauração de backups. Um ponto de recuperação não deve ser aplicado a um cliente diferente daquele em que foi criado. Eles não definem a validade temporal do catálogo no perfil atual.

## Depois de uma atualização oficial

1. Conclua a atualização pelo Starter e os downloads adicionais solicitados pelo jogo.
2. Feche o jogo e o Starter.
3. Abra o mesmo BD2PTBR.exe e use **Buscar atualização**.
4. Atualize o catálogo quando houver uma nova versão. A referência de cobertura aparece no aplicativo.
5. Use **Atualizar aplicativo** quando essa ação for oferecida para receber melhorias do atualizador.

**Verificar** confere a integridade do mod instalado. **Restaurar versão anterior** recupera seu estado local, sujeito ao mesmo cliente e a backups íntegros; não desfaz atualizações oficiais.

O runtime valida o catálogo e os pontos essenciais antes de permanecer ativo. A compatibilidade estrutural reduz a manutenção causada por atualizações de conteúdo, mas não garante que toda mudança futura do cliente seja suportada. Consulte [Instalação e recuperação](./INSTALACAO.md).
