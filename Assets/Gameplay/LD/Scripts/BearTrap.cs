using Bus;
using Gameplay.PlayerData;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Gameplay.LD.Scripts
{
    [RequireComponent(typeof(NetworkObject))]
    public class BearTrap : NetworkBusListener, IInteractable
    {
        [SerializeField] private int damage;

        [Tooltip("Temps en secondes avant que le piège se déclenche après qu'un joueur soit entré dans la zone de détection.")]
        [SerializeField] private float triggerTime;
        [SerializeField] private Animator triggerAnimator;
        [SerializeField] private string triggerStateName = "Trigger";

        [SerializeField] private float interactionRange = 4f;

        [Header("Visuals")]
        [SerializeField] private Material visualArmed;
        [SerializeField] private Material visualDisarmed;
        [SerializeField] private List<GameObject> meshs = new();

        private readonly NetworkVariable<bool> isArmed = new(true);
        private Coroutine triggerCoroutine;
        private bool isCountingDown;
        private readonly List<Renderer> renderers = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            isArmed.OnValueChanged += OnArmedChanged;
            CacheRenderers();
            ApplyVisualTrap(isArmed.Value);
        }

        public override void OnNetworkDespawn()
        {
            isArmed.OnValueChanged -= OnArmedChanged;
            CancelCountDown();
            base.OnNetworkDespawn();
        }

        private void OnArmedChanged(bool previous, bool current)
        {
            ApplyVisualTrap(current);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !isArmed.Value || isCountingDown) return;

            PlayerData player = other.GetComponentInParent<PlayerData>();
            if (player == null) return;

            triggerCoroutine = StartCoroutine(TriggerTrapCoroutine(player));
        }

        private IEnumerator TriggerTrapCoroutine(PlayerData player)
        {
            isCountingDown = true;

            yield return new WaitForSeconds(triggerTime);

            isCountingDown = false;
            triggerCoroutine = null;

            if (!isArmed.Value) yield break;

            isArmed.Value = false;

            if (player != null) player.TakeDamage(damage);

            SnapClientRpc();
        }

        private void CancelCountDown()
        {
            if (triggerCoroutine != null)
                StopCoroutine(triggerCoroutine);

            triggerCoroutine = null;
            isCountingDown = false;
        }

        // Touche E
        public void Interact() => ToggleArmedServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleArmedServerRpc(RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client)) return;

            NetworkObject playerNetworkObject = client.PlayerObject;
            if (playerNetworkObject == null) return;

            float distanceToTrap = Vector3.Distance(playerNetworkObject.transform.position, transform.position);
            if (distanceToTrap > interactionRange) return;

            bool willBeArmed = !isArmed.Value;

            if (!willBeArmed)
                CancelCountDown(); // désamorcé : plus de dégâts prévus

            isArmed.Value = willBeArmed;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SnapClientRpc()
        {
            //triggerAnimator.Play(triggerStateName, 0);
            // TODO : Ici pour sfx + vfx
        }

        private void CacheRenderers()
        {
            renderers.Clear();

            foreach (GameObject mesh in meshs)
            {
                if (mesh == null) continue;

                Renderer meshRenderer = mesh.GetComponent<Renderer>();
                if (meshRenderer == null)
                {
                    Debug.LogWarning($"BearTrap: '{mesh.name}' n'a pas de Renderer", mesh);
                    continue;
                }

                renderers.Add(meshRenderer);
            }
        }

        private void ApplyVisualTrap(bool armed)
        {
            Material material = armed ? visualArmed : visualDisarmed;
            if (material == null) return;

            foreach (Renderer meshRenderer in renderers)
                meshRenderer.sharedMaterial = material;

            // ouvert si amorcé / fermé sinon, voir avec l'animator
        }
    }
}