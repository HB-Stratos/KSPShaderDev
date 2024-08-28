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

*/

public class ParticleSystemManager : MonoBehaviour
{
    //TODO make the manager aquire all shaders automatically, for now manual asignment
    public List<string> particleComputeShaderNames;

    List<ComputeShader> computeShaders;

    private void Start()
    {
        //for some reason everything breaks if this is assigned outside of Start()
        particleComputeShaderNames = new List<string>()
        {
            "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
        };

        //TODO Modify this function to also grab the newly added CpuSources
        int bufferSize = GetShaderStructSize(particleComputeShaderNames[0], "Particle");
        if (bufferSize % 4 != 0)
            Debug.LogWarning("Generated buffer size is not a multiple of 4: " + bufferSize);

        int EmitKernel = computeShaders[0].FindKernel("_Emit");
        int UpdateKernel = computeShaders[0].FindKernel("_Update");
    }

    protected int GetShaderStructSize(string path, string structName)
    {
        string shaderFileContent = LoadTextFileFromPath(path);
        if (String.IsNullOrEmpty(shaderFileContent))
            throw new FileNotFoundException();

        var decls = ShaderParser.ParseTopLevelDeclarations(
            shaderFileContent,
            new HLSLParserConfig() { PreProcessorMode = PreProcessorMode.StripDirectives }
        );

        var visitor = new StructSizeVisitor();
        visitor.VisitMany(decls);

        return visitor.ParticleStructSize;
    }

    protected string LoadTextFileFromPath(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }
}
