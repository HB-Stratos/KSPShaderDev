using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicComputeSpheres : MonoBehaviour
{
    public int sphereAmount = 17;
    public ComputeShader shader;

    public Mesh mesh;
    public Material material;

    ComputeBuffer sphereLocations;
    int kernel;
    uint threadGroupSize;
    Bounds bounds;
    int threadGroups;

    ComputeBuffer meshTriangles;
    ComputeBuffer meshVertexPositions;

    // Start is called before the first frame update
    void Start()
    {
        kernel = shader.FindKernel("Spheres");
        shader.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);

        threadGroups = (int)((sphereAmount + (threadGroupSize - 1)) / threadGroupSize);

        sphereLocations = new ComputeBuffer(sphereAmount, sizeof(float) * 3);

        int[] inputMeshTriangles = mesh.triangles;
        meshTriangles = new ComputeBuffer(inputMeshTriangles.Length, sizeof(int));
        meshTriangles.SetData(inputMeshTriangles);

        Vector3[] inputMeshVertexPositions = mesh.vertices;
        meshVertexPositions = new ComputeBuffer(inputMeshVertexPositions.Length, sizeof(float) * 3);
        meshVertexPositions.SetData(inputMeshVertexPositions);

        shader.SetBuffer(kernel, "SphereLocations", sphereLocations);

        material.SetBuffer("SphereLocations", sphereLocations);
        material.SetBuffer("MeshTriangles", meshTriangles);
        material.SetBuffer("MeshVertexPositions", meshVertexPositions);

        bounds = new Bounds(Vector3.zero, Vector3.one * 20);
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat("Time", Time.time);
        shader.Dispatch(kernel, threadGroups, 1, 1);

        Graphics.DrawProcedural(
            material,
            bounds,
            MeshTopology.Triangles,
            meshTriangles.count,
            sphereAmount
        );
    }

    void OnDestroy()
    {
        sphereLocations.Dispose();
        meshTriangles.Dispose();
        meshVertexPositions.Dispose();
    }
}
