using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VFX_Manage
{
    public class PoolVFX : MonoBehaviour
    {
        public string Key { get; private set; }

        private ParticleSystem root;
        private Action<PoolVFX> onReturnToPool;

        public void Init(string key, Action<PoolVFX> onReturnToPool)
        {
            Key = key;
            this.onReturnToPool = onReturnToPool;
            root = GetComponentInChildren<ParticleSystem>();
        }

        public void Play(Vector3 position, Quaternion rotation, float scale)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);

            if(root != null)
            {
                root.Play();
            }

            StartCoroutine(WaitForCompletion());
        }

        private IEnumerator WaitForCompletion()
        {
            while(root != null && root.IsAlive(true))
            {
                yield return null;
            }

            gameObject.SetActive(false);
            onReturnToPool?.Invoke(this);
        }
    }
}
