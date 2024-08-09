using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TrailParticles : MonoBehaviour
{
    //All particles will be handled in local space and faked into global space

    //General
    public Shader shader;

    public float maxDistanceBeforeNewParticle = 1;
    public float minDistanceBetweenParticles = 0.5f;

    public Vector3 emitterPositionOffset = Vector3.zero;
    public Vector3 emitterRotationOffset = Vector3.zero;

    //Particle Settings
    public bool debugParticles = true;
    public bool drawParticles = true;
    public float emissionVelocity = 5;

    public float lifetime = 30f;

    //Connecting Trail Settings
    public bool drawConnectingTrail = true;
    public float trailWidth = 1;
    public int quadTrailBufferSize = 100;

    //Particle Physics
    public bool hasPhysics = true;

    public bool inheritEmitterVelocity = true;

    public bool hasCollision = true;

    public float density = 1.225f;
    public float dragCoefficient = 0.5f;
    public float gravity = 9.81f;

    //Private things
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    Mesh trailMesh;
    Vector3[] trailVertices;
    int[] trailTriangles;

    List<Particle> particles = new List<Particle>();

    GameObject spawner;
    Transform emitterTransform;
    Vector3 emitterDirection;

    void Start()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        trailMesh = new Mesh();
        meshFilter.mesh = trailMesh;
        shader = Shader.Find("Standard");
        meshRenderer.material = new Material(shader);

        spawner = gameObject;
        emitterTransform = spawner.transform;
        emitterDirection = emitterTransform.TransformDirection(Vector3.forward);

        SetUpMeshArrays();
    }

    void Update()
    {
        SpawnNewParticleIfNeeded();
        HandleParticleMovement();
        UpdateParticleMeshes();
        UpdateTrailMesh();
    }

    void OnDrawGizmos()
    {
        if (!debugParticles)
            return;
        foreach (Particle particle in particles)
        {
            Gizmos.DrawSphere(particle.position, 0.1f);
        }
    }

    private void SetUpMeshArrays()
    {
        trailVertices = new Vector3[quadTrailBufferSize * 2 + 2];
        trailTriangles = new int[quadTrailBufferSize * 6]; //consider making this a gpu vertex strip at some point
    }

    private void SpawnNewParticleIfNeeded()
    {
        if (
            particles.Any()
            && Vector3.SqrMagnitude(emitterPositionOffset - particles.Last().position)
                < Mathf.Pow(maxDistanceBeforeNewParticle, 2)
        )
            return;

        particles.Add(new Particle(emissionVelocity * emitterDirection, emitterPositionOffset, 0));
    }

    private void HandleParticleMovement()
    {
        for (int i = particles.Count - 1; i >= 0; i--) //Iterate backwards to avoid indexing issues when removing
        {
            Particle particle = particles[i];

            if (particle.lifetime > lifetime)
            {
                particles.RemoveAt(i);
                continue;
            }
            if (
                i != particles.Count - 1
                && Vector3.Distance(particles[i + 1].position, particle.position)
                    <= minDistanceBetweenParticles
            )
            {
                particles.RemoveAt(i + 1);
            }

            particle.lifetime += Time.deltaTime;

            particle.position += particle.velocity * Time.deltaTime;

            //TESTING
            particle.velocity += Vector3.up * 0.1f * Time.deltaTime;

            if (dragCoefficient != 0)
                particle.velocity *=
                    1 - dragCoefficient * Time.deltaTime * particle.velocity.magnitude;

            particles[i] = particle;
        }
    }

    private void UpdateParticleMeshes() { }

    private void UpdateTrailMesh()
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        int[] triangleOrder = new int[] { 0, 1, 2, 1, 3, 2 };
        for (int i = 0; i < particles.Count; i++)
        {
            Vector3 vectorToCamera = particles[i].position - cameraPosition;
            Vector3 sideVector = Vector3.Cross(vectorToCamera, particles[i].velocity).normalized;

            trailVertices[2 * i + 0] = 0.5f * trailWidth * sideVector + particles[i].position;
            trailVertices[2 * i + 1] = 0.5f * trailWidth * -sideVector + particles[i].position;

            if (i == 0)
                continue;

            for (int j = 0; j < 6; j++)
            {
                trailTriangles[(i - 1) * 6 + j] = triangleOrder[j] + 2 * (i - 1);
            }
        }

        trailMesh.Clear();
        trailMesh.vertices = trailVertices;
        trailMesh.triangles = trailTriangles;
        trailMesh.RecalculateNormals();
    }

    // void FixedUpdate() { } //consider this maybe? or at least do raycasts here
}

struct Particle
{
    public Vector3 velocity;
    public Vector3 position;
    public float lifetime;

    public Particle(Vector3 velocity, Vector3 position, float lifetime)
    {
        this.velocity = velocity;
        this.position = position;
        this.lifetime = lifetime;
    }
}
