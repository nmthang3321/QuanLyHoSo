using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Documents
{
    public static class InitialResultDocumentGenerator
    {
        private static readonly XNamespace WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XName ParagraphName = WordNamespace + "p";
        private static readonly XName RunName = WordNamespace + "r";
        private static readonly XName RunPropertiesName = WordNamespace + "rPr";
        private static readonly XName HighlightName = WordNamespace + "highlight";
        private static readonly XName TextName = WordNamespace + "t";

        private static readonly DocumentTemplateDefinition[] Templates =
        {
            new DocumentTemplateDefinition("phieu_de_xuat.docx", "phieu_de_xuat.docx", TemplateKind.Proposal),
            new DocumentTemplateDefinition("phieu_huong_dan.docx", "phieu_huong_dan.docx", TemplateKind.Guidance),
            new DocumentTemplateDefinition("thong_bao.docx", "thong_bao.docx", TemplateKind.Notice)
        };

        public static IReadOnlyList<AttachmentDraft> Generate(RecordFormDraft record, string recordCode, string processingDate, string outputRoot)
        {
            if (record == null || string.IsNullOrWhiteSpace(recordCode))
            {
                return Array.Empty<AttachmentDraft>();
            }

            var templateRoot = GetTemplateRoot();
            if (!Directory.Exists(templateRoot))
            {
                return Array.Empty<AttachmentDraft>();
            }

            var outputFolder = Path.Combine(outputRoot, SanitizeFileName(recordCode));
            Directory.CreateDirectory(outputFolder);

            var generated = new List<AttachmentDraft>();
            foreach (var template in Templates)
            {
                var templatePath = Path.Combine(templateRoot, template.FileName);
                if (!File.Exists(templatePath))
                {
                    continue;
                }

                var outputPath = GetWritableOutputPath(outputFolder, template.OutputFileName);
                CreateWordDocument(templatePath, outputPath, BuildReplacementValues(template.Kind, record, recordCode, processingDate));

                generated.Add(new AttachmentDraft
                {
                    FileName = Path.GetFileName(outputPath),
                    FileSize = FormatFileSize(new FileInfo(outputPath).Length),
                    FilePath = outputPath
                });
            }

            return generated;
        }

        private static string GetTemplateRoot()
        {
            var templateRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doc");
            if (Directory.Exists(templateRoot))
            {
                return templateRoot;
            }

            templateRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "doc");
            if (Directory.Exists(templateRoot))
            {
                return templateRoot;
            }

            return Path.Combine(Environment.CurrentDirectory, "doc");
        }

        private static void CreateWordDocument(string templatePath, string outputPath, IReadOnlyList<string> replacements)
        {
            File.Copy(templatePath, outputPath, true);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);
            var replacementIndex = 0;
            foreach (var entry in archive.Entries.Where(IsWordXmlPart).ToList())
            {
                var document = LoadXml(entry);
                ReplaceHighlightedRuns(document, replacements, ref replacementIndex);
                SaveXml(archive, entry, document);
            }
        }

        private static bool IsWordXmlPart(ZipArchiveEntry entry)
        {
            return entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                && (entry.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase)
                    || entry.FullName.Contains("/header", StringComparison.OrdinalIgnoreCase)
                    || entry.FullName.Contains("/footer", StringComparison.OrdinalIgnoreCase));
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }

        private static void SaveXml(ZipArchive archive, ZipArchiveEntry entry, XDocument document)
        {
            var fullName = entry.FullName;
            entry.Delete();
            var newEntry = archive.CreateEntry(fullName, CompressionLevel.Optimal);
            using var stream = newEntry.Open();
            document.Save(stream, SaveOptions.DisableFormatting);
        }

        private static void ReplaceHighlightedRuns(XDocument document, IReadOnlyList<string> replacements, ref int replacementIndex)
        {
            foreach (var paragraph in document.Descendants(ParagraphName))
            {
                var highlightedGroup = new List<XElement>();
                foreach (var run in paragraph.Elements(RunName).ToList())
                {
                    if (IsHighlightedRun(run))
                    {
                        highlightedGroup.Add(run);
                        continue;
                    }

                    ReplaceHighlightedGroup(highlightedGroup, replacements, ref replacementIndex);
                }

                ReplaceHighlightedGroup(highlightedGroup, replacements, ref replacementIndex);
                if (replacementIndex >= replacements.Count)
                {
                    return;
                }
            }
        }

        private static bool IsHighlightedRun(XElement run)
        {
            return run.Element(RunPropertiesName)?.Element(HighlightName) != null;
        }

        private static void ReplaceHighlightedGroup(List<XElement> runs, IReadOnlyList<string> replacements, ref int replacementIndex)
        {
            if (runs.Count == 0)
            {
                return;
            }

            if (replacementIndex >= replacements.Count)
            {
                runs.Clear();
                return;
            }

            var firstRun = runs[0];
            RemoveHighlight(firstRun);
            ReplaceRunText(firstRun, replacements[replacementIndex++] ?? string.Empty);

            foreach (var run in runs.Skip(1).ToList())
            {
                run.Remove();
            }

            runs.Clear();
        }

        private static void RemoveHighlight(XElement run)
        {
            run.Element(RunPropertiesName)?.Element(HighlightName)?.Remove();
        }

        private static void ReplaceRunText(XElement run, string text)
        {
            var runProperties = run.Element(RunPropertiesName);
            run.Elements().Where(element => element.Name != RunPropertiesName).Remove();
            if (runProperties != null && run.Element(RunPropertiesName) == null)
            {
                run.AddFirst(runProperties);
            }

            run.Add(new XElement(
                TextName,
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                text));
        }

        private static string GetWritableOutputPath(string outputFolder, string outputFileName)
        {
            var outputPath = Path.Combine(outputFolder, outputFileName);
            if (!File.Exists(outputPath) || CanWriteFile(outputPath))
            {
                return outputPath;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFileName);
            var extension = Path.GetExtension(outputFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return Path.Combine(outputFolder, $"{fileNameWithoutExtension}_{timestamp}{extension}");
        }

        private static bool CanWriteFile(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static IReadOnlyList<string> BuildReplacementValues(TemplateKind kind, RecordFormDraft record, string recordCode, string processingDate)
        {
            var dateText = NormalizeDate(processingDate);
            var senderName = record.SenderName ?? string.Empty;
            var contactAddress = record.ContactAddress ?? string.Empty;
            var receiveSource = record.ReceiveSource ?? string.Empty;
            var content = record.Content ?? string.Empty;
            var note = record.Note ?? string.Empty;
            var additionalNote = record.AdditionalNote ?? string.Empty;

            return kind switch
            {
                TemplateKind.Proposal => new[]
                {
                    dateText,
                    dateText,
                    senderName,
                    contactAddress,
                    receiveSource,
                    content,
                    note,
                    additionalNote
                },
                TemplateKind.Guidance => new[]
                {
                    senderName,
                    recordCode,
                    dateText,
                    dateText,
                    senderName,
                    contactAddress,
                    receiveSource,
                    content,
                    note
                },
                _ => new[]
                {
                    dateText,
                    dateText,
                    senderName,
                    contactAddress,
                    receiveSource,
                    content,
                    note
                }
            };
        }

        private static string NormalizeDate(string value)
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var exactDate))
            {
                return exactDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                ? date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
                : value ?? string.Empty;
        }

        private static string FormatFileSize(long bytes)
        {
            return bytes < 1024 * 1024
                ? $"{Math.Max(1, bytes / 1024)} KB"
                : $"{bytes / (1024d * 1024d):0.0} MB";
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat((value ?? string.Empty).Select(ch => invalidChars.Contains(ch) ? '_' : ch));
        }

        private enum TemplateKind
        {
            Proposal,
            Guidance,
            Notice
        }

        private sealed class DocumentTemplateDefinition
        {
            public DocumentTemplateDefinition(string fileName, string outputFileName, TemplateKind kind)
            {
                FileName = fileName;
                OutputFileName = outputFileName;
                Kind = kind;
            }

            public string FileName { get; }
            public string OutputFileName { get; }
            public TemplateKind Kind { get; }
        }
    }
}
