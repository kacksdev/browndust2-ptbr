using System;
using System.Diagnostics;
using System.IO;
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

        public MainWindow()
        {
            InitializeComponent();
            ConfigureResponsiveWindow();
            Loaded += async (_, __) => await DetectAndInspectAsync(null);
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
                PrimaryButton.Content = "ESCOLHER PASTA";
                PrimaryButton.IsEnabled = true;
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
                    PrimaryButton.Content = "ATUALIZAR / REINSTALAR";
                    VerifyButton.IsEnabled = true;
                    RemoveButton.IsEnabled = true;
                    break;
                case InstallationHealth.RepairRequired:
                    SetStatus("REPARO NECESSÁRIO", inspection.Summary, new SolidColorBrush(Color.FromRgb(229, 181, 84)));
                    PrimaryButton.Content = "REPARAR AGORA";
                    VerifyButton.IsEnabled = true;
                    RemoveButton.IsEnabled = true;
                    foreach (var issue in inspection.Issues)
                    {
                        AddLog(issue);
                    }
                    break;
                case InstallationHealth.LegacyInstallation:
                    SetStatus("VERSÃO ANTERIOR ENCONTRADA", inspection.Summary, Brushes.White);
                    PrimaryButton.Content = "ATUALIZAR COM SEGURANÇA";
                    VerifyButton.IsEnabled = false;
                    RemoveButton.IsEnabled = true;
                    break;
                default:
                    SetStatus("PRONTO PARA INSTALAR", inspection.Summary, Brushes.White);
                    PrimaryButton.Content = "INSTALAR TRADUÇÃO";
                    VerifyButton.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                    break;
            }
            PrimaryButton.IsEnabled = true;
        }

        private async void PrimaryAction_OnClick(object sender, RoutedEventArgs e)
        {
            if (_inspection == null)
            {
                Browse_OnClick(sender, e);
                return;
            }
            await RunOperationAsync(InstallerAction.Install);
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
                if (action == InstallerAction.Uninstall)
                {
                    report = await Task.Run(() => new InstallerEngine().Uninstall(gameRoot, progress));
                }
                else if (action == InstallerAction.Verify)
                {
                    report = await Task.Run(() => new InstallerEngine().Verify(gameRoot, progress));
                }
                else
                {
                    report = await Task.Run(() => new InstallerEngine().Install(gameRoot, progress));
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
                picker.Description = "Selecione a pasta que contém Brown Dust II.exe";
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
            if (busy)
            {
                PrimaryButton.IsEnabled = false;
                VerifyButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
            }
            else if (!preserveActionState && _inspection != null)
            {
                ApplyInspection(_inspection);
            }
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
