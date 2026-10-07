using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteelFlameAbyss.Editor.Data
{
    internal class CsvTable
    {
        private readonly string sourceName;
        private readonly List<string> headers;
        private readonly List<List<string>> rows;

        public int RowCount => rows.Count;

        private CsvTable(string sourceName, List<string> headers, List<List<string>> rows)
        {
            this.sourceName = sourceName;
            this.headers = headers;
            this.rows = rows;
        }

        /// <summary>URL에서 내려받은 CSV 문자열을 헤더와 행 단위로 해석합니다.</summary>
        public static CsvTable Parse(string sourceName, string text)
        {
            var parsed = ParseRows(text);
            if (parsed.Count == 0)
                throw new InvalidDataException($"{sourceName}: CSV 내용이 비어 있습니다.");

            var headers = parsed[0];
            if (headers.Count > 0)
                headers[0] = headers[0].TrimStart('\uFEFF');
            parsed.RemoveAt(0);

            for (var i = parsed.Count - 1; i >= 0; i--)
            {
                if (parsed[i].TrueForAll(string.IsNullOrWhiteSpace))
                    parsed.RemoveAt(i);
            }

            return new CsvTable(sourceName, headers, parsed);
        }

        public void Require(params string[] requiredHeaders)
        {
            foreach (var header in requiredHeaders)
            {
                if (IndexOf(header) < 0)
                    throw new InvalidDataException($"{sourceName}: required column '{header}' is missing.");
            }
        }

        public bool Has(params string[] requiredHeaders)
        {
            foreach (var header in requiredHeaders)
            {
                if (IndexOf(header) < 0)
                    return false;
            }
            return true;
        }

        /// <summary>Total Card의 두 번째 행처럼 열의 자료형만 적힌 행인지 판별합니다.</summary>
        public bool IsTypeDeclarationRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= rows.Count || rows[rowIndex].Count == 0)
                return false;

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "string", "int", "enum", "bool", "float"
            };
            return rows[rowIndex].Where(value => !string.IsNullOrWhiteSpace(value))
                .All(value => allowed.Contains(value.Trim()));
        }

        public string Get(int rowIndex, string header)
        {
            var columnIndex = IndexOf(header);
            if (columnIndex < 0)
                throw new InvalidDataException($"{sourceName}: column '{header}' is missing.");
            return columnIndex < rows[rowIndex].Count ? rows[rowIndex][columnIndex].Trim() : string.Empty;
        }

        public int SourceLine(int rowIndex) => rowIndex + 2;

        private int IndexOf(string header) => headers.FindIndex(value =>
            string.Equals(value.Trim(), header, StringComparison.OrdinalIgnoreCase));

        private static List<List<string>> ParseRows(string text)
        {
            var result = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                if (current == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (current == ',' && !inQuotes)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if ((current == '\r' || current == '\n') && !inQuotes)
                {
                    if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                    row.Add(field.ToString());
                    field.Clear();
                    result.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(current);
                }
            }

            if (inQuotes)
                throw new InvalidDataException("CSV contains an unclosed quoted field.");

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                result.Add(row);
            }
            return result;
        }
    }
}
