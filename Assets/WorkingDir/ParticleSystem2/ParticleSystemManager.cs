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

        var shaderParser = new ShaderParser();

        int bufferSize = shaderParser
            .GetHLSLShaderStructBufferSize(particleComputeShaderNames[0], "Particle")
            .Value;

        if (bufferSize % 4 != 0)
            Debug.LogWarning("Generated buffer size is not a multiple of 4: " + bufferSize);
    }

    //TODO remove this
    public void TestCallShaderParser(string path, string nodeName)
    {
        var shaderParser = new ShaderParser();
        string shaderFileContent = shaderParser.LoadTextFileFromPath(path);
        if (String.IsNullOrEmpty(shaderFileContent))
            throw new FileNotFoundException();

        var tokens = HLSLLexer.Lex(shaderFileContent, null, null, false, out _);

        var parsed = HLSLParser
            .ParseTopLevelDeclarations(
                tokens,
                new HLSLParserConfig() { PreProcessorMode = PreProcessorMode.StripDirectives },
                out _,
                out _
            )
            .First();
        var test = shaderParser.VisitMany(new List<HLSLSyntaxNode>() { parsed });
    }

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }
}
