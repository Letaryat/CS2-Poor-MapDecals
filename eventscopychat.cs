using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Poor_MapDecals.Managers;

public class EventManager(CS2_Poor_MapDecals plugin)
{
    private readonly CS2_Poor_MapDecals _plugin = plugin;
    private bool spawnDecals = false;

    public void RegisterEvents()
    {
        //Events:
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        _plugin.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
        _plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>(OnMatchStart);
        _plugin.RegisterEventHandler<EventGameEnd>(OnGameEnd, HookMode.Pre);
        _plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd, HookMode.Pre);
        //Listeners:
        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>((ResourceManifest manifest) =>
        {
            foreach (var prop in _plugin.Config.Props)
            {
                manifest.AddResource(prop);
            }
        });
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        _plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        _plugin.RegisterEventHandler<EventMapTransition>(OnMapTransition);
    }
    // NOWY - wywoływane gdy gra się kończy
    private HookResult OnGameEnd(EventGameEnd @event, GameEventInfo info)
    {
        _plugin.DebugMode("Game ending - stopping all decal operations");
        _plugin.GameEnded = true;
        _plugin.PluginUtils!.OnMapChangeStart();
        return HookResult.Continue;
    }

    // NOWY - wywoływane gdy mecz się kończy
    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        _plugin.DebugMode("Match ending - preparing for map change");
        _plugin.GameEnded = true;

        // Bezpieczne czyszczenie
        _plugin.PluginUtils!.SafeCleanup();

        // Podstawowe czyszczenie
        _plugin.PropManager?._props.Clear();
        _plugin.AllowAdminCommands = false;
        _plugin.PingPlacement = false;
        _plugin.DecalAdToPlace = 0;

        return HookResult.Continue;
    }

    private HookResult OnMapTransition(EventMapTransition @event, GameEventInfo info)
    {
        var allAdvs = Utilities.FindAllEntitiesByDesignerName<CEnvDecal>("env_decal");
        foreach (var ad in allAdvs)
        {
            if (ad.Entity!.Name.Contains("advert"))
            {
                ad.Remove();
            }
        }
        return HookResult.Continue;
    }


    private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
    {

        if (!spawnDecals)
        {
            spawnDecals = true;
            _plugin.DebugMode("Spawn Decals set to true");
        }
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Sprawdź czy można tworzyć decale
        if (!_plugin.GameEnded)
        {
            try
            {
                _plugin.PropManager?.SpawnProps();
            }
            catch (Exception ex)
            {
                _plugin.DebugMode($"Error spawning props: {ex.Message}");
            }
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



    private void OnMapStart(string mapName)
    {
        _plugin.DebugMode($"Map starting: {mapName}");
        
        try
        {
            // Reset stanu - PODSTAWOWE operacje najpierw
            _plugin.PropManager?._props.Clear();
            _plugin.AllowAdminCommands = false;
            _plugin.PingPlacement = false;
            _plugin.DecalAdToPlace = 0;
            _plugin.GameEnded = false;

            // DELAY - poczekaj aż Material System będzie gotowy
            Server.NextFrame(() =>
            {
                try
                {
                    _plugin.DebugMode("OnMapStart: First NextFrame - basic setup");
                    _plugin.PropManager!._mapName = mapName;
                    _plugin.PropManager!._mapFilePath = Path.Combine(_plugin.ModuleDirectory, "maps", $"{mapName}.json");
                    _plugin.PropManager.GenerateJsonFile();

                    // WIĘCEJ DELAY - Material System potrzebuje czasu
                    Server.NextFrame(() =>
                    {
                        Server.NextFrame(() =>
                        {
                            try
                            {
                                _plugin.DebugMode("OnMapStart: Third NextFrame - checking Material System");
                                
                                // Sprawdź czy Material System jest gotowy PRZED ładowaniem props
                                if (!_plugin.PluginUtils!.IsMaterialSystemAvailable())
                                {
                                    _plugin.DebugMode("Material System not ready - delaying props loading");
                                    
                                    // Spróbuj jeszcze raz za chwilę
                                    Server.NextFrame(() =>
                                    {
                                        try
                                        {
                                            if (_plugin.PluginUtils!.IsMaterialSystemAvailable())
                                            {
                                                _plugin.DebugMode("Material System ready - loading props");
                                                _plugin.PluginUtils?.OnMapLoaded();
                                                _plugin.PropManager.LoadPropsFromMap();
                                            }
                                            else
                                            {
                                                _plugin.DebugMode("Material System still not ready - skipping props loading");
                                                // Podstawowa inicjalizacja bez props
                                                _plugin.PluginUtils!.OnMapLoaded();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _plugin.DebugMode($"Error in delayed props loading: {ex.Message}");
                                           _plugin.PluginUtils!.OnMapLoaded();
                                        }
                                    });
                                }
                                else
                                {
                                    _plugin.DebugMode("Material System ready - proceeding normally");
                                    _plugin.PluginUtils!.OnMapLoaded();
                                    _plugin.PropManager.LoadPropsFromMap();
                                }
                            }
                            catch (Exception ex)
                            {
                                _plugin.DebugMode($"Error in third NextFrame: {ex.Message}");
                                // Fallback - przynajmniej powiadom DecalManager
                                try
                                {
                                    _plugin.PluginUtils!.OnMapLoaded();
                                }
                                catch (Exception ex2)
                                {
                                    _plugin.DebugMode($"Error in fallback OnMapLoaded: {ex2.Message}");
                                }
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    _plugin.DebugMode($"Error in first NextFrame: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.DebugMode($"Error in OnMapStart main: {ex.Message}");
        }
    }

    private void OnMapEnd()
    {
        _plugin.DebugMode("Map ending - cleaning up");

        // Powiadom DecalManager o zmianie mapy
        _plugin.PluginUtils?.OnMapChangeStart();

        // Podstawowe czyszczenie
        _plugin.PropManager?._props.Clear();
        _plugin.AllowAdminCommands = false;
        _plugin.PingPlacement = false;
        _plugin.DecalAdToPlace = 0;
        _plugin.GameEnded = true;
    }

    private void ClearCache()
    {
        _plugin.DebugMode("Clearing cache due to manual map change");
        _plugin.PluginUtils?.OnMapChangeStart();
        _plugin.PropManager?._props.Clear();
        _plugin.GameEnded = true;
    }

    public HookResult ListenerChangeLevel(CCSPlayerController? player, CommandInfo info)
    {
        ClearCache();
        return HookResult.Continue;
    }

}
