﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UI.Toggle;

public class UIManager : MonoBehaviour
{

    public InputField inptMassVal;
    public InputField inptRadiusVal;
    public InputField inptDensityVal;
    public Transform contentPanel;
    public Transform viewPort;
    public PhysicsEngine physicsEngine;
    public int manipMode; //0 = Launch Mode, 1 = Move mode
    public bool spawnSymetry;
    public int symDivs;
    public float orbVMultiplier;

    private AudioVisualTranslator audioVT;
    private PhysicsObject selectedObject;
    private CUIColorPicker colPicker;
    private Dictionary<string, Object> CelestialObjects = new Dictionary<string, Object>();
    private Dictionary<string, Material> Skyboxes = new Dictionary<string, Material>();
    private GameObject objectToSpawn;
    private GameObject activePanel;
    private GameObject planetPanel;
    private GameObject starPanel;
    private GameObject othersPanel;
    private GameObject pauseButton;
    private GameObject playButton;
    private InputField inptTime;
    private InputField inptDivs;
    private Text objectName;
    private CanvasGroup canvasGroup;
    private GameObject pausePanel;
    private InputField inptPosX;
    private InputField inptPosY;
    private InputField inptPosZ;
    private Image imgSpawnObj;
    private Color desiredTrailColor;
    private GameObject panObjects;
    private GameObject panBrush;
    private GameObject panSpawn;
    private GameObject panObject;
    private GameObject panSettings;
    private GameObject panBackgrounds;
    private UnityEngine.UI.Button tabObj;
    private UnityEngine.UI.Button tabScene;
    
    InfiniteGrids placementGrid;

    void Awake()
    {
        manipMode = 1;
        symDivs = 0;
        spawnSymetry = false;
        orbVMultiplier = 0;

        physicsEngine = FindObjectOfType<PhysicsEngine>();
    }

