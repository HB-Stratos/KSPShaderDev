using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TrailRenderTest : MonoBehaviour
{
    Camera targetCamera;

    [Range(1, 100)]
    public int trailLength = 10;

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        targetCamera = Camera.main;

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        ConfigureMaterial();
    }

    // Update is called once per frame
    void Update()
    {
        GenerateMesh();
        UpdateMesh();
    }

    void GenerateMesh()
    {
        // vertices = new Vector3[]
        // {
        //     new Vector3(0, 0, 0),
        //     new Vector3(0, 0, 1),
        //     new Vector3(1, 0, 0),
        //     new Vector3(1, 0, 1)
        // };

        // triangles = new int[] { 0, 1, 2, 1, 3, 2 };

        vertices = new Vector3[2 * trailLength + 2];
        triangles = new int[2 * trailLength * 3];

        Vector3 backwardVector = new Vector3(0, 0, 1);

        // Debug.DrawRay(targetCamera.transform.position, cameraVector, Color.red);
        // Debug.DrawRay(Vector3.zero, backwardVector, Color.red);

        int[] triangleOrder = new int[] { 0, 1, 2, 1, 3, 2 };

        for (int i = 0; i <= trailLength; i++)
        {
            Vector3 currentPositionOnTrail = backwardVector + Vector3.up * 0.02f * i;
            Vector3 vectorToCamera = currentPositionOnTrail - targetCamera.transform.position;

            Vector3 sideVector = Vector3.Cross(vectorToCamera, backwardVector).normalized;

            float vectorAngle = Vector3.Angle(vectorToCamera, currentPositionOnTrail);

            vertices[i * 2 + 0] = -sideVector * 0.5f + backwardVector * i;
            vertices[i * 2 + 1] = sideVector * 0.5f + backwardVector * i;

            if (i == 0)
                continue;

            for (int j = 0; j < 6; j++)
            {
                triangles[(i - 1) * 6 + j] = triangleOrder[j] + 2 * (i - 1);
            }
        }
    }

    void ConfigureMaterial()
    {
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
