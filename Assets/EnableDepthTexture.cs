using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableDepthTexture : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    Camera cam = GetComponent<Camera>();
    cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;

    Camera.main.depthTextureMode = DepthTextureMode.Depth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
