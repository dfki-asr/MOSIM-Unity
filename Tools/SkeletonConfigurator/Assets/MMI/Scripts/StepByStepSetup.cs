using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using MMIUnity;
using RuntimeGizmos;
using SimpleFileBrowser;

public class StepByStepSetup : MonoBehaviour
{
    private enum State
    {
        RootSelect,
        RotationAdjust,
        PositionAdjust,
        ScaleAdjust,
        JointMap,
        IsSetup,
        SelectPath
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
    private Button _HideHands;
    private List<GameObject> _HandSigns;
    private State _state;
    private Dictionary<State, GameObject> _dic;
    private TransformGizmo _transGiz;
    private Vector3 _oldFlyCamPos;
    private Quaternion _oldFlyCamRot;
    private string _filepath;
    private bool _changedPelvOrRoot = false;
    private List<GameObject> _boneMeshes;
    #endregion

    #region SetupSelect
    private Dropdown _rootDrop;
    private Dropdown _pelvDrop;
    private Button _ConfirmSetup;
    private GameObject _ChooseRot;
    #endregion

    #region SetupRotate
    private GameObject _ChangeRot;
    private Button _BigTurnLeft;
    private Button _BigTurnRight;
    private Button _ConfirmRotation;
    private Button _ResetRot;
    private int _direction;
    private Quaternion _oldRot;
    private string _rotationTooltiptext;
    private Text _rotationTooltip;
    #endregion

    #region SetupPos
    private GameObject _ChangePos;
    private Button _BigMoveLeft;
    private Button _BigMoveRight;
    private Button _BigMoveUp;
    private Button _BigMoveDown;
    private Button _ConfirmPos;
    private Button _ResetPos;
    private string _positionTooltiptext;
    private Text _positionTooltip;

    private Vector2 _posDir;
    private Vector3 _oldPos;

    private List<GameObject> _planes;
    #endregion

    #region SetupScale
    private GameObject _ChangeScale;
    private Button _BigScaleUp;
    private Button _BigScaleDown;
    private Button _ConfirmScale;
    private Button _ResetScale;
    private int _scale;
    private Vector3 _oldScale;
    private string _scaleTooltiptext;
    private Text _scaleTooltip;
    private float _calculatedHeight;
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
        _dic = new Dictionary<State, GameObject>();

        this._state = State.RootSelect;
        _back = GameObject.Find("Back Button").GetComponent<Button>();
        _back.onClick.AddListener(delegate { goBack(_state -1); });
        _back.gameObject.SetActive(false);
        _HideHands = GameObject.Find("Hide Hands").GetComponent<Button>();
        _HideHands.onClick.AddListener(delegate { DisableOrEnableHandSigns(_HideHands.transform.GetComponentInChildren<Text>()); });
        _HideHands.gameObject.SetActive(false);
        _HandSigns = new List<GameObject>();
        _Background = GameObject.Find("Canvas").transform.Find("Background").GetComponent<Image>();

        _rootDrop = GameObject.Find("Root Dropdown").GetComponent<Dropdown>();
        _pelvDrop = GameObject.Find("Pelvis Dropdown").GetComponent<Dropdown>();
        _ConfirmSetup = GameObject.Find("ConfirmSetup").GetComponent<Button>();

        _ChooseRot = GameObject.Find("Choose Root + Pelvis");
        _dic.Add(State.RootSelect, _ChooseRot);

        _ChooseRot.SetActive(false);

        
        _flycam = GameObject.FindObjectOfType<FlyCam>();
        _transGiz = _flycam.gameObject.GetComponent<TransformGizmo>();
        _transGiz.enabled = false;
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
        _oldFlyCamPos = _flycam.gameObject.transform.position;
        _oldFlyCamRot = _flycam.gameObject.transform.rotation;
        
        _ChangeRot = GameObject.Find("Orient Avatar");
        _dic.Add(State.RotationAdjust, _ChangeRot);
        _rotationTooltip = GameObject.Find("RotationTooltip").GetComponent<Text>();
        _rotationTooltiptext = _rotationTooltip.text;
        _ChangeRot.SetActive(false);

