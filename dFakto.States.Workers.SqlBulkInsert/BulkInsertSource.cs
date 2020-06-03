using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Abstractions.Sql;

namespace dFakto.States.Workers.SqlBulkInsert
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