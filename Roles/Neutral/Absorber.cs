using AmongUs.GameOptions;
using System;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral
{
    internal class Absorber : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 311000;
        public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Absorber);
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
        //==================================================================\\

        private static OptionItem DefaultKillCooldown;
        private static OptionItem IncreaseKillCooldown;
        private static OptionItem MaxKillCooldown;
        private static OptionItem HasImpostorVision;
        private static OptionItem CanVent;
        private static OptionItem ShieldTimes;

        private static readonly Dictionary<byte, float> NowCooldown = new();
        private static readonly List<byte> playerIdList = new();

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Absorber);
            ShieldTimes = IntegerOptionItem.Create(Id + 10, "AbsorberShieldTimes", new IntRange(1, 15), 2, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Times);

            IncreaseKillCooldown = FloatOptionItem.Create(Id + 11, "IncreaseKillCooldown", new FloatRange(2.5f, 180f), 15f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            MaxKillCooldown = FloatOptionItem.Create(Id + 12, "MaxKillCooldown", new FloatRange(0f, 180f), 2.5f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber]);

            CanVent = BooleanOptionItem.Create(Id + 14, "CanVent", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber]);
        }

        public override void Init()
        {
            playerIdList.Clear();
            NowCooldown.Clear();
        }

        public override void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            AbilityLimit = ShieldTimes.GetInt();
        }

        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];

        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            if (killer == target || AbilityLimit <= 0) return true;
            if (killer.Is(CustomRoles.KillingMachine)) return true;
            if (killer.Is(CustomRoles.Pestilence)) return true;
            if (killer.Is(CustomRoles.Jinx)) return true;
            if (killer.Is(CustomRoles.CursedWolf)) return true;
            if (killer.Is(CustomRoles.Provocateur)) return true;

            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);

            NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat(), MaxKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
            killer.ResetKillCooldown();
            killer.SyncSettings();

            AbilityLimit -= 1;
            SendSkillRPC();

            return true;
        }

        public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
        public override bool CanUseKillButton(PlayerControl pc) => true;
        public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());
    }
}