        _ChangePos = GameObject.Find("Position Avatar");
        _dic.Add(State.PositionAdjust, _ChangePos);
        _positionTooltip = GameObject.Find("PositionTooltip").GetComponent<Text>();
        _positionTooltiptext = _positionTooltip.text;
        _ChangePos.SetActive(false);
        _planes = new List<GameObject>();

        _ChangeScale = GameObject.Find("Scale Avatar");
        _dic.Add(State.ScaleAdjust, _ChangeScale);
        _scaleTooltip = GameObject.Find("ScaleTooltip").GetComponent<Text>();
        _scaleTooltiptext = _scaleTooltip.text;
        _ChangeScale.SetActive(false);

        _MosimSelector = GameObject.Find("Configuration-File");
        _dic.Add(State.SelectPath, _MosimSelector);
        _MosimSelector.transform.Find("Buttons/New Mos File").GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(CreateNewMos()); });
        _MosimSelector.transform.Find("Buttons/Select Mos File").GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(SelectMos()); });
        _filepath = Application.streamingAssetsPath + "/Configs/avatar.mos_initial";
        _MosimSelector.transform.Find("Buttons/Default").GetComponent<Button>().onClick.AddListener(delegate { ConfirmMOSIMFile(_filepath); });
        _MosimSelector.SetActive(false);


        _ConfirmMapping = GameObject.Find("ConfirmMapping").GetComponent<Button>();
        _ConfirmMapping.interactable = false;
        _ConfirmMapping.onClick.AddListener(delegate { ConfirmRetargeting(); });
        _SetUpJoints = GameObject.Find("Setup Skeleton");
        _dic.Add(State.JointMap, _SetUpJoints);
        _SetUpJoints.SetActive(false);

        _SetuppedConfigurator = GameObject.Find("Control-after-import");
        _dic.Add(State.IsSetup, _SetuppedConfigurator);
        _SetuppedConfigurator.SetActive(false);
        _boneMeshes = new List<GameObject>();

    }

    private void Update()
    {
        if (_state == State.RotationAdjust)
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

        if (_state == State.ScaleAdjust)
        {
            // Up and down to Scale up or down.
            if (Input.GetKeyUp(KeyCode.UpArrow))
                _scale = 0;
            if (Input.GetKeyUp(KeyCode.DownArrow))
                _scale = 0;

            if (_scale < 0)
                ScaleRoot();
            if (_scale > 0)
                ScaleRoot();

            if (Input.GetKeyDown(KeyCode.DownArrow))
                _scale = -1;
            if (Input.GetKeyDown(KeyCode.UpArrow))
                _scale = 1;
        }

        if (_state == State.PositionAdjust)
        {
            // Left Right up to move Position
            if (Input.GetKeyUp(KeyCode.LeftArrow))
                _posDir = new Vector2(0,0);
            if (Input.GetKeyUp(KeyCode.RightArrow))
                _posDir = new Vector2(0, 0);
            if (Input.GetKeyUp(KeyCode.UpArrow))
                _posDir = new Vector2(0, 0);
            if (Input.GetKeyUp(KeyCode.DownArrow))
                _posDir = new Vector2(0, 0);

            if (_posDir != new Vector2(0, 0))
                MoveRoot();

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                _posDir.x = 1;
            if (Input.GetKeyDown(KeyCode.RightArrow))
                _posDir.x = -1;
            if (Input.GetKeyDown(KeyCode.UpArrow))
                _posDir.y = 1;
            if (Input.GetKeyDown(KeyCode.DownArrow))
                _posDir.y = -1;

        }
    }

    IEnumerator CalculateHeight()
    {
        yield return new WaitForEndOfFrame();
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (Transform t in _bonelist)
        {
            if(t.GetComponent<SkinnedMeshRenderer>() != null)
            {
                if (t.GetComponent<SkinnedMeshRenderer>().bounds.min.y < min)
                    min = t.GetComponent<SkinnedMeshRenderer>().bounds.min.y;
                if (t.GetComponent<SkinnedMeshRenderer>().bounds.max.y > max)
                    max = t.GetComponent<SkinnedMeshRenderer>().bounds.max.y;
            }
        }
        this._calculatedHeight = max - min;
        UpdateScaleText();

    }

    private void UpdateScaleText()
    {
        _scaleTooltip.text = _scaleTooltiptext + $"\n\ncalculated height: {this._calculatedHeight} \ncurrent root scale: {this._root.localScale.ToString()}";
    }

    private void UpdateMoveText()
    {
        _positionTooltip.text = _positionTooltiptext + $"\n\ncurrent root position: {this._root.position.ToString()}";
    }

    private void UpdateRotationText()
    {
        _rotationTooltip.text = _rotationTooltiptext + $"\n\ncurrent root rotation: {this._root.rotation.eulerAngles.ToString()}";
    }

    private void ScaleRoot()
    {
        _root.localScale += new Vector3(_scale, _scale, _scale)* 0.05f * Time.deltaTime;
        StartCoroutine(CalculateHeight());
    }

    private void MoveRoot()
    {
        _root.position += new Vector3(_posDir.x, _posDir.y, 0) * 0.1f * Time.deltaTime;
        UpdateMoveText();
    }
    public void ConfirmSelection(List<Transform>Tlist)
    {
        //Get the transforms for pelvis and root
        if (_root != Tlist[_rootDrop.value] || _pelvis != Tlist[_pelvDrop.value])
            _changedPelvOrRoot = true;
        _root = Tlist[_rootDrop.value];
        _pelvis = Tlist[_pelvDrop.value];

        //Zero the Position of everything above root so it is centered. Do we want to do that? Or just set the groundplane below
        var totest = _root;
        while (totest.parent != null)
        {
            totest = totest.parent;
            totest.position = Vector3.zero;
        }
        _root.position = Vector3.zero;


        //Enable second Step 
        _back.gameObject.SetActive(true);
        _back.interactable = true;
        _ChooseRot.SetActive(false);
        _ChangeRot.SetActive(true);
        _state = State.RotationAdjust;
        SetUpmaterials();
        SetupRotator();
        _Background.gameObject.SetActive(false);


        //Deactivate flycam controls until Rotation is Confirmed.
        _flycam.target = _pelvis;

        _oldRot = _root.rotation;
        _oldPos = _root.position;
        _oldScale = _root.localScale;
    }

    public void ConfirmRotation()
    {
        // Place World Canvas UI on Hands.
        PlaceHandUI();

        //Deactivate UI of Rotator
        _ChangeRot.SetActive(false);

        // Activate ChangePos
        _ChangePos.SetActive(true);
        _state = State.PositionAdjust;
        _HideHands.gameObject.SetActive(true);
        SetUpPositioner();

    }

    public void ConfirmPos()
    {
        _ChangePos.SetActive(false);

        _ChangeScale.SetActive(true);
        _state = State.ScaleAdjust;
        SetupScaler();
    }

    private void ConfirmScale()
    {
        foreach (GameObject p in _planes)
            Destroy(p);
        _planes.Clear();

        _ChangeScale.SetActive(false);

        _flycam.GetComponent<FlyCamController>().ClearPlanes();
        _flycam.GetComponent<FlyCamController>().ResetToNonOrtho();
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = true;

        //Activate UI of MosimSelector
        _SetUpJoints.SetActive(true);
        _state = State.JointMap;

        AddConfiguratorScripts(_filepath);

    }

    public void ConfirmRetargeting()
    {

        //Deactivate Mapper UI
        _SetUpJoints.SetActive(false);

        //Activate interaction UI
        _SetuppedConfigurator.SetActive(true);
        _transGiz.enabled = true;
        _state = State.IsSetup;

        //AutoSetupInterface Initializemethod for after successful mapping.#
        GameObject AvatarParent = GameObject.Find("Avatar");
        AvatarParent.GetComponent<AutoSetupInterface>().AfterMapping();
    }

    private void ConfirmMOSIMFile(string filepath)
    {
        //Deactivate UI of the step
        _MosimSelector.SetActive(false);

        _SetuppedConfigurator.SetActive(true);
        _state = State.IsSetup;
        _transGiz.enabled = true;

        GameObject.Find("Avatar").GetComponent<TestIS>().ConfigurationFilePath = filepath;

        GameObject.Find("Avatar").GetComponent<TestIS>().SaveConfig();
    }

    public void ChooseConfigFileLoc()
    {
        _MosimSelector.SetActive(true);
        _state = State.SelectPath;
        _SetuppedConfigurator.SetActive(false);

        _transGiz.enabled = false;
    }

    private void SetupRotator()
    {
        _BigTurnLeft = _ChangeRot.transform.Find("RotateLeft").GetComponent<Button>();
        _BigTurnRight = _ChangeRot.transform.Find("RotateRight").GetComponent<Button>();
        _ConfirmRotation = _ChangeRot.transform.Find("Buttons/Confirm Orientation").GetComponent<Button>();
        _ResetRot = _ChangeRot.transform.Find("Buttons/Reset Orientation").GetComponent<Button>();

        _BigTurnLeft.onClick.RemoveAllListeners();
        _BigTurnRight.onClick.RemoveAllListeners();
        _ConfirmRotation.onClick.RemoveAllListeners();
        _ResetRot.onClick.RemoveAllListeners();

        _BigTurnLeft.onClick.AddListener(delegate { RotateY(90f); UpdateRotationText(); });
        _BigTurnRight.onClick.AddListener(delegate { RotateY(-90f); UpdateRotationText(); });
        _ConfirmRotation.onClick.AddListener(delegate { ConfirmRotation(); UpdateRotationText(); });
        _ResetRot.onClick.AddListener(delegate { _root.rotation = _oldRot; UpdateRotationText(); });
        UpdateRotationText();
    }

    private void SetUpPositioner()
    {
        foreach(GameObject p in _planes)
        {
            Destroy(p);
        }
        _planes.Clear();

        _BigMoveLeft = _ChangePos.transform.Find("Move Buttons/Move left").GetComponent<Button>();
        _BigMoveRight = _ChangePos.transform.Find("Move Buttons/Move right").GetComponent<Button>();
        _BigMoveUp = _ChangePos.transform.Find("Move Buttons/Up Down Buttons/MoveUp").GetComponent<Button>();
        _BigMoveDown = _ChangePos.transform.Find("Move Buttons/Up Down Buttons/MoveDown").GetComponent<Button>();
        _ConfirmPos = _ChangePos.transform.Find("Buttons/Confirm Position").GetComponent<Button>();
        _ResetPos = _ChangePos.transform.Find("Buttons/Reset Position").GetComponent<Button>();

        _BigTurnLeft.onClick.RemoveAllListeners();
        _BigTurnRight.onClick.RemoveAllListeners();
        _BigMoveUp.onClick.RemoveAllListeners();
        _BigMoveDown.onClick.RemoveAllListeners();
        _ConfirmPos.onClick.RemoveAllListeners();
        _ResetPos.onClick.RemoveAllListeners();

        _BigMoveLeft.onClick.AddListener(delegate { _root.position += new Vector3(.1f, 0, 0); UpdateMoveText(); });
        _BigMoveRight.onClick.AddListener(delegate { _root.position += new Vector3(-.1f, 0, 0); UpdateMoveText(); });
        _BigMoveDown.onClick.AddListener(delegate { _root.position += new Vector3(0, -.1f, 0); UpdateMoveText(); });
        _BigMoveUp.onClick.AddListener(delegate { _root.position += new Vector3(0, .1f, 0); UpdateMoveText(); });
        _ConfirmPos.onClick.AddListener(delegate { ConfirmPos(); });
        _ResetPos.onClick.AddListener(delegate { _root.position = _oldPos; UpdateMoveText(); });

        // Add Planes and change to projection
        var plane1 = Instantiate(Resources.Load("Plane") as GameObject, Vector3.zero, Quaternion.Euler(90,0,0));
        var plane2 = Instantiate(Resources.Load("Plane") as GameObject, Vector3.zero, Quaternion.Euler(90,0,0));
        plane2.transform.position -= new Vector3(0,plane2.transform.localScale.x *5,-.001f);
        var mat = plane2.GetComponent<MeshRenderer>().material;
        mat.color = new Color(0, 0, 0);
        mat.SetFloat("_Metallic", 1f);
        mat.SetFloat("_Glossiness", 0f);
        _planes.Add(plane1);
        _planes.Add(plane2);

        var zerorotation = Quaternion.Euler(Vector3.zero);

        _flycam.gameObject.GetComponent<FlyCamController>().ChangeProjection(zerorotation, _planes);
        _flycam.gameObject.GetComponent<Camera>().orthographicSize = 1.5f;
        if (_flycam.transform.childCount > 0)
            _flycam.transform.GetChild(0).GetComponent<Camera>().orthographicSize = 1.5f;
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = true;

        UpdateMoveText();
    }

    private void SetupScaler()
    {
        foreach(GameObject p in _planes)
        {
            Destroy(p);
        }
        _planes.Clear();
        _BigScaleUp = _ChangeScale.transform.Find("Scale Buttons/ScaleUp").GetComponent<Button>();
        _BigScaleDown = _ChangeScale.transform.Find("Scale Buttons/ScaleDown").GetComponent<Button>();
        _ConfirmScale = _ChangeScale.transform.Find("Buttons/Confirm Scale").GetComponent<Button>();
        _ResetScale = _ChangeScale.transform.Find("Buttons/Reset Scale").GetComponent<Button>();

        _BigScaleUp.onClick.RemoveAllListeners();
        _BigScaleDown.onClick.RemoveAllListeners();
        _ConfirmScale.onClick.RemoveAllListeners();
        _ResetScale.onClick.RemoveAllListeners();

        _BigScaleUp.onClick.AddListener(delegate { _root.localScale += new Vector3(.1f, .1f, .1f);
            StartCoroutine(CalculateHeight());
        });
        _BigScaleDown.onClick.AddListener(delegate { _root.localScale -= new Vector3(0.1f, .1f, .1f);
            StartCoroutine(CalculateHeight());
        });

        _ConfirmScale.onClick.AddListener(delegate { ConfirmScale(); });
        _ResetScale.onClick.AddListener(delegate { _root.localScale = _oldScale; CalculateHeight(); });

        var plane1 = Instantiate(Resources.Load("Plane") as GameObject, Vector3.zero, Quaternion.Euler(90, 0, 0));
        _planes.Add(plane1);

        var zerorotation = Quaternion.Euler(Vector3.zero);

        _flycam.gameObject.GetComponent<FlyCamController>().ChangeProjection(zerorotation, _planes);
        _flycam.gameObject.GetComponent<Camera>().orthographicSize = 1.5f;
        if(_flycam.transform.childCount > 0)
            _flycam.transform.GetChild(0).GetComponent<Camera>().orthographicSize = 1.5f;
        _flycam.gameObject.GetComponent<FlyCamController>().enabled = true;

        StartCoroutine(CalculateHeight());

    }

    private void PlaceHandUI()
    {
        //Go through the hierarchy of the Avatar. As soon as a bone with 5 childs with satisfying depth is being found, add the corresponding UI Canvas.
        Transform left = FindHand(_pelvis);
        GameObject LeftUI = Instantiate (Resources.Load<GameObject>("In World Canvas"));
        LeftUI.transform.GetChild(0).GetComponent<WorldUI>().Target = left;

        _HandSigns.Add(LeftUI);


        Transform right = FindHand(_pelvis, 0, false);
        GameObject RightUI = Instantiate(Resources.Load<GameObject>("In World Canvas"));
        RightUI.transform.GetChild(0).GetComponent<WorldUI>().Target = right;
        RightUI.transform.GetChild(0).GetComponent<WorldUI>().left = false;

        _HandSigns.Add(RightUI);
    }

    private Transform FindHand(Transform origin, int depth = 0,bool left = true)
    {
        Transform result = null;
        var bonecount = 0;
        for (int i = 0; i < origin.childCount; i++)
        {
            if (origin.GetChild(i).gameObject.activeInHierarchy)
                bonecount++;
        }
        if (depth >=5 && bonecount == 5)
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
        _root.Rotate(new Vector3(0, degree, 0), Space.World);
        UpdateRotationText();
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
        if (!HasScripts(AvatarParent))
        {
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

            testy.ConfigurationFilePath = filepath;

            //Setup Mapper
            mappy.Root = _root;

            //Setup AutoSetuper
            setupy.root = _root;

            // Do we want to let the jointmap be? 
            _changedPelvOrRoot = false;
            _boneMeshes.Clear();
        } else
        {
            if (_changedPelvOrRoot)
            {
                var bony = AvatarParent.GetComponent<BoneVisualization>();
                var testy = AvatarParent.GetComponent<TestIS>();
                var controlly = AvatarParent.GetComponent<CharacterMeshRendererController>();
                var mappy = AvatarParent.GetComponent<JointMapper2>();
                var setupy = AvatarParent.GetComponent<AutoSetupInterface>();

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
                mappy.Root = _root;

                //Setup AutoSetuper
                setupy.root = _root;
            }
            foreach(GameObject g in _boneMeshes)
            {
                g.SetActive(true);
            }
            _boneMeshes.Clear();
        }


    }

    private bool HasScripts(GameObject g)
    {
        return
            (g.GetComponent<BoneVisualization>() != null) &&
            (g.GetComponent<TestIS>() != null) &&
            (g.GetComponent<CharacterMeshRendererController>() != null) &&
            (g.GetComponent<JointMapper2>() != null) &&
            (g.GetComponent<AutoSetupInterface>() != null);
    }
    

    public void ResetToStart()
    {
        SafeStepBack(State.RootSelect);
        DestroyInfluenceOfScripts(State.RootSelect);
        _state = State.RootSelect;       
    }

    private void SetUpmaterials()
    {
        var mats = GameObject.Find("Avatar").GetComponentsInChildren<SkinnedMeshRenderer>();
        var o = Resources.Load("Materials/Avatar", typeof (Material)) as Material;
        foreach (var mat in mats)
        {
            mat.material = o;
        }

        
    }

    /// <summary>
    /// Go back to state s. Deactivates and activates necessary UI, changes are not reversed.
    /// TODO: Delete all files and bones.
    /// </summary>
    /// <param name="s">Target State</param>
    private void goBack(State s)
    {
        GameObject a;
        //TODO: When coming from a state further than RotationAdjust, we need to delete the WorldCanvas Objects and also visual bones which were created and the scripts.
        switch (s)
        {
            case State.RootSelect:
                SafeStepBack(s);
                _Background.gameObject.SetActive(true);
                _back.interactable= false;
                _state = s;                
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                break;
            case State.RotationAdjust:
                SafeStepBack(s);
                _state = s;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                SetUpmaterials();
                SetupRotator();
                break;
            case State.PositionAdjust:
                SafeStepBack(s);
                _state = s;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                SetUpPositioner();
                break;
            case State.ScaleAdjust:
                SafeStepBack(s);
                _state = s;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                SetupScaler();
                break;
            case State.SelectPath:
                SafeStepBack(s);
                _state = s;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                break;
            case State.JointMap:
                SafeStepBack(s);
                _state = s;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                break;
            case State.IsSetup:
                SafeStepBack(s);
                _state = s;
                _transGiz.enabled = true;
                _dic.TryGetValue(s, out a);
                if (a != null)
                    a.SetActive(true);
                break;
            default:
                break;
        }

    }

    public bool CanExitOrtho()
    {
        if (_state >= State.JointMap)
            return true;
        else
            return false;
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

    private void SafeStepBack(State s)
    {
        if (_state >= State.RotationAdjust +1 && s < State.JointMap)
        {
            _flycam.gameObject.GetComponent<FlyCamController>().ClearPlanes();
            _flycam.gameObject.GetComponent<FlyCamController>().ResetToNonOrtho();
            _flycam.gameObject.GetComponent<FlyCamController>().enabled = false;
        }
        RevertInfluenceOfScripts(s);
        DeleteWorldCanvas();
        ResetSteps(s);
    }

    private void ResetSteps(State s)
    {
        foreach (GameObject p in _planes)
            Destroy(p);
        _planes.Clear();
        Slider Skinslider = null;
        Slider Boneslider = null;
        try
        {
            Skinslider = GameObject.Find("SkinMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
            Boneslider = GameObject.Find("BoneMeshOpacity").transform.Find("Slider").GetComponent<Slider>();
        }
        catch
        {

        }
        if (Skinslider != null)
            Skinslider.value = 1;
        if (Boneslider != null)
            Boneslider.value = 1;

        _ChangePos.SetActive(false);
        _ChangeScale.SetActive(false);
        _ChangeRot.SetActive(false);
        _SetUpJoints.SetActive(false);
        _SetuppedConfigurator.SetActive(false);
        _ChooseRot.SetActive(false);
        _MosimSelector.SetActive(false);

        _transGiz.enabled = false;


        _ConfirmMapping.interactable = false;
        if (s < State.IsSetup && _state >= State.IsSetup)
        {
            _flycam.GetComponent<FlyCamController>().DeletePlanes();
            _flycam.GetComponent<FlyCamController>().ResetToNonOrtho();
            GameObject.Find("Avatar").GetComponent<CharacterMeshRendererController>().alpha = 1f;
            GameObject.Find("Avatar").GetComponent<TestIS>().ResetBase();
        }
        if (s <= State.JointMap && _state >= State.JointMap)
        {
            var avatar = GameObject.Find("Avatar");
            var skelvis = avatar.GetComponent<TestIS>().skelVis;
            if (skelvis != null)
                skelvis.root.Destroy();
        }
        if (s <= State.RotationAdjust)
        {
            _HideHands.gameObject.SetActive(false);
        }
        _flycam.gameObject.GetComponent<Camera>().orthographic = false;
        if (s == State.RootSelect)
        {
            _flycam.DefaultCamera();
        }
    }

    private void RevertInfluenceOfScripts(State s)
    {
        if (_state >= State.JointMap && s < State.JointMap)
        {
            //Hide all BoneVisualisation meshes
            var avatar = GameObject.Find("Avatar");
            ShredAvatar(avatar.transform, true);

            _transGiz.enabled = false;
            GameObject.Find("Avatar").GetComponent<TestIS>().ResetStarted();
        }
        if(_state == State.IsSetup && s != State.IsSetup)
        {
            var test = GameObject.Find("Avatar").GetComponent<TestIS>();
            if(test != null)
            {
                if (test.IsPlayingAnim())
                    test.PlayPauseExampleClip();
            }
        }
    }

    private void DestroyInfluenceOfScripts(State s)
    {
        if ( s < State.JointMap)
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

            ShredJointController();

            _SetUpJoints.SetActive(true);
            Transform obj = _SetUpJoints.transform.Find("Logic UI/JointMap/Viewport/Content");
            for (int i = 0; i < obj.childCount; i++)
            {
                Destroy(obj.GetChild(i).gameObject);
            }
            _SetUpJoints.SetActive(false);


        }
    }

    /// <summary>
    /// Recursive function to go over all transforms of the Avatar and look for childs which are not included in the bonelist and will be deleted.
    /// </summary>
    /// <param name="toshred">The bones of the skeleton (including avatar, root,..)</param>
    private void ShredAvatar(Transform toshred, bool hide = false)
    {
        if (hide)
        {
            for (int i = 0; i < toshred.childCount; i++)
            {
                if (!_bonelist.Contains(toshred.GetChild(i)))
                {
                    this._boneMeshes.Add(toshred.GetChild(i).gameObject);
                    toshred.GetChild(i).gameObject.SetActive(false);
                }
                else
                    ShredAvatar(toshred.GetChild(i), hide);
            }
        }
        else
        {
            for (int i = 0; i < toshred.childCount; i++)
            {
                if (!_bonelist.Contains(toshred.GetChild(i)))
                    Destroy(toshred.GetChild(i).gameObject);
                else
                    ShredAvatar(toshred.GetChild(i), hide);
            }
        }
    }

    private void ShredJointController(Transform toshred = null)
    {
        if(toshred == null)
            toshred = GameObject.Find("Avatar").transform;
        var brrrt = toshred.gameObject.GetComponent<JointController>();
        if (brrrt != null)
            Destroy(brrrt);

        for(int i = 0; i<toshred.childCount; i++)
        {
                ShredJointController(toshred.GetChild(i));
            
        }
    }

    private void DeleteWorldCanvas()
    {
        if (_state >= State.PositionAdjust && _HandSigns.Count > 0)
        {
            foreach (var ob in _HandSigns)
            {
                Destroy(ob);
            }
            _HandSigns.Clear();
        }
    }

    private void DisableOrEnableHandSigns(Text Child)
    {
        if (Child.text.Contains("Disable"))
        {
            foreach(var obj in _HandSigns)
            {
                obj.SetActive(false);
            }
            Child.text = Child.text.Replace("Disable", "Enable");

        } else
        {
            foreach (var obj in _HandSigns)
            {
                obj.SetActive(true);
            }
            Child.text = Child.text.Replace("Enable", "Disable");
        }

    }
}
