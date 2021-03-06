﻿/*
Author:
Vivekkumar Chaudhari (vcha0018@student.monash.edu) 
    Student - Master of Information Technology
    Monash University, Clayton, Australia

Purpose:
Developed under Summer Project 'AR Hand Gesture Capture and Analysis'

Supervisors: 
Barrett Ens (barrett.ens@monash.edu)
    Monash University, Clayton, Australia
 Max Cordeil (max.cordeil@monash.edu)
    Monash University, Clayton, Australia

About File:
Manage all UI element's interections.
*/

using DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls all UI operations.
/// </summary>
public class CanvasController : MonoBehaviour
{
    #region Global Variables
    private System.Random _random = new System.Random();
    private bool isInitialized = false;
    private float rescaleFactor = float.NaN;
    private int boxScale = 300;
    private int spaceBetweenGestureType = 100;
    private int spaceBetweenPersons = 100;
    private int spaceBetweenGestures = 40;
    private float cubeOverHandModel_ZIndex = -20.0f; 
    #endregion

    #region UI Components
    UnityEngine.UI.Toggle toleranceSwitch;
    UnityEngine.UI.Toggle animationModeSwitch;
    UnityEngine.UI.Slider toleranceSlider;
    UnityEngine.UI.Slider animationSlider;
    UnityEngine.UI.Slider zoomSlider;
    UnityEngine.UI.Button analyseButton;
    Camera mainCamera;
    TMP_InputField toleranceInput;
    TMP_Dropdown gestureTypeDDL;
    TMP_Dropdown handTypeDDL;
    TMP_Text resultTolerance;
    TMP_Text resultConsensus;
    TMP_Text outputGestureTypeLabel;
    GameObject svContent;
    GameObject gridRow;
    GameObject gridCell;
    ComparisionResult fromResult; 
    #endregion

