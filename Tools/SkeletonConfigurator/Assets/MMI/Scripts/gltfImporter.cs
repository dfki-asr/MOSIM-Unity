using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using MMIUnity;
using SimpleFileBrowser;

public class gltfImporter : MonoBehaviour
{
    private GameObject gltfLoader;
    private GltfImport gltf;
    private FlyCam camscript;

    private GameObject ImportButton;
    private GameObject newImportButton;
    //private GameObject ControlAfterImport;
    private GameObject ChooseRot;
    private GameObject _Background;
    
    private StepByStepSetup setupscript;

    private ApplyBVH bvhReader;
    private string bvhpath = "";
    private GameObject zeroFramePopup;


    // Start is called before the first frame update
    void Start()
    {
        //Change Fullscreen mode by script because even when changing Project Settings its not always in Windowed.
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);

        ImportButton = GameObject.Find("gltf import");
        newImportButton = GameObject.Find("gltf new import");
        //ControlAfterImport = GameObject.Find("Control-after-import");
        ChooseRot = GameObject.Find("Choose Root + Pelvis");
        _Background = GameObject.Find("Canvas").transform.Find("Background").gameObject;
        setupscript = this.gameObject.GetComponent<StepByStepSetup>();

        newImportButton.GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(BrowseFile()); });
        ImportButton.GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(BrowseFile()); });

        newImportButton.SetActive(false);
        //ControlAfterImport.SetActive(false);
        //ChooseRot.SetActive(false);
        if (gltfLoader == null)
        {
            gltfLoader = new GameObject("Avatar");
        }

        zeroFramePopup = GameObject.Find("Zeroframe");
        zeroFramePopup.SetActive(false);

    }

    private IEnumerator BrowseFile()
    {
        _Background.SetActive(true);
        setupscript.ResetToStart();
        if (gltfLoader.transform.childCount > 0)
        {
            GameObject.Destroy(gltfLoader);
            gltfLoader = new GameObject("Avatar");
            var obj = GameObject.FindObjectsOfType<WorldUI>();
            foreach(var ob in obj)
            {
                Destroy(ob.transform.parent.gameObject);
            }
        }
        FileBrowser.SetFilters(true, new FileBrowser.Filter[]{ new FileBrowser.Filter("bvh", ".bvh"), new FileBrowser.Filter("gltf",".gltf")});
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Initial path: default (Documents), Title: "Load File", submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(false, null, "Load File", "Load");

        // Dialog is closed
        // Print whether a file is chosen (FileBrowser.Success)
        // and the path to the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)

        if (FileBrowser.Success && FileBrowser.Result.Contains(".gltf"))
        {
            var path = FileBrowser.Result;
            gltf = new GltfImport();
            var task = gltf.Load(path);
            var success = false;
            yield return new WaitUntil(() => success = task.IsCompleted);

            if (success)
            {
                gltf.InstantiateMainScene(gltfLoader.transform);
                // Activate Button for Rootfind.#
                ChooseRot.SetActive(true);
                setupscript.SetUpDropdowns(gltfLoader.transform);

                //Deactivate other Buttons
                ImportButton.SetActive(false);
                newImportButton.SetActive(true);
            }
            else
            {
                Debug.Log("Loading GLTF Failed! Make sure, that a gltf file is being selected and try again.");
            }
        } else if(FileBrowser.Success && FileBrowser.Result.Contains(".bvh"))
        {
            // BVH loading
            this.bvhpath = FileBrowser.Result;
            bvhReader = this.gameObject.GetComponent<ApplyBVH>();
            //bvhReader = this.gameObject.AddComponent<ApplyBVH>();
            //bvhReader.baseObject = gltfLoader;

            //Ask if the BVH got a T-pose at first frame. 
            zeroFramePopup.SetActive(true);
            ImportButton.SetActive(false);

        }
    }

    public void LoadBVH(bool b)
    {
        GameObject root = bvhReader.Init(this.bvhpath, b);
        root.transform.parent = gltfLoader.transform;

        ChooseRot.SetActive(true);
        setupscript.SetUpDropdowns(root.transform);

        zeroFramePopup.SetActive(false);
        newImportButton.SetActive(true);
    }

    /// <summary>
    /// This method goes through all childs of the Gameobject and gives back a transform if root is inside its name.
    /// </summary>
    /// <param name="trans"></param>
    /// <returns></returns>
    private Transform findRoot(Transform trans) {
        Debug.Log($"findROOT: Looking at Childs of Transform: {trans.name}");
        Transform result = null;
        for (int i = 0; i< trans.childCount; i++)
        {
            if (trans.GetChild(i).name.ToLower().Contains("root")) {
                Debug.Log($"findROOT: Found root: {trans.GetChild(i).name}");
                return trans.GetChild(i);
            }

            else
            {
                result = findRoot(trans.GetChild(i));
                if (result != null)
                    return result;
            }
                
            
        }
        Debug.LogError("No root found");
        return result;
    
    }
}
