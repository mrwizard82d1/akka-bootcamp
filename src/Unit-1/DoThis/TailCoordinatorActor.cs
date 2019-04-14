using Akka.Actor;

namespace WinTail
{
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
                    // Here we create our first parent/child relationship!
                    // The `TailActor` instance created here is a child of
                    // this instance of a `TailCoordinatorActor`.
                    Context.ActorOf(Props.Create(() => new TailActor(st.ReporterActor, st.FilePath)),
                        $"TailActor-${st.FilePath}");
                    break;
            }
        }
    }
}