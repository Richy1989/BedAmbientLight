using System;
using System.Device.Gpio;
using System.Threading;
using BedLightESP.Enumerations;
using BedLightESP.EventArgsHelper;
using BedLightESP.Logging;

namespace BedLightESP.Manager
{
    /// <summary>
    /// Button Pressed Event Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ButtonPressedEventHandler(object sender, ButtonPressEventArgs e);

    /// <summary>
    /// Represents a touch manager that handles touch events for buttons.
    /// </summary>
    internal class TouchManager
    {
        // Event to be raised when a button is pressed
        public event ButtonPressedEventHandler ButtonPressed;

        private const int ButtonLeftPin = 34; // GPIO pin number for the touch sensor
        private const int ButtonRightPin = 35; // GPIO pin number for the touch sensor
                                               // Constants for timing thresholds (in milliseconds)
        private const int DebounceDelay = 30;     // Debounce time
        private const int SingleTouchDelay = 300; // Maximum delay for a single touch
        private const int DoubleTouchDelay = 600; // Maximum delay for a single touch

        private readonly GpioController _gpioController;
        private int _touchCount = 0;
        private DateTime _lastTouchTime;
        private bool _isInCheck = false;
        private ButtonPosition _currentClick = ButtonPosition.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchManager"/> class.
        /// </summary>
        /// <param name="_gpioController">The GPIO controller.</param>
        public TouchManager(GpioController _gpioController)
        {
            this._gpioController = _gpioController;
            
            // Initialize the GPIO pin
            _gpioController.OpenPin(ButtonLeftPin, PinMode.Input);
            _gpioController.OpenPin(ButtonRightPin, PinMode.Input);

            // Register event handlers for pin value changes
            _gpioController.RegisterCallbackForPinValueChangedEvent(ButtonLeftPin, PinEventTypes.Rising, ButtonLeftPressed);
            _gpioController.RegisterCallbackForPinValueChangedEvent(ButtonRightPin, PinEventTypes.Rising, ButtonRightPressed);
        }

        /// <summary>
        /// Handles the button press event when the left button is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="pinValueChangedEventArgs">The event arguments.</param>
        private void ButtonLeftPressed(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (_currentClick == ButtonPosition.None || _currentClick == ButtonPosition.Left)
            {
                _currentClick = ButtonPosition.Left;
                TouchDetected(pinValueChangedEventArgs);
            }
        }

        /// <summary>
        /// Handles the button press event when the right button is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="pinValueChangedEventArgs">The event arguments.</param>
        private void ButtonRightPressed(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (_currentClick == ButtonPosition.None || _currentClick == ButtonPosition.Right)
            {
                _currentClick = ButtonPosition.Right;
                TouchDetected(pinValueChangedEventArgs);
            }
        }

        /// <summary>
        /// Handles the touch event when a touch is detected.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="pinValueChangedEventArgs">The event arguments.</param>
        private void TouchDetected(PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            DateTime now = DateTime.UtcNow;

            // Handle debounce: Ignore touches that are too close together
            if ((now - _lastTouchTime).TotalMilliseconds < DebounceDelay)
            {
                return; // Ignore this touch event (it's likely noise)
            }

            // Increase Touch Count
            _touchCount++;

            // Update the last touch time
            _lastTouchTime = now;

            // Process the touch event after a short delay to determine type
            new Thread(() =>
            {
                if (!_isInCheck)
                {
                    _isInCheck = true;
                    Thread.Sleep(SingleTouchDelay); // Wait to check if another touch follows
                    DetermineTouchType(pinValueChangedEventArgs.PinNumber);
                }
            }).Start();
        }

        /// <summary>
        /// Determines the type of touch based on the touch count.
        /// </summary>
        private void DetermineTouchType(int pinNumber)
        {
            if (_touchCount == 1)
            {
                // Handle single touch
                HandleSingleTouch(pinNumber);
            }
            else if (_touchCount == 2)
            {
                // Handle double touch
                HandleDoubleTouch(pinNumber);
            }

            // Reset touch count after processing
            _touchCount = 0;
            _currentClick = ButtonPosition.None;
            _isInCheck = false;
        }

        /// <summary>
        /// Handles a single touch event and determines if it is a continuous touch.
        /// </summary>
        /// <param name="pinNumber">The GPIO pin number where the touch event was detected.</param>
        private void HandleSingleTouch(int pinNumber)
        {
            bool isContinuous = false;
            var positionOfLongClick = _currentClick;
            while (_gpioController.Read(pinNumber) == PinValue.High)
            {
                isContinuous = true;
                Logger.Info($"Continuous Single Touch Detected: {positionOfLongClick}");
                ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.SingleHold, DateTime.UtcNow));
                Thread.Sleep(500);
            }

            if (!isContinuous)
            {
                Logger.Info($"Single Touch Detected: {positionOfLongClick}");
                ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.Single, DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Handles the continuous button press event when the button is held down.
        /// </summary>
        /// <param name="pinNumber">The GPIO pin number.</param>
        private void HandleDoubleTouch(int pinNumber)
        {
            var positionOfLongClick = _currentClick;

            // Double touch detected
            Logger.Info($"Double touch detected: {positionOfLongClick}");
            ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.Double, DateTime.UtcNow));

            while (_gpioController.Read(pinNumber) == PinValue.High)
            {
                if ((DateTime.UtcNow - _lastTouchTime).TotalMilliseconds > SingleTouchDelay + DoubleTouchDelay)
                {
                    // Fire an event every 300ms as long as the button is pressed
                    Logger.Info($"Continuous double touch detected: {positionOfLongClick}");
                    ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.DoubleHold, DateTime.UtcNow));
                    Thread.Sleep(500);
                }
                Thread.Sleep(1);
            }
        }
    }
}
