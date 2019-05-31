using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for monitoring a specific <see cref="PerformanceCounter"/>.
    /// </summary>
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        // We use a generator to generate `PerformanceCounter` instances because `PerformanceCounter` instances 
        // implement `IDisposable`. By default, if an actor instance is restarted, the actor system supplies the
        // **same** instances supplied to the original constructor. However, since these instances are `IDisposable`, 
        // it is likely that these instance **have been disposed**. 
        //
        // By using a generator, this class can generate the appropriate instances to monitor when restarted.
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _counter;
        
        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;

            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
        }
        
        #region Actor lifecycle methods

        protected override void PreStart()
        {
            // Create an new instance of the performance counter.
            _counter = _performanceCounterGenerator();
            
            // Schedule gathering metrics.
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(250), // initial delay
                                                            TimeSpan.FromMilliseconds(250), // then repeat every
                                                            Self, // send myself a message
                                                            new GatherMetrics(), // time to gather metrics
                                                            Self, // other actors involved?
                                                            _cancelPublishing // token to cancel scheduled messages
                                                            );
        }

        protected override void PostStop()
        {
            try
            {
                // Terminate the scheduled Task
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
                // Don't care about additional `ObjectDisposed` exceptions
            }
            finally
            {
                base.PostStop();
            }
        }

        #endregion
        
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case GatherMetrics _:
                    // Publish the latest counter value to all subscribers
                    var metric = new Metric(_seriesName, _counter.NextValue());
                    foreach (var sub in _subscriptions)
                    {
                        sub.Tell(metric);
                    }

                    break;

                case SubscribeCounter subscribeCounter:
                    // Add a subscription for this counter
                    // (It's the parent's job to filter by counter types)
                    _subscriptions.Add(subscribeCounter.Subscriber);
                    break;

                case UnsubscribeCounter unsubscribeCounter:
                    // Remove a subscription for this counter
                    _subscriptions.Remove(unsubscribeCounter.Subscriber);
                    break;
            }
        }
    }
}