using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace BD2PTBR.Installer
{
    public partial class MainWindow : Window
    {
        private const string GitHubUrl = "https://github.com/kacksdev/browndust2-ptbr";
        private const string GameBananaUrl = "https://gamebanana.com/mods/706840";
        private bool _busy;
        private InstallationInspection _inspection;
        private UpdateRelease _release;

        public MainWindow(string initialGameRoot = null)
        {
            InitializeComponent();
            ConfigureResponsiveWindow();
            Loaded += async (_, __) => { await DetectAndInspectAsync(initialGameRoot); if (_inspection != null) await CheckUpdatesAsync(); };
            Closing += (_, e) => { if (_busy) e.Cancel = true; };
        }

        private void ConfigureResponsiveWindow()
        {
            var workArea = SystemParameters.WorkArea;
            var highResolution = workArea.Width >= 2400 && workArea.Height >= 1300;
            var targetWidth = highResolution ? 1920.0 : 1280.0;
            var targetHeight = highResolution ? 1080.0 : 720.0;
            Width = Math.Min(targetWidth, Math.Max(960, workArea.Width - 16));
            Height = Math.Min(targetHeight, Math.Max(600, workArea.Height - 16));
            MaxWidth = workArea.Width;
            MaxHeight = workArea.Height;
        }

        private async Task DetectAndInspectAsync(string requestedPath)
        {
            SetBusy(true);
            _release = null;
            SetStatus("LOCALIZANDO O JOGO", "Validando a pasta oficial do Brown Dust 2.", Brushes.White);
            AddLog("Iniciando detecção segura do cliente.");
            try
            {
                var path = requestedPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = await Task.Run(() => GameDetector.Detect());
                }

                _inspection = await Task.Run(() => new InstallerEngine().Inspect(path));
                GamePathBox.Text = _inspection.GameRoot;
                ModVersionText.Text = _inspection.ModVersion;
                ClientVersionText.Text = _inspection.TestedClient;
                PackageText.Text = string.IsNullOrWhiteSpace(_inspection.DetectedPackage) ? "não identificado" : _inspection.DetectedPackage;
                ApplyInspection(_inspection);
                AddLog("Cliente validado: " + _inspection.GameRoot);
                AddLog("Estado: " + _inspection.Summary);
            }
            catch (Exception error)
            {
                _inspection = null;
                GamePathBox.Text = requestedPath ?? string.Empty;
                SetStatus("PASTA NÃO CONFIRMADA", error.Message, new SolidColorBrush(Color.FromRgb(229, 181, 84)));
                UpdatePrimaryAction();
                VerifyButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                AddLog("Detecção não concluída: " + error.Message);
            }
            finally
            {
                SetBusy(false, preserveActionState: true);
            }
        }

        private void ApplyInspection(InstallationInspection inspection)
        {
            switch (inspection.Health)
            {
                case InstallationHealth.InstalledAndVerified:
                    SetStatus("INSTALAÇÃO VERIFICADA", inspection.Summary, new SolidColorBrush(Color.FromRgb(215, 230, 219)));
                    VerifyButton.IsEnabled = true;
                    RemoveButton.IsEnabled = true;
                    break;
                case InstallationHealth.RepairRequired:
                    SetStatus("REPARO NECESSÁRIO", inspection.Summary, new SolidColorBrush(Color.FromRgb(229, 181, 84)));
                    VerifyButton.IsEnabled = true;
                    RemoveButton.IsEnabled = true;
                    foreach (var issue in inspection.Issues)
                    {
                        AddLog(issue);
                    }
                    break;
                case InstallationHealth.LegacyInstallation:
                    SetStatus("VERSÃO ANTERIOR ENCONTRADA", inspection.Summary, Brushes.White);
                    VerifyButton.IsEnabled = false;
                    RemoveButton.IsEnabled = true;
                    break;
                default:
                    SetStatus("PRONTO PARA INSTALAR", inspection.Summary, Brushes.White);
                    VerifyButton.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                    break;
            }
            UpdatePrimaryAction();
            RestoreButton.IsEnabled = inspection.CanRestore;
        }

        private void UpdatePrimaryAction()
        {
            var installed = _inspection != null && _inspection.Health != InstallationHealth.NotInstalled;
            if (_release != null)
            {
                PrimaryButton.Content = _release.modVersion == _inspection.ModVersion
                    ? "REPARAR / REINSTALAR"
                    : (installed ? "ATUALIZAR PARA " : "INSTALAR ") + _release.modVersion;
                PrimaryButton.ToolTip = "Baixa e aplica o pacote compatível após sua confirmação.";
            }
            else
            {
                PrimaryButton.Content = !installed ? "INSTALAR TRADUÇÃO"
                    : _inspection.Health == InstallationHealth.RepairRequired ? "REPARAR TRADUÇÃO"
                    : _inspection.Health == InstallationHealth.LegacyInstallation ? "ATUALIZAR MOD"
                    : "REINSTALAR TRADUÇÃO";
                PrimaryButton.ToolTip = _inspection == null
                    ? "Escolha a pasta do jogo para consultar a compatibilidade."
                    : "Use Buscar atualização para localizar um pacote compatível.";
            }
            PrimaryButton.IsEnabled = !_busy && _inspection != null && _release != null;
        }

        private async void PrimaryAction_OnClick(object sender, RoutedEventArgs e)
        {
            if (_busy || _inspection == null || _release == null) return;
            if (MessageBox.Show("Baixar e aplicar o mod " + _release.modVersion + " neste cliente? Um backup será criado antes de qualquer alteração.", "Confirmar atualização", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            await RunOperationAsync(InstallerAction.Install);
        }

        private async void CheckUpdates_OnClick(object sender, RoutedEventArgs e) => await CheckUpdatesAsync();

        private void Starter_OnClick(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            try
            {
                var message = OfficialStarter.Open();
                SetStatus("STARTER OFICIAL", message, Brushes.White);
                AddLog(message);
            }
            catch (Exception error)
            {
                SetStatus("STARTER NÃO ABERTO", error.Message, new SolidColorBrush(Color.FromRgb(229, 181, 84)));
                AddLog("Starter: " + error.Message);
                DetailsExpander.IsExpanded = true;
            }
        }

        private async Task CheckUpdatesAsync()
        {
            if (_busy || _inspection == null) return;
            SetBusy(true);
            _release = null;
            UpdatePrimaryAction();
            SetStatus("CONSULTANDO ATUALIZAÇÕES", "Verificando o canal oficial e a compatibilidade deste cliente.", Brushes.White);
            try
            {
                var root = _inspection.GameRoot;
                _release = await new UpdateService().CheckAsync(root, CancellationToken.None);
                Version installed;
                if (_release != null && Version.TryParse(_inspection.ModVersion, out installed) && Version.Parse(_release.modVersion) < installed) _release = null;
                if (_release == null)
                {
                    SetStatus("SEM PACOTE COMPATÍVEL", "Nenhuma atualização compatível está disponível neste canal. A instalação existente não foi alterada.", new SolidColorBrush(Color.FromRgb(229, 181, 84)));
                }
                else
                {
                    bool same = _release.modVersion == _inspection.ModVersion;
                    SetStatus(same ? "VERSÃO ATUAL DISPONÍVEL" : "ATUALIZAÇÃO COMPATÍVEL", "Mod " + _release.modVersion + ". O download só começa após sua confirmação.", Brushes.White);
                    AddLog("Canal validado: mod " + _release.modVersion + "; cliente compatível.");
                }
            }
            catch (Exception error) { PresentFailure(OperationReport.Failure("CheckUpdate", _inspection.GameRoot, error)); }
            finally { SetBusy(false, true); UpdatePrimaryAction(); RestoreButton.IsEnabled = _inspection.CanRestore; }
        }

        private async void Restore_OnClick(object sender, RoutedEventArgs e)
        {
            if (_busy || _inspection == null || !_inspection.CanRestore) return;
            if (MessageBox.Show("Restaurar o backup anterior? A recuperação será bloqueada se o cliente do jogo mudou ou se os backups estiverem corrompidos.", "Confirmar recuperação", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                await RunOperationAsync(InstallerAction.Restore);
        }

        private async void Verify_OnClick(object sender, RoutedEventArgs e)
        {
            await RunOperationAsync(InstallerAction.Verify);
        }

        private async void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            if (_inspection == null)
            {
                return;
            }
            var answer = MessageBox.Show(
                "Remover a tradução PT-BR deste cliente? Arquivos anteriores preservados pelo instalador serão restaurados.",
                "Confirmar remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                await RunOperationAsync(InstallerAction.Uninstall);
            }
        }

        private async Task RunOperationAsync(InstallerAction action)
        {
            if (_busy || _inspection == null)
            {
                return;
            }

            SetBusy(true);
            DetailsExpander.IsExpanded = true;
            InstallProgress.Value = 0;
            ProgressPercent.Text = "0%";
            AddLog("Operação iniciada: " + action + ".");
            try
            {
                var progress = new Progress<InstallerProgress>(UpdateProgress);
                OperationReport report;
                var gameRoot = _inspection.GameRoot;
                if (action == InstallerAction.Restore)
                {
                    report = await Task.Run(() => new InstallerEngine().Restore(gameRoot, progress));
                }
                else if (action == InstallerAction.Uninstall)
                {
                    report = await Task.Run(() => new InstallerEngine().Uninstall(gameRoot, progress));
                }
                else if (action == InstallerAction.Verify)
                {
                    report = await Task.Run(() => new InstallerEngine().Verify(gameRoot, progress));
                }
                else
                {
                    if (_release == null) throw new InvalidOperationException("Consulte o canal de atualização antes de instalar.");
                    var options = await new UpdateService().DownloadAsync(_release, gameRoot, progress, CancellationToken.None);
                    report = await Task.Run(() => new InstallerEngine().Install(gameRoot, progress, options));
                }

                SetStatus("OPERAÇÃO CONCLUÍDA", report.Message, new SolidColorBrush(Color.FromRgb(215, 230, 219)));
                CompletionHint.Text = "Depois de testar, avalie o projeto no GitHub ou no GameBanana. Se encontrar um problema, envie um relato por uma dessas páginas.";
                AddLog(report.Message);
                foreach (var warning in report.Warnings)
                {
                    AddLog("Aviso: " + warning);
                }
                await DetectAndInspectAsync(gameRoot);
            }
            catch (InstallerOperationException error)
            {
                PresentFailure(error.Report);
            }
            catch (Exception error)
            {
                PresentFailure(OperationReport.Failure(action.ToString(), _inspection.GameRoot, error));
            }
            finally
            {
                SetBusy(false, preserveActionState: true);
            }
        }

        private void PresentFailure(OperationReport report)
        {
            var recovery = report.RolledBack
                ? " O estado anterior foi restaurado automaticamente."
                : string.Empty;
            SetStatus("OPERAÇÃO NÃO CONCLUÍDA", report.Message + recovery, new SolidColorBrush(Color.FromRgb(229, 181, 84)));
            AddLog("Falha: " + report.Message);
            if (report.RolledBack)
            {
                AddLog("Rollback concluído: nenhum estado parcial foi mantido.");
            }
            DetailsExpander.IsExpanded = true;
        }

        private void UpdateProgress(InstallerProgress item)
        {
            Dispatcher.Invoke(() =>
            {
                InstallProgress.Value = Math.Max(0, Math.Min(100, item.Percent));
                ProgressPercent.Text = Math.Max(0, Math.Min(100, item.Percent)) + "%";
                ProgressPhase.Text = item.Phase ?? "PROCESSANDO";
                AddLog(item.Message);
            });
        }

        private async void Browse_OnClick(object sender, RoutedEventArgs e)
        {
            using (var picker = new Forms.FolderBrowserDialog())
            {
                picker.Description = "Selecione a pasta que contém BrownDust II.exe";
                picker.ShowNewFolderButton = false;
                if (picker.ShowDialog() == Forms.DialogResult.OK)
                {
                    await DetectAndInspectAsync(picker.SelectedPath);
                }
            }
        }

        private void SetBusy(bool busy, bool preserveActionState = false)
        {
            _busy = busy;
            BrowseButtonState(!busy);
            StarterButton.IsEnabled = !busy;
            CheckUpdatesButton.IsEnabled = !busy && _inspection != null;
            if (busy)
            {
                PrimaryButton.IsEnabled = false;
                VerifyButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                RestoreButton.IsEnabled = false;
            }
            else if (!preserveActionState && _inspection != null)
            {
                ApplyInspection(_inspection);
            }
            else if (_inspection != null)
            {
                VerifyButton.IsEnabled = _inspection.Health == InstallationHealth.InstalledAndVerified || _inspection.Health == InstallationHealth.RepairRequired;
                RemoveButton.IsEnabled = _inspection.Health != InstallationHealth.NotInstalled;
                RestoreButton.IsEnabled = _inspection.CanRestore;
            }
            UpdatePrimaryAction();
        }

        private void BrowseButtonState(bool enabled)
        {
            BrowseButton.IsEnabled = enabled;
        }

        private void SetStatus(string title, string message, Brush color)
        {
            StatusTitle.Text = title;
            StatusMessage.Text = message;
            StatusDot.Fill = color;
        }

        private void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogText.Text += "[" + timestamp + "] " + message + Environment.NewLine;
            LogScroller.ScrollToEnd();
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void GitHub_OnClick(object sender, MouseButtonEventArgs e) => OpenUrl(GitHubUrl);
        private void GameBanana_OnClick(object sender, MouseButtonEventArgs e) => OpenUrl(GameBananaUrl);
        private void Minimize_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

        private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }
    }
}
