using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GpuParticleSystem2 : MonoBehaviour
{
    [SerializeField]
    int maxParticles = 10_000;

    [SerializeField]
    ComputeShader particleComputeShader;
    ComputeBuffer updateIndArgs;

    int initKernel;
    int emitKernel;
    int earlyUpdateKernel;
    int updateKernel;

    int gpuID_availableIndices;
    int gpuID_aliveIndices;
    int gpuID_nextAliveIndices;
    int gpuID_particleData;
    int gpuID_updateIndArgs;
    int gpuID_customConstants;

    ComputeBuffer availableIndices;
    ComputeBuffer aliveIndices;
    ComputeBuffer nextAliveIndices;
    ComputeBuffer aliveIndices1;
    ComputeBuffer aliveIndices2;
    ComputeBuffer particleData;
    ComputeBuffer customConstants;

    bool isEvenFrame = false;

    void Start()
    {
        if (particleComputeShader == null)
            throw new ArgumentException("No Shader assigned to script");

        InitializeBuffers();

        FindKernelIDs();

        FindBufferIDs();

        UpdateSwapBuffers(isEvenFrame);

        DispatchParticleInitialize();

        aliveIndices1.SetCounterValue(0);
        aliveIndices2.SetCounterValue(0);

        DebugVisualizeInit();
    }

    void Update()
    {
        isEvenFrame = !isEvenFrame;
        UpdateSwapBuffers(isEvenFrame);

        // DispatchParticleInitialize();
        //Emit particle and consume free indices buffer
        EmitParticle();
        //Dispatch update kernel indirect
        particleComputeShader.SetFloat("_DeltaTime", Time.deltaTime); //TEMP TESTING
        DispatchParticleUpdate();
        //create material and dispatch vertex shader for debug visualisation
        DebugVisualizeUpdate();
    }

    void InitializeBuffers()
    {
        // csharpier-ignore-start
        availableIndices = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Counter) { name = "availableIndices" };
        aliveIndices1 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Counter) { name = "aliveIndices1" };
        aliveIndices2 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Counter) { name = "aliveIndices2" };


        availableIndices.SetCounterValue((uint)maxParticles);

        ParticleShaderAnalyzer.OutputData particleShaderData = new ParticleShaderAnalyzer().AnalyzeShader("Assets/WorkingDir/ParticleSystem2/TestParticle.compute");
        //Particle Data is a counter buffer, but we're only using the counter to increment a random seed
        particleData = new ComputeBuffer(maxParticles, particleShaderData.particleStructSize, ComputeBufferType.Counter ) { name = "particleData" };
        // particleComputeShader.SetInt("_ParticleDataSize", maxParticles);
        // particleComputeShader.SetInt("_ParticleDataStride", particleShaderData.particleStructSize);

        updateIndArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments) { name = "updateIndArgs" };
        int[] bufferWithArgsData = new int[]{0 /*x thread count*/,  1 /*y thread count*/,  1 /*z thread count*/  };

        customConstants = new ComputeBuffer(3, sizeof(int), ComputeBufferType.Default) { name = "customConstants" };
        // csharpier-ignore-end
        updateIndArgs.SetData(bufferWithArgsData);
    }

    void FindKernelIDs()
    {
        initKernel = particleComputeShader.FindKernel("_Init");
        emitKernel = particleComputeShader.FindKernel("_CpuEmit");
        earlyUpdateKernel = particleComputeShader.FindKernel("_EarlyUpdate");
        updateKernel = particleComputeShader.FindKernel("_Update");
    }

    void FindBufferIDs()
    {
        gpuID_availableIndices = Shader.PropertyToID("_AvailableIndices");
        gpuID_aliveIndices = Shader.PropertyToID("_AliveIndices");
        gpuID_nextAliveIndices = Shader.PropertyToID("_NextAliveIndices");
        gpuID_particleData = Shader.PropertyToID("_ParticleData");
        gpuID_updateIndArgs = Shader.PropertyToID("_UpdateIndArgs");
        gpuID_customConstants = Shader.PropertyToID("_CustomConstants");
    }

    void DispatchParticleInitialize()
    {
        AttachBuffersToKernel(initKernel);

        int initThreadGroups = Mathf.CeilToInt(maxParticles / 64f);
        particleComputeShader.Dispatch(initKernel, initThreadGroups, 1, 1);
    }

    void EmitParticle()
    {
        //this one should only have one thead in group
        AttachBuffersToKernel(emitKernel);
        particleComputeShader.Dispatch(emitKernel, 1, 1, 1);
    }

    void DispatchParticleUpdate()
    {
        ComputeBuffer.CopyCount(aliveIndices, updateIndArgs, 0);

        particleComputeShader.SetBuffer(earlyUpdateKernel, gpuID_updateIndArgs, updateIndArgs);
        AttachBuffersToKernel(earlyUpdateKernel);
        particleComputeShader.Dispatch(earlyUpdateKernel, 1, 1, 1);

        //TODO remove this editor safeguard
        int[] tempdata = new int[3];
        updateIndArgs.GetData(tempdata);
        if (tempdata[0] * 64 > maxParticles)
            throw new Exception("Too many threads: " + tempdata[0]);
        Debug.Log(tempdata[0]);

        AttachBuffersToKernel(updateKernel);
        particleComputeShader.DispatchIndirect(updateKernel, updateIndArgs);
    }

    void AttachBuffersToKernel(int kernel)
    {
        particleComputeShader.SetBuffer(kernel, gpuID_availableIndices, availableIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_aliveIndices, aliveIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_nextAliveIndices, nextAliveIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_particleData, particleData);
        particleComputeShader.SetBuffer(kernel, gpuID_customConstants, customConstants);
    }

    void UpdateSwapBuffers(bool isEvenFrame)
    {
        aliveIndices = isEvenFrame ? aliveIndices1 : aliveIndices2;
        nextAliveIndices = isEvenFrame ? aliveIndices2 : aliveIndices1;
    }

    private List<ComputeBuffer> GetAllComputeBuffers()
    {
        var computeBufferFields = this.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.FieldType == typeof(ComputeBuffer));
        return computeBufferFields.Select(p => (ComputeBuffer)p.GetValue(this)).ToList();
    }

    void OnDestroy()
    {
        List<ComputeBuffer> test = this.GetAllComputeBuffers();
        foreach (ComputeBuffer computeBuffer in GetAllComputeBuffers())
        {
            computeBuffer.Dispose();
        }
    }

    #region  Debug Visualisation

    [SerializeField]
    Mesh debugMesh;

    Shader debugShader;

    Material debugMaterial;

    ComputeBuffer debugMeshTriangles;
    ComputeBuffer debugMeshVertexPositions;
    ComputeBuffer debugShaderBufferWithArgs;

    void DebugVisualizeInit()
    {
        debugShader = Shader.Find("Hidden/ParticleDebug");

        debugMaterial = new Material(debugShader);

        int[] inputMeshTriangles = debugMesh.triangles;
        debugMeshTriangles = new ComputeBuffer(inputMeshTriangles.Length, sizeof(int))
        {
            name = "debugMeshTriangles"
        };
        debugMeshTriangles.SetData(inputMeshTriangles);

        Vector3[] inputMeshVertexPositions = debugMesh.vertices;
        debugMeshVertexPositions = new ComputeBuffer(
            inputMeshVertexPositions.Length,
            sizeof(float) * 3
        )
        {
            name = "debugMeshVertexPositions"
        };
        debugMeshVertexPositions.SetData(inputMeshVertexPositions);

        debugShaderBufferWithArgs = new ComputeBuffer(
            5,
            sizeof(int),
            ComputeBufferType.IndirectArguments
        )
        {
            name = "debugShaderBufferWithArgs"
        };
        int[] bufferWithArgsData = new int[]
        {
            debugMeshTriangles.count, /*vertex count per instance*/
            1, /*instance count*/
            0, /*start vertex location*/
            0, /*start instance location*/
        };
        debugShaderBufferWithArgs.SetData(bufferWithArgsData);
    }

    void DebugVisualizeUpdate()
    {
        ComputeBuffer.CopyCount(nextAliveIndices, debugShaderBufferWithArgs, sizeof(int));
        debugMaterial.SetBuffer("_ParticleData", particleData);
        debugMaterial.SetBuffer("_MeshTriangles", debugMeshTriangles);
        debugMaterial.SetBuffer("_MeshVertexPositions", debugMeshVertexPositions);
        debugMaterial.SetBuffer("_AliveIndices", aliveIndices);
        // debugMaterial.SetBuffer("_ArgsBuffer", debugShaderBufferWithArgs);

        Graphics.DrawProceduralIndirect(
            debugMaterial,
            new Bounds(Vector3.zero, Vector3.one * 1_000_000),
            MeshTopology.Triangles,
            debugShaderBufferWithArgs
        );
    }

    #endregion
}
