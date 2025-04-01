/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Research.TouchMouseSensor
{
    /// <summary>
    /// Mouse event structure detailing the state of the mouse.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOUCHMOUSESTATUS
    {
        /// <summary>
        /// Unique identifier for mouse. If more than one mouse is present they 
        /// will each report a different identifier. When a mouse disconnects 
        /// its identifier may be reused when another connection is made.
        /// </summary>
        public Int64 m_dwID;

        /// <summary>
        /// Mouse disconnected indicator. Normally false, the last report from 
        /// a mouse connection will set this true.
        /// </summary>
        public bool m_fDisconnect;

        /// <summary>
        /// The elapsed time in milliseconds since the previous report. This 
        /// time is derived from a clock on the mouse itself and should be 
        /// considered more accurate than any other source of timing for the
        /// report.
        /// </summary>
        /// <remarks>
        /// The elapsed time is only good for short time spans. The delta is 
        /// reported as zero if more than about 100ms of time has elapsed.
        /// </remarks>
        public Int32 m_dwTimeDelta;

        /// <summary>
        /// Width of the image.
        /// </summary>
        /// <remarks>
        /// Current mice all report 15. If pabImage contains an image, 
        /// pabImage[0] is the top-left pixel, pabImage[1] the pixel to the 
        /// right of pabImage[0] and pabImage[m_dwImageWidth] the pixel below
        /// the pabImage[0].
        /// </remarks>
        public Int32 m_dwImageWidth;

        /// <summary>
        /// Height of the image.
        /// </summary>
        /// <remarks>
        /// Current mice all report 13.
        /// </remarks>
        public Int32 m_dwImageHeight;
    }

    /// <summary>
    /// Type of callback delegates from DLL.
    /// </summary>
    /// <param name="pTouchMouseStatus">The status.</param>
    /// <param name="pabImage">The image.</param>
    /// <param name="dwImageSize">The image size.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TouchMouseCallback(
        [In] ref TOUCHMOUSESTATUS pTouchMouseStatus,
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pabImage,
        [In] Int32 dwImageSize
    );

    /// <summary>
    /// The interop class for the mouse.
    /// </summary>
    public class TouchMouseSensorInterop
    {
        /// <summary>
        /// The name of the TouchMouseSensor DLL.
        /// </summary>
        const string TouchMouseSensorDll = "TouchMouseSensor.dll";

        /// <summary>
        /// Sets the callback function to be used.
        /// </summary>
        /// <param name="callback">The callback.</param>
        [DllImport(TouchMouseSensorDll)]
        public static extern void RegisterTouchMouseCallback(TouchMouseCallback callback);

        /// <summary>
        /// Resets the callback function.
        /// </summary>
        /// <remarks>
        /// Calls to the previous function are not guaranteed to stop immediately.
        ///  </remarks>
        [DllImport(TouchMouseSensorDll)]
        public static extern void UnregisterTouchMouseCallback();
    }
}
