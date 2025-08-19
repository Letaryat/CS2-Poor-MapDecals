using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Poor_MapDecals.Managers;

public class EventManager(CS2_Poor_MapDecals plugin)
{
    private readonly CS2_Poor_MapDecals _plugin = plugin;
    private bool isPluginReady = false;
    private bool isMapReady = false;
    public void RegisterEvents()
    {
        //Events:
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        _plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        _plugin.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
        //Listeners:
        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>((ResourceManifest manifest) =>
        {
            foreach (var prop in _plugin.Config.Props)
            {
                manifest.AddResource(prop);
            }
        });
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        //_plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);


        _plugin.AddCommandListener("changelevel", ListenerChangeLevel, HookMode.Pre);
        _plugin.AddCommandListener("map", ListenerChangeLevel, HookMode.Pre);
        _plugin.AddCommandListener("host_workshop_map", ListenerChangeLevel, HookMode.Pre);
        _plugin.AddCommandListener("ds_workshop_changelevel", ListenerChangeLevel, HookMode.Pre);

    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (isPluginReady)
        {
            isPluginReady = false;
        }
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (isPluginReady) return HookResult.Continue;
        if (!isMapReady)
        {
            isMapReady = true;
            _plugin.PropManager!.LoadPropsFromMap();
        }
        _plugin.DebugMode("Roundstart. Trying to spawn decals");

        var gamerules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        if (gamerules == null)
        {
            _plugin.DebugMode("No Gamerules. Ending");
            return HookResult.Continue;
        }
        if (!gamerules.WarmupPeriod)
        {
            try
            {
                isPluginReady = true;
                Server.NextFrameAsync(() =>
                {
                    _plugin.PropManager!.SpawnProps();
                });
            }
            catch (Exception error)
            {
                _plugin.DebugMode($"WarmupPeriod Error: {error}");
            }

        }
        else
        {
            _plugin.DebugMode("Not spawning - Warmup!");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        var ping = @event;
        var player = ping.Userid;
        if (player == null) return HookResult.Continue;

        if (!_plugin.AllowAdminCommands || !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag) || !_plugin.PingPlacement)
        {
            return HookResult.Continue;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return HookResult.Continue;


        _plugin.PluginUtils!.CreateDecalOnClick(pawn, new Vector(ping.X, ping.Y, ping.Z), _plugin.DecalWidth, _plugin.DecalHeight, _plugin.ForceOnVip);

        return HookResult.Continue;
    }


    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        var allAdvs = Utilities.FindAllEntitiesByDesignerName<CEnvDecal>("env_decal");
        if (allAdvs == null || !allAdvs.Any()) return;
        try
        {
            foreach (var entry in infoList)
            {
                CCheckTransmitInfo info;
                CCSPlayerController? player;

                try
                {
                    (info, player) = ((CCheckTransmitInfo, CCSPlayerController))entry;
                }
                catch
                {
                    continue;
                }

                if (player == null) continue;

                if (AdminManager.PlayerHasPermissions(player, _plugin.Config.VipFlag))
                {
                    foreach (var ad in allAdvs)
                    {
                        //if (ad?.Entity?.Name != null && ad.Entity.Name.Contains("advert"))
                        if (ad.Entity!.Name == null) continue;
                        if (ad!.Entity!.Name.StartsWith("advert") && !ad!.Entity!.Name.Contains("force"))
                        {
                            info.TransmitEntities.Remove(ad);
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"CheckTransmit: ${error}");
        }

    }


    private void ClearingCache()
    {
        if (_plugin.PropManager == null) return;
        if (_plugin.PropManager._props.Count() > 0)
        {
            _plugin.PropManager!._props.Clear();
        }
        _plugin.AllowAdminCommands = false;
        _plugin.PingPlacement = false;
        _plugin.DecalAdToPlace = 0;
    }

    private void OnMapStart(string mapName)
    {
        
        if (_plugin.PropManager == null) return;
        try
        {

            _plugin.PropManager._mapName = mapName;
            _plugin.PropManager!._mapFilePath = Path.Combine(_plugin.ModuleDirectory, "maps", $"{mapName}.json");

            _plugin.PropManager.GenerateJsonFile();
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"[ERROR] OnMapStart {error}");
        }

        isPluginReady = false;

    }

    private HookResult ListenerChangeLevel(CCSPlayerController? player, CommandInfo commandInfo)
    {
        try
        {
            ClearingCache();
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"[ERROR] Clearing cache on ListenereChangeLevel: ${error}");
        }

        return HookResult.Continue;
    }


}
