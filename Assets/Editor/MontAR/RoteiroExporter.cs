using System;
using System.Globalization;
using System.IO;
using System.Text;
using MontAR.Dev;
using UnityEditor;
using UnityEngine;

namespace MontAR.EditorTools
{
    /// <summary>
    /// Exporta o roteiro e o catálogo de peças para Markdown (docs/documents/roteiro-bancada-v0.md).
    /// O manual técnico e o vídeo das condições de controle precisam seguir exatamente estas etapas.
    /// </summary>
    public static class RoteiroExporter
    {
        public const string OutputPath = "docs/documents/roteiro-bancada-v0.md";

        [MenuItem("MontAR/App/Exportar roteiro (Markdown)")]
        private static void ExportMenu()
        {
            Debug.Log("[MontAR] Roteiro exportado: " + Export());
        }

        public static string Export()
        {
            var procedure = AssetDatabase.LoadAssetAtPath<AssemblyProcedure>(MontagemPreviewBuilder.ProcedurePath);
            var catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(AppBuilder.CatalogPath);
            if (procedure == null || catalog == null)
                throw new InvalidOperationException("Roteiro ou catálogo não encontrado. Rode os construtores antes.");

            var md = new StringBuilder();
            md.AppendLine($"# Roteiro de montagem em bancada ({procedure.ProcedureId})");
            md.AppendLine();
            md.AppendLine($"> Gerado a partir de `{MontagemPreviewBuilder.ProcedurePath}` e `{AppBuilder.CatalogPath}` "
                + $"em {DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} (menu **MontAR › App › Exportar roteiro**). "
                + "Não edite este arquivo à mão: mude as etapas na Unity (ou no `spec_montagem.json`) e exporte de novo.");
            md.AppendLine();
            md.AppendLine("O app, o **manual técnico** e o **tutorial em vídeo** das condições de controle precisam seguir exatamente estas etapas, "
                + "na mesma ordem e com o mesmo kit (CLAUDE.md, §2).");
            md.AppendLine();
            md.AppendLine("**Rascunho v0.** Placa ATX genérica com posições aproximadas: o kit do laboratório ainda não foi definido (decisão D5). "
                + "Montagem em bancada, sem gabinete (decisão D4 em aberto).");
            md.AppendLine();

            md.AppendLine("## Peças (uma opção de cada, por enquanto)");
            md.AppendLine();
            md.AppendLine("| Peça | Modelo | Quantidade aceita | Padrão |");
            md.AppendLine("|---|---|---|---|");
            foreach (PartDefinition part in catalog.Parts)
            {
                string model = part.Models.Count > 0 ? part.Models[0].DisplayName : "—";
                string range = part.MinQuantity == part.MaxQuantity ? part.MinQuantity.ToString() : $"{part.MinQuantity} a {part.MaxQuantity}";
                string optional = part.IsRequired ? "" : " (opcional)";
                md.AppendLine($"| {part.DisplayName} (`{part.PartId}`) | {model} | {range}{optional} | {part.DefaultQuantity} |");
            }
            md.AppendLine();

            md.AppendLine("## Etapas");
            md.AppendLine();
            md.AppendLine("A coluna \"entra quando\" diz de qual peça a etapa depende. Com o kit padrão (2 pentes de RAM, SSD e placa de vídeo), "
                + "o roteiro tem " + StepFilter.Filter(procedure.Steps, catalog.CreateDefaultSelection()).Count + " etapas.");
            md.AppendLine();
            md.AppendLine("| # | Etapa | Entra quando | ID no CSV |");
            md.AppendLine("|---|---|---|---|");
            for (int i = 0; i < procedure.Steps.Count; i++)
            {
                AssemblyStep step = procedure.Steps[i];
                md.AppendLine($"| {i + 1} | {step.Title} | {Condition(step, catalog)} | `{step.StepId}` |");
            }
            md.AppendLine();

            for (int i = 0; i < procedure.Steps.Count; i++)
            {
                AssemblyStep step = procedure.Steps[i];
                md.AppendLine($"### {i + 1}. {step.Title}");
                md.AppendLine();
                md.AppendLine($"- **Componente:** {step.ComponentName}");
                md.AppendLine($"- **Entra quando:** {Condition(step, catalog)}");
                md.AppendLine($"- **Na RA:** {(string.IsNullOrEmpty(step.ReferenceImageName) ? "sem marcador (só instrução e checklist)" : $"overlay preso ao marcador `{step.ReferenceImageName}`")}");
                md.AppendLine();
                md.AppendLine(step.Instruction);
                md.AppendLine();
                if (!string.IsNullOrEmpty(step.CommonErrorAlert))
                {
                    md.AppendLine($"> **Atenção:** {step.CommonErrorAlert}");
                    md.AppendLine();
                }
                md.AppendLine("Validação (checklist, todos os itens precisam ser confirmados):");
                md.AppendLine();
                foreach (string item in step.ChecklistItems)
                    md.AppendLine($"- [ ] {item}");
                md.AppendLine();
            }

            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, md.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
            return fullPath;
        }

        private static string Condition(AssemblyStep step, PartCatalog catalog)
        {
            if (string.IsNullOrEmpty(step.RequiredPartId))
                return "sempre";

            PartDefinition part = catalog.Find(step.RequiredPartId);
            string name = part != null ? part.DisplayName : step.RequiredPartId;
            int min = Mathf.Max(1, step.MinPartQuantity);
            int max = step.MaxPartQuantity;
            string text;
            if (max > 0 && max == min)
                text = $"{name}: exatamente {min} {(part != null ? part.UnitFor(min) : "")}".TrimEnd();
            else if (max > 0)
                text = $"{name}: de {min} a {max}";
            else if (min > 1)
                text = $"{name}: {min} ou mais";
            else
                text = part != null && !part.IsRequired ? $"{name} no kit" : name;

            if (!string.IsNullOrEmpty(step.RequiredModelId))
                text += $" (modelo `{step.RequiredModelId}`)";
            return text;
        }
    }
}
