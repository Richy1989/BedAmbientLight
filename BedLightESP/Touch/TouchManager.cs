﻿using System;
using System.Device.Gpio;
using System.Threading;
using BedLightESP.Enumerations;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.Settings;

namespace BedLightESP.Touch
{
    /// <summary>
    /// Button Pressed Event Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    //internal delegate void ButtonPressedEventHandler(object sender, ButtonPressEventArgs e);

    /// <summary>
    /// Represents a touch manager that handles touch events for buttons.
    /// </summary>
    /// <remarks>
    /// This class manages the touch events for left and right buttons using GPIO pins.
    /// It handles single and double touch events, including continuous touches.
    /// </remarks>
    internal class TouchManager : ITouchManager
    {
        // Event handler for button press events
        //public event ButtonPressedEventHandler ButtonPressed;

        private readonly int ButtonLeftPin; // GPIO pin number for the touch sensor / default 34 
        private readonly int ButtonRightPin; // GPIO pin number for the touch sensor / default 35

        private const int DebounceDelay = 30;     // Debounce time
        private const int SingleTouchDelay = 300; // Maximum delay for a single touch
        private const int DoubleTouchDelay = 600; // Maximum delay for a single touch

        private readonly GpioController _gpioController;
        private readonly ISettingsManager _settingsManager;
        private readonly IMessageService _messageService;

        private int _touchCount = 0;
        private DateTime _lastTouchTime;
        private bool _isInCheck = false;
        private ButtonPosition _currentClick = ButtonPosition.None;


        public TouchManager(GpioController gpioController, ISettingsManager settingsManager, IMessageService messageService)
        {
            this._gpioController = gpioController;
            this._settingsManager = settingsManager;
            this._messageService = messageService;

            // Initialize pins for left and right buttons
            ButtonLeftPin = _settingsManager.Settings.LeftSidePin;
            ButtonRightPin = _settingsManager.Settings.RightSidePin;

            // Initialize the GPIO pin
            _gpioController.OpenPin(ButtonLeftPin, PinMode.Input);
            _gpioController.OpenPin(ButtonRightPin, PinMode.Input);

            // Register event handlers for pin value changes
            _gpioController.RegisterCallbackForPinValueChangedEvent(ButtonLeftPin, PinEventTypes.Rising, ButtonLeftPressed);
            _gpioController.RegisterCallbackForPinValueChangedEvent(ButtonRightPin, PinEventTypes.Rising, ButtonRightPressed);
            _messageService = messageService;
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
                //ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.SingleHold, DateTime.UtcNow));
                FireTouchMessage(positionOfLongClick, ClickType.SingleHold, DateTime.UtcNow);
                Thread.Sleep(500);
            }

            if (!isContinuous)
            {
                Logger.Info($"Single Touch Detected: {positionOfLongClick}");
                //ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.Single, DateTime.UtcNow));
                FireTouchMessage(positionOfLongClick, ClickType.Single, DateTime.UtcNow);
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
            //ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.Double, DateTime.UtcNow));
            FireTouchMessage(positionOfLongClick, ClickType.Double, DateTime.UtcNow);

            while (_gpioController.Read(pinNumber) == PinValue.High)
            {
                if ((DateTime.UtcNow - _lastTouchTime).TotalMilliseconds > SingleTouchDelay + DoubleTouchDelay)
                {
                    // Fire an event every 300ms as long as the button is pressed
                    Logger.Info($"Continuous double touch detected: {positionOfLongClick}");
                    //ButtonPressed?.Invoke(this, new ButtonPressEventArgs(positionOfLongClick, ClickType.DoubleHold, DateTime.UtcNow));
                    FireTouchMessage(positionOfLongClick, ClickType.DoubleHold, DateTime.UtcNow);
                    Thread.Sleep(500);
                }
                Thread.Sleep(1);
            }
        }

        private void FireTouchMessage(ButtonPosition buttonPosition, ClickType type, DateTime dateTime)
        {
            Logger.Debug($"Firing Touch Message. Position: {buttonPosition}, Type: {type}, Time: {dateTime}");
            _messageService.SendMessage(new TouchMessage(buttonPosition, type, dateTime));
        }
    }
}
