

using MMICSharp.Common;
using MMICSharp.Common.Attributes;
using MMIStandard;
using MMIUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarryObject : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();

    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "CarryObject";

        //Assign the motion type of the MMU
        this.MotionType = "Object/Carry";

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
        var IKDesc = avatarDescription.Clone();
        IKDesc.AvatarID = IKName;
        this.ServiceAccess.IKService.Setup(IKDesc, new Dictionary<string, string>());


        return new MBoolResponse(true);
    }

    public string defaultHandedness = "right";
    public string defaultCarryPosition = "side";
    public MGeometryConstraint defaultOrientationConstraint = new MGeometryConstraint("") { ParentToConstraint = new MTransform("", new MVector3(0.0, 1.25, 0.25), new MQuaternion(0, 0, 0, 1), new MVector3(1, 1, 1)), RotationConstraint = MRotationConstraintExtensions.EasyRotationConstraint(0, 0, -1, 1, 0, 0) };


    public string handedness;
    public string position;
    public MVector3 carryPosition;
    public MGeometryConstraint ObjectOrientationConstraint;
    public MSceneObject targetObject;
    public MInstruction instruction;

    public MVector3 DefaultCarryFront = new MVector3(0, 1.1, 0.45);
    public MVector3 DefaultCarrySide = new MVector3(0.270287990570068, 0.936306536197662, 0.108768180012703); //new MVector3(0.25, 1.2, 0.1);
    public MQuaternion DefaultCarryRotSide = new MQuaternion(-0.0469124093651772, -0.0962091833353043, -0.989148914813995, 0.100636035203934);
    public MQuaternion DefaultCarryRotFront = new MQuaternion();

    // Constraints for grasping points on the target object
    public List<MConstraint> targetObjectGraspConstraint = new List<MConstraint>();


    private MBoolResponse ParseInstruction(MInstruction instruction, out MGeometryConstraint ObjectOrientationConstraint, out string handedness, out string position, out MVector3 carryPosition, out MSceneObject targetObject)
    {
        MBoolResponse resp = new MBoolResponse(true);
        targetObject = null;
        handedness = defaultHandedness;
        position = defaultCarryPosition;
        ObjectOrientationConstraint = null;
        carryPosition = DefaultCarrySide;

        MSceneObject goalObject;
        if (instruction.Properties.ContainsKey("Object"))
        {
            string objectID = instruction.Properties["Object"];
            goalObject = this.SceneAccess.GetSceneObjectByID(objectID);
            if (goalObject == null)
            {
                goalObject = this.SceneAccess.GetSceneObjectByName(objectID);
                if (goalObject == null)
                {
                    resp.Successful = false;
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"Goal Object with ID {objectID} not found");
                    resp.LogData.Add($"Goal Object with ID {objectID} not found");
                    return resp;
                }
            }
        }
        else
        {
            resp.Successful = false;
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "No Goal Object ID provided");
            resp.LogData.Add("No Goal Object ID provided");
            return resp;
        }
        targetObject = goalObject;


        if (instruction.Properties.ContainsKey("Handedness"))
        {
            handedness = instruction.Properties["Handedness"];
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "No Handedness provided, using default handedness.");
        }

        if (instruction.Properties.ContainsKey("CarryPosition"))
        {
            position = instruction.Properties["CarryPosition"];
            if(position == "side")
            {
                carryPosition = DefaultCarrySide;
            } else if(position == "front")
            {
                carryPosition = DefaultCarryFront;
            } else
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, $"Unknown position {position}, using default position.");
            }
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "No position provided, using default position.");
        }

        targetObjectGraspConstraint.Clear();
        bool found = false;
        // check for prescribed grasping constraint. 
        if (instruction.Properties.ContainsKey("GraspingPoint"))
        {
            foreach (MConstraint c in instruction.Constraints)
            {
                if (c.ID == instruction.Properties["GraspingPoint"])
                {
                    targetObjectGraspConstraint.Add(c);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                foreach (var c in targetObject.Constraints)
                {
                    if (c.ID == instruction.Properties["GraspingPoint"])
                    {
                        targetObjectGraspConstraint.Add(c);
                        found = true;
                        break;
                    }
                }
            }
        }
        // if still not found, revert to default. 
        if (!found && targetObject.Constraints.Count > 0)
        {
            foreach (var c in targetObject.Constraints)
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


        if (instruction.Properties.ContainsKey("OrientationConstraint"))
        {
            string cId = instruction.Properties["OrientationConstraint"];
            MConstraint tC;
            if (!instruction.Constraints.TryGetConstraint(cId, out tC))
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"No OrientationConstraint with ID {cId} provided");
                resp.Successful = false;
                resp.LogData.Add($"No OrientationConstraint with ID {cId} provided");
                return resp;
            }
            if (tC.GeometryConstraint != null)
            {
                ObjectOrientationConstraint = tC.GeometryConstraint;
            }
            else
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"No MGeometryConstraint defined within Constraint with ID {cId}");
                resp.Successful = false;
                resp.LogData.Add($"No MGeometryConstraint defined within Constraint with ID {cId}");
                return resp;
            }
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "No OrientationConstraint provided, using default TargetConstraint.");
            //ObjectOrientationConstraint.ParentObjectID = instruction.AvatarID;
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
    [MParameterAttribute("Object", "string", "Identifier of the object to be retrieved", true)]
    [MParameterAttribute("OrientationConstraint", "string", "Identifier of the orientation constraint definining the orientation of the object during carry (e.g. a box of screws)", false)]
    [MParameterAttribute("Handedness", "string", "Handedness of retrieval: {left, right, both}. Default right", false)]
    [MParameterAttribute("CarryPosition", "string", "Position of the carry: {side, front}. Default side", false)]


    public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState state)
    {
        //To do -> insert your assignment code in here
        //To do -> insert your assignment code in here
        MBoolResponse resp = new MBoolResponse(true);

        MTransform rootT = MTransformExtensions.Identity();
        //Execute instructions on main thread
        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            this.SkeletonAccess.SetChannelData(state.Current);
            this.AssignPostureValues(state.Current);

            rootT = new MTransform("", this.SkeletonAccess.GetRootPosition(state.Current.AvatarID), this.SkeletonAccess.GetRootRotation(state.Current.AvatarID), new MVector3(1, 1, 1));
        });



        MGeometryConstraint ObjectOrientationConstraint;
        string handedness;
        string position;
        MSceneObject targetObject;
        MVector3 carryPos;
        resp = ParseInstruction(instruction, out ObjectOrientationConstraint, out handedness, out position, out carryPos, out targetObject);
        if (!resp.Successful) return resp;

        // make global
        if (ObjectOrientationConstraint != null && ObjectOrientationConstraint.ParentObjectID == state.Current.AvatarID)
        {
            ObjectOrientationConstraint.ParentToConstraint = ObjectOrientationConstraint.ParentToConstraint.Multiply(rootT);
        }

        this.instruction = instruction;
        this.ObjectOrientationConstraint = ObjectOrientationConstraint;
        this.handedness = handedness;
        this.targetObject = targetObject;
        this.carryPosition = carryPos;

        /*
        this.instructions.AddOrUpdate(instruction);
        this.targetConstraints.AddOrUpdate(instruction.ID, goalConstraint);
        this.handedness.AddOrUpdate(instruction.ID, handedness);
        this.targetObjects.AddOrUpdate(instruction.ID, goalObject);
        */
        return resp;
    }
    private string IKName;

    private void ObjectToWrist(MVector3 objPos, MQuaternion objRot, out MVector3 wristPos, out MQuaternion wristRot)
    {
        if (this.targetObjectGraspConstraint.Count > 0)
        {
            string key = "GraspingPointR";
            if (this.handedness == "left")
                key = "GraspingPointL";
            foreach (MConstraint c in targetObjectGraspConstraint)
            {
                if (c.ID == key || targetObjectGraspConstraint.Count == 1)
                {
                    // take the respectiv grasping point or, in case there is only one, the one provided. 
                    var jc = c.PostureConstraint.JointConstraints[0];
                    objPos = objPos.Add(objRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                    objRot = (objRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Rotation));
                }
            }
        }
        wristPos = objPos.Clone();
        wristRot = objRot.Clone();
    }

    private void WristToObject(MVector3 wristPos, MQuaternion wristRot, out MVector3 objPos, out MQuaternion objRot)
    {
        string key = "GraspingPointR";
        if (this.handedness == "left")
            key = "GraspingPointL";
        foreach (MConstraint c in targetObjectGraspConstraint)
        {
            if (c.ID == key || targetObjectGraspConstraint.Count == 1)
            {
                var jc = c.PostureConstraint.JointConstraints[0];
                wristRot = wristRot.Multiply(MQuaternionExtensions.Inverse(jc.GeometryConstraint.ParentToConstraint.Rotation));
                wristPos = wristPos.Subtract(wristRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                break;
            }
        }
        objPos = wristPos;
        objRot = wristRot;
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
        string nameBack = current.AvatarID + "";
        current.AvatarID = IKName;
        MIKServiceResult result = this.ServiceAccess.IKService.CalculateIKPosture(current, new List<MConstraint>() { reachConstraint }, new Dictionary<string, string>() { });
        result.Posture.AvatarID = nameBack;
        current.AvatarID = nameBack;

        return result.Posture;
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
        MTransform rootT = MTransformExtensions.Identity();

        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            this.SkeletonAccess.SetChannelData(state.Current);
            this.AssignPostureValues(state.Current);
            rootT = new MTransform("", this.SkeletonAccess.GetRootPosition(state.Current.AvatarID), this.SkeletonAccess.GetRootRotation(state.Current.AvatarID), new MVector3(1, 1, 1));

        });

        //To do -> insert your do step code in here

        // This is a dummy implementation: 
        // result.Events.Add(new MSimulationEvent("DummySimulation Ended", mmiConstants.MSimulationEvent_End, this.instruction.ID));
        // Request target object to be moved to target position
        string attachementPoint = $"{state.Initial.AvatarID}:RightWrist";

        // apply to root transform
        MVector3 carryPos = rootT.Rotation.Multiply(carryPosition).Add(rootT.Position);
        MQuaternion carryRot;
        if(ObjectOrientationConstraint != null)
        {
            this.ObjectToWrist(carryPos, ObjectOrientationConstraint.ParentToConstraint.Rotation, out carryPos, out carryRot);
        } else
        {
            // Todo: recalculate wrist rotation. 
            carryRot = rootT.Rotation.Multiply(DefaultCarryRotSide);
        }

        MJointType hand = MJointType.RightWrist;
        if(handedness == "left")
        {
            hand = MJointType.LeftWrist;
        }
        result.Posture = IKBlendToTarget(result.Posture, carryPos, carryRot, hand, 1.0f);
        this.SkeletonAccess.SetChannelData(result.Posture);


        var handPos = this.GetSkeletonAccess().GetGlobalJointPosition(this.AvatarID, hand);
        var handRot = this.GetSkeletonAccess().GetGlobalJointRotation(this.AvatarID, hand);
        MVector3 objectPos;
        MQuaternion objectRot;
        //TODO recalculate object pos: 
        this.WristToObject(handPos, handRot, out objectPos, out objectRot);

        result.SceneManipulations.Add(new MSceneManipulation()
        {
            Transforms = new List<MTransformManipulation>()
                 {
                      new MTransformManipulation()
                      {
                          Target = this.targetObject.ID,
                          //Compute the new global position of the object
                           Position = objectPos,
                           //Compute the new global rotation of the object
                           Rotation = objectRot
                        }
                 },
            Attachments = new List<MAttachmentManipulation>()
            {
                // Not working!
                 // TODO: Implement possibility to attach objects to the ego-avatar (ego-avatar is not a scene object right now). 
                new MAttachmentManipulation(attachementPoint, this.targetObject.ID, true)
            }
        });

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
        MGeometryConstraint ObjectOrientationConstraint;
        string handedness;
        string position;
        MSceneObject targetObject;
        MVector3 carryPos;
        MBoolResponse resp = ParseInstruction(instruction, out ObjectOrientationConstraint, out handedness, out position, out carryPos, out targetObject);

        MTransform rootT = MTransformExtensions.Identity();

        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            rootT = new MTransform("", this.SkeletonAccess.GetRootPosition(instruction.AvatarID), this.SkeletonAccess.GetRootRotation(instruction.AvatarID), new MVector3(1, 1, 1));
        });

        MVector3 globalPos = rootT.Rotation.Multiply(carryPos).Add(rootT.Position);
        MQuaternion carryRot;
        if (ObjectOrientationConstraint != null)
        {
            this.ObjectToWrist(carryPos, ObjectOrientationConstraint.ParentToConstraint.Rotation, out carryPos, out carryRot);
        }
        else
        {
            // Todo: recalculate wrist rotation. 
            carryRot = rootT.Rotation.Multiply(DefaultCarryRotSide);
        }


        MConstraint carConst = new MConstraint("CarryConstraint")
        {
            GeometryConstraint = new MGeometryConstraint("")
            {
                ParentToConstraint = new MTransform("", globalPos, carryRot, MVector3Extensions.One())
            }
        };

        return new List<MConstraint>() { carConst };
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

}
