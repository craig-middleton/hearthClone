using UnityEngine;

namespace HearthstoneClone.UI
{
    // The burst owns this runtime material, including natural stop and interrupted effects.
    public sealed class SpellBurstMaterialOwner : MonoBehaviour
    {
        internal Material material;

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
