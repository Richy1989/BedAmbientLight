﻿using System;
using System.Text;

namespace BedLightESP.Touch
{
    internal interface ITouchManager
    {
        // Event handler for button press events
        event ButtonPressedEventHandler ButtonPressed;
    }
}