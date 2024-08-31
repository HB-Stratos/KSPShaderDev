using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityShaderParser.Common;
using UnityShaderParser.HLSL;
using UnityShaderParser.HLSL.PreProcessor;

/*

General
- Aquire list of all Particle Compute Shaders
- Reflect on Shaders to determine how big the buffers need to be
- initialize buffers (per particle system / per particle type?) (buffers shouldbe immutable)
    - buffer should probably be initialized in a compute shader, on cpu is slow
    - for simplicity I'll go with one buffer per particle system for now
- in Update dispatch vertex shader(s) depending on set particle type, passing in the respective buffer



Compute Shader Architecture : Artist written
- Emit kernel creates new particle struct and writes it to the append buffer.
    - Emit can create multiple particles, parent and child (?)
- Update kernel checks lifetime, if particle still exists it moves
    - Needs to somehow provide a reference to parent particle if present.
    - Update should be able to spawn child particles (?) in rotating buffer design would need to know where there is space
- Clear kernel clears out the entire particle buffer (if this ends up being needed)
- Particle struct holds all data needed for a particle to be updated from frame to frame
- Particle buffer holds particle structs in the order they were created with a looping start point
    - therefore a newly added child particle can hold an index to its parent in its struct.
- Render struct holds rendering information that is written straight to the vertex buffer (?)
    - Update loop would call render function passing in instances of that struct to render
    - needs communication to CPU to determine amount of needed vertex shaders?

Compute Shader Architecture : Shared
- Sort kernel is given all particle buffers that currently exist and sorts them back to front relative to camera
    - sorting may struggle with trail renderers unless I can break them into parts (possibly sort per node in trail, then render each quad after another)
- Compute shader has to generate triangle data into a compute buffer.

https://stackoverflow.com/questions/41416272/set-counter-of-append-consume-buffer-on-gpu
Write data to an normal buffer into the position of thread ID, have a separate buffer of append type where particle is alive is pushed into with an index.
We consume the index buffer and access the normal buffer. As we consume the index buffer we can pass the index into the update function to spawn a child particle
Or we invert this and use the index buffer as a list of available indices we are allowed to write to

https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/sm5-object-rwstructuredbuffer-incrementcounter?redirectedfrom=MSDN
Incrementing the counter has a return value of the counter before incrementing.
increment/decrement counter should be atomic, so every particle will for sure get a different number.

A particle only lives for one frame, for a particle to persist it has to emit itself.

Buffers that exist per "system", each double buffered, one "current frame", one "next frame"
- Index buffer of currently existing particles: "current index buffer"
- Index buffer of free spots in the particle buffer: "free index buffer"
- Data buffer into which the index buffers link

Emitting a particle
- Happens by consuming an index from the free index buffer and writing to that index in the data buffer
- The index that was written to is then appended to the current index buffer

Particle Update
- on update, compute shaders according to the count in the current index buffers get dispatched
- each compute shader thread consumes one index from the current index buffer, reads it and executes its update function
- the update function emits itself again with the same method as emitting a particle
- the update function can emit multiple particles, parenting one to the next by temporarily storing the index that is being written to.

If multiple particle effects depend on each other, this manager must figure out their order


*/

public class ParticleSystemManager : MonoBehaviour
{
    //TODO make the manager aquire all shaders automatically, for now manual asignment
    List<string> particleComputeShaderNames;

    // struct ParticleBuffers
    // {
    //     ComputeShader computeShader;
    //     ComputeBuffer renderArguments;
    //     ComputeBuffer particleData;
    //     ComputeBuffer vertices;
    //     ComputeBuffer triangles;
    //     Material material;

    //     ParticleBuffers(
    //         ComputeShader computeShader,
    //         int maxParticleCout,
    //         int particleDataStructSize
    //     )
    //     {
    //         this.computeShader = computeShader;
    //         this.renderArguments = new ComputeBuffer(
    //             5,
    //             sizeof(int),
    //             ComputeBufferType.IndirectArguments
    //         );
    //         this.particleData = new ComputeBuffer(maxParticleCout, particleDataStructSize);
    //         this.vertices =
    //     }
    // }

    //TODO all this below here should really be a list of instances of a struct/class containing all this data. SoonTM
    [SerializeField]
    Material material;

    [SerializeField]
    ComputeShader computeShader;

    ComputeBuffer renderArguments;
    ComputeBuffer particleData;
    ComputeBuffer vertices;
    ComputeBuffer triangles;

    [SerializeField]
    [Tooltip("This is only refreshed when Start() is run")]
    int maxParticles;

    private void Start()
    {
        #region Safety Checks
        if (!SystemInfo.supportsComputeShaders)
            throw new NotSupportedException(
                "The Particle system requires Compute Shader support to function"
            );
        #endregion

        #region Temporary Setup
        //for some reason everything breaks if this is assigned outside of Start()
        particleComputeShaderNames = new List<string>()
        {
            "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
        };
        #endregion

        #region Parse Shader
        //TODO Modify this function to also grab the newly added CpuSources
        int particleDataStructSize = GetShaderStructSize(particleComputeShaderNames[0], "Particle");
        if (particleDataStructSize % 4 != 0)
            Debug.LogWarning(
                "Generated buffer size is not a multiple of 4: " + particleDataStructSize
            );

        //TODO this is currently bullshit code, kernels do not exist
        int EmitKernel = computeShader.FindKernel("_Emit");
        int UpdateKernel = computeShader.FindKernel("_Update");
        #endregion

        #region Set up Rendering

        renderArguments = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
        particleData = new ComputeBuffer(maxParticles, particleDataStructSize);
        //TODO remove this magic number for max vertices and tris
        vertices = new ComputeBuffer(1000, sizeof(float) * 3);
        triangles = new ComputeBuffer(1000, sizeof(int) * 3);

        #endregion
    }

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }

    protected int GetShaderStructSize(string path, string structName)
    {
        ParticleShaderAnalyzer analyzer = new ParticleShaderAnalyzer();
        return analyzer.AnalyzeShader(path).particleStructSize;
    }

    protected string LoadTextFileFromPath(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
