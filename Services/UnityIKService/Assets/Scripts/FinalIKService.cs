using MMICSharp.Services;
using MMIStandard;
using MMIUnity;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIKService;

public class FinalIKService : MonoBehaviour, MInverseKinematicsService.Iface
{
    // Prefab object configured for IK based upon the Intermediate Skeleton hierarchy. 
    public GameObject IK_Avatar_Prefab;
    // List of avatar instances. 
    private Dictionary<string, FinalIK_MAvatar> avatars = new Dictionary<string, FinalIK_MAvatar>();
    // used for server build
    public static bool IsServerBuild = false;

    /// <summary>
    ///  Address for this service
    /// </summary>
    public MIPAddress address = new MIPAddress();
    /// <summary>
    /// Address for the registry. 
    /// </summary>
    public MIPAddress registerAddress = new MIPAddress();

    /// <summary>
    /// Service Controller managing the MOSIM Service communication. 
    /// </summary>
    private ServiceController controller;


    /// <summary>
    /// The Setup method will be called before any other method is called. In this method, the 
    /// IK Avatar is spawned and scaled. 
    /// The parameters are defined by the interface and cannot be changed. 
    /// </summary>
    /// <param name="avatar"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
    {
        Debug.Log("Setup");
        this.ExecuteOnMainThread(() =>
        {
            // Instantiate 
            var go = GameObject.Instantiate(IK_Avatar_Prefab, this.transform);
            var component = go.GetComponent<FinalIK_MAvatar>();
            
            // scale avatar
            component.ScaleAvatar(avatar);

            // replace in container list if already existing
            if (avatars.ContainsKey(avatar.AvatarID))
            {
                GameObject.Destroy(avatars[avatar.AvatarID].gameObject);
                avatars.Remove(avatar.AvatarID);
            }
            // otherwise add
            avatars.Add(avatar.AvatarID, component);
            avatars[avatar.AvatarID].SetupBaseTargets();
        });
        return new MBoolResponse(true);
    }


