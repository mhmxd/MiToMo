using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Panel.Select
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

        private double _prcNoiseStd; // Process noise std
        private double _msrNoiseStd; // Measurement noise std

        private readonly MatrixBuilder<double> _mBuilder = Matrix<double>.Build; // Single instance

        public KalmanVeloFilter(double prcNoiseStd, double msrNoiseStd)
        {
            _prcNoiseStd = prcNoiseStd;
            _msrNoiseStd = msrNoiseStd;

            // Measurement matrix H (velocity observed)
            H = _mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 0, 1, 0 }
            });

            // Process noise covariance matrix (will be updated in Predict with new dTs)
            double q = Pow(_prcNoiseStd, 2);
            Q = _mBuilder.DenseOfArray(new double[,]
            {
                { q, 0, 0, 0 },
                { 0, q, 0, 0 },
                { 0, 0, q, 0 },
                { 0, 0, 0, q }
            });

            // Measurement noise covariance matrix
            double r = Pow(_msrNoiseStd, 2);
            R = _mBuilder.DenseOfArray(new double[,]
            {
                { r, 0 },
                { 0, r }
            });
        }

        public void Initialize(double vX0, double vY0)
        {

            // Initial state vector [velocityX, accelerationX, velocityY, accelerationY]
            x = Vector<double>.Build.DenseOfArray(new double[]
                { vX0, 0, vX0, 0 }
            );

            P = _mBuilder.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });
        }

        public void Predict(double dT)
        {
            // State transition matrix F
            F = _mBuilder.DenseOfArray(new double[,]
            {
                { 1, dT, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, dT },
                { 0, 0, 0, 1 }
            });

            // Recalculate Process noise covariance matrix Q based on current dT (scaling might be needed)
            double q = Pow(_prcNoiseStd, 2) * dT; // Simple scaling, you might need a more sophisticated model
            Q = _mBuilder.DenseOfArray(new double[,]
            {
                { q, 0, 0, 0 },
                { 0, q, 0, 0 },
                { 0, 0, q, 0 },
                { 0, 0, 0, q }
            });

            // Predict!
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
