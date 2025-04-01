/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using System;

namespace Microsoft.Research.TouchMouseSensor
{
    /// <summary>
    /// The arguments passed by TouchMouseSensorEventManager when it receives a callback from the mouse.
    /// </summary>
    public class TouchMouseSensorEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="status">Status from mouse.</param>
        /// <param name="image">Image bytes.</param>
        internal TouchMouseSensorEventArgs(TOUCHMOUSESTATUS status, byte[] image)
        {
            // Copy the arguments.
            Status = status;
            Image = image;
        }

        /// <summary>
        /// The mouse status.
        /// </summary>
        public TOUCHMOUSESTATUS Status { get; private set; }

        /// <summary>
        /// The image bytes.
        /// </summary>
        public byte[] Image { get; private set; }
    }
}
