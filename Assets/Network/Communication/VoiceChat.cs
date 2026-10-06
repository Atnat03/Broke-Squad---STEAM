using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : NetworkBehaviour
{
    [Header("Voice Chat")]
    [SerializeField] private bool _isProximityChat = false;

    [Header("Audio")]
    [SerializeField] private int sampleRate = 16000;

    [SerializeField]
    private int jitterBufferMs = 200;

    // 10 ms de voix à 16 kHz = 160 samples = 640 octets.
    private const int PacketSamples = 160;

    private const int Channels = 1;

    // Buffer de sortie : 1 seconde.
    private int bufferSize;

    private float[] audioBuffer;

    private int writePosition;
    private int readPosition;
    private int bufferedSamples;

    private readonly object bufferLock = new object();

    private AudioSource audioSource;

    // Microphone
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private int lastSamplePosition;

    // Buffer d'envoi réutilisé.
    private readonly float[] sendSamples = new float[PacketSamples];

    // 160 float = 640 bytes.
    private readonly byte[] sendBytes =
        new byte[PacketSamples * sizeof(float)];

    // Buffer jitter.
    private int jitterBufferSamples;

    private bool playbackStarted;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        bufferSize = sampleRate;

        audioBuffer = new float[bufferSize];

        jitterBufferSamples =
            Mathf.Clamp(
                sampleRate * jitterBufferMs / 1000,
                PacketSamples,
                bufferSize / 2
            );
    }

    private void Start()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        audioSource.spatialize = _isProximityChat;
        audioSource.spatialBlend =
            _isProximityChat ? 1f : 0f;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        // Push to talk
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            StartRecording();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            StopRecording();
        }

        if (microphoneClip != null)
        {
            SendMicrophoneData();
        }
    }


    // =========================================================
    // MICROPHONE
    // =========================================================

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

        microphoneClip = Microphone.Start(
            microphoneDevice,
            true,
            1,
            sampleRate
        );

        lastSamplePosition = 0;

        Debug.Log(
            $"Voice recording started : {microphoneDevice}"
        );
    }

    private void StopRecording()
    {
        if (!string.IsNullOrEmpty(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }

        microphoneDevice = null;
        microphoneClip = null;
        lastSamplePosition = 0;

        Debug.Log("Voice recording stopped");
    }


    // =========================================================
    // SEND MICROPHONE
    // =========================================================

    private void SendMicrophoneData()
    {
        if (microphoneClip == null)
            return;

        if (string.IsNullOrEmpty(microphoneDevice))
            return;

        int currentPosition =
            Microphone.GetPosition(microphoneDevice);

        if (currentPosition < 0)
            return;

        int availableSamples;

        if (currentPosition >= lastSamplePosition)
        {
            availableSamples =
                currentPosition - lastSamplePosition;
        }
        else
        {
            availableSamples =
                microphoneClip.samples
                - lastSamplePosition
                + currentPosition;
        }

        // Tant qu'on possède au moins 10 ms de voix.
        while (availableSamples >= PacketSamples)
        {
            ReadMicrophonePacket();

            Buffer.BlockCopy(
                sendSamples,
                0,
                sendBytes,
                0,
                sendBytes.Length
            );

            SendVoiceServerRpc(
                sendBytes,
                default
            );

            lastSamplePosition += PacketSamples;

            if (lastSamplePosition >= microphoneClip.samples)
            {
                lastSamplePosition -= microphoneClip.samples;
            }

            availableSamples -= PacketSamples;
        }
    }


    private void ReadMicrophonePacket()
    {
        // Cas normal : le paquet ne dépasse pas la fin du clip.
        if (lastSamplePosition + PacketSamples
            <= microphoneClip.samples)
        {
            microphoneClip.GetData(
                sendSamples,
                lastSamplePosition
            );

            return;
        }

        // Cas où le microphone boucle.
        int firstPart =
            microphoneClip.samples - lastSamplePosition;

        int secondPart =
            PacketSamples - firstPart;

        float[] first =
            new float[firstPart];

        float[] second =
            new float[secondPart];

        microphoneClip.GetData(
            first,
            lastSamplePosition
        );

        microphoneClip.GetData(
            second,
            0
        );

        Array.Copy(
            first,
            0,
            sendSamples,
            0,
            firstPart
        );

        Array.Copy(
            second,
            0,
            sendSamples,
            firstPart,
            secondPart
        );
    }


    // =========================================================
    // SERVER RPC
    // =========================================================

    [ServerRpc(
        Delivery = RpcDelivery.Unreliable
    )]
    private void SendVoiceServerRpc(
        byte[] data,
        ServerRpcParams rpcParams = default)
    {
        if (NetworkManager == null)
            return;

        foreach (
            ulong clientId
            in NetworkManager.ConnectedClientsIds
        )
        {
            // Ne pas renvoyer la voix à l'émetteur.
            if (clientId == OwnerClientId)
                continue;

            SendVoiceClientRpc(
                data,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds =
                            new[] { clientId }
                    }
                }
            );
        }
    }


    // =========================================================
    // CLIENT RPC
    // =========================================================

    [ClientRpc(
        Delivery = RpcDelivery.Unreliable
    )]
    private void SendVoiceClientRpc(
        byte[] data,
        ClientRpcParams rpcParams = default)
    {
        if (IsOwner)
            return;

        if (data == null)
            return;

        if (data.Length !=
            PacketSamples * sizeof(float))
        {
            Debug.LogWarning(
                $"Voice packet invalide : {data.Length} bytes"
            );

            return;
        }

        float[] samples =
            new float[PacketSamples];

        Buffer.BlockCopy(
            data,
            0,
            samples,
            0,
            data.Length
        );

        AddSamplesToBuffer(samples);
    }


    // =========================================================
    // JITTER BUFFER
    // =========================================================

    private void AddSamplesToBuffer(
        float[] samples)
    {
        lock (bufferLock)
        {
            foreach (float sample in samples)
            {
                // Si le buffer est plein,
                // on supprime le sample le plus ancien.
                if (bufferedSamples >= bufferSize)
                {
                    readPosition =
                        (readPosition + 1)
                        % bufferSize;

                    bufferedSamples--;
                }

                audioBuffer[writePosition] =
                    sample;

                writePosition =
                    (writePosition + 1)
                    % bufferSize;

                bufferedSamples++;
            }

            // On attend d'avoir suffisamment de voix
            // avant de démarrer la lecture.
            if (!playbackStarted &&
                bufferedSamples >= jitterBufferSamples)
            {
                playbackStarted = true;

                StartAudioPlayback();
            }
        }
    }


    // =========================================================
    // AUDIO PLAYBACK
    // =========================================================

    private void StartAudioPlayback()
    {
        if (audioSource.isPlaying)
            return;

        AudioClip clip =
            AudioClip.Create(
                "VoiceOutput",
                sampleRate,
                Channels,
                sampleRate,
                true,
                OnAudioRead
            );

        audioSource.clip = clip;
        audioSource.loop = true;

        audioSource.Play();
    }


    private void OnAudioRead(float[] data)
    {
        lock (bufferLock)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (bufferedSamples > 0)
                {
                    data[i] =
                        audioBuffer[readPosition];

                    readPosition =
                        (readPosition + 1)
                        % bufferSize;

                    bufferedSamples--;
                }
                else
                {
                    // Sous-débit :
                    // silence au lieu de données invalides.
                    data[i] = 0f;
                }
            }
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    public override void OnNetworkDespawn()
    {
        StopRecording();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        lock (bufferLock)
        {
            writePosition = 0;
            readPosition = 0;
            bufferedSamples = 0;
            playbackStarted = false;
        }
    }

    private void OnDestroy()
    {
        StopRecording();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }
}

