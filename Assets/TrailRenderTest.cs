using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TrailRenderTest : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        GenerateMesh();
        ConfigureMaterial();
        UpdateMesh();
    }

    // Update is called once per frame
    void Update() { }

    void GenerateMesh()
    {
        vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0)
        };

        triangles = new int[] { 0, 1, 2 };
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
    }
}
