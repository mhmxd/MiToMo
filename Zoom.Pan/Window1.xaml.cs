/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Research.TouchMouseSensor;
using CommunityToolkit.HighPerformance;
using System.IO;
using System.Windows.Markup;
using System.Windows.Input;
using System.Text;
using CommunityToolkit.HighPerformance.Helpers;
using Tensorflow;
using System.Runtime.InteropServices;
using NumSharp;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Media;
using System.Resources;
using System.Xml.Linq;
using System.Reflection;

namespace Zoom.Pan
{

    public class DataPoint
    {
        [LoadColumn(0)] public float Sum { get; set; }
        [LoadColumn(1)] public float Mean { get; set; }
        [LoadColumn(2)] public float NzCount { get; set; }
        [LoadColumn(3)] public float Std { get; set; }
        [LoadColumn(4)] public bool Label { get; set; }

        public DataPoint(float[] features)
        {
            (Sum, Mean, NzCount, Std) = (features[0], features[1], features[2], features[3]);
        }
    }

    public class GesturePrediction
    {
        public bool PredictedLabel { get; set; }
    }

    public class TouchPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Value { get; set; }

        override public string ToString()
        {
            return string.Format("({0}, {1}): {2}", X, Y, Value);
        }
    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        // Constants
        private int GESTURE_TIME_WINDOW = 300; // ms
        private string DATA_FILE_DIR = "C:\\Users\\User\\Documents\\MIDE\\Data\\";
        //private int THRESHOLD = 10;

        private int baseThreshold = 50; // Base threshold, can be adjusted dynamically
        private int minClusterSize = 10; // Minimum size of a cluster to be considered a fingertip

        private TouchMouseCallback touchMouseCallback;
        private static long totalTime = 0;

        //List<Byte[,]> gestureShots = new List<byte[,]>();
        //List<float> gestureVector = new List<float>();
        byte[,] gestureShot;
        private List<(int row, int col, byte val)> touchPoints;
        //private List<(int row, int col)> tpCoM = new List<(int row, int col)>();
        private List<(int row, int col, byte val, long time)> thumbFrames;

        private List<(float[] Data, long TimeStamp)> gestureVector; 
        private StreamWriter squeezeWriter, idleWriter;

        private Stopwatch stopwatch = new Stopwatch();

        bool isRecording = false;
        int recordingType = -1;
        //bool isPrompted = false;

        private int nGestures = 0;

        private MLContext mlContext;
        private PredictionEngine<DataPoint, GesturePrediction> predictionEngine;
        private ITransformer squeezeModel;

        private bool gestureOn = false;

        private double prevFingerDist;
        private double pinchAmt;

        [DllImport("User32.dll")]
        static extern short GetAsyncKeyState(Int32 vKey);

        int VK_SPACE = 0x10;

        public Window1()
        {
            InitializeComponent();

            gestureVector = new List<(float[], long)>();
            touchPoints = new List<(int, int, byte)>();
            thumbFrames = new List<(int, int, byte, long)>();

            // Ensure the image rendering does not interpolate
            //RenderOptions.SetBitmapScalingMode(SensorImage, BitmapScalingMode.NearestNeighbor);

            // Initialise the mouse and register the callback function
            //touchMouseCallback = new TouchMouseCallback(TouchMouseCallbackFunction);
            //TouchMouseSensorInterop.RegisterTouchMouseCallback(touchMouseCallback);

            TouchMouseSensorEventManager.Handler += TouchMouseSensorHandler;

            //sqzWriter = new StreamWriter(DATA_FILE_DIR + "sqz-features.txt", append: true);
            //nrmWriter = new StreamWriter(DATA_FILE_DIR + "nrm-features.txt", append: true);

            TrainModel();
            SaveModel("sqz_model.zip");
            LoadModel("sqz_model.zip");
            this.KeyDown += Window1_KeyDown;
            //this.Closing += Window1_Closing;
            this.Deactivated += (s, e) => this.Activate();

            stopwatch.Start();

            
        }

