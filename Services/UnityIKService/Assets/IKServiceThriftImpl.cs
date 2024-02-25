using MMICSharp.Adapter;
using MMICSharp.Clients;
using MMICSharp.Common.Communication;
using MMICSharp.Common.Tools;
using RootMotion.FinalIK;
using MMICSharp.Services;
using MMIStandard;
using MMIUnity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RootMotion;
using static RootMotion.BipedReferences;
using MMIUnity.Retargeting;
using MMICSharp.Common;

namespace UnityIKService
{
    public class IKServiceThriftImpl : UnityAvatarBase, MInverseKinematicsService.Iface
    {

        public bool UseBend = false;
        public bool ApplyJointCorrection = true;
        /// In case of the T-Pose overwriting any animation -> disable the "Fix Transform" in the FullBodyBipedIK
        public FullBodyBipedIK finalBPIK;
        public LookAtIK lookAtIK;
        public List<GameObject> ikTargets;
        #region private variables

        private MAvatarPosture initialPosture;
        private MIPAddress address = new MIPAddress();
        private MIPAddress addressInt = null;
        private MIPAddress registerAddress = new MIPAddress();

        private ISVisualizationJoint scaledSkeleton = null;
        private Dictionary<string, MAvatarDescription> avatarDescriptions = new Dictionary<string, MAvatarDescription>();
        private string externalName;
        private string internalName = "lalala";
        private Dictionary<MJointType,GameObject> jointTypeToGameObject;

        public List<Transform> IK_Visualizers = new List<Transform>();


        /// <summary>
        /// Correction values
        /// </summary>
        private readonly MQuaternion WristLRot = new MQuaternion(X: 0.0447047867461652, Y: 0.96615551458421, Z: -0.0613638358178201, W: 0.24653626251476);
        private readonly MVector3 WristLPos = new MVector3(X: 5.43068740640862E-07, Y: -0.00154577306488557, Z: -3.60417249980355E-05);

        private readonly MQuaternion WristRRot = new MQuaternion(X: -0.061364120309072, Y: -0.246536483785734, Z: 0.044705334655243, W: -0.966155388143654);
        private readonly MVector3 WristRPos = new MVector3(X: 7.82020158244663E-07, Y: -0.00154565422805336, Z: 3.59681488943664E-05);


        /// <summary>
        /// The utlized service controller to host the actual service
        /// </summary>
        private ServiceController controller;

        /// <summary>
        /// The service description
        /// </summary>
        private MServiceDescription description = new MServiceDescription()
        {
            ID = "ikService31032020",
            Language = "UnityC#",
            Name = "ikService",

            //Directly define the parameters
            Parameters = new List<MParameter>()
            {
                new MParameter("test", "", "Test", true),
            }
        };


        #endregion

        private MMICSharp.Log_level logLevel = MMICSharp.Log_level.L_DEBUG;


        public static bool IsServerBuild = false;


        /// <summary>
        /// The height offset of the bend
        /// </summary>
        private Dictionary<string, float> HeightOffsetsBend = new Dictionary<string, float>();

        /// <summary>
        /// -1backwards 1 forwards
        /// </summary>
        private Dictionary<string, float> BendFacingDirections = new Dictionary<string, float>();

        protected override void Start()
        {
            base.Start();


            //Check if we are within a server build
            IsServerBuild = IsHeadlessMode();

            //Create a new instance of the logger
            MMICSharp.Logger.Instance = new UnityLogger();
            MMICSharp.Logger.Instance.Level = logLevel;

                


            //Check if this is a server build which has no visualization and a console instead
            if (IsServerBuild)
            {
                System.Console.WriteLine(@"   __  __      _ __           ______ __    _____                 _         ");
                System.Console.WriteLine(@"  / / / /___  (_) /___  __   /  _/ //_/   / ___/___  ______   __(_)_______ ");
                System.Console.WriteLine(@" / / / / __ \/ / __/ / / /   / // ,<      \__ \/ _ \/ ___/ | / / / ___/ _ \");
                System.Console.WriteLine(@"/ /_/ / / / / / /_/ /_/ /  _/ // /| |    ___/ /  __/ /   | |/ / / /__/  __/");
                System.Console.WriteLine(@"\____/_/ /_/_/\__/\__, /  /___/_/ |_|   /____/\___/_/    |___/_/\___/\___/ ");
                System.Console.WriteLine(@"                 /____/                                                    ");
            }

            else
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, "Starting IK server");
            }

