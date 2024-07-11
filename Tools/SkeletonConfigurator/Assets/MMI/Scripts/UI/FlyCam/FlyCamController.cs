using UnityEngine;
using System.Collections.Generic;



[RequireComponent(typeof(FlyCam))]
public class FlyCamController : MonoBehaviour
{
    //Edited to include orthographic Functionality
    private List<GameObject> planes;
    private Transform oldTarget;
    private FlyCam flyCam;
    private Camera camera;
    public float rotationSensibility = 0.5F;
    public float zoomSensiblility = 0.1F;
    private bool isMousePressing;
    private Vector3 currentMousePos;
    private Vector3 lastMousePos;
    private Vector3 deltaRotation;

    private bool isCtrlMousePressing;
    private Vector3 deltaTranslation;

    private bool _handMode = false;
    private Vector3 _preHandRot;
    private float _preHandDistance = 0;
    private List<GameObject> _oldplanes;

    void Awake()
    {
        flyCam = GetComponent<FlyCam>();
        camera = GetComponent<Camera>();
        isMousePressing = false;
        this.planes = new List<GameObject>();
    }
    void OnGUI()
    {
        OnRightMouseButtonDrag();
        OnMouseWheelScroll();
        OnCtrlMiddleMouseButtonDrag();
    }

    // move camera around traget using middle mouse button
    void OnRightMouseButtonDrag()
    {
            if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftShift))
            {
            //rotating the view only in non orthographic view.
                if (!camera.orthographic)
                {
                    currentMousePos = Input.mousePosition;
                    if (!isMousePressing)
                        isMousePressing = true;
                    else
                    {
                        deltaRotation = (currentMousePos - lastMousePos) * rotationSensibility;
                        deltaRotation = new Vector3(-deltaRotation.y, deltaRotation.x, 0);
                        flyCam.rotation += deltaRotation;
                    }
                    lastMousePos = currentMousePos;
                } else
                {
                // Releasing the camera when wanting to rotate
                var obj = GameObject.FindObjectOfType<StepByStepSetup>();
                if(obj != null){
                        if (obj.CanExitOrtho())
                            ResetToNonOrtho();
                    }
                }
            }
            else
            {
                isMousePressing = false;
            }
        
    }

    void OnMouseWheelScroll()
    {
        if (!camera.orthographic)
            flyCam.distance -= Input.mouseScrollDelta.y * zoomSensiblility;
        else
        {
            camera.orthographicSize -= Input.mouseScrollDelta.y * zoomSensiblility;
            if (camera.gameObject.transform.childCount > 0)
            {
                var childcam = camera.transform.GetChild(0).GetComponent<Camera>();
                childcam.orthographicSize = camera.orthographicSize;
            }
        }
    }

    void OnCtrlMiddleMouseButtonDrag()
    {
        if((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButton(1))
        {
            currentMousePos = Input.mousePosition;
            if (!isCtrlMousePressing)
                isCtrlMousePressing = true;
            else
            {
                Vector3 currentPos = new Vector3(currentMousePos.x, currentMousePos.y, flyCam.distance);
                Vector3 lastPos = new Vector3(lastMousePos.x, lastMousePos.y, flyCam.distance);
                deltaTranslation = camera.ScreenToWorldPoint(currentPos) - camera.ScreenToWorldPoint(lastPos);
                deltaTranslation = -deltaTranslation;
                flyCam.translation += deltaTranslation;
            }
            lastMousePos = currentMousePos;
        }
        else
        {
            isCtrlMousePressing = false;
        }
    }

    public void ChangeProjection(Quaternion quat, List<GameObject> planes)
    {
        camera.orthographic = true;
        camera.orthographicSize = 1.5f;
        if (_handMode)
        {
            _handMode = false;
            flyCam.distance = _preHandDistance;
            flyCam.rotation = _preHandRot;
            flyCam.target = oldTarget;
            oldTarget = null;
            foreach (GameObject p in this.planes)
                Destroy(p);
            this.planes.Clear();
        }
        if (oldTarget != null)
        {
            flyCam.target = oldTarget;
            oldTarget = null;
        }

        if (camera.gameObject.transform.childCount > 0)
        {
            var childcam = camera.transform.GetChild(0).GetComponent<Camera>();
            childcam.orthographic = true;
            childcam.orthographicSize = 1.5f;
        }

        quat *= Quaternion.Euler(0, 180, 0);
        flyCam.rotation = quat.eulerAngles;
        this.planes = new List<GameObject>(planes);
        foreach (GameObject p in planes)
            p.SetActive(true);
    }

    public void ResetToNonOrtho()
    {
        camera.orthographic = false;
        if (_handMode)
        {
            _handMode = false;
            flyCam.distance = _preHandDistance;
            flyCam.rotation = _preHandRot;
            flyCam.target = oldTarget;
            oldTarget = null;
            foreach (GameObject p in this.planes)
                Destroy(p);
            this.planes.Clear();
            this.planes = new List<GameObject>(_oldplanes);
        }
        if(camera.gameObject.transform.childCount > 0)
        {
            var childcam = camera.transform.GetChild(0).GetComponent<Camera>();
            childcam.orthographic = false;
        }
        if (planes == null)
            return;
        foreach (GameObject p in planes)
            p.SetActive(false);
    }

    public void ChangeToHandOrtho(Transform hand, List<GameObject> planes)
    {
        oldTarget = flyCam.target;
        _preHandDistance = flyCam.distance;
        _preHandRot = flyCam.rotation;
        _handMode = true;
        flyCam.target = hand;

        camera.orthographic = true;
        camera.orthographicSize = 1;
        if (camera.gameObject.transform.childCount > 0)
        {
            var childcam = camera.transform.GetChild(0).GetComponent<Camera>();
            childcam.orthographic = true;
            childcam.orthographicSize = 1;
        }
        flyCam.LookAtTargetFrom(Vector3.up, Vector3.down, 1);
        if (planes != null)
            _oldplanes = new List<GameObject>(this.planes);
        this.planes = new List<GameObject>(planes);
        if (planes == null)
            return;

        foreach (GameObject p in planes)
            p.SetActive(true);



    }

    public void ClearPlanes()
    {
        planes.Clear();
    }

    public void DeletePlanes()
    {
        foreach (GameObject p in this.planes)
        {
            p.SetActive(true);
            Destroy(p);
        }
        this.planes.Clear();


    }
}
