using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityShaderParser;
using UnityShaderParser.Common;
using UnityShaderParser.HLSL;
using UnityShaderParser.HLSL.PreProcessor;
using UnityShaderParser.ShaderLab;

/*

- Aquire list of all Particle Compute Shaders
- Reflect on Shaders to determine how big the buffers need to be
- initialize buffers (per particle system / per particle type?) (buffers shouldbe immutable)#
- in Update dispatch vertex shader(s)

*/

public class ParticleSystemManager : MonoBehaviour
{
    //TODO make the manager aquire all shaders automatically, for now manual asignment
    public List<string> particleComputeShaderNames;

    List<ComputeShader> computeShaders;

    private void Start()
    {
        //TODO make this dynamic
        particleComputeShaderNames = new List<string>()
        {
            "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
        };

        int bufferSize = ShaderParser
            .GetHLSLShaderStructBufferSize(particleComputeShaderNames[0], "Particle")
            .Value;

        if (bufferSize % 4 != 0)
            Debug.LogWarning("Generated buffer size is not a multiple of 4: " + bufferSize);
    }

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }
}
