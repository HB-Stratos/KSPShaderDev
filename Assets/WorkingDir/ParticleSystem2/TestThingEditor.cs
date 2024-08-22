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
        {
            var particleSystemManager = FindObjectOfType<ParticleSystemManager>();
            // particleSystemManager.Start(
            //     "Assets/WorkingDir/ParticleSystem2/TestParticle.compute",
            //     "Particle"
            // );
        }
    }
}
