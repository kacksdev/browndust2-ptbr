# Estado técnico e validação

Versão, cliente testado, resultados e limitações de cada distribuição ficam na [Release correspondente](https://github.com/kacksdev/browndust2-ptbr/releases). A identidade verificável do catálogo publicado está em [catalog/manifest.json](../catalog/manifest.json).

## Critérios da distribuição

- Extração incremental por chave e original, sem promover traduções de fontes alteradas por simples coincidência de ID.
- Catálogo consolidado, sem camadas obsoletas capazes de sobrescrever as traduções atuais.
- Verificação de JSON, chaves únicas, fonte, Unicode, músicas protegidas e hashes.
- Marcadores conferidos pelo mesmo PlaceholderValidator.cs usado pelo runtime.
- Testes do atualizador: compatibilidade, downloads, arquivos externos, migração legada, transações interrompidas, rollback e restauração.
- Teste do artefato empacotado e execução real no cliente declarado antes da liberação.

Os relatórios públicos reproduzíveis são [audit-report.json](../catalog/audit-report.json) e [runtime-audit-report.json](../catalog/runtime-audit-report.json). Consulte [Compilação e testes](./COMPILACAO.md).

## Limites

Verificações estruturais não certificam o sentido de cada frase. A tradução continua em beta e não recebeu revisão humana integral independente. Uma amostra visual confirma somente as telas observadas. Conteúdo novo, diferenças de contexto e mudanças internas podem exigir correções. O aplicativo não possui certificado comercial de assinatura de código.
