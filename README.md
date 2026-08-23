<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/bd2-hero-dark.png" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/bd2-hero-light.png" />
  <img src="./assets/bd2-hero-light.png" alt="Brown Dust 2 em arte monocromática do projeto" width="100%" />
</picture>

<h1 align="center">BROWN DUST 2 PT-BR / PC</h1>

<p align="center">
  <strong>TRADUÇÃO COMUNITÁRIA / BETA / WINDOWS X64</strong>
</p>

<p align="center">
  <code>v0.1.1 BETA</code>&nbsp;&nbsp;
  <code>CLIENTE 2.32.10</code>&nbsp;&nbsp;
  <code>PACOTE 20260814000</code>&nbsp;&nbsp;
  <code>INSTALADOR GRÁFICO</code>
</p>

<p align="center">
  <a href="https://github.com/kacksdev/browndust2-ptbr/releases/latest"><strong>BAIXAR A VERSÃO ATUAL</strong></a>
  &nbsp;·&nbsp;
  <a href="./docs/INSTALACAO.md">INSTALAR, VERIFICAR OU REMOVER</a>
  &nbsp;·&nbsp;
  <a href="./docs/STATUS-TECNICO.md">VER O ESTADO TÉCNICO</a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/ink-rule-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/ink-rule-light.svg" />
  <img src="./assets/ink-rule-light.svg" alt="" width="100%" />
</picture>

## 01 / O PROJETO

**Brown Dust 2 PT-BR** é uma tradução comunitária gratuita para a versão de PC do jogo. O mod substitui textos exibidos pelo cliente por português brasileiro sem distribuir executáveis, bancos de dados, imagens ou outros arquivos proprietários do Brown Dust 2.

Esta é uma **beta funcional de cobertura ampla**, não uma revisão editorial integral. O catálogo conhecido foi traduzido e passou por auditorias estruturais e testes reais, mas a maior parte das entradas foi produzida em escala com auxílio de IA. Ainda podem existir frases literais, escolhas inadequadas ao contexto, inconsistências ou textos introduzidos por atualizações posteriores.

> A versão **1.0.0** fica reservada para uma tradução integralmente revisada, contextualizada e validada no jogo. Essa revisão avança de forma voluntária e não possui prazo anunciado.

O projeto é criado, dirigido, mantido e validado por **Kacks**. Não possui afiliação, patrocínio ou endosso da NEOWIZ ou da GAMFS N.

## 02 / ESTADO ATUAL

| Indicador | Resultado |
| --- | ---: |
| Entradas efetivas no catálogo | **223.743** |
| Entradas com tratamento editorial explícito | **24.873 (11,12%)** |
| Entradas produzidas em escala com auxílio de IA | **198.773 (88,84%)** |
| Entradas ainda classificadas como rascunho | **89** |
| Reparos automáticos de segurança estrutural | **8** |
| Títulos musicais preservados no original | **293** |
| Falhas estruturais detectadas | **0** |
| Itens na fila automática de auditoria | **0** |

Os números descrevem o processo do catálogo. Eles não significam que 24.873 frases tenham recebido uma segunda revisão humana profissional independente. Zero alerta automático também não é sinônimo de perfeição semântica.

O pacote atual foi instalado e executado no cliente **v2.32.10 (FHD-221:619)**, pacote **20260814000**. O plugin `0.1.1` carregou 223.743 traduções, 36 pontos centrais, 11 pontos visuais, proteção do guia e cobertura de caracteres PT-BR sem erro conhecido.

## 03 / INSTALAÇÃO RÁPIDA

