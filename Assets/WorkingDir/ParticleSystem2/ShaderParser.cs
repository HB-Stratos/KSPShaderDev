using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityShaderParser.Common;
using UnityShaderParser.HLSL;
using UnityShaderParser.HLSL.PreProcessor;

public class ShaderParser : HLSLSyntaxVisitor<string>
{
    /// <summary>
    /// -
    /// </summary>
    /// <param name="path">Unity relative Path of the shader file to parse (must include file extension)</param>
    /// <param name="structName">Target struct</param>
    /// <returns>Size of target struct in bytes</returns>
    public int? GetHLSLShaderStructBufferSize(string path, string structName)
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

        int totalByteSize = parsed
            .Where(node => node.GetType() == typeof(StructDefinitionNode))
            .Where(node =>
                node.Tokens.First(token => token.Kind == TokenKind.IdentifierToken).Identifier
                == structName
            )
            .First()
            .Tokens.Where(token => Enum.GetName(typeof(TokenKind), token.Kind).Contains("Keyword"))
            .Select(token => GetShaderVarSize(token))
            .Sum();

        return totalByteSize;
    }

    //TODO finish this
    public int? GetHLSLShaderStructBufferSize2(string path, string structName)
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

        var test = parsed[0];
        return null;
    }

    #region VisitorImpl

    public override string VisitScalarTypeNode(ScalarTypeNode node)
    {
        string enumName = PrintingUtil.GetEnumName(node.Kind);
        Debug.Log(enumName);
        return enumName;
    }

    #endregion

    public string LoadTextFileFromPath(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private int GetShaderVarSize(ScalarTypeNode node)
    {
        throw new NotImplementedException();
    }

    private int GetShaderVarSize(Token<UnityShaderParser.HLSL.TokenKind> token)
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
}
