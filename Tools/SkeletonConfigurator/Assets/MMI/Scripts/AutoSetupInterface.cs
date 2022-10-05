using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MMIUnity;
using System;

public class AutoSetupInterface : MonoBehaviour
{
    private GameObject plane;
    private GameObject sideplane;
    private GameObject rightHandPlane;

    public Transform root;
    public string ConfigFilePath = "";

    private TestIS testis;
    private JointMapper2 mapper;
    private FlyCam flycam;
    // Start is called before the first frame update
    void Start()
    {
        testis = this.GetComponent<TestIS>();
        flycam = GameObject.FindObjectOfType<FlyCam>();
        mapper = this.GetComponent<JointMapper2>();

        var ReMap = GameObject.Find("ReMap").GetComponent<Button>();
        ReMap.onClick.RemoveAllListeners();

        var ClearMap = GameObject.Find("ClearMap").GetComponent<Button>();
        ClearMap.onClick.RemoveAllListeners();

        var ApplyRetareting = GameObject.Find("ApplyRetargeting").GetComponent<Button>();
        ApplyRetareting.onClick.RemoveAllListeners();

        ReMap.onClick.AddListener(delegate { mapper.AutoRemap(); });


        ClearMap.onClick.AddListener(delegate { mapper.ClearMap(); });

        ApplyRetareting.onClick.AddListener(delegate { try
            {
                testis.AutoLoadConfigFile = false;
                testis.ResetBoneMap2();
                GameObject.Find("ConfirmMapping").GetComponent<Button>().interactable = true;
            }
            catch (Exception e)
            {
                GameObject.Find("ConfirmMapping").GetComponent<Button>().interactable = false;
                Debug.Log($"The retargeting could not be applied with Error:");
                Debug.LogException(e);
            }
        });


    }

