using System.Collections;
using System.Collections.Generic;
using UnityShaderParser.HLSL;

class StructSizeVisitor : HLSLSyntaxVisitor
{
    // Output
    public int ParticleStructSize = 0;

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
                return 0; // Add whichever types you care about
        }
    }

    // Visitor impl
    public override void VisitStructTypeNode(StructTypeNode node)
    {
        // Only care about particle struct
        if (node.Name.GetName() == "Particle" && ParticleStructSize == 0)
        {
            foreach (var field in node.Fields)
            {
                switch (field.Kind)
                {
                    case ScalarTypeNode scalar:
                        ParticleStructSize += GetScalarTypeSize(scalar.Kind);
                        break;
                    case VectorTypeNode vector:
                        ParticleStructSize += vector.Dimension * GetScalarTypeSize(vector.Kind);
                        break;
                    case MatrixTypeNode matrix:
                        ParticleStructSize +=
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
}
