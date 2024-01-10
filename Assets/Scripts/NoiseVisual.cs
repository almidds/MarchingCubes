using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseVisual : MonoBehaviour
{
    public NoiseGenerator NoiseGenerator;

    float[] _weights;

    void Start()
    {
        // _weights = NoiseGenerator.GetNoise();
        _weights = new float[GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk];
        for (int i = 0; i < _weights.Length; i++) {
            _weights[i] = Random.value;
        }
    }

    private void OnDrawGizmos() {
        if (_weights == null || _weights.Length == 0)
        {
            return;
        }
        for (int x = 0; x < GridMetrics.PointsPerChunk; x++)
        {
            for (int y = 0; y < GridMetrics.PointsPerChunk; y++)
            {
                for (int z = 0; z < GridMetrics.PointsPerChunk; z++)
                {
                    int index = x + GridMetrics.PointsPerChunk * (y + GridMetrics.PointsPerChunk * z);
                    float noiseValue = _weights[index];
                    Gizmos.color = Color.Lerp(Color.black, Color.white, noiseValue);
                    Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one * 0.2f);
                }
            }
        }
    }
}
