
using CognitiveServices;
using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using UnityEngine.XR.WSA.WebCam;

public class myInfo : MonoBehaviour
{
    public Text InfoPanel;
    public Text AnalysisPanel;
    public Text ThreatAssessmentPanel;
    public Text DiagnosticPanel;

    private TextTyper InfoPanelTyper;
    private TextTyper AnalysisPanelTyper;
    private TextTyper ThreatAssessmentPanelTyper;
    private TextTyper DiagnosticsPanelTyper;

    PhotoCapture _photoCaptureObject = null;
    IEnumerator coroutine;

    [Header("Number of seconds between snapshots")]
    [Range(1, 60)]
    public int _loopSeconds = 20;

    [Header("Computer Vision Key")]
    [Tooltip("You can find the key in the Azure portal under Keys for your Computer Vision setup")]
    public string _computerVisionKey = "-your key goes here-";

    [Header("Azure endpoint for Computer Vision")]
    [Tooltip("You can find the endpoint in the Azure portal under Overview for your Computer Vision setup")]
    public string _computerVisionEndpoint = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/";

    private const string _facesParameters = "analyze?visualFeatures=Tags,Faces,Description";
    private const string _ocrParameters = "ocr";

    private TextToSpeech textToSpeechManager;

    private void Awake()
    {
        // get the speech manager
        textToSpeechManager = GetComponentInChildren<TextToSpeech>();
    }


    void Start()
    {

 

        // set the 'typers' for the various output
        InfoPanelTyper = InfoPanel.GetComponent<TextTyper>();
        AnalysisPanelTyper = AnalysisPanel.GetComponent<TextTyper>();
        ThreatAssessmentPanelTyper = ThreatAssessmentPanel.GetComponent<TextTyper>();
        DiagnosticsPanelTyper = DiagnosticPanel.GetComponent<TextTyper>();

        
        InfoPanelTyper.TypeText("Connecting to Microsoft AZURE Service");

        // Start the picture taking loop
        StartCoroutine(CoroLoop());
    }

