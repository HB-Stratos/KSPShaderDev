using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(TestThing))]
public class TestThingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Run test code"))
            ShaderParser.GetHLSLShaderStructBufferSize(
                "Assets/WorkingDir/ParticleSystem2/TestParticle.compute"
            );
    }
}
