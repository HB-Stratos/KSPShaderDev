using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityShaderParser.Common;
using UnityShaderParser.HLSL;
using UnityShaderParser.HLSL.PreProcessor;

class ParticleShaderAnalyzer : HLSLSyntaxVisitor
{
    public ParticleShaderAnalyzer()
    {
        outputData = new OutputData() { particleStructSize = 0, cpuSources = new List<string>() };
    }

    OutputData outputData;

    public struct OutputData
    {
        public int particleStructSize;
        public List<string> cpuSources;
        public int customConstantsBufferSize;
    }

    public OutputData AnalyzeShader(string path)
    {
        string shaderFileContent = LoadTextFileFromPath(path);
        if (String.IsNullOrEmpty(shaderFileContent))
            throw new FileNotFoundException();

        var decls = ShaderParser.ParseTopLevelDeclarations(
            shaderFileContent,
            new HLSLParserConfig() { PreProcessorMode = PreProcessorMode.StripDirectives }
        );

        this.VisitMany(decls); //call with extensive side effects

        return outputData;
    }

    protected string LoadTextFileFromPath(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // Helpers
    int GetScalarTypeSize(ScalarType scalarType)
    {
        switch (scalarType)
        {
            case ScalarType.Int:
            case ScalarType.Uint:
            case ScalarType.Float:
                return 4;
            case ScalarType.Half:
                return 2;
            case ScalarType.Double:
                return 8;
            default:
                throw new NotImplementedException("Error, type not yet supported in get size");
        }
    }

    // Visitor impl
    public override void VisitStructTypeNode(StructTypeNode node)
    {
        // Only care about particle struct
        if (node.Name.GetName() == "Particle" && outputData.particleStructSize == 0)
        {
            foreach (var field in node.Fields)
            {
                switch (field.Kind)
                {
                    case ScalarTypeNode scalar:
                        outputData.particleStructSize += GetScalarTypeSize(scalar.Kind);
                        break;
                    case VectorTypeNode vector:
                        outputData.particleStructSize +=
                            vector.Dimension * GetScalarTypeSize(vector.Kind);
                        break;
                    case MatrixTypeNode matrix:
                        outputData.particleStructSize +=
                            matrix.FirstDimension
                            * matrix.SecondDimension
                            * GetScalarTypeSize(matrix.Kind);
                        break;
                    default:
                        break;
                }
            }
        }
        else
        {
            base.VisitStructTypeNode(node);
        }
    }

    public override void VisitVariableDeclaratorNode(VariableDeclaratorNode node)
    {
        if (
            node.Annotations.Count != 0
            && node.Annotations[0].Declarators.Count != 0
            && node.Annotations[0].Declarators[0].Name == "CpuSource"
            && node.Annotations[0].Declarators[0].Initializer.GetType()
                == typeof(ValueInitializerNode)
        )
        {
            ValueInitializerNode initializerNode = (ValueInitializerNode)
                node.Annotations[0].Declarators[0].Initializer;
            if (initializerNode.Expression.GetType() == typeof(LiteralExpressionNode))
            {
                LiteralExpressionNode literalExpressionNode = (LiteralExpressionNode)
                    initializerNode.Expression;

                outputData.cpuSources.Add(literalExpressionNode.Lexeme);
            }
        }
        else if ( //TODO make this prettier and fix up include files once I know how, this code does nothing so far.
            node.Annotations.Count != 0
            && node.Annotations[0].Declarators.Count != 0
            && node.Annotations[0].Declarators[0].Name == "requestedSize"
            && node.Annotations[0].Declarators[0].Initializer.GetType()
                == typeof(ValueInitializerNode)
        )
        {
            ValueInitializerNode initializerNode = (ValueInitializerNode)
                node.Annotations[0].Declarators[0].Initializer;
            if (initializerNode.Expression.GetType() == typeof(LiteralExpressionNode))
            {
                LiteralExpressionNode literalExpressionNode = (LiteralExpressionNode)
                    initializerNode.Expression;

                outputData.customConstantsBufferSize = Int32.Parse(literalExpressionNode.Lexeme);
            }
        }
        else
        {
            base.VisitVariableDeclaratorNode(node);
        }
    }
}
