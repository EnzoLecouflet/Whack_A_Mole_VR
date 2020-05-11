﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public class SampleLogger : MonoBehaviour
{

    [SerializeField]
    private string fileName = "log";
    [SerializeField]
    private string savePath = "";
    private string completeFileName = "";
    private string filePath;
    private char fieldSeperator = ',';

    [SerializeField]
    float samplingFrequency = 0.02f;

    [SerializeField]
    private PupilLabs.TimeSync timeSync;

    private TrackerHub trackerHub = new TrackerHub();

    private Dictionary<string, Dictionary<int, string>> logs = new Dictionary<string, Dictionary<int, string>>();

    private int logCount = 0;

    // Start is called before the first frame update
    void Awake()
    {
        logs.Add("Framecount", new Dictionary<int, string>());
        logs.Add("TimeStamp", new Dictionary<int, string>());
        logs.Add("Time", new Dictionary<int, string>());
        logs.Add("Date", new Dictionary<int, string>());
        logs.Add("Event", new Dictionary<int, string>());
        logs.Add("PupilTime", new Dictionary<int, string>());
        logs.Add("UnityToPupilTimeOffset", new Dictionary<int, string>());

        trackerHub.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Initialises the CSV file parameters (name and file path).
    private void InitFile()
    {
        completeFileName = fileName + "_" + GetTimeStamp().Replace('/', '-').Replace(":", "-");
        filePath = savePath + "/" + completeFileName + ".csv";
    }

    // If game state updated event is raised (by the gameDirector), updates the UI accordingly.
    public void OnGameDirectorStateUpdate(GameDirector.GameState newState)
    {
        Debug.Log("Called");
        switch(newState)
        {
            case GameDirector.GameState.Stopped:
                FinishLogging();
                break;
            case GameDirector.GameState.Playing:
                StartLogging();
                break;
            case GameDirector.GameState.Paused:
                // TODO
                break;
        }
    }

    public void StartLogging() {
        trackerHub.StartTrackers();
        InitFile();
        StartCoroutine("SampleLog", samplingFrequency);
    }

    public void FinishLogging() {
        trackerHub.StopTrackers();
        StopCoroutine("SampleLog");
        SaveCsvLogs();
        ResetLogs();
    }

    // Generates a "logs" row (see class description) from the given datas. Adds mandatory parameters and 
    // the PersistentEvents parameters to the row when generating it.
    private IEnumerator SampleLog(float sampleFreq)
    {
        while (true) {
            logs["Framecount"].Add(logCount, Time.frameCount.ToString());
            logs["TimeStamp"].Add(logCount, GetTimeStamp());
            logs["Date"].Add(logCount, System.DateTime.Now.ToString("yyyy-MM-dd"));
            logs["Event"].Add(logCount, "Sample");
            logs["Time"].Add(logCount, System.DateTime.Now.ToString("HH:mm:ss.ffff"));
            logs["PupilTime"].Add(logCount, timeSync != null ? timeSync.GetPupilTimestamp().ToString() : "NULL");
            logs["UnityToPupilTimeOffset"].Add(logCount, timeSync != null ? timeSync.UnityToPupilTimeOffset.ToString() : "NULL");

            // Adds the parameters of the objects tracked by the TrackerHub's trackers
            Dictionary<string, object> trackedLogs = trackerHub.GetTracks();

            foreach (KeyValuePair<string, object> pair in trackedLogs)
            {
                if (logs.ContainsKey(pair.Key))
                {
                    logs[pair.Key].Add(logCount, ConvertToString(pair.Value));
                }
                else
                {
                    logs.Add(pair.Key, new Dictionary<int, string>{{logCount, ConvertToString(pair.Value)}});
                }
            }

            logCount++;
            yield return new WaitForSeconds(sampleFreq);
        }
    }

    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private string ConvertToString(object arg)
    {
        if (arg is float)
        {
            return ((float)arg).ToString("0.0000").Replace(",", ".");
        }
        else if (arg is Vector3)
        {
            return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        else
        {
            return arg.ToString();
        }
    }

    // Returns a time stamp including the milliseconds.
    private string GetTimeStamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // UpdateHeadersAndDefaults).
    private void SaveCsvLogs()
    {

        GenerateHeaders();
        string temp;
        for (int i = 0; i < logCount; i++)
        {
            string line = "";
            foreach (KeyValuePair<string, Dictionary<int, string>> log in logs)
            {
                if (line != "")
                {
                    line += fieldSeperator;
                }

                if (log.Value.TryGetValue(i, out temp))
                {
                    line += temp;
                }
                else
                {
                    line += "NULL";
                }
            }
            SaveToFile(line);
        }
    }


    // Generates the headers in a CSV format and saves them to the CSV file
    private void GenerateHeaders()
    {
        string headers = "";
        foreach (string key in logs.Keys)
        {
            if (headers != "")
            {
                headers += fieldSeperator;
            }
            headers += key;
        }
        SaveToFile(headers);
    }

    // Saves the given CSV line to the CSV file.
    private void SaveToFile(string line, bool end = true)
    {
        string tempLine = line;

        if (end)
        {
            tempLine += Environment.NewLine;
        }
        File.AppendAllText(filePath, tempLine);
    }

    // Clears the logs, "Current Mole" log, log count and unique test ID. Used to clear the logs when a new game is started.
    private void ResetLogs()
    {
        
        foreach(Dictionary<int, string> dict in logs.Values)
        {
            dict.Clear();
        }
        logCount = 0;
    }

}