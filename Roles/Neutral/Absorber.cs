using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
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

            DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "DefaultKillCooldown", new FloatValueRule(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            IncreaseKillCooldown = FloatOptionItem.Create(Id + 11, "IncreaseKillCooldown", new FloatValueRule(2.5f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            MaxKillCooldown = FloatOptionItem.Create(Id + 12, "MaxKillCooldown", new FloatValueRule(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Seconds);

            HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber]);

            CanVent = BooleanOptionItem.Create(Id + 14, "CanVent", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber]);

            ShieldTimes = IntegerOptionItem.Create(Id + 15, "AbsorberShieldTimes", new IntegerValueRule(1, 15, 1), 2, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Times);
        }

        public override void Init()
        {
            playerIdList.Clear();
            NowCooldown.Clear();
        }

        public override void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            if (!NowCooldown.ContainsKey(playerId))
            {
                NowCooldown[playerId] = DefaultKillCooldown.GetFloat();
            }

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            AbilityLimit = ShieldTimes.GetInt();
        }

        public override void SetKillCooldown(byte id)
        {
            if (NowCooldown.ContainsKey(id))
            {
                Main.AllPlayerKillCooldown[id] = NowCooldown[id];
            }
        }

        public override bool CanUseKillButton(PlayerControl pc) => true;

        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            if (killer == target || AbilityLimit <= 0) return true;

            // Check if the killer's role should bypass the absorber's shield
            if (killer.Is(CustomRoles.KillingMachine) ||
                killer.Is(CustomRoles.Pestilence) ||
                killer.Is(CustomRoles.Jinx) ||
                killer.Is(CustomRoles.CursedWolf) ||
                killer.Is(CustomRoles.Provocateur))
            {
                return true;
            }

            // Block the kill
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
        } 
            // Increase the killer's cooldown
            if (NowCooldown.ContainsKey(killer.PlayerId))
            {
                float newCooldown = Math.Clamp(NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat(), 0f, MaxKillCooldown.GetFloat());
                NowCooldown[killer.PlayerId] = newCooldown;
                killer.ResetKillCooldown();
                killer.SyncSettings();
            }

            // Decrease ability limit and send the skill RPC
            AbilityLimit -= 1;
            SendSkillRPC();

            return true;
        }
    
}
