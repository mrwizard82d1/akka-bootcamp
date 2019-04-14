using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor that validates user input and signals result to others.
    /// </summary>
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // Signal that the user needs to supply an input
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                if (IsValid(msg))
                {
                    // Send success to console writer.
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    // Signal that input was bad
                    _consoleWriterActor.Tell(
                        new Messages.ValidationError("Invalid: input had odd number of characters."));
                }
            }
            
            // Tell sender to continue doing its thing (whatever that may be, this actor doesn't care).
            Sender.Tell(new Messages.ContinueProcessing());
            
        }

        /// <summary>
        /// Determines if the message received is valid.
        /// Currently, arbitrarily checks if number of characters in message is even.
        /// </summary>
        /// <param name="msg">The message to be tested.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        private bool IsValid(string msg)
        {
            var result = msg.Length % 2 == 0;
            return result;
        }
    }
}