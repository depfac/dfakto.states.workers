namespace dFakto.States.Workers.Stores
{
    public class StoreFactoryConfig
    {
        public StoreFactoryConfig()
        {
            Stores = new StoreConfig[0];
        }
        public StoreConfig[] Stores { get; set; }
    }
}