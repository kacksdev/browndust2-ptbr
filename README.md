<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/bd2-hero-dark.png" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/bd2-hero-light.png" />
  <img src="./assets/bd2-hero-light.png" alt="Brown Dust 2 em arte monocromática do projeto" width="100%" />
</picture>

<h1 align="center">BROWN DUST 2 PT-BR / PC</h1>

<p align="center">
  <strong>TRADUÇÃO COMUNITÁRIA / BETA PÚBLICA / WINDOWS</strong>
</p>

<p align="center">
  <code>v0.1.0 BETA</code>&nbsp;&nbsp;
  <code>CLIENTE 2.32.7</code>&nbsp;&nbsp;
  <code>PACOTE 20260814000</code>&nbsp;&nbsp;
  <code>GRATUITO</code>
</p>

<p align="center">
  <a href="https://github.com/kacksdev/browndust2-ptbr/releases/tag/v0.1.0-beta"><strong>BAIXAR A VERSÃO ATUAL</strong></a>
  &nbsp;·&nbsp;
  <a href="./docs/INSTALACAO.md">INSTALAR OU REMOVER</a>
  &nbsp;·&nbsp;
  <a href="./docs/STATUS-TECNICO.md">VER O ESTADO TÉCNICO</a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/ink-rule-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/ink-rule-light.svg" />
  <img src="./assets/ink-rule-light.svg" alt="" width="100%" />
</picture>

## 01 / O PROJETO

**Brown Dust 2 PT-BR** é uma tradução comunitária gratuita para a versão de PC do jogo. O mod substitui textos exibidos pelo cliente por português brasileiro sem distribuir executáveis, bancos de dados, imagens ou outros arquivos proprietários do jogo.

Esta é uma **versão beta funcional**, não uma revisão editorial integral. O catálogo conhecido foi traduzido e passou por verificações estruturais e testes reais, mas a maior parte das entradas foi produzida com auxílio de IA. Portanto, ainda podem existir frases literais, escolhas inadequadas ao contexto, inconsistências ou textos introduzidos por atualizações posteriores.

> A numeração recomeçou em **0.1.0 beta** para representar o estado público com honestidade. A versão **1.0.0** fica reservada para uma tradução integralmente revisada, contextualizada e validada no jogo. Essa revisão avança de forma voluntária e não possui prazo anunciado.

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

Os números descrevem o processo do catálogo e não significam que 24.873 frases tenham recebido uma segunda revisão humana profissional independente. Zero alerta automático também não é sinônimo de perfeição semântica.

O pacote atual foi instalado e testado no cliente **2.32.7**, pacote **20260814000**. O plugin carregou o catálogo completo, aplicou os pontos centrais de tradução, preservou caracteres PT-BR e concluiu os testes de integração sem erro conhecido.

## 03 / INSTALAÇÃO RÁPIDA

1. Atualize e feche o Brown Dust 2.
2. Na seção [Releases](https://github.com/kacksdev/browndust2-ptbr/releases), baixe **`BD2PTBR-v0.1.0.zip`**.
3. Extraia o ZIP para uma pasta comum, como Downloads.
4. Execute **`INSTALAR-BD2-PTBR.cmd`** e confirme a pasta do jogo, caso ela não seja encontrada automaticamente.
5. Abra o jogo normalmente.

O instalador cria um backup antes de alterar os arquivos do carregador. Para remover o mod, execute **`REMOVER-BD2-PTBR.cmd`** no mesmo pacote. O guia com atualização, diagnóstico e restauração está em [Instalação e remoção](./docs/INSTALACAO.md).

> **Não use `Code → Download ZIP` para instalar.** Esse botão baixa o código-fonte. O pacote pronto fica exclusivamente em **Releases**.

## 04 / ATUALIZAÇÕES DO JOGO

O mod foi projetado para **falhar de forma segura**: se o catálogo estiver incompleto ou os pontos essenciais do cliente mudarem, os patches são desativados e o jogo mantém o texto original em vez de receber uma tradução parcial ou um estado instável.

Isso reduz o risco, mas nenhuma modificação pode prometer compatibilidade absoluta com versões futuras ainda desconhecidas. Depois de uma atualização do Brown Dust 2, use primeiro o lançador oficial, abra o jogo sem forçar arquivos antigos e consulte a [matriz de compatibilidade](./docs/COMPATIBILIDADE.md). Conteúdo novo pode aparecer em inglês até o catálogo ser atualizado.

## 05 / TRADUÇÃO EM JOGO

As imagens abaixo são capturas reais do cliente com o mod ativo. Elas demonstram interface, inventário, diálogos, conteúdos narrativos, Crônicas, Glossário e Guia.

| Interface e equipamentos | Diálogo e pacote de história |
| --- | --- |
| ![Tela principal traduzida](./assets/screenshots/01-interface-principal.webp) | ![Diálogo traduzido](./assets/screenshots/03-dialogo.webp) |
| ![Equipamentos traduzidos](./assets/screenshots/02-equipamentos.webp) | ![Pacote de história traduzido](./assets/screenshots/04-pacote-de-historia.webp) |

| Histórias Cotidianas e Crônicas | Glossário e Guia |
| --- | --- |
| ![Histórias Cotidianas traduzidas](./assets/screenshots/05-historias-cotidianas.webp) | ![Glossário traduzido](./assets/screenshots/07-glossario.webp) |
| ![Crônicas traduzidas](./assets/screenshots/06-cronicas.webp) | ![Guia traduzido](./assets/screenshots/08-guia.webp) |

## 06 / CÓDIGO ABERTO E VERIFICAÇÃO

| Área | Conteúdo público |
| --- | --- |
| [`src`](./src) | Código-fonte C# do plugin. |
| [`catalog`](./catalog) | Catálogo PT-BR em JSONL e relatório de auditoria. |
| [`installer`](./installer) | Instalação, backup, remoção e restauração. |
| [`tools`](./tools) | Auditorias, compilação, empacotamento e testes. |
| [`docs`](./docs) | Arquitetura, compatibilidade, autoria e uso. |

O pacote de Release inclui um manifesto e um arquivo SHA-256 para conferência. O repositório não inclui DLLs nem recursos proprietários do Brown Dust 2; as referências necessárias para recompilar o plugin devem ser obtidas da instalação legítima do próprio usuário.

## 07 / AUTORIA E TRANSPARÊNCIA

**Brown Dust 2 PT-BR é um projeto criado, dirigido, mantido e validado por mim.** A definição do escopo, do padrão de qualidade, das decisões editoriais, dos testes no cliente, da compatibilidade e da publicação permanece sob minha responsabilidade.

O **OpenAI Codex** integra o fluxo como ferramenta auxiliar. Ele foi utilizado para produzir as traduções PT-BR das entradas do catálogo, aplicar correções orientadas pelo projeto, apoiar o desenvolvimento do plugin, automatizar auditorias e acelerar a documentação. A engenharia e o controle de qualidade avançaram de forma colaborativa: defini requisitos, identifiquei problemas durante o uso real, conduzi testes, aprovei resultados e participei da execução técnica em grande escala.

Os detalhes estão em [Autoria e ferramentas](./docs/AUTORIA-E-FERRAMENTAS.md).

## 08 / RELATOS E DIREITOS

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
