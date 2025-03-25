using UnityEngine;

public class AvatarHeadSync : MonoBehaviour
{
    public Transform vrHead; // VR 头显的 Transform
    public Transform avatarHead; // Avatar 头部的 Transform

    void Update()
    {
        avatarHead.position = vrHead.position;
        avatarHead.rotation = vrHead.rotation;
    }
}