        private void Window1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //sqzWriter.Close();
            //nrmWriter.Close();
            //Console.WriteLine(string.Join(" > ", thumbFrames.ToArray()));
            //string paddedFile = "C:\\Users\\User\\Documents\\MIDE\\Dev\\MiToMo\\gesture-pad.csv";
            //int frameSize = 195;

            //// Read all lines from the CSV file
            //string[] lines = File.ReadAllLines(DATA_FILE_PATH);
            //List<List<float>> sequences = new List<List<float>>();
            //int maxFrames = 0;

            //// Parse each line into a list of floats and determine the maximum number of frames
            //foreach (string line in lines)
            //{
            //    List<float> numbers = line.Split(',').Select(float.Parse).ToList();
            //    int numFrames = numbers.Count / frameSize;
            //    maxFrames = Math.Max(maxFrames, numFrames);
            //    sequences.Add(numbers);
            //}

            //// Pad each sequence to the maximum number of frames
            //for (int i = 0; i < sequences.Count; i++)
            //{
            //    int numFrames = sequences[i].Count / frameSize;

            //    // If the sequence is shorter than the maximum, pad it
            //    if (numFrames < maxFrames)
            //    {
            //        List<float> lastFrame = sequences[i].GetRange((numFrames - 1) * frameSize, frameSize);

            //        for (int j = numFrames; j < maxFrames; j++)
            //        {
            //            sequences[i].AddRange(lastFrame);
            //        }
            //    }
            //}

            //// Write the padded sequences to a new CSV file
            //using (StreamWriter sw = new StreamWriter(paddedFile))
            //{
            //    foreach (var sequence in sequences)
            //    {
            //        sw.WriteLine(string.Join(",", sequence));
            //    }
            //}

