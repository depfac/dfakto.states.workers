using dFakto.States.Workers.Abstractions.Sql;

namespace dFakto.States.Workers.SqlToCsv
{
    public class SqlExportToCsvInput: SqlQuery
    {
        public string ConnectionName { get; set; }
        public string OutputFileStoreName { get; set; }
        public string OutputFileName { get; set; }
        public char Separator { get; set; } = ';';
        public new SqlQueryType Type { get; set; } = SqlQueryType.Reader;
    }
}