

using JetBrains.Annotations;
using MMICSharp.Common;
using MMICSharp.Common.Attributes;
using MMIStandard;
using MMIUnity;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using UnityEngine;

public class PlaceObject : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();
    public float RetrieveRange = 1.2f;
    private bool objectGrabbed = false;

    private const string BLENDHEIGHT_PARAMETER = "BlendHeight";
    private const string BLENDMIDDLE_PARAMETER = "BlendToMiddle";

    private Animator anim;

    private float transition_start_time = 0.2f;
    private float reach_time = 0.5f;
    private float detransition_end_time = 0.75f;
    private float goal_end_time = 1.0f;

    private string IKName;

    private MVector3 goalPos;
    private MQuaternion goalRot;


    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "PlaceObject";

        //Assign the motion type of the MMU
        this.MotionType = "Object/Place";

        //Auto generated source code for bone type mapping:
        this.BoneTypeMapping = new Dictionary<string, MJointType>()
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
        this.Pelvis = this.gameObject.GetComponentsInChildren<Transform>().First(s => s.name == "pelvis");
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

    public string defaultHandedness = "right";
    public MGeometryConstraint targetConstraint;
    public string handedness;
    public MSceneObject targetObject;
    public MInstruction instruction;
    public bool Release;

    private MGeometryConstraint tmp_referenceConstraint;

    public MGeometryConstraint defaultIntermediatePosition = new MGeometryConstraint("") { ParentToConstraint = new MTransform("", new MVector3(0.0, 1.25, 0.25), new MQuaternion(0, 0, 0, 1), new MVector3(1, 1, 1)) };




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
        avatarDescription.AvatarID = IKName;
        this.ServiceAccess.IKService.Setup(avatarDescription, new Dictionary<string, string>());

        return new MBoolResponse(true);
    }

    // Constraints for grasping points on the target object
    public List<MConstraint> targetObjectGraspConstraint = new List<MConstraint>();



    private MBoolResponse ParseInstruction(MInstruction instruction, out MGeometryConstraint targetConstraint, out string handedness, out MSceneObject targetObject, out bool release, out MGeometryConstraint referenceConstraint)
    {
        MBoolResponse resp = new MBoolResponse(true);
        handedness = defaultHandedness;
        targetConstraint = null;
        targetObject = null;
        release = true;
        referenceConstraint = null;


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
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"PlaceObject: Goal Object with ID {objectID} not found");
                    resp.LogData.Add($"Goal Object with ID {objectID} not found");
                    return resp;
                }
            }
        }
        else
        {
            resp.Successful = false;
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "PlaceObject: No Goal Object ID provided");
            resp.LogData.Add("No Goal Object ID provided");
            return resp;
        }
        targetObject = goalObject;

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

        if (instruction.Properties.ContainsKey("Handedness"))
        {
            handedness = instruction.Properties["Handedness"];
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "PlaceObject: No Handedness provided, using default handedness.");
        }

        if (instruction.Properties.ContainsKey("Release"))
        {
            if(!bool.TryParse(instruction.Properties["Release"], out release))
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, $"PlaceObject: Release paremeter provided as {instruction.Properties["Release"]}, but not as a bool (true, false).");
            }
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_DEBUG, "PlaceObject: No release paremeter provided, releasing object by default.");
        }

        MGeometryConstraint goalConstraint = null;
        if (instruction.Properties.ContainsKey("TargetConstraint"))
        {
            string cId = instruction.Properties["TargetConstraint"];
            MConstraint tC;
            if (!instruction.Constraints.TryGetConstraint(cId, out tC))
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"PlaceObject: No TargetConstraint with ID {cId} provided");
                resp.Successful = false;
                resp.LogData.Add($"No TargetConstraint with ID {cId} provided");
                return resp;
            }
            if (tC.GeometryConstraint != null)
            {
                goalConstraint = tC.GeometryConstraint;
                goalConstraint = goalConstraint.MakeGlobalConstraint(this.SceneAccess);
            }
            else
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"PlaceObject: No MGeometryConstraint defined within Constraint with ID {cId}");
                resp.Successful = false;
                resp.LogData.Add($"No MGeometryConstraint defined within Constraint with ID {cId}");
                return resp;
            }
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "PlaceObject: No TargetConstraint provided, using default TargetConstraint.");
            resp.Successful = false;
            resp.LogData.Add($"No TargetConstraint provided.");
            return resp;

        }
        targetConstraint = goalConstraint;

        if (instruction.Properties.ContainsKey("ReferenceConstraint"))
        {
            string cId = instruction.Properties["ReferenceConstraint"];
            MConstraint tC;
            if(!instruction.Constraints.TryGetConstraint(cId, out tC))
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, $"PlaceObject: No Reference Constraint with ID {cId} found in instruction");
                if (!targetObject.Constraints.TryGetConstraint(cId, out tC))
                {
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"PlaceObject: No Reference Constraint with ID {cId} found in object or instruction");
                    resp.Successful = false;
                    resp.LogData.Add($"No Reference Constraint with ID {cId} found in object");
                    return resp;
                }
            }

            if (tC.GeometryConstraint != null)
            {
                referenceConstraint = tC.GeometryConstraint;
                referenceConstraint = referenceConstraint.MakeGlobalConstraint(this.SceneAccess);
            }
            else
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"PlaceObject: No MGeometryConstraint defined within Constraint with ID {cId}");
                resp.Successful = false;
                resp.LogData.Add($"No MGeometryConstraint defined within Constraint with ID {cId}");
                return resp;
            }
        } else 
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "PlaceObject: No ReferenceConstraint provided, using transformation as default.");
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
    [MParameterAttribute("TargetConstraint", "string", "Identifier of the target constraint to be reached after retrieval, defaults to front carry with one hand", false)]
    [MParameterAttribute("Handedness", "string", "Handedness of retrieval: {left, right, both}. Default right", false)]
    [MParameterAttribute("Release", "Boolean", "Whether to release the object after placement or not: {true, false}, default: true)", false)]
    [MParameterAttribute("ReferenceConstraint", "String", "The (optional) constraint property of the object, which needs to be placed on the target constraint", false)]


    public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState state)
    {
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

        MGeometryConstraint goalConstraint; MSceneObject goalObject; string handed; bool release; MGeometryConstraint referenceConstraitn;
        resp = ParseInstruction(instruction, out goalConstraint, out handed, out goalObject, out release, out referenceConstraitn);
        if(!resp.Successful) { return resp; }

        this.tmp_referenceConstraint = referenceConstraitn;
        this.instruction = instruction;
        this.targetConstraint = goalConstraint;
        this.handedness = handed;
        this.targetObject = goalObject;
        this.Release = release;

        if (referenceConstraitn != null)
        {
            var tref = tmp_referenceConstraint.ParentToConstraint;
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"tref Rot: {tref.Rotation}, tref Pos {tref.Position}, target rot {targetConstraint.ParentToConstraint.Rotation}, target pos: {targetConstraint.ParentToConstraint.Position}");
            var rot = targetConstraint.ParentToConstraint.Rotation.Multiply(MQuaternionExtensions.Inverse(tref.Rotation));
            targetConstraint.ParentToConstraint.Rotation = rot;
            targetConstraint.ParentToConstraint.Position = targetConstraint.ParentToConstraint.Position.Subtract(rot.Multiply(tref.Position));
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, $"After. target rot {targetConstraint.ParentToConstraint.Rotation}, target pos: {targetConstraint.ParentToConstraint.Position}");
        }
        this.objectGrabbed = true;
        this.ExecuteOnMainThread(() =>
        {
            if (this.handedness == "right")
                this.anim.SetTrigger("ReachRight");
            else if (this.handedness == "left")
                this.anim.SetTrigger("ReachLeft");

            updateAnimator(0.03f);
        });


        return resp;

    }
    private void updateAnimator(double deltaTime)
    {
        anim.speed = 1.0f;
        anim.Update((float)deltaTime);
        anim.speed = 0.0f;

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
        //Debug.Log("Compute IK");
        string nameBack = current.AvatarID + "";
        current.AvatarID = IKName;
        MIKServiceResult result = this.ServiceAccess.IKService.CalculateIKPosture(current, new List<MConstraint>() { reachConstraint }, new Dictionary<string, string>() { });
        result.Posture.AvatarID = nameBack;

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

        var rootPos = this.SkeletonAccess.GetRootPosition(this.AvatarID);
        var rootRot = this.SkeletonAccess.GetRootRotation(this.AvatarID);
        var hand = MJointType.RightWrist;
        if (handedness == "left")
            hand = MJointType.LeftWrist;
        
        string key = "GraspingPointR";
        if (this.handedness == "left")
            key = "GraspingPointL";


        if (objectGrabbed)
        {

            goalPos = this.targetObject.Transform.Position;
            goalRot = this.targetObject.Transform.Rotation;

            ObjectToWrist(this.targetConstraint.ParentToConstraint.Position,
                this.targetConstraint.ParentToConstraint.Rotation, out goalPos, out goalRot);

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
        if(!objectGrabbed && !Release)
        {
            MMICSharp.Logger.LogDebug($"PlaceObject: Not animating dues to Release = {Release}");
            animTime = reach_time;
            // do not animate, if the object was placed but the hand should remain at the position. 
        } else
        {
            this.ExecuteOnMainThread(() =>
            {
                updateAnimator(time);
                result.Posture = this.GetRetargetedPosture();

                if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Reach"))
                {
                    animTime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                }
            });
        }
        // Perform IK pass to reach the goal during the time, where the object is still grabbed or if we should never release it. 
        if (objectGrabbed || !Release)
        {
            float weight = 0.0f;

            ///During the reach animation, the ik will slowly be increased to actually reach to the target
            ///~0.48f is the normalizeTime where the target is reached
            ///TODO scale this over the distance to the objects original position
            ///TODO scale down Push, Reach, Pull of the other arm that is not used => fixes the stiffness
            if (animTime > transition_start_time && (objectGrabbed || !Release))
            {

                ///Set the position weight depending on the normalized value of the normalized animation time
                ///~0.12f is where the clip starts and ~0.48f is where the target will be reached
                weight = Mathf.Clamp((animTime - transition_start_time) / (reach_time - transition_start_time), 0, 1);
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

        // Move Object
        if(objectGrabbed)
        {
            // Move object
            var handPos = this.GetSkeletonAccess().GetGlobalJointPosition(this.AvatarID, hand).Clone();
            var handRot = this.GetSkeletonAccess().GetGlobalJointRotation(this.AvatarID, hand).Clone();

            MVector3 objPos; MQuaternion objRot;
            this.WristToObject(handPos, handRot, out objPos, out objRot);

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
        //var handPos = this.GetSkeletonAccess().GetGlobalJointPosition(this.AvatarID, hand);

        if (objectGrabbed && animTime >= reach_time)
        {
            objectGrabbed = false;

            MMICSharp.Logger.LogDebug("PlaceObject: ObjectPlaced Event");
            result.Events.Add(new MSimulationEvent("ObjectPlaced", "ObjectPlaced", this.instruction.ID));
            result.SceneManipulations[0].Transforms[0].Position = this.targetConstraint.ParentToConstraint.Position.Clone();
            result.SceneManipulations[0].Transforms[0].Rotation = this.targetConstraint.ParentToConstraint.Rotation.Clone();

            /*
            Attachments = new List<MAttachmentManipulation>()
            {
                // Not working!
                 // TODO: Implement possibility to attach objects to the ego-avatar (ego-avatar is not a scene object right now). 
                new MAttachmentManipulation(attachementPoint, this.targetObject.ID, true)
            }*/
        }
        else if (!objectGrabbed && Release)
        {
            float weight = 0.0f;

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
            }
            /*
            else if (animTime >= detransition_end_time && this.targetConstraint != null)
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
                }
                ///
                weight = Mathf.Min(1f, (animTime - detransition_end_time) / (goal_end_time - reach_time));
                result.Posture = this.IKBlendToTarget(result.Posture, gp, gr, hand, weight);
                this.SkeletonAccess.SetChannelData(result.Posture);
            }
        */


        }

        if (Release && (!objectGrabbed && animTime >= 1.0)) //MVector3Extensions.Distance(handPos, targetConstraint.ParentToConstraint.Position) < 0.11
        {
            MMICSharp.Logger.LogDebug("PlaceObject: Place Simulation Ended");
            // This is a dummy implementation: 
            result.Events.Add(new MSimulationEvent("Place Simulation Ended", mmiConstants.MSimulationEvent_End, this.instruction.ID));
            // Request target object to be moved to target position
            // clear animation cache
            this.ExecuteOnMainThread(() =>
            {
                updateAnimator(2);
            });
        }

        //To do -> insert your do step code in here
        // This is a dummy implementation: 

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
        return new List<MConstraint>() {
            /*
        new MConstraint("HandPositionConstraint")
        {
            GeometryConstraint = defaultIntermediatePosition,
            Properties = new Dictionary<string, string> { { "Handedness","Right"}, {"Type","HoldConstraint"} }
        }
            */
        };
    }

    /// <summary>
    /// Method checks if the prerequisites for starting the instruction are fulfilled.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public override MBoolResponse CheckPrerequisites(MInstruction instruction)
    {
        var rootPos = this.SkeletonAccess.GetRootPosition(instruction.AvatarID);
        MGeometryConstraint goalConstraint; MSceneObject goalObject; string handed; bool release; MGeometryConstraint reference;
        var resp = ParseInstruction(instruction, out goalConstraint, out handed, out goalObject, out release, out reference);

        // Check distance
        if (rootPos != null)
        {
            if (resp.Successful == true)
            {
                if (goalObject.Transform.Position.Subtract(rootPos).Magnitude() > RetrieveRange)
                {
                    MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "PlaceObject: goal object out of reach");

                    return new MBoolResponse(true) { LogData = new List<string>() { "Reason: out of reach" } };
                }
            }
            else
            {
                return resp;
            }
        }
        else
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "PlaceObject: no root position provided, using default TargetConstraint.");
            return new MBoolResponse(true) { LogData = new List<string>() { $"Message: no root position found for avatar {instruction.AvatarID}" } };
        }

        // Check attachment -> TODO: Attachment checking does not work right now!
        // TODO: Implement possibility to attach objects to the ego-avatar (ego-avatar is not a scene object right now). 
        var attach = this.SceneAccess.GetAttachments();

        return resp;
    }

}
