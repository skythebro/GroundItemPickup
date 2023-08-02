using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using HarmonyLib;

namespace GroundItemPickup;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("gg.deca.Bloodstone")]
public class Plugin : BasePlugin, IRunOnInitialized
{
    
    public static Harmony _harmony;
    public static ManualLogSource LogInstance { get; private set; }

    public override void Load()
    {
        LogInstance = Log;

        if (!VWorld.IsServer)
        {
            Log.LogWarning("This plugin is a server-only plugin!");
        }
        
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loading!");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        return true;
    }

    public void OnGameInitialized()
    {
        if (VWorld.IsClient)
        {
            return;
        }
        
        Log.LogInfo("Looking if VCF is installed:");
        if (VCFCompat.Commands.Enabled)
        {
            VCFCompat.Commands.Register();
        }
        else
        {
            Log.LogInfo("This mod has addItem command. You need to install VampireCommandFramework to use it.");
        }
        
        // Harmony patching
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        
        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

       
    }
}