            string skelConfigPath = AppDomain.CurrentDomain.BaseDirectory + "/" + this.ConfigurationFilePath;

#if UNITY_EDITOR
            skelConfigPath = Application.dataPath + "/" + this.ConfigurationFilePath;

#endif


            if (!System.IO.File.Exists(skelConfigPath))
            {
                MMICSharp.Logger.LogError($"Problem setting up retargeting: The required file: {skelConfigPath} is not available");
                return;
            }



            try
            {
                MMICSharp.Logger.Log(MMICSharp.Log_level.L_INFO, "Loading skeleton configuration from: " + skelConfigPath);

                string json = System.IO.File.ReadAllText(skelConfigPath);
                MAvatarPosture p = JsonConvert.DeserializeObject<MAvatarPosture>(json);
                p.AvatarID = internalName;

                //Setup the ik with the loaded posture
                this.SetupRetargeting(internalName, p);
                this.AssignPostureValues(retargetingService.RetargetToIntermediate(p));

                MMICSharp.Logger.LogDebug("Retargeting set up successfully");

            }
            catch (Exception e)
            {
                MMICSharp.Logger.LogError("Error: Problem at loading the configuration file");

            }










            //Application.targetFrameRate = 20;
            //QualitySettings.SetQualityLevel(0, true);


            //Add the main thread dispatcher add the beginning if not already available 
            if (GameObject.FindObjectOfType<MainThreadDispatcher>() == null)
                this.gameObject.AddComponent<MainThreadDispatcher>();


            if (this.GetComponent<Animator>() != null)
            {
                this.GetComponent<Animator>().enabled = false;
            }


            //Only use this if self_hosted and within edit mode -> Otherwise the launcher which starts the service assigns the address and port
#if UNITY_EDITOR
            this.address.Address = "127.0.0.1";
            this.address.Port = 8951;

            this.registerAddress.Port = 9009;
            this.registerAddress.Address = "127.0.0.1";
#else
        //Parse the command line arguments
        if (!this.ParseCommandLineArguments(System.Environment.GetCommandLineArgs()))
        {
            MMICSharp.Logger.Log(MMICSharp.Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the service!");
            return;
        }
#endif


            //Add the present address 
            this.description.Addresses = new List<MIPAddress>()
            {
                this.address
            };


            //Create a new service controller
            this.controller = new ServiceController(description, registerAddress, new MInverseKinematicsService.Processor(this), addressInt);

            this.controller.Start();

            //Start asynchronously
            //this.controller.StartAsync();
        }

        private void Update()
        {
            //Debug.Log("Update");
        }

