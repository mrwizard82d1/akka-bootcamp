using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main()
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // Initialize the top-level actors within the actor system.
            var consoleWriterProps = Props.Create<ConsoleWriterActor>();
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            // Make the tail coordinator actor.
            var tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
            MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor"); // created via side-effect

            // Pass the tail coordinator actor to fileValidatorActorProps (just adding on extra argument)
            var fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor));
            MyActorSystem.ActorOf(fileValidatorActorProps, "validationActor"); // created via side-effect

            var consoleReaderProps = Props.Create<ConsoleReaderActor>();
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");
            
            // Begin processing.
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
