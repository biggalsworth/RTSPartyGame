using System;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    Camera cam;
    Vector2 mousePos;

    //MOVEMENT
    public float dragSpeed = 1;
    private Vector3 dragOrigin;
    bool dragging;

    //Zoom
    float sensitivity = 10f;
    public float zoomSpeed = 10f;
    public float zoomSmoothTime = 0.2f;
    public float minZoomDistance = -20f;
    public float maxZoomDistance = 20f;

    private float currentZoomDistance;


    private float targetZoomDistance;
    private float zoomVelocity;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;

        dragging = false;

        currentZoomDistance = 0f;
        targetZoomDistance = 0f;

    }




    // Update is called once per frame
    void Update()
    {
        mousePos = Input.mousePosition;

        CheckMovement();

        CheckZoom();

        RestrictCamera();
    }

    private void RestrictCamera()
    {
        Vector3 clampedPos = cam.transform.position;

        clampedPos.x = Mathf.Clamp(clampedPos.x, -MatchSettings.instance.size.x * 4, MatchSettings.instance.size.x * 4);
        clampedPos.z = Mathf.Clamp(clampedPos.z, -MatchSettings.instance.size.y * 4, MatchSettings.instance.size.y * 4);

        cam.transform.position = clampedPos;

    }

    //slide movement
    public void CheckMovement()
    {
        //check if dragging
        if (Input.GetMouseButtonDown(0) && !dragging)
        {
            dragOrigin = Input.mousePosition;
            dragging = true;
        }

        if (!Input.GetMouseButton(0))
        {
            dragging = false;
        }
        if (dragging)
        {
            //while it is held, move the cam
            Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 move = new Vector3(pos.x * dragSpeed, 0, pos.y * dragSpeed);
            cam.transform.Translate(move, Space.World);
        }

        // WASD movement
        Vector3 keyboardMove = Vector3.zero;
        Vector2 keyInput = Controls.instance.input.Player.Move.ReadValue<Vector2>();

        keyboardMove += new Vector3(keyInput.x, 0, keyInput.y);

        keyboardMove = keyboardMove.normalized * 10 * Time.deltaTime;
        cam.transform.Translate(keyboardMove, Space.World);

    }

    //drag movment
    //public void CheckMovement()
    //{
    //    // Start dragging
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        dragOrigin = Input.mousePosition;
    //        dragging = true;
    //        return;
    //    }
    //
    //    // Stop dragging
    //    if (!Input.GetMouseButton(0))
    //    {
    //        dragging = false;
    //        return;
    //    }
    //
    //    if (dragging)
    //    {
    //        Vector3 difference = cam.ScreenToWorldPoint(dragOrigin) - cam.ScreenToWorldPoint(Input.mousePosition);
    //        difference.y = 0; // Keep camera level on the Y axis
    //
    //        cam.transform.position += difference;
    //        dragOrigin = Input.mousePosition; // Update origin for smooth dragging
    //    }
    //
    //}


    public void CheckZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoomDistance += scroll * zoomSpeed;
            targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);
        }

        float zoomDelta = Mathf.SmoothDamp(currentZoomDistance, targetZoomDistance, ref zoomVelocity, zoomSmoothTime);
        float moveAmount = zoomDelta - currentZoomDistance;
        currentZoomDistance = zoomDelta;

        cam.transform.position += cam.transform.forward * moveAmount;


    }
}