        /// <summary>
        /// Basic setup method
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
        {
            if (!this.avatarDescriptions.ContainsKey(avatar.AvatarID))
                this.avatarDescriptions.Add(avatar.AvatarID, avatar);
            else
                this.avatarDescriptions[avatar.AvatarID] = avatar;

            /*
            //hier sollte die avatar mos von der Target Scene verwendet werden, momentan nur manuell gesetzt
            //könnte man sich dann auch in dem Dictionary speichern
            //string json = System.IO.File.ReadAllText("C:/Users/J4N/Desktop/UnityIKService/UnityIKService_NEW/Assets/configurations/avatar_m11.mos");
            string json = System.IO.File.ReadAllText(this.ConfigurationFilePath);
            MAvatarPosture p = JsonConvert.DeserializeObject<MAvatarPosture>(json);
            p.AvatarID = internalName;


            */
            // Setup the Dictionary, that maps the FullBodyBipedIKEffectors to the GameObjects inside the scene
            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                /*if(this.scaledSkeleton == null)
                {
                    RJoint root = RJoint.Initialize(avatar.ZeroPosture.Joints);
                    GameObject rootBone = GameObject.Instantiate(gameJointPrefab);
                    this.scaledSkeleton = new ISVisualizationJoint(root, rootBone.transform, bonenameMap.Invert());
                    scaledSkeleton.CreateGameObjSkel(rootBone);
                }
                
                scaledSkeleton.ApplyPostureValues();*/


                /*
                //Setup the ik with the loaded posture
                this.SetupRetargeting(internalName, p);
                this.AssignPostureValues(retargetingService.RetargetToIntermediate(p));
                */

                ///does not work via ZeroPosture -> Avatar gets buggy
                //this.SetupRetargeting(avatar.AvatarID, avatar.ZeroPosture);
                //this.AssignPostureValues(retargetingService.RetargetToIntermediate(avatar.ZeroPosture));

                this.jointTypeToGameObject = Extensions.MJointTypeToGameObject(ikTargets);
                finalBPIK.ResetIK();
                lookAtIK.enabled = false;
            });
            MMICSharp.Logger.LogDebug("New Retargeting Successful!");

