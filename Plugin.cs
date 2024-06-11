using AK;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace FridosSmartPing
{
    [BepInPlugin("com.fridos.smartping", "FridosSmartPing", "1.2.2")]
    public class SmartPing : BasePlugin
    {
        public static List<ItemInLevel> itemList = new();
        public Harmony HarmonyInstance { get; private set; }

        public static void Initialize()
        {
            itemList.Clear();
            Logger.Info("Init SmartPing and preparing.");
            foreach (ItemInLevel item in UnityEngine.Object.FindObjectsOfType<ItemInLevel>())
            {
                if (item != null)
                {
                    LG_GenericTerminalItem component = item.GetComponentInChildren<LG_GenericTerminalItem>();
                    if (component != null)
                    {
                        var name = component.TerminalItemKey;
                        if (name != null)
                        {
                            if (name.Contains("PACK") || name.StartsWith("TOOL_REFILL") ||
                                name.StartsWith("KEY_") || name.StartsWith("PID_") || name.StartsWith("BULKHEAD_KEY_") ||
                                name.StartsWith("DATA_CUBE_") || name.StartsWith("CELL_") || name.StartsWith("FOG_TURBINE"))
                            {
                                itemList.Add(item);
                            }
                        }
                    }
                }
            }
        }
        public override void Load()
        {
            // Plugin startup logic
            Logger.Info("Fridos Smart Ping enabled!");

            HarmonyInstance = new Harmony("com.fridos.smartping");
            HarmonyInstance.PatchAll();
            LG_Factory.OnFactoryBuildDone += (Il2CppSystem.Action)SmartPing.Initialize;
        }
    }

    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.SetupCommands))]
    public class SmartPingPatch
    {
        static void Postfix(LG_ComputerTerminalCommandInterpreter __instance) {
            __instance.AddCommand(TERM_Command.InvalidCommand, "SMARTPING", new LocalizedText
            {
                UntranslatedText = "Ping items inside the current zone",
                Id = 0u
            },TERM_CommandRule.Normal);
        }
    }
    /*[HarmonyPatch(typeof(ItemInLevel), nameof(ItemInLevel.OnPickedUp))]
    public class RemoveNavMarkerPatch
    {
        static void Postfix(ItemInLevel __instance, PlayerAgent player, InventorySlot slot, AmmoType ammoType)
        {
            if(SNet.IsMaster)
            {
                LG_GenericTerminalItem component = __instance.GetComponentInChildren<LG_GenericTerminalItem>();
                if (component != null)
                {
                    var name = component.TerminalItemKey;
                    if (name != null)
                    {
                        foreach (NavMarker nM in GuiManager.NavMarkerLayer.m_markersActive)
                        {
                            if (nM != null)
                            {
                                if (nM.m_title != null && nM.m_title.text != null)
                                {
                                    if (nM.m_title.text.Contains(component.TerminalItemKey))
                                    {
                                        nM.SetVisible(false);
                                        nM.m_currentState = NavMarkerState.Inactive;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }*/
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
    public class SmartPingOverridePatch
    {
            static bool Prefix(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            if (inputLine.Contains("SMARTPING"))
            {
                __instance.m_linesSinceCommand = 0;
                __instance.m_terminal.IsWaitingForAnyKeyInLinePause = false;
                __instance.AddOutput(__instance.NewLineStart() + inputLine, false);
                __instance.AddOutput(__instance.NewLineStart() + "   ______                                      __      _______   __                     ", false);
                __instance.AddOutput(__instance.NewLineStart() + "  /      \\                                    |  \\    |       \\ |  \\                    ", false);
                __instance.AddOutput(__instance.NewLineStart() + " |  $$$$$$\\ ______ ____    ______    ______  _| $$_   | $$$$$$$\\ \\$$ _______    ______  ", false);
                __instance.AddOutput(__instance.NewLineStart() + " | $$___\\$$|      \\    \\  |      \\  /      \\|   $$ \\  | $$__/ $$|  \\|       \\  /      \\ ", false);
                __instance.AddOutput(__instance.NewLineStart() + "  \\$$    \\ | $$$$$$\\$$$$\\  \\$$$$$$\\|  $$$$$$\\\\$$$$$$  | $$    $$| $$| $$$$$$$\\|  $$$$$$\\", false);
                __instance.AddOutput(__instance.NewLineStart() + "  _\\$$$$$$\\| $$ | $$ | $$ /      $$| $$   \\$$ | $$ __ | $$$$$$$ | $$| $$  | $$| $$  | $$", false);
                __instance.AddOutput(__instance.NewLineStart() + " |  \\__| $$| $$ | $$ | $$|  $$$$$$$| $$       | $$|  \\| $$      | $$| $$  | $$| $$__| $$", false);
                __instance.AddOutput(__instance.NewLineStart() + "  \\$$    $$| $$ | $$ | $$ \\$$    $$| $$        \\$$  $$| $$      | $$| $$  | $$ \\$$    $$", false);
                __instance.AddOutput(__instance.NewLineStart() + "   \\$$$$$$  \\$$  \\$$  \\$$  \\$$$$$$$ \\$$         \\$$$$  \\$$       \\$$ \\$$   \\$$ _\\$$$$$$$", false);
                __instance.AddOutput(__instance.NewLineStart() + "                                                                              |  \\__| $$", false);
                __instance.AddOutput(__instance.NewLineStart() + "          by LolBit & Frido                                                    \\$$    $$", false);
                __instance.AddOutput(__instance.NewLineStart() + "                                                                                \\$$$$$$ ", false);
                __instance.AddOutput(__instance.NewLineStart(), false);
                __instance.AddOutput(TerminalLineType.ProgressWait, __instance.NewLineStart() + "Initalizing Fridos Smart Ping™...", 5f, TerminalSoundType.LineTypeDefault, TerminalSoundType.LineTypeDefault);
                __instance.OnEndOfQueue = new Action(delegate ()
                {
                    int pingedItems = 0;
                    foreach (ItemInLevel item in SmartPing.itemList)
                    {
                        if (item.internalSync.GetCurrentState().status != ePickupItemStatus.PickedUp)
                        {
                            var current_zone = "ZONE_" + __instance.m_terminal.SpawnNode.m_zone.NavInfo.Number;
                            var terminalItem = item.GetComponentInChildren<LG_GenericTerminalItem>();
                            var name = terminalItem.TerminalItemKey;
                            try
                            {
                                if (current_zone == terminalItem.FloorItemLocation && item.internalSync.GetCurrentState().status != ePickupItemStatus.PickedUp &&
                                    (name.Contains("PACK") || name.StartsWith("TOOL_REFILL") || name.StartsWith("CARGO_") ||
                                    name.StartsWith("KEY_") || name.StartsWith("PID_") || name.StartsWith("BULKHEAD_KEY_") ||
                                    name.StartsWith("DATA_CUBE_") || name.StartsWith("CELL_") || name.StartsWith("GLP_") ||
                                    name.StartsWith("PD_") || name.StartsWith("OSIP_") || name.StartsWith("HDD_") || name.StartsWith("FOG_TURBINE")))
                                {
                                    NavMarker m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(item.gameObject);
                                    m_marker.PersistentBetweenRestarts = false;
                                    if(item.GetCustomData().ammo >= 20)
                                    {
                                        m_marker.SetTitle(name + "\n(" + item.GetCustomData().ammo / 20 + " Uses)");
                                    } else
                                    {
                                        m_marker.SetTitle(name);
                                    }
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
                                        m_marker.SetStyle(eNavMarkerStyle.PlayerPingLoot);
                                    }
                                    m_marker.SetIconScale(0.4f);
                                    m_marker.SetAlpha(0.4f);
                                    m_marker.SetVisible(true);
                                    m_marker.FadeOutOverTime(20, 10);
                                    m_marker.m_fadeRoutine = CoroutineManager.StartCoroutine(GuiManager.NavMarkerLayer.FadeMarkerOverTime(m_marker, m_marker.name, UnityEngine.Random.Range(0.1f, 0.5f), 30f, false), null);
                                    pingedItems += 1;
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                        }
                    }
                    CellSound.Post(EVENTS.TERMINAL_PING_MARKER_SFX, __instance.m_terminal.transform.position);
                    __instance.AddOutput(__instance.NewLineStart() + "Fridos Smart Ping™ has finished and pinged a total of " + pingedItems + " items.", false);
                });
                return false;
            }
            return true;
        }
    }
}