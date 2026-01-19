using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Holoi.Library.ARUX
{
    public class ARRaycastPlacmentVisualController : MonoBehaviour
    {
        [SerializeField] Texture2D _trueTexture;
        [SerializeField] Texture2D _falseTexture;

        Animator _animator;
        VisualEffect _vfx;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _vfx = GetComponent<VisualEffect>();
        }

        public void OnHit()
        {
            _vfx.SetBool("IsHit", true);
            _vfx.SetTexture("MainTex", _trueTexture);
        }
        public void OnNotHit()
        {
            _vfx.SetBool("IsHit", false);
            _vfx.SetTexture("MainTex", _falseTexture);
        }

        public void PlayAnimationDeath()
        {
            _animator.SetTrigger("Die");
        }

        // animation events
        public void DisableAfterDieAnimationOver(AnimationEvent animationEvent)
        {
            gameObject.SetActive(false);
        }
    }
}