            if (properties != null)
            {
                if (properties.ContainsKey("BendFacingDirection"))
                {
                    float bendFacingDirection = 0f;
                    if (float.TryParse(properties["BendFacingDirection"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out bendFacingDirection))
                    {
                        MMICSharp.Logger.Log(this.logLevel, "BendFacingDirection set");

                        if (!this.BendFacingDirections.ContainsKey(avatar.AvatarID))
                        {
                            this.BendFacingDirections.Add(avatar.AvatarID, bendFacingDirection);
                        }
                        else
                        {
                            this.BendFacingDirections[avatar.AvatarID] = bendFacingDirection;
                        }
                    }
                }

                if (properties.ContainsKey("HeightOffsetBend"))
                {
                    float heightOffsetBend = 0f;
                    if (float.TryParse(properties["HeightOffsetBend"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out heightOffsetBend))
                    {
                        MMICSharp.Logger.Log(this.logLevel, "HeightOffsetBend set");

                        if (!this.HeightOffsetsBend.ContainsKey(avatar.AvatarID))
                        {
                            this.HeightOffsetsBend.Add(avatar.AvatarID, heightOffsetBend);
                        }
                        else
                        {
                            this.HeightOffsetsBend[avatar.AvatarID] = heightOffsetBend;
                        }
                    }
                }
            }
            var resp = new MBoolResponse(true);
            resp.LogData = new List<string>() { "test" };
            return resp;
        }

        //TODO Update the interface, so we dont need the ComputeIK anymore
        public MAvatarPostureValues ComputeIK(MAvatarPostureValues postureValues, List<MIKProperty> properties)
        {
            return null;
        }

        private void resetIK()
        {
            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                finalBPIK.ResetIK();
                finalBPIK.solver.leftArmMapping.weight = 0;
                finalBPIK.solver.rightArmMapping.weight = 0;
                lookAtIK.enabled = false;
            });
            
        }

        /// <summary>
        /// New interface which used MConstraints
        /// </summary>
        /// <param name="postureValues"></param>
        /// <param name="constraints"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        //public override MIKServiceResult CalculateIKPosture(MAvatarPostureValues postureValues, List<MConstraint> constraints, Dictionary<string, string> properties)
        public MIKServiceResult CalculateIKPosture(MAvatarPostureValues postureValues, List<MConstraint> constraints, Dictionary<string, string> properties)
        {
            //Debug.Log("CalculateIKPosture");
            //Reset the stopwatch
            var watch = new System.Diagnostics.Stopwatch();
            watch.Restart();
            float t_prep; float t_ik = 0; int n_ik = 0;
            MIKServiceResult result = new MIKServiceResult();
            String externalAvatarID = postureValues.AvatarID + "";
            this.externalName = postureValues.AvatarID + "";

            // UnityLogger.Log(MMICSharp.Log_level.L_INFO, "Using the new ik interface");

            List<MConstraint> jointConstraints = constraints.Where(s => s.JointConstraint != null).ToList(); //.Select(s => s.JointConstraint).ToList();
               
            /// currently properties will not be sent to the IKServiceThriftImpl for some unkown reason
            /*if (properties != null)
            {
                foreach (string key in properties.Keys) Debug.Log(key);
                if (properties.ContainsKey("WatchReachTarget"))
                {
                    if (properties["WatchReachTarget"].ToLower().Equals("true"))
                    {
                        lookAtIK.enabled = true;
                    }
                    else lookAtIK.enabled = false;
                }
                else lookAtIK.enabled = false;
            }*/


            //Convert the joint constraint to ik properties

            t_prep = (float)watch.ElapsedMilliseconds;

            foreach (MConstraint c in jointConstraints)
            {
                MJointConstraint jointConstraint = c.JointConstraint;
                if (jointConstraint.GeometryConstraint != null)
                {
                    MVector3 jointPosition = null;
                    MQuaternion jointRotation = null;

                    //Check if an explicit transform is defined
                    if (jointConstraint.GeometryConstraint.ParentToConstraint != null)
                    {
                        jointPosition = jointConstraint.GeometryConstraint.ParentToConstraint.Position;
                        jointRotation = jointConstraint.GeometryConstraint.ParentToConstraint.Rotation;


                           if (ApplyJointCorrection)
                           {
                               //Convert the position and rotation
                               switch (jointConstraint.JointType)
                               {
                                   case MJointType.LeftWrist:
                                       {
                                           jointPosition = Extensions.ConvertPosition(jointPosition, WristLPos, jointRotation);
                                           jointRotation = Extensions.ConvertRotation(jointRotation, WristLRot);
                                           break;
                                       }
                                   case MJointType.RightWrist:
                                       {
                                           jointPosition = Extensions.ConvertPosition(jointPosition, WristRPos, jointRotation);
                                           jointRotation = Extensions.ConvertRotation(jointRotation, WristRRot);
                                           break;
                                       }
                               }
                           }
                       }

                       else
                       {
                           if (jointConstraint.GeometryConstraint.TranslationConstraint != null)
                           {
                               jointPosition = jointConstraint.GeometryConstraint.TranslationConstraint.GetVector3();
                           }

                           if (jointConstraint.GeometryConstraint.RotationConstraint != null)
                           {
                               jointRotation = jointConstraint.GeometryConstraint.RotationConstraint.GetQuaternion();
                           }
                       }
                    

                    //Debug.Log("Position:" + jointPosition);
                    /*
                    float posWeight = 1f; float rotWeight = 1f;
                    if(c.Properties.ContainsKey("Weight"))
                    {
                        posWeight = float.Parse(c.Properties["Weight"], System.Globalization.NumberStyles.Any);
                        rotWeight = posWeight;
                    } else {
                        if(c.Properties.ContainsKey("PositionWeight"))
                        {
                            posWeight = float.Parse(c.Properties["PositionWeight"], System.Globalization.NumberStyles.Any);
                        }
                        if(c.Properties.ContainsKey("RotationWeight"))
                        {
                            rotWeight = float.Parse(c.Properties["RotationWeight"], System.Globalization.NumberStyles.Any);
                        }
                    }*/

                    
                    if(jointPosition != null && jointRotation != null)
                    {
                        float _startCompute = watch.ElapsedMilliseconds;
                        resetIK();
                        MainThreadDispatcher.Instance.ExecuteBlocking(delegate
                        {
                            postureValues.AvatarID = this.internalName;

                            this.AssignPostureValues(postureValues);
                            finalBPIK.enabled = true;

                            Extensions.ApplyFBBIK(jointTypeToGameObject, finalBPIK, jointPosition, jointRotation, jointConstraint.JointType, (float)jointConstraint.GeometryConstraint.WeightingFactor, (float)jointConstraint.GeometryConstraint.WeightingFactor);
                            postureValues = this.GetRetargetedPosture();
                            result.Posture = postureValues;
                            result.Posture.AvatarID = this.externalName;
                            result.Error = new List<double>();

                            // Turn off fbbik for performance reasons
                            finalBPIK.enabled = false;
                        });
                        float _compute = watch.ElapsedMilliseconds - _startCompute;
                        t_ik += _compute;
                        n_ik += 1;
                    }
                }
                
            }



            result.Posture.AvatarID = this.externalName;

            float totalDuration = (float)watch.Elapsed.TotalMilliseconds;
            //MMICSharp.Logger.Log(this.logLevel, $"{result.Posture.AvatarID} Total duration: {totalDuration}ms Ik: {t_ik}ms; IK/n: {t_ik / n_ik}ms, {n_ik} times");
            watch.Stop();
            return result;
        }

        public Dictionary<string, string> GetStatus()
        {
            return new Dictionary<string, string>()
            {
                { "Running", "true"}
            };
        }



        /// <summary>
        /// Needs to be implemented in future
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Dictionary<string, string> Consume(Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }




        public MServiceDescription GetDescription()
        {
            return this.description;
        }

        #region private methods

        private void OnApplicationQuit()
        {
            try
            {
                this.controller.Dispose();
            }
            catch (Exception)
            {

            }
        }

        #endregion

        /// <summary>
        /// Indicates whether the current build is in headless mode (no graphics device)
        /// </summary>
        /// <returns></returns>
        private static bool IsHeadlessMode()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }


        /// <summary>
        /// Tries to parse the command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool ParseCommandLineArguments(string[] args)
        {
            //Parse the command line arguments
            OptionSet p = new OptionSet()
            {
                { "a|address=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          this.address.Address = addr[0];
                          this.address.Port = int.Parse(addr[1]);
                      }
                      MMICSharp.Logger.LogDebug("Address: " + v);
                  }
                },

                 { "aint|addressInternal=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          addressInt = new MIPAddress();
                          addressInt.Address = addr[0];
                          addressInt.Port = int.Parse(addr[1]);
                      }
                  }
                },

                { "r|raddress=", "The address of the register which holds the central information.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          this.registerAddress.Address = addr[0];
                          this.registerAddress.Port = int.Parse(addr[1]);
                      }
                      MMICSharp.Logger.LogDebug("Register address: " + v);
                  }
                }
            };

            try
            {
                p.Parse(args);
                return true;
            }

            catch (System.Exception)
            {
                MMICSharp.Logger.LogError("Cannot parse arguments");
            }

            return false;
        }

        public MBoolResponse Dispose(Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }

        public MBoolResponse Restart(Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }
    }



    /// <summary>
    /// Implementation of a logger which outputs the text on the unity console
    /// </summary>
    public class UnityLogger : MMICSharp.Logger
    {
        protected override void CreateDebugLog(string text)
        {
            if (IKServiceThriftImpl.IsServerBuild)
            {
                base.CreateDebugLog(text);
            }
            else
            {
                Debug.Log(text);
            }
        }



        protected override void CreateErrorLog(string text)
        {
            if (IKServiceThriftImpl.IsServerBuild)
            {
                //Call the base class
                base.CreateErrorLog(text);
            }
            else
            {
                Debug.LogError(text);
            }
        }

        protected override void CreateInfoLog(string text)
        {
            if (IKServiceThriftImpl.IsServerBuild)
            {
                //Call the base class
                base.CreateInfoLog(text);
            }
            else
            {
                Debug.Log(text);
            }

        }
    }


}
