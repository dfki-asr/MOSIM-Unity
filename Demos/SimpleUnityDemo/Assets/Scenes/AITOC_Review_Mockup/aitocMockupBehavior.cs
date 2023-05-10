using MMICoSimulation;
using MMICSharp.MMIStandard.Utils;
using MMIStandard;
using MMIUnity.TargetEngine;
using MMIUnity.TargetEngine.Scene;
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class InstructionWrapper
{
    [SerializeField]
    public MInstruction instr;
    public bool continuous = false;
    public string lastID = "";
}

public class aitocMockupBehavior : AvatarBehavior
{
    private readonly string MOTION_CARRY = "Object/Carry";
    private readonly string MOTION_GAZE = "Pose/Gaze";
    private readonly string MOTION_IDLE = "Pose/Idle";
    private readonly string MOTION_MOVEFINGERS = "Pose/MoveFingers";
    private readonly string MOTION_MOVE = "Object/Move";
    private readonly string MOTION_REACH = "Pose/Reach";
    private readonly string MOTION_RELEASE = "Object/Release";
    private readonly string MOTION_SIMPLE = "Object/Test";
    private readonly string MOTION_TURN = "Object/Turn";
    private readonly string MOTION_WALK = "Locomotion/Walk";
    private readonly string MOTION_GRASP = "Pose/Grasp";

    [SerializeField]
    public string lastInstr = "";

    private List<MInstruction> logicSteps = new List<MInstruction>();
    //public List<string> continuousInstr = new List<string>();

    public UnityHandPoseSimple LeftHand;
    public UnityHandPoseSimple rightHand;

