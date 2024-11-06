using System;

namespace BedLightESP.Touch
{
    /// <summary>
    /// Interface for managing touch functionality.
    /// </summary>
    internal interface ITouchManager : IDisposable
    {
        /// <summary>
        /// Initializes the touch manager.
        /// </summary>
        void Initialize();
    }
}
