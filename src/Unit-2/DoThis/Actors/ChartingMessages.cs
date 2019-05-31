using Akka.Actor;

namespace ChartApp.Actors
{
    #region Reporting
    
    /// <summary>
    /// Signal that it's time to sample all counters.
    /// </summary>
    public class GatherMetrics { }

    /// <summary>
    /// Metric data at time of sample.
    /// </summary>
    public class Metric
    {
        public Metric(string series, float counterValue)
        {
            CounterValue = counterValue;
            Series = series;
        }
        
        public string Series { get; }
        
        public float CounterValue { get; }
    }
    
    #endregion
    
    #region Performance Counter Management

    /// <summary>
    /// All types of counters supported by this example.
    /// </summary>
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    /// <summary>
    /// Enables a specific counter and begin publishing values to <see cref="Subscriber"/>.
    /// </summary>
    public class SubscribeCounter
    {
        public SubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; }
        
        public IActorRef Subscriber { get; }
    }

    /// <summary>
    /// Unsubscribes <see cref="Subscriber"/> from receiving updates for the specified counter.
    /// </summary>
    public class UnsubscribeCounter
    {
        public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }
        
        public CounterType Counter { get; }
        
        public IActorRef Subscriber { get; }
    }
    
    #endregion
}