1. Atualize o Brown Dust 2 pelo lançador oficial e feche o jogo.
2. Na [Release mais recente](https://github.com/kacksdev/browndust2-ptbr/releases/latest), baixe **`BD2PTBR-Installer-v0.1.1.exe`**.
3. Abra o executável e confirme a pasta detectada. Se necessário, use **Escolher pasta**.
4. Clique em **Instalar** e aguarde a mensagem **Operação concluída**.
5. Abra o jogo normalmente.

O mesmo executável permite **instalar, reparar, atualizar, verificar e remover** o mod. Ele mostra progresso, mantém detalhes técnicos recolhíveis, cria um ponto de restauração antes das alterações e desfaz uma operação que falhar.

Não é necessário extrair arquivos, abrir terminal ou executar comandos manualmente. Consulte [Instalação, atualização e remoção](./docs/INSTALACAO.md) para o guia completo.

> **Não use `Code > Download ZIP` para instalar.** Esse botão baixa o código-fonte. O instalador pronto fica exclusivamente em **Releases**.

## 04 / CORREÇÃO DA DISTRIBUIÇÃO 0.1.0

A distribuição `0.1.0` foi retirada porque seu atalho procurava o script principal na pasta errada e podia informar sucesso sem instalar o mod. O catálogo e o plugin não apresentaram falha estrutural, mas o pacote não atendia ao padrão de instalação do projeto.

A versão `0.1.1` substitui os scripts por um único instalador gráfico. Ela não depende de arquivos externos ao executável e só confirma sucesso depois de verificar integralmente a instalação. Não utilize pacotes antigos da versão `0.1.0`.

## 05 / VALIDAÇÃO DO INSTALADOR

| Validação | Resultado |
| --- | --- |
| Matriz automatizada | **10 cenários e 19 operações aprovadas** |
| Instalação limpa | **Aprovada em cliente real com 2.164 arquivos originais** |
| Reparação e atualização | **Aprovadas com verificação criptográfica** |
| Falha simulada | **Rollback automático aprovado** |
| Preservação de outros mods | **Aprovada antes e depois da instalação** |
| Remoção | **Zero arquivo extra, ausente ou divergente** |
| Inicialização do jogo | **Plugin 0.1.1 e 223.743 traduções carregados** |
| Tela inicial | **Comando de início exibido em português no cliente** |
| Desempenho observado | **Primeira passagem em 15 ms; seguintes em 0 ms** |

O executável ainda não possui certificado comercial de assinatura de código. O Windows pode mostrar **Fornecedor desconhecido** ou o SmartScreen. Baixe somente pelas páginas oficiais e compare o SHA-256 publicado na mesma Release.

## 06 / ATUALIZAÇÕES DO JOGO

O mod não modifica o sistema de atualização do Brown Dust 2. Atualize o cliente sempre pelo lançador oficial e depois use **Verificar** no instalador.

O plugin foi projetado para falhar com segurança. Se o catálogo estiver incompleto ou os pontos essenciais do cliente mudarem, os interceptores do mod são removidos e o jogo mantém o texto original. Isso reduz o risco, mas nenhuma modificação pode prometer compatibilidade absoluta com versões futuras ainda desconhecidas. Conteúdo novo pode aparecer em inglês até o catálogo ser atualizado.

Consulte a [matriz de compatibilidade](./docs/COMPATIBILIDADE.md) antes de reinstalar o mod após uma atualização importante.

## 07 / TRADUÇÃO EM JOGO

As imagens abaixo são capturas reais do cliente com o mod ativo. Elas demonstram interface, inventário, diálogos, conteúdos narrativos, Crônicas, Glossário e Guia.

| Interface e equipamentos | Diálogo e pacote de história |
| --- | --- |
| ![Tela principal traduzida](./assets/screenshots/01-interface-principal.webp) | ![Diálogo traduzido](./assets/screenshots/03-dialogo.webp) |
| ![Equipamentos traduzidos](./assets/screenshots/02-equipamentos.webp) | ![Pacote de história traduzido](./assets/screenshots/04-pacote-de-historia.webp) |

| Histórias Cotidianas e Crônicas | Glossário e Guia |
| --- | --- |
| ![Histórias Cotidianas traduzidas](./assets/screenshots/05-historias-cotidianas.webp) | ![Glossário traduzido](./assets/screenshots/07-glossario.webp) |
| ![Crônicas traduzidas](./assets/screenshots/06-cronicas.webp) | ![Guia traduzido](./assets/screenshots/08-guia.webp) |

## 08 / CÓDIGO ABERTO E VERIFICAÇÃO

| Área | Conteúdo público |
| --- | --- |
| [`src`](./src) | Código-fonte C# do plugin. |
| [`catalog`](./catalog) | Catálogo PT-BR em JSONL e relatório de auditoria. |
| [`installer`](./installer) | Código-fonte do instalador gráfico e seus recursos. |
| [`tools`](./tools) | Auditorias, compilação, empacotamento e matriz de testes. |
| [`docs`](./docs) | Arquitetura, compatibilidade, autoria e instruções. |

Cada Release inclui o instalador, um manifesto JSON e um arquivo SHA-256 para conferência. O repositório não inclui DLLs nem recursos proprietários do jogo. As referências necessárias para recompilar o plugin devem ser obtidas da instalação legítima do próprio usuário.

## 09 / AUTORIA E TRANSPARÊNCIA

**Brown Dust 2 PT-BR é um projeto criado, dirigido, mantido e validado por mim.** A definição do escopo, do padrão de qualidade, das decisões editoriais, dos testes no cliente, da compatibilidade e da publicação permanece sob minha responsabilidade.

O **OpenAI Codex** integra o fluxo como ferramenta auxiliar. Ele foi utilizado para produzir as traduções PT-BR das entradas do catálogo, aplicar correções orientadas pelo projeto, apoiar o desenvolvimento do plugin e do instalador, automatizar auditorias e acelerar a documentação. A engenharia e o controle de qualidade avançaram de forma colaborativa: defini requisitos, identifiquei problemas durante o uso real, conduzi testes, aprovei resultados e participei da execução técnica em grande escala.

Os detalhes estão em [Autoria e ferramentas](./docs/AUTORIA-E-FERRAMENTAS.md).

## 10 / RELATOS E DIREITOS

- Erros de tradução, instalação ou compatibilidade podem ser relatados pelas [Issues](https://github.com/kacksdev/browndust2-ptbr/issues).
- Vulnerabilidades devem ser enviadas pelo [relato privado de segurança](https://github.com/kacksdev/browndust2-ptbr/security/advisories/new).
- Brown Dust 2, personagens, nomes, artes e demais conteúdos pertencem aos respectivos titulares.
- O mod é gratuito, comunitário e não representa uma tradução oficial.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/ink-rule-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/ink-rule-light.svg" />
  <img src="./assets/ink-rule-light.svg" alt="" width="100%" />
</picture>

<p align="center"><code>KACKS / COMMUNITY TRANSLATION / BRASIL</code></p>