            //Console.WriteLine("Padding complete.");
        }

        private void TrainModel()
        {
            mlContext = new MLContext();

            // Load the data from your dataset
            Console.WriteLine("Loading the data...");
            IDataView dataView = mlContext.Data.LoadFromTextFile<DataPoint>(path: DATA_FILE_DIR + "sqz-dataset.csv", hasHeader: true, separatorChar: ',');

            // Shuffle the data
            var shuffledData = mlContext.Data.ShuffleRows(dataView);

            // Split the data into training and test sets
            var trainTestData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var trainingData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            // Preview the data to verify correct loading
            var preview = dataView.Preview();
            Console.WriteLine(preview);

            // Define and train the model
            Console.WriteLine("Define and train the model...");
            var trainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: "Label");
            var pipeline = mlContext.Transforms.Concatenate("Features", "Sum", "Mean", "NzCount", "Std").Append(trainer);

            squeezeModel = pipeline.Fit(trainingData);

            predictionEngine = mlContext.Model.CreatePredictionEngine<DataPoint, GesturePrediction>(squeezeModel);

            // Evaluate the model
            Console.WriteLine("Evaluate the model...");
            var predictions = squeezeModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions);

            Console.WriteLine($"Accuracy: {metrics.Accuracy}");
        }

        private void SaveModel(string modelPath)
        {
            mlContext.Model.Save(squeezeModel, null, modelPath);
            Console.WriteLine("Model saved to: " + modelPath);
        }

        private void LoadModel(string modelPath)
        {
            DataViewSchema modelSchema;
            squeezeModel = mlContext.Model.Load(modelPath, out modelSchema);
            predictionEngine = mlContext.Model.CreatePredictionEngine<DataPoint, GesturePrediction>(squeezeModel);
            Console.WriteLine("Model loaded from " + modelPath);
        }

        public bool PredictSqueeze(float[] featureValues)
        {
            if (predictionEngine == null) throw new InvalidOperationException("The model has not been loaded");
            
            var sampleData = new DataPoint(featureValues);

            var prediction = predictionEngine.Predict(sampleData);
            return prediction.PredictedLabel;
        }

        private float[] getSqueezeFeatures(byte[,] gestureShot)
        {
            var shotSpan = new Span2D<byte>(gestureShot);
            var sliceSpan = shotSpan.Slice(0, 0, 13, 15);
            byte[,] slice = sliceSpan.ToArray();
            Span<byte> sqzSlice = stackalloc byte[52];

            shotSpan.Slice(0, 13, 13, 2).CopyTo(sqzSlice);
            shotSpan.Slice(0, 0, 13, 2).CopyTo(sqzSlice);
            // Normalize to float
            List<float> sqzList = new List<float>();
            foreach (byte b in sqzSlice)
            {
                sqzList.Add(b / 255.0f);
            }

            // Extract features (sum, mean, num. non-zero, std)
            float[] sqzFeatures = new float[4];
            double mean = sqzList.Average();
            double variance = sqzList.Average(value => Math.Pow(value - mean, 2));
            double standardDeviation = Math.Sqrt(variance);

            sqzFeatures[0] = sqzList.Sum();
            sqzFeatures[1] = (float)mean;
            sqzFeatures[2] = sqzList.Count(value => value != 0.0f);
            sqzFeatures[3] = (float)standardDeviation;

            return sqzFeatures;
        }

        private void Window1_KeyDown(object sender, KeyEventArgs e)
        {
            if (gestureShot != null)
            {
                float[] sqzFeatures = getSqueezeFeatures(gestureShot);

                //Console.WriteLine(string.Join(", ", sqzFeatures.ToArray()));

                if (e.Key == Key.Space)
                {
                    Console.WriteLine(string.Join(", ", sqzFeatures.Select(f => f.ToString("F2"))) + ", 1");
                    // Working...
                    using (squeezeWriter = new StreamWriter(DATA_FILE_DIR + "squeeze-features.txt", append: true))
                    {
                        squeezeWriter.WriteLine(string.Join(", ", sqzFeatures.Select(f => f.ToString("F2"))) + ", 1");
                    }

                    // Predict
                    //Console.WriteLine("Squeeze? " + PredictSqueeze(sqzFeatures));

                    Console.WriteLine("Squeezing -------------------------");
                    //for (int i = 0; i < slice.GetLength(0); i++)
                    //{
                    //    for (int j = 0; j < slice.GetLength(1); j++)
                    //    {
                    //        Console.Write(slice[i, j] + "\t");
                    //    }
                    //    Console.WriteLine();
                    //}
                    //Console.WriteLine("-----------------------------------");
                }
                else if (e.Key == Key.Enter)
                {
                    //Console.WriteLine(string.Join(", ", sqzFeatures.Select(f => f.ToString("F2"))) + ", 0");
                    // Working...
                    using (idleWriter = new StreamWriter(DATA_FILE_DIR + "idle-features.txt", append: true))
                    {
                        idleWriter.WriteLine(string.Join(", ", sqzFeatures.Select(f => f.ToString("F2"))) + ", 0");
                    }

                    Console.WriteLine("Idle -------------------------");
                    //for (int i = 0; i < slice.GetLength(0); i++)
                    //{
                    //    for (int j = 0; j < slice.GetLength(1); j++)
                    //    {
                    //        Console.Write(slice[i, j] + "\t");
                    //    }
                    //    Console.WriteLine();
                    //}
                    //Console.WriteLine("---------------------------------");
                }
                else if (e.Key == Key.M)
                {
                    var mlContext = new MLContext();

                    // Load the data from your dataset
                    Console.WriteLine("Loading the data...");
                    IDataView dataView = mlContext.Data.LoadFromTextFile<DataPoint>(path: DATA_FILE_DIR + "sqz-dataset.csv", hasHeader: true, separatorChar: ',');

                    // Shuffle the data
                    var shuffledData = mlContext.Data.ShuffleRows(dataView);

                    // Split the data into training and test sets
                    var trainTestData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
                    var trainingData = trainTestData.TrainSet;
                    var testData = trainTestData.TestSet;

                    // Preview the data to verify correct loading
                    var preview = dataView.Preview();
                    Console.WriteLine(preview);

                    // Define and train the model
                    Console.WriteLine("Define and train the model...");
                    var trainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: "Label");
                    var pipeline = mlContext.Transforms.Concatenate("Features", "Sum", "Mean", "NzCount", "Std").Append(trainer);

                    var model = pipeline.Fit(trainingData);

                    // Evaluate the model
                    Console.WriteLine("Evaluate the model...");
                    var predictions = model.Transform(testData);
                    var metrics = mlContext.BinaryClassification.Evaluate(predictions);

                    Console.WriteLine($"Accuracy: {metrics.Accuracy}");
                }
            }
        }

        void TouchMouseCallbackFunction(ref TOUCHMOUSESTATUS pTouchMouseStatus, byte[] pabImage, int dwImageSize)
        {
            // If you had objects that tracked the state of the mouse you would examine 
            // pTouchMouseStatus->m_dwID here and either find the existing tracking 
            // objects or create new objects for it.

            // Sum of pixel values.
            int pixelSum = 0;

            // Sum of X values weighted by pixel values.
            int xSum = 0;

            // Sum of Y values weighted by pixel values.
            int ySum = 0;

            // Update the total time taken
            totalTime += pTouchMouseStatus.m_dwTimeDelta;

            // Iterate over rows.
            for (Int32 y = 0; y < pTouchMouseStatus.m_dwImageHeight; y++)
            {
                // Iterate over columns.
                for (Int32 x = 0; x < pTouchMouseStatus.m_dwImageWidth; x++)
                {
                    // Get the pixel value at current position.
                    int pixel = pabImage[pTouchMouseStatus.m_dwImageWidth * y + x];

                    Console.WriteLine("({0}, {1}) Pixel: {2}", x, y, pixel);

                    // Increment values.
                    pixelSum += pixel;
                    xSum += x * pixel;
                    ySum += y * pixel;
                }
            }

            if (pTouchMouseStatus.m_dwTimeDelta == 0)
            {
                // If the time delta is zero then there has been an 
                // undetermined delta since the last report.

                totalTime = 0;
                Console.WriteLine("----------------------------------");
            }

            if (pixelSum == 0)
            {
                // There are no lit pixels, so the mouse is not being touched 
                // and normally will fall idle.

                // pTouchMouseStatus->m_dwID is a 64-bit value, however the 
                // lower 16-bits are unique enough for display purposes.
                Console.WriteLine("Mouse #{0:X4}: T= {1,6}MS (Frame {2,3}MS): No touch, probably falling idle",
                    (pTouchMouseStatus.m_dwID & 0xFFFF),
                    totalTime,
                    pTouchMouseStatus.m_dwTimeDelta);
            }
            else
            {
                // Calculate and display the center of mass for the touches present.

                double xPos = (double)xSum / pixelSum;
                double yPos = (double)ySum / pixelSum;

                //    Console.WriteLine("Mouse #{0:X4}: T= {1,6}MS (Frame {2,3}MS): Center ({3:F2}, {4:F2})",
                //        (pTouchMouseStatus.m_dwID & 0xFFFF),
                //        totalTime,
                //        pTouchMouseStatus.m_dwTimeDelta,
                //        xPos,
                //        yPos);
                //}

                if (pTouchMouseStatus.m_fDisconnect)
                {
                    // The mouse is now disconnected, if we had created objects to track 
                    // the mouse they would be destroyed here.

                    Console.WriteLine("\nMouse #{0:X4}: Disconnected\n",
                        (pTouchMouseStatus.m_dwID & 0xFFFF));
                }
            }
        }

            /// <summary>
            /// Handle callback from mouse.  
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The event arguments.</param>
        void TouchMouseSensorHandler(object sender, TouchMouseSensorEventArgs e)
        {
            if (!stopwatch.IsRunning) stopwatch.Start();
            //Console.WriteLine("Sender: {0}; args: {1}, {2}", sender?.ToString(), 
            //    e.Status.m_dwTimeDelta, countNonZero(e.Image));
            Dispatcher.Invoke((Action<TouchMouseSensorEventArgs>) RecordGesture, e);
            //Dispatcher.Invoke((Action<TouchMouseSensorEventArgs>)SetSource, e);

            //if (!isPrompted)
            //{
            //    Console.WriteLine("Press 1 to record gesture, 2 to record idle");

            //    int pressedKey = Console.Read();
            //    if (pressedKey == 1)
            //    {
            //        recordingType = 1;
            //        if (!stopwatch.IsRunning) stopwatch.Start();

            //        Dispatcher.Invoke((Action<TouchMouseSensorEventArgs>)RecordGesture, e);
            //    }

            //    isPrompted = true;
            //}


            // We're in a thread belonging to the mouse, not the user interface 
            // thread. Need to dispatch to the user interface thread.
            //Console.WriteLine("Seder = {0}", sender.ToString());


        }

        private void printSpan(Span2D<Byte> span)
        {
            for (int i = 0; i < span.Height; i++)
            {
                for (int j = 0; j < span.Width; j++)
                {
                    Console.Write(span[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine("---------------------------------");
        }

        private int countNonZero(byte[] array)
        {
            int nonZeroCount = 0;

            foreach (byte b in array)
            {
                if (b != 0)
                {
                    Console.WriteLine($"{b}");
                    nonZeroCount++;
                }
            }

            return nonZeroCount;
        }

        void SetSource(TouchMouseSensorEventArgs e)
        {

            // Convert bitmap from memory to graphic form.
            BitmapSource source = 
                BitmapSource.Create(e.Status.m_dwImageWidth, e.Status.m_dwImageHeight, 
                255, 255,
                PixelFormats.Gray8, null, e.Image, e.Status.m_dwImageWidth);

            // Show bitmap in user interface.
            //SensorImage.Source = source;
        }

        void ShowImage(Byte[] img)
        {
            // Convert bitmap from memory to graphic form.
            BitmapSource source =
                BitmapSource.Create(15, 13,
                96, 96,
                PixelFormats.Gray8, null, img, 15);

            // Show bitmap in user interface.
            //SensorImage.Source = source;
        }

        void RecordGesture(TouchMouseSensorEventArgs e)
        {

            touchPoints.Clear();

            //long timeStamp = stopwatch.ElapsedMilliseconds;
            //gestureVector.Add((e.Image.Select(b => b / 255.0f).ToArray(), timeStamp));
            gestureShot = new byte[13, 15];
            var shotSpan = new Span2D<Byte>(gestureShot);

            // Populate the 2D array
            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    //gestureShot[i, j] = e.Image[i * 15 + j];
                    shotSpan[i, j] = e.Image[i * 15 + j];
                    //if (gestureShot[i, j] > 200) Console.WriteLine("Touchpoint {0}, {1}", i, j);
                }
            }

            //printSpan(shotSpan);

            //-- Detect Squeeze
            // Get features
            float[] sqzFeatures = getSqueezeFeatures(gestureShot);
            float[] off = new float[] { 0, 0, 0, 0 };

            // If ToMo is grabbed, check the features for Squeeze
            if (!sqzFeatures.SequenceEqual(off))
            {
                //Console.WriteLine(string.Join(",", sqzFeatures));
                //Test with the trained model
                if (PredictSqueeze(sqzFeatures))
                {
                    Console.WriteLine("Squeezed");
                    if (!gestureOn)
                    {
                        SoundPlayer audio = new SoundPlayer(Zoom.Pan.Properties.Resources.gesture_on);
                        audio.Play();
                        gestureOn = true;
                    }

                }
                else
                {
                    Console.WriteLine("Idle");
                    if (gestureOn)
                    {
                        SoundPlayer audio = new SoundPlayer(Zoom.Pan.Properties.Resources.gesture_off);
                        audio.Play();
                        gestureOn = false;
                    }
                }
            }
            else
            {
                if (gestureOn)
                {
                    SoundPlayer audio = new SoundPlayer(Zoom.Pan.Properties.Resources.gesture_off);
                    audio.Play();
                    gestureOn = false;
                }
            }


            //TrackFingers(gestureShot);

            //-- Distinguish the fingertips for pinch

            //-- Find max values of each column
            (byte val, int row)[] colMax = new (byte, int)[15]; // (value, row)
            int nFingers = 0;
            byte max;
            int maxInd;
            for (int c = 0; c < shotSpan.Width; c++)
            {
                byte[] colArray = shotSpan.GetColumn(c).ToArray();
                colMax[c] = (colArray.Max(), Array.IndexOf(colArray, colArray.Max()));
            }

            //Console.WriteLine(string.Join(", ", colMax));
            //-- Compare max values to find local maxima
            // First column
            if (colMax[0].val > colMax[1].val)
            {
                touchPoints.Add((colMax[0].row, 0, colMax[0].val));
                nFingers++;
            }

            // Rest of columns
            for (int col = 1; col < colMax.Length - 1; col++)
            {
                //Console.WriteLine("{0} | {1} | {2}", colMax[col - 1], colMax[col], colMax[col + 1]);
                // Local maximum
                if (colMax[col].val > colMax[col - 1].val && colMax[col].val >= colMax[col + 1].val)
                {
                    touchPoints.Add((colMax[col].row, col, colMax[col].val));
                    nFingers++;
                }
            }

            // Last column
            if (colMax[14].val > colMax[13].val)
            {
                touchPoints.Add((colMax[14].row, 14, colMax[14].val));
                nFingers++;
            }

            // Calculate the center of the mass for the touch points
            List<(double x, double y)> tpCoM = new List<(double, double)>();
            for (int p = 0; p < touchPoints.Count; p++)
            {
                var (row, col, val) = touchPoints[p];
                List<(int row, int col, byte val)> cells = new List<(int row, int col, byte val)>();
                if (row > 0)
                {
                    cells.Add((row - 1, col, shotSpan[row - 1, col]));
                    if (col > 0) cells.Add((row - 1, col - 1, shotSpan[row - 1, col - 1]));
                    if (col < 14) cells.Add((row - 1, col + 1, shotSpan[row - 1, col + 1]));
                } 
                
                if (col > 0)
                {
                    cells.Add((row, col - 1, shotSpan[row, col - 1]));
                }

                if (col < 14)
                {
                    cells.Add((row, col + 1, shotSpan[row, col + 1]));
                }
                
                if (row < 12)
                {
                    cells.Add((row + 1, col, shotSpan[row + 1, col]));
                    if (col > 0) cells.Add((row + 1, col - 1, shotSpan[row + 1, col - 1]));
                    if (col < 14) cells.Add((row + 1, col + 1, shotSpan[row + 1, col + 1]));
                }

                cells.Add((row, col, shotSpan[row, col]));

                int totalMass = 0;
                int sumXWeight = 0;
                int sumYWeight = 0;
                foreach ((int r, int c, byte v) in cells)
                {
                    totalMass += v;
                    sumXWeight += v * c;
                    sumYWeight += v * r;
                }

                
                tpCoM.Add(((double)sumXWeight / totalMass, (double)sumYWeight / totalMass));

                //Console.WriteLine($"Center of Mass: ({centerOfMassX:F2}, {centerOfMassY:F2})");

            }

            // Track pinch
            //Console.WriteLine("Number of Touchpoints = {0}, {1}", nFingers, touchPoints.Count);
            if (touchPoints.Count == 5)
            {
                double dX = tpCoM[0].x - tpCoM[1].x;
                double dY = tpCoM[0].y - tpCoM[1].y;
                double fingersDist = Math.Sqrt(dX * dX + dY * dY);
                Console.WriteLine("Prev|Now: {0:F2}|{1:F2}", prevFingerDist, fingersDist);
                if (prevFingerDist == 0) prevFingerDist = fingersDist;
                else
                {
                    pinchAmt = fingersDist - prevFingerDist;
                    prevFingerDist = fingersDist;
                    Console.WriteLine($"pinchAmt: {pinchAmt:F2}");
                    if (Math.Abs(pinchAmt) > 0.01)
                    {

                        double zoomAmt = pinchAmt * 10;
                        MyCircle.Height += zoomAmt;
                        MyCircle.Width += zoomAmt;

                    }
                }
            }



                //    //Console.WriteLine("----------------------------------------------------");
                //}
                //else
                //{
                //    // Reset fingerDist when fingers are lifted off
                //    prevFingerDist = 0;
                //}

                // Track thumb (if touching, should be the first in touchPoints)
                //if (touchPoints.Count > 0)
                //{
                //    if (thumbFrames.Count > 0)
                //    {
                //        //if (touchPoints.First().col == 1) Console.WriteLine("Thumb moved");
                //        if (touchPoints.First().col > 2) // Thumb lifted off => reset
                //        {
                //            //Console.WriteLine(string.Join(" > ", thumbFrames.ToArray()));
                //            //Console.WriteLine("=============================================================");
                //            thumbFrames.Clear();
                //        } 
                //        else if (thumbFrames.Last().col == touchPoints.First().col - 1)
                //        {
                //            var (r, c, v) = touchPoints.First();
                //            thumbFrames.Add((r, c, v, stopwatch.ElapsedMilliseconds));
                //        }
                //    } 
                //    else 
                //    {
                //        if (touchPoints.First().col == 0) // First thumb touch
                //        {
                //            var (r, c, v) = touchPoints.First();
                //            thumbFrames.Add((r, c, v, stopwatch.ElapsedMilliseconds));
                //        }
                //    }

                //} else // No fingers
                //{

                //}


                //Console.WriteLine(string.Join(" > ", thumbFrames.ToArray()));
                //Console.WriteLine("--------------------------------------------------");

                // Add the values as a vector
                //gestureVector.AddRange(e.Image.Select(b => b / 255.0f));

                //gestureShots.Add(gestureShot);
                //Console.WriteLine(stopwatch.ElapsedMilliseconds);
                //if (stopwatch.ElapsedMilliseconds >= GESTURE_TIME_WINDOW)
                //{
                //    isRecording = false;
                //    Console.WriteLine("Waiting for the key...");
                //    while (true)
                //    {
                //        // If SPACE => save the vector (gesture). Otherwise, get the next vector.
                //        int pressedKey = Console.Read();
                //        if (pressedKey != -1) Console.WriteLine("Key: {0}", pressedKey);
                //        if (pressedKey == ((char)Key.Space))
                //        {
                //            //Console.WriteLine("Writing {0} numbers to file", gestureVector.Count / 195);
                //            // Write the list as a single line, separated by commas
                //            seqWriter.WriteLine(string.Join(",", gestureVector));

                //            foreach (var b in gestureVector) Console.Write(b + " , ");
                //            Console.WriteLine();
                //            Console.WriteLine("------------------------------");

                //            isRecording = true;
                //            gestureVector.Clear();
                //            //gestureShots.Clear();
                //            stopwatch.Restart();

                //            break;

                //        } else if (pressedKey == ((char)Key.Escape))
                //        {
                //            isRecording = true;
                //            gestureVector.Clear();
                //            //gestureShots.Clear();
                //            stopwatch.Restart();
                //            break;
                //        }
                //    }


                //    //Console.WriteLine("Number of shots = {0}", gestureShots.Count);
                //    //List<Tuple<int, int, byte>> pointerCoords = new List<Tuple<int, int, byte>>();

                //    //// Multiply the max value from column 0 to 3
                //    ////bool swiped = true;
                //    //foreach (var shot in gestureShots)
                //    //{
                //    //    var shotSpan = new Span2D<Byte>(shot);

                //    //    for (int col = 1; col < 4; col++)
                //    //    {
                //    //        var array = shotSpan.GetColumn(col).ToArray();
                //    //        var max = array.Max();
                //    //        if (max > 210) pointerCoords.Add(Tuple.Create(Array.IndexOf(array, max), col, max)); 
                //    //        //Console.Wri teLine(shotSpan.GetColumn(col).ToArray().Max());
                //    //        //swiped = swiped & (shotSpan.GetColumn(col).ToArray().Max() > 200);
                //    //    }
                //    //}



                //    //foreach (var point in pointerCoords) Console.Write(point + " | "); 

                //}

                //Console.WriteLine("---------------------------");

                //Console.WriteLine("dT = {0}", e.Status.m_dwTimeDelta);

                //if (e.Status.m_dwTimeDelta != 0)
                //{
                //    stopwatch.Start();
                //    gestureShots.Add(gestureShot);
                //} else
                //{
                //    // One gesture is finished (dT of the mouse is zeroed) => process
                //    stopwatch.Stop();
                //    Console.WriteLine("dT = {0}; Num of shots = {1}", stopwatch.Elapsed.TotalSeconds, gestureShots.Count);
                //    stopwatch.Reset();
                //    gestureShots.Clear(); 
                //}


                // Show data of the first row
                //Console.Write("First row = ");
                //for (int j = 0; j < 15; j++)
                //{
                //    Console.Write("{0} | ", gestureArray[0, j]);
                //}
                //Console.WriteLine();



            }

        private void TrackFingers(byte[,] touchArray)
        {
            bool[,] visited = new bool[13, 15];
            List<Point> fingertips = new List<Point>();

            // Calculate a dynamic threshold based on the touchArray values
            int dynamicThreshold = CalculateDynamicThreshold(touchArray);

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    if (!visited[i, j] && touchArray[i, j] > dynamicThreshold)
                    {
                        List<TouchPoint> cluster = new List<TouchPoint>();
                        FloodFill(touchArray, i, j, visited, cluster, dynamicThreshold);

                        // Only consider clusters that are large enough
                        if (cluster.Count >= minClusterSize)
                        {
                            var centroid = CalculateCentroid(cluster);
                            fingertips.Add(centroid);
                        }

                    }
                }
            }

            Console.WriteLine("Number of fingers = {0}", fingertips.Count);
            
        }

        private void FloodFill(byte[,] touchArray, int x, int y, bool[,] visited, List<TouchPoint> cluster, int threshold)
        {
            if (x < 0 || x >= 13 || y < 0 || y >= 15 || visited[x, y] || touchArray[x, y] <= threshold)
                return;

            visited[x, y] = true;
            cluster.Add(new TouchPoint { X = x, Y = y, Value = touchArray[x, y] });

            // Visit 8 neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 || dy != 0)
                        FloodFill(touchArray, x + dx, y + dy, visited, cluster, threshold);
                }
            }
        }

        private Point CalculateCentroid(List<TouchPoint> cluster)
        {
            int sumX = 0, sumY = 0, sumValue = 0;

            foreach (var point in cluster)
            {
                sumX += point.X * point.Value;
                sumY += point.Y * point.Value;
                sumValue += point.Value;
            }

            int centroidX = sumX / sumValue;
            int centroidY = sumY / sumValue;

            return new Point(centroidX, centroidY);
        }

        private int CalculateDynamicThreshold(byte[,] array)
        {
            int maxValue = 0;

            // Find the maximum value in the array
            foreach (var value in array)
            {
                if (value > maxValue)
                {
                    maxValue = value;
                }
            }

            // Set the dynamic threshold as a percentage of the maximum value
            return Math.Max(baseThreshold, maxValue / 4); // Example: 25% of maxValue
        }

        private void SaveGestureDataToFile()
        {
            // Filter the gestureVector to include only events within the last GESTURE_TIME_WINDOW
            long currentTime = stopwatch.ElapsedMilliseconds;
            var filteredGestureData = gestureVector
                .Where(data => currentTime - data.TimeStamp <= GESTURE_TIME_WINDOW)
                .Select(data => data.Data)
                .ToList();

            // Write the filtered data to the CSV file
            //using (StreamWriter writer = new StreamWriter(DATA_FILE_PATH, append: true))
            //{
            //    StringBuilder dataSeq = new StringBuilder();
            //    Console.WriteLine("Gesture #{0} ({1} frames)", ++nGestures, filteredGestureData.Count);
                
            //    foreach (var gestureData in filteredGestureData)
            //    {
            //        //Console.WriteLine("Num of data per frame = {0}", gestureData.Length);
            //        dataSeq.Append(string.Join(",", gestureData)).Append(",");
            //    }

            //    //Console.WriteLine(dataSeq);
            //    dataSeq.Remove(dataSeq.Length - 1, 1);
            //    writer.WriteLine(dataSeq);

            //}

            // Clear the gesture vector after saving to file
            gestureVector.Clear();
        }


        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            // Do something with the event...
        }

    }
}
