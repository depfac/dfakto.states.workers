namespace dFakto.States.Workers.Config
{
    public class StepFunctionsConfig
    {
        public string AuthenticationKey { get; set; }
        public string AuthenticationSecret { get; set; }
        public string ServiceUrl { get; set; }
        public string AwsRegion { get; set; }
        public string EnvironmentName { get; set; }
        public int RegisterRetryDelay { get; set; } = 5;
        public bool IgnoreSelfSignedCertificates { get; set; }
    }
}