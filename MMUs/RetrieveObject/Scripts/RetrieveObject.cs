
using MMICSharp.Common;
using MMICSharp.Common.Attributes;
using MMIStandard;
using MMIUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RetrieveObject : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();
    public float RetrieveRange = 1.2f;
    private bool objectGrabbed = false;
    private bool fingersStarted = false;

    private const string BLENDHEIGHT_PARAMETER = "BlendHeight";
    private const string BLENDMIDDLE_PARAMETER = "BlendToMiddle";

    private Animator anim;

    private float transition_start_time = 0.2f;
    private float reach_time = 0.5f;
    private float grasp_time = 0.35f;
    private float detransition_end_time = 0.75f;
    private float goal_end_time = 1.0f;

    private string IKName;

    private MVector3 goalPos;
    private MQuaternion goalRot;



    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "RetrieveObject";

        //Assign the motion type of the MMU
        this.MotionType = "Object/Retrieve";

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

        this.anim = this.GetComponent<Animator>();

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
            updateAnimator(0.01);
        });

        IKName = System.Guid.NewGuid().ToString();
        var IKDesc = avatarDescription.Clone();
        IKDesc.AvatarID = IKName;
        this.ServiceAccess.IKService.Setup(IKDesc, new Dictionary<string, string>());

        return new MBoolResponse(true);
    }


    public MGeometryConstraint defaultCarryConstraint = new MGeometryConstraint("") { ParentToConstraint = new MTransform("", new MVector3(0.0, 1.00, 0.45), new MQuaternion(0, 0, 0, 1), new MVector3(1, 1, 1)) };
    public string defaultHandedness = "right";
    /*
    public Dictionary<string, MGeometryConstraint> targetConstraints = new Dictionary<string, MGeometryConstraint>();
    public Dictionary<string, string> handedness = new Dictionary<string, string>();
    public Dictionary<string, MSceneObject> targetObjects = new Dictionary<string, MSceneObject>();
    public List<MInstruction> instructions = new List<MInstruction>();
    */
    public MGeometryConstraint targetConstraint;
    public string handedness;
    public MSceneObject targetObject;
    // Constraints for grasping points on the target object
    public List<MConstraint> targetObjectGraspConstraint = new List<MConstraint>();
    public MInstruction instruction;

    private MBoolResponse ParseInstruction(MInstruction instruction, out MGeometryConstraint targetConstraint, out string handedness, out MSceneObject targetObject)
    {
        MBoolResponse resp = new MBoolResponse(true);
        handedness = defaultHandedness;
        targetConstraint = null;
        targetObject = null;


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

        targetObjectGraspConstraint.Clear();
        bool found = false;
        // check for prescribed grasping constraint. 
        if (instruction.Properties.ContainsKey("GraspingPoint"))
        {
            foreach(MConstraint c in instruction.Constraints)
            {
                if(c.ID == instruction.Properties["GraspingPoint"])
                {
                    targetObjectGraspConstraint.Add(c);
                    found = true;
                    break;
                }
            }
            if(!found)
            {
                foreach(var c in targetObject.Constraints)
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
        if(!found && targetObject.Constraints.Count > 0)
        {
            foreach(var c in targetObject.Constraints)
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


        MGeometryConstraint goalConstraint = defaultCarryConstraint.Clone();
        if (instruction.Properties.ContainsKey("TargetConstraint"))
        {
            string cId = instruction.Properties["TargetConstraint"];
            MConstraint tC;
            if (!instruction.Constraints.TryGetConstraint(cId, out tC))
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"No TargetConstraint with ID {cId} provided");
                resp.Successful = false;
                resp.LogData.Add($"No TargetConstraint with ID {cId} provided");
                return resp;
            }
            if (tC.GeometryConstraint != null)
            {
                goalConstraint = tC.GeometryConstraint;
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
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "No TargetConstraint provided.");
        }

        if(goalConstraint != null)
        {
            // make global
            var rootPos = this.SkeletonAccess.GetRootPosition(this.AvatarID);
            var rootRot = this.SkeletonAccess.GetRootRotation(this.AvatarID);

            goalConstraint.ParentToConstraint.Position = rootPos.Add(rootRot.Multiply(goalConstraint.ParentToConstraint.Position));
            goalConstraint.ParentToConstraint.Rotation = rootRot.Multiply(goalConstraint.ParentToConstraint.Rotation);
        }
        targetConstraint = goalConstraint;
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
    [MParameterAttribute("TargetConstraint", "string", "Identifier of the target constraint to be reached after retrieval, defaults to front carry with one hand", false)]
    [MParameterAttribute("Handedness", "string", "Handedness of retrieval: {left, right, both}. Default right", false)]
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
            this.AssignPostureValues(state.Current);

            rootT = new MTransform("", this.SkeletonAccess.GetRootPosition(state.Current.AvatarID), this.SkeletonAccess.GetRootRotation(state.Current.AvatarID), new MVector3(1, 1, 1));
        });

        
        
        MGeometryConstraint goalConstraint; MSceneObject goalObject; string handed;
        resp = ParseInstruction(instruction, out goalConstraint, out handed, out goalObject);
        if (!resp.Successful) return resp;

        // make global
        if (goalConstraint != null && goalConstraint.ParentObjectID == state.Current.AvatarID)
        {
            goalConstraint.ParentToConstraint = goalConstraint.ParentToConstraint.Multiply(rootT);
        }

        this.instruction = instruction;
        this.targetConstraint = goalConstraint;
        this.handedness = handed;
        this.targetObject = goalObject;

        this.objectGrabbed = false;
        this.fingersStarted = false;
        this.ExecuteOnMainThread(() =>
        {
            if (this.handedness == "right")
                this.anim.SetTrigger("ReachRight");
            else if (this.handedness == "left")
                this.anim.SetTrigger("ReachLeft");

            updateAnimator(0.03f);
        });
        /*
        this.instructions.AddOrUpdate(instruction);
        this.targetConstraints.AddOrUpdate(instruction.ID, goalConstraint);
        this.handedness.AddOrUpdate(instruction.ID, handedness);
        this.targetObjects.AddOrUpdate(instruction.ID, goalObject);
        */
        return resp;
    }

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
        MIKServiceResult result = this.ServiceAccess.IKService.CalculateIKPosture(current, new List<MConstraint>() { reachConstraint }, new Dictionary<string, string>() {});
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
            Constraints = new List<MConstraint>(),
            Events = new List<MSimulationEvent>(),
            //Constraints = state.Constraints!=null ? state.Constraints: new List<MConstraint>(),
            //Events = state.Events !=null? state.Events: new List<MSimulationEvent>(),
            SceneManipulations = state.SceneManipulations!=null ? state.SceneManipulations: new List<MSceneManipulation>(),
        };

        if(state.Constraints != null)
            result.Constraints.AddRange(state.Constraints);

        if (state.Events != null)
            result.Events.AddRange(state.Events);



        this.ExecuteOnMainThread(() =>
        {
            //Set the channel data of the current simulation state
            this.SkeletonAccess.SetChannelData(state.Current);
            this.AssignPostureValues(state.Current);
        });

        var rootPos = this.SkeletonAccess.GetRootPosition(this.AvatarID);
        var rootRot = this.SkeletonAccess.GetRootRotation(this.AvatarID);
        var hand = MJointType.RightWrist;
        if (handedness == "left")
            hand = MJointType.LeftWrist;


        if (!objectGrabbed)
        {

            goalPos = this.targetObject.Transform.Position;
            goalRot = this.targetObject.Transform.Rotation;

            ObjectToWrist(this.targetObject.Transform.Position, this.targetObject.Transform.Rotation, out goalPos, out goalRot);
            /*
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
                        goalPos = goalPos.Add(goalRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                        goalRot = (goalRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Rotation));
                    }
                }
            }*/

            MVector3 goalPosLocal = MQuaternionExtensions.Inverse(rootRot).Multiply(goalPos.Subtract(rootPos));


            this.ExecuteOnMainThread(() =>
            {
                float blendHeightWeight = (float)goalPosLocal.Y;
                float blendToMiddleWeight = -(float)goalPosLocal.X;
                //Debug.Log($"Blend Tree Weights {blendToMiddleWeight} x {blendHeightWeight} : {(float)time}");
                anim.SetFloat(BLENDHEIGHT_PARAMETER, blendHeightWeight);
                anim.SetFloat(BLENDMIDDLE_PARAMETER, blendToMiddleWeight);
            });
        }
        float animTime = 0.0f;
        this.ExecuteOnMainThread(() =>
        {
            updateAnimator(time);
            result.Posture = Merge(result.Posture, this.GetRetargetedPosture());            

            if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Reach"))
            {
                animTime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }
            else {
                MMICSharp.Logger.LogError($"(Retrieve {handedness}) not in Reach anymore");
                if(objectGrabbed)
                    animTime = 1.0f;
            }
        });


        //Debug.Log($"AnimTime: {animTime}");

        // Perform IK pass to reach the goal
        if (!objectGrabbed)
        {
            float weight = 0.0f;

            ///During the reach animation, the ik will slowly be increased to actually reach to the target
            ///~0.48f is the normalizeTime where the target is reached
            ///TODO scale this over the distance to the objects original position
            ///TODO scale down Push, Reach, Pull of the other arm that is not used => fixes the stiffness
            if (animTime > transition_start_time && animTime <= reach_time)
            {

                ///Set the position weight depending on the normalized value of the normalized animation time
                ///~0.12f is where the clip starts and ~0.48f is where the target will be reached
                weight = (animTime - transition_start_time) / (reach_time - transition_start_time);
                ///Do the same for the lookAtIK
                //this.lookAtIK.solver.target = reachTarget.transform;
                //this.lookAtIK.solver.SetIKPositionWeight(((animator.GetCurrentAnimatorStateInfo(0).normalizedTime - 0f) / (0.48f - 0.12f)));
            }
            //weight = 0.0f;
            if (weight > 0.0f)
            {
                result.Posture = this.IKBlendToTarget(result.Posture, goalPos, goalRot, hand, weight);
                this.SkeletonAccess.SetChannelData(result.Posture);
            }

        }

        var handPos = this.GetSkeletonAccess().GetGlobalJointPosition(this.AvatarID, hand);
        
        // Trigger event to open the fingers. 
        if(!fingersStarted && animTime >= grasp_time)
        {
            fingersStarted = true;
            MMICSharp.Logger.LogDebug("start finger animation");
            result.Events.Add(new MSimulationEvent("AnimateFingers", "AnimateFingers", this.instruction.ID));
        }

        if (!objectGrabbed && animTime >= reach_time)
        {
            objectGrabbed = true;
            MMICSharp.Logger.LogDebug("Object reached");
            result.Events.Add(new MSimulationEvent("ObjectReached", mmiConstants.MSimulationEvent_CycleEnd, this.instruction.ID));
        }

        if(objectGrabbed) {
            float weight = 0.0f;
            //string key = "GraspingPointR";
            //if (this.handedness == "left")
            //    key = "GraspingPointL";



            ///After the target is reached the ik will slowly be reduced
            if (animTime > reach_time && animTime < detransition_end_time)
            {
                ///Set the IK weight of the hand inversely proportional to the animation clip time
                weight = 1 - (animTime - reach_time) / (detransition_end_time - reach_time);

                if (weight > 0.0f)
                {
                    result.Posture = this.IKBlendToTarget(result.Posture, goalPos, goalRot, hand, weight);
                    this.SkeletonAccess.SetChannelData(result.Posture);
                }
            } else if(animTime >= detransition_end_time && this.targetConstraint != null )
            {
                var gp = this.targetConstraint.ParentToConstraint.Position.Clone();
                var gr = this.targetConstraint.ParentToConstraint.Rotation.Clone();

                /*
                foreach (MConstraint c in targetObjectGraspConstraint)
                {
                    if (c.ID == key)
                    {
                        var jc = c.PostureConstraint.JointConstraints[0];
                        gp = gp.Add(gr.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                        gr = (gr.Multiply(jc.GeometryConstraint.ParentToConstraint.Rotation));
                        break;
                    }
                }*/

                weight = Mathf.Min(1f, (animTime - detransition_end_time) / (goal_end_time - reach_time));
                result.Posture = this.IKBlendToTarget(result.Posture, gp, gr, hand, weight);
                this.SkeletonAccess.SetChannelData(result.Posture);
            }

            // recalculate object position
            handPos = this.GetSkeletonAccess().GetGlobalJointPosition(this.AvatarID, hand);
            var handRot = this.GetSkeletonAccess().GetGlobalJointRotation(this.AvatarID, hand);
            MVector3 objPos; MQuaternion objRot;
            this.WristToObject(handPos, handRot, out objPos, out objRot);
            /*
            foreach (MConstraint c in targetObjectGraspConstraint)
            {
                if (c.ID == key || targetObjectGraspConstraint.Count == 1)
                {
                    var jc = c.PostureConstraint.JointConstraints[0];
                    handRot = handRot.Multiply(MQuaternionExtensions.Inverse(jc.GeometryConstraint.ParentToConstraint.Rotation));
                    handPos = handPos.Subtract(handRot.Multiply(jc.GeometryConstraint.ParentToConstraint.Position));
                    break; 
                }
            }*/

            string attachementPoint = $"{state.Initial.AvatarID}:RightWrist";
            result.SceneManipulations.Add(new MSceneManipulation()
            {
                Transforms = new List<MTransformManipulation>()
                 {
                      new MTransformManipulation()
                      {
                          Target = this.targetObject.ID,
                          //Compute the new global position of the object
                           Position = objPos,
                           //Compute the new global rotation of the object
                           Rotation = objRot
                        }
                 },
                /*
                Attachments = new List<MAttachmentManipulation>()
                {
                    // Not working!
                     // TODO: Implement possibility to attach objects to the ego-avatar (ego-avatar is not a scene object right now). 
                    new MAttachmentManipulation(attachementPoint, this.targetObject.ID, true)
                }*/
            });
        }

        if (animTime >= 1.0) //MVector3Extensions.Distance(handPos, targetConstraint.ParentToConstraint.Position) < 0.11
        {
            MMICSharp.Logger.LogDebug($"(Retrieve {handedness}: End Animation");
            // This is a dummy implementation: 
            result.Events.Add(new MSimulationEvent($"Retrieve {handedness} Simulation Ended", mmiConstants.MSimulationEvent_End, this.instruction.ID));
            // Request target object to be moved to target position
            // clear animation cache
            this.ExecuteOnMainThread(() =>
            {
                updateAnimator(2);
            });
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
        return new List<MConstraint>();
    }

    /// <summary>
    /// Method checks if the prerequisites for starting the instruction are fulfilled.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public override MBoolResponse CheckPrerequisites(MInstruction instruction)
    {
        var rootPos = this.SkeletonAccess.GetRootPosition(instruction.AvatarID);
        MGeometryConstraint goalConstraint; MSceneObject goalObject; string handed;
        var resp = ParseInstruction(instruction, out goalConstraint, out handed, out goalObject);

        if (rootPos != null) 
        { 
            if(resp.Successful == true)
            {
                if(goalObject.Transform.Position.Subtract(rootPos).Magnitude() > RetrieveRange)
                {
                    MMICSharp.Logger.LogError("Reason: out of reach");
                    return new MBoolResponse(true);
                    //return new MBoolResponse(false) { LogData = new List<string>() { "Reason: out of reach" } };
                } else
                {
                    return new MBoolResponse(true);
                }
            } else
            {
                return resp;
            }
        } else
        {
            return new MBoolResponse(false) { LogData = new List<string>() { $"Message: no root position found for avatar {instruction.AvatarID}" } };
        }
        
    }

    private void updateAnimator(double deltaTime)
    {
        anim.speed = 1.0f;
        anim.Update((float)deltaTime);
        anim.speed = 0.0f;
    }

    private MAvatarPostureValues Merge(MAvatarPostureValues a, MAvatarPostureValues b)
    {
        if (this.handedness == "right")
        {
            return a.OverwriteWithPartial(b.MakePartial(RightArm));
        }
        else if (this.handedness == "left")
        {
            return a.OverwriteWithPartial(b.MakePartial(LeftArm));
        } else
        {
            return b;
        }
    }

    private List<MJointType> RightArm = new List<MJointType>() { MJointType.RightShoulder, MJointType.RightElbow, MJointType.RightWrist };
    private List<MJointType> LeftArm = new List<MJointType>() { MJointType.LeftShoulder, MJointType.LeftElbow, MJointType.LeftWrist };

}
