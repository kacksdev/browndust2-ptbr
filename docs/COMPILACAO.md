# Compilação e testes

## Requisitos

Windows x64, PowerShell 7, Python 3.12 e SDK .NET 8 ou posterior. Para compilar o plugin, prepare as referências de uma instalação legítima local com BepInEx; as DLLs do jogo não são publicadas neste repositório.

```powershell
pwsh -File tools/Prepare-References.ps1 -GameRoot "D:\Jogos\BrownDust2_10000002"
python tools/audit_public_catalog.py
$catalog = Get-Content catalog/manifest.json -Raw | ConvertFrom-Json
dotnet run --project tools/RuntimeCatalogAudit -c Release -- catalog $catalog.expectedEffectiveEntries
dotnet build src/BD2PTBR.Plugin/BD2PTBR.csproj -c Release
pwsh -File tools/Build-UniversalLauncher.ps1 -OutputRoot dist/launcher-local
dotnet run --project tools/UpdaterTests -c Release -- work/updater-tests-local
```

Use pastas de saída novas. A contagem do comando de auditoria deve corresponder a `expectedEffectiveEntries` em `catalog/manifest.json`; ela identifica o snapshot, não um critério editorial.

## Pacote de atualização

Prepare um staging isolado com a distribuição oficial BepInEx indicada nos avisos de terceiros, o plugin compilado, os avisos de licença e os dois JSONL publicados, em `BepInEx/plugins/BD2PTBR/translations`. Não use a pasta real do jogo como staging nem inclua suas DLLs proprietárias.

```powershell
pwsh -File tools/New-UpdatePackage.ps1 -PayloadRoot work/payload-staging -GameRoot "D:\Jogos\BrownDust2_10000002" -ModVersion $catalog.modVersion -OutputRoot dist/payload-local
```

O empacotador verifica o escopo dos arquivos e registra a identidade do cliente local. Essa operação produz um candidato; não comprova compatibilidade real nem autoriza publicação. O candidato precisa passar pela auditoria, pela matriz do artefato e pelo teste no jogo.

Os scripts históricos Build-Installer, Build-Release, Install-Local e Test-Installer foram desativados para impedir que o fluxo antigo gere ou instale um catálogo obsoleto. O histórico completo permanece no Git.

## Publicação

Depois dos gates, publique os artefatos na Release, confira tamanho e SHA-256 por download e só então atualize `updates/stable.json`. Mantenha o README e a página do GameBanana permanentes; alterações específicas, cobertura e compatibilidade ficam na Release e no feed. Uma mudança no próprio aplicativo pode exigir migração excepcional.
