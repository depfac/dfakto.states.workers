
using dFakto.States.Workers.SqlBulkInsert;

namespace dFakto.States.Workers.Sql.Common
{
    public class BulkInsertInput
    {
        public BulkInsertSource Source { get; set; } = new BulkInsertSource();
        public BulkInsertDestination Destination { get; set; } = new BulkInsertDestination();
    }
}