using MMICSharp.MMIStandard.Utils;
using MMIStandard;
using MMIUnity.TargetEngine.Scene;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// This script should be executed behore SimulationController
public class MultipleAvatarSceneManager : MonoBehaviour
{
    [Tooltip("Collider that is used to pick random walking point")]
    public MeshCollider PlaneCollider;

    [Tooltip("Walk target near the reach target")]
    public MMISceneObject WalkToReachTarget;

    [Tooltip("A scene object to reach")]
    public MMISceneObject ReachTarget;

    [Tooltip("Number of avatars to spawn at start up")]
    public int AvatarsNumber = 5;

    [Tooltip("Minimal distance between spawned at start up avatars")]
    public float MinSpawnDistance = 1;

    [Tooltip("The width of area on plane edge not used for point generation")]
    public float EdgeOffset = 1.0f;

    public ExposedAvatarBehaviour AvatarPrefab;
    public MMISceneObject WalkTargetPrefab;
    public CanvasGroup CanvasGroup;
    public Dropdown SelectedAvatarDropdown;
    public Text StatusText;
    
    private List<ExposedAvatarBehaviour> avatarBehaviours;
    private ExposedAvatarBehaviour selectedAvatar;
    private RaycastHit[] raycastCache;
    private int initializedAvatarsNum;
    
    private const string MOTION_IDLE = "Pose/Idle";
    private const string MOTION_WALK = "Locomotion/Walk";
    private const string MOTION_REACH = "Pose/Reach";

    private string _status;
    private bool _started = false;
    public string filename = "test.csv";
    private int counter = 0;
    private int _activeAgentCounter = 0;
    
    private void Start()
    {
        raycastCache = new RaycastHit[1];
        initializedAvatarsNum = 0;
        CanvasGroup.interactable = false;
        StatusText.text = "Avatars initialization in process...";

        int avatarIndex = 0;
        avatarBehaviours = FindObjectsOfType<ExposedAvatarBehaviour>().ToList();
        foreach (var existingAvatar in avatarBehaviours)
        {
            MMISceneObject walkTarget = Instantiate(WalkTargetPrefab);
            walkTarget.name = $"WalkTarget({existingAvatar.name})";
            walkTarget.transform.position = existingAvatar.transform.position;
            existingAvatar.LinkedWalkPoint = walkTarget;

            // Dirty trick to assign different services, fix by using one service
            // or extend the framework for natural assignment
            existingAvatar.MMISettings.RemoteSkeletonAccessPort += avatarIndex;
            ++avatarIndex;

            existingAvatar.MMIAvatar.OnInitialized += onAvatarInitialized;
        }

        for (int i = 0; i < AvatarsNumber; ++i)
        {
            spawnAvatarAndWalkTarget(avatarIndex);
            ++avatarIndex;
        }

        SelectedAvatarDropdown.options.AddRange(
            avatarBehaviours.Select(a => new Dropdown.OptionData(a.name)).ToArray());

        CSVWriter.ChangeFileName(filename);
        CSVWriter.CreateCSVwithString("Counter;Frame;SystemTime;TimePerFrame;Number of Agents;Statusses");
    }

    private void spawnAvatarAndWalkTarget(int avatarIndex)
    {
        int nameIndex = avatarIndex + 1;
        ExposedAvatarBehaviour newAvatar = Instantiate(AvatarPrefab);
        newAvatar.name = "Avatar" + nameIndex;
        newAvatar.transform.SetParent(transform);
        newAvatar.transform.position = getRandomPointOnPlane(
            100, avatarBehaviours.Select(a => a.transform.position),
                MinSpawnDistance, true);

        MMISceneObject walkTarget = Instantiate(WalkTargetPrefab);
        walkTarget.name = $"WalkTarget({newAvatar.name})";
        walkTarget.transform.position = newAvatar.transform.position;
        walkTarget.transform.SetParent(transform);
        newAvatar.LinkedWalkPoint = walkTarget;

        // Dirty trick to assign different services, fix by using one service
        // or extend the framework for natural assignment
        newAvatar.MMISettings.RemoteSkeletonAccessPort += avatarIndex;

        avatarBehaviours.Add(newAvatar);
        newAvatar.MMIAvatar.OnInitialized += onAvatarInitialized;
    }

