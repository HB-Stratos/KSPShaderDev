Shader "Hidden/ParticleDebug"
{
    //TODO This shader only supports forward rendering, kinda silly
    //show values to edit in inspector
    Properties
    {
        [HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM

            #pragma enable_d3d11_debug_symbols

            //include useful shader functions
            #include "UnityCG.cginc"

            //define vertex and fragment shader functions
            #pragma vertex vert
            #pragma fragment frag

            //tint of the texture
            fixed4 _Color;

            struct Particle
            {
                float3 position; //must be first entry by convention
                float life;
                float3 velocity;
                int index; //index of particle in the data buffer, used for parenting
                int parent;
            };

            //buffers
            StructuredBuffer<Particle> _ParticleData;
            StructuredBuffer<int> _MeshTriangles;
            StructuredBuffer<float3> _MeshVertexPositions;
            StructuredBuffer<int> _AliveIndices;

            //the vertex shader function
            float4 vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID) : SV_POSITION
            {
                //get vertex position
                int positionIndex = _MeshTriangles[vertex_id];
                float3 position = _MeshVertexPositions[positionIndex];
                //add sphere position
                position += _ParticleData[_AliveIndices[instance_id]].position;
                //convert the vertex position from world space to clip space
                return mul(UNITY_MATRIX_VP, float4(position, 1));
            }

            //the fragment shader function
            fixed4 frag() : SV_TARGET
            {
                //return the final color to be drawn on screen
                return _Color;
            }
            
            ENDCG
        }
    }
    Fallback "VertexLit"
}
