using Unity.Netcode.Components;
using UnityEngine;

public class NetWorkAnimController : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
