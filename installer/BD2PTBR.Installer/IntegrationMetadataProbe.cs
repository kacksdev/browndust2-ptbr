using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace BD2PTBR.Installer
{
    internal static class IntegrationMetadataProbe
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Inspect(string root)
        {
            var managed = PathSafety.CombineUnder(root, "BrownDust II_Data/Managed");
            // No resolving/loading into the CLR and no game code execution.
            using (var game = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll")))
            using (var tmp = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Unity.TextMeshPro.dll")))
            {
                var text = game.MainModule.GetType("Proto.Design.common.NameTextTable");
                if (!HasProperty(text, "Id") || !HasProperty(text, "TextEn"))
                    throw new InvalidDataException("As tabelas de texto do cliente não oferecem a estrutura suportada.");
                var score = game.MainModule.Types.Where(t => t.IsAbstract && t.IsSealed)
                    .Select(LocalizationScore).DefaultIfEmpty(0).Max();
                if (score < 50) throw new InvalidDataException("A central de localização do cliente mudou de estrutura.");
                var guide = game.MainModule.GetType("GuideBookUI")?.NestedTypes.FirstOrDefault(t => t.Name == "GuideBookContentState");
                if (guide == null || !guide.Methods.Any(m => !m.IsStatic && m.HasBody && m.ReturnType.FullName == "System.Void" &&
                    m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Collections.Generic.List`1<Proto.Design.common.GuideTutorialTable>"))
                    throw new InvalidDataException("A proteção de paginação do guia requer uma integração atualizada.");
                var tmpText = tmp.MainModule.GetType("TMPro.TMP_Text");
                if (tmpText == null || !tmpText.Methods.Any(m => !m.IsStatic && m.HasBody && (m.Name == "set_text" || m.Name == "SetText") &&
                    m.Parameters.Count > 0 && m.Parameters[0].ParameterType.FullName == "System.String"))
                    throw new InvalidDataException("O renderizador de texto requer uma integração atualizada.");
                var font = tmp.MainModule.GetType("TMPro.TMP_FontAsset");
                if (font == null || !font.Methods.Any(m => !m.IsStatic && m.HasBody && m.Name == "TryAddCharacterInternal" &&
                    m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.UInt32" &&
                    m.Parameters[1].ParameterType.FullName == "TMPro.TMP_Character&"))
                    throw new InvalidDataException("A proteção de caracteres PT-BR requer uma integração atualizada.");
                return "Integração Mono x64 reconhecida; tabelas, central de localização, renderização, fontes e guia disponíveis.";
            }
        }

        private static bool HasProperty(TypeDefinition type, string name) => type != null && type.Properties.Any(p => p.Name == name);

        private static int LocalizationScore(TypeDefinition type)
        {
            int score = 0;
            foreach (var method in type.Methods.Where(m => m.IsStatic && m.ReturnType.FullName == "System.String" && m.Parameters.Count >= 1 && m.Parameters.Count <= 2))
            {
                var first = method.Parameters[0].ParameterType;
                if (first.FullName == "System.Int32") score++;
                else if (first.Namespace == "Proto.Design.common")
                {
                    var definition = type.Module.GetType(first.FullName);
                    if (HasProperty(definition, "Id") && HasProperty(definition, "TextEn")) score += 10;
                }
            }
            return score;
        }
    }
}
