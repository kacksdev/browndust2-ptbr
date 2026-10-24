using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BD2PTBR.Installer
{
    internal static class OfficialStarter
    {
        internal const string GameUri = "browndust2:games/10000002?usn=0";

        internal static ProcessStartInfo CreateStartInfo(string registeredCommand)
        {
            var match = Regex.Match(registeredCommand ?? string.Empty,
                "^\\s*\"(?<exe>[^\"]+)\"\\s+(?:\"%1\"|%1)\\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                throw new InvalidOperationException("O protocolo oficial do Brown Dust 2 não está registrado corretamente. Repare ou reinstale o Starter oficial.");
            var executable = Environment.ExpandEnvironmentVariables(match.Groups["exe"].Value);
            if (!Regex.IsMatch(executable, @"^[A-Za-z]:[\\/]") ||
                !string.Equals(Path.GetFileName(executable), "Browndust2Starter.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("O protocolo não aponta para o executável esperado do Starter oficial. Nenhum aplicativo foi aberto.");
            executable = Path.GetFullPath(executable);
            return new ProcessStartInfo(executable, GameUri)
            {
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = true
            };
        }

        internal static void EnsureGameClosed(IEnumerable<string> processNames)
        {
            if (processNames.Any(name => string.Equals(name, "BrownDust II", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Feche o jogo antes de abrir o Starter para atualizar o cliente.");
        }

        internal static string Open()
        {
            var processNames = new List<string>();
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try { processNames.Add(process.ProcessName); }
                    catch (InvalidOperationException) { }
                }
            }
            EnsureGameClosed(processNames);
            if (processNames.Any(name => string.Equals(name, "Browndust2Starter", StringComparison.OrdinalIgnoreCase)))
                return "O Starter oficial já está aberto. Conclua a atualização por ele; nenhuma instalação do mod foi iniciada.";

            string registeredCommand;
            using (var key = Registry.ClassesRoot.OpenSubKey(@"browndust2\shell\open\command"))
                registeredCommand = key == null ? null : key.GetValue(null) as string;
            var startInfo = CreateStartInfo(registeredCommand);
            if (!File.Exists(startInfo.FileName))
                throw new FileNotFoundException("O Starter registrado não foi encontrado. Repare ou reinstale o Starter oficial antes de tentar novamente.");
            using (var process = Process.Start(startInfo)) { }
            return "Starter oficial aberto. Conclua a atualização do jogo e feche o jogo e o Starter antes de aplicar uma atualização do mod.";
        }
    }
}
