using UnityEngine;
using System.Collections;
using GooglePlayGames;
using GooglePlayGames.BasicApi.Multiplayer;
using System;
using System.Collections.Generic;
public class MultiplayerController : RealTimeMultiplayerListener
{
    private int _finishMessageLength = 6;
    private int _myMessageNum;

    public MPUpdateListener updateListener;

    private byte _protocolVersion = 2;
    // Byte + Byte + 2 floats for position + 2 floats for velcocity + 1 float for rotZ
    private int _updateMessageLength = 26;
    private List<byte> _updateMessage;

    private uint minimumOpponents = 1;
    private uint maximumOpponents = 1;
    private uint gameVariation = 0;
    public MPLobbyListener lobbyListener;
    private static MultiplayerController _instance = null;
    public List<Participant> GetAllPlayers()
    {
        return PlayGamesPlatform.Instance.RealTime.GetConnectedParticipants();
    }
    public string GetMyParticipantId()
    {
        return PlayGamesPlatform.Instance.RealTime.GetSelf().ParticipantId;
    }
    private void StartMatchMaking()
    {
        PlayGamesPlatform.Instance.RealTime.CreateQuickGame(minimumOpponents, maximumOpponents, gameVariation, this);
    }
    private MultiplayerController()
    {
        _updateMessage = new List<byte>(_updateMessageLength);
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

    }
    public void SendMyUpdate(float posX, float posY, Vector2 velocity, float rotZ)
    {
        _updateMessage.Clear();
        _updateMessage.Add(_protocolVersion);
        _updateMessage.Add((byte)'U');
        _updateMessage.AddRange(System.BitConverter.GetBytes(++_myMessageNum)); // THIS IS THE NEW LINE

        _updateMessage.AddRange(System.BitConverter.GetBytes(posX));
        _updateMessage.AddRange(System.BitConverter.GetBytes(posY));
        _updateMessage.AddRange(System.BitConverter.GetBytes(velocity.x));
        _updateMessage.AddRange(System.BitConverter.GetBytes(velocity.y));
        _updateMessage.AddRange(System.BitConverter.GetBytes(rotZ));
        byte[] messageToSend = _updateMessage.ToArray();
        Debug.Log("Sending my update message  " + messageToSend + " to all players in the room");
        PlayGamesPlatform.Instance.RealTime.SendMessageToAll(false, messageToSend);
    }
    public void SendFinishMessage(float totalTime)
    {
        List<byte> bytes = new List<byte>(_finishMessageLength);
        bytes.Add(_protocolVersion);
        bytes.Add((byte)'F');
        bytes.AddRange(System.BitConverter.GetBytes(totalTime));
        byte[] messageToSend = bytes.ToArray();
        PlayGamesPlatform.Instance.RealTime.SendMessageToAll(true, messageToSend);
    }
    public void SignInAndStartMPGame()
    {
        if (!PlayGamesPlatform.Instance.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.localUser.Authenticate((bool success) => {
                if (success)
                {
                    Debug.Log("We're signed in! Welcome " + PlayGamesPlatform.Instance.localUser.userName);
                    StartMatchMaking();
                }
                else
                {
                    Debug.Log("Oh... we're not signed in.");
                }
            });
        }
        else
        {
            Debug.Log("You're already signed in.");
            // We could also start our game now
        }
    }
    public void SignOut()
    {
        PlayGamesPlatform.Instance.SignOut();
    }

    public bool IsAuthenticated()
    {
        return PlayGamesPlatform.Instance.localUser.authenticated;
    }
    public void TrySilentSignIn()
    {
        if (!PlayGamesPlatform.Instance.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.Authenticate((bool success) => {
                if (success)
                {
                    Debug.Log("Silently signed in! Welcome " + PlayGamesPlatform.Instance.localUser.userName);
                }
                else
                {
                    Debug.Log("Oh... we're not signed in.");
                }
            }, true);
        }
        else
        {
            Debug.Log("We're already signed in");
        }
    }
    private void ShowMPStatus(string message)
    {
        Debug.Log(message);
        if (lobbyListener != null)
        {
            lobbyListener.SetLobbyStatusMessage(message);
        }
    }
    public void OnRoomSetupProgress(float percent)
    {
        ShowMPStatus("We are " + percent + "% done with setup");
    }

   

    public void OnRoomConnected(bool success)
    {
        if (success)
        {
            lobbyListener.HideLobby();
            lobbyListener = null;
            _myMessageNum = 0;

            Application.LoadLevel("MainGame");
        }
        else
        {
            ShowMPStatus("Uh-oh. Encountered some error connecting to the room.");
        }
    }

    public void OnLeftRoom()
    {
        ShowMPStatus("We have left the room.");
        if (updateListener != null)
        {
            updateListener.LeftRoomConfirmed();
        }
    }

    public void OnParticipantLeft(Participant participant)
    {
        throw new NotImplementedException();
    }

    public void OnPeersConnected(string[] participantIds)
    {
        foreach (string participantID in participantIds)
        {
            ShowMPStatus("Player " + participantID + " has joined.");
        }
    }

    public void OnPeersDisconnected(string[] participantIds)
    {
        foreach (string participantID in participantIds)
        {
            ShowMPStatus("Player " + participantID + " has left.");
            if (updateListener != null)
            {
                updateListener.PlayerLeftRoom(participantID);
            }
        }
    }

    public void OnRealTimeMessageReceived(bool isReliable, string senderId, byte[] data)
    {
        // We'll be doing more with this later...
        byte messageVersion = (byte)data[0];
        if (messageVersion < _protocolVersion)
        {
            // Our opponent seems to be out of date.
            Debug.Log("Our opponent is using an older client.");
            return;
        }
        else if (messageVersion > _protocolVersion)
        {
            // Our opponents seem to be using a newer client version than our own! 
            // In a real game, we might want to prompt the user to update their game.
            Debug.Log("Our opponent has a newer client!");
            return;
        }
        // Let's figure out what type of message this is.
        char messageType = (char)data[1];
        if (messageType == 'U' && data.Length == _updateMessageLength)
        {
            int messageNum = System.BitConverter.ToInt32(data, 2);
            float posX = System.BitConverter.ToSingle(data, 6);
            float posY = System.BitConverter.ToSingle(data, 10);
            float velX = System.BitConverter.ToSingle(data, 14);
            float velY = System.BitConverter.ToSingle(data, 18);
            float rotZ = System.BitConverter.ToSingle(data, 22);

            if (updateListener != null)
            {
                updateListener.UpdateReceived(senderId, messageNum, posX, posY, velX, velY, rotZ);
            }
        }
        else if (messageType == 'F' && data.Length == _finishMessageLength)
        {
            // We received a final time!
            float finalTime = System.BitConverter.ToSingle(data, 2);
            Debug.Log("Player " + senderId + " has finished with a time of " + finalTime);
        }


    }
    public void LeaveGame()
    {
        PlayGamesPlatform.Instance.RealTime.LeaveRoom();
    }

    public static MultiplayerController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MultiplayerController();
            }
            return _instance;
        }
    }

}
