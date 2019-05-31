using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Translates UI calls into ActorSystem messages
    /// </summary>
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to updates for <see cref="CounterType"/>.
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; }
        }

        
        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/> to updates for <see cref="CounterType"/>.
        /// </summary>
        public class Unwatch
        {
            public Unwatch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; }
        }
        
        #endregion

        /// <summary>
        /// A map between <see cref="CounterType"/> and function to generate the correct
        /// <see cref="PerformanceCounter"/>.
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>>
            CounterGenerators = new Dictionary<CounterType, Func<PerformanceCounter>>
            {
                {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
                {CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
                {CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)}
            };

        /// <summary>
        /// Mapping between <see cref="CounterType"/>s and functions to generate a new <see cref="Series"/>. The
        /// functions specify distinct colors and names corresponding to each <see cref="PerformanceCounter"/>.
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>>
            CounterSeries = new Dictionary<CounterType, Func<Series>>
            {
                {
                    CounterType.Cpu, () => new Series(CounterType.Cpu.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkGreen
                    }
                },
                {
                    CounterType.Memory, () => new Series(CounterType.Memory.ToString())
                    {
                        ChartType = SeriesChartType.FastLine,
                        Color = Color.MediumBlue
                    }
                },
                {
                    CounterType.Disk, () => new Series(CounterType.Disk.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkRed
                    }
                }
            };


        public PerformanceCounterCoordinatorActor(IActorRef chartingActor)
        : this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {
            
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
                                                  Dictionary<CounterType, IActorRef> counterActors)
        {
            Receive<Watch>(watch =>
            {
                // Create a child actor to monitor this counter if one **does not** already exist
                if (!counterActors.ContainsKey(watch.Counter))
                {
                    // Create new monitoring actor
                    var counterActor =
                        Context.ActorOf(Props.Create(
                                            () => new PerformanceCounterActor(
                                                watch.Counter.ToString(), CounterGenerators[watch.Counter])));

                    // Add this counter to our index
                    counterActors[watch.Counter] = counterActor;
                }
                
                // Register this series with the ChartingActor
                chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));
                
                // Tell the counter actor to begin publishing its statistics to the _chartingActor.
                counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter, chartingActor));
            });

            Receive<Unwatch>(unwatch =>
            {
                if (!counterActors.ContainsKey(unwatch.Counter))
                {
                    return; // a no-op
                }

                // Unsubscribe the ChartingActor from receiving more updates
                counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(unwatch.Counter, chartingActor));

                // Remove this series from the ChartingActor
                chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
            });
        }
    }
}