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

    ComputeBuffer availableIndices1;
    ComputeBuffer availableIndices2;

    ComputeBuffer aliveIndices1;
    ComputeBuffer aliveindices2;

    ComputeBuffer particleData1;
    ComputeBuffer particleData2;

    ComputeBuffer updateIndArgs;

    bool isEvenFrame = false;

    int initKernel;
    int emitKernel;
    int earlyUpdateKernel;
    int updateKernel;

    int gpuID_currAvailableIndices;
    int gpuID_nextAvailableIndices;
    int gpuID_currAliveIndices;
    int gpuID_nextAliveIndices;
    int gpuID_currData;
    int gpuID_nextData;
    int gpuID_updateIndArgs;

    ComputeBuffer currAvailableIndices;
    ComputeBuffer nextAvailableIndices;
    ComputeBuffer currAliveIndices;
    ComputeBuffer nextAliveIndices;
    ComputeBuffer currData;
    ComputeBuffer nextData;

    void Start()
    {
        if (particleComputeShader == null)
            throw new ArgumentException("No Shader assigned to script");

        InitializeBuffers();

        FindKernelIDs();

        FindBufferIDs();

        DispatchParticleInitialize();
    }

    void Update()
    {
        // isEvenFrame = !isEvenFrame; //This must stay here to flip buffers after nextAvailableIndices is initialized in Start()
        // UpdateCurrNextBuffers();

        //Add a CPU emit function //Emit particle and consume free indices buffer
        EmitParticle();
        //Dispatch update kernel indirect
        DispatchParticleUpdate();
        //create material and dispatch vertex shader for debug visualisation
        DebugVisualize();
    }

    void InitializeBuffers()
    {
        // csharpier-ignore-start
        availableIndices1 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append) {name = "availableIndices1"};
        availableIndices2 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append) {name = "availableIndices2"};

        aliveIndices1 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append) {name = "aliveIndices1"};
        aliveindices2 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append) {name = "aliveindices2"};

        ParticleShaderAnalyzer.OutputData particleShaderData =
            new ParticleShaderAnalyzer().AnalyzeShader(
                "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
            );

        particleData1 = new ComputeBuffer(
            maxParticles,
            particleShaderData.particleStructSize,
            ComputeBufferType.Counter
        ) {name = "particleData1"};
        particleData2 = new ComputeBuffer(
            maxParticles,
            particleShaderData.particleStructSize,
            ComputeBufferType.Counter
        ) {name = "particleData2"};
        // csharpier-ignore-end


        updateIndArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments)
        {
            name = "updateIndArgs"
        };
        int[] bufferWithArgsData = new int[]
        {
            0, /*x thread count*/
            1, /*y thread count*/
            1, /*z thread count*/
        };
        updateIndArgs.SetData(bufferWithArgsData);

        UpdateCurrNextBuffers();
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
        gpuID_currAvailableIndices = Shader.PropertyToID("_CurrAvailableIndices");
        gpuID_nextAvailableIndices = Shader.PropertyToID("_NextAvailableIndices");
        gpuID_currAliveIndices = Shader.PropertyToID("_CurrAliveIndices");
        gpuID_nextAliveIndices = Shader.PropertyToID("_NextAliveIndices");
        gpuID_currData = Shader.PropertyToID("_CurrData");
        gpuID_nextData = Shader.PropertyToID("_NextData");
        gpuID_updateIndArgs = Shader.PropertyToID("_UpdateIndArgs");
    }

    /// <summary>
    /// Initializes only the _NextAvailableIndices Buffer, must swap buffers after use
    /// </summary>
    void DispatchParticleInitialize()
    {
        //initialize free indices buffer
        //change thread group count to div by 64
        AttachBuffersToKernel(initKernel, isEvenFrame);

        int initThreadGroups = Mathf.CeilToInt(maxParticles / 64f);
        particleComputeShader.Dispatch(initKernel, initThreadGroups, 1, 1);
    }

    void EmitParticle()
    {
        //this one should only have one thead in group
        AttachBuffersToKernel(emitKernel, isEvenFrame);
        particleComputeShader.Dispatch(emitKernel, 1, 1, 1);
    }

    void DispatchParticleUpdate()
    {
        particleComputeShader.SetBuffer(earlyUpdateKernel, gpuID_updateIndArgs, updateIndArgs);
        particleComputeShader.Dispatch(earlyUpdateKernel, 1, 1, 1);

        AttachBuffersToKernel(updateKernel, isEvenFrame);
        ComputeBuffer.CopyCount(currAliveIndices, updateIndArgs, 0);
        particleComputeShader.DispatchIndirect(updateKernel, updateIndArgs);
    }

    void DebugVisualize()
    {
        throw new NotImplementedException();
    }

    void UpdateCurrNextBuffers()
    {
        currAvailableIndices = isEvenFrame ? availableIndices1 : availableIndices2;
        nextAvailableIndices = isEvenFrame ? availableIndices2 : availableIndices1;
        currAliveIndices = isEvenFrame ? aliveIndices1 : aliveindices2;
        nextAliveIndices = isEvenFrame ? aliveindices2 : aliveIndices1;
        currData = isEvenFrame ? particleData1 : particleData2;
        nextData = isEvenFrame ? particleData2 : particleData1;
    }

    void AttachBuffersToKernel(int kernel, bool isEvenFrame)
    {
        particleComputeShader.SetBuffer(kernel, gpuID_currAvailableIndices, currAvailableIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_nextAvailableIndices, nextAvailableIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_currAliveIndices, currAliveIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_nextAliveIndices, nextAliveIndices);
        particleComputeShader.SetBuffer(kernel, gpuID_currData, currData);
        particleComputeShader.SetBuffer(kernel, gpuID_nextData, nextData);
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
}
