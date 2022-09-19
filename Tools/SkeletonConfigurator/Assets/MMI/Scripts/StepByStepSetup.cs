using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using MMIUnity;
using SimpleFileBrowser;

public class StepByStepSetup : MonoBehaviour
{
    private enum State
    {
        RootSelect,
        RotationAdjust,
        JointMap,
        IsSetup
    }

    #region PublicVars
    public GameObject BonePrefab;
    public GameObject EndPrefab;
    public GameObject GameJointPrefab;
    #endregion

    #region General Private Variables
    private Transform _root;
    private Transform _pelvis;
    private Image _Background;
    private FlyCam _flycam;
    private State _state = State.RootSelect;
    #endregion

    #region SetupFirstStep
    private Dropdown _rootDrop;
    private Dropdown _pelvDrop;
    private Button _ConfirmSetup;
    private GameObject _ChooseRot;
    #endregion

    #region SetupSecondStep
    private GameObject _ChangeRot;

    private Button _BigTurnLeft;
    private Button _BigTurnRight;
    private Button _ConfirmRotation;
    private Button _Reset;

    private bool _canTurnSmall;
    private int _direction;
    private Quaternion _oldRot;


    #endregion

    #region SetupThirdStep
    private GameObject _SetUpJoints;
    #endregion
    private void Start()
    {
        _Background = GameObject.Find("Background").GetComponent<Image>();

        _rootDrop = GameObject.Find("Root Dropdown").GetComponent<Dropdown>();
        _pelvDrop = GameObject.Find("Pelvis Dropdown").GetComponent<Dropdown>();
        _ConfirmSetup = GameObject.Find("ConfirmSetup").GetComponent<Button>();
        _ChooseRot = GameObject.Find("Choose Root + Pelvis");
        
        _flycam = GameObject.FindObjectOfType<FlyCam>();
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
        
        _ChangeRot = GameObject.Find("Orient Avatar");
        _ChangeRot.SetActive(false);

        _SetUpJoints = GameObject.Find("Setup Skeleton");
        _SetUpJoints.SetActive(false);

    }

    private void Update()
    {
        if (_canTurnSmall)
        {
            // Left Key Rotate Left, Right Key Rotate Right.
            if (Input.GetKeyUp(KeyCode.LeftArrow))
                _direction = 0;
            if (Input.GetKeyUp(KeyCode.RightArrow))
                _direction = 0;

            if (_direction < 0)
                RotateY(45f * Time.deltaTime);
            if (_direction > 0)
                RotateY(-45f * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                _direction = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow))
                _direction = 1;
        }
    }
    public void ConfirmSelection(List<Transform>Tlist)
    {
        //Get the transforms for pelvis and root
        _root = Tlist[_rootDrop.value];
        _pelvis = Tlist[_pelvDrop.value];

        //Zero the Position of everything above root so it is centered. Do we want to do that? Or just set the groundplane below
        var totest = _root;
        while (totest.parent != null)
        {
            totest = totest.parent;
            totest.position = Vector3.zero;
        }


        //Enable second Step 
        _Background.gameObject.SetActive(false);
        _ChooseRot.SetActive(false);
        _ChangeRot.SetActive(true);
        SetupRotator();


        //Deactivate flycam controls until Rotation is Confirmed.
        _flycam.target = _pelvis;
    }

    private void SetupRotator()
    {
        _BigTurnLeft = _ChangeRot.transform.Find("RotateLeft").GetComponent<Button>();
        _BigTurnRight = _ChangeRot.transform.Find("RotateRight").GetComponent<Button>();
        _ConfirmRotation = _ChangeRot.transform.Find("Confirm Orientation").GetComponent<Button>();
        _Reset = _ChangeRot.transform.Find("Reset Orientation").GetComponent<Button>();
        _canTurnSmall = true;

        _BigTurnLeft.onClick.RemoveAllListeners();
        _BigTurnRight.onClick.RemoveAllListeners();
        _ConfirmRotation.onClick.RemoveAllListeners();
        _Reset.onClick.RemoveAllListeners();

        _BigTurnLeft.onClick.AddListener(delegate { RotateY(90f); });
        _BigTurnRight.onClick.AddListener(delegate { RotateY(-90f); });
        _ConfirmRotation.onClick.AddListener(delegate { ConfirmRotation(); });
        _Reset.onClick.AddListener(delegate { _pelvis.rotation = _oldRot; });

        _oldRot = _pelvis.rotation;


    }

    public void ConfirmRotation()
    {
        //Confirm the Avatar is placed correctly and the Scripts can now be placed on it.
        AddConfiguratorScripts();

        //Deactivate UI of Rotator
        _ChangeRot.SetActive(false);
        _canTurnSmall = false;

        //Activate the UI for the Setup of the joints.
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = true;

    }

    public void RotateY(float degree)
    {
        _pelvis.Rotate(new Vector3(0, degree, 0), Space.World);
    }



    public void SetUpDropdowns(Transform gltfObject)
    {
        _rootDrop.ClearOptions();
        _pelvDrop.ClearOptions();
        List<Transform> list = new List<Transform>();
        List<string> slist = new List<string>();
        list = IterateThrough(gltfObject, list);
        
        for(int i = 0; i < list.Count; i++)
        {
            var name = list[i].name;
            _rootDrop.options.Add(new Dropdown.OptionData() { text = name});
            _pelvDrop.options.Add(new Dropdown.OptionData() { text = name });

            if (name.ToLower().Contains("root"))
                _rootDrop.value = i;
            if (name.ToLower().Contains("pelvis"))
                _pelvDrop.value = i;
        }
        _ConfirmSetup.onClick.RemoveAllListeners();
        _ConfirmSetup.onClick.AddListener(delegate { ConfirmSelection(list); });
    }
    private List<Transform> IterateThrough(Transform obj, List<Transform>Tlist)
    {
        Tlist.Add(obj);
        for(int i = 0; i< obj.childCount; i++)
        {
            Tlist = IterateThrough(obj.GetChild(i), Tlist);
        }
        return Tlist;
    }

    /// <summary>
    /// Adds necessary scripts for configuration of skeleton.
    /// </summary>
    /// <param name="AvatarParent"> The parentobject of the rig.</param>
    /// <param name="root">The upper part of the righ hierarchy.</param>
    private void AddConfiguratorScripts()
    {
        GameObject AvatarParent = GameObject.Find("Avatar");
        // Add the necessary Script components
        var bony = AvatarParent.AddComponent<BoneVisualization>();
        var testy = AvatarParent.AddComponent<TestIS>();
        var controlly = AvatarParent.AddComponent<CharacterMeshRendererController>();
        var mappy = AvatarParent.AddComponent<JointMapper2>();
        var setupy = AvatarParent.AddComponent<AutoSetupInterface>();

        // Setup BoneVis
        bony.Root = _root.parent;
        bony.bonePrefab = BonePrefab;
        bony.endPrefab = EndPrefab;

        //Setup TestIS
        testy.RootTransform = _root.parent;
        testy.Pelvis = _pelvis;
        testy.gameJointPrefab = GameJointPrefab;
        testy.UseSkeletonVisualization = true;

        //Setup Mapper
        mappy.Root = _root.GetChild(0);

        //Setup AutoSetuper
        setupy.root = _root;


    }

    private void GoToStep(int i)
    {

    }

    public void ResetUI()
    {
        _ChangeRot.SetActive(false);
        _ChooseRot.SetActive(false);
        _SetUpJoints.SetActive(false);

        _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
    }
}
