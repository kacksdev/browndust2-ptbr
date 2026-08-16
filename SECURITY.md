# Política de segurança

## Versões cobertas

| Versão | Suporte |
| --- | --- |
| 0.1.x beta | Sim |
| Pacotes anteriores privados | Não |

## Relato privado

Não abra uma Issue pública para vulnerabilidade, credencial, dado pessoal ou comportamento que possa comprometer o computador de outro usuário. Use o [relato privado de vulnerabilidade](https://github.com/kacksdev/browndust2-ptbr/security/advisories/new).

Inclua, quando possível:

- versão do mod e do cliente;
- arquivo ou etapa afetada;
- comportamento observado e esperado;
- reprodução mínima sem dados pessoais.

## Medidas do projeto

- pacote acompanhado por manifesto e hash SHA-256;
- catálogo auditado antes do empacotamento;
- caminhos de instalação validados;
- backup antes da alteração de arquivos existentes;
- removedor restrito aos componentes conhecidos;
- ativação fail-open: falha central desativa o mod e preserva o texto original;
- secret scanning, push protection, Dependabot e branch principal protegida no GitHub.

## Fora do escopo

- vulnerabilidades do próprio Brown Dust 2 ou de seus serviços;
- contorno de autenticação, proteção, anticheat ou monetização;
- arquivos do cliente obtidos de fontes não autorizadas;
- modificações feitas por pacotes de terceiros.
