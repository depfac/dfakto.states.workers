using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dFakto.States.Workers.SqlToCsv
{
    public class CsvStreamWriter
    {
        private const string Doublequotes = "\"\"";

        private readonly char _theSeparator;
        private readonly string _theQuotesReplacementToken; // ou bien "\\\""
        private readonly TextWriter _theTextWriter;
        public bool ForceQuotes { get; set; }

        public CsvStreamWriter(TextWriter aWriter, char aSeparator = ',', string aQuotesReplacementToken = Doublequotes)
        {
            _theTextWriter = aWriter;
            _theSeparator = aSeparator;
            _theQuotesReplacementToken = aQuotesReplacementToken;
        }

        public CsvStreamWriter(string aFileName)
            : this(File.CreateText(aFileName))
        {
        }

        public void WriteValue(string aValue)
        {
            if (string.IsNullOrEmpty(aValue))
            {
                if (ForceQuotes)
                {
                    _theTextWriter.Write("\"\"");
                }
                return;
            }


            // TODO : réflechir aux " (si commence par " mais finit pas?)
            StringBuilder builder = new StringBuilder();
            if (ForceQuotes || aValue.IndexOfAny(new[] {'"', '\n', '\r', _theSeparator}) != -1)
            {
                builder.Append($"\"{aValue.Replace("\"", _theQuotesReplacementToken)}\"");
            }
            else
            {
                builder.Append(aValue);
            }

            _theTextWriter.Write(builder.ToString());
        }

        public void WriteLine(params string[] aLineValues)
        {
            WriteLine((IEnumerable<string>) aLineValues);
        }

        public void WriteLine(IEnumerable<string> aLineValues)
        {
            int position = 0;
            foreach (string value in aLineValues)
            {
                if (position > 0)
                    _theTextWriter.Write(_theSeparator);

                WriteValue(value);
                position++;
            }
            _theTextWriter.WriteLine();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theTextWriter?.Dispose();
            }
        }

        ~CsvStreamWriter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}