    public void AfterMapping()
    {
        testis = this.GetComponent<TestIS>();
        FlyCam flycam = GameObject.FindObjectOfType<FlyCam>();
        var resetting = GameObject.Find("Resetting").transform;
        var changeView = GameObject.Find("ChangeViewPanel").transform;
        mapper = this.GetComponent<JointMapper2>();

        // Pelvis might face other direction than Root does. This is most probably no elegant (maybe even false) solution. 
        var topdownrotation = Quaternion.Euler(new Vector3(root.rotation.eulerAngles.x, root.GetChild(0).transform.rotation.eulerAngles.y, root.rotation.eulerAngles.x));
        var sideRotation = topdownrotation * Quaternion.Euler(0, 90, 0);
        var frontrotation = topdownrotation;
        //sideRotation *= Quaternion.Euler(90, 0, 0);

        //Add a plane inside the avatar this might be removed down the line
        plane = Instantiate(Resources.Load("Plane") as GameObject, root.position, frontrotation * Quaternion.Euler(90, 0, 0));
        sideplane = Instantiate(Resources.Load("Plane") as GameObject, root.position, sideRotation * Quaternion.Euler(90, 0, 0));

        //Scale plane so one rectangle is 10cm
        plane.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, .5f);
        sideplane.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, .5f);
        var planes = new List<GameObject>() { plane, sideplane };
        foreach (GameObject p in planes)
            p.SetActive(false);


        //Remove all Listeners, so when a new Rig is being used the Listeners won't malfunction.
        changeView.Find("FrontViewButton").GetComponent<Button>().onClick.RemoveAllListeners();
        changeView.Find("SideViewButton").GetComponent<Button>().onClick.RemoveAllListeners();
        changeView.Find("HandViewButton").GetComponent<Button>().onClick.RemoveAllListeners();

        resetting.Find("RealignButton").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("ResetPose").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("ResetBase").GetComponent<Button>().onClick.RemoveAllListeners() ;
        resetting.Find("SaveButton").GetComponent<Button>().onClick.RemoveAllListeners();
        //resetting.Find("LoadButton").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("PlayToggle").GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
        resetting.Find("Restart Clip").GetComponent<Button>().onClick.RemoveAllListeners();
        resetting.Find("ApplySkeleton").GetComponent<Button>().onClick.RemoveAllListeners();

        var toggle = GameObject.Find("SwitchRetargeting").GetComponent<Toggle>();
        toggle.onValueChanged.RemoveAllListeners();

        var BoneSlider = GameObject.Find("BoneMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
        BoneSlider.onValueChanged.RemoveAllListeners();

        var SkinSlider = GameObject.Find("SkinMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
        SkinSlider.onValueChanged.RemoveAllListeners();

        


        // Add listeners.
        changeView.Find("FrontViewButton").GetComponent<Button>().onClick.AddListener(delegate { GameObject.FindObjectOfType<FlyCamController>().ChangeProjection(frontrotation, planes); });
        changeView.Find("SideViewButton").GetComponent<Button>().onClick.AddListener(delegate { GameObject.FindObjectOfType<FlyCamController>().ChangeProjection(sideRotation, planes); });
        changeView.Find("HandViewButton").GetComponent<Button>().onClick.AddListener(delegate { RightHandPrep(topdownrotation); });


        resetting.Find("RealignButton").GetComponent<Button>().onClick.AddListener(delegate { testis.RealignSkeletons(); });
        resetting.Find("ResetPose").GetComponent<Button>().onClick.AddListener(delegate { testis.ResetBasePosture(); });
        resetting.Find("ResetBase").GetComponent<Button>().onClick.AddListener(delegate { testis.ResetBase(); });
        resetting.Find("SaveButton").GetComponent<Button>().onClick.AddListener(delegate { testis.InitiateSafe(); });
        //resetting.Find("LoadButton").GetComponent<Button>().onClick.AddListener(delegate { testis.LoadConfig(); mapper.SetJointMap(testis.jointMap); mapper.UpdateJointMap(); });
        resetting.Find("PlayToggle").GetComponent<Toggle>().onValueChanged.AddListener(delegate { testis.PlayPauseExampleClip(); });
        resetting.Find("Restart Clip").GetComponent<Button>().onClick.AddListener(delegate {testis.ResetFrameCount(); });
        resetting.Find("ApplySkeleton").GetComponent<Button>().onClick.AddListener(delegate {
            try
            {
                testis.AutoLoadConfigFile = false;
                testis.ResetBoneMap2(true, false);
            }
            catch (Exception e)
            {
                Debug.Log($"The retargeting could not be applied with Error:");
                Debug.LogException(e);
            }
        });
        toggle.onValueChanged.AddListener(delegate { testis.ToggleAvatar2ISRetargeting(toggle.isOn); });


        BoneSlider.onValueChanged.AddListener(delegate { this.GetComponent<BoneVisualization>().SetAlpha(BoneSlider.value); });


        SkinSlider.onValueChanged.AddListener(delegate { this.GetComponent<CharacterMeshRendererController>().alpha = SkinSlider.value; });
    }

    private void RightHandPrep(Quaternion topdownrotation)
    {
        // Find the right wrist.
        Transform rightWrist = GetComponent<JointMapper2>().GetJoint(MJointType.RightWrist);
        if (rightWrist == null)
        {
            Debug.Log("No Right Wrist assigned");
        }
        if (rightHandPlane == null && rightWrist != null){
            rightHandPlane = Instantiate(Resources.Load("Plane") as GameObject, rightWrist.position - new Vector3(0,0.2f,0), Quaternion.identity);
            rightHandPlane.transform.localScale *= .005f;
            var mat = rightHandPlane.GetComponent<MeshRenderer>().material;
            mat.SetColor("_Color", new Color(0f, .2f, 0.36470588235f));
            mat.mainTextureScale = new Vector2(10,10);
        }
        List<GameObject> list = new List<GameObject>() { rightHandPlane};
        flycam.GetComponent<FlyCamController>().ChangeToHandOrtho(rightWrist, list);
    }

    private void OnDestroy()
    {
        GameObject.Destroy(plane);
        GameObject.Destroy(sideplane);
        GameObject.Destroy(rightHandPlane);
    }
}
