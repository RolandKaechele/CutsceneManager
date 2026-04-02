#if CUTSCENEMANAGER_IM
using UnityEngine;
using InventoryManager.Runtime;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// Optional bridge between CutsceneManager and InventoryManager.
    /// Enable define <c>CUTSCENEMANAGER_IM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Listens to <see cref="CutsceneManager.OnCustomEvent"/> and interprets event payloads
    /// that match the configured prefixes as inventory commands, allowing cutscene authors to
    /// grant, remove, or consume items as part of a cutscene sequence.
    /// </para>
    /// <para><b>Event payload format</b> (<c>customEvent</c> field on a <c>CutsceneStep</c>):</para>
    /// <list type="bullet">
    /// <item><c>"inventory.add:sword"</c>     — adds 1 unit of <c>sword</c></item>
    /// <item><c>"inventory.add:sword:3"</c>   — adds 3 units of <c>sword</c></item>
    /// <item><c>"inventory.remove:sword"</c>  — removes 1 unit of <c>sword</c></item>
    /// <item><c>"inventory.remove:sword:3"</c>— removes 3 units</item>
    /// <item><c>"inventory.use:sword"</c>     — triggers <see cref="InventoryManager.UseItem"/></item>
    /// </list>
    /// <para>The command verbs are configurable in the Inspector.</para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Inventory Cutscene Bridge")]
    [DisallowMultipleComponent]
    public class InventoryCutsceneBridge : MonoBehaviour
    {
        [Tooltip("Event payload verb that adds items to inventory (e.g. \"inventory.add\").")]
        [SerializeField] private string addVerb    = "inventory.add";

        [Tooltip("Event payload verb that removes items from inventory (e.g. \"inventory.remove\").")]
        [SerializeField] private string removeVerb = "inventory.remove";

        [Tooltip("Event payload verb that uses an item (e.g. \"inventory.use\").")]
        [SerializeField] private string useVerb    = "inventory.use";

        private CutsceneManager _cutscene;
        private InventoryManager.Runtime.InventoryManager _inventory;

        private void Awake()
        {
            _cutscene  = GetComponent<CutsceneManager>() ?? FindFirstObjectByType<CutsceneManager>();
            _inventory = GetComponent<InventoryManager.Runtime.InventoryManager>()
                         ?? FindFirstObjectByType<InventoryManager.Runtime.InventoryManager>();

            if (_cutscene  == null) Debug.LogWarning("[InventoryCutsceneBridge] CutsceneManager not found.");
            if (_inventory == null) Debug.LogWarning("[InventoryCutsceneBridge] InventoryManager not found.");
        }

        private void OnEnable()
        {
            if (_cutscene != null) _cutscene.OnCustomEvent += OnCustomEvent;
        }

        private void OnDisable()
        {
            if (_cutscene != null) _cutscene.OnCustomEvent -= OnCustomEvent;
        }

        private void OnCustomEvent(string sequenceId, string eventData)
        {
            if (string.IsNullOrEmpty(eventData) || _inventory == null)
                return;

            if (TryParseItemCommand(eventData, addVerb, out string addId, out int addQty))
            {
                _inventory.AddItem(addId, addQty);
                return;
            }

            if (TryParseItemCommand(eventData, removeVerb, out string removeId, out int removeQty))
            {
                _inventory.RemoveItem(removeId, removeQty);
                return;
            }

            int useVerbLen = useVerb.Length;
            if (eventData.StartsWith(useVerb) &&
                eventData.Length > useVerbLen + 1 && eventData[useVerbLen] == ':')
            {
                string itemId = eventData.Substring(useVerbLen + 1).Trim();
                if (!string.IsNullOrEmpty(itemId))
                    _inventory.UseItem(itemId);
            }
        }

        /// <summary>Parses <c>"verb:itemId"</c> or <c>"verb:itemId:qty"</c>.</summary>
        private static bool TryParseItemCommand(string payload, string verb,
                                                out string itemId, out int qty)
        {
            itemId = null;
            qty    = 1;

            if (string.IsNullOrEmpty(verb)) return false;
            if (!payload.StartsWith(verb))  return false;

            int verbLen = verb.Length;
            if (payload.Length <= verbLen + 1 || payload[verbLen] != ':')
                return false;

            string rest           = payload.Substring(verbLen + 1);
            int    lastColon      = rest.LastIndexOf(':');

            if (lastColon >= 0 && int.TryParse(rest.Substring(lastColon + 1), out int parsed))
            {
                itemId = rest.Substring(0, lastColon).Trim();
                qty    = parsed;
            }
            else
            {
                itemId = rest.Trim();
                qty    = 1;
            }

            return !string.IsNullOrEmpty(itemId);
        }
    }
}
#else
namespace CutsceneManager.Runtime
{
    /// <summary>No-op stub. Enable CUTSCENEMANAGER_IM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("CutsceneManager/Inventory Cutscene Bridge")]
    public class InventoryCutsceneBridge : UnityEngine.MonoBehaviour
    {
        private void Awake() =>
            UnityEngine.Debug.Log("[InventoryCutsceneBridge] InventoryManager integration is disabled. " +
                                  "Add the scripting define CUTSCENEMANAGER_IM to enable it.");
    }
}
#endif