    /// <summary>
    /// Perform the IK Pass
    /// </summary>
    /// <param name="postureValues"></param>
    /// <param name="constraints"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public MIKServiceResult CalculateIKPosture(MAvatarPostureValues postureValues, List<MConstraint> constraints, Dictionary<string, string> properties)
    {
        if(!this.avatars.ContainsKey(postureValues.AvatarID))
        {
            Debug.LogError("Avatar not available, call setup before calculating postures!");
            return new MIKServiceResult(postureValues, false, new List<double> ());
        }
        MAvatarPostureValues resultP = postureValues;
        FinalIK_MAvatar currentAvatar = avatars[postureValues.AvatarID];


        MIKServiceResult result = new MIKServiceResult();

        //bool reached = false;
        this.ExecuteOnMainThread(() =>
        {
            currentAvatar.ApplyPosture(postureValues);

            List<MConstraint> jointConstraints = constraints.Where(s => s.JointConstraint != null).ToList();

            //Convert the joint constraint to ik properties

            foreach (MConstraint c in jointConstraints)
            {
                var jointConstraint = c.JointConstraint;
                if (jointConstraint.GeometryConstraint != null)
                {
                    MVector3 jointPosition = null;
                    MQuaternion jointRotation = null;

                    //Check if an explicit transform is defined
                    if (jointConstraint.GeometryConstraint.ParentToConstraint != null)
                    {
                        jointPosition = jointConstraint.GeometryConstraint.ParentToConstraint.Position;
                        jointRotation = jointConstraint.GeometryConstraint.ParentToConstraint.Rotation;
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


                    Debug.Log("Position:" + jointPosition);

                    float posWeight = 1f; float rotWeight = 1f;
                    if (c.Properties.ContainsKey("Weight"))
                    {
                        posWeight = float.Parse(c.Properties["Weight"], System.Globalization.NumberStyles.Any);
                        rotWeight = posWeight;
                    }
                    else
                    {
                        if (c.Properties.ContainsKey("PositionWeight"))
                        {
                            posWeight = float.Parse(c.Properties["PositionWeight"], System.Globalization.NumberStyles.Any);
                        }
                        if (c.Properties.ContainsKey("RotationWeight"))
                        {
                            rotWeight = float.Parse(c.Properties["RotationWeight"], System.Globalization.NumberStyles.Any);
                        }
                    }

                    // if the jointPosition and rotation exist -> use the FinalIK
                    if (jointPosition != null && jointRotation != null)
                    {
                        currentAvatar.finalBPIK.ResetIK();
                        //this.avatars[postureValues.AvatarID].ApplyPosture(postureValues);
                        currentAvatar.finalBPIK.enabled = true;

                        // Apply the Final IK (Full Body Biped IK)

                        UnityIKService.Extensions.ApplyFBBIK(currentAvatar.jointTypeToGameObject, currentAvatar.finalBPIK, jointPosition, jointRotation, jointConstraint.JointType, posWeight, rotWeight);

                        result.Posture = currentAvatar.GetPostureValues();
                        result.Posture.AvatarID = postureValues.AvatarID;
                        result.Error = new List<double>();

                        // Turn off fbbik for performance reasons
                        currentAvatar.finalBPIK.enabled = false;
                    }
                }
            }

            resultP = currentAvatar.GetPostureValues();
        });
        //MIKServiceResult result = new MIKServiceResult(resultP, reached, new List<double>());
        result.Success = true;
        return result;
    }

    public MAvatarPostureValues ComputeIK(MAvatarPostureValues postureValues, List<MIKProperty> properties)
    {
        // This method will be deprecated
        throw new System.NotImplementedException();
    }

    public Dictionary<string, string> Consume(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }

    public MBoolResponse Dispose(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// The service description
    /// </summary>
    private MServiceDescription description = new MServiceDescription()
    {
        ID = "FinalIkServiceV001",
        Language = "UnityC#",
        Name = "ikService",

        //Directly define the parameters
        Parameters = new List<MParameter>()
            {
                new MParameter("test", "", "Test", true),
            }
    };

    public MServiceDescription GetDescription()
    {
        return this.description;
    }

    public Dictionary<string, string> GetStatus()
    {
        return new Dictionary<string, string>()
            {
                { "Running", "true"}
            };
    }

    public MBoolResponse Restart(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }



    private void Start()
    {
        //Check if we are within a server build
        IsServerBuild = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

        //Create a new instance of the logger
        MMICSharp.Logger.Instance = new UnityLogger();
        MMICSharp.Logger.Instance.Level = MMICSharp.Log_level.L_INFO;


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

        //Application.targetFrameRate = 20;
        //QualitySettings.SetQualityLevel(0, true);


        //Add the main thread dispatcher add the beginning if not already available 
        if (GameObject.FindObjectOfType<MainThreadDispatcher>() == null)
            this.gameObject.AddComponent<MainThreadDispatcher>();


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
        this.controller = new ServiceController(description, registerAddress, new MInverseKinematicsService.Processor(this));

        this.controller.Start();

        //Start asynchronously
        //this.controller.StartAsync();
    }
    /// <summary>
    /// Any method changing the scene graph needs to run on the main thread. 
    /// </summary>
    /// <param name="function"></param>
    private void ExecuteOnMainThread(Action function)
    {
        if (MainThreadDispatcher.Instance == null)
        {
            UnityEngine.Debug.Log("Cannot execute on main thread, Main thread dispatcher not available");
        }

        //Execute using MainThreadDispatcher
        MainThreadDispatcher.Instance.ExecuteBlocking(function);
    }

    /// <summary>
    /// Tries to parse the command line arguments
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private bool ParseCommandLineArguments(string[] args)
    {
        //Parse the command line arguments
        MMICSharp.Adapter.OptionSet p = new MMICSharp.Adapter.OptionSet()
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
                      Debug.Log("Address: " + v);
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
                      Debug.Log("Register address: " + v);
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
            Debug.Log("Cannot parse arguments");
        }

        return false;
    }

}
