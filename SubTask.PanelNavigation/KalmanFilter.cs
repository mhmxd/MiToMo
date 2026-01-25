using MathNet.Numerics.LinearAlgebra;
using System;
using System.Windows;

namespace SubTask.PanelNavigation
{
    internal class KalmanFilter
    {
        private double dt;

        private Matrix<double> F;
        private Matrix<double> Q;
        private Matrix<double> H;
        private Matrix<double> R;
        private Matrix<double> P;

        private Vector<double> x; // State

        //private Matrix<double> x0;
        //private Matrix<double> P0;

        private double _prcNoiseStd = 0.7; // Process noise pr
        private double _msrNoiseStd = 10; // Measurement noise std

        public KalmanFilter(double dT, double prcNoiseStd, double msrNoiseStd)
        {
            _prcNoiseStd = prcNoiseStd;
            _msrNoiseStd = msrNoiseStd;
            dt = dT;

            var mBuilder = Matrix<double>.Build;

            // State transition matrix F
            F = mBuilder.DenseOfArray(new double[,]
            {
                { 1, dt, 0.5 * dt * dt, 0, 0, 0 },
                { 0, 1, dt, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 1, dt, 0.5 * dt * dt },
                { 0, 0, 0, 0, 1, dt },
                { 0, 0, 0, 0, 0, 1 }
            });

            // Measurement matrix H (position observed, velocity and accel estimated)
            H = mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0 }
            });

            // Process noise covariance matrix
            double q = Math.Pow(_prcNoiseStd, 2);
            Q = mBuilder.DenseOfArray(new double[,]
            {
                { q, 0, 0, 0, 0, 0 },
                { 0, q, 0, 0, 0, 0 },
                { 0, 0, q, 0, 0, 0 },
                { 0, 0, 0, q, 0, 0 },
                { 0, 0, 0, 0, q, 0 },
                { 0, 0, 0, 0, 0, q }
            });

            // Measurement noise covariance matrix
            double r = Math.Pow(_msrNoiseStd, 2);
            R = mBuilder.DenseOfArray(new double[,]
            {
                { r, 0 },
                { 0, r }
            });
        }

        public KalmanFilter(double dT)
        {
            this.dt = dT;

            var mBuilder = Matrix<double>.Build;

            // State transition matrix F
            F = mBuilder.DenseOfArray(new double[,]
            {
                { 1, dT, 0.5 * dT * dT, 0, 0, 0 },
                { 0, 1, dT, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 1, dT, 0.5 * dT * dT },
                { 0, 0, 0, 0, 1, dT },
                { 0, 0, 0, 0, 0, 1 }
            });

            // Measurement matrix H (position observed, velocity and accel estimated)
            H = mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0 }
            });

            // Process noise covariance matrix
            double q = Math.Pow(_prcNoiseStd, 2);
            Q = mBuilder.DenseOfArray(new double[,]
            {
                { q, 0, 0, 0, 0, 0 },
                { 0, q, 0, 0, 0, 0 },
                { 0, 0, q, 0, 0, 0 },
                { 0, 0, 0, q, 0, 0 },
                { 0, 0, 0, 0, q, 0 },
                { 0, 0, 0, 0, 0, q }
            });

            // Measurement noise covariance matrix
            double r = Math.Pow(_msrNoiseStd, 2);
            R = mBuilder.DenseOfArray(new double[,]
            {
                { r, 0 },
                { 0, r }
            });

        }

        public void Initialize(Point initPos)
        {
            var mBuilder = Matrix<double>.Build;

            // Initial state vector [posX, velX, accX, posY, velY, accY]
            x = Vector<double>.Build.DenseOfArray(new double[]
                { initPos.X, 0, 0, initPos.Y, 0, 0 }
            );
            //Vector<double> initialVector = Vector<double>.Build.DenseOfArray(new double[] 
            //    { initPos.x, 0, 0, initPos.y, 0, 0 }
            //);
            //x0 = initialVector.ToColumnMatrix();

            P = mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0, 0, 0 },
                { 0, 1, 0, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0 },
                { 0, 0, 0, 0, 1, 0 },
                { 0, 0, 0, 0, 0, 1 }
            });

            // Create the filter
            //filter = new DiscreteKalmanFilter(x0, P0);
        }

        public void Predict()
        {
            // Previous Version ------------------
            // Predict the next state
            x = F * x;

            // Predict the next error covariance
            P = (F * P * F.Transpose()) + Q;

            // New Version ------------------
            //filter.Predict(F, Q);
        }

        public void Update(Point p)
        {
            // Previous Version ------------------
            // Measurement
            Vector<double> z = Vector<double>.Build.Dense(new double[] { p.X, p.Y });

            // Innovation or measurement residual
            Vector<double> y = z - (H * x);

            // Innovation (residual) covariance
            Matrix<double> S = (H * P * H.Transpose()) + R;

            // Kalman gain
            Matrix<double> K = P * H.Transpose() * S.Inverse();

            // Update the state
            x = x + (K * y);

            // Update the error covariance
            int size = P.RowCount;
            Matrix<double> I = Matrix<double>.Build.DenseIdentity(size);
            P = (I - K * H) * P;

        }

        public (double, double) GetEstPosition()
        {
            return (x[0], x[3]);
        }

        public (double, double) GetEstVelocity()
        {
            return (x[1], x[4]);
        }


    }
}
