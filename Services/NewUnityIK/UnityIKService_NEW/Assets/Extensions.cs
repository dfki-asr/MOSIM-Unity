using MMIStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RootMotion.FinalIK;
using UnityEngine;

namespace UnityIKService
{
    internal static class Extensions
    {
    #region HelperFunctions
        /// <summary>
        /// Helping function for the retargeting of the wrist position.
        /// </summary>
        /// <param name="oldPosition"></param>
        /// <param name="addedPosition"></param>
        /// <param name="addedRotation"></param>
        /// <returns></returns>
        public static MVector3 ConvertPosition(MVector3 oldPosition, MVector3 addedPosition, MQuaternion addedRotation)
        {
            MVector3 retargetedPosition;
            retargetedPosition = oldPosition.Add(addedRotation.Multiply(addedPosition));

            return retargetedPosition;
        }

        /// <summary>
        /// Helper-function for the retargeting of the wrist rotation
        /// </summary>
        /// <param name="oldRotation"></param>
        /// <param name="addedRotation"></param>
        /// <returns></returns>
        public static MQuaternion ConvertRotation(MQuaternion oldRotation, MQuaternion addedRotation)
        {
            MQuaternion retargetedRotation;
            retargetedRotation = oldRotation.Multiply(addedRotation);

            return retargetedRotation;
        }

        /// <summary>
        /// Helper-function to convert MVector3 into Vector3
        /// </summary>
        /// <param name="mVec"></param>
        /// <returns></returns>
        public static Vector3 MVector3ToVector3(this MVector3 mVec)
        {
            Vector3 convertedVector;
            convertedVector = new Vector3((float)mVec.X, (float)mVec.Y, (float)mVec.Z);

            return convertedVector;
        }
        /// <summary>
        /// Helper-function to convert MQuaternion to Quaternion
        /// </summary>
        /// <param name="mQuat"></param>
        /// <returns></returns>
        public static Quaternion MQuaternionToQuaternion(this MQuaternion mQuat)
        {
            Quaternion convertedQuaternion;
            convertedQuaternion = new Quaternion((float)mQuat.X, (float)mQuat.Y, (float)mQuat.Z, (float)mQuat.W);

            return convertedQuaternion;
        }
        /// <summary>
        /// Converts MJointTypes to FullBodyBipedEffectors.
        /// If no matching effector is found, FullBodyBipedEffector.Body will be returned.
        /// </summary>
        /// <param name="type">MJointType</param>
        /// <returns></returns>
        public static FullBodyBipedEffector ToIKEffectorType(this MJointType type)
        {
            switch (type)
            {
                case MJointType.LeftBall:
                    return FullBodyBipedEffector.LeftFoot;

                case MJointType.RightBall:
                    return FullBodyBipedEffector.RightFoot;

                case MJointType.LeftWrist:
                    return FullBodyBipedEffector.LeftHand;

                case MJointType.RightWrist:
                    return FullBodyBipedEffector.RightHand;
            }
            return FullBodyBipedEffector.Body;
        }
    #endregion HelperFunctions

        /// <summary>
        /// Given a List of GameObjects, this method will create a dictionary, that matches the MJointTypes to the GameObjects with fitting tags.
        /// </summary>
        /// <param name="gameObjects">List of GameObjects</param>
        /// <returns></returns>
        public static Dictionary<MJointType, GameObject> MJointTypeToGameObject(List<GameObject> gameObjects)
        {
            Dictionary<MJointType, GameObject> jointTypeToObjectDict = new Dictionary< MJointType, GameObject>();

            foreach(GameObject obj in gameObjects)
            {
                switch (obj.tag)
                {
                    case string str when str.Equals("LeftFoot"):
                        jointTypeToObjectDict.Add(MJointType.LeftBall, obj);
                        break;
                    case string str when str.Equals("RightFoot"):
                        jointTypeToObjectDict.Add(MJointType.RightBall, obj);
                        break;
                    case string str when str.Equals("LeftHand"):
                        jointTypeToObjectDict.Add(MJointType.LeftWrist, obj);
                        break;
                    case string str when str.Equals("RightHand"):
                        jointTypeToObjectDict.Add(MJointType.RightWrist, obj);
                        break;
                    case string str when str.Equals("Gaze"):
                        jointTypeToObjectDict.Add(MJointType.HeadJoint, obj);
                        break;
                }
            }
            return jointTypeToObjectDict;
        }

