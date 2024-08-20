using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/*

- Aquire list of all Particle Compute Shaders
- Reflect on Shaders to determine how big the buffers need to be
- initialize buffers (per particle system / per particle type?) (buffers shouldbe immutable)#
- in Update dispatch vertex shader(s)

*/

public class ParticleSystemManager : MonoBehaviour
{
    //TODO make the manager aquire all shaders automatically, for now manual asignment
    public List<ComputeShader> computeShaders;
    public Shader shader;

    private void Start()
    {
        // computeShaders[0];

        Shader.PropertyToID("mrow");
    }

    private void Update()
    {
        //Dispatch vertex shader (May need to decide on general type of particle, billboard / ribbon / ribbon volume / etc)
    }
}
