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
        SelectPath,
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
    private List<Transform> _bonelist;
    private Image _Background;
    private FlyCam _flycam;
    private Button _back;
    private State _state;
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

    #region MosFile
    private GameObject _MosimSelector;
    #endregion
    #region SetupJointMapping
    private GameObject _SetUpJoints;
    private Button _ConfirmMapping;
    #endregion

    #region SetupCompleted
    private GameObject _SetuppedConfigurator;
    #endregion
    private void Start()
    {
        this._state = State.RootSelect;
        _back = GameObject.Find("Back Button").GetComponent<Button>();
        _back.onClick.AddListener(delegate { goBack(_state -1); });
        _back.gameObject.SetActive(false);
        _Background = GameObject.Find("Canvas").transform.Find("Background").GetComponent<Image>();

        _rootDrop = GameObject.Find("Root Dropdown").GetComponent<Dropdown>();
        _pelvDrop = GameObject.Find("Pelvis Dropdown").GetComponent<Dropdown>();
        _ConfirmSetup = GameObject.Find("ConfirmSetup").GetComponent<Button>();

        _ChooseRot = GameObject.Find("Choose Root + Pelvis");
        _ChooseRot.SetActive(false);
        
        _flycam = GameObject.FindObjectOfType<FlyCam>();
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
        
        _ChangeRot = GameObject.Find("Orient Avatar");
        _ChangeRot.SetActive(false);

        _MosimSelector = GameObject.Find("Configuration-File");        
        _MosimSelector.transform.Find("Buttons/New Mos File").GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(CreateNewMos()); });
        _MosimSelector.transform.Find("Buttons/Select Mos File").GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(SelectMos()); });
        _MosimSelector.transform.Find("Buttons/Default").GetComponent<Button>().onClick.AddListener(delegate { ConfirmMOSIMFile(Application.dataPath + "/Samples/Configs/avatar.mos_initial"); });
        _MosimSelector.SetActive(false);


        _ConfirmMapping = GameObject.Find("ConfirmMapping").GetComponent<Button>();
        _ConfirmMapping.interactable = false;
        _ConfirmMapping.onClick.AddListener(delegate { ConfirmRetargeting(); });
        _SetUpJoints = GameObject.Find("Setup Skeleton");
        _SetUpJoints.SetActive(false);

        _SetuppedConfigurator = GameObject.Find("Control-after-import");
        _SetuppedConfigurator.SetActive(false);

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
        _back.gameObject.SetActive(true);
        _back.interactable = true;
        _ChooseRot.SetActive(false);
        _ChangeRot.SetActive(true);
        _state = State.RotationAdjust;
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
        // Place World Canvas UI on Hands.
        PlaceHandUI();

        //Deactivate UI of Rotator
        _ChangeRot.SetActive(false);
        _canTurnSmall = false;

        //Activate UI of MosimSelector
        _MosimSelector.SetActive(true);

    }

    private void ConfirmMOSIMFile(string filepath)
    {
        //Deactivate UI of the step
        _MosimSelector.SetActive(false);

        //Activate the UI for the Setup of the joints.
        _SetUpJoints.SetActive(true);
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = true;
        _state = State.JointMap;

        //Confirm the Avatar is placed correctly and the Scripts can now be placed on it.
        AddConfiguratorScripts(filepath);
    }

    public void ConfirmRetargeting()
    {

        //Deactivate Mapper UI
        _SetUpJoints.SetActive(false);

        //Activate interaction UI
        _SetuppedConfigurator.SetActive(true);
        _state = State.IsSetup;

        //AutoSetupInterface Initializemethod for after successful mapping.#
        GameObject AvatarParent = GameObject.Find("Avatar");
        AvatarParent.GetComponent<AutoSetupInterface>().AfterMapping();
    }

    private void PlaceHandUI()
    {
        //Go through the hierarchy of the Avatar. As soon as a bone with 5 childs with satisfying depth is being found, add the corresponding UI Canvas.
        Transform left = FindHand(_pelvis);
        GameObject LeftUI = Instantiate (Resources.Load<GameObject>("In World Canvas"));
        LeftUI.transform.GetChild(0).GetComponent<WorldUI>().Target = left;


        Transform right = FindHand(_pelvis, 0, false);
        GameObject RightUI = Instantiate(Resources.Load<GameObject>("In World Canvas"));
        RightUI.transform.GetChild(0).GetComponent<WorldUI>().Target = right;
        RightUI.transform.GetChild(0).GetComponent<WorldUI>().left = false;
    }

    private Transform FindHand(Transform origin, int depth = 0,bool left = true)
    {
        Transform result = null;
        if (depth >=5 && origin.childCount == 5)
        {
            //Assumes the Character is rotated to front the Camera
            if (left) { 
                if( origin.transform.position.x < 0)
                
                    return origin;
                }
            else
            {
                if(origin.position.x > 0)
                    return origin;
            }
        }
        for(int i = 0; i< origin.childCount; i++)
        {
            if (result != null)
                break;
            result = FindHand(origin.GetChild(i), depth + 1, left);
            
        }
        if (result == null && depth == 0)
            Debug.Log("No Hand Found");
        return result;
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
        list = IterateThrough(gltfObject, list);
        _bonelist = list;
        
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
    private void AddConfiguratorScripts(string filepath)
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
        //TODO: GIVE CONFIG FILEPATH!
        testy.ConfigurationFilePath = filepath;

        //Setup Mapper
        mappy.Root = _pelvis;

        //Setup AutoSetuper
        setupy.root = _root;


    }
    public void ResetUI()
    {
        _ChangeRot.SetActive(false);
        _ChooseRot.SetActive(false);
        _SetUpJoints.SetActive(false);
        _SetuppedConfigurator.SetActive(false);

        _ConfirmMapping.interactable = false;

        _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
    }

    /// <summary>
    /// Go back to state s. Deactivates and activates necessary UI, changes are not reversed.
    /// TODO: Delete all files and bones.
    /// </summary>
    /// <param name="s">Target State</param>
    private void goBack(State s)
    {
        //TODO: When coming from a state further than RotationAdjust, we need to delete the WorldCanvas Objects and also visual bones which were created and the scripts.
        switch (s)
        {
            case State.RootSelect:
                SafeStepBack();
                _Background.gameObject.SetActive(true);
                _back.interactable= false;
                _state = s;
                _ChangeRot.SetActive(false);
                _SetUpJoints.SetActive(false);
                _SetuppedConfigurator.SetActive(false);
                _ChooseRot.SetActive(true);
                _MosimSelector.SetActive(false);
                _back.interactable = false;
                break;
            case State.RotationAdjust:
                SafeStepBack();
                _state = s;
                _ChangeRot.SetActive(true);
                _SetUpJoints.SetActive(false);
                _SetuppedConfigurator.SetActive(false);
                _ChooseRot.SetActive(false);
                _MosimSelector.SetActive(false);
                break;
            case State.SelectPath:
                SafeStepBack();
                _state = s;
                _ChangeRot.SetActive(false);
                _SetUpJoints.SetActive(false);
                _SetuppedConfigurator.SetActive(false);
                _ChooseRot.SetActive(false);
                _MosimSelector.SetActive(true);
                break;
            case State.JointMap:
                SafeStepBack();
                _state = s;
                _ChangeRot.SetActive(false);
                _SetUpJoints.SetActive(true);
                _SetuppedConfigurator.SetActive(false);
                _ChooseRot.SetActive(false);
                _MosimSelector.SetActive(false);
                break;
            case State.IsSetup:
                SafeStepBack();
                _state = s;
                _ChangeRot.SetActive(false);
                _SetUpJoints.SetActive(false);
                _SetuppedConfigurator.SetActive(true);
                _ChooseRot.SetActive(false);
                _MosimSelector.SetActive(false);
                break;
            default:
                break;
        }

    }

    IEnumerator CreateNewMos()
    {
        //Let the user choose a folder where a new Mos File should be created.
        yield return FileBrowser.WaitForLoadDialog(true, null, "Create new .mos file in folder", "Create");

        if (FileBrowser.Success)
        {
            var filepath = FileBrowser.Result;
            filepath = filepath + "/avatar.mos";
            System.IO.File.WriteAllText(filepath, "");
            ConfirmMOSIMFile(filepath);
        }
    }

    IEnumerator SelectMos()
    {
        //Let the user choose an existing .mos file.
        yield return FileBrowser.WaitForLoadDialog(false, null, "Select a .mos file", "Select");

        if (FileBrowser.Success && FileBrowser.Result.Contains(".mos"))
        {
            // Next step and filepath
            var filepath = FileBrowser.Result;
            ConfirmMOSIMFile(filepath);
        }
    }

    private void SafeStepBack()
    {
        RevertInfluenceOfScripts();
        DeleteWorldCanvas();
    }

    private void RevertInfluenceOfScripts()
    {
        if(_state >= State.JointMap)
        {
            //Destroy all scripts which were attached to the GameObject.
            var avatar = GameObject.Find("Avatar");
            foreach (var comp in avatar.GetComponents<Component>())
            {
                if (!(comp is Transform))
                {
                    Destroy(comp);
                }
            }
            ShredAvatar(avatar.transform);
            
        }
    }

    /// <summary>
    /// Recursive function to go over all transforms of the Avatar and look for childs which are not included in the bonelist and will be deleted.
    /// </summary>
    /// <param name="toshred">The bones of the skeleton (including avatar, root,..)</param>
    private void ShredAvatar(Transform toshred)
    {
        for(int i = 0; i<toshred.childCount; i++)
        {
            if (!_bonelist.Contains(toshred.GetChild(i)))
                Destroy(toshred.GetChild(i).gameObject);
            else
                ShredAvatar(toshred.GetChild(i));
        }
    }

    private void DeleteWorldCanvas()
    {
        if (_state <= State.SelectPath)
        {
            var obj = GameObject.FindObjectsOfType<WorldUI>();
            foreach (var ob in obj)
            {
                Destroy(ob.transform.parent.gameObject);
            }
        }
    }
}
