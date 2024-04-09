using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

    [Range(0, 4)]
    public int LOD;

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

    public MeshCollider MeshCollider;

    Mesh _mesh;

    void CreateBuffers()
    {
        // 5 is the maximum number of triangles per cube
        _trianglesBuffer = new ComputeBuffer
            (
                5 * GridMetrics.PointsPerChunk(LOD) *
                GridMetrics.PointsPerChunk(LOD) *
                GridMetrics.PointsPerChunk(LOD),
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
                GridMetrics.PointsPerChunk(LOD) *
                GridMetrics.PointsPerChunk(LOD) *
                GridMetrics.PointsPerChunk(LOD),
                sizeof(float)
            );
    }

    void ReleaseBuffers()
    {
        _trianglesBuffer.Release();
        _trianglesCountBuffer.Release();
        _weightsBuffer.Release();
    }

    private void Start() {
        Create();
    }

    void Create()
    {
        CreateBuffers();
        _weights = NoiseGenerator.GetNoise(LOD);
        _mesh = new Mesh();
        UpdateMesh();
        ReleaseBuffers();
    }

    void UpdateMesh() {
        Mesh mesh = ConstructMesh();
        MeshFilter.sharedMesh = mesh;
        MeshCollider.sharedMesh = mesh;
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
        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        return _mesh;
    }

    Mesh ConstructMesh()
    {
        int kernel = MarchingShader.FindKernel("March");

        MarchingShader.SetBuffer(0, "_Triangles", _trianglesBuffer);
        MarchingShader.SetBuffer(0, "_Weights", _weightsBuffer);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(LOD));
        MarchingShader.SetFloat("_IsoLevel", 0.5f);
        MarchingShader.SetInt("_Scale", GridMetrics.Scale);

        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);

        MarchingShader.Dispatch
            (
                kernel,
                GridMetrics.ThreadGroups(LOD),
                GridMetrics.ThreadGroups(LOD),
                GridMetrics.ThreadGroups(LOD)
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

    public void EditWeights(Vector3 hitPosition, float brushSize, bool add) {
        CreateBuffers();
        int kernel = MarchingShader.FindKernel("UpdateWeights");

        _weightsBuffer.SetData(_weights);
        MarchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(LOD));
        MarchingShader.SetVector("_HitPosition", hitPosition);
        MarchingShader.SetFloat("_BrushSize", brushSize);
        MarchingShader.SetFloat("_TerraformStrength", add ? 1f : -1f);
        MarchingShader.SetInt("_Scale", GridMetrics.Scale);

        MarchingShader.Dispatch
            (
                kernel,
                GridMetrics.ThreadGroups(LOD),
                GridMetrics.ThreadGroups(LOD),
                GridMetrics.ThreadGroups(LOD)
            );

        _weightsBuffer.GetData(_weights);

        UpdateMesh();
        ReleaseBuffers();
    }

    private void OnValidate() {
        if (Application.isPlaying) {
            Create();
        }
    }

}