    // Use this for initialization
    void Start ()
    {
        panObjects = transform.Find("panLeft").gameObject;
        panBrush = transform.Find("panBrush").gameObject;
        panSpawn = transform.Find("panSpawn").gameObject;
        panObject = transform.Find("panObject").gameObject;
        panSettings = transform.Find("panSettings").gameObject;
        panBackgrounds = transform.Find("panBackgrounds").gameObject;
	    objectName = transform.Find("panObject/TitleObj").GetComponent<Text>();
	    playButton = transform.Find("panBottom/btnPlay").gameObject;
	    pauseButton = transform.Find("panBottom/btnPause").gameObject;
	    inptTime = transform.Find("panBottom/txtTimeScale/inptTime").GetComponent<InputField>();
	    inptDivs = transform.Find("panSpawn/txtSym/inptDivs").GetComponent<InputField>();
        planetPanel = transform.Find("panLeft/panPlanets").gameObject;
        starPanel = transform.Find("panLeft/panStars").gameObject;
        othersPanel = transform.Find("panLeft/panOthers").gameObject;
	    pausePanel = transform.Find("panPause").gameObject;
	    inptPosX = transform.Find("panObject/txtPosX/inptPosX").GetComponent<InputField>();
	    inptPosY = transform.Find("panObject/txtPosY/inptPosY").GetComponent<InputField>();
	    inptPosZ = transform.Find("panObject/txtPosZ/inptPosZ").GetComponent<InputField>();
	    imgSpawnObj = transform.Find("panBrush/imgSpawnObj").GetComponent<Image>();
        tabObj = transform.Find("panTabs/tabObjs/btnObjs").GetComponent<UnityEngine.UI.Button>();
        tabScene = transform.Find("panTabs/tabScene/btnScene").GetComponent<UnityEngine.UI.Button>();
        audioVT = GameObject.FindObjectOfType<AudioVisualTranslator>();
	    colPicker = GameObject.FindObjectOfType<CUIColorPicker>();
        activePanel = starPanel;
	    canvasGroup = transform.GetComponent<CanvasGroup>();
        placementGrid = FindObjectOfType<InfiniteGrids>();

	    inptDivs.text = symDivs.ToString();

	    inptTime.text = Time.timeScale.ToString();

	    colPicker.SetOnValueChangeCallback(TrailColChanged);

        //Highlight Active Tab
        ColorBlock colBlock = ColorBlock.defaultColorBlock;
        colBlock.colorMultiplier = 1.5f;
        tabObj.colors = colBlock;

        //Load Celestial Objects
        Object[] CelestialObj = Resources.LoadAll("Prefabs/Objects");
	    foreach (Object obj in CelestialObj)
	    {
	        CelestialObjects.Add(obj.name, obj);
	    }

        //Load Skyboxes
        Object[] sbxs = Resources.LoadAll("Materials/Skyboxes");
        foreach (Object skybox in sbxs)
        {
            Skyboxes.Add(skybox.name, (Material)skybox);
        }

        SetSelectedObject(GameObject.FindGameObjectWithTag("host").GetComponent<PhysicsObject>());
    }

	
	// Update is called once per frame
	void Update ()      
	{
        //Update properties of selected object based on UI
	    if (selectedObject != null)
	    {
            //Mass
            if(!inptMassVal.isFocused)
	            inptMassVal.text = selectedObject.rb.mass.ToString();
            //Radius
            if(!inptRadiusVal.isFocused)
                inptRadiusVal.text = selectedObject.Radius.ToString();
            //Density
            if(!inptDensityVal.isFocused)
                inptDensityVal.text = selectedObject.Density.ToString();            
            //Name
	        objectName.text = selectedObject.name.ToUpper();
	        //PosX
            if (!inptPosX.isFocused)
	            inptPosX.text = selectedObject.rb.position.x.ToString();
	        //PosY
            if(!inptPosY.isFocused)
	            inptPosY.text = selectedObject.rb.position.y.ToString();
	        //PosZ
            if(!inptPosZ.isFocused)
    	        inptPosZ.text = selectedObject.rb.position.z.ToString();

        }

        //Update timescale based on UI
        if (!inptTime.isFocused)
	        inptTime.text = Time.timeScale.ToString();

        //Select object
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
	    {
	        RaycastHit hit;
	        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	        if (!Physics.Raycast(ray, out hit, 10000))
	        {
	            SpawnObject();
	        }
	        else
	        {
	            Debug.Log("Object Clicked!");
	        }
	    }
        //Show/Hide UI
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (canvasGroup.alpha == 1.0f)
            {

                canvasGroup.alpha = 0.0f;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }
        }
        //Pause
	    if (Input.GetKeyDown(KeyCode.Escape))
	    {
	        if (!pausePanel.activeSelf)
	        {
	            PauseGame();
	        }
            else
	        {
	            ResumeGame();
	        }
	    }
        //Delete
	    if (Input.GetKeyDown(KeyCode.Delete))
	    {
	        Destroy(selectedObject.gameObject);
	    }
	}

    public void SetSkybox(string name)
    {
        if(Skyboxes[name] != null)
            RenderSettings.skybox = Skyboxes[name];
    }

    public void SetSelectedObject(PhysicsObject obj)
    {
        selectedObject = obj;
    }

    public void ToggleNeon(bool val)
    {
        Bloom bloomSettings = Camera.main.GetComponent<PostProcessLayer>().GetSettings<Bloom>();
        if (val)
        {
            bloomSettings.intensity.value = 2.35f;
            bloomSettings.threshold.value = 0.4f;
            //bloomSettings.radius = 4.0f;
            audioVT.isActivated = true;
        }
        else
        {
            audioVT.isActivated = false;
            bloomSettings.intensity.value = 0.0f;
            bloomSettings.threshold.value = 1.0f;
            //bloomSettings.setRadius(0.0f);
            //bloomSettings.lensDirt.intensity = 0;
        }
        //TODO: Do settings need to be set again?
    }

    public void SetObjectToSpawn(string name)
    {
        objectToSpawn = (GameObject)CelestialObjects[name];
        //Set colour picker UI to reflect trail colour
        colPicker.Color = objectToSpawn.GetComponentInChildren<TrailRenderer>().startColor;
        //reset desired trail colour
        desiredTrailColor = colPicker.Color;
    }

    public void TrailColChanged(Color col)
    {
        //Update desired trail colour based on user selection
        desiredTrailColor = col;
    }

    public void SetImgSpawnObj(Image btnImage)
    {
        imgSpawnObj.sprite = btnImage.sprite;
    }


    public void ReloadScene()
    {
        Application.LoadLevel(Application.loadedLevel);
    }

    //Pause
    public void pausePressed()
    {
        physicsEngine.pauseSimulation();
        pauseButton.SetActive(false);
        playButton.SetActive(true);
    }

    //Play
    public void playPressed()
    {
        physicsEngine.resumeSimulation();
        playButton.SetActive(false);
        pauseButton.SetActive(true);
    }

    public void timeScaled(string scale)
    {
        try
        {
            physicsEngine.timeScale = int.Parse(scale);
        }
        catch (ArgumentNullException)
        {
        }
        catch (FormatException)
        {
        }
        catch (OverflowException)
        {
        }
    }

    public void OrbVMultiplierChanged(float val)
    {
        orbVMultiplier = val;
    }

    public void trailsToggled(bool state)
    {
        if(state)
            Camera.main.cullingMask = Camera.main.cullingMask | (1 << 8);
        else
            Camera.main.cullingMask = Camera.main.cullingMask & ~(1 << 8);
    }

    public void ManipModeToggled(int mode)
    {
        manipMode = mode;
    }

    public void SpawnWithOrbitToggled(bool state)
    {
        foreach (Object item in CelestialObjects.Values)
        {
            ((GameObject)item).GetComponent<PhysicsObject>().spawnWithOrbit = state;
        }
    }

    public void SwitchPanels(int id)
    {
        if (id == 0)
        {
            if (activePanel != starPanel)
            {
                activePanel.SetActive(false);
                starPanel.SetActive(true);
                activePanel = starPanel;
            }
        }
        else if (id == 1)
        {
            if (activePanel != planetPanel)
            {
                activePanel.SetActive(false);
                planetPanel.SetActive(true);
                activePanel = planetPanel;
            }
        }
        else if (id == 2)
        {
            if (activePanel != othersPanel)
            {
                activePanel.SetActive(false);
                othersPanel.SetActive(true);
                activePanel = othersPanel;
            }
        }
    }

    void SpawnObject()
    {
        if (objectToSpawn != null)
        { 
            
            Debug.Log("Object Spawn Start!");
            // Get mouse position on screen
            Vector3 screenPosition = Input.mousePosition;

            // Raycast into screen looking for placement plane
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            // Check if ray hit, if so, get hitpoint
            float rayLength = 0.0f;
            Vector3 hitPoint = new Vector3();
            if(placementGrid.plane.Raycast(ray, out rayLength))
            {
                hitPoint = ray.GetPoint(rayLength);
            }
            else
            {
                Debug.Log("Ray did not hit placement plane.");
                return;
            }

            // Spawn object
            GameObject SpawnedObj = Instantiate(objectToSpawn);
            SpawnedObj.transform.position = hitPoint;

            // Set Trail colour based on UI selection
            TrailRenderer tR = SpawnedObj.GetComponentInChildren<TrailRenderer>();
            tR.startColor = desiredTrailColor;
            tR.endColor = desiredTrailColor;
            // Ensure alpha value is 0
            tR.endColor = new Vector4(tR.endColor.r, tR.endColor.g, tR.endColor.b, 0.0f);

        }
    }

    public void SwitchTab(int val)
    {
        //If Obejcts Tab Pressed
        if (val == 0)
        {
            //Toggle Object Panels
            panObjects.SetActive(!panObjects.activeSelf);
            panBrush.SetActive(!panBrush.activeSelf);
            panSpawn.SetActive(!panSpawn.activeSelf);
            panObject.SetActive(!panObject.activeSelf);
            //Set Scene Panels to off
            panSettings.SetActive(false);
            panBackgrounds.SetActive(false);

            if (panObject.activeSelf)
            {
                //Highlight Active Tab
                ColorBlock colBlock = ColorBlock.defaultColorBlock;
                colBlock.colorMultiplier = 1.5f;
                tabObj.colors = colBlock;
            }
            else
            {
                //Highlight Active Tab
                ColorBlock colBlock = ColorBlock.defaultColorBlock;
                colBlock.colorMultiplier = 1.0f;
                tabObj.colors = colBlock;
            }
            //Remove Highlight from other Tab
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.colorMultiplier = 1.0f;
            tabScene.colors = cb;

        }
        //If Scene Tab Pressed
        if (val == 1)
        {
            //Toggle Scene Panels
            panSettings.SetActive(!panBackgrounds.activeSelf);
            panBackgrounds.SetActive(!panBackgrounds.activeSelf);
            //Set Object Panels to Off
            panObjects.SetActive(false);
            panBrush.SetActive(false);
            panSpawn.SetActive(false);
            panObject.SetActive(false);

            if (panBackgrounds.activeSelf)
            {
                //Highlight Active Tab
                ColorBlock colBlock = ColorBlock.defaultColorBlock;
                colBlock.colorMultiplier = 1.5f;
                tabScene.colors = colBlock;
            }
            else
            {
                //Remove Highlight
                ColorBlock colBlock = ColorBlock.defaultColorBlock;
                colBlock.colorMultiplier = 1.0f;
                tabScene.colors = colBlock;
            }
            //Remove Highlight from other Tab
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.colorMultiplier = 1.0f;
            tabObj.colors = cb;
        }
    }

    public void LockToggled(Toggle tgl)
    {
        if (selectedObject != null)
        {
            if (tgl.name == "tglMassLock")
            {
                selectedObject.massLocked = tgl.isOn;
                inptMassVal.interactable = !tgl.isOn;
            }
            else if (tgl.name == "tglDensityLock")
            {
                selectedObject.densityLocked = tgl.isOn;
                inptDensityVal.interactable = !tgl.isOn;
            }
            else if (tgl.name == "tglRadiusLock")
            {
                selectedObject.radiusLocked = tgl.isOn;
                inptRadiusVal.interactable = !tgl.isOn;
            }
        }
    }

    public void toggleSymetry(bool state)
    {
        spawnSymetry = state;
        inptDivs.interactable = state;
    }

    public void DivsChanged()
    {
        int result;
        if (int.TryParse(inptDivs.text, out result))
        {
            if(result > 1 && result < 100)
            {
                symDivs = result;
            }
            else
            {
                inptDivs.text = symDivs.ToString();
            }
        }
        else
        {
            inptDivs.text = symDivs.ToString();
        }
    }

    public void finEditingMass(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            selectedObject.setMass(valResult);
        }
    }
    public void finEditingRadius(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            selectedObject.setRadius(valResult);
        }
    }
    public void finEditingDensity(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            selectedObject.setDensity(valResult);
        }
    }

    public void finEditingPosX(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            Vector3 objPos = selectedObject.transform.position;
            selectedObject.transform.position = new Vector3(valResult, objPos.y, objPos.z);
        }
    }
    public void finEditingPosY(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            Vector3 objPos = selectedObject.transform.position;
            selectedObject.transform.position = new Vector3(objPos.x, valResult, objPos.z);
        }
    }
    public void finEditingPosZ(string val)
    {
        float valResult;
        if (float.TryParse(val, out valResult))
        {
            Vector3 objPos = selectedObject.transform.position;
            selectedObject.transform.position = new Vector3(objPos.x, objPos.y, valResult);
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        physicsEngine.pauseSimulation();
    }

    public void ResumeGame()
    {
       // if(pausePanel == null)
        //    pausePanel = GameObject.Find("panPause").gameObject;
        pausePanel.SetActive(false);
        physicsEngine.resumeSimulation();
    }

    public void Quit()
    {
        Application.Quit();
    }
}
