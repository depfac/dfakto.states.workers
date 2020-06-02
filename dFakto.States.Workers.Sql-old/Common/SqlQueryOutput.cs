using System.Collections.Generic;

namespace dFakto.States.Workers.Sql.Common
{
    public class SqlQueryOutput
    {
        public object Scalar { get; set; }
        public List<Dictionary<string,object>> Result { get; set; }
    }
}