using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicComputeSpheres : MonoBehaviour
{
    public int SphereAmount = 17;
    public ComputeShader Shader;
    public GameObject Prefab;

    ComputeBuffer resultBuffer;
    int kernel;
    uint threadGroupSize;
    Vector3[] output;

    Transform[] instances;

    // Start is called before the first frame update
    void Start()
    {
        kernel = Shader.FindKernel("Spheres");
        Shader.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);

        resultBuffer = new ComputeBuffer(SphereAmount, sizeof(float) * 3);
        output = new Vector3[SphereAmount];

        instances = new Transform[SphereAmount];
        for (int i = 0; i < SphereAmount; i++)
        {
            instances[i] = Instantiate(Prefab, transform).transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetBuffer(kernel, "Result", resultBuffer);
        Shader.SetFloat("Time", Time.time);
        int threadGroups = (int)((SphereAmount + (threadGroupSize - 1)) / threadGroupSize);
        Shader.Dispatch(kernel, threadGroups, 1, 1);
        resultBuffer.GetData(output);

        for (int i = 0; i < instances.Length; i++)
            instances[i].localPosition = output[i];
    }

    void OnDestroy()
    {
        resultBuffer.Dispose();
    }
}
