using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PitchDetector;
using UnityEngine.UI;
using System.Linq;
using System;

public class tuner : MonoBehaviour
{ 


    public GameObject aiguille;
    public Text aimString;
    private float angle;
    private float aimFrequency;

    private int minFreq, maxFreq;                       //Max and min frequencies window
    public string selectedDevice { get; private set; }  //Mic selected
    private bool micSelected = false;                   //Mic flag

    private Detector pitchDetector;
    float[] data;                                       //Sound samples data
    public int cumulativeDetections = 5;                //Number of consecutive detections used to determine current note
    int[] detectionsMade;                               //Detections buffer
    private int maxDetectionsAllowed = 50;              //Max buffer size
    private int detectionPointer = 0;                       //Current buffer pointer
    public int pitchTimeInterval = 100;                     //Millisecons needed to detect tone
    private float refValue = 0.1f;                      // RMS value for 0 dB
    public float minVolumeDB = -17f;                        //Min volume in bd needed to start detection

    private int currentDetectedNote = 0;                    //Las note detected (midi number)
    private string currentDetectedNoteName;             //Note name in modern notation (C=Do, D=Re, etc..)

    void Awake()
    {
        pitchDetector = new Detector();
        pitchDetector.setSampleRate(AudioSettings.outputSampleRate);
    }

    //Start function for web player (also works on other platforms)
    IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            selectedDevice = Microphone.devices[0].ToString();
            micSelected = true;
            GetMicCaps();

            //Estimates bufer len, based on pitchTimeInterval value
            int bufferLen = (int)Mathf.Round(AudioSettings.outputSampleRate * pitchTimeInterval / 1000f);
            Debug.Log("Buffer len: " + bufferLen);
            data = new float[bufferLen];

            detectionsMade = new int[maxDetectionsAllowed]; //Allocates detection buffer

            setUptMic();
        }
        else
        {
        }
    }

    // This function will detect the pitch
    void Update()
    {



        if (selectedDevice == null)
            return;
        GetComponent<AudioSource>().GetOutputData(data, 0);
        float sum = 0f;
        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];
        float rmsValue = Mathf.Sqrt(sum / data.Length);
        float dbValue = 20f * Mathf.Log10(rmsValue / refValue);
        if (dbValue < minVolumeDB)
        {
            aimString.text = "String : --";
        }

        //PITCH DTECTED!!
        pitchDetector.DetectPitch(data);
        int midiant = pitchDetector.lastMidiNote();
        int midi = findMode();
        detectionsMade[detectionPointer++] = midiant;
        detectionPointer %= cumulativeDetections;
        float freq = pitchDetector.lastFrequency();

        aimFrequency = getAimFrequency(freq);
        
        angle = (aimFrequency - freq) * 2;
        if (angle > 90)
            angle = 89;
        if (angle < -90)
            angle = -89;


        if (angle < 70 && angle > -70)
        {

            aiguille.transform.eulerAngles = new Vector3(0, 0, angle);
        }
        else
        {

        }
    }

    public float getAimFrequency(float frequency)
    {
        List<float> freqList = new List<float>();
        freqList.Add(196f);
        freqList.Add(293.7f);
        freqList.Add(440f);
        freqList.Add(659.3f);
        float aimFreq = freqList.OrderBy(item => Math.Abs(frequency - item)).First();
        if (aimFreq == 196f)
            aimString.text = "String : G";
        if (aimFreq == 293.7f)
            aimString.text = "String : D";
        if (aimFreq == 440f)
            aimString.text = "String : A";
        if (aimFreq == 659.3f)
            aimString.text = "String : E";


        return aimFreq;
    }

    void setUptMic()
    {
        //GetComponent<AudioSource>().volume = 0f;
        GetComponent<AudioSource>().clip = null;
        GetComponent<AudioSource>().loop = true; // Set the AudioClip to loop
        GetComponent<AudioSource>().mute = false; // Mute the sound, we don't want the player to hear it
        StartMicrophone();
    }

    public void GetMicCaps()
    {
        Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);//Gets the frequency of the device
        if ((minFreq + maxFreq) == 0)
            maxFreq = 44100;
    }

    public int findMode()
    {
        cumulativeDetections = (cumulativeDetections >= maxDetectionsAllowed) ? maxDetectionsAllowed : cumulativeDetections;
        int moda = 0;
        int veces = 0;
        for (int i = 0; i < cumulativeDetections; i++)
        {
            if (repetitions(i) > veces)
                moda = detectionsMade[i];
        }
        return moda;
    }

    public void StartMicrophone()
    {
        Debug.Log("Setting");
        GetComponent<AudioSource>().clip = Microphone.Start(selectedDevice, true, 10, maxFreq);//Starts recording
        while (!(Microphone.GetPosition(selectedDevice) > 0)) { } // Wait until the recording has started
        GetComponent<AudioSource>().Play(); // Play the audio source!
        Debug.Log("End Setting");
    }

    int repetitions(int element)
    {
        int rep = 0;
        int tester = detectionsMade[element];
        for (int i = 0; i < cumulativeDetections; i++)
        {
            if (detectionsMade[i] == tester)
                rep++;
        }
        return rep;
    }
}

