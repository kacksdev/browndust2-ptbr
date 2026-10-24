# Arquitetura

| Componente | Responsabilidade |
| --- | --- |
| BepInEx | Carregar o plugin no cliente de PC. |
| Plugin C# | Aplicar traduções nos pontos cobertos, coordenar o runtime e as proteções de texto e fonte. |
| Catálogo JSONL | Vincular chave e original à tradução; arquivos consolidados identificados por manifesto e SHA-256. |
| Atualizador universal | Consultar o canal oficial, selecionar um pacote compatível e administrar instalação, verificação, recuperação e remoção. |
| Auditoria | Verificar estrutura e marcadores antes da distribuição. |

## Ativação

O plugin carrega os arquivos JSONL, verifica a quantidade mínima esperada e instala os pontos essenciais de tradução. Se a ativação central falhar, remove os próprios patches e preserva o idioma original. O catálogo é consultado em memória; as varreduras visuais são limitadas à inicialização e a mudanças de cena.

## Atualização transacional

BD2PTBR.exe é separado do payload. O canal oficial declara URL, tamanho, SHA-256, versão mínima do aplicativo e identidades de clientes aceitas. O aplicativo verifica o cliente antes de baixar e antes de aplicar, valida o conteúdo do ZIP e cria backups verificados antes de alterar arquivos gerenciados.

Jornais persistentes permitem recuperar uma operação interrompida. A restauração manual exige cliente e backups válidos; não reverte a versão oficial do jogo. Arquivos externos ao mod são preservados.

**Buscar atualização** apenas consulta o canal. O botão principal instala, atualiza ou repara após confirmação. **Abrir Starter oficial** usa o protocolo registrado pela instalação legítima; não depende de uma pasta fixa nem atualiza o mod.

A recompilação e os testes estão descritos em [COMPILACAO.md](./COMPILACAO.md). As referências proprietárias do jogo não são redistribuídas.
