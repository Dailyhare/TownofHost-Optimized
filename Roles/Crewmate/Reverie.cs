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
            ShieldTimes = IntegerOptionItem.Create(Id + 10, "AbsorberShieldTimes", new IntegerValueRule(1, 15, 1), 2, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Times);

            DefaultKillCooldown = FloatOptionItem.Create(Id + 11, "DefaultKillCooldown", new FloatValueRule(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            IncreaseKillCooldown = FloatOptionItem.Create(Id + 12, "IncreaseKillCooldown", new FloatValueRule(2.5f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            MaxKillCooldown = FloatOptionItem.Create(Id + 13, "MaxKillCooldown", new FloatValueRule(0f, 180f, 2.5f), 2.5f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            HasImpostorVision = BooleanOptionItem.Create(Id + 14, "ImpostorVision", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber]);

            CanVent = BooleanOptionItem.Create(Id + 15, "CanVent", true, TabGroup.NeutralRoles, false)
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

        public override bool CanUseKillButton(PlayerControl pc) => true; // Ensure this returns true

        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            if (killer == target || AbilityLimit <= 0) return true;
            if (killer.Is(CustomRoles.KillingMachine)) return true;
            if (killer.Is(CustomRoles.Pestilence)) return true;
            if (killer.Is(CustomRoles.Jinx)) return true;
            if (killer.Is(CustomRoles.CursedWolf)) return true;
            if (killer.Is(CustomRoles.Provocateur)) return true;

            // Block the kill and apply cooldown adjustment
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);

            // Adjust the killer's cooldown
            float newCooldown = Math.Clamp(NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat(), 0f, MaxKillCooldown.GetFloat());
            NowCooldown[killer.PlayerId] = newCooldown;
            killer.ResetKillCooldown();
            killer.SyncSettings();

            // Decrease ability limit and send the skill RPC
            AbilityLimit -= 1;
            SendSkillRPC();

            return true;
        }
    }
}
