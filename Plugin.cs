using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LevelGeneration;
using Localization;
using AK;

namespace FridosSmartPing
{
    [BepInPlugin("com.fridos.smartping", "FridosSmartPing", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public Harmony HarmonyInstance { get; private set; }

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo("Smart Ping enabled!");

            HarmonyInstance = new Harmony("com.fridos.smartping");
            HarmonyInstance.PatchAll();
        }
    }
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.SetupCommands))]
    public class SmartPingPatch
    {
        static void Postfix(LG_ComputerTerminalCommandInterpreter __instance) {
            //__instance.m_commandsPerString.Add("smartping", TERM_Command.InvalidCommand);
            //__instance.m_commandHelpStrings.Add(TERM_Command.InvalidCommand, new LocalizedText
            //{
            //    UntranslatedText = "Ping an item inside the current zone to get its location",
            //    Id = 0u
            //});
            //__instance.m_commandEventMap.Add(tm, "smartping");
            //__instance.m_commandPostOutputMap.Add(tm, "smartping");
            //__instance.m_commandsPerEnum.Add(TERM_Command.InvalidCommand, "smartping");
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
                __instance.AddOutput(__instance.NewLineStart() + inputLine, false);
                foreach (LG_GenericTerminalItem item in UnityEngine.Object.FindObjectsOfType<LG_GenericTerminalItem>())
                {
                    iTerminalItem component = item.GetComponent<iTerminalItem>();
                    var current_zone = "ZONE_" + __instance.m_terminal.SpawnNode.m_zone.NavInfo.Number;
                    if (current_zone == component.FloorItemLocation && (item.TerminalItemKey.Contains("PACK") || item.TerminalItemKey.StartsWith("TOOL_REFILL") ||
                        item.TerminalItemKey.StartsWith("KEY_") || item.TerminalItemKey.StartsWith("PID_") || item.TerminalItemKey.StartsWith("BULKHEAD_KEY_") ||
                        item.TerminalItemKey.StartsWith("DATA_CUBE_") || item.TerminalItemKey.StartsWith("CELL_")) && !component.WasCollected)
                    {
                        CellSound.Post(EVENTS.TERMINAL_PING_MARKER_SFX, item.transform.position);
                        NavMarker m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(item.gameObject);
                        m_marker.PersistentBetweenRestarts = false;
                        m_marker.SetTitle(item.TerminalItemKey);
                        if (item.TerminalItemKey.StartsWith("MEDIPACK"))
                        {
                            m_marker.SetStyle(eNavMarkerStyle.PlayerPingHealth);
                        }
                        else if (item.TerminalItemKey.StartsWith("AMMOPACK"))
                        {
                            m_marker.SetStyle(eNavMarkerStyle.PlayerPingAmmo);
                        }
                        else if (item.TerminalItemKey.StartsWith("TOOL_REFILL"))
                        {
                            m_marker.SetStyle(eNavMarkerStyle.PlayerPingToolRefill);
                        }
                        else if (item.TerminalItemKey.StartsWith("DISINFECT_PACK"))
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
                        m_marker.FadeOutOverTime(10, 10);
                        m_marker.m_fadeRoutine = CoroutineManager.StartCoroutine(GuiManager.NavMarkerLayer.FadeMarkerOverTime(m_marker, m_marker.name, UnityEngine.Random.Range(0.1f, 0.5f), 30f, false), null);
                    }
                }
                return false;
            }
            return true;
        }
    }
}