    /// <summary>
    /// Get random point on plane that is far enough from listed points
    /// </summary>
    /// <param name="numOfTries">Number of tries to reposition the wrong result before giving up</param>
    /// <param name="occupiedPoints">The list of points that should remain far enough from new point</param>
    /// <param name="minDist">Minimal distance the new point </param>
    /// <returns></returns>
    private Vector3 getRandomPointOnPlane(int numOfTries, IEnumerable<Vector3> occupiedPoints, float minDist, bool raycast)
    {
        Vector3 result = Vector3.zero;
        float minSqDist = minDist * minDist;
        int c = 0;
        do
        {
            result = new Vector3(
                Random.Range(
                    -PlaneCollider.bounds.extents.x + EdgeOffset,
                    PlaneCollider.bounds.extents.x - EdgeOffset),
                PlaneCollider.bounds.center.y,
                Random.Range(
                    -PlaneCollider.bounds.extents.z + EdgeOffset,
                    PlaneCollider.bounds.extents.z - EdgeOffset));

            if (++c > numOfTries ||
                (!raycast || Physics.RaycastNonAlloc(result + 0.01f * Vector3.up, Vector3.up, raycastCache) == 0) && // no objects above
                occupiedPoints.All(p => (p - result).sqrMagnitude >= minSqDist)) // no other points in proximity
                break;
        }
        while (true);

        return result;
    }

    private void onAvatarInitialized(object sender, System.EventArgs e)
    {
        ++initializedAvatarsNum;
        StatusText.text = $"Avatars initialized: {initializedAvatarsNum} out of {avatarBehaviours.Count}";
        if (initializedAvatarsNum == avatarBehaviours.Count)
        {
            CanvasGroup.interactable = true;
            StatusText.text = "Avatars are ready";
        }
    }

