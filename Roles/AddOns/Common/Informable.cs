namespace TOHE.Roles.AddOns.Common;

public static class Informable
{
    private const int Id = 31100;

    public static OptionItem ImpCanBeInformable;
    public static OptionItem CrewCanBeInformable;
    public static OptionItem NeutralCanBeInformable;
    
    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Sleuth, canSetNum: true);
        ImpCanBeInformable = BooleanOptionItem.Create(Id + 10, "ImpCanBeInformable", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Informable]);
        CrewCanBeInformable = BooleanOptionItem.Create(Id + 11, "CrewCanBeInformable", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Informable]);
        NeutralCanBeInformable = BooleanOptionItem.Create(Id + 12, "NeutralCanBeInformable", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Informable]);
    }

    public static void InformableDead(PlayerControl target, bool inMeeting)
    {
        if (target.IsDisconnected()) return;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (inMeeting)
            {
                Utils.SendMessage(string.Format(Translator.GetString("InformableNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false)));
            }
        }
    }
}