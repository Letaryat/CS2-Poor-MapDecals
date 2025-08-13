using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.CompilerServices;


namespace CS2_Poor_MapDecals.Utils;

public class PluginUtils(CS2_Poor_MapDecals plugin)
{
    private readonly CS2_Poor_MapDecals _plugin = plugin;

    public unsafe void CreateDecal(Vector cords, QAngle angle, int index, float width, float height, bool forceOnVip)
    {
        var entity = Utilities.CreateEntityByName<CEnvDecal>("env_decal");
        if (entity == null) return;

        try
        {
            var material = _plugin.Config.Props[index];
            var materialPtr = FindMaterialByPath(material);
            if (materialPtr == IntPtr.Zero)
            {
                _plugin.DebugMode("Could not find a material. Skipping.");
                return;
            }

            entity.Entity!.Name = "advert";

            if (forceOnVip)
            {
                entity.Entity!.Name += "force";
            }

            entity.Width = width;
            entity.Height = height;
            entity.Depth = 4;

            Unsafe.Write((void*)entity.DecalMaterial.Handle, materialPtr);
            Utilities.SetStateChanged(entity, "CEnvDecal", "m_hDecalMaterial");

            entity.RenderOrder = 1;
            entity.RenderMode = RenderMode_t.kRenderNormal;

            entity.ProjectOnWorld = true;

            entity.Teleport(cords, new QAngle(angle.X, angle.Y, 0));
            entity.DispatchSpawn();
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"{error}");
        }

    }

    public void CreateDecalOnClick(CCSPlayerPawn pawn, Vector position, float width, float height, bool forceOnVip)
    {
        float flippedYaw = (pawn.EyeAngles.Y + 180.0f) % 360.0f;
        QAngle spriteAngle = new QAngle(pawn.EyeAngles.X, flippedYaw, pawn.EyeAngles.Z);
        Vector impactPos = new Vector(position.X, position.Y, position.Z);

        Vector backward = -GetForwardVector(pawn.EyeAngles);
        backward = Normalize(backward);

        Vector offsetPos = impactPos + backward * 2f;

        var eyeAngleZ = GetPlayerEyeVector(pawn);

        try
        {
            if (eyeAngleZ < -0.90)
            {
                offsetPos.Z += 1f;
                CreateDecal(offsetPos, new QAngle(0, spriteAngle.Y, 0), _plugin.DecalAdToPlace, width, height, forceOnVip);
                _plugin.PropManager!.PushCordsToFile(offsetPos, new QAngle(0, spriteAngle.Y, 0), _plugin.DecalAdToPlace, width, height, forceOnVip);
            }
            else
            {
                CreateDecal(offsetPos, new QAngle(90, spriteAngle.Y, 0), _plugin.DecalAdToPlace, width, height, forceOnVip);
                _plugin.PropManager!.PushCordsToFile(offsetPos, new QAngle(90, spriteAngle.Y, 0), _plugin.DecalAdToPlace, width, height, forceOnVip);
            }
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"{error}");
        }
    }

    public Vector GetForwardVector(QAngle angles)
    {
        float radYaw = angles.Y * (float)(Math.PI / 180.0);
        return new Vector((float)Math.Cos(radYaw), (float)Math.Sin(radYaw), 0);
    }
    public Vector Normalize(Vector vec)
    {
        float length = MathF.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        if (length == 0)
            return new Vector(0, 0, 0);
        return new Vector(vec.X / length, vec.Y / length, vec.Z / length);
    }

    private float GetPlayerEyeVector(CCSPlayerPawn pawn)
    {
        // Credits to: 
        // https://github.com/edgegamers/Jailbreak/blob/main/mod/Jailbreak.Warden/Paint/WardenPaintBehavior.cs#L131
        if (pawn == null || !pawn.IsValid) return 0;
        var eyeAngle = pawn.EyeAngles;
        var pitch = Math.PI / 180 * eyeAngle.X;
        var yaw = Math.PI / 180 * eyeAngle.Y;
        var eyeVector = new Vector((float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)(Math.Sin(yaw) * Math.Cos(pitch)), (float)-Math.Sin(pitch));
        return eyeVector.Z;
    }

    // Credits to:
    // https://github.com/samyycX/CS2-SkyboxChanger/blob/master/Helper.cs#L26

    delegate IntPtr FindOrCreateMaterialFromResourceDelegate(IntPtr pMaterialSystem, IntPtr pOut, string materialName);

    public static unsafe IntPtr FindMaterialByPath(string material)
    {
        if (material.EndsWith("_c"))
        {
            material = material.Substring(0, material.Length - 2);
        }
        IntPtr pIMaterialSystem2 = NativeAPI.GetValveInterface(0, "VMaterialSystem2_001");
        IntPtr functionPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(pIMaterialSystem2) + (GameData.GetOffset("IMaterialSystem_FindOrCreateMaterialFromResource") * IntPtr.Size));
        var FindOrCreateMaterialFromResource = Marshal.GetDelegateForFunctionPointer<FindOrCreateMaterialFromResourceDelegate>(functionPtr);
        IntPtr outMaterial = 0;
        IntPtr pOutMaterial = (nint)(&outMaterial);
        IntPtr materialptr3;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            materialptr3 = FindOrCreateMaterialFromResource.Invoke(pIMaterialSystem2, pOutMaterial, material);
        }
        else
        {
            materialptr3 = FindOrCreateMaterialFromResource.Invoke(pOutMaterial, 0, material);
        }
        if (materialptr3 == 0)
        {
            return 0;
        }
        return *(IntPtr*)materialptr3; // CMaterial*** -> CMaterial** (InfoForResourceTypeIMaterial2)
    }

}
