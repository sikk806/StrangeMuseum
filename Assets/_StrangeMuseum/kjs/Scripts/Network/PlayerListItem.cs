using System;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    //public Zone
    public string PlayerName;
    public int ConnectionID;
    public ulong PlayerSteamID;

    public TMP_Text PlayerNameText;
    public RawImage PlayerIcon;
    public TMP_Text PlayerReadyText;
    public bool Ready;

    // private Zone
    private bool AvatarReceived;

    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    public void ChangeReadyStatus()
    {
        if(Ready)
        {
            PlayerReadyText.text = "준비";
            PlayerReadyText.color = Color.green;
        }
        else
        {
            PlayerReadyText.text = "대기중..";
            PlayerReadyText.color = Color.gray;
        }
    }

    private void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if(callback.m_steamID.m_SteamID == PlayerSteamID)
        {
            PlayerIcon.texture = GetSteamImageAsTexture(callback.m_iImage);
        }
        else
        {
            return;
        }
    }

    public void SetPlayerValues()
    {
        PlayerNameText.text = PlayerName;
        ChangeReadyStatus();
        if(!AvatarReceived) { GetPlayerIcon(); }
    }

    // Steam Player Icon
    void GetPlayerIcon()
    {
        int ImageID = SteamFriends.GetLargeFriendAvatar((CSteamID)PlayerSteamID);
        if(ImageID == -1)
        {
            Debug.Log("No ImageId");
            return;
        }

        Debug.Log("Check The ImageId");

        PlayerIcon.texture = GetSteamImageAsTexture(ImageID);
    }

    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if(isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if(isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }

        AvatarReceived = true;
        return texture;
    }
}
