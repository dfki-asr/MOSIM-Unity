using MMICoSimulation;
using MMICSharp.MMIStandard.Utils;
using MMIStandard;
using MMIUnity.TargetEngine;
using MMIUnity.TargetEngine.Scene;
using UnityEngine;

using System.IO;

/// <summary>
/// Via this script the Avatar will perform walk to random spot and reach corresponding point commands. The corresponding scene can be used to record MMU and Cosimulator Timings.
/// </summary>
public class SingleAvatarBehaviour : AvatarBehavior
{
    public GameObject[] targets;

    public string filename = "test2.csv";

    private string status = "Start";
    private bool started = false;
    private int counter = 0;

    private TextWriter tw; 

    private readonly string MOTION_WALK = "Locomotion/Walk";
    private readonly string MOTION_IDLE = "Pose/Idle";
    private readonly string MOTION_REACH = "Pose/Reach";


    protected override void GUIBehaviorInput()
    {
        if (GUI.Button(new Rect(140, 10, 120, 25), "Walk to and Reach"))
        {
            InitiateBehaviour();
        }

        if (GUI.Button(new Rect(280, 10, 120, 25), "Stop Procedure"))
        {
            StopBehaviour();
        }
    }

    public void StopBehaviour()
    {
        this.CoSimulator.MSimulationEventHandler -= this.CoSimulator_MSimulationEventHandler;
        this.CoSimulator.Abort();
        // TODO: stop file writing
        started = false;
        CSVWriter.StopThread();
    }

    public void InitiateBehaviour()
    {
        this.CoSimulator.MSimulationEventHandler -= this.CoSimulator_MSimulationEventHandler;
        var randomTarget = randomizeSelection();

        //get Walktarget from random Selections
        var randomWalk = randomTarget.transform.GetChild(0).gameObject;
        var randomReachLeft = randomTarget.transform.GetChild(3).gameObject;

        MInstruction walkInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Walk", MOTION_WALK)
        {
            Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance.GetSceneObjectByName(randomWalk.name).ID)
        };

        MInstruction ReachInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Reach Left", MOTION_REACH)
        {
            Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance.GetSceneObjectByName(randomReachLeft.name).ID, "Hand", "Left"),
            StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
        };

        MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE)
        {
            //Start idle after walk has been finished
            StartCondition = ReachInstruction.ID + ":" + mmiConstants.MSimulationEvent_End //synchronization constraint similar to bml "id:End"  (bml original: <bml start="id:End"/>
        };

        //TODO: How to get the Reach MMU to end and fluidly go into idle? Maybe not important.. 
        // But also: Can we check if MMU is finished and if so: Repeat Cicle until timelimit is reached?

        this.CoSimulator.Abort();


        MSimulationState currentState = new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() };

        //Assign walk and idle instruction
        this.CoSimulator.AssignInstruction(walkInstruction, currentState);
        this.CoSimulator.AssignInstruction(ReachInstruction, currentState);
        this.CoSimulator.AssignInstruction(idleInstruction, currentState);
        this.CoSimulator.MSimulationEventHandler += this.CoSimulator_MSimulationEventHandler;

        //Startfile writing
        started = true;
        CSVWriter.StartConCurrentCSVWrite();
    }

    protected override void Start()
    {
        base.Start();
        CSVWriter.ChangeFileName(filename);
        CSVWriter.CreateCSVwithString("Counter;Frame;SystemTime;TimePerFrame;Status");
    }

    private void Update()
    {
        if (started) { 
            //Debug.Log(counter + " " + this.CoSimulator.FrameNumber + " "+ System.DateTime.Now.Ticks + " " + Time.deltaTime + " " + status);
            CSVWriter.AddToQueue( counter + ";" + this.CoSimulator.FrameNumber + ";" + System.DateTime.Now.Ticks/10000 + ";" + Time.deltaTime + ";" + status);
            counter++;
        }
        
    }

    private void OnDisable()
    {
        CSVWriter.StopThread();
    }


    private GameObject randomizeSelection()
    {
        return targets[UnityEngine.Random.Range(0, targets.Length - 1)];
    }

    /// <summary>
    /// Callback for the co-simulation event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CoSimulator_MSimulationEventHandler(object sender, MSimulationEvent e)
    {
        //Debug.Log(e.Reference + " " + e.Name + " " + e.Type);
        status = e.Name + " " + e.Type;
        if (e.Name == "Idle")
        {
            InitiateBehaviour();
        }
    }
}
