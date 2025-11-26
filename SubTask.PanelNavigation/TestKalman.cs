using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Interop;

namespace SubTask.PanelNavigation
{
    internal class TestKalman
    {
        List<Point> positions = new List<Point>();
        KalmanFilter filter;

        public TestKalman(string sampleFileName)
        {
            // Read positions
            using (StreamReader reader = new StreamReader(sampleFileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Point p = Point.Parse(line);
                    positions.Add(p);
                }
            }

        }

        public void Test()
        {
            // Init param values
            double prNoiseStd = 0.001; // Start small, increase
            double msNoiseStd = 1000; // Start large, decrease

            // To compare MSEs
            double bestMSE = double.MaxValue;
            double bestPRNoiseStd = 0;
            double bestMSNoiseStd = 0;

            // Error
            double mse = 0;

            // Feed timely positions
            System.Timers.Timer timer = new System.Timers.Timer(10);


            while (prNoiseStd < 1.0)
            {
                // Initialize the Kalman filter with current parameters
                filter = new KalmanFilter(0.02, prNoiseStd, msNoiseStd);
                filter.Initialize(positions[0]);
                int index = 1;

                timer.Elapsed += (sender, e) =>
                {
                    if (index < positions.Count)
                    {
                        // Feed the position to the Kalman filter
                        filter.Update(positions[index]);

                        // Get the estimated position from the filter
                        (double X, double Y) estPos = filter.GetEstPosition();

                        // Calculate the error
                        double errorX = estPos.X - positions[index].X;
                        double errorY = estPos.Y - positions[index].Y;
                        mse += errorX * errorX + errorY * errorY;

                        index++;
                    }
                    else
                    {
                        // Calculate the average MSE
                        mse /= positions.Count;

                        Console.WriteLine($"PRN = {prNoiseStd:F3}, MSN = {msNoiseStd:F3} => MSE = {mse:F3}");

                        // Update best parameters if current MSE is lower
                        if (mse != 0 && mse < bestMSE)
                        {
                            bestMSE = mse;
                            bestPRNoiseStd = prNoiseStd;
                            bestMSNoiseStd = msNoiseStd;
                        }

                        // Reset for the next iteration
                        mse = 0;
                        index = 0;
                        timer.Stop();

                        // Adjust prNoiseStd and msNoiseStd for the next iteration
                        prNoiseStd += 0.001;
                        msNoiseStd -= 1;
                    }
                };

                timer.Start();
                // Wait for the timer to finish
                while (timer.Enabled)
                {
                    Thread.Sleep(10); // Adjust the sleep Time as needed
                }
            }

            // Print the best parameters
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Best found: prn = {bestPRNoiseStd}, msr = {bestMSNoiseStd} => MSE = {bestMSE:F3}");

        }
    }
}
