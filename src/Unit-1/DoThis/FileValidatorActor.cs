using System.IO;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor that validates user input and signals result to others.
    /// </summary>
    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;
        private readonly IActorRef _tailCoordinatorActor;

        public FileValidatorActor(IActorRef consoleWriterActor, IActorRef tailCoordinatorActor)
        {
            _consoleWriterActor = consoleWriterActor;
            _tailCoordinatorActor = tailCoordinatorActor;
        }
        
        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
               // Signal that the user needs to supply an input.
               _consoleWriterActor.Tell(new Messages.NullInputError("Input was blank. Please try again.\n"));
               
               // Tell Sender to continue doing its thing (whatever that may be; this actor does not care).
               Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                if (IsFileUri(msg))
                {
                    // Signal successful input
                    _consoleWriterActor.Tell(new Messages.InputSuccess($"Starting processing for {msg}."));
                    
                    // Start coordinator
                    _tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
                }
                else
                {
                    // Signal that input was bad.
                    _consoleWriterActor.Tell(new Messages.ValidationError($"{msg} is not an existing URI on disk."));
                    
                    // Tell Sender to continue doing its thing (whatever that may be; this actor does not care).
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }
        }

        /// <summary>
        /// Checks if file exists at path provided by user.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>True if exists; otherwise, false.</returns>
        private bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}