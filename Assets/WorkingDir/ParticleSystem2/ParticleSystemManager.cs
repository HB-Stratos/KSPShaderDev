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
        string shaderFilePath = "Assets/WorkingDir/ParticleSystem2/TestParticle.compute";
        string shaderFileContent = "";
        if (File.Exists(shaderFilePath))
            shaderFileContent = File.ReadAllText(shaderFilePath);
        else
            Debug.LogWarning("File not found at: " + shaderFilePath);

        var hlslParserConfig = new HLSLParserConfig()
        {
            PreProcessorMode = PreProcessorMode.StripDirectives
        };
        var hlslTokens = HLSLLexer.Lex(shaderFileContent, null, null, true, out _);
        var hlslParsed = HLSLParser.ParseTopLevelDeclarations(
            hlslTokens,
            hlslParserConfig,
            out _,
            out _
        );

        var test = hlslParsed[0].GetType();
        var tes2 = test == typeof(StructDefinitionNode);
        var tes3 = hlslParsed.Where(n => n.GetType() == typeof(StructDefinitionNode));
        var tes4 = tes3.ToList();
        var tes5 = tes4[0];
        var tes6 = HLSLParser.ParseStatements(tes5.Tokens, hlslParserConfig, out _, out _);
        var tes7 = HLSLParser.ParseExpression(tes5.Tokens, hlslParserConfig, out _, out _);
        var tes8 = tes5.Tokens.Count(t => t.Kind == UnityShaderParser.HLSL.TokenKind.Float3Keyword);

        var hlslStructs = hlslParsed
            .Where(node => node.GetType() == typeof(StructDefinitionNode))
            .ToList();
        var hlslParticleStruct = hlslStructs
            .Where(node =>
                node.Tokens.First(token =>
                    token.Kind == UnityShaderParser.HLSL.TokenKind.IdentifierToken
                ).Identifier == "Particle"
            )
            .ToArray()[0];
        var hlslParticleStructKeywords = hlslParticleStruct
            .Tokens.Where(token =>
                Enum.GetName(typeof(UnityShaderParser.HLSL.TokenKind), token.Kind)
                    .Contains("Keyword")
            )
            .Skip(1) // skip the struct keyword itself
            .ToArray();

        int bufferSize = 0;
        foreach (Token<UnityShaderParser.HLSL.TokenKind> token in hlslParticleStructKeywords)
        {
            bufferSize += GetShaderVarSize(token);
        }
        if (bufferSize % 4 != 0)
            Debug.LogWarning("Generated buffer size is not a multiple of 4: " + bufferSize);

        // hlslParsed = HLSLParser.ParseStatements(hlslParsed, hlslParserConfig, out _, out _);
        // var hlslPrinter = new HLSLPrinter();
        // hlslPrinter.Visit(hlslParsed[0]);
        // hlslPrinter.
        // var test = hlslPrinter.Text;
    }

    int GetShaderVarSize(Token<UnityShaderParser.HLSL.TokenKind> token)
    {
        string tokenName = Enum.GetName(typeof(UnityShaderParser.HLSL.TokenKind), token.Kind)
            .Replace("Keyword", "");
        var dimensionRegex = new Regex(@"(?<!\d)(\d)(x\d)?");
        int sizeMultiplier = 1;
        var match = dimensionRegex.Match(tokenName);
        int dimSize;
        if (Int32.TryParse(match.Groups[1].Value, out dimSize))
        {
            sizeMultiplier = dimSize;
            if (
                !String.IsNullOrEmpty(match.Groups[2].Value)
                && Int32.TryParse(match.Groups[2].Value.Substring(1), out dimSize)
            ) //remove 1 to remove the leading x
            {
                sizeMultiplier *= dimSize;
            }
        }

        string tokenNameNoDim = String.IsNullOrEmpty(match.Value)
            ? tokenName
            : tokenName.Replace(match.Value, "");
        int baseByteSize = 0;
        if (sizeInBytes.TryGetValue(tokenNameNoDim.ToLower(), out baseByteSize) == false)
            Debug.LogError("No Memory size listed for type" + tokenNameNoDim);

        return baseByteSize * sizeMultiplier;
    }

    private readonly Dictionary<string, int> sizeInBytes = new Dictionary<string, int>
    {
        { "int", 32 / 8 },
        { "uint", 32 / 8 },
        { "dword", 32 / 8 },
        { "half", 16 / 8 },
        { "float", 32 / 8 },
        { "double", 64 / 8 },
    };

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }
}
