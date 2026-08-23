# Brown Dust 2 PT-BR 0.1.1

Primeira distribuição pública com o novo instalador gráfico e com o ciclo completo de instalação validado em um cliente limpo.

## Compatibilidade validada

- Brown Dust 2 `v2.32.10 (FHD-221:619)`;
- pacote oficial `20260814000`;
- Windows x64;
- BepInEx `5.4.23.5`, incorporado e verificado por SHA-256.

## Download correto

Baixe somente:

```text
BD2PTBR-Installer-v0.1.1.exe
```

O arquivo `.sha256` permite conferir a integridade do executável. O manifesto JSON registra versões, métricas, dependências e o hash do pacote interno.

## Instalação

1. Feche Brown Dust 2 e o launcher.
2. Abra `BD2PTBR-Installer-v0.1.1.exe`.
3. Confirme a pasta detectada ou escolha manualmente a pasta que contém `Brown Dust II.exe`.
4. Clique em **Instalar**.
5. Aguarde **Operação concluída** e abra o jogo.

O mesmo executável permite reparar, atualizar, verificar e remover o mod.

## Correção da versão anterior

A versão `0.1.0` foi retirada porque seu atalho procurava o script principal na pasta errada e podia informar sucesso sem executar a instalação. A versão `0.1.1` elimina o atalho, não depende de arquivos externos ao executável e somente confirma sucesso depois da verificação integral.

## Segurança da operação

- Payload interno verificado antes da cópia.
- Ponto de restauração criado antes de alterações.
- Rollback automático em falhas.
- Recuperação de transação interrompida.
- Preservação de mods e carregadores preexistentes.
- Remoção seletiva baseada no estado registrado.
- Nenhum download ou envio de telemetria durante a instalação.

O executável ainda não possui certificado comercial de assinatura de código. O Windows pode mostrar **Fornecedor desconhecido** ou o SmartScreen. Compare o SHA-256 publicado e use somente as páginas oficiais do projeto.

## Cobertura e grau de revisão

- 223.743 entradas efetivas;
- 24.873 entradas com tratamento editorial explícito, equivalentes a 11,12%;
- 198.773 entradas produzidas em escala com auxílio de IA, equivalentes a 88,84%;
- 89 registros em estado de trabalho e 8 reparos estruturais;
- 293 títulos musicais preservados no original;
- zero falha estrutural e zero item na fila automática vigente.

Esta é uma tradução beta de cobertura ampla, não uma revisão integral frase por frase. Literalidade, contexto, gênero, nomes e consistência ainda podem receber correções.

## Evidências de validação

- 10 cenários automatizados;
- 19 operações de instalação, verificação, atualização, reparo, remoção e rollback aprovadas;
- instalação real sobre um cliente limpo com 2.164 arquivos;
- BepInEx e plugin `0.1.1` carregados sem erro;
- 223.743 traduções, 36 pontos centrais e 11 pontos visuais ativos;
- comando da tela inicial exibido em português;
- primeira passagem medida em 15 ms e passagens seguintes em 0 ms;
- remoção com zero arquivo extra, ausente ou hash divergente na raiz do cliente.

## Autoria e transparência

O projeto é criado, dirigido, testado e publicado por Kacks. O OpenAI Codex foi utilizado como ferramenta auxiliar na tradução em escala, na engenharia, nas auditorias, no instalador e na documentação. Consulte [Autoria e ferramentas](./AUTORIA-E-FERRAMENTAS.md) para a atribuição completa.
