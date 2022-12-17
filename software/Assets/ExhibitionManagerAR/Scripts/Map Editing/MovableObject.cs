/*
    Author: Dominik Truong
    Year: 2022
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ExhibitionManagerAR
{
    /// <summary>
    ///     Represents a virtual object that can be moved, rotated and scaled.
    /// </summary>
    public class MovableObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public int id;
        public string objectName;

        public GameObject selectionIndicator;
        public GameObject modelParent;

        [SerializeField]
        private GameObject axes;

        private float clickHoldTime = 0.1f;
        private float timeHold = 0f;
        private bool editingContent = false;
        private bool editingContentControlButtons = false;

        private Transform cameraTransform;
        private float movePlaneDistance;

        private Quaternion currRotation; // only used when moving object with the laser
        private Vector3 direction = Vector3.zero;   // only used when moving object with control buttons
        private float moveSpeed = 0.5f; // only used when moving object with control buttons

        private float rotationSpeed = 60.0f;
        private float rotationSpeedNreal = 120.0f;

        private bool scaleUp = false;
        private bool scaleDown = false;
        private float scaleSpeed = 1.0f;

        void Start()
        {
            cameraTransform = Camera.main.transform;
            SaveObject();
        }

        void Update()
        {
            if (MapEditingManager.Instance.selectedObject != this)
                return;

            if (editingContent && StateManager.currState == State.MapEditing)
            {
                if (DataManager.Instance.usingNreal)
                {
                    if (MapEditingManager.Instance.movingAllowed)
                    {
                        GameObject laser = GameObject.Find("LaserRaycaster");
                        var origin = laser.transform.TransformPoint(0f, 0f, 0f);
                        transform.position = origin + laser.transform.forward * Vector3.Distance(origin, transform.position);
                        transform.rotation = currRotation;
                    }
                    else if (MapEditingManager.Instance.rotationAllowed)
                    {
                        GameObject laser = GameObject.Find("LaserRaycaster");
                        var origin = laser.transform.TransformPoint(0f, 0f, 0f);
                       
                        Vector3 newPosition = origin + laser.transform.forward * Vector3.Distance(origin, transform.position);
                        Vector3 difference = newPosition - transform.position;
                        difference = new Vector3(difference.x, difference.y, 0.0f);
                        difference = Vector3.Normalize(difference);

                        float h = rotationSpeedNreal * -difference.y * Time.deltaTime;
                        float v = rotationSpeedNreal * -difference.x * Time.deltaTime;

                        Vector3 currRotation = modelParent.transform.localRotation.eulerAngles;
                        Vector3 newRotation = Vector3.zero;

                        if (MapEditingManager.Instance.rotationXAxis)
                        {
                            Vector3 tmpRotation = currRotation + new Vector3(h / 2.0f, 0.0f, 0.0f);

                            if ((tmpRotation.x % 360 >= 0.0f && tmpRotation.x % 360 <= 90.0f) ||
                                (tmpRotation.x % 360 >= 270.0f && tmpRotation.x % 360 <= 360.0f) ||
                                (tmpRotation.x >= -90.0f && tmpRotation.x <= 0.0f))
                            {
                                newRotation = tmpRotation;
                            }
                            else
                            {
                                newRotation = currRotation;
                            }
                        }
                        else if (MapEditingManager.Instance.rotationYAxis)
                        {
                            if (origin.z < newPosition.z)
                            {
                                newRotation = currRotation + new Vector3(0.0f, v, 0.0f);
                            }
                            else
                            {
                                newRotation = currRotation + new Vector3(0.0f, -v, 0.0f);
                            }
                        }
                        else if (MapEditingManager.Instance.rotationZAxis)
                        {
                            if (origin.z < newPosition.z)
                            {
                                newRotation = currRotation + new Vector3(0.0f, 0.0f, v);
                            }
                            else
                            {
                                newRotation = currRotation + new Vector3(0.0f, 0.0f, -v);
                            }
                        }
                        modelParent.transform.localRotation = Quaternion.Euler(newRotation);
                    }
                }
                else
                {
                    if (MapEditingManager.Instance.movingAllowed)
                    {
                        Vector3 projection = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, movePlaneDistance));
                        transform.position = projection;
                    }
                    else if (MapEditingManager.Instance.rotationAllowed)
                    {
                        float h = rotationSpeed * -Input.GetAxis("Mouse X") * Time.deltaTime;
                        float v = rotationSpeed * -Input.GetAxis("Mouse Y") * Time.deltaTime;

                        if (MapEditingManager.Instance.rotationXAxis)
                        {
                            modelParent.transform.Rotate(v, 0.0f, 0.0f);
                        }
                        else if (MapEditingManager.Instance.rotationYAxis)
                        {
                            modelParent.transform.Rotate(0.0f, h, 0.0f);
                        }
                        else if (MapEditingManager.Instance.rotationZAxis)
                        {
                            modelParent.transform.Rotate(0.0f, 0.0f, -v);
                        }
                    }
                }
            }

            if (editingContentControlButtons && StateManager.currState == State.MapEditing)
            {
                if (MapEditingManager.Instance.movingAllowed)
                {
                    transform.position += direction * moveSpeed * Time.deltaTime;
                }
                else if (MapEditingManager.Instance.scalingAllowed)
                {
                    if (scaleUp)
                    {
                        transform.localScale += transform.localScale * scaleSpeed * Time.deltaTime;
                    }
                    else if (scaleDown)
                    {
                        if (transform.localScale.x >= 0 &&
                            transform.localScale.y >= 0 &&
                            transform.localScale.z >= 0)
                        {
                            transform.localScale -= transform.localScale * scaleSpeed * Time.deltaTime;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Saves this object.
        /// </summary>
        public void SaveObject()
        {
            if (StateManager.currState != State.MapEditing)
                return;

            if (!MapEditingManager.Instance.contentList.Contains(this))
                MapEditingManager.Instance.contentList.Add(this);

            MapEditingManager.Instance.SaveContent();
        }

        /// <summary>
        ///     Removes this object from the list of contents and destroys itself.
        /// </summary>
        public void RemoveObject()
        {
            if (StateManager.currState != State.MapEditing)
                return;

            if (MapEditingManager.Instance.contentList.Contains(this))
                MapEditingManager.Instance.contentList.Remove(this);

            MapEditingManager.Instance.SaveContent();
            Destroy(gameObject);
        }

        /// <summary>
        ///     Sets the direction in which the object will move.
        /// </summary>
        /// <param name="inDirection"> The direction in which the object will move. </param>
        public void SetObjectDirection(Vector3 inDirection)
        {
            if (inDirection == Vector3.zero)
            {
                editingContentControlButtons = false;
                SaveObject();
            }
            else
            {
                editingContentControlButtons = true;
            }

            direction = inDirection;
        }

        /// <summary>
        ///     Scales the object up in the Update() method.
        /// </summary>
        public void ScaleUp()
        {
            editingContentControlButtons = true;
            scaleUp = true;
        }

        /// <summary>
        ///     Scales the object down in the Update() method.
        /// </summary>
        public void ScaleDown()
        {
            editingContentControlButtons = true;
            scaleDown = true;
        }

        /// <summary>
        ///     Stops scaling the object.
        /// </summary>
        public void StopScaling()
        {
            editingContentControlButtons = false;
            scaleUp = scaleDown = false;
            SaveObject();
        }

        /// <summary>
        ///     Sets the visibility of the axes.
        /// </summary>
        public void SetAxesVisibility(bool visible)
        {
            axes.SetActive(visible);
        }

        /// <summary>
        ///     Enables/disables selection indicator visiblity.
        /// </summary>
        /// <param name="visible"> Whether or not the indicator is visible. </param>
        public void ToggleSelectionIndicatorVisibility(bool visible)
        {
            selectionIndicator.SetActive(visible);
        }

        ///--------------------------------------------------------- Nreal input handling -------------------------------------------------------------------------------///

        public void OnPointerDown(PointerEventData eventData)
        {
            if (StateManager.currState != State.MapEditing || !DataManager.Instance.usingNreal)
                return;

            if (MapEditingManager.Instance.movingAllowed)
            {
                currRotation = transform.rotation;
                transform.SetParent(cameraTransform);
                editingContent = true;
            }
            else if (MapEditingManager.Instance.rotationAllowed)
            {
                editingContent = true;
            }
            else
            {
                MapEditingManager.Instance.SetSelectedObject(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (StateManager.currState != State.MapEditing || !DataManager.Instance.usingNreal)
                return;

            if (MapEditingManager.Instance.movingAllowed)
            {
                string key = DataManager.Instance.mapToLoad.id + "." + DataManager.Instance.contentToLoad;
                GameObject parent = DataManager.Instance.contentToParentDict[key];
                transform.SetParent(parent.transform);
                SaveObject();
                currRotation = Quaternion.identity;
                editingContent = false;
            }
            else if (MapEditingManager.Instance.rotationAllowed)
            {
                SaveObject();
                editingContent = false;
            }
        }

        ///--------------------------------------------------------- Android input handling -----------------------------------------------------------------------------///

        private void OnMouseDown()
        {
            if (StateManager.currState != State.MapEditing || DataManager.Instance.usingNreal)
                return;
            if (!DataManager.Instance.usingNreal && IsPointerOverUIObject())
                return;

            MapEditingManager.Instance.SetSelectedObject(this);
        }

        private void OnMouseDrag()
        {
            if (StateManager.currState != State.MapEditing || DataManager.Instance.usingNreal)
                return;
            if (!DataManager.Instance.usingNreal && IsPointerOverUIObject())
                return;

            if (MapEditingManager.Instance.movingAllowed)
            {
                timeHold += Time.deltaTime;

                if (timeHold >= clickHoldTime && !editingContent)
                {
                    movePlaneDistance = Vector3.Dot(transform.position - cameraTransform.position, cameraTransform.forward) / cameraTransform.forward.sqrMagnitude;
                    editingContent = true;
                }
            }
            else if (MapEditingManager.Instance.rotationAllowed)
            {
                editingContent = true;
            }
        }

        private void OnMouseUp()
        {
            if (StateManager.currState != State.MapEditing || DataManager.Instance.usingNreal)
                return;

            if (MapEditingManager.Instance.movingAllowed)
            {
                SaveObject();
                timeHold = 0f;
                editingContent = false;
            }
            else if (MapEditingManager.Instance.rotationAllowed)
            {
                SaveObject();
                editingContent = false;
            }
        }

        private bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

    }
}

