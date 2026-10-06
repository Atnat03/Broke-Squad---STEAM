using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : NetworkBehaviour
{
    [SerializeField] private bool _isProximityChat = false;
    
    private AudioSource audioSource;
    
    private AudioClip microphoneClip;
    private string microphoneDevice;

    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int PacketSamples = 320;

    private int lastSamplePosition;

    private const int BufferSize = SampleRate * 2;

    private float[] audioBuffer;
    private int writePosition;
    private int readPosition;
    private int bufferedSamples;

    private void Awake()
    {
        audioBuffer = new float[BufferSize];
        
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        
        audioSource.spatialize = _isProximityChat;
        audioSource.spatialBlend = _isProximityChat ? 1 : 0;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
            StartRecording();

        if (Input.GetKeyUp(KeyCode.Tab))
            StopRecording();

        if (microphoneClip != null)
            SendMicrophoneData();
    }

    private void StartRecording()
    {
        if (microphoneClip != null)
            return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("Aucun microphone trouvé.");
            return;
        }

        microphoneDevice = Microphone.devices[0];

        microphoneClip = Microphone.Start(microphoneDevice, true, 1, SampleRate);

        lastSamplePosition = 0;

        Debug.Log("Recording started : " + microphoneDevice);
    }

    private void StopRecording()
    {
        if (microphoneDevice != null)
        {
            Microphone.End(microphoneDevice);
            microphoneDevice = null;
        }

        microphoneClip = null;

        Debug.Log("Recording stopped");
    }

    private void SendMicrophoneData()
    {
        int currentPosition = Microphone.GetPosition(microphoneDevice);

        if (currentPosition < 0)
            return;

        int availableSamples;

        if (currentPosition >= lastSamplePosition)
        {
            availableSamples = currentPosition - lastSamplePosition;
        }
        else
        {
            availableSamples = microphoneClip.samples - lastSamplePosition + currentPosition;
        }

        if (availableSamples < PacketSamples)
            return;

        float[] samples = new float[PacketSamples];

        if (lastSamplePosition + PacketSamples <= microphoneClip.samples)
        {
            microphoneClip.GetData(samples, lastSamplePosition);
        }
        else
        {
            int firstPart = microphoneClip.samples - lastSamplePosition;

            int secondPart = PacketSamples - firstPart;

            float[] first = new float[firstPart];

            microphoneClip.GetData(first, lastSamplePosition);

            Array.Copy(first, 0, samples, 0, firstPart);

            float[] second = new float[secondPart];

            microphoneClip.GetData(second, 0);

            Array.Copy(second, 0, samples, firstPart, secondPart);
        }

        lastSamplePosition += PacketSamples;

        if (lastSamplePosition >= microphoneClip.samples)
        {
            lastSamplePosition -= microphoneClip.samples;
        }

        byte[] data = new byte[samples.Length * sizeof(float)];

        Buffer.BlockCopy(samples, 0, data, 0, data.Length);

        SendVoiceServerRpc(data);
    }

    [ServerRpc]
    private void SendVoiceServerRpc(byte[] data)
    {
        List<ulong> targetClients = new List<ulong>();

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId != OwnerClientId)
            {
                targetClients.Add(clientId);
            }
        }

        if (targetClients.Count == 0)
            return;

        SendVoiceClientRpc(data, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClients.ToArray()
                }
            }
        );
    }

    [ClientRpc]
    private void SendVoiceClientRpc(byte[] data, ClientRpcParams rpcParams = default)
    {
        float[] samples = new float[data.Length / sizeof(float)];

        Buffer.BlockCopy(data, 0, samples, 0, data.Length
        );

        AddSamplesToBuffer(samples);
    }

    private void AddSamplesToBuffer(float[] samples)
    {
        foreach (float sample in samples)
        {
            if (bufferedSamples >= BufferSize)
                break;

            audioBuffer[writePosition] = sample;

            writePosition++;

            if (writePosition >= BufferSize)
                writePosition = 0;

            bufferedSamples++;
        }

        if (!audioSource.isPlaying)
        {
            StartAudioPlayback();
        }
    }

    private void StartAudioPlayback()
    {
        AudioClip clip = AudioClip.Create("VoiceOutput", BufferSize, Channels, SampleRate, true, OnAudioRead);

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void OnAudioRead(
        float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (bufferedSamples > 0)
            {
                data[i] = audioBuffer[readPosition];

                readPosition++;

                if (readPosition >= BufferSize)
                    readPosition = 0;

                bufferedSamples--;
            }
            else
            {
                data[i] = 0f;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (microphoneDevice != null)
        {
            Microphone.End(microphoneDevice);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }
}