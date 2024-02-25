using MMICSharp.Common.Attributes;
using MMIStandard;
using MMIUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandScrew : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();

    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "HandScrew";

        //Assign the motion type of the MMU
        this.MotionType = "Object/Screw/Hand";

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

        return new MBoolResponse(true);
    }

    public string defaultHandedness = "right";
    public string handedness;
    public MSceneObject targetObject;
    public MInstruction instruction;
    public MGeometryConstraint PositionConstraint;

    private MBoolResponse ParseInstruction(MInstruction instruction, out string handedness, out MSceneObject targetObject, out MGeometryConstraint PositionConstraint)
    {
        MBoolResponse resp = new MBoolResponse(true);
        handedness = defaultHandedness;
        targetObject = null;
        PositionConstraint = null;


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

        if(instruction.Properties.ContainsKey("PositionConstraint"))
        {
            foreach(var c in instruction.Constraints)
            {
                if(c.ID == instruction.Properties["PositionConstraint"] && c.GeometryConstraint != null)
                {
                    PositionConstraint = c.GeometryConstraint;
                    PositionConstraint = PositionConstraint.MakeGlobalConstraint(this.SceneAccess);
                    break;
                }
            }

            if(PositionConstraint == null)
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, $"HandScrew: Could not find fitting Geometry Constraint with ID {instruction.Properties["PositionConstraint"]}");
            }
        } else {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, $"HandScrew: Could not find property for PositionConstraint");
        }

        return resp;
    }

    private double dummyCounter = 0; 

    /// <summary>
    /// Method to assign a MInstriction to the MMU:
    /// The method must be provided by the MMU developer.
    /// </summary>
    /// <param name="motionCommand"></param>
    /// <param name="avatarState"></param>
    /// <returns></returns>
    [MParameterAttribute("Object", "string", "Identifier of the object to be retrieved", true)]
    [MParameterAttribute("Handedness", "string", "Handedness of retrieval: {left, right, both}. Default right", false)]
    [MParameterAttribute("PositionConstraint", "string", "Identifier of the position constraint of the screw for constraint chaining", false)]

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

        MSceneObject goalObject; string handed; MGeometryConstraint positionConstraint;
        resp = ParseInstruction(instruction, out handed, out goalObject, out positionConstraint);
        if (!resp.Successful) { return resp; }

        this.instruction = instruction;
        this.handedness = handed;
        this.targetObject = goalObject;
        this.PositionConstraint = positionConstraint;

        dummyCounter = 0;

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


        //To do -> insert your do step code in here

        // no dummy implementation required, as there is no visual effect
        dummyCounter+=time;
        if(dummyCounter >= 3)
        {
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

        MGeometryConstraint screwHeadConstraint = null;
        MConstraint screwShaftConstraint = null;
        MConstraint screwPositionConstraint = null;

        MSceneObject screwObject; string handed; MGeometryConstraint positionConstraint;
        MBoolResponse resp = ParseInstruction(instruction, out handed, out screwObject, out positionConstraint);

        MVector3 relativePosition = MVector3Extensions.Zero(); MQuaternion relativeRotation = MQuaternionExtensions.Identity();

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
                    screwShaftConstraint = c;
                }

            }
        }
        if (screwHeadConstraint != null && screwShaftConstraint != null)
        { 
            string s_screwLength;
            if (!screwShaftConstraint.Properties.TryGetValue("Length", out s_screwLength))
            {
                s_screwLength = "0";
            }
            float screwLength = float.Parse(s_screwLength) / 1000.0f;
            relativeRotation = screwShaftConstraint.GeometryConstraint.ParentToConstraint.Rotation;
            relativePosition = screwHeadConstraint.ParentToConstraint.Position.Add(relativeRotation.Multiply(new MVector3(screwLength, 0, 0)));
        } else
        {             
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, $"No screw constraints found. Taking target position directly");
    }


        MConstraint screwRefConstraint = new MConstraint(System.Guid.NewGuid().ToString())
        {
            GeometryConstraint = new MGeometryConstraint("")
            {
                ParentToConstraint = new MTransform("", relativePosition, relativeRotation, MVector3Extensions.One())
            }
        };
        /*
        MQuaternion goalRot; MVector3 goalPos;
        if (positionConstraint == null)
        {
            goalRot = screwObject.Transform.Rotation;
            goalPos = screwObject.Transform.Position;
        }
        else
        {
            goalRot = positionConstraint.ParentToConstraint.Rotation;
            if (screwShaftConstraint != null) goalRot = goalRot.Multiply(screwShaftConstraint.GeometryConstraint.ParentToConstraint.Rotation);
            goalPos = positionConstraint.ParentToConstraint.Position;
            if (screwHeadConstraint != null) goalPos = goalPos.Add(screwObject.Transform.Rotation.Multiply(screwHeadConstraint.ParentToConstraint.Position));
        }*/

        return new List<MConstraint>() { screwRefConstraint };
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
