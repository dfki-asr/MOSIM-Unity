

using MMICSharp.Common;
using MMIStandard;
using MMIUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MMICSharp.Common.Tools;

public class BlendedIdle : UnityMMUBase
{
    public Dictionary<string,MJointType> BoneTypeMapping = new Dictionary<string,MJointType>();
    private Animator animator;
    private int window_size;
    private int counter;
    MAvatarPosture initialPosture;

    protected override void Awake()
    {
        //Assign the name of the MMU
        this.Name = "BlendedIdle";

        //Assign the motion type of the MMU
        this.MotionType = "Pose/BlendedIdle";
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
        this.RootTransform = this.transform;
        this.Pelvis = this.GetComponentsInChildren<Transform>().First(s => s.name == "pelvis");
        this.window_size = 60;

        //It is important that the bone assignment is done before the base class awake is called
        base.Awake();
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
            //Call the base class initialization -> Retargeting is also set up in there
            base.Initialize(avatarDescription, properties);
            this.animator = this.GetComponent<Animator>();
            //Set animation mode to always animate (even if not visible)
            this.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            this.animator.enabled = false;
            this.Name = "unityBlendedIdleMMU";
            //Get the initial posture
            this.animator.Update(0.01f);
            this.counter = 0;
            //Get the initial posture
            this.initialPosture = this.GetZeroPosture();
        });

        return new MBoolResponse(true);
    }

    /// <summary>
    /// Method to assign a MInstriction to the MMU:
    /// The method must be provided by the MMU developer.
    /// </summary>
    /// <param name="motionCommand"></param>
    /// <param name="avatarState"></param>
    /// <returns></returns>
    public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState state)
    {
        //Execute instructions on main thread
        this.ExecuteOnMainThread(() =>
        {
            //Assign the posture
            this.AssignPostureValues(state.Current);
            this.animator.Update(0.1f);
            // reset counter for blending
            this.counter = 0;
        });

        return new MBoolResponse(true);
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
            //Events = state.Events !=null? state.Events: new List<MSimulationEvent>(),
            SceneManipulations = state.SceneManipulations!=null ? state.SceneManipulations: new List<MSceneManipulation>(),
        };

        //Execute instructions on main thread
        this.ExecuteOnMainThread(() =>
        {
            this.SkeletonAccess.SetChannelData(state.Current);

            this.AssignPostureValues(state.Current);

            //this.transform.position = this.SkeletonAccess.GetRootPosition(this.AvatarDescription.AvatarID).ToVector3();
            //this.transform.rotation = this.SkeletonAccess.GetRootRotation(this.AvatarDescription.AvatarID).ToQuaternion();

            this.animator.Update((float)time);
            MAvatarPostureValues RetargetedPosture = this.GetRetargetedPosture();
            this.counter += 1;
            float weight = this.counter > this.window_size ? 1.0f : (1.0f / (float)this.window_size) * this.counter;

            MAvatarPostureValues BlendedPosture = Blending.PerformBlend(this.GetSkeleton(), state.Current, RetargetedPosture, weight, null);
            result.Posture = BlendedPosture;

            //result.Posture = this.GetRetargetedPosture();

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
        return new List<MConstraint>();
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
