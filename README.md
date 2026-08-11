<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/workbench-dark.png" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/workbench-light.png" />
  <img src="./assets/workbench-light.png" alt="Mesa de produção em desenho monocromático, com interfaces, código e anotações de localização" width="100%" />
</picture>

<h1 align="center">BROWN DUST 2 PT-BR / PC</h1>

<p align="center">
  <strong>LOCALIZAÇÃO COMUNITÁRIA / PROGRESSO PÚBLICO / DISTRIBUIÇÃO FECHADA</strong>
</p>

<p align="center">
  <code>v0.9.6</code>&nbsp;&nbsp;
  <code>VALIDAÇÃO PRIVADA</code>&nbsp;&nbsp;
  <code>CLIENTE v2.31.8(FHD-046:137)</code>&nbsp;&nbsp;
  <code>PC</code>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/ink-rule-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/ink-rule-light.svg" />
  <img src="./assets/ink-rule-light.svg" alt="" width="100%" />
</picture>

## 01 / O QUE É ESTE REPOSITÓRIO

Este é o painel público de desenvolvimento do **Brown Dust 2 PT-BR para PC**, um projeto comunitário, gratuito e sem afiliação oficial com a NEOWIZ ou a GAMFS N.

O repositório publica progresso, decisões técnicas, histórico e critérios de qualidade. **Ele não contém o mod, DLLs, catálogos traduzidos, código de injeção, instaladores, pacotes ou versões para download.** O botão **Code → Download ZIP** baixa somente esta documentação e suas imagens.

> **O mod ainda não está disponível.** A implementação e os pacotes de teste permanecem privados até que a experiência esteja segura e pronta para uma etapa pública autorizada pelo mantenedor.

## 02 / ESTADO ATUAL

<table>
  <tr>
    <td width="240" align="center">
      <picture>
        <source media="(prefers-color-scheme: dark)" srcset="./assets/bd2-icon-dark.jpg" />
        <source media="(prefers-color-scheme: light)" srcset="./assets/bd2-icon-light.jpg" />
        <img src="./assets/bd2-icon-light.jpg" alt="Ícone de Brown Dust 2 reinterpretado em desenho monocromático" width="205" />
      </picture>
    </td>
    <td>
      <strong>FASE 4 DE 6 — VALIDAÇÃO PRIVADA</strong><br /><br />
      <strong>223.743</strong> entradas efetivas<br />
      <strong>24.873</strong> entradas com revisão editorial explícita<br />
      <strong>293</strong> títulos musicais preservados no original<br />
      <strong>53</strong> títulos e <strong>53</strong> descrições de Crônicas cobertos<br /><br />
      Auditoria estrutural zerada, fila editorial zerada, Histórias Cotidianas cobertas, diálogo progressivo corrigido, crash do guia eliminado na causa e desempenho validado no cliente compatível.
    </td>
  </tr>
</table>

| Marco | Estado | Evidência atual |
| --- | --- | --- |
| Base técnica e catálogo integral | **Concluído** | Catálogo carregado pelo plugin e integração funcional no cliente. |
| Cobertura textual e terminológica | **Concluído** | 223.743 entradas efetivas e resíduos conhecidos classificados. |
| Integridade estrutural e estabilidade | **Concluído** | Zero falha estrutural; correções de crash, digitação e varredura contínua validadas. |
| Validação privada ampliada | **Em andamento** | Testes reais de narrativa, interface, combate, guias, Crônicas e atualização. |
| Candidato público e instalação assistida | **Pendente** | Exige matriz de testes maior, rollback final e documentação completa de instalação. |
| Versão 1.0.0 | **Pendente** | Somente após qualidade suficiente e aprovação dos critérios finais definidos por Kacks. |

O detalhamento técnico está em [Status técnico](./docs/STATUS-TECNICO.md), e os próximos critérios estão no [Roadmap](./docs/ROADMAP.md).

## 03 / O QUE MUDOU

- **0.8.0:** base integral recuperada e preservada como ponto de partida.
- **0.9.0:** consolidação editorial, terminológica e estrutural; proteção de músicas e preparação do pacote privado.
- **0.9.6:** cobertura de Crônicas e novas Histórias Cotidianas; correção do texto progressivo, da escolha de diálogo em inglês, do crash do livro de ajuda e do custo de varredura em execução.

Consulte o [Histórico público](./docs/HISTORICO.md) para os números e validações de cada versão.

## 04 / AUTORIA E FERRAMENTAS

**Brown Dust 2 PT-BR é um projeto criado, dirigido, mantido e validado por Kacks.** A definição do escopo, o padrão de qualidade, as decisões editoriais, os testes no cliente, a compatibilidade e a publicação permanecem sob responsabilidade do autor do projeto.

O **OpenAI Codex** integra o fluxo como ferramenta auxiliar. Ele foi utilizado para produzir as traduções PT-BR das entradas do catálogo, aplicar correções orientadas pelo projeto, apoiar o desenvolvimento do plugin, automatizar auditorias e acelerar a documentação. A engenharia e o controle de qualidade avançaram de forma colaborativa: Kacks definiu requisitos, identificou problemas durante o uso real, conduziu os testes e aprovou os resultados; Codex auxiliou na execução técnica em grande escala.

Na versão 0.9.6, **198.773 entradas** permanecem classificadas internamente como `machine` e **24.873 entradas** possuem tratamento editorial explícito. Essas classificações descrevem o processo do catálogo e não significam revisão humana profissional independente de cada frase. O projeto mantém essa informação pública para evitar qualquer ambiguidade sobre como a tradução foi produzida.

Leia a atribuição completa em [Autoria e ferramentas](./docs/AUTORIA-E-FERRAMENTAS.md).

## 05 / PRINCÍPIOS

| Regra | Critério |
| --- | --- |
| **Contexto antes da literalidade** | Intenção, personalidade e emoção têm prioridade sobre substituição palavra por palavra. |
| **Identidade preservada** | Nomes próprios, músicas e marcas ficam no original quando traduzi-los prejudicaria a obra. |
| **Integridade técnica** | Placeholders, tags, layout, paginação, carregamento e atualização não podem ser quebrados. |
| **Compatibilidade declarada** | Toda versão informa exatamente qual cliente foi validado; atualização do jogo exige nova checagem. |
| **Publicação responsável** | Sem pacote público antes de instalação, remoção, rollback e regressões estarem suficientemente cobertos. |

## 06 / LIMITES E DIREITOS

- Projeto comunitário para **PC**, gratuito e sem monetização.
- Nenhum arquivo proprietário do jogo é distribuído neste repositório.
- Brown Dust 2, personagens, nomes e artes pertencem aos respectivos titulares.
- Este projeto não é uma tradução oficial e não representa endosso dos titulares do jogo.
- A existência pública desta documentação **não significa liberação do mod**.

Sugestões de documentação podem ser enviadas pelas [Issues](https://github.com/kacksdev/browndust2-ptbr/issues). Pull requests não alteram o projeto automaticamente e só podem ser integrados pelo proprietário.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/ink-rule-dark.svg" />
  <source media="(prefers-color-scheme: light)" srcset="./assets/ink-rule-light.svg" />
  <img src="./assets/ink-rule-light.svg" alt="" width="100%" />
</picture>

<p align="center"><code>KACKS / COMMUNITY LOCALIZATION / BRASIL</code></p>
