using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GpuParticleSystem2 : MonoBehaviour
{
    [SerializeField]
    int maxParticles;

    ComputeShader particleCompute;

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
    int updateKernel;

    int gpuID_currAvailableIndices;
    int gpuID_nextAvailableIndices;
    int gpuID_currAliveIndices;
    int gpuID_nextAliveIndices;
    int gpuID_currData;
    int gpuID_nextData;

    ComputeBuffer currAvailableIndices;
    ComputeBuffer nextAvailableIndices;
    ComputeBuffer currAliveIndices;
    ComputeBuffer nextAliveIndices;
    ComputeBuffer currData;
    ComputeBuffer nextData;

    void Start()
    {
        availableIndices1 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append);
        availableIndices2 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append);

        aliveIndices1 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append);
        aliveindices2 = new ComputeBuffer(maxParticles, sizeof(uint), ComputeBufferType.Append);

        ParticleShaderAnalyzer.OutputData particleShaderData =
            new ParticleShaderAnalyzer().AnalyzeShader(
                "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
            );

        particleData1 = new ComputeBuffer(maxParticles, particleShaderData.particleStructSize);
        particleData2 = new ComputeBuffer(maxParticles, particleShaderData.particleStructSize);

        updateIndArgs = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        int[] bufferWithArgsData = new int[]
        {
            0, /*vertex count per instance*/
            1, /*instance count*/
            0, /*start vertex location*/
            0, /*start instance location*/
        };
        updateIndArgs.SetData(bufferWithArgsData);

        initKernel = particleCompute.FindKernel("Init");
        emitKernel = particleCompute.FindKernel("Emit");
        updateKernel = particleCompute.FindKernel("Update");

        gpuID_currAvailableIndices = Shader.PropertyToID("_CurrAvailableIndices");
        gpuID_nextAvailableIndices = Shader.PropertyToID("_NextAvailableIndices");
        gpuID_currAliveIndices = Shader.PropertyToID("_CurrAliveIndices");
        gpuID_nextAliveIndices = Shader.PropertyToID("_NextAliveIndices");
        gpuID_currData = Shader.PropertyToID("_CurrData");
        gpuID_nextData = Shader.PropertyToID("_NextData");

        //initialize free indices buffer
        //change thread group count to div by 64
        AttachBuffersToKernel(initKernel, isEvenFrame);
        particleCompute.Dispatch(initKernel, maxParticles, 1, 1);
    }

    void Update()
    {
        isEvenFrame = !isEvenFrame;
        UpdateCurrNextBuffers();

        //Add a CPU emit function //Emit particle and consume free indices buffer
        EmitParticle();
        //Dispatch update kernel indirect
        AttachBuffersToKernel(updateKernel, isEvenFrame);
        ComputeBuffer.CopyCount(currAliveIndices, updateIndArgs, 0);
        particleCompute.DispatchIndirect(updateKernel, updateIndArgs);
        //create material and dispatch vertex shader for debug visualisation
        DebugVisualize();
    }

    void EmitParticle()
    {
        //this one should only have one thead in group
        AttachBuffersToKernel(emitKernel, isEvenFrame);
        particleCompute.Dispatch(emitKernel, 1, 1, 1);
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
        particleCompute.SetBuffer(kernel, gpuID_currAvailableIndices, currAvailableIndices);
        particleCompute.SetBuffer(kernel, gpuID_currAvailableIndices, currAvailableIndices);
        particleCompute.SetBuffer(kernel, gpuID_currAliveIndices, currAliveIndices);
        particleCompute.SetBuffer(kernel, gpuID_nextAliveIndices, nextAliveIndices);
        particleCompute.SetBuffer(kernel, gpuID_currData, currData);
        particleCompute.SetBuffer(kernel, gpuID_nextData, nextData);
    }
}
