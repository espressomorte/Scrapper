public partial class MetricsCollectorService
{
    [Serializable]
    public class MetricParseException : System.Exception
    {
        public MetricParseException() { }
        public MetricParseException(string message) : base(message) { }
        public MetricParseException(string message, System.Exception inner) : base(message, inner) { }
        protected MetricParseException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
