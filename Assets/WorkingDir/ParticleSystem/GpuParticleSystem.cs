using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GpuParticleSystem : MonoBehaviour
{
    [Header("Particle System Variables")]
    public Vector3 emitVector = Vector3.forward;

    public bool isBillboard = true;

    [Range(1, 1_000_000)]
    public int maxParticleCount = 100_000;

    [Header("To be replaced with procedural gets")]
    public Mesh particleMesh;
    public ComputeShader computeShader;
    public Shader particleShader;

    // Particle:
    // - Position
    // - Velocity


    ComputeBuffer particlePositions;
    ComputeBuffer particleVelocies;

    Material particleMaterial;
    Bounds bounds;

    int computeThreadGroups;

    ComputeBuffer particleTriangles;
    ComputeBuffer particleVertexPositions;

    int particleUpdateKernel;

    private List<ComputeBuffer> GetAllComputeBuffers()
    {
        var computeBufferFields = this.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.FieldType == typeof(ComputeBuffer));
        return computeBufferFields.Select(p => (ComputeBuffer)p.GetValue(this)).ToList();
    }

    // Start is called before the first frame update
    void Start()
    {
        particleMaterial = new Material(particleShader);

        particlePositions = new ComputeBuffer(maxParticleCount, sizeof(float) * 3);
        particleVelocies = new ComputeBuffer(maxParticleCount, sizeof(float) * 3);

        particleUpdateKernel = computeShader.FindKernel("UpdateParticle");
        uint computeThreadGroupSize;
        computeShader.GetKernelThreadGroupSizes(
            particleUpdateKernel,
            out computeThreadGroupSize,
            out _,
            out _
        );
        computeThreadGroups = (int)(
            (maxParticleCount + (computeThreadGroupSize - 1)) / computeThreadGroupSize
        );

        int[] inputParticleTriangles = particleMesh.triangles;
        particleTriangles = new ComputeBuffer(inputParticleTriangles.Length, sizeof(int));
        particleTriangles.SetData(inputParticleTriangles);
        Vector3[] inputParticleVertexPositions = particleMesh.vertices;
        particleVertexPositions = new ComputeBuffer(
            inputParticleVertexPositions.Length,
            sizeof(float) * 3
        );
        particleVertexPositions.SetData(inputParticleVertexPositions);

        computeShader.SetVector("_EmitVector", emitVector); //actually sets a vec4, but with w=0;
        computeShader.SetBuffer(particleUpdateKernel, "_ParticlePositions", particlePositions);
        computeShader.SetBuffer(particleUpdateKernel, "_ParticleVelocities", particleVelocies);

        bounds = new Bounds(Vector3.zero, Vector3.one * 1_000_000);

        particleMaterial.SetBuffer("_ParticlePositions", particlePositions);
        particleMaterial.SetBuffer("_ParticleTriangles", particleTriangles);
        particleMaterial.SetBuffer("_ParticleVertexPositions", particleVertexPositions);
        particleMaterial.SetInt("_isBillboard", isBillboard ? 1 : 0);
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetFloat("unity_DeltaTime", Time.deltaTime);
        computeShader.Dispatch(particleUpdateKernel, computeThreadGroups, 1, 1);

        Graphics.DrawProcedural(
            particleMaterial,
            bounds,
            MeshTopology.Triangles,
            particleTriangles.count,
            maxParticleCount
        );
    }

    void OnDestroy()
    {
        List<ComputeBuffer> test = this.GetAllComputeBuffers();
        foreach (ComputeBuffer computeBuffer in GetAllComputeBuffers())
        {
            computeBuffer.Dispose();
        }
    }
}
