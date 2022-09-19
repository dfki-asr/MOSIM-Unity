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

    void Awake()
    {
        flyCam = GetComponent<FlyCam>();
        camera = GetComponent<Camera>();
        isMousePressing = false;
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
                ResetToNonOrtho();
                }
            }
            else
            {
                isMousePressing = false;
            }
        
    }

    void OnMouseWheelScroll()
    {
        if(!camera.orthographic)
            flyCam.distance -= Input.mouseScrollDelta.y * zoomSensiblility;
        else 
            camera.orthographicSize -= Input.mouseScrollDelta.y * zoomSensiblility;
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
        quat *= Quaternion.Euler(0, 180, 0);
        flyCam.rotation = quat.eulerAngles;
        this.planes = planes;
        foreach (GameObject p in planes)
            p.SetActive(true);
    }

    private void ResetToNonOrtho()
    {
        camera.orthographic = false;
        if (oldTarget != null)
            flyCam.target = oldTarget;
            oldTarget = null;
        if (planes == null)
            return;
        foreach (GameObject p in planes)
            p.SetActive(false);
    }

    public void ChangeToHandOrtho(Transform hand, List<GameObject> planes)
    {
        oldTarget = flyCam.target;
        flyCam.target = hand;

        camera.orthographic = true;
        flyCam.rotation = hand.rotation.eulerAngles;

        foreach (GameObject p in planes)
            p.SetActive(true);



    }
}
