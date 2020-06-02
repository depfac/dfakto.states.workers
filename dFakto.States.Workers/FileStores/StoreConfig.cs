using Microsoft.Extensions.Configuration;

namespace dFakto.States.Workers.FileStores
{
    public class StoreConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public IConfigurationSection Config { get; set; }
    }
}