    #region Unity Function Events
    private void Awake()
    {
        InitializeUIElements();
        GameObject handJoint = GameObject.Find("HandJoint");
        //BuildPositionVectores(GestureProcessor.Instance.TotalGestures);
        AssignHandModelsToGestures(handJoint);
        handJoint.SetActive(false);
        isInitialized = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        string gtype = GetCurrentGesture();
        ChangeVisibilityOfGestures(
            true,
            GetCurrentHandType(),
            gtype.ToLower() != "all" ? gtype : "");
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialized && animationModeSwitch.isOn)
            foreach (Person person in GestureProcessor.Instance.GestureCollection)
                foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                    foreach (Gesture gesture in gestureItem.Value)
                        if (gesture.HandModel != null)
                            gesture.ConditionCheck();
    }

    private void FixedUpdate()
    {
        if (isInitialized && animationModeSwitch.isOn)
            foreach (Person person in GestureProcessor.Instance.GestureCollection)
                foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                    foreach (Gesture gesture in gestureItem.Value)
                        if (gesture.HandModel != null)
                            gesture.AnimateInAnimationMode();
    }

    /// <summary>
    /// Draws the bounding box for a gesture
    /// </summary>
    private void OnDrawGizmos()
    {
        if (isInitialized)
            foreach (Person person in GestureProcessor.Instance.GestureCollection)
                foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                    foreach (Gesture gesture in gestureItem.Value)
                    {
                        if (gesture.HandModel != null)
                        {
                            Gizmos.color = Color.red;
                            if (gesture.HandModel != null && gesture.HandModel.activeSelf)
                            {
                                Gizmos.DrawWireCube(gesture.Centroid - gesture.PositionFactor * rescaleFactor, gesture.GetBoundingBoxSize() * rescaleFactor);
                            }
                        }
                    }
    }
    #endregion

    #region UI Initialization, Events
    private void InitializeUIElements()
    {
        svContent = GameObject.Find("SVContent");
        gridRow = GameObject.Find("Row");
        gridCell = GameObject.Find("RowCell");

        outputGestureTypeLabel = GameObject.Find("OutputGestureTypeLabel").GetComponent<TMP_Text>();
        resultTolerance = GameObject.Find("ResultTolerance").GetComponent<TMP_Text>();
        resultConsensus = GameObject.Find("ResultConsensus").GetComponent<TMP_Text>();
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        analyseButton = GameObject.Find("AnalyseButton").GetComponent<UnityEngine.UI.Button>();
        toleranceSwitch = GameObject.Find("ToleranceSwitch").GetComponent<UnityEngine.UI.Toggle>();
        toleranceSlider = GameObject.Find("ToleranceSlider").GetComponent<UnityEngine.UI.Slider>();
        animationModeSwitch = GameObject.Find("AnimationModeSwitch").GetComponent<UnityEngine.UI.Toggle>();
        animationSlider = GameObject.Find("AnimationSlider").GetComponent<UnityEngine.UI.Slider>();
        zoomSlider = GameObject.Find("ZoomSlider").GetComponent<UnityEngine.UI.Slider>();
        toleranceInput = GameObject.Find("ToleranceInput").GetComponent<TMP_InputField>();
        gestureTypeDDL = GameObject.Find("GestureTypeDDL").GetComponent<TMP_Dropdown>();
        handTypeDDL = GameObject.Find("HandTypeDDL").GetComponent<TMP_Dropdown>();

        gridRow.SetActive(false);

        analyseButton.onClick.AddListener(delegate { OnAnalyseButtonClick(analyseButton); });

        toleranceInput.enabled = toleranceSwitch.isOn = false;
        toleranceSwitch.onValueChanged.AddListener(delegate { OnToleranceSwitchValueChanged(toleranceSwitch); });

        toleranceSlider.minValue = 0;
        toleranceSlider.maxValue = 100;
        toleranceSlider.value = 0;
        toleranceSlider.wholeNumbers = true;
        toleranceInput.text = "0.00";
        toleranceSlider.onValueChanged.AddListener(delegate { OnToleranceSliderValueChanged(toleranceSlider); });

        List<string> gnames = Enum.GetNames(typeof(GestureTypeFormat)).ToList();
        gnames.Remove("None");
        gnames.Add("ALL");
        gnames.Sort();
        gestureTypeDDL.options = new List<TMP_Dropdown.OptionData>(
            from item in gnames
            select new TMP_Dropdown.OptionData(item)
            );
        gestureTypeDDL.value = 0;
        gestureTypeDDL.onValueChanged.AddListener(delegate { OnGestureTypeDDLValueChanged(gestureTypeDDL); });

        List<string> htypes = Enum.GetNames(typeof(HandTypeFormat)).ToList();
        htypes.Sort();
        handTypeDDL.options = new List<TMP_Dropdown.OptionData>(
            from item in htypes
            select new TMP_Dropdown.OptionData(item));
        handTypeDDL.value = 0;
        handTypeDDL.onValueChanged.AddListener(delegate { OnHandTypeDDLValueChanged(handTypeDDL); });

        animationModeSwitch.isOn = true;
        animationModeSwitch.onValueChanged.AddListener(delegate { OnAnimationModeSwitchValueChanged(animationModeSwitch); });

        animationSlider.enabled = !animationModeSwitch.isOn;
        animationSlider.minValue = 0;
        animationSlider.maxValue = 20;
        animationSlider.value = 0;
        animationSlider.wholeNumbers = true;
        animationSlider.onValueChanged.AddListener(delegate { OnAnimationSliderValueChanged(animationSlider); });

        zoomSlider.minValue = 35;
        zoomSlider.maxValue = 70;
        zoomSlider.value = mainCamera.fieldOfView;
        zoomSlider.wholeNumbers = true;
        zoomSlider.onValueChanged.AddListener(delegate { OnZoomSliderValueChanged(zoomSlider); });

    }

    private void OnZoomSliderValueChanged(UnityEngine.UI.Slider slider)
    {
        mainCamera.fieldOfView = slider.value;
    }

    private void OnAnimationSliderValueChanged(UnityEngine.UI.Slider slider)
    {
        foreach (Person person in GestureProcessor.Instance.GestureCollection)
            foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                foreach (Gesture gesture in gestureItem.Value)
                    if (gesture.HandModel != null)
                        gesture.AnimateInSliderMode(slider.maxValue, slider.value);
    }

    private void OnAnimationModeSwitchValueChanged(UnityEngine.UI.Toggle toggle)
    {
        animationSlider.enabled = !toggle.isOn;
        foreach (Person person in GestureProcessor.Instance.GestureCollection)
            foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                foreach (Gesture gesture in gestureItem.Value)
                    if (gesture.HandModel != null)
                        gesture.Reset();
    }

    private void OnToleranceSwitchValueChanged(UnityEngine.UI.Toggle toggle)
    {
        toleranceInput.enabled = toggle.isOn;
    }

    private void OnToleranceSliderValueChanged(UnityEngine.UI.Slider slider)
    {
        toleranceInput.text = Math.Round(slider.value / 100, 2).ToString();
        if (fromResult != null)
        {
            resultTolerance.text = string.Format("Tolerance: {0}", toleranceInput.text);
            resultConsensus.text = string.Format("Consesnsus: {0}%", fromResult.GetRelativeConsensus(Math.Round(slider.value / 100, 2)));
            HighLightGesturePair(Math.Round(slider.value / 100, 2));
        }
    }

    private void OnGestureTypeDDLValueChanged(TMP_Dropdown ddl)
    {
        string gtype = GetCurrentGesture();
        ChangeVisibilityOfGestures(
            true,
            GetCurrentHandType(),
            gtype.ToLower() != "all" ? gtype : "");
    }

    private void OnHandTypeDDLValueChanged(TMP_Dropdown ddl)
    {
        string gtype = GetCurrentGesture();
        ChangeVisibilityOfGestures(
            true,
            GetCurrentHandType(),
            gtype.ToLower() != "all" ? gtype : "");
    }

    private void OnAnalyseButtonClick(UnityEngine.UI.Button button)
    {
        string gestureType = GetCurrentGesture();
        GestureTypeFormat gType = GestureTypeFormat.None;
        if (Enum.TryParse(gestureType.Trim().ToLower(), ignoreCase: true, out gType) && gType != GestureTypeFormat.None)
        {
            fromResult = GestureProcessor.Instance.GetConsensusOfPersons(
                dissimilarityFunctionType: DissimilarityFunctionType.NormalizedDTW,
                aggregationType: AggregationType.Average,
                gestureType: gType,
                handType: GetCurrentHandType(),
                graphScale: 100
                );
            double inputtolerancevalue = toleranceSwitch.isOn ? double.Parse(toleranceInput.text) : -1;
            Tuple<double, double> toleranceConsensusPair;
            if (!toleranceSwitch.isOn)
                toleranceConsensusPair = fromResult.GetHighestToleranceConsensusPair();
            else
                toleranceConsensusPair = new Tuple<double, double>(
                    Math.Round(inputtolerancevalue, 2),
                    fromResult.GetRelativeConsensus(Math.Round(inputtolerancevalue, 2)));

            resultTolerance.text = string.Format("Tolerance: {0}", Math.Round(toleranceConsensusPair.Item1, 2));
            resultConsensus.text = string.Format("Consesnsus: {0}%", Math.Round(toleranceConsensusPair.Item2, 2));
            toleranceSlider.value = (int)(Math.Round(toleranceConsensusPair.Item1, 2) * 100);
            toleranceInput.text = Math.Round(toleranceConsensusPair.Item1, 2).ToString();
            CreateResultTable(fromResult);
            HighLightGesturePair(Math.Round(toleranceConsensusPair.Item1, 2));
        }
        else
            Debug.LogError("Invalid Gesture Type entered!");
    }
    #endregion

    /// <summary>
    /// Generate HandModel view(GameObject) with required joints and other objects.
    /// </summary>
    /// <param name="handJointObject"></param>
    /// <param name="parentObject"></param>
    /// <returns></returns>
    private GameObject BuildHandModel(GameObject handJointObject, GameObject parentObject)
    {
        if (parentObject == null)
            throw new System.ArgumentException("Empty Parent Handmodel!");
        GameObject handModel = Instantiate(GameObject.Find("HandModelTemplate"));
        handModel.name = string.Format("HandModel_{0}", _random.Next(0, 9999).ToString("D4"));
        handModel.transform.SetParent(parentObject.transform);
        handModel.transform.localPosition = new Vector3(0, 0, 0);
        handModel.transform.localRotation = Quaternion.Euler(0, 0, 0);
        rescaleFactor = float.Parse(System.Math.Sqrt(Screen.width * Screen.height / GestureProcessor.Instance.TotalGestures).ToString()) / boxScale;
        handModel.transform.localScale *= (rescaleFactor - 0.05f);
        var jointsObject = handModel.transform.Find("Joints");

        if (handJointObject != null)
        {
            for (int i = 0; i < Constants.NUM_JOINTS; i++)
            {
                GameObject clone = Instantiate(handJointObject, Vector3.zero, Quaternion.identity, jointsObject.transform);
                clone.name = string.Format("Joint{0}", i);
            }
        }
        else
            throw new System.Exception("Unable to Find HandJoint GameObject!");

        return handModel;
    }

    /// <summary>
    /// Assign HandModel to related Gesture class.
    /// </summary>
    /// <param name="handJointObject"></param>
    private void AssignHandModelsToGestures(GameObject handJointObject)
    {
        GameObject haneModelParent = GameObject.Find("HandModels");
        List<Person> gesture_collection = GestureProcessor.Instance.GestureCollection;
        Vector3 nextVector = new Vector3(0, 0, 0);
        foreach (Person person in gesture_collection)
        {
            foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
            {
                foreach (Gesture gesture in gestureItem.Value)
                {
                    gesture.HandModel = BuildHandModel(handJointObject, haneModelParent);
                    gesture.HandModel.SetActive(false);
                    gesture.Tag = gestureItem.Key.ToString();
                    gesture.HandModel.GetComponentInChildren<CubeController>().gestureName =
                        string.Format("{0} | {1} | {2}", person.Name, gesture.HandType.ToString(), gestureItem.Key.ToString());

                    // Prepair data....
                    gesture.NormalizeGesture();
                    gesture.TransformJoints();
                }
            }
        }
    }

    /// <summary>
    /// To update position of HandModels along with transparent Cube. relative to 0,0,0.
    /// </summary>
    /// <param name="gestureType"></param>
    /// <param name="handType"></param>
    private void SetHandModelPositions(GestureTypeFormat gestureType, HandTypeFormat handType)
    {
        Vector3 nextVector = new Vector3(0, 0, 0);
        foreach (Person person in GestureProcessor.Instance.GestureCollection)
        {
            foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
            {
                if (gestureItem.Key == gestureType || gestureType == GestureTypeFormat.None)
                {
                    foreach (Gesture gesture in gestureItem.Value)
                    {
                        if (gesture.HandType == handType && gesture.HandModel != null)
                        {
                            gesture.PositionFactor = new Vector3(nextVector.x, nextVector.y, nextVector.z);
                            gesture.HandModel.transform.localPosition = new Vector3((nextVector.x / boxScale) * 14, (nextVector.y / boxScale) * 14, 0);
                            gesture.HandModel.transform.GetChild(0).localPosition = new Vector3(gesture.PositionFactor.x, gesture.PositionFactor.y, cubeOverHandModel_ZIndex);
                            nextVector = new Vector3(nextVector.x + boxScale + spaceBetweenGestures, nextVector.y, nextVector.z);

                            gesture.Reset();
                            gesture.ConditionCheck();
                            gesture.AnimateInAnimationMode();
                        }
                    }
                    nextVector = new Vector3(nextVector.x + spaceBetweenGestureType, nextVector.y, nextVector.z);
                }
            }
            nextVector = new Vector3(0, nextVector.y + boxScale + spaceBetweenPersons, 0);
        }
    }

    /// <summary>
    /// To change visibility of handmodels based on gesture type and hand type. It also update the position of the handmodels.
    /// </summary>
    /// <param name="visibility"></param>
    /// <param name="handType"></param>
    /// <param name="byCategoryName"></param>
    private void ChangeVisibilityOfGestures(bool visibility, HandTypeFormat handType, string byCategoryName = "")
    {
        GestureTypeFormat gType = GestureTypeFormat.None;
        if (byCategoryName != null && byCategoryName != "")
            if (!System.Enum.TryParse(byCategoryName, out gType))
                throw new System.Exception("Cannot able to convert given category name to format.");
        SetHandModelPositions(gType, handType);
        foreach (Person person in GestureProcessor.Instance.GestureCollection)
            foreach (KeyValuePair<GestureTypeFormat, List<Gesture>> gestureItem in person.Gestures)
                foreach (Gesture gesture in gestureItem.Value)
                {
                    if (gesture.HandModel != null)
                        if (gesture.HandType == handType)
                        {
                            if (gType != GestureTypeFormat.None && gestureItem.Key == gType)
                                gesture.HandModel.SetActive(visibility);
                            else if (byCategoryName == null || byCategoryName == "")
                                gesture.HandModel.SetActive(visibility);
                            else
                                gesture.HandModel.SetActive(!visibility);
                        }
                        else
                            gesture.HandModel.SetActive(false);
                }
    }

    /// <summary>
    /// To get current selected hand type.
    /// </summary>
    /// <returns></returns>
    private HandTypeFormat GetCurrentHandType()
    {
        return (HandTypeFormat)Enum.Parse(typeof(HandTypeFormat), handTypeDDL.options[handTypeDDL.value].text);
    }

    /// <summary>
    /// To get current selected gesture type.
    /// </summary>
    /// <returns></returns>
    private string GetCurrentGesture()
    {
        return gestureTypeDDL.options[gestureTypeDDL.value].text;
    }

    /// <summary>
    /// Create Dissimilarity Matric by adding Rows
    /// </summary>
    /// <param name="fromResult"></param>
    private void CreateResultTable(ComparisionResult fromResult)
    {
        outputGestureTypeLabel.text = GetCurrentGesture();
        if (svContent.transform.childCount > 1)
            for (int i = 1; i < svContent.transform.childCount; i++)
                Destroy(svContent.transform.GetChild(i).gameObject);
        var result = fromResult.GetDissimilarityMatric();
        for (int i = 0; i < result.Count; i++)
        {
            GameObject newRow = Instantiate(gridRow, svContent.transform);
            newRow.SetActive(true);
            newRow.name = string.Format("Row{0}", i);
            newRow.transform.GetChild(0).gameObject.SetActive(false);
            newRow.GetComponent<UnityEngine.UI.RawImage>().color = new Color(132, 219, 121, 0);
            AddCell(newRow, string.Format("{0} {1}", "Person", result[i].Item1), new RectOffset(5, 0, 0, 0));
            AddCell(newRow, string.Format("{0} {1}", "Person", result[i].Item2), new RectOffset(5, 0, 0, 0));
            AddCell(newRow, string.Format("{0}", result[i].Item3), new RectOffset(0, 18, 0, 0), TextAlignmentOptions.MidlineRight);
        }
    }

    /// <summary>
    /// To add cell object into row object.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="text"></param>
    /// <param name="cellPadding"></param>
    /// <param name="textAlignment"></param>
    private void AddCell(GameObject row, string text, RectOffset cellPadding, TextAlignmentOptions textAlignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject newCell = Instantiate(gridCell, row.transform);
        newCell.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().padding = cellPadding;
        TMP_Text textComponent = newCell.GetComponentInChildren<TMP_Text>();
        textComponent.color = Color.black;
        textComponent.text = text;
        textComponent.alignment = textAlignment;
    }

    /// <summary>
    /// Highlight Rows that falls under current tolerance value.
    /// </summary>
    /// <param name="currentToleranceValue"></param>
    private void HighLightGesturePair(double currentToleranceValue)
    {
        if (fromResult != null && svContent.transform.childCount > 0)
        {
            for (int i = 1; i < svContent.transform.childCount; i++)
            {
                var child = svContent.transform.GetChild(i);
                double childToleranceValue = Math.Round(double.Parse(child.GetChild(3).transform.GetChild(0).GetComponent<TMP_Text>().text), 2);
                child.GetComponent<UnityEngine.UI.RawImage>().color = (childToleranceValue <= currentToleranceValue) ? new Color(0.454902f, 0.7490196f, 0.4156863f, 0.67f) : new Color(0.454902f, 0.7490196f, 0.4156863f, 0.0f);
            }
        }
    }

}
