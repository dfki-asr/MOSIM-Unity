using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMIStandard;
using MMIUnity;
using RootMotion.FinalIK;
using System;

public class TestFBBPIK : MonoBehaviour
{

    public Transform ReachIKTarget_R, WalkIKTarget_R, ReachIKTarget_L, WalkIKTarget_L;
    //public UnityIKService.IKServiceThriftImpl impl;

    public FullBodyBipedIK fbbIK;
    private bool wasReaching;

    // Start is called before the first frame update
    void Start()
    {
        wasReaching = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SwitchIKMode();
        }

        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (wasReaching)
            {
                ApplyIK(fbbIK.solver.rightHandEffector, ReachIKTarget_R);
            }
            else
            {
                ApplyIK(fbbIK.solver.rightFootEffector, WalkIKTarget_R);
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (wasReaching)
            {
                ApplyIK(fbbIK.solver.leftHandEffector, ReachIKTarget_L);
            }
            else
            {
                ApplyIK(fbbIK.solver.leftFootEffector, WalkIKTarget_L);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ApplyIK(fbbIK.solver.leftHandEffector, ReachIKTarget_L, 1f);
            ApplyIK(fbbIK.solver.rightHandEffector, ReachIKTarget_R, 1f);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetLimbEffectorWeight();
        }
    }

    private void SwitchIKMode()
    {
        ResetLimbEffectorWeight();
        wasReaching = !wasReaching;
        Debug.Log("Switched IK Mode!");
    }

    private void ApplyIK(IKEffector ikEffec, Transform Target, float weightIncrease = 0.2f)
    {
        ikEffec.target = Target;
        ikEffec.positionWeight += weightIncrease;
        if (wasReaching) ikEffec.rotationWeight += weightIncrease;
        fbbIK.solver.Update();
    }
    private void ResetEffectorWeights(IKEffector effec)
    {
        effec.positionWeight = 0;
        effec.rotationWeight = 0;
    }
    private void ResetLimbEffectorWeight()
    {
        ResetEffectorWeights(fbbIK.solver.leftFootEffector);
        ResetEffectorWeights(fbbIK.solver.rightFootEffector);
        ResetEffectorWeights(fbbIK.solver.leftHandEffector);
        ResetEffectorWeights(fbbIK.solver.rightHandEffector);
    }
}
