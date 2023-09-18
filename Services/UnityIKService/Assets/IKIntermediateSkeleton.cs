using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKIntermediateSkeleton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ApplyTransforms(Transform isVisualization)
    {
        Transform[] referenceTransforms = isVisualization.GetComponentsInChildren<Transform>();

        Transform[] targetTransforms = this.GetComponentsInChildren<Transform>();

        for(int i=0; i< referenceTransforms.Length; i++)
        {
            targetTransforms[i].position = referenceTransforms[i].position;
            targetTransforms[i].rotation = referenceTransforms[i].rotation;
        }
    }

    public void CopyTransforms(Transform target)
    {

    }
}
