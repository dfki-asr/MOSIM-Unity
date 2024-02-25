

using MMICSharp.Common;
using MMICSharp.Common.Attributes;
using MMIStandard;
using MMIUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScrewManual : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();

    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "ScrewManual";

        //Assign the motion type of the MMU
        this.MotionType = "Object/Screw/Manual";

		//Auto generated source code for bone type mapping:
		this.BoneTypeMapping = new Dictionary<string,MJointType>()
		{
			{"pelvis", MJointType.PelvisCentre},
			{"thigh_l", MJointType.LeftHip},
			{"thigh_r", MJointType.RightHip},
			{"calf_l", MJointType.LeftKnee},
			{"calf_r", MJointType.RightKnee},
			{"foot_l", MJointType.LeftAnkle},
			{"foot_r", MJointType.RightAnkle},
			{"spine_01", MJointType.S1L5Joint},
			{"spine_02", MJointType.T1T2Joint},
			{"neck_01", MJointType.C4C5Joint},
			{"head", MJointType.HeadJoint},
			{"upperarm_l", MJointType.LeftShoulder},
			{"upperarm_r", MJointType.RightShoulder},
			{"lowerarm_l", MJointType.LeftElbow},
			{"lowerarm_r", MJointType.RightElbow},
			{"hand_l", MJointType.LeftWrist},
			{"hand_r", MJointType.RightWrist},
			{"ball_l", MJointType.LeftBall},
			{"ball_r", MJointType.RightBall},
			{"thumb_01_l", MJointType.LeftThumbTip},
			{"thumb_02_l", MJointType.LeftThumbMid},
			{"thumb_03_l", MJointType.LeftThumbCarpal},
			{"index_01_l", MJointType.LeftIndexProximal},
			{"index_02_l", MJointType.LeftIndexMeta},
			{"index_03_l", MJointType.LeftIndexDistal},
			{"middle_01_l", MJointType.LeftMiddleProximal},
			{"middle_02_l", MJointType.LeftMiddleMeta},
			{"middle_03_l", MJointType.LeftMiddleDistal},
			{"ring_01_l", MJointType.LeftRingProximal},
			{"ring_02_l", MJointType.LeftRingMeta},
			{"ring_03_l", MJointType.LeftRingDistal},
			{"pinky_01_l", MJointType.LeftLittleProximal},
			{"pinky_02_l", MJointType.LeftLittleMeta},
			{"pinky_03_l", MJointType.LeftLittleDistal},
			{"thumb_01_r", MJointType.RightThumbTip},
			{"thumb_02_r", MJointType.RightThumbMid},
			{"thumb_03_r", MJointType.RightThumbCarpal},
			{"index_01_r", MJointType.RightIndexProximal},
			{"index_02_r", MJointType.RightIndexMeta},
			{"index_03_r", MJointType.RightIndexDistal},
			{"middle_01_r", MJointType.RightMiddleProximal},
			{"middle_02_r", MJointType.RightMiddleMeta},
			{"middle_03_r", MJointType.RightMiddleDistal},
			{"ring_01_r", MJointType.RightRingProximal},
			{"ring_02_r", MJointType.RightRingMeta},
			{"ring_03_r", MJointType.RightRingDistal},
			{"pinky_01_r", MJointType.RightLittleProximal},
			{"pinky_02_r", MJointType.RightLittleMeta},
			{"pinky_03_r", MJointType.RightLittleDistal},
		};

		//Auto generated source code for assignemnt of root transform and root bone:
		this.Pelvis = this.gameObject.GetComponentsInChildren<Transform>().First(s=>s.name == "pelvis");
		this.RootTransform = this.transform;

		//It is important that the bone assignment is done before the base class awake is called
		base.Awake();
    }


    // Start is called before the first frame update
    protected override void Start()
    {
        //All required scripts should be added in here

		//Auto generated source code for script initialization.

		base.Start();

    }

    /// <summary>
    /// Basic initialize routine. This routine is called when the MMU is initialized. 
    /// The method must be implemented by the MMU developer.
    /// </summary>
    /// <param name="avatarDescription"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public override MBoolResponse Initialize(MAvatarDescription avatarDescription, Dictionary<string, string> properties)
    {
		//Execute instructions on main thread
        this.ExecuteOnMainThread(() =>
        {
			//Call the base class initialization
            base.Initialize(avatarDescription, properties);


            //To do -> insert your initialization code in here

        });

        IKName = System.Guid.NewGuid().ToString();
        avatarDescription.AvatarID = IKName;
        this.ServiceAccess.IKService.Setup(avatarDescription, new Dictionary<string, string>());

        return new MBoolResponse(true);
    }

    public string defaultHandedness = "right";
    public string handedness;
    public MSceneObject screwObject;
    public MSceneObject screwdriverObject;
    public MInstruction instruction;
    public MConstraint referenceConstraint;
    public float screwLength = 0;
    MVector3 startPosition;
    MQuaternion startRotation;

    private double dummyCounter = 0;
    private double dummyDuration = 3;

    // Constraints for grasping points on the target object
    public List<MConstraint> targetObjectGraspConstraint = new List<MConstraint>();
    private string IKName;


    private MBoolResponse ParseInstruction(MInstruction instruction, out string handedness, out MSceneObject targetObject, out MSceneObject screwdriverObject, out MConstraint referenceConstraint)
    {
        MBoolResponse resp = new MBoolResponse(true);
        handedness = defaultHandedness;
        targetObject = null; screwdriverObject = null;
        referenceConstraint = null;

        MSceneObject goalObject;
        if (instruction.Properties.ContainsKey("ScrewObject"))
        {
            string objectID = instruction.Properties["ScrewObject"];
            goalObject = this.SceneAccess.GetSceneObjectByID(objectID);
            if (goalObject == null)
            {
                goalObject = this.SceneAccess.GetSceneObjectByName(objectID);
                if (goalObject == null)
                {
                    resp.Successful = false;
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"Screw Object with ID {objectID} not found");
                    resp.LogData.Add($"Screw Object with ID {objectID} not found");
                    return resp;
                }
            }
        }
        else
        {
            resp.Successful = false;
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "No Screw Object ID provided");
            resp.LogData.Add("No Screw Object ID provided");
            return resp;
        }
        targetObject = goalObject;

        if(instruction.Properties.ContainsKey("ReferenceConstraint"))
        {
            var key = instruction.Properties["ReferenceConstraint"];
            if(!instruction.Constraints.TryGetConstraint(key, out referenceConstraint))
            {
                if(!targetObject.Constraints.TryGetConstraint(key, out referenceConstraint))
                {
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"No reference constraint with id {key} found in instruction or object.");
                    resp.LogData.Add($"No reference constraint with id {key} found in instruction or object.");
                }
            }
        }
        if(referenceConstraint != null)
        {
            referenceConstraint.GeometryConstraint = referenceConstraint.GeometryConstraint.MakeGlobalConstraint(this.SceneAccess);
        }
        

        if (instruction.Properties.ContainsKey("ScrewdriverObject"))
        {
            string objectID = instruction.Properties["ScrewdriverObject"];
            screwdriverObject = this.SceneAccess.GetSceneObjectByID(objectID);
            if (screwdriverObject == null)
            {
                screwdriverObject = this.SceneAccess.GetSceneObjectByName(objectID);
                if (screwdriverObject == null)
                {
                    resp.Successful = false;
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"Screwdriver Object with ID {objectID} not found");
                    resp.LogData.Add($"Screwdriver Object with ID {objectID} not found");
                    return resp;
                }
            }
        }
        else
        {
            resp.Successful = false;
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "No Goal Object ID provided");
            resp.LogData.Add("No Screwdriver Object ID provided");
            return resp;
        }

        targetObjectGraspConstraint.Clear();
        if (screwdriverObject.Constraints.Count > 0)
        {
            foreach (var c in screwdriverObject.Constraints)
            {
                if (c.ID.Contains("GraspingPoint") && c.PostureConstraint != null)
                {
                    this.targetObjectGraspConstraint.Add(c);
                }
                else if (c.ID.Contains("GraspingPoint") && c.GeometryConstraint != null)
                {
                    MJointType wrist = MJointType.RightWrist;
                    if (c.ID == "GraspingPointL") { wrist = MJointType.LeftWrist; }
                    var dummy_pc = new MPostureConstraint(new MAvatarPostureValues(this.AvatarID, new List<double>()))
                    {
                        JointConstraints = new List<MJointConstraint>()
                        {
                            new MJointConstraint(wrist)
                            {
                                GeometryConstraint = c.GeometryConstraint
                            }
                        }
                    };
                    c.PostureConstraint = dummy_pc;
                    this.targetObjectGraspConstraint.Add(c);
                }
            }
        }

        if (instruction.Properties.ContainsKey("Handedness"))
        {
            handedness = instruction.Properties["Handedness"];
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "No Handedness provided, using default handedness.");
        }

        return resp;
    }


    /// <summary>
    /// Method to assign a MInstriction to the MMU:
    /// The method must be provided by the MMU developer.
    /// </summary>
    /// <param name="motionCommand"></param>
    /// <param name="avatarState"></param>
    /// <returns></returns>
    /// 
    [MParameterAttribute("ScrewObject", "string", "Identifier of the object to be retrieved", true)]
    [MParameterAttribute("ScrewdriverObject", "string", "Identifier of the object to be retrieved", true)]
    [MParameterAttribute("Handedness", "string", "Handedness of retrieval: {left, right, both}. Default right", false)]
    [MParameterAttribute("ReferenceConstraint", "string", "Identifier of the objects constraint in the screwed configuration", false)]
    public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState state)
    {
        //To do -> insert your assignment code in here
        MBoolResponse resp = new MBoolResponse(true);
        MTransform rootT = MTransformExtensions.Identity();
        //Execute instructions on main thread
        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            this.SkeletonAccess.SetChannelData(state.Current);

            rootT = new MTransform("", this.SkeletonAccess.GetRootPosition(state.Current.AvatarID), this.SkeletonAccess.GetRootRotation(state.Current.AvatarID), new MVector3(1, 1, 1));
        });

        MSceneObject screwObject; MSceneObject screwdriverObject; string handed; MConstraint refConstraint;
        resp = ParseInstruction(instruction, out handed, out screwObject, out screwdriverObject, out refConstraint);
        if (!resp.Successful) { return resp; }

        this.instruction = instruction;
        this.handedness = handed;
        this.screwdriverObject = screwdriverObject;
        this.screwObject = screwObject;
        this.referenceConstraint = refConstraint;

        dummyCounter = 0;

        this.screwLength = 0;
        if (referenceConstraint != null)
        {
            var sScrewlenght = "";
            if (referenceConstraint.Properties.TryGetValue("Length", out sScrewlenght))
            {
                this.screwLength = float.Parse(sScrewlenght, System.Globalization.NumberStyles.Any) / 1000.0f;
            } else
            {
                MMICSharp.Logger.LogError("ScrewManual: missing Parameter Length on reference constraint. Using default length");
                this.screwLength = 40 / 1000.0f;
            }
        }
        this.startPosition = this.screwObject.Transform.Position.Clone();
        this.startRotation = this.screwObject.Transform.Rotation.Clone();

        if (screwObject.Constraints != null)
        {
            foreach (MConstraint c in screwObject.Constraints)
            {
                if (c.ID == "Screw head" && c.GeometryConstraint != null)
                {
                    startPosition = startPosition.Add(startRotation.Multiply(c.GeometryConstraint.ParentToConstraint.Position));

                }
                if (c.ID.Contains("shaft") && c.GeometryConstraint != null)
                {
                    startRotation = startRotation.Multiply(c.GeometryConstraint.ParentToConstraint.Rotation);
                }
            }
        }

        return resp;
    }

    /// <summary>
    /// Basic do step routine. This method must be implemented by the MMU developer.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="avatarState"></param>
    /// <returns></returns>
    public override MSimulationResult DoStep(double time, MSimulationState state)
    {
        //Create a new simulation result
        MSimulationResult result = new MSimulationResult()
        {
            Posture = state.Current,
            Constraints = state.Constraints!=null ? state.Constraints: new List<MConstraint>(),
            Events = state.Events !=null? state.Events: new List<MSimulationEvent>(),
            SceneManipulations = state.SceneManipulations!=null ? state.SceneManipulations: new List<MSceneManipulation>(),
        };

        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            this.SkeletonAccess.SetChannelData(state.Current);
            this.AssignPostureValues(state.Current);
        });

        MVector3 goalPos = this.screwdriverObject.Transform.Position;
        MQuaternion goalRot = this.screwdriverObject.Transform.Rotation;
        
        var hand = MJointType.RightWrist;
        string key = "GraspingPointR";
        if (this.handedness == "left")
        {
            hand = MJointType.LeftWrist;
            key = "GraspingPointL";
        }


        if (this.targetObjectGraspConstraint.Count > 0)
        {
            foreach (MConstraint c in targetObjectGraspConstraint)
            {
                if (c.ID == key)
                {
                    var jc = c.PostureConstraint.JointConstraints[0];
                    goalPos = goalPos.Add(goalRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                    goalRot = (goalRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Rotation));
                }
            }
        }

        //result.Posture = this.IKBlendToTarget(result.Posture, goalPos, goalRot, hand, 1.0f);
        //this.SkeletonAccess.SetChannelData(result.Posture);


        //To do -> insert your do step code in here
        double percentage = (dummyCounter / dummyDuration);

        result.SceneManipulations.Add(new MSceneManipulation()
        {
            Transforms = new List<MTransformManipulation>()
                 {
                      new MTransformManipulation()
                      {
                          Target = this.screwObject.ID,
                          //Compute the new global position of the object
                           Position = startPosition.Add(startRotation.Multiply(new MVector3(1, 0, 0)).Multiply(percentage * this.screwLength)),
                           //Compute the new global rotation of the object
                           Rotation = screwObject.Transform.Rotation
                        }
                 }
        }) ;


        // Dummy Behavior
        dummyCounter += time;
        if (dummyCounter >= dummyDuration)
        {
            MMICSharp.Logger.LogDebug("ScrewManual: Simulation Ended");
            result.Events.Add(new MSimulationEvent("DummySimulation Ended", mmiConstants.MSimulationEvent_End, this.instruction.ID));
            dummyCounter = 0;
        }
        return result;
    }


    /// <summary>
    /// Method to return the boundary constraints.
    /// Method can be optionally implemented by the developers.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public override List<MConstraint> GetBoundaryConstraints(MInstruction instruction)
    {
        MGeometryConstraint tooltip = null;
        MGeometryConstraint screwHeadConstraint = null;
        MGeometryConstraint screwShaftConstraint = null;
        MConstraint toolPositionConstraint = null;

        MSceneObject screwObject; MSceneObject screwdriverObject; string handed; MConstraint refC;
        MBoolResponse resp = ParseInstruction(instruction, out handed, out screwObject, out screwdriverObject, out refC);

        if (screwdriverObject.Constraints != null)
        {
            foreach (MConstraint c in screwdriverObject.Constraints)
            {
                // TODO retrieve screw tip, provide constraint   
                if (c.ID == "Tooltip" && c.GeometryConstraint != null)
                {
                    tooltip = c.GeometryConstraint;
                    break;
                }
            }
        }
        if (screwObject.Constraints != null)
        {
            foreach (MConstraint c in screwObject.Constraints)
            {
                if (c.ID == "Screw head" && c.GeometryConstraint != null)
                {
                    screwHeadConstraint = c.GeometryConstraint;
                }
                if (c.ID.Contains("shaft") && c.GeometryConstraint != null)
                {
                    screwShaftConstraint = c.GeometryConstraint;
                }

            }
        }
        if (screwHeadConstraint == null)
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, $"No screw constraints found. Taking target position directly");
            screwHeadConstraint = new MGeometryConstraint("") { ParentToConstraint = screwObject.Transform };
        }
        MQuaternion goalRot = screwObject.Transform.Rotation;
        if (screwShaftConstraint != null) goalRot = goalRot.Multiply(screwShaftConstraint.ParentToConstraint.Rotation);
        MVector3 goalPos = screwObject.Transform.Position;
        if (screwHeadConstraint != null) goalPos = goalPos.Add(screwObject.Transform.Rotation.Multiply(screwHeadConstraint.ParentToConstraint.Position));
        // Default Tool position constraint
        toolPositionConstraint = new MConstraint("toolPosition")
        {
            GeometryConstraint = new MGeometryConstraint("")
            {
                ParentToConstraint = new MTransform("", goalPos, goalRot, MVector3Extensions.One())
            }
        };

        if (tooltip != null)
        {
            // If tool has tooltip, place tooltip at goal constraint. 
            var newGoalRot = goalRot.Multiply(MQuaternionExtensions.Inverse(tooltip.ParentToConstraint.Rotation));
            var newGoalPos = goalPos.Subtract(newGoalRot.Multiply(tooltip.ParentToConstraint.Position));
            toolPositionConstraint.GeometryConstraint.ParentToConstraint.Position = newGoalPos;
            toolPositionConstraint.GeometryConstraint.ParentToConstraint.Rotation = newGoalRot;
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, $"No tooltip constraints found. Taking target position directly");
        }
        return new List<MConstraint>() { toolPositionConstraint };
    }

    /// <summary>
    /// Method checks if the prerequisites for starting the instruction are fulfilled.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public override MBoolResponse CheckPrerequisites(MInstruction instruction)
    {
        return new MBoolResponse(true);
    }
    private MAvatarPostureValues IKBlendToTarget(MAvatarPostureValues current, MVector3 goalPos, MQuaternion goalRot, MJointType hand, float weight)
    {
        MConstraint reachConstraint = new MConstraint(System.Guid.NewGuid().ToString())
        {
            JointConstraint = new MJointConstraint(hand)
            {
                GeometryConstraint = new MGeometryConstraint("")
                {
                    ParentObjectID = "",
                    ParentToConstraint = new MTransform(System.Guid.NewGuid().ToString(), goalPos, goalRot, new MVector3(1, 1, 1)),
                    WeightingFactor = weight
                }
            }
        };
        //Debug.Log("Compute IK");
        string nameBack = current.AvatarID + "";
        current.AvatarID = IKName;
        MIKServiceResult result = this.ServiceAccess.IKService.CalculateIKPosture(current, new List<MConstraint>() { reachConstraint }, new Dictionary<string, string>() { });
        result.Posture.AvatarID = nameBack;

        return result.Posture;
    }
}
