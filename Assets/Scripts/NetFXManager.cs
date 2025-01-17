using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using LiteNetLib;

public class NetFXManager : MonoBehaviour
{
    public static NetFXManager Instance { get; private set; }

    [Header("SFX Details")]
    [SerializeField] private GameObject sfxPrefab;
    [SerializeField] private string sfxDirectory = "SFX/";

    [Header("VFX Details")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private string vfxDirectory = "VFX/";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("<color=red><b>CNet</b></color>: There are multiple NetFXManager instances in the scene, destroying one.");
            Destroy(this);
        }
    }

    public void PlaySFX(string name, float volume, Vector3? pos = null, bool sync = true)
    {
        if (sync)
        {
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.SFX);
            packet.Write(name);
            packet.Write(volume);
            if (pos != null)
            {
                packet.Write(pos.Value);
            }
            NetManager.Instance.Send(packet, DeliveryMethod.ReliableOrdered);
        }

        if (!sync || NetManager.Instance.IsHost)
        {
            AudioSource newSFX = Instantiate(sfxPrefab).GetComponent<AudioSource>();
            newSFX.clip = Resources.Load<AudioClip>(sfxDirectory + name);

            if (newSFX.clip == null)
            {
                Debug.LogError("<color=red><b>CNet</b></color>: NetFXManager could not find AudioClip with name '" + name + "'");
                return;
            }

            newSFX.volume = volume;

            if (pos != null)
            {
                newSFX.transform.position = (Vector3)pos;
                newSFX.spatialBlend = 1f;
            }

            newSFX.Play();

            Destroy(newSFX.gameObject, newSFX.clip.length);
        }
    }

    public void PlayVFX(string name, Vector3 position, float scale, bool sync = true)
    {
        if (sync)
        {
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.VFX);
            packet.Write(name);
            packet.Write(position);
            packet.Write(scale);
            NetManager.Instance.Send(packet, DeliveryMethod.ReliableOrdered);
        }

        if (!sync || NetManager.Instance.IsHost)
        {
            VisualEffect vfx = Instantiate(vfxPrefab, position, Quaternion.identity).GetComponent<VisualEffect>();

            vfx.visualEffectAsset = Resources.Load<VisualEffectAsset>(vfxDirectory + name);

            if (vfx.visualEffectAsset == null)
            {
                Debug.LogError("<color=red><b>CNet</b></color>: NetFXManager could not find VisualEffectAsset with name '" + name + "'");
                return;
            }

            vfx.transform.localScale = new Vector3(scale, scale, scale);

            if (vfx.HasFloat("_Duration"))
            {
                Destroy(vfx.gameObject, vfx.GetFloat("_Duration"));
            }
            else
            {
                Debug.LogWarning("<color=red><b>CNet</b></color>: NetFXManager could not find a _Duration property for VisualEffectAsset with name '" + name + "', will not be destroyed!");
            }
        }
    }
}