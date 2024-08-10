using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral
{
    internal class Artist : RoleBase
    {
        private static readonly NetworkedPlayerInfo.PlayerOutfit PaintedOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "visor_Crack", "", "");
        private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = new Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit>();

        private const int Id = 28800;
        private static readonly HashSet<byte> PlayerIds = new HashSet<byte>();
        public static bool HasEnabled => PlayerIds.Any();

        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

        private static OptionItem KillCooldown;
        private static OptionItem CanVent;
        private static OptionItem HasImpostorVision;
        private static OptionItem AbilityUses;

        private static readonly Dictionary<byte, List<byte>> PlayerSkinsPainted = new Dictionary<byte, List<byte>>();
        private static readonly Dictionary<byte, List<byte>> PaintingTarget = new Dictionary<byte, List<byte>>();

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Artist);
            KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
                .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
            HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
            AbilityUses = IntegerOptionItem.Create(Id + 11, "AbilityUses", new(0, 15, 1), 5, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
                .SetValueFormat(OptionFormat.Times);
        }

        public override void Init()
        {
            PlayerSkinsPainted.Clear();
            OriginalPlayerSkins.Clear();
            PlayerIds.Clear();
        }

        public override void Add(byte playerId)
        {
            PlayerSkinsPainted[playerId] = new List<byte>();
            PlayerIds.Add(playerId);
            PaintingTarget[playerId] = new List<byte>();
        }

        public override void ApplyGameOptions(IGameOptions opt, byte id)
        {
            opt.SetVision(HasImpostorVision.GetBool());
        }

        public override void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        }

        public override bool CanUseKillButton(PlayerControl pc) => true;
        public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
        public override bool CanUseSabotage(PlayerControl pc) => false;

        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
        
            if (AbilityUses.GetInt() > 0)
            {
                
                if (!PlayerSkinsPainted[killer.PlayerId].Contains(target.PlayerId))
                {
                    SetPainting(killer, target);
                    return false; 
                }
                return false; 
            }
            return true;
        }

        private void SetPainting(PlayerControl killer, PlayerControl target)
        {
            if (!PlayerSkinsPainted[killer.PlayerId].Contains(target.PlayerId))
            {
                if (!Camouflage.IsCamouflage)
                {
                    SetSkin(target, PaintedOutfit);
                }

                PlayerSkinsPainted[killer.PlayerId].Add(target.PlayerId);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("ArtistPaintedSkin")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("PaintedByArtist")));

                OriginalPlayerSkins[target.PlayerId] = Camouflage.PlayerSkins.GetValueOrDefault(target.PlayerId, null);
                Camouflage.PlayerSkins[target.PlayerId] = PaintedOutfit;

                SendRPC(killer.PlayerId, target.PlayerId);

                killer.SetKillCooldown();
            }
            else
            {
                target.RpcMurderPlayer(killer);
            }
        }

        public static bool IsPainting(byte seer, byte target)
        {
            return PlayerSkinsPainted.GetValueOrDefault(seer, new List<byte>()).Contains(target);
        }

        private static void SendRPC(byte playerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(AbilityUses.GetInt());
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
        {
            byte playerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            if (!PaintingTarget.ContainsKey(playerId))
                PaintingTarget[playerId] = new List<byte>();

            PaintingTarget[playerId].Add(targetId);
        }

        private void SetSkin(PlayerControl target, NetworkedPlayerInfo.PlayerOutfit outfit)
        {
            var sender = CustomRpcSender.Create(name: $"Artist.RpcSetSkin({target.Data.PlayerName})");

            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(target.Data.NetId)
                .Write((byte)outfit.ColorId)
                .EndRpc();

            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit.HatId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();

            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit.SkinId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();

            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit.VisorId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();

            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit.PetId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();

            sender.SendMessage();
        }
    }
}
