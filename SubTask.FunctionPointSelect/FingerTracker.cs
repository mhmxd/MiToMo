using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SubTask.FunctionPointSelect
{
    // Helper class for blob detection
    // Helper class for blob detection with improved features
    public class BlobInfo
    {
        public List<Point> Points { get; private set; } = new List<Point>();
        public int PointCount => Points.Count;
        public int TotalPressure { get; private set; }
        public double CenterX { get; private set; }
        public double CenterY { get; private set; }
        public double AveragePressure => TotalPressure / (double)PointCount;

        // For weighted center calculation
        private int _weightedSumX;
        private int _weightedSumY;

        // For shape analysis
        private int _minX = int.MaxValue;
        private int _maxX = int.MinValue;
        private int _minY = int.MaxValue;
        private int _maxY = int.MinValue;

        public void AddPoint(int x, int y, byte pressure)
        {
            Points.Add(new Point(x, y));
            TotalPressure += pressure;
            _weightedSumX += x * pressure;
            _weightedSumY += y * pressure;

            // Update bounding box
            _minX = Math.Min(_minX, x);
            _maxX = Math.Max(_maxX, x);
            _minY = Math.Min(_minY, y);
            _maxY = Math.Max(_maxY, y);
        }

        public void MergeWith(BlobInfo other)
        {
            Points.AddRange(other.Points);
            TotalPressure += other.TotalPressure;
            _weightedSumX += other._weightedSumX;
            _weightedSumY += other._weightedSumY;

            // Update bounding box
            _minX = Math.Min(_minX, other._minX);
            _maxX = Math.Max(_maxX, other._maxX);
            _minY = Math.Min(_minY, other._minY);
            _maxY = Math.Max(_maxY, other._maxY);
        }

        public void CalculateCenterOfMass()
        {
            if (TotalPressure > 0)
            {
                CenterX = _weightedSumX / (double)TotalPressure;
                CenterY = _weightedSumY / (double)TotalPressure;
            }
            else if (PointCount > 0)
            {
                // Fallback if pressure data is unreliable
                int sumX = 0, sumY = 0;
                foreach (var point in Points)
                {
                    sumX += point.X;
                    sumY += point.Y;
                }
                CenterX = sumX / (double)PointCount;
                CenterY = sumY / (double)PointCount;
            }
        }

        public double GetAspectRatio()
        {
            int width = _maxX - _minX + 1;
            int height = _maxY - _minY + 1;

            if (height == 0) return 0;
            return width / (double)height;
        }

        public double GetArea()
        {
            return PointCount;
        }

        public override string ToString()
        {
            return $"Blob at ({CenterX:F1}, {CenterY:F1}), Points: {PointCount}, Pressure: {AveragePressure:F1}";
        }
    }

    internal class FingerTracker
    {
        // Constants for optimization
        private const int MaxFingers = 5;
        private const int PressureThreshold = 10;
        private const double MaxTrackingDistance = 30;

        // Storage for tracked fingers - using array for performance
        private readonly FullFinger[] _fingers = new FullFinger[MaxFingers];
        private readonly bool[] _fingerActive = new bool[MaxFingers];
        //private int _activeFingerCount = 0;

        // For debugging
        public List<BlobInfo> LastDetectedBlobs { get; private set; } = new List<BlobInfo>();

        // Detect and track fingers in the current frame
        public FullFinger[] ProcessFrame(Span2D<byte> touchData)
        {
            // Mark all fingers as inactive for this frame
            for (int i = 0; i < MaxFingers; i++)
                _fingerActive[i] = false;

            // Detect blobs in current frame
            List<BlobInfo> blobs = DetectBlobs(touchData);
            LastDetectedBlobs = blobs;  // Store for debugging

            // Match blobs to existing fingers
            MatchBlobsToFingers(blobs);

            // Mark unmatched fingers as inactive
            for (int i = 0; i < MaxFingers; i++)
            {
                if (_fingers[i] != null && !_fingerActive[i])
                    _fingers[i].MarkInactive();
            }

            // Return active fingers
            return _fingers.Where(f => f != null && f.IsActive).ToArray();
        }

        // Improved blob detection with better isolation
        private List<BlobInfo> DetectBlobs(Span2D<byte> touchData)
        {
            int width = touchData.Width;
            int height = touchData.Height;

            // Create a matrix to track component labels
            int[,] labels = new int[height, width];
            int nextLabel = 1;
            Dictionary<int, BlobInfo> blobs = new Dictionary<int, BlobInfo>();

            // First pass: Assign provisional labels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (touchData[y, x] < PressureThreshold)
                        continue;

                    // Get neighbors (consider 8-connectivity for better detection)
                    List<int> neighborLabels = new List<int>();

                    // Check the 8 neighbors
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                                labels[ny, nx] != 0)
                            {
                                neighborLabels.Add(labels[ny, nx]);
                            }
                        }
                    }

                    if (neighborLabels.Count == 0)
                    {
                        // No neighbors, create new blob
                        labels[y, x] = nextLabel;
                        blobs[nextLabel] = new BlobInfo();
                        blobs[nextLabel].AddPoint(x, y, touchData[y, x]);
                        nextLabel++;
                    }
                    else
                    {
                        // Use the minimum label
                        int minLabel = neighborLabels.Min();
                        labels[y, x] = minLabel;
                        blobs[minLabel].AddPoint(x, y, touchData[y, x]);

                        // Merge components if needed
                        if (neighborLabels.Count > 1)
                        {
                            // Remember equivalences for second pass
                            foreach (int otherLabel in neighborLabels)
                            {
                                if (otherLabel != minLabel)
                                {
                                    // Transfer points from other blob to this one
                                    blobs[minLabel].MergeWith(blobs[otherLabel]);

                                    // Mark for removal
                                    blobs.Remove(otherLabel);

                                    // Update all previous labels in the image
                                    for (int scanY = 0; scanY <= y; scanY++)
                                    {
                                        for (int scanX = 0; scanX < width; scanX++)
                                        {
                                            if (labels[scanY, scanX] == otherLabel)
                                                labels[scanY, scanX] = minLabel;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Post-processing: Calculate centers and filter small blobs
            List<BlobInfo> validBlobs = new List<BlobInfo>();
            foreach (var blob in blobs.Values)
            {
                blob.CalculateCenterOfMass();

                // Filter: minimum size to be considered a finger
                if (blob.PointCount >= 3)  // Minimum size threshold
                {
                    validBlobs.Add(blob);
                }
            }

            // Further improve by splitting large blobs that might be multiple fingers
            List<BlobInfo> finalBlobs = new List<BlobInfo>();
            foreach (var blob in validBlobs)
            {
                if (ShouldSplitBlob(blob))
                {
                    var splitBlobs = SplitBlob(blob, touchData);
                    finalBlobs.AddRange(splitBlobs);
                }
                else
                {
                    finalBlobs.Add(blob);
                }
            }

            return finalBlobs.Take(MaxFingers).ToList();
        }

        // Determine if a blob should be split (large area or strange shape)
        private bool ShouldSplitBlob(BlobInfo blob)
        {
            // Size-based splitting
            if (blob.PointCount > 20)  // Large blob
                return true;

            // Shape-based splitting using elongation
            double aspectRatio = blob.GetAspectRatio();
            if (aspectRatio > 2.5)  // Elongated shape probably is multiple fingers
                return true;

            return false;
        }

        // Split a large blob into potentially multiple fingers
        private List<BlobInfo> SplitBlob(BlobInfo blob, Span2D<byte> touchData)
        {
            // K-means clustering approach for splitting
            const int maxIterations = 5;

            // Try to find optimal number of clusters (fingers)
            int numClusters = EstimateOptimalClusters(blob);

            // Initialize centroids
            List<Point> centroids = InitializeCentroids(blob, numClusters);

            // Perform K-means clustering
            Dictionary<int, List<Point>> clusters = new Dictionary<int, List<Point>>();
            Dictionary<int, int> clusterPressures = new Dictionary<int, int>();

            for (int i = 0; i < maxIterations; i++)
            {
                // Reset clusters
                clusters.Clear();
                clusterPressures.Clear();
                for (int j = 0; j < numClusters; j++)
                {
                    clusters[j] = new List<Point>();
                    clusterPressures[j] = 0;
                }

                // Assign points to clusters
                foreach (var point in blob.Points)
                {
                    // Find closest centroid
                    int closestCentroid = 0;
                    double minDistance = double.MaxValue;

                    for (int j = 0; j < numClusters; j++)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(point.X - centroids[j].X, 2) +
                            Math.Pow(point.Y - centroids[j].Y, 2)
                        );

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestCentroid = j;
                        }
                    }

                    // Add point to cluster
                    clusters[closestCentroid].Add(point);
                    clusterPressures[closestCentroid] += touchData[point.Y, point.X];
                }

                // Update centroids
                bool centroidsChanged = false;
                for (int j = 0; j < numClusters; j++)
                {
                    if (clusters[j].Count == 0)
                        continue;

                    int sumX = 0;
                    int sumY = 0;

                    foreach (var point in clusters[j])
                    {
                        sumX += point.X;
                        sumY += point.Y;
                    }

                    Point newCentroid = new Point(
                        sumX / clusters[j].Count,
                        sumY / clusters[j].Count
                    );

                    if (newCentroid.X != centroids[j].X || newCentroid.Y != centroids[j].Y)
                    {
                        centroids[j] = newCentroid;
                        centroidsChanged = true;
                    }
                }

                // Exit if centroids didn't change
                if (!centroidsChanged)
                    break;
            }

            // Convert clusters to blobs
            List<BlobInfo> resultBlobs = new List<BlobInfo>();

            foreach (var clusterKvp in clusters)
            {
                if (clusterKvp.Value.Count >= 3)  // Minimum size threshold
                {
                    BlobInfo newBlob = new BlobInfo();

                    foreach (var point in clusterKvp.Value)
                    {
                        newBlob.AddPoint(point.X, point.Y, touchData[point.Y, point.X]);
                    }

                    newBlob.CalculateCenterOfMass();
                    resultBlobs.Add(newBlob);
                }
            }

            return resultBlobs;
        }

        // Estimate optimal number of clusters (fingers) in a blob
        private int EstimateOptimalClusters(BlobInfo blob)
        {
            // Simple heuristic based on area and shape
            double aspectRatio = blob.GetAspectRatio();

            if (blob.PointCount > 40)
                return 3;
            else if (blob.PointCount > 25 || aspectRatio > 3.0)
                return 2;

            return 1;
        }

        // Initialize K-means centroids
        private List<Point> InitializeCentroids(BlobInfo blob, int numClusters)
        {
            List<Point> centroids = new List<Point>();

            if (numClusters == 1)
            {
                // Single cluster - use center of mass
                centroids.Add(new Point((int)blob.CenterX, (int)blob.CenterY));
            }
            else
            {
                // Multiple clusters - initialize along principal axis
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;

                foreach (var point in blob.Points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                // Use diagonal as principal axis if no better info
                for (int i = 0; i < numClusters; i++)
                {
                    double t = i / (numClusters - 1.0);
                    int x = (int)(minX + t * (maxX - minX));
                    int y = (int)(minY + t * (maxY - minY));
                    centroids.Add(new Point(x, y));
                }
            }

            return centroids;
        }

        // Match detected blobs to existing fingers
        private void MatchBlobsToFingers(List<BlobInfo> blobs)
        {
            if (blobs.Count == 0)
                return;

            // Create distance matrix between blobs and fingers
            double[,] distances = new double[blobs.Count, MaxFingers];
            bool[,] valid = new bool[blobs.Count, MaxFingers];

            for (int i = 0; i < blobs.Count; i++)
            {
                for (int j = 0; j < MaxFingers; j++)
                {
                    if (_fingers[j] == null)
                    {
                        distances[i, j] = double.MaxValue;
                        valid[i, j] = false;
                    }
                    else
                    {
                        double dx = blobs[i].CenterX - _fingers[j].Position.X;
                        double dy = blobs[i].CenterY - _fingers[j].Position.Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        distances[i, j] = dist;
                        valid[i, j] = dist <= MaxTrackingDistance;
                    }
                }
            }

            // Match blobs to fingers using greedy algorithm
            HashSet<int> assignedBlobs = new HashSet<int>();
            HashSet<int> assignedFingers = new HashSet<int>();

            // First pass: Assign based on min distance
            while (assignedBlobs.Count < blobs.Count && assignedFingers.Count < MaxFingers)
            {
                // Find the minimum valid distance
                double minDist = double.MaxValue;
                int minBlobIdx = -1;
                int minFingerIdx = -1;

                for (int i = 0; i < blobs.Count; i++)
                {
                    if (assignedBlobs.Contains(i))
                        continue;

                    for (int j = 0; j < MaxFingers; j++)
                    {
                        if (assignedFingers.Contains(j) || !valid[i, j])
                            continue;

                        if (distances[i, j] < minDist)
                        {
                            minDist = distances[i, j];
                            minBlobIdx = i;
                            minFingerIdx = j;
                        }
                    }
                }

                // If no valid match found, break
                if (minBlobIdx == -1)
                    break;

                // Assign blob to finger
                assignedBlobs.Add(minBlobIdx);
                assignedFingers.Add(minFingerIdx);

                BlobInfo blob = blobs[minBlobIdx];

                // Update existing finger
                _fingers[minFingerIdx].Update(
                    new Point((int)blob.CenterX, (int)blob.CenterY),
                    blob.AveragePressure,
                    blob.PointCount
                );

                _fingerActive[minFingerIdx] = true;
            }

            // Second pass: Create new fingers for unassigned blobs
            for (int i = 0; i < blobs.Count; i++)
            {
                if (assignedBlobs.Contains(i))
                    continue;

                // Find an unused finger slot
                int freeIdx = -1;
                for (int j = 0; j < MaxFingers; j++)
                {
                    if (_fingers[j] == null)
                    {
                        freeIdx = j;
                        break;
                    }
                    else if (!_fingers[j].IsActive)
                    {
                        freeIdx = j;
                        break;
                    }
                }

                // If no slot found, we've reached max fingers
                if (freeIdx == -1)
                    break;

                // Create new finger
                BlobInfo blob = blobs[i];
                _fingers[freeIdx] = new FullFinger(
                    freeIdx,
                    new Point((int)blob.CenterX, (int)blob.CenterY),
                    blob.AveragePressure,
                    blob.PointCount
                );

                _fingerActive[freeIdx] = true;
            }
        }
    }
}
