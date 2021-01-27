using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogTest : MonoBehaviour {
    public float updateTime = 2.0f;
    float currTime;

	// Use this for initialization
	void Start () {
        currTime = updateTime;
        GLog.StartSession(version: "V1.0");
    }

    void TriggerLog() {
        GLog.Debug("Test");
        GLog.Info("Test");
        GLog.Performance("Test");
        GLog.Subsystem("Test");
        GLog.Warning("Test");
        GLog.Info("Test");
        GLog.Error("Test");
        GLog.Player("Test");
        GLog.Event("Test");
    }

    // Update is called once per frame
    void Update () {
        currTime -= Time.deltaTime;
        if (currTime < 0) {
            currTime += updateTime;
            TriggerLog();
        }
	}
}
