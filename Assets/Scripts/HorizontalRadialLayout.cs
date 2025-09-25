using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;

public class HorizontalRadialLayout : MonoBehaviour
{
    public Transform[] objects;    // le 3 sfere
    public float radius = 1.5f;    // raggio del semicerchio
    public float angleRange = 90f; // apertura in gradi

    void Update()
    {
        if (objects == null || objects.Length == 0) return;

        // centro davanti alla camera
        Vector3 center = Camera.main.transform.position + Camera.main.transform.forward * radius;

        for (int i = 0; i < objects.Length; i++)
        {
            float angle = -angleRange / 2f + (angleRange / (objects.Length - 1)) * i;
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 pos = center + rot * (Vector3.right * radius * 0.5f);

            objects[i].position = pos;
            objects[i].LookAt(Camera.main.transform); // facoltativo: sempre rivolti verso di te
        }
    }
}
