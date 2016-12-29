﻿using UnityEngine;
using System.Collections;

public class OpponentCarController : MonoBehaviour {
    private Vector3 _lastKnownVel;
    private Vector3 _startPos;
    private Vector3 _destinationPos;
    private Quaternion _startRot;
    private Quaternion _destinationRot;
    private float _lastUpdateTime;

    public float lastUpdateTime { get { return _lastUpdateTime; } }

    private float _timePerUpdate = 0.16f;
    private int _lastMessageNum;

    public Sprite[] carSprites;
    // Use this for initialization

    public void SetCarNumber(int carNum)
    {
        GetComponent<SpriteRenderer>().sprite = carSprites[carNum - 1];
    }
    public void SetCarInformation(int messageNum, float posX, float posY, float velX, float velY, float rotZ)
    {
        if (messageNum <= _lastMessageNum)
        {
            // Discard any out of order messages
            return;
        }
        _lastMessageNum = messageNum;
        // 1
        _startPos = transform.position;
        _startRot = transform.rotation;
        // 2
        _destinationPos = new Vector3(posX, posY, 0);
        _destinationRot = Quaternion.Euler(0, 0, rotZ);
        //3
        _lastKnownVel = new Vector3(velX, velY, 0);
        _lastUpdateTime = Time.time;
    }
    void Start () {
        _lastUpdateTime = Time.time;
        _lastMessageNum = 0;

        _startPos = transform.position;
        _startRot = transform.rotation;
    }
    public void HideCar()
    {
        gameObject.GetComponent<Renderer>().enabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        // 1
        float pctDone = (Time.time - _lastUpdateTime) / _timePerUpdate;

        if (pctDone <= 1.0)
        {
            // 2
            transform.position = Vector3.Lerp(_startPos, _destinationPos, pctDone);
            transform.rotation = Quaternion.Slerp(_startRot, _destinationRot, pctDone);
        }
        else
        {
            // Guess where we might be
            transform.position = transform.position + (_lastKnownVel * Time.deltaTime);
        }
    }
}
