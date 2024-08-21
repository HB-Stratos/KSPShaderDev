using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityShaderParser.HLSL;
using UnityShaderParser.HLSL.PreProcessor;

public static class ShaderParser
{
    // Start is called before the first frame update
    public static int? GetHLSLShaderStructBufferSize(string path)
    {
        string shaderFileContent = LoadTextFileFromPath(path);
        if (String.IsNullOrEmpty(shaderFileContent))
            throw new FileNotFoundException();

        var tokens = HLSLLexer.Lex(shaderFileContent, null, null, false, out _);

        var parsed = HLSLParser.ParseTopLevelDeclarations(
            tokens,
            new HLSLParserConfig() { PreProcessorMode = PreProcessorMode.StripDirectives },
            out _,
            out _
        );

        var particleStructNode = parsed
            .Where(node => node.GetType() == typeof(StructDefinitionNode))
            .Where(node =>
                node.Tokens.First(token => token.Kind == TokenKind.IdentifierToken).Identifier
                == "Particle"
            )
            .ToArray()[0];
        var particleStructMembers = particleStructNode
            .Tokens.Where(token => Enum.GetName(typeof(TokenKind), token.Kind).Contains("Keyword"))
            .ToArray();

        var test = HLSLParser.ParseExpression(
            particleStructNode.Tokens,
            new HLSLParserConfig(),
            out _,
            out _
        );

        var test2 = test.GetPrettyPrintedCode();

        return null;
    }

    private static string LoadTextFileFromPath(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
