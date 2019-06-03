using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Manages button toggles (on the UI thread).
    /// </summary>
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Toggles the button on and off and sends the appropriate message to
        /// <see cref="PerformanceCounterCoordinatorActor"/>.
        /// </summary>
        public class Toggle { }

        #endregion

        private readonly CounterType _myCounterType;
        private bool _isToggledOn;
        private readonly Button _myButton;
        private readonly IActorRef _coordinatorActor;

        public ButtonToggleActor(IActorRef coordinatorActor, Button myButton, CounterType myCounterType, 
                                 bool isToggledOn = false)
        {
            _coordinatorActor = coordinatorActor;
            _myButton = myButton;
            _isToggledOn = isToggledOn;
            _myCounterType = myCounterType;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && _isToggledOn)
            {
                // Toggle currently on so...
                
                // Stop watching this counter
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(_myCounterType));

                FlipToggle();
            }
            else if (message is Toggle && !_isToggledOn)
            {
                // Toggle is currently off so...
                
                // Start watching this counter
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(_myCounterType));

                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            // Flip the toggle
            _isToggledOn = !_isToggledOn;
            
            // Change the text of the button
            _myButton.Text = $@"{_myCounterType.ToString().ToUpperInvariant()} ({(_isToggledOn ? "ON" : "OFF")})";
        }
    }
}