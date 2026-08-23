# Compatibilidade

## Versões registradas

| Mod | Cliente | Pacote | Resultado |
| --- | --- | --- | --- |
| 0.1.1 beta | v2.32.10 (FHD-221:619) | 20260814000 | Instalação limpa, carregamento, catálogo, runtime, remoção e restauração aprovados |
| 0.1.0 beta | 2.32.7 | 20260814000 | Distribuição retirada por falha no atalho de instalação |

Não utilize a versão `0.1.0`. O catálogo e o plugin daquela versão não apresentaram falha estrutural, mas o atalho podia informar sucesso sem executar a instalação.

## Política após atualizações

O plugin e o instalador não modificam o sistema de atualização do Brown Dust 2. O cliente deve ser atualizado pelo lançador oficial.

Quando o jogo muda:

1. arquivos do cliente podem ser substituídos pelo atualizador;
2. textos novos podem não existir no catálogo do mod;
3. nomes internos ou assinaturas podem mudar;
4. a compatibilidade precisa ser testada novamente.

Depois de uma atualização oficial:

1. feche o jogo;
2. conclua a atualização pelo Brown Dust 2 Starter;
3. abra o instalador do mod;
4. use **Verificar**;
5. use **Reparar** somente se o instalador indicar divergência.

O runtime valida a integridade do catálogo e a existência dos pontos essenciais antes de permanecer ativo. Se uma dependência central falhar, os interceptores do mod são removidos e o idioma original é preservado. Esse comportamento reduz o risco de deixar o cliente em estado parcial, mas não constitui promessa absoluta para versões futuras ainda desconhecidas.

Sempre consulte a Release mais recente antes de reinstalar o mod depois de uma atualização do jogo.
