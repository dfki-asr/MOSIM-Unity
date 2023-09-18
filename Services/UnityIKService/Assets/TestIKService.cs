using MMIStandard;
using MMIUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityIKService;

public class TestIKService : MonoBehaviour
{
    public Transform IKTarget;
    public UnityIKService.IKServiceThriftImpl impl;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.UpArrow))
        //{
            List<MConstraint> cs = new List<MConstraint>()
            {
                new MConstraint(){JointConstraint = new MJointConstraint(MJointType.RightWrist){GeometryConstraint = 
                new MGeometryConstraint(""){ParentToConstraint = new MTransform("", IKTarget.position.ToMVector3(), IKTarget.rotation.ToMQuaternion(),new MVector3(1,1,1))}} }
            };
            impl.CalculateIKPosture(impl.GetPosture(), cs, null);
        //}
    }
}
