namespace dFakto.States.Workers.Abstractions
{
    public class SqlQuery
    {
        public string QueryFileToken { get; set; }
        public string Query { get; set; }
        public SqlQueryType Type { get; set; } = SqlQueryType.NonQuery;
        public int MaxResults { get; set; } = 10;
        public SqlQueryParameter[] Params { get; set; }
    }
}