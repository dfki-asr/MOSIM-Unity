using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMIStandard;
using MMIUnity;
using MMICSharp.Common;

public class FinalIK_MAvatar : MonoBehaviour
{

    private MAvatarPosture reference;
    public Transform Root;
    private IntermediateSkeleton intermediate_skel = new IntermediateSkeleton();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Scales the Avatar to the size of the avatar description
    /// </summary>
    /// <param name="description"></param>
    public void ScaleAvatar(MAvatarDescription description)
    {
        reference = description.ZeroPosture;
        intermediate_skel.InitializeAnthropometry(description);
        this._scale(Root); 
    }

    /// <summary>
    /// Returns a matching joint from the this.references
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private MJoint GetMJoint (Transform t)
    {
        return GetMJoint(t, reference.Joints);
    }

    /// <summary>
    /// Return a matching joint from the list
    /// </summary>
    /// <param name="t"></param>
    /// <param name="joints"></param>
    /// <returns></returns>
    private MJoint GetMJoint(Transform t, List<MJoint> joints)
    {
        if(reference == null) { Debug.LogError("Please call ScaleAvatar before accessing the reference!"); return null; }
        foreach(MJoint j in joints)
        {
            if(j.Type.ToString() == t.name) { return j; }
        }
        //Debug.LogWarning($"No suitable joint found for {t.name}. ");
        return null;
    }

    /// <summary>
    /// Actual implementation of the scaling
    /// </summary>
    /// <param name="t"></param>
    private void _scale(Transform t)
    {
        MJoint j = GetMJoint(t);
        if(j != null)
        {
            if (j.Type == MJointType.PelvisCentre)
            {
                Vector3 offset = j.Position.ToVector3();
                t.position = offset;
            }
            // child #0 is always the primary child
            MJoint child = GetMJoint(t.GetChild(0));
            if (child != null)
            {
                // scale this to childs offset. 
                float cDist = (t.GetChild(0).position - t.position).magnitude;
                float gDist = child.Position.Magnitude();
                float scale = gDist / cDist;
                t.localScale *= scale;

                // inverse scale all children 
                float invScale = (1.0f / scale);
                for (int i = 0; i < t.childCount; i++)
                {
                    t.GetChild(i).localScale *= invScale;
                }

                // consistency check. Remove this after development. 
                float checkDist = (t.GetChild(0).position - t.position).magnitude;
                if (Mathf.Abs(checkDist - gDist) > 0.001) { Debug.Log("Scale did not work"); }
            }
            // recurse
            for (int i = 0; i < t.childCount; i++)
            {
                _scale(t.GetChild(i));
            }
        }
    }

    /// <summary>
    /// Apply the current pose to the skeleton
    /// </summary>
    /// <param name="values"></param>
    public void ApplyPosture(MAvatarPostureValues values)
    {
        intermediate_skel.SetChannelData(values);
        var posture = intermediate_skel.GetCurrentGlobalPosture(values.AvatarID);
        _setPosture(this.Root, posture.Joints);
    }

    /// <summary>
    /// Actually applying the current pose to the skeleton
    /// </summary>
    /// <param name="t"></param>
    /// <param name="joints"></param>
    private void _setPosture(Transform t, List<MJoint> joints)
    {
        MJoint j = GetMJoint(t, joints);
        if (j != null)
        {
            // it is possible to directly set the local positions and rotations,
            // because the game avatar is the same as the mosim avatar
            t.position = j.Position.ToVector3();
            t.rotation = j.Rotation.ToQuaternion();
        }
        // recurse
        for (int i = 0; i < t.childCount; i++)
        {
            _setPosture(t.GetChild(i), joints);
        }
    }

    /// <summary>
    /// Returns the current posture
    /// </summary>
    /// <returns></returns>
    public MAvatarPostureValues GetPostureValues()
    {
        MAvatarPostureValues values = new MAvatarPostureValues(reference.AvatarID, new List<double>());
        this._setValues(this.Root);
        values = intermediate_skel.RecomputeCurrentPostureValues(reference.AvatarID);
        return values;
    }

    /// <summary>
    /// Actually sets the current posture to the intermediate skeleton
    /// </summary>
    /// <param name="t"></param>
    private void _setValues(Transform t)
    {
        MJoint j = GetMJoint(t);
        if(j != null)
        {
            // It is possible to directly set the global coordinates
            // as the game avatar is the same as the intermediate skeleton
            intermediate_skel.SetGlobalJointPosition(reference.AvatarID, j.Type, t.position.ToMVector3());
            intermediate_skel.SetGlobalJointRotation(reference.AvatarID, j.Type, t.rotation.ToMQuaternion());
        }

        // recurse
        for (int i = 0; i < t.childCount; i++)
        {
            _setValues(t.GetChild(i));
        }
    }
}
