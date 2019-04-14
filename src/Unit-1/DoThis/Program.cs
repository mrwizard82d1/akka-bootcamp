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

            // Initialize the actors using separate Props.
            var consoleWriterProps = Props.Create<ConsoleWriterActor>(); // **NOT** recommended
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            // Make the tail coordinator actor.
            var tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
            var tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

            // Pass the tail coordinator actor to fileValidatorActorProps (just adding on extra argument)
            var fileValidatorActorProps =
                Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoordinatorActor));
            var fileValidatorActor = MyActorSystem.ActorOf(fileValidatorActorProps, "fileValidatorActor");

            var consoleReaderProps = Props.Create<ConsoleReaderActor>(fileValidatorActor);
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");
            
            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
