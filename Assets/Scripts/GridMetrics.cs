using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GridMetrics {
    // Number of threads per work group
    public const int NumThreads = 8;
    // Size of each chunk
    public const int PointsPerChunk = 8;
}
