using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    /// <inheritdoc />
    /// <summary>
    /// Starts and stops tailing files at user-specified paths.
    /// </summary>
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Start tailing the file at the user-specified path reporting changes to another actor.
        /// </summary>
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }

            public string FilePath { get; }
            
            public IActorRef ReporterActor { get; }
        }

        /// <summary>
        /// Stop tailing the file at the user-specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }
            
            public string FilePath { get; }
        }
        
        #endregion
        
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartTail st:
                    Context.ActorOf(Props.Create(() => new TailActor(st.ReporterActor, st.FilePath)),
                        $"TailActor-${Path.GetFileName(st.FilePath)}");
                    break;
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maximum number of retries
                TimeSpan.FromSeconds(30), // within an interval 
                x => // local only decider
                {
                    switch (x)
                    {
                        case ArithmeticException _:
                            // Perhaps we consider an ArithmeticException to not be application critical
                            // so we simply ignore the error and keep going.
                            return Directive.Resume;
                        case NotSupportedException _:
                            // An error from which we cannot recover so we stop the failing actor.
                            return Directive.Stop;
                        default:
                            // In all other cases, simply restart the failing actor.
                            return Directive.Restart;
                    }
                }); 
        }
    }
}