    private MInstruction BothHandedGrasp(string avatarID, string graspTargetLeft, string graspTargetRight, UnityHandPoseSimple left, UnityHandPoseSimple right)
    {
        // Grasp Object
        var idReachLeft = MInstructionFactory.GenerateID();
        var idReachRight = MInstructionFactory.GenerateID();

        MInstruction multiInstruction = new MInstruction(MInstructionFactory.GenerateID(), "BothHandedGrasp", "multi", avatarID) { Instructions = new List<MInstruction>() };
        
        MInstruction reachLeft = new MInstruction(idReachLeft, "reachLeft", MOTION_REACH, avatarID)
        {
            Properties = PropertiesCreator.Create("TargetID", graspTargetLeft, "Hand", "Left"),
        };
        multiInstruction.Instructions.Add(reachLeft);
        //instructionList.Add(new InstructionWrapper() { instr = reachLeft });

        MInstruction reachRight = new MInstruction(idReachRight, "reachRight", MOTION_REACH, avatarID)
        {
            Properties = PropertiesCreator.Create("TargetID", graspTargetRight, "Hand", "Right"),
            //StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(reachRight);
        //instructionList.Add(new InstructionWrapper() { instr = reachRight });

        MConstraint GraspConstraint = right.GetGraspConstraint(false);
        MInstruction graspRight = new MInstruction(MInstructionFactory.GenerateID(), "graspRight", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Right", "HandPose", GraspConstraint.ID, "KeepHandPose", "False"),
            Constraints = new List<MConstraint>() { GraspConstraint },
            StartCondition = idReachRight + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(graspRight);
        //instructionList.Add(new InstructionWrapper() { instr = graspRight, continuous=true});

        MConstraint GraspConstraint2 = left.GetGraspConstraint(false);
        MInstruction graspLeft = new MInstruction(MInstructionFactory.GenerateID(), "graspLeft", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Left", "HandPose", GraspConstraint2.ID, "KeepHandPose", "False"),
            Constraints = new List<MConstraint>() { GraspConstraint2 },
            StartCondition = idReachLeft + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(graspLeft);
        return multiInstruction;
    }

    public MInstruction BothHandedCarryToPosition(string avatarID, string objectID, string carryTarget, string WalkTarget)
    {
        var carryWalkID = MInstructionFactory.GenerateID();

        MInstruction multiInstruction = new MInstruction(MInstructionFactory.GenerateID(), "BothHandedCarryToPosition", "multi", avatarID) { Instructions = new List<MInstruction>() };

        MInstruction carryInstructionBoth = new MInstruction(MInstructionFactory.GenerateID(), "carry object", MOTION_CARRY, avatarID)
        {
            Properties = PropertiesCreator.Create("TargetID", objectID, "Hand", "Both", "CarryTargetID", carryTarget),
            EndCondition = carryWalkID + ":" + mmiConstants.MSimulationEvent_End,

            // StartCondition = reachLeft.ID + ":" + mmiConstants.MSimulationEvent_End + " && " + reachRight.ID + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(carryInstructionBoth);
        //instructionList.Add(new InstructionWrapper() { instr = carryInstructionBoth, continuous = true });

        // grasping while walking: 
        MConstraint GraspConstraint = rightHand.GetGraspConstraint(false);
        MInstruction graspRight = new MInstruction(MInstructionFactory.GenerateID(), "graspRight", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Right", "HandPose", GraspConstraint.ID),
            Constraints = new List<MConstraint>() { GraspConstraint },
            EndCondition = carryWalkID + ":" + mmiConstants.MSimulationEvent_End,
        };
        multiInstruction.Instructions.Add(graspRight);
        //instructionList.Add(new InstructionWrapper() { instr = graspRight, continuous=true});

        MConstraint GraspConstraint2 = LeftHand.GetGraspConstraint(false);
        MInstruction graspLeft = new MInstruction(MInstructionFactory.GenerateID(), "graspLeft", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Left", "HandPose", GraspConstraint2.ID),
            Constraints = new List<MConstraint>() { GraspConstraint2 },
            EndCondition = carryWalkID + ":" + mmiConstants.MSimulationEvent_End,
        };
        multiInstruction.Instructions.Add(graspLeft);


        MInstruction walkInstruction2 = new MInstruction(carryWalkID, "Walk", MOTION_WALK, avatarID)
        {
            Properties = PropertiesCreator.Create("TargetID", WalkTarget)
        };
        multiInstruction.Instructions.Add(walkInstruction2);
        return multiInstruction;
    }

    public MInstruction BothHandedPlace(string avatarID, string objectID, string targetID)
    {
        var carryID2 = MInstructionFactory.GenerateID();
        MInstruction multiInstruction = new MInstruction(MInstructionFactory.GenerateID(), "BothHandedPlace", "multi", avatarID) { Instructions = new List<MInstruction>() };
        MInstruction carryInstructionBoth2 = new MInstruction(carryID2, "carry object to target", MOTION_CARRY, avatarID)
        {
            Properties = PropertiesCreator.Create("TargetID", objectID, "Hand", "Both", "CarryTargetID", targetID, "FinishWithGoal", "True"),
        };
        multiInstruction.Instructions.Add(carryInstructionBoth2);
        //instructionList.Add(new InstructionWrapper() { instr = carryInstructionBoth2 });

        // grasping while walking: 
        MConstraint GraspConstraint = rightHand.GetGraspConstraint(false);
        MInstruction graspRight = new MInstruction(MInstructionFactory.GenerateID(), "graspRight", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Right", "HandPose", GraspConstraint.ID),
            Constraints = new List<MConstraint>() { GraspConstraint },
            EndCondition = carryID2 + ":" + mmiConstants.MSimulationEvent_End,
        };
        multiInstruction.Instructions.Add(graspRight);
        //instructionList.Add(new InstructionWrapper() { instr = graspRight, continuous=true});

        MConstraint GraspConstraint2 = LeftHand.GetGraspConstraint(false);
        MInstruction graspLeft = new MInstruction(MInstructionFactory.GenerateID(), "graspLeft", MOTION_GRASP, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Left", "HandPose", GraspConstraint2.ID),
            Constraints = new List<MConstraint>() { GraspConstraint2 },
            EndCondition = carryID2 + ":" + mmiConstants.MSimulationEvent_End,
        };
        multiInstruction.Instructions.Add(graspLeft);



        MInstruction release = new MInstruction(MInstructionFactory.GenerateID(), "release", MOTION_RELEASE, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Left"),
            StartCondition = carryID2 + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(release);
        //instructionList.Add(new InstructionWrapper() { instr = release, continuous = true });

        MInstruction release2 = new MInstruction(MInstructionFactory.GenerateID(), "release", MOTION_RELEASE, avatarID)
        {
            Properties = PropertiesCreator.Create("Hand", "Right"),
            StartCondition = carryID2 + ":" + mmiConstants.MSimulationEvent_End
        };
        multiInstruction.Instructions.Add(release2);
        return multiInstruction;

    }

    protected override void GUIBehaviorInput()
    {
        string avatarID = this.avatar.MAvatar.ID; 

        if (GUI.Button(new Rect(10, 10, 220, 50), "Pickup"))
        {
            // create underlying idle animation
            MInstruction idleInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Idle", MOTION_IDLE, avatarID)
            {
                //StartCondition = walkInstruction.ID + ":" + mmiConstants.MSimulationEvent_End
            };
            this.CoSimulator.AssignInstruction(idleInstruction, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });


            // generate mockup logic steps 
            MInstruction walkInstruction = new MInstruction(MInstructionFactory.GenerateID(), "Walk", MOTION_WALK, avatarID)
            {
                Properties = PropertiesCreator.Create("TargetID", UnitySceneAccess.Instance.GetSceneObjectByName("WalkTarget1").ID)
            };
            logicSteps.Add(walkInstruction);

            MInstruction bothHandedGrasp = BothHandedGrasp(avatarID, UnitySceneAccess.Instance["LeftHand"].ID, UnitySceneAccess.Instance["RightHand"].ID, LeftHand, rightHand);
            logicSteps.Add(bothHandedGrasp);

            MInstruction bothHandedCarry = BothHandedCarryToPosition(avatarID, UnitySceneAccess.Instance["LargeObject"].ID, UnitySceneAccess.Instance["CarryTarget"].ID, UnitySceneAccess.Instance["WalkTarget2"].ID);
            logicSteps.Add(bothHandedCarry);
            

            MInstruction bothHandedPlace = BothHandedPlace(avatarID, UnitySceneAccess.Instance["LargeObject"].ID, UnitySceneAccess.Instance["Target"].ID);
            logicSteps.Add(bothHandedPlace);

            this.CoSimulator.MSimulationEventHandler += this.CoSimulator_MSimulationEventHandler;

            // start simulation
            nextStep();

        }
    }

    private void nextStep()
    {
        if(this.logicSteps.Count > 0)
        {
            MInstruction inst = this.logicSteps[0];
            this.CoSimulator.AssignInstruction(inst, new MSimulationState() { Initial = this.avatar.GetPosture(), Current = this.avatar.GetPosture() });
            this.logicSteps.RemoveAt(0);
            lastInstr = inst.ID;
        }
        
    }

    private void CoSimulator_MSimulationEventHandler(object sender, MSimulationEvent e)
    {
        Debug.Log(e.Reference + " " + e.Name + " " + e.Type + " " +  e.Properties);

        if (e.Type == "end" && lastInstr == e.Reference)
        {
            nextStep();
        }
    }
}
