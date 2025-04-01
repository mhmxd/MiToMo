/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

namespace Microsoft.Research.TouchMouseSensor
{
    /// <summary>
    /// Wrapper for single callback function from native code into event driven 
    /// managed world.
    /// </summary>
    public static class TouchMouseSensorEventManager
    {
        /// <summary>
        /// Static reference to callback delegate.
        /// </summary>
        /// <remarks>
        /// A static reference needs to be held to prevent it being garbage 
        /// collected.
        /// </remarks>
        static TouchMouseCallback callback = Callback;

        /// <summary>
        /// The actual event handler.
        /// </summary>
        static event TouchMouseSensorHandler handler;

        /// <summary>
        /// Access to the event handler.
        /// </summary>
        public static event TouchMouseSensorHandler Handler
        {
            add
            {
                if (handler == null)
                {
                    // First handler is being added, so register for the callback.
                    TouchMouseSensorInterop.RegisterTouchMouseCallback(callback);
                }

                handler += value;
            }
            remove
            {
                handler -= value;

                if (handler == null)
                {
                    // Last handler has been removed, so unregister for the callback.
                    TouchMouseSensorInterop.UnregisterTouchMouseCallback();
                }
            }
        }

        /// <summary>
        /// The callback handler.
        /// </summary>
        /// <param name="status">Status.</param>
        /// <param name="image">Image bytes.</param>
        /// <param name="imageSize">Size of image bytes.</param>
        static void Callback(ref TOUCHMOUSESTATUS status, 
            byte[] image, 
            int imageSize)
        {
            if (handler != null)
            {
                // We have one or more handlers present, so create arguments and call.
                var e = new TouchMouseSensorEventArgs(status, image);
                handler(null, e);
            }
        }
    }
}
