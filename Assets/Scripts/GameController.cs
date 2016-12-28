using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames.BasicApi.Multiplayer;
using System;

public class GameController : MonoBehaviour, MPUpdateListener
{


    public GameObject opponentPrefab;

    private bool _multiplayerReady;
    private string _myParticipantId;
    private Vector2 _startingPoint = new Vector2(0.09675431f, -1.752321f);
    private float _startingPointYOffset = 0.2f;
    private Dictionary<string, OpponentCarController> _opponentScripts;

    public GameObject myCar;
	public GuiController guiObject;
	public GUISkin guiSkin;
	public GameObject background;
	public Sprite[] backgroundSprites;
	public float[] startTimesPerLevel;
	public int[] lapsPerLevel;

	public bool _paused;
	private float _timeLeft;
	private float _timePlayed;
	private int _lapsRemaining;
	private bool _showingGameOver;
	private bool _multiplayerGame;
	private string gameOvertext;
	private float _nextCarAngleTarget = Mathf.PI;
	private const float FINISH_TARGET = Mathf.PI;

	// Use this for initialization
	void Start () {
		RetainedUserPicksScript userPicksScript = RetainedUserPicksScript.Instance;
		_multiplayerGame = userPicksScript.multiplayerGame;
		if (! _multiplayerGame) {
			// Can we get the car number from the previous menu?
			myCar.GetComponent<CarController>().SetCarChoice(userPicksScript.carSelected, false);
			// Set the background
			background.GetComponent<SpriteRenderer>().sprite = backgroundSprites[userPicksScript.diffSelected ];
			// Set our time left and laps remaining
			_timeLeft = startTimesPerLevel[userPicksScript.diffSelected ];
			_lapsRemaining = lapsPerLevel[userPicksScript.diffSelected ];

			guiObject.SetTime (_timeLeft);
			guiObject.SetLaps (_lapsRemaining);
		} else {
            SetupMultiplayerGame();
		}

	}
    public void UpdateReceived(string senderId, float posX, float posY, float velX, float velY, float rotZ)
    {
        if (_multiplayerReady)
        {
            OpponentCarController opponent = _opponentScripts[senderId];
            if (opponent != null)
            {
                opponent.SetCarInformation(posX, posY, velX, velY, rotZ);
            }
        }
    }

    void SetupMultiplayerGame() {
        // 1
        MultiplayerController.Instance.updateListener = this;

        _myParticipantId = MultiplayerController.Instance.GetMyParticipantId();
        // 2
        List<Participant> allPlayers = MultiplayerController.Instance.GetAllPlayers();
        _opponentScripts = new Dictionary<string, OpponentCarController>(allPlayers.Count - 1);
        for (int i = 0; i < allPlayers.Count; i++)
        {
            string nextParticipantId = allPlayers[i].ParticipantId;
            Debug.Log("Setting up car for " + nextParticipantId);
            // 3
            Vector3 carStartPoint = new Vector3(_startingPoint.x, _startingPoint.y + (i * _startingPointYOffset), 0);
            if (nextParticipantId == _myParticipantId)
            {
                // 4
                myCar.GetComponent<CarController>().SetCarChoice(i + 1, true);
                myCar.transform.position = carStartPoint;
            }
            else
            {
                // 5
                GameObject opponentCar = (Instantiate(opponentPrefab, carStartPoint, Quaternion.identity) as GameObject);
                OpponentCarController opponentScript = opponentCar.GetComponent<OpponentCarController>();
                opponentScript.SetCarNumber(i + 1);
                // 6
                _opponentScripts[nextParticipantId] = opponentScript;
            }
        }
        // 7
        _lapsRemaining = 3;
        _timePlayed = 0;
        guiObject.SetLaps(_lapsRemaining);
        guiObject.SetTime(_timePlayed);
        _multiplayerReady = true;
    }


	void PauseGame() {
		_paused = true;
		myCar.GetComponent<CarController>().SetPaused(true);
	}
	
	void ShowGameOver(bool didWin) {
		gameOvertext = (didWin) ? "Woo hoo! You win!" : "Awww... better luck next time";
		PauseGame ();
		_showingGameOver = true;
		Invoke ("StartNewGame", 3.0f);
	}

	void StartNewGame() {
		Application.LoadLevel ("MainMenu");
	}

	void OnGUI() {
		if (_showingGameOver) {
			GUI.skin = guiSkin;
			GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f), gameOvertext);

		}
	}
    
    
    void DoMultiplayerUpdate() {
        // In a multiplayer game, time counts up!
        _timePlayed += Time.deltaTime;
        guiObject.SetTime(_timePlayed);

        MultiplayerController.Instance.SendMyUpdate(myCar.transform.position.x,
                                            myCar.transform.position.y,
                                            myCar.GetComponent<Rigidbody2D>().velocity,
                                            myCar.transform.rotation.eulerAngles.z);

    }
	
	void Update () {
		if (_paused) {
			return;
		}

		if (_multiplayerGame) {
            DoMultiplayerUpdate();
		} else {
			_timeLeft -= Time.deltaTime;
			guiObject.SetTime (_timeLeft);
			if (_timeLeft <= 0) {
				ShowGameOver (false);
			}
		}

		float carAngle = Mathf.Atan2 (myCar.transform.position.y, myCar.transform.position.x) + Mathf.PI;
		if (carAngle >= _nextCarAngleTarget && (carAngle - _nextCarAngleTarget) < Mathf.PI / 4) {
			_nextCarAngleTarget += Mathf.PI / 2;
			if (Mathf.Approximately(_nextCarAngleTarget, 2*Mathf.PI)) _nextCarAngleTarget = 0;
			if (Mathf.Approximately(_nextCarAngleTarget, FINISH_TARGET)) {
				_lapsRemaining -= 1;
				Debug.Log("Next lap finished!");
				guiObject.SetLaps (_lapsRemaining);
				myCar.GetComponent<CarController>().PlaySoundForLapFinished();
				if (_lapsRemaining <= 0) {
					if (_multiplayerGame) {
						// TODO: Properly finish a multiplayer game
					} else {
						ShowGameOver(true);
					}
				}
			}
		}

	}

  
}