    IEnumerator CoroLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_loopSeconds);
            AnalyzeScene();
        }
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        _photoCaptureObject = captureObject;

        // find the best supported resolution
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters
        {
            hologramOpacity = 0.0f,
            cameraResolutionWidth = cameraResolution.width,
            cameraResolutionHeight = cameraResolution.height,
            pixelFormat = CapturePixelFormat.BGRA32
        };

        // start the capture
        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {       // take the picture
            _photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {       // couldn't take the picture. Show an error
            InfoPanelTyper.TypeText("Cancel");
            DiagnosticsPanelTyper.TypeText("Unable to make a picture", true);
        }
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        _photoCaptureObject.Dispose();
        _photoCaptureObject = null;
    }

    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            // Create our Texture2D for use and set the correct resolution
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            // encode as JPEG to send to cognitiva service api's
            var imageBytes = targetTexture.EncodeToJPG();

            // Get information for the image from cognitive services
            GetTagsAndFaces(imageBytes);
            ReadWords(imageBytes);
        }
        else
        {       // show error
            DiagnosticsPanelTyper.TypeText("Failed take picture.\nError: " + result.hResult);
            InfoPanelTyper.TypeText("Cancel");
        }
        // stop handling the picture
        _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void AnalyzeScene()
    {
        InfoPanelTyper.TypeText("M A K I N G   P I C T U R E");
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    public void GetTagsAndFaces(byte[] image)
    {
        try
        {
            coroutine = RunComputerVision(image);
            StartCoroutine(coroutine);
        }
        catch (Exception ex)
        {
            DiagnosticsPanelTyper.TypeText("Get Tags failed.\n\n" + ex.Message);
            InfoPanelTyper.TypeText("Cancel");
        }
    }

    public void ReadWords(byte[] image)
    {
        try
        {
            coroutine = ReadTextInImage(image);
            StartCoroutine(coroutine);
            DiagnosticsPanelTyper.TypeText("Reading words ...");
        }
        catch (Exception ex)
        {
            DiagnosticsPanelTyper.TypeText("Reading words failed.\n\n" + ex.Message);
            InfoPanelTyper.TypeText("Cancel");
        }
    }

    IEnumerator RunComputerVision(byte[] image)
    {
        var headers = new Dictionary<string, string>()
        {
            { "Ocp-Apim-Subscription-Key", _computerVisionKey},
            { "Content-Type", "application/octet-stream" }
        };

        WWW www = new WWW(getFacesUrl(), image, headers);
        yield return www;

        if (www.error != null && www.error != "")
        {       // on error, show information and return
            InfoPanelTyper.TypeText("Cancel");
            DiagnosticsPanelTyper.TypeText("ERROR: " + www.error);
            yield break;
        }

        try
        {
            var resultObject = JsonUtility.FromJson<AnalysisResult>(www.text);

            // show all the tags returned
            List<string> tags = new List<string>();
            foreach (var tag in resultObject.tags)
            {
                tags.Add(tag.name);
            }
            AnalysisPanelTyper.TypeText("MESSAGE:\n" + string.Join("\n", tags.ToArray()));

            // Read what has been found be CV 
            if (textToSpeechManager != null)
            {
                // Create message
                var msg = string.Format(" {0} ", string.Join("", tags.ToArray()) );

                // Speak message
                textToSpeechManager.StartSpeaking(msg);
                InfoPanelTyper.TypeText("TextToSpeech: OK ");

            }


            // show all the faces with age returned
            List<string> faces = new List<string>();
            foreach (var face in resultObject.faces)
            {
                faces.Add(string.Format("{0} AGE {1}.", face.gender, face.age));
            }
            if (faces.Count > 0)
            {
                InfoPanelTyper.TypeText("M A T C H");
            }
            else
            {
                InfoPanelTyper.TypeText("LOOKING FOR OBJECTS");
            }
            ThreatAssessmentPanelTyper.TypeText("DETAILS: " + string.Join("\n", faces.ToArray()));
        }
        catch (Exception ex)
        {       // show error details in UI
            
            DiagnosticsPanelTyper.TypeText("ANALYSIS:\n" + ex.Message);
        }
    }

    IEnumerator ReadTextInImage(byte[] image)
    {
        var headers = new Dictionary<string, string>()
        {
            { "Ocp-Apim-Subscription-Key", _computerVisionKey },
            { "Content-Type", "application/octet-stream" }
        };

        WWW www = new WWW(getOcrUrl(), image, headers);
        yield return www;

        if (www.error != null && www.error != "")
        {       // on error, show information and return
            
            DiagnosticsPanelTyper.TypeText("ANALYSIS: " + www.error);
            yield break;
        }

        // get the text from the response
        List<string> words = new List<string>();
        var resultsObject = JsonUtility.FromJson<OcrResults>(www.text);
        foreach (var region in resultsObject.regions)
        {
            foreach (var line in region.lines)
            {
                foreach (var word in line.words)
                {
                    words.Add(word.text);
                }
            }
        }

          string textToRead = string.Join(" ", words.ToArray());

          DiagnosticPanel.text = "TEXT TO READ: " + textToRead;

        if (textToRead.Length > 0)
        {   // if there is text, also show the language that was determined
            DiagnosticPanel.text = "(language=" + resultsObject.language + ")\n" + textToRead;
            if (1 == 1) //(resultsObject.language.ToLower() == "en")
            {   // only text to speech if in English (only one supported with local speech engine currently)
                textToSpeechManager.StartSpeaking(textToRead);
            }
        }
        else
        {       // nothing found, so nothing to show
            DiagnosticPanel.text = "NO READABLE TEXT FOUND"; // string.Empty;
        }
    }

    private string getFacesUrl()
    {
        string url = _computerVisionEndpoint;
        if (!url.EndsWith("/")) url += "/";
        url += _facesParameters;
        return url;
    }

    private string getOcrUrl()
    {
        string url = _computerVisionEndpoint;
        if (!url.EndsWith("/")) url += "/";
        url += _ocrParameters;
        return url;
    }
}
