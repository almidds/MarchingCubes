using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{

    public ComputeShader NoiseShader;

    // For communication with the GPU
    ComputeBuffer _weightsBuffer;

    // Noise Settings
    [SerializeField] float amplitude = 5f;
    [SerializeField] float frequency = 0.005f;
    [SerializeField] int octaves = 8;
    [SerializeField, Range(0f, 1f)] float groundPercent = 0.2f;

    public void Awake()
    {
        CreateBuffers();
    }

    public void OnDestroy()
    {
        ReleaseBuffers();
    }

    void CreateBuffers()
    {
        _weightsBuffer = new ComputeBuffer
            (
                GridMetrics.PointsPerChunk *
                GridMetrics.PointsPerChunk *
                GridMetrics.PointsPerChunk,
                sizeof(float)
            );
    }

    void ReleaseBuffers()
    {
        _weightsBuffer.Release();
    }

    public float[] GetNoise() {
        // Generate array for noise
        float[] noiseValues =
            new float[GridMetrics.PointsPerChunk *
                      GridMetrics.PointsPerChunk *
                      GridMetrics.PointsPerChunk];
        
        // Set buffer between CPU and GPU
        NoiseShader.SetBuffer(0, "_Weights", _weightsBuffer);

        NoiseShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk);
        NoiseShader.SetFloat("_Amplitude", amplitude);
        NoiseShader.SetFloat("_Frequency", frequency);
        NoiseShader.SetInt("_Octaves", octaves);
        NoiseShader.SetFloat("_GroundPercent", groundPercent);

        // Run the compute shader
        NoiseShader.Dispatch(
            0,
            GridMetrics.PointsPerChunk / GridMetrics.NumThreads,
            GridMetrics.PointsPerChunk / GridMetrics.NumThreads,
            GridMetrics.PointsPerChunk / GridMetrics.NumThreads
        );
        // Get the data back from the GPU
        _weightsBuffer.GetData(noiseValues);
        return noiseValues;
    }
}
