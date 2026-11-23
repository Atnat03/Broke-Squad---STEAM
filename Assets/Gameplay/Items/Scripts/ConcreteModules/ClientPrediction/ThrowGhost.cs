using System;
using System.Collections.Generic;
using Bus;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class ThrowGhost : MonoBehaviour
    {
        //Ghost life variables
        private const float ClaimTimeout = 2.5f;
        private const float MaxLifeTime = 4f;
        private const float BlendDuration = 0.2f;
        private const float SettledSpeed = 0.05f;

        private static readonly List<ThrowGhost> Alive = new();

        //Physic ghost variables
        private Rigidbody _rb;
        private float _age;
        private Action _onUnclaimed;

        //Real Item
        private ItemPickup _real;
        private bool _claimed;
        private Vector3 _lastRealPos;
        private readonly List<Renderer> _hiddenRenderers = new();
        private readonly List<Collider> _hiddenColliders = new();

        //Final Blend
        private bool _blending;
        private float _blendT;
        private Vector3 _blendFromPos;
        private Quaternion _blendFromRot;

        #region Creation

        public static void Create(ItemPickup prefab, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Collider[] ignoredColliders, Action onUnclaimed = null)
        {
            //On Instancie un holder vide et pour pas faire proc les Awake, OnEnable sur sur ItemPickup, on le desactive
            
            GameObject holder = new GameObject("ThrowGhostHolder");
            holder.SetActive(false);
            
            GameObject go = Instantiate(prefab.gameObject, position, rotation, holder.transform);

            //On supprime tout les composant en dehors des collider et rigidbody et visuels
            MonoBehaviour[] comps = go.GetComponentsInChildren<MonoBehaviour>(true);
            
            for (int i = comps.Length - 1; i >= 0; i--)
            {
                if (comps[i] is NetworkBehaviour || comps[i] is MonoBusListener)
                    DestroyImmediate(comps[i]);
            }
            DestroyImmediate(go.GetComponent<NetworkObject>());

            //Maintenant que le ghost est vide de logique, on peut l'enlever du parent.
            go.transform.SetParent(null, true);
            Destroy(holder);
            
            if (!go.TryGetComponent(out Rigidbody rb))
            {
                Destroy(go);
                onUnclaimed?.Invoke();
                return;
            }
            
            //On set le rigidbody correctement pour qu'il colle avec ItemPickup
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.position = position;
            rb.rotation = rotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            //On ajoute ce script sur le nouvel item
            ThrowGhost ghost = go.AddComponent<ThrowGhost>();
            ghost._rb = rb;
            
            //On s'abonne a la delegate passé en parametre une fois la claim reclamé
            ghost._onUnclaimed = onUnclaimed;
            Alive.Add(ghost);
        }

        #endregion

        #region Rencontre avec le vrai item

        //Une fois que le vrai ItemPickup a ete recu par le client, on appele son ghost pour vérifier son état
        public static bool TryClaim(ItemPickup real)
        {
            foreach (ThrowGhost ghost in Alive)
            {
                if (ghost._claimed) continue;

                ghost.Claim(real);
                return true;
            }
            return false;
        }

        private void Claim(ItemPickup real)
        {
            _claimed = true;
            _real = real;
            _lastRealPos = real.transform.position;

            foreach (Renderer r in real.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    _hiddenRenderers.Add(r);
                }
            }

            foreach (Collider c in real.GetComponentsInChildren<Collider>())
            {
                if (c.enabled)
                {
                    c.enabled = false;
                    _hiddenColliders.Add(c);
                }
            }
        }

        private void RevealReal()
        {
            foreach (Renderer r in _hiddenRenderers) 
                if (r != null) 
                    r.enabled = true;
            
            foreach (Collider c in _hiddenColliders)
                if (c != null) 
                    c.enabled = true;
            _hiddenRenderers.Clear();
            _hiddenColliders.Clear();
        }

        #endregion

        #region Vie du fantôme
        
        private void Update()
        {
            _age += Time.deltaTime;

            //Pas de ItemPickup recu => roll back dans la main du joueur (sécurité)
            if (!_claimed)
            {
                if (_age > ClaimTimeout)
                {
                    _onUnclaimed?.Invoke();
                    Destroy(gameObject);
                }
                return;
            }

            //Le vrai item a etait Détruit ou pickup => On détruit aussi le ghost
            if (_real == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!_blending)
            {
                Vector3 realPos = _real.transform.position;
                float realSpeed = (realPos - _lastRealPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                _lastRealPos = realPos;

                //Si le ghost ou le vrai ItemPickup sont presque immobile ou que le temps de vie max du ghost est depassé => On échange
                bool bothSettled = _rb.IsSleeping() && realSpeed < SettledSpeed;
                if (bothSettled || _age > MaxLifeTime)
                    BeginBlend();
                
                return;
            }

            //Une fois qu'on a commencé à blend => Blend entre la position du ghost et celle du vrai ItemPickup
            _blendT += Time.deltaTime / BlendDuration;
            float t = Mathf.SmoothStep(0f, 1f, _blendT);
            transform.SetPositionAndRotation(Vector3.Lerp(_blendFromPos, _real.transform.position, t), Quaternion.Slerp(_blendFromRot, _real.transform.rotation, t));

            //Une fois au bout du blend => on détruit le ghost
            if (_blendT >= 1f)
                Destroy(gameObject);
        }

        private void BeginBlend()
        {
            _blending = true;
            
            _blendFromPos = transform.position;
            _blendFromRot = transform.rotation;
            
            _rb.isKinematic = true;
        }

        private void OnDestroy()
        {
            Alive.Remove(this);
            RevealReal();
        }

        #endregion
    }
}