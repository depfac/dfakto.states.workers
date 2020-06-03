namespace dFakto.States.Workers.Sql.Common
{
    public class BulkInsertDestination
    {
        public string ConnectionName { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public int Timeout { get; set; } = 600;
        public bool TruncateFirst { get; set; } = false;
    }
}