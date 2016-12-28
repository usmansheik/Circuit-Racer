using UnityEngine;
using System.Collections;

public class OpponentCarController : MonoBehaviour {
    public Sprite[] carSprites;
    // Use this for initialization

    public void SetCarNumber(int carNum)
    {
        GetComponent<SpriteRenderer>().sprite = carSprites[carNum - 1];
    }
    public void SetCarInformation(float posX, float posY, float velX, float velY, float rotZ)
    {
        transform.position = new Vector3(posX, posY, 0);
        transform.rotation = Quaternion.Euler(0, 0, rotZ);
        // We're going to do nothing with velocity.... for now
    }
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
