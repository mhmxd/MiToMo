/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

namespace Microsoft.Research.TouchMouseSensor
{
    /// <summary>
    /// The delagate type used for mouse callback events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void TouchMouseSensorHandler(object sender, TouchMouseSensorEventArgs e);
}
