using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MMIUnity;

public class AutoSetupInterface : MonoBehaviour
{
    private GameObject plane;
    private GameObject sideplane;
    private GameObject rightHandPlane;
    public Transform root;
    public string ConfigFilePath = "";
    // Start is called before the first frame update
    void Start()
    {
        TestIS testis = this.GetComponent<TestIS>();
        FlyCam flycam = GameObject.FindObjectOfType<FlyCam>();
        var resetting = GameObject.Find("Resetting").transform;
        var changeView = GameObject.Find("ChangeViewPanel").transform;
        JointMapper2 mapper = this.GetComponent<JointMapper2>();

        // Pelvis might face other direction than Root does. This is most probably no elegant (maybe even false) solution. 
        var topdownrotation = Quaternion.Euler(new Vector3(root.rotation.eulerAngles.x, root.GetChild(0).transform.rotation.eulerAngles.y, root.rotation.eulerAngles.x));
        var sideRotation = topdownrotation * Quaternion.Euler(0, 90, 0);
        var frontrotation = topdownrotation;
        //sideRotation *= Quaternion.Euler(90, 0, 0);

        //Add a plane inside the avatar this might be removed down the line
        plane = Instantiate(Resources.Load("Plane") as GameObject, root.position, frontrotation * Quaternion.Euler(90,0,0));
        sideplane = Instantiate(Resources.Load("Plane") as GameObject, root.position, sideRotation * Quaternion.Euler(90,0,0));
        
        //Scale plane so one rectangle is 10cm
        plane.GetComponent<MeshRenderer>().material.color = new Color(1,1,1,.5f);
        sideplane.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, .5f);
        var planes = new List<GameObject>() {plane, sideplane };
        foreach (GameObject p in planes)
            p.SetActive(false);


        //Remove all Listeners, so when a new Rig is being used the Listeners won't malfunction.
        changeView.Find("FrontViewButton").GetComponent<Button>().onClick.RemoveAllListeners();
        changeView.Find("SideViewButton").GetComponent<Button>().onClick.RemoveAllListeners();
        changeView.Find("HandViewButton").GetComponent<Button>().onClick.RemoveAllListeners();

        resetting.Find("RealignButton").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("ResetPose").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("SaveButton").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("LoadButton").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("PlayButton").GetComponent<Button>().onClick.RemoveAllListeners();

        var toggle = GameObject.Find("SwitchRetargeting").GetComponent<Toggle>();
        toggle.onValueChanged.RemoveAllListeners();

        var BoneSlider = GameObject.Find("BoneMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
        BoneSlider.onValueChanged.RemoveAllListeners();

        var SkinSlider = GameObject.Find("SkinMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
        SkinSlider.onValueChanged.RemoveAllListeners();

        var ReMap = GameObject.Find("ReMap").GetComponent<Button>();
        ReMap.onClick.RemoveAllListeners();

        var ClearMap = GameObject.Find("ClearMap").GetComponent<Button>();
        ClearMap.onClick.RemoveAllListeners();

        var ApplyRetareting = GameObject.Find("ApplyRetargeting").GetComponent<Button>();
        ApplyRetareting.onClick.RemoveAllListeners();


        // Add listeners.
        changeView.Find("FrontViewButton").GetComponent<Button>().onClick.AddListener(delegate { GameObject.FindObjectOfType<FlyCamController>().ChangeProjection(frontrotation, planes); });
        changeView.Find("SideViewButton").GetComponent<Button>().onClick.AddListener(delegate { GameObject.FindObjectOfType<FlyCamController>().ChangeProjection(sideRotation, planes); });
        changeView.Find("HandViewButton").GetComponent<Button>().onClick.AddListener(delegate { RightHandPrep(testis, topdownrotation); });


        resetting.Find("RealignButton").GetComponent<Button>().onClick.AddListener(delegate { testis.RealignSkeletons(); });
        resetting.Find("ResetPose").GetComponent<Button>().onClick.AddListener(delegate { testis.ResetBasePosture(); });
        resetting.Find("SaveButton").GetComponent<Button>().onClick.AddListener(delegate { testis.SaveConfig(); });
        resetting.Find("LoadButton").GetComponent<Button>().onClick.AddListener(delegate { testis.LoadConfig(); mapper.SetJointMap(testis.jointMap);  mapper.UpdateJointMap(); });
        resetting.Find("PlayButton").GetComponent<Button>().onClick.AddListener(delegate { testis.PlayExampleClip(); });

        toggle.onValueChanged.AddListener(delegate { testis.ToggleAvatar2ISRetargeting(toggle); });


        BoneSlider.onValueChanged.AddListener(delegate { this.GetComponent<BoneVisualization>().SetAlpha(BoneSlider.value); });


        SkinSlider.onValueChanged.AddListener(delegate { this.GetComponent<CharacterMeshRendererController>().alpha = SkinSlider.value; });


        ReMap.onClick.AddListener(delegate { mapper.AutoRemap(); });


        ClearMap.onClick.AddListener(delegate { mapper.ClearMap(); });

        ApplyRetareting.onClick.AddListener(delegate { testis.ResetBoneMap2(); });

        var cam = GameObject.Find("Main Camera");
        if(cam != null)
        {
            var flyCam = cam.GetComponent<FlyCam>();
            if(flyCam != null)
            {
                // target Pelvis for more central fixation of the Avatar
                flyCam.target = root.GetChild(0);
            }
        }


    }

    private void RightHandPrep(TestIS testis, Quaternion topdownrotation)
    {
        // Find the right wrist.
        var map = testis.jointMap;
        Transform rightWrist = null;
        foreach (KeyValuePair<MJointType, Transform> pair in map.GetJointMap())
        {
            if (pair.Key == MJointType.RightWrist)
            {
                if (pair.Value == null)
                {
                    Debug.Log("No Right Wrist assigned");
                    break;
                }
                rightWrist = pair.Value;

            }
        }
        if (rightHandPlane == null && rightWrist != null){
            rightHandPlane = Instantiate(Resources.Load("Plane") as GameObject, rightWrist.position, topdownrotation);
            rightHandPlane.transform.localScale *= .1f;
        }
    }

    private void OnDestroy()
    {
        GameObject.Destroy(plane);
        GameObject.Destroy(sideplane);
        GameObject.Destroy(rightHandPlane);
    }
}
