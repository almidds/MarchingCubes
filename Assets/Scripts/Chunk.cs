using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

    public NoiseGenerator NoiseGenerator;

    float[] _weights;

    public MeshFilter MeshFilter;

    public ComputeShader MarchingShader;

    struct Triangle {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public static int SizeOf => sizeof(float) * 3 * 3;
    }

    // All triangles
    ComputeBuffer _trianglesBuffer;
    // The number of triangles
    ComputeBuffer _trianglesCountBuffer;
    // Weights generated from noise functions
    ComputeBuffer _weightsBuffer;

    void CreateBuffers()
    {
        // 5 is the maximum number of triangles per cube
        _trianglesBuffer = new ComputeBuffer
            (
                5 * GridMetrics.PointsPerChunk *
                GridMetrics.PointsPerChunk *
                GridMetrics.PointsPerChunk,
                Triangle.SizeOf,
                ComputeBufferType.Append
            );

        _trianglesCountBuffer = new ComputeBuffer
            (
                1,
                sizeof(int),
                ComputeBufferType.Raw
            );

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
        _trianglesBuffer.Release();
        _trianglesCountBuffer.Release();
        _weightsBuffer.Release();
    }

    public void Awake()
    {
        CreateBuffers();
    }

    public void OnDestroy()
    {
        ReleaseBuffers();
    }

    void Start()
    {
        _weights = NoiseGenerator.GetNoise();
        MeshFilter.sharedMesh = ConstructMesh();
    }

    Mesh CreateMeshFromTriangles(Triangle[] triangles)
    {
        Vector3[] verts = new Vector3[triangles.Length * 3];
        int[] tris = new int[triangles.Length * 3];

        for (int i = 0; i < triangles.Length; i++)
        {
            int startIndex = i * 3;

            verts[startIndex] = triangles[i].a;
            verts[startIndex + 1] = triangles[i].b;
            verts[startIndex + 2] = triangles[i].c;

            tris[startIndex] = startIndex;
            tris[startIndex + 1] = startIndex + 1;
            tris[startIndex + 2] = startIndex + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    Mesh ConstructMesh()
    {
        MarchingShader.SetBuffer(0, "_Triangles", _trianglesBuffer);
        MarchingShader.SetBuffer(0, "_Weights", _weightsBuffer);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk);
        MarchingShader.SetFloat("_IsoLevel", 0.5f);

        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);

        MarchingShader.Dispatch
            (
                0,
                GridMetrics.PointsPerChunk / GridMetrics.NumThreads,
                GridMetrics.PointsPerChunk / GridMetrics.NumThreads,
                GridMetrics.PointsPerChunk / GridMetrics.NumThreads
            );
        
        Triangle[] triangles = new Triangle[ReadTriangleCount()];
        _trianglesBuffer.GetData(triangles);

        return CreateMeshFromTriangles(triangles);
    }

    int ReadTriangleCount()
    {
        int[] triCount = { 0 };
        ComputeBuffer.CopyCount(_trianglesBuffer, _trianglesCountBuffer, 0);
        _trianglesCountBuffer.GetData(triCount);
        return triCount[0];
    }

    private void OnDrawGizmos()
    {
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
