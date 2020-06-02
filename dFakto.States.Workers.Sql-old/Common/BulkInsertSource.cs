using System;

namespace dFakto.States.Workers.Sql.Common
{
    public class BulkInsertSource
    {
        public string ConnectionName { get; set; }
        public SqlQuery Query { get; set; }
        public string FileToken { get; set; }
        public string FileName { get; set; }
        public char Separator { get; set; }
        public bool Headers { get; set; }
        public string CultureName { get; set; } = "EN-us";
    }
}