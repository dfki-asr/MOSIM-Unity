using MMICoSimulation;
using MMICSharp.MMIStandard.Utils;
using MMIStandard;
using MMIUnity.TargetEngine;
using MMIUnity.TargetEngine.Scene;
using MMICSharp;
using UnityEngine;


public class BerlinDemoBehavior : AvatarBehavior
{
     private string carryID;

    // Motion Types
    private readonly string MOTION_CARRY ="Object/Carry";
    private readonly string MOTION_GAZE = "Pose/Gaze";
    private readonly string MOTION_IDLE = "Pose/Idle";
    private readonly string MOTION_MOVEFINGERS = "Pose/MoveFingers";
    private readonly string MOTION_MOVE= "Object/Move";
    private readonly string MOTION_REACH = "Pose/Reach";
    private readonly string MOTION_RELEASE = "Object/Release";
    private readonly string MOTION_SIMPLE = "Object/Test";
    private readonly string MOTION_TURN = "Object/Turn";
    private readonly string MOTION_WALK = "Locomotion/Walk";


    protected override void GUIBehaviorInput()
{
        if (GUI.Button(new Rect(10, 10, 120, 50), "Idle"))
        {
            MSkeletonAccess.Iface skeletonAccess = this.avatar.GetSkeletonAccess();
            skeletonAccess.SetChannelData(this.avatar.GetPosture());

            MInstruction instruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID);
            MSimulationState simstate = new MSimulationState(this.avatar.GetPosture(), this.avatar.GetPosture());

            this.CoSimulator.Abort();
            this.CoSimulator.AssignInstruction(instruction, simstate);
        }
        if (GUI.Button(new Rect(140, 10, 120, 50), "Walk to"))
        {
            MInstruction walkInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Walk", MOTION_WALK, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance.GetSceneObjectByName("WalkTarget1").ID, "Velocity", velocity.ToString("0.00"))
            };

            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID)
            {
                //Start idle after walk has been finished
                StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End //synchronization constraint similar to bml "id:End"  (bml original: <bml start="id:End"/>
            };

            //this.CoSimulator.Abort();

            MSimulationState currentState = new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() };

            //Assign walk and idle instruction
            this.CoSimulator.AssignInstruction(walkInstruction, currentState);
            this.CoSimulator.AssignInstruction(idleInstruction, currentState);
            this.CoSimulator.MSimulationEventHandler += this.CoSimulator_MSimulationEventHandler;
        }


        if (GUI.Button(new Rect(270, 10, 120, 50), "Reach Object"))
        {

            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID);


            MInstruction reachRight = new MInstruction(MInstructionFactory.GenerateID(), "reach right", MOTION_REACH, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance["GraspTargetR"].ID, "Hand", "Right", "MinDistance", 2.0.ToString()),//, "SingleShotIK", true.ToString()),
            };

            this.CoSimulator.Abort();
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(reachRight, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
        }


        if (GUI.Button(new Rect(400, 10, 120, 50), "Move Object"))
        {
            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID);

            MInstruction moveObject = new MInstruction(MInstructionFactory.GenerateID(), "move object", MOTION_MOVE, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create("SubjectID", UnitySceneAccess.Instance["GraspObject"].ID, "Hand", "Right", "TargetID", UnitySceneAccess.Instance["PositioningTarget"].ID),
            };

            this.CoSimulator.Abort();
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(moveObject, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
        }

        if (GUI.Button(new Rect(530, 10, 120, 50), "Carry Object"))
        {
            MInstruction carryInstruction = null;
            carryID = MInstructionFactory.GenerateID();
            MVector3 objPos = UnitySceneAccess.Instance["GraspObject"].Transform.Position;
            MVector3 handPos = this.avatar.GetSkeletonAccess().GetGlobalJointPosition(this.avatar.MAvatar.ID, MJointType.RightWrist);
            if(false && objPos.Subtract(handPos).Magnitude() > 0.15)
            {
                Debug.Log("Additional Reach Required");
                MInstruction reachRight = new MInstruction(MInstructionFactory.GenerateID(), "reach right", MOTION_REACH, this.avatar.MAvatar.ID)
                {
                    Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance["GraspTargetR"].ID, "Hand", "Right", "MinDistance", 2.0.ToString()),
                };
                this.CoSimulator.AssignInstruction(reachRight, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
                carryInstruction = new MInstruction(carryID, "carry object", MOTION_CARRY, this.avatar.MAvatar.ID)
                {
                    Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance["GraspObject"].ID, "Hand", "Right"),//, "CarryTarget", UnitySceneAccess.Instance["CarryTarget"].ID),
                    StartCondition = reachRight.ID + ":" + mmiConstants.MSimulationEvent_End //synchronization constraint similar to bml "id:End"  (bml original: <bml start="id:End"/>
                };

            }
            else
            {
                carryInstruction = new MInstruction(carryID, "carry object", MOTION_CARRY, this.avatar.MAvatar.ID)
                {
                    Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance["GraspObject"].ID, "Hand", "Right")//, "CarryTarget", UnitySceneAccess.Instance["CarryTarget"].ID),
                };
            }
            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID);



            this.CoSimulator.Abort();
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(carryInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
        }

        if (GUI.Button(new Rect(660, 10, 120, 50), "Walk to other Goal"))
        {
            MInstruction walkInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Walk", MOTION_WALK, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance.GetSceneObjectByName("WalkTarget2").ID, "Velocity", velocity.ToString("0.00"))
            };
            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID)
            {
                //Start idle after walk has been finished
                StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End //synchronization constraint similar to bml "id:End"  (bml original: <bml start="id:End"/>
            };
            this.CoSimulator.AssignInstruction(walkInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });

        }

        if (GUI.Button(new Rect(790, 10, 120, 50), "Place Object"))
        {
            MInstruction moveObject = new MInstruction(MInstructionFactory.GenerateID(), "move object", MOTION_MOVE, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create("SubjectID", UnitySceneAccess.Instance["GraspObject"].ID, "Hand", "Right", "TargetID", UnitySceneAccess.Instance["PositioningTargetRight"].ID),
            };

            MInstruction releaseRight = new MInstruction(MInstructionFactory.GenerateID(), "release object", MOTION_RELEASE, this.avatar.MAvatar.ID)
            {
                Properties = PropertiesCreator.Create( "Hand", "Right", CoSimTopic.OnStart, carryID + ":" + CoSimAction.EndInstruction),
                StartCondition = moveObject.ID + ":" + mmiConstants.MSimulationEvent_End
            };
            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, this.avatar.MAvatar.ID)
            {
                //Start idle after walk has been finished
                StartCondition = releaseRight.ID + ":" + mmiConstants.MSimulationEvent_End //synchronization constraint similar to bml "id:End"  (bml original: <bml start="id:End"/>
            };


            this.CoSimulator.AssignInstruction(moveObject, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(releaseRight, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });

        }

    }

    /// <summary>
    /// Callback for the co-simulation event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CoSimulator_MSimulationEventHandler(object sender, MSimulationEvent e)
    {
        Debug.Log(e.Reference + " " + e.Name + " " + e.Type);
    }

}
