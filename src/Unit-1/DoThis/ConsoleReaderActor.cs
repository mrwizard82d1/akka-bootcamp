using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";
        
        private readonly IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            else if (message is Messages.InputError)
            {
                _consoleWriterActor.Tell(message as Messages.InputError);
            }

            GetAndValidateInput();
        }

        #region Internal methods
        
        /// <summary>
        /// Tell the user what to do.
        /// </summary>
        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }

        /// <summary>
        /// Reads input from console, validates it, then signals appropriate response
        /// (continue processing, error, success, etc.).
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();
            if (string.IsNullOrEmpty(message))
            {
                // Signal the user needs to supply an input, as previously received  input was blank
                Self.Tell(new Messages.NullInputError("No input received."));
            }
            else if (string.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // Shutdown the entire actor system (allows process to exit)
                //
                // This method returns a `Task` but we **do not** wait for it to complete.
                Context.System.Terminate();
            }
            else
            {
                if (IsValid(message))
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                    
                    // Continue reading messages from the console
                    Self.Tell(new Messages.ContinueProcessing());
                }
                else
                {
                    Self.Tell(new Messages.ValidationError("Invalid: input had odd number of characters."));
                }
            } 
        }

        /// <summary>
        /// Validates <paramref name="message"/>.
        /// Currently says messages are valid if they have an even number of characters.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>True if <paramref name="message"/> is valid; otherwise, false.</returns>
        private bool IsValid(string message)
        {
            var result = message.Length % 2 == 0;
            return result;    
        }

        #endregion

    }
}