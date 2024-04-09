using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GridMetrics {
    // Number of threads per work group
    public const int NumThreads = 8;

    public static int[] LODs = {
		8,
		16,
		24,
		32,
		40
	};

    public static int PointsPerChunk(int lod) {
        return LODs[lod];
    }

    public static int ThreadGroups(int lod) {
        return LODs[lod] / NumThreads;
    }

    public const int Scale = 32;

    public const int GroundLevel = Scale / 2;

    public static int LastLod = LODs.Length - 1;
}
