using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Core;
usingstatic TOHE.Translator;
usingstatic TOHE.Utils;

namespaceTOHE.Roles.Neutral
{
    internalclassAbsorber : RoleBase
    {
        privateconstint Id = 2320000; // Unique ID for the Absorber roleprivatestaticreadonly HashSet<byte> MarkedPlayers = new HashSet<byte>(); // Players marked for absorbingpublicstaticbool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Absorber);

        publicoverride CustomRoles ThisRoleBase => CustomRoles.Impostor;
        publicoverride Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

        privatestatic OptionItem AbsorberKillCooldown;
        privatestatic OptionItem AbsorberMarkCooldown;
        privatestatic OptionItem AbsorberAbilityUses;

        publicoverridevoidSetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Absorber);
            AbsorberKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Absorber]);
            AbsorberMarkCooldown = FloatOptionItem.Create(Id + 11, "AbsorberMarkCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Absorber]);
            AbsorberAbilityUses = IntegerOptionItem.Create(Id + 12, "AbilityUses", new(0, 15, 1), 3, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Absorber])
                .SetValueFormat(OptionFormat.Times);
        }

        publicoverridevoidInit()
        {
            MarkedPlayers.Clear();
        }

        publicoverridevoidAdd(byte playerId)
        {
            AbilityLimit = AbsorberAbilityUses.GetInt();
            MarkedPlayers.Clear(); // Reset marked players for new game
        }

        publicoverridevoidSetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbsorberKillCooldown.GetFloat();
        publicoverrideboolCanUseKillButton(PlayerControl pc) => true;
        publicoverrideboolCanUseImpostorVentButton(PlayerControl pc) => false;
        publicoverrideboolCanUseSabotage(PlayerControl pc) => false;

        publicoverridevoidApplyGameOptions(IGameOptions opt, byte id)
        {
            opt.SetVision(true); // Default impostor vision option
        }

        publicoverrideboolOnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) returntrue;
            if (AbilityLimit <= 0) returntrue;

            if (MarkedPlayers.Contains(target.PlayerId))
            {
                _ = new LateTask(() => { killer.SetKillCooldown(AbsorberKillCooldown.GetFloat()); }, 0.1f, "Absorber set kcd");
                returntrue;
            }
            else
            {
                return killer.CheckDoubleTrigger(target, () => 
                { 
                    AbilityLimit -= 1;
                    MarkPlayer(target);
                    killer.SetKillCooldown(AbsorberMarkCooldown.GetFloat());
                    Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
                });
            }
        }

        privatevoidMarkPlayer(PlayerControl target)
        {
            if (!MarkedPlayers.Contains(target.PlayerId))
            {
                MarkedPlayers.Add(target.PlayerId);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Absorber), GetString("AbsorberMarked")));

                SendRPC(_Player.PlayerId, target.PlayerId);

                Logger.Info($"Player {target.GetNameWithRole()} marked by absorber {_Player.GetNameWithRole()}", "Absorber");
            }
        }

        privatestaticvoidSendRPC(byte playerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        publicoverridevoidReceiveRPC(MessageReader reader, PlayerControl NaN)
        {
            byte playerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            if (!MarkedPlayers.Contains(targetId))
            {
                MarkedPlayers.Add(targetId);
            }
        }

        publicoverridestringGetProgressText(byte playerId, bool comms) 
            => Utils.ColorString(CanMark(playerId) ? Utils.GetRoleColor(CustomRoles.Absorber).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

        privateboolCanMark(byte id) => AbilityLimit > 0;

        publicoverridevoidOnOthersTaskComplete(PlayerControl player, PlayerTask task)
        {
            if (player == null || _Player == null) return;
            if (!player.IsAlive()) return;
            byte playerId = player.PlayerId;

            if (MarkedPlayers.Contains(playerId))
            {
                player.SetDeathReason(PlayerState.DeathReason.Suicide);
                player.RpcMurderPlayer(player);
                Logger.Info($"Player {player.GetNameWithRole()} died because they were marked by { _Player.GetNameWithRole() }", "Absorber");
            }
        }
    }
}
    