        /// <summary>
        /// Given a Dictionary, that matches MJointTypes to predifined GameObject (ik targets), this method will utilize a given FullBodyBipedIK to compute the IK for a certain jointType. 
        /// To do so, the selected ik target is moved and rotated according to the target position and rotation. Then, this target will be set as target of the fbbik effector of the given jointType.
        /// This IK (FABRIK) is applied to the avatar when the solver's Update function is called.
        /// This method could also be moved to a seperate class (including the ResetIK method)
        /// </summary>
        /// <param name="jointTypeToGameObject">Dictionary that matches MJointTypes to predifined GameObject ik targets</param>
        /// <param name="finalBPIK">The FullBodyBipedIK of the avatar</param>
        /// <param name="targetPosition">The position of the ik target</param>
        /// <param name="targetRotation">The rotation of the ik target</param>
        /// <param name="jointType">The type of joint that is converted into the corresponding effector of the fbbik</param>
        public static void ApplyFBBIK(Dictionary<MJointType, GameObject> jointTypeToGameObject, FullBodyBipedIK finalBPIK, MVector3 targetPosition, MQuaternion targetRotation , MJointType jointType)
        {
            IKEffector ikEffector;
            GameObject ikTarget;

            FullBodyBipedEffector fbbikEffector = jointType.ToIKEffectorType();
            if(fbbikEffector != FullBodyBipedEffector.Body)
            {
                ikTarget = jointTypeToGameObject[jointType];

                //instead of using UnityIK we will use the finalBPIK effector directly
                ikTarget.transform.position = targetPosition.MVector3ToVector3();
                ikTarget.transform.rotation = targetRotation.MQuaternionToQuaternion();

                ///TODO Gaze
                // set the lookAt target to the ikTarget
                //if (lookAtIK.enabled) lookAtIK.solver.target = ikTarget.transform;

                ikEffector = finalBPIK.solver.GetEffector(fbbikEffector);
                ikEffector.target = ikTarget.transform;
                ikEffector.positionWeight = 1f;
                ikEffector.rotationWeight = 1f;

                finalBPIK.solver.Update();
            }
        }

        /// <summary>
        /// New FinalIKApply method
        /// </summary>
        /// <param name="finalBPIK"></param>
        /// <param name="localTarget"></param>
        /// <param name="newTargetPosition"></param>
        /// <param name="newTargetRotation"></param>
        /// <param name="jointType"></param>
        public static void ApplyFinalIKToJointTargets(FullBodyBipedIK finalBPIK, GameObject localTarget, MVector3 newTargetPosition, MQuaternion newTargetRotation, MJointType jointType)
        {
            IKEffector ikEffector;
            FullBodyBipedEffector fbbikEffector = jointType.ToIKEffectorType();
            if (fbbikEffector != FullBodyBipedEffector.Body)
            {
                //instead of using UnityIK we will use the finalBPIK effector directly
                localTarget.transform.position = newTargetPosition.MVector3ToVector3();
                localTarget.transform.rotation = newTargetRotation.MQuaternionToQuaternion();

                ///TODO Gaze
                // set the lookAt target to the ikTarget
                //if (lookAtIK.enabled) lookAtIK.solver.target = ikTarget.transform;

                ikEffector = finalBPIK.solver.GetEffector(fbbikEffector);
                // FinalIK target is set to the position the localTarget, which was set to the position of the target in the target scene
                ikEffector.target = localTarget.transform;
                ikEffector.positionWeight = 1f;
                ikEffector.rotationWeight = 1f;

                finalBPIK.solver.Update();
            }
        }
        
        /// <summary>
        /// Given the FullBodyBipedIK of the avatar, this method will simply reset all of the effectors' weights and update the IK solver afterwards.
        /// This results in resetting the IK.
        /// </summary>
        /// <param name="finalBPIK">FullBodyBipedIK of the avatar</param>
        public static void ResetIK(this FullBodyBipedIK finalBPIK)
        {
            finalBPIK.enabled = true;
            foreach (IKEffector effec in finalBPIK.solver.effectors)
            {
                effec.positionWeight = 0;
                effec.rotationWeight = 0;
            }
            finalBPIK.solver.Update();
            finalBPIK.enabled = false;
        }
    }


}