    public void IdleAll()
    {
        foreach (var avatarBehaviour in avatarBehaviours)
        {
            MSkeletonAccess.Iface skeletonAccess = avatarBehaviour.MMIAvatar.GetSkeletonAccess();
            skeletonAccess.SetChannelData(avatarBehaviour.MMIAvatar.GetPosture());

            MInstruction instruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE);
            MSimulationState simstate = new MSimulationState(avatarBehaviour.MMIAvatar.GetPosture(), avatarBehaviour.MMIAvatar.GetPosture());

            avatarBehaviour.MMICoSimulator.Abort();
            avatarBehaviour.MMICoSimulator.AssignInstruction(instruction, simstate);
        }
    }

    public void IdleSelected()
    {
        if (selectedAvatar != null)
        {
            MSkeletonAccess.Iface skeletonAccess = selectedAvatar.MMIAvatar.GetSkeletonAccess();
            skeletonAccess.SetChannelData(selectedAvatar.MMIAvatar.GetPosture());

            MInstruction instruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE);
            MSimulationState simstate = new MSimulationState(
                selectedAvatar.MMIAvatar.GetPosture(), selectedAvatar.MMIAvatar.GetPosture());

            selectedAvatar.MMICoSimulator.Abort();
            selectedAvatar.MMICoSimulator.AssignInstruction(instruction, simstate);
        }
        else
            StatusText.text = "Select avatar first";
    }

    public void WalkAll()
    {
        _activeAgentCounter = avatarBehaviours.Count;
        for (int i = 0; i < avatarBehaviours.Count; ++i)
        {
            var avatarBehaviour = avatarBehaviours[i];

            // only avoid with set up points
            var occupiedPoints = avatarBehaviours.Take(i).Select(a => a.LinkedWalkPoint.transform.position);
            avatarBehaviour.LinkedWalkPoint.transform.position = getRandomPointOnPlane(
                100, occupiedPoints, MinSpawnDistance, true);

            MInstruction walkInstruction = new MInstruction(
                MInstructionFactory.GenerateID(), "Walk", MOTION_WALK)
            {
                Properties = PropertiesCreator.Create(
                    "TargetID", avatarBehaviour.LinkedWalkPoint.MSceneObject.ID)
            };

            MInstruction idleInstruction = new MInstruction(
                MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE)
            {
                StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
            };

            avatarBehaviour.MMICoSimulator.Abort();

            MSimulationState simstate = new MSimulationState(
                avatarBehaviour.MMIAvatar.GetPosture(), avatarBehaviour.MMIAvatar.GetPosture());

            //Assign walk and idle instruction
            avatarBehaviour.MMICoSimulator.AssignInstruction(walkInstruction, simstate);
            avatarBehaviour.MMICoSimulator.AssignInstruction(idleInstruction, simstate);
            //Added for CSV
            avatarBehaviour.MMICoSimulator.MSimulationEventHandler += CoSimulator_MSimulationEventHandler;
        }
        //Added for CSV2
        CSVWriter.StartConCurrentCSVWrite();
        _started = true;
    }

    public void WalkSelected()
    {
        if (selectedAvatar != null)
        {
            // don't consider current point
            var occupiedPoints = avatarBehaviours
                .Where(a => a != selectedAvatar)
                .Select(a => a.LinkedWalkPoint.transform.position);
            selectedAvatar.LinkedWalkPoint.transform.position = getRandomPointOnPlane(
                100, occupiedPoints, MinSpawnDistance, true);

            MInstruction walkInstruction = new MInstruction(
                MInstructionFactory.GenerateID(), "Walk", MOTION_WALK)
            {
                Properties = PropertiesCreator.Create(
                    "TargetID", selectedAvatar.LinkedWalkPoint.MSceneObject.ID)
            };

            MInstruction idleInstruction = new MInstruction(
                MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE)
            {
                StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
            };

            selectedAvatar.MMICoSimulator.Abort();

            MSimulationState simstate = new MSimulationState(
                selectedAvatar.MMIAvatar.GetPosture(), selectedAvatar.MMIAvatar.GetPosture());

            //Assign walk and idle instruction
            selectedAvatar.MMICoSimulator.AssignInstruction(walkInstruction, simstate);
            selectedAvatar.MMICoSimulator.AssignInstruction(idleInstruction, simstate);
            // Added for CSV
            _activeAgentCounter = 1;
            selectedAvatar.MMICoSimulator.MSimulationEventHandler += CoSimulator_MSimulationEventHandler;
            CSVWriter.StartConCurrentCSVWrite();
            _started = true;
        }
        else
            StatusText.text = "Select avatar first";
    }

    public void WalkAndReachSelected()
    {
        if (selectedAvatar != null)
        {
            MInstruction walkInstruction = new MInstruction(
                MInstructionFactory.GenerateID(), "Walk", MOTION_WALK)
            {
                Properties = PropertiesCreator.Create(
                    "TargetID", WalkToReachTarget.MSceneObject.ID)
            };

            MInstruction reachRight = new MInstruction(MInstructionFactory.GenerateID(), "reach right", MOTION_REACH)
            {
                Properties = PropertiesCreator.Create(
                    "TargetID", ReachTarget.MSceneObject.ID,
                    "Hand", "Left",
                    "MinDistance", "2.0"),
                StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
            };

            //Was missing.
            selectedAvatar.MMICoSimulator.Abort();

            MSimulationState simstate = new MSimulationState(
                selectedAvatar.MMIAvatar.GetPosture(), selectedAvatar.MMIAvatar.GetPosture());
            selectedAvatar.MMICoSimulator.AssignInstruction(walkInstruction, simstate);
            selectedAvatar.MMICoSimulator.AssignInstruction(reachRight, simstate);
            //Added for CSV
            _activeAgentCounter = 1;
            selectedAvatar.MMICoSimulator.MSimulationEventHandler += CoSimulator_MSimulationEventHandler;
            CSVWriter.StartConCurrentCSVWrite();
            _started = true;
        }
        else
            StatusText.text = "Select avatar first";
    }

    public void OnSelectedAvatarChanged(int selectedIndex)
    {
        if (selectedIndex == 0)
            selectedAvatar = null;
        else
        {
            selectedAvatar = avatarBehaviours[selectedIndex - 1];
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(PlaneCollider.transform.position,
            new Vector3(PlaneCollider.bounds.size.x - 2 * EdgeOffset, 0,
                    PlaneCollider.bounds.size.x - 2 * EdgeOffset));

        if(selectedAvatar != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0);
            Vector3 markerSize = new Vector3(1, 0, 1);
            Gizmos.DrawWireCube(selectedAvatar.transform.position, markerSize);
        }
    }

    //Added for CSV
    private void Update()
    {
        if (_started)
        {
            //Debug.Log(counter + " " + this.CoSimulator.FrameNumber + " "+ System.DateTime.Now.Ticks + " " + Time.deltaTime + " " + status);
            CSVWriter.AddToQueue(counter + ";" + avatarBehaviours[0].MMICoSimulator.FrameNumber + ";" + System.DateTime.Now.Ticks / 10000 + ";" + Time.deltaTime + ";" + avatarBehaviours.Count + ";" +  _status);
            counter++;
        }
    }

    private void Awake()
    {
        //Parse Numbers in international instead German format.
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
    }

    private void StopBehaviour()
    {
        for (int i = 0; i < avatarBehaviours.Count; ++i)
        {
            avatarBehaviours[i].MMICoSimulator.MSimulationEventHandler -= this.CoSimulator_MSimulationEventHandler;
        }
        _started = false;
        CSVWriter.StopThread();

    }

    /// <summary>
    /// Callback for the co-simulation event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CoSimulator_MSimulationEventHandler(object sender, MSimulationEvent e)
    {
        //Debug.Log(e.Reference + " " + e.Name + " " + e.Type);
        _status = "(";
        for(int i = 0; i< avatarBehaviours.Count; i++)
        {
            if (i > 0)
                _status += ", ";

            if (avatarBehaviours[i].MMICoSimulator.GetActiveInstructions().Count > 0)
            {
                _status += avatarBehaviours[i].MMICoSimulator.GetActiveInstructions()[0].Name;
            } else
            {
                _status += "No Instruction";
            }
        }
        _status += ")";
        if (e.Name == "Idle" || (e.Name.Contains("reach") && e.Type=="end"))
        {
            _activeAgentCounter--;
            //Debug.Log(_activeAgentCounter);
            if (_activeAgentCounter <= 0)
            {
                StopBehaviour();
                Debug.Log("Stopped");
                _activeAgentCounter = 0;
            }
        }
    }
}
