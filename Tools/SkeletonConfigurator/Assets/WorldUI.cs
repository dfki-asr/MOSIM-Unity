using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldUI : MonoBehaviour
{
    public Transform Target;

    public bool left = true;

    private void Start()
    {
        this.transform.parent.GetComponent<Canvas>().worldCamera = FindObjectOfType<Camera>();
        this.transform.Find("BoneBackground/Bone").gameObject.GetComponent<Text>().text = Target.name;
        if (left)
            this.transform.Find("HandBackground/Name").GetComponent<Text>().text = "Left Hand";
        else
            this.transform.Find("HandBackground/Name").GetComponent<Text>().text = "Right Hand";

    }

    // Update is called once per frame
    void Update()
    {
        if(Target != null)
            this.transform.position = Target.position;
        this.transform.LookAt(GameObject.FindObjectOfType<Camera>().transform);
        this.transform.Rotate(new Vector3(0,180,0));
        //this.transform.rotation = Target.rotation;
    }
}
