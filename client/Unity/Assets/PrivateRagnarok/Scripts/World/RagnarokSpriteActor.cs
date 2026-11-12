using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Displays a licensed/imported 8-direction sprite set. It never downloads or embeds game art.</summary>
    [DisallowMultipleComponent]
    public sealed class RagnarokSpriteActor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        private RagnarokSpriteDefinition definition;
        private Camera viewCamera;

        public void Initialize(RagnarokSpriteDefinition spriteDefinition, Camera camera = null)
        {
            definition = spriteDefinition;
            viewCamera = camera != null ? camera : Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (definition == null) return;

            if (definition.animatorController != null)
            {
                if (animator == null) animator = gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = definition.animatorController;
            }
            ApplyFacingSprite();
        }

        private void LateUpdate()
        {
            if (definition == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            ApplyFacingSprite();
        }

        private void ApplyFacingSprite()
        {
            var sprites = definition.directionalSprites;
            if (sprites == null || sprites.Length == 0 || spriteRenderer == null) return;
            var direction = 0;
            if (viewCamera != null)
            {
                var toCamera = viewCamera.transform.position - transform.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    var angle = Vector3.SignedAngle(transform.forward, toCamera.normalized, Vector3.up);
                    direction = Mathf.RoundToInt(angle / 45f);
                    direction = (direction % 8 + 8) % 8;
                }
            }
            if (sprites.Length == 1)
            {
                if (sprites[0] != null) spriteRenderer.sprite = sprites[0];
            }
            else if (sprites.Length >= 8 && sprites[direction] != null)
            {
                spriteRenderer.sprite = sprites[direction];
            }
            else
            {
                for (var i = 0; i < sprites.Length; i++)
                    if (sprites[i] != null) { spriteRenderer.sprite = sprites[i]; break; }
            }
        }
    }
}
