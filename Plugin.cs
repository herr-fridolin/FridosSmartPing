using AK;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace FridosSmartPing
{
    [BepInPlugin("com.fridos.smartping", "FridosSmartPing", "1.1.0")]
    public class SmartPing : BasePlugin
    {
        public static List<LG_GenericTerminalItem> itemList = new List<LG_GenericTerminalItem>();
        public static List<LG_GenericTerminalItem> pickedItemList = new List<LG_GenericTerminalItem>();
        public Harmony HarmonyInstance { get; private set; }

        public static void Initialize()
        {
            itemList.Clear();
            pickedItemList.Clear();
            Logger.Error("Init SmartPing and preparing.");
            foreach (LG_GenericTerminalItem item in UnityEngine.Object.FindObjectsOfType<LG_GenericTerminalItem>())
            {
                iTerminalItem component = item.GetComponent<iTerminalItem>();
                if (item != null)
                {
                    var name = item.TerminalItemKey;
                    if (name != null)
                    {
                        if (name.Contains("PACK") || name.StartsWith("TOOL_REFILL") ||
                            name.StartsWith("KEY_") || name.StartsWith("PID_") || name.StartsWith("BULKHEAD_KEY_") ||
                            name.StartsWith("DATA_CUBE_") || name.StartsWith("CELL_"))
                        {
                            itemList.Add(item);
                        }
                    }
                }
            }
        }
        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo("Fridos Smart Ping enabled!");

            HarmonyInstance = new Harmony("com.fridos.smartping");
            HarmonyInstance.PatchAll();
            LG_Factory.OnFactoryBuildDone += (Il2CppSystem.Action)SmartPing.Initialize;
        }
    }

    [HarmonyPatch(typeof(LG_PickupItem_Sync), nameof(LG_PickupItem_Sync.AttemptInteract))]
    public class OnPickupDeleteItemPatch
    {
        static void Prefix(LG_PickupItem_Sync __instance, pPickupItemInteraction interaction)
        {
            //SYNC LISTS
            if (interaction.type.ToString() == "Pickup") //IGNORE QUEST ITEMS
            {
                LG_GenericTerminalItem itemToRemove = new LG_GenericTerminalItem();
                bool foundItem = false;
                foreach (LG_GenericTerminalItem item in SmartPing.itemList)
                {
                    if (__instance.name.Contains(item.TerminalItemKey))
                    {
                        itemToRemove = item;
                        foundItem = true;
                        break;
                    }
                }
                if (foundItem)
                {
                    SmartPing.pickedItemList.Add(itemToRemove);
                    foreach(NavMarker nM in GuiManager.NavMarkerLayer.m_markersActive)
                    {
                        if (nM != null)
                        {
                            if (nM.m_title != null && nM.m_title.text != null)
                            {
                                if (__instance.name.Contains(nM.m_title.text) && !__instance.name.Contains("CELL"))
                                {
                                    nM.SetVisible(false);
                                    break;
                                }
                            }
                        }
                    }
                    SmartPing.itemList.Remove(itemToRemove);
                }
            }
            else if (interaction.type.ToString() == "Place")
            {
                bool foundItem = false;
                LG_GenericTerminalItem itemToRemove = new LG_GenericTerminalItem();
                foreach (LG_GenericTerminalItem item in SmartPing.pickedItemList)
                {
                    if (__instance.name.Contains(item.TerminalItemKey))
                    {
                        itemToRemove = item;
                        foundItem = true;
                        SmartPing.itemList.Add(item);
                    }
                }
                if (foundItem)
                {
                    SmartPing.pickedItemList.Remove(itemToRemove);
                }
            }
        }
    }
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.SetupCommands))]
    public class SmartPingPatch
    {
        static void Postfix(LG_ComputerTerminalCommandInterpreter __instance) {
            __instance.AddCommand(TERM_Command.InvalidCommand, "SMARTPING", new LocalizedText
            {
                UntranslatedText = "Ping an item inside the current zone to get its location",
                Id = 0u
            },TERM_CommandRule.Normal);
        }
    }
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
    public class SmartPingOverridePatch
    {
        static bool Prefix(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            if (inputLine.Contains("SMARTPING"))
            {
                __instance.m_linesSinceCommand = 0;
                __instance.m_terminal.IsWaitingForAnyKeyInLinePause = false;

                __instance.AddOutput("", true);
                __instance.AddOutput(TerminalLineType.ProgressWait, "Initalizing Fridos Smart Ping™...", 5f, TerminalSoundType.LineTypeDefault, TerminalSoundType.LineTypeDefault);
                __instance.OnEndOfQueue = new Action(delegate ()
                {
                    int pingedItems = 0;
                    foreach (LG_GenericTerminalItem item in SmartPing.itemList)
                    {
                        Logger.Error("Getting Zone");
                        var current_zone = "ZONE_" + __instance.m_terminal.SpawnNode.m_zone.NavInfo.Number;
                        Logger.Error("Getting Key");
                        var name = item.TerminalItemKey;
                        Logger.Error("Cheking"); 
                        if (current_zone == item.FloorItemLocation && (name.Contains("PACK") || name.StartsWith("TOOL_REFILL") || 
                            name.StartsWith("KEY_") || name.StartsWith("PID_") || name.StartsWith("BULKHEAD_KEY_") ||
                            name.StartsWith("DATA_CUBE_") || name.StartsWith("CELL_") || name.StartsWith("GLP_") || 
                            name.StartsWith("PD_") || name.StartsWith("OSIP_") || name.StartsWith("HDD_")) && !item.WasCollected)
                        {
                            CellSound.Post(EVENTS.TERMINAL_PING_MARKER_SFX, item.transform.position);
                            Logger.Error("Getting Marker");
                            NavMarker m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(item.gameObject);
                            Logger.Error("Marker made.");
                            m_marker.PersistentBetweenRestarts = false;
                            m_marker.SetTitle(name);
                            Logger.Error("Getting asdasd");
                            if (name.StartsWith("MEDIPACK"))
                            {
                                m_marker.SetStyle(eNavMarkerStyle.PlayerPingHealth);
                            }
                            else if (name.StartsWith("AMMOPACK"))
                            {
                                m_marker.SetStyle(eNavMarkerStyle.PlayerPingAmmo);
                            }
                            else if (name.StartsWith("TOOL_REFILL"))
                            {
                                m_marker.SetStyle(eNavMarkerStyle.PlayerPingToolRefill);
                            }
                            else if (name.StartsWith("DISINFECT_PACK"))
                            {
                                m_marker.SetStyle(eNavMarkerStyle.PlayerPingDisinfection);
                            }
                            else
                            {
                                m_marker.SetStyle(eNavMarkerStyle.TerminalPing);
                            }
                            m_marker.SetIconScale(0.4f);
                            m_marker.SetAlpha(0.4f);
                            m_marker.SetVisible(true);
                            m_marker.FadeOutOverTime(20, 10);
                            m_marker.m_fadeRoutine = CoroutineManager.StartCoroutine(GuiManager.NavMarkerLayer.FadeMarkerOverTime(m_marker, m_marker.name, UnityEngine.Random.Range(0.1f, 0.5f), 30f, false), null);
                            pingedItems += 1;
                        }
                    }
                    __instance.AddOutput("Fridos Smart Ping™ has finished and pinged a total of " + pingedItems + " items.", false);
                    __instance.AddOutput(__instance.NewLineStart() + inputLine, false);
                });
                return false;
            }
            return true;
        }
    }
}