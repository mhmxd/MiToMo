using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class KalmanVeloFilter
    {
        private double dt;

        private Matrix<double> F;
        private Matrix<double> Q;
        private Matrix<double> H;
        private Matrix<double> R;
        private Matrix<double> P;

        private Vector<double> x; // State: [velocityX, accelerationX, velocityY, accelerationY]

        private double _prcNoiseStd = 0.7; // Process noise std
        private double _msrNoiseStd = 10; // Measurement noise std

        public KalmanVeloFilter(double dT, double prcNoiseStd, double msrNoiseStd)
        {
            _prcNoiseStd = prcNoiseStd;
            _msrNoiseStd = msrNoiseStd;
            dt = dT;

            var mBuilder = Matrix<double>.Build;

            // State transition matrix F
            F = mBuilder.DenseOfArray(new double[,]
            {
                { 1, dt, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, dt },
                { 0, 0, 0, 1 }
            });

            // Measurement matrix H (velocity observed)
            H = mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 0, 1, 0 }
            });

            // Process noise covariance matrix
            double q = Math.Pow(_prcNoiseStd, 2);
            Q = mBuilder.DenseOfArray(new double[,]
            {
                { q, 0, 0, 0 },
                { 0, q, 0, 0 },
                { 0, 0, q, 0 },
                { 0, 0, 0, q }
            });

            // Measurement noise covariance matrix
            double r = Math.Pow(_msrNoiseStd, 2);
            R = mBuilder.DenseOfArray(new double[,]
            {
                { r, 0 },
                { 0, r }
            });
        }

        //public KalmanVeloFilter(double dT) : this(dT, 0.7, 10) { }

        //public KalmanVeloFilter(double dT) : this(dT, 0.1, 30) { }

        public void Initialize(double vX0, double vY0)
        {
            var mBuilder = Matrix<double>.Build;

            // Initial state vector [velocityX, accelerationX, velocityY, accelerationY]
            x = Vector<double>.Build.DenseOfArray(new double[]
                { vX0, 0, vX0, 0 }
            );

            P = mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });
        }

        public void Predict()
        {
            x = F * x;
            P = (F * P * F.Transpose()) + Q;
        }

        public void Update(double vX, double vY)
        {
            // Measurement
            Vector<double> z = Vector<double>.Build.Dense(new double[] { vX, vY });

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

        public (double, double) GetEstVelocity()
        {
            return (x[0], x[2]);
        }

        public (double, double) GetEstAcceleration()
        {
            return (x[1], x[3]);
        }
    }
}
