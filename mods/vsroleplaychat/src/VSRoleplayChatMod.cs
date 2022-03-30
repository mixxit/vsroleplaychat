using System;
using System.Drawing;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace vsroleplaychat.src
{
    public class VSRoleplayChatMod : ModSystem
    {
        Random rand;
        private ICoreServerAPI csapi;
        private int localChatDistance = 700;
        private int shoutChatDistance = 2000;
        public Color oocColor = Color.White;
        public Color emoteColor = Color.Aquamarine;
        public Color shoutColor = Color.Red;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerChat += this.OnPlayerChat;
            api.RegisterCommand("ooc", "sends a message to ooc", "", CmdOoc, null);
            api.RegisterCommand("o", "sends a message to ooc", "", CmdOoc, null);
            api.RegisterCommand("oo", "sends a message to ooc", "", CmdOoc, null);
            api.RegisterCommand("global", "sends a message to ooc", "", CmdOoc, null);
            api.RegisterCommand("local", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("loc", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("lo", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("l", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("say", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("shout", "shouts a message to local roleplay chat", "", CmdShout, null);
            api.RegisterCommand("emote", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("e", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("em", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("do", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("roll", "rolls a dice", "", CmdRoll, null);
            api.RegisterCommand("npcsay", "makes an npc say something", "", CmdEntitySay, "root");
            api.RegisterCommand("npcdo", "makes an npc emote something", "", CmdEntityDo, "root");
            api.RegisterCommand("entitysay", "makes an npc say something", "", CmdEntitySay, "root");
            api.RegisterCommand("entitydo", "makes an npc emote something", "", CmdEntityDo, "root");
            api.RegisterCommand("npcnear", "list entity ids for nearby entities", "", CmdEntityNear, "root");
            api.RegisterCommand("entitynear", "list entity ids for nearby entities", "", CmdEntityNear, "root");
            this.csapi = api;
            base.StartServerSide(api);
        }

        public override void Start(ICoreAPI api)
        {
            rand = new Random();
            base.Start(api);

        }

        private void CmdEntityNear(IServerPlayer player, int groupId, CmdArgs args)
        {
            int distance = 5;
            if (args.Length > 0)
            {

                try
                {
                    distance = int.Parse(args[0]);
                }
                catch (Exception)
                {
                    player.SendMessage(groupId, "Invalid distance number ", EnumChatType.CommandError);
                    return;
                }
            }

            player.SendMessage(groupId, "Entitynear Distance: " + distance, EnumChatType.CommandError);

            foreach (var entity in player.Entity.World.GetEntitiesAround(new Vec3d(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z), distance, distance))
                player.SendMessage(groupId, entity.EntityId + ": " + entity.GetName() + " Distance: " + player.Entity.ServerPos.SquareDistanceTo(entity.ServerPos), EnumChatType.CommandError);
        }

        private void CmdRoll(IServerPlayer player, int groupId, CmdArgs args)
        {
            int maxnumber = 1;
            try
            {
                maxnumber = int.Parse(args[0]);
            }
            catch (Exception)
            {
                player.SendMessage(groupId, "Invalid number ", EnumChatType.CommandError);
                return;
            }

            String message = "rolls 1d" + maxnumber + ". It's a " + rand.Next(1, maxnumber + 1) + "!";
            SendEmote(player.Entity, message, true);
        }

        private void OnPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            consumed.value = true;
            SendOoc(byPlayer.Entity, StripDefaultChatPrefix(message));
        }

        private string StripDefaultChatPrefix(string message)
        {
            if (message.Contains("</strong>") && message.StartsWith("<strong>"))
                message = message.Substring(message.IndexOf("</strong>")+9,message.Length-(message.IndexOf("</strong>") + 9));
            return message.Trim();
        }

        private void SendOoc(EntityAgent sourceEntity, string message)
        {
            foreach (var player in csapi.Server.Players)
                SendOOCMessage(sourceEntity, player, message);
        }

        private void SendEmote(EntityAgent sourceEntity, string message, bool prefixNonUserEmote = false)
        {
            foreach (var player in csapi.Server.Players)
            {
                if (player.Entity.ServerPos.SquareDistanceTo(sourceEntity.ServerPos) > localChatDistance)
                    continue;

                SendEmoteLocally(sourceEntity, player, message, prefixNonUserEmote);
            }
        }


        private void SendLocal(EntityAgent sourceEntity, string message, bool shouting = false)
        {
            bool didAnyoneHear = false;
            foreach (var player in csapi.Server.Players)
            {
                if (player.Entity.ServerPos.SquareDistanceTo(sourceEntity.ServerPos) > (shouting ? shoutChatDistance : localChatDistance))
                    continue;

                SendLocalMessage(sourceEntity, player, message, shouting);
                if (didAnyoneHear == false && !sourceEntity.EntityId.Equals(player.Entity.EntityId))
                    didAnyoneHear = true;
            }

            if (didAnyoneHear == false && sourceEntity is EntityPlayer && ((EntityPlayer)sourceEntity)?.Player != null && ((EntityPlayer)sourceEntity)?.Player is IServerPlayer)
                ((IServerPlayer)((EntityPlayer)sourceEntity)?.Player).SendMessage(GlobalConstants.GeneralChatGroup, "* You sense no one is close enough to hear you", EnumChatType.Notification);
        }

        private void SendOOCMessage(EntityAgent sourceEntity, IServerPlayer destinationPlayer, string message)
        {
            var chatType = EnumChatType.OwnMessage;
            if (!sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                chatType = EnumChatType.OthersMessage;

            var prefix = "[" + EnumRPChannelPrefix.OutOfCharacter.ToString() + "] ";
            destinationPlayer.SendMessage(GlobalConstants.GeneralChatGroup, 
                HexColor.ColorMessage(oocColor, prefix + PlayerNameUtils.GetFullRoleplayNameAsDisplayFormat(sourceEntity, oocColor, true) + ": " + message), chatType);

            // LOG IT
            if (sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                sourceEntity.Api.Logger.Chat(sourceEntity.GetName() + "@" + prefix + HexColor.ColorMessage(oocColor, message));
        }

        private void SendLocalMessage(EntityAgent sourceEntity, IServerPlayer destinationPlayer, string message, bool shouting)
        {
            var seperator = shouting == false ? " says " : " shouts ";
            var color = shouting == false ? emoteColor : shoutColor;

            if (shouting)
                message = message.ToUpper();

            var chatType = EnumChatType.OwnMessage;
            if (!sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                chatType = EnumChatType.OthersMessage;

            var prefix = "[" + EnumRPChannelPrefix.NearbyRoleplay.ToString()+"] " + PlayerNameUtils.GetFullRoleplayNameAsDisplayFormat(sourceEntity, null, false);
            destinationPlayer.SendMessage(GlobalConstants.GeneralChatGroup, HexColor.ColorMessage(color, prefix + seperator + "'" +message+"'"), chatType);

            // LOG IT
            if (sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                sourceEntity.Api.Logger.Chat(sourceEntity.GetName() + "@" + prefix + HexColor.ColorMessage(emoteColor, message));
        }

        private void SendEmoteLocally(EntityAgent sourceEntity, IServerPlayer destinationPlayer, string message, bool prefixNonUserEmote = false)
        {
            var messageColor = emoteColor;

            var chatType = EnumChatType.OwnMessage;
            if (!sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                chatType = EnumChatType.OthersMessage;

            // used to prevent /roll fraud
            var prefix = "";
            if (prefixNonUserEmote)
                prefix = "[A] ";

            prefix += "* " + PlayerNameUtils.GetFullRoleplayNameAsDisplayFormat(sourceEntity, null, false) + " ";

            destinationPlayer.SendMessage(GlobalConstants.GeneralChatGroup, HexColor.ColorMessage(messageColor, prefix + message), chatType);

            // LOG IT
            if (sourceEntity.EntityId.Equals(destinationPlayer.Entity.EntityId))
                sourceEntity.Api.Logger.Chat(sourceEntity.GetName() + "@" + prefix + message);
        }

        private void CmdOoc(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendOoc(player.Entity, ArgsToString(args));
        }

        private void CmdEmote(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendEmote(player.Entity, ArgsToString(args));
        }

        private string ArgsToString(CmdArgs args, int skip = 0)
        {
            var message = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (skip > 0 && i <= (skip - 1))
                    continue;

                message += args[i] + " ";
            }

            return message.TrimEnd();
        }

        private void CmdEntitySay(IServerPlayer player, int groupId, CmdArgs args)
        {
            long entityId = 0;

            if (args.Length > 0)
            {

                try
                {
                    entityId = Convert.ToInt64(args[0]);
                }
                catch (Exception)
                {
                    player.SendMessage(groupId, "Invalid entityId", EnumChatType.CommandError);
                    return;
                }
            }
            else
            {
                player.SendMessage(groupId, "Invalid entityId", EnumChatType.CommandError);
                return;
            }

            var entity = player.Entity.World.GetEntityById(entityId);
            if (entity == null || !(entity is EntityAgent))
            {
                player.SendMessage(GlobalConstants.CurrentChatGroup, "Cannot find entity agent by id : " + entityId, EnumChatType.CommandError);
                return;
            }
            SendLocal((EntityAgent)entity, ArgsToString(args, 1));
        }

        private void CmdEntityDo(IServerPlayer player, int groupId, CmdArgs args)
        {
            long entityId = 0;

            if (args.Length > 0)
            {

                try
                {
                    entityId = Convert.ToInt64(args[0]);
                }
                catch (Exception)
                {
                    player.SendMessage(groupId, "Invalid entityId", EnumChatType.CommandError);
                    return;
                }
            } else
            {
                player.SendMessage(groupId, "Invalid entityId", EnumChatType.CommandError);
                return;
            }
            
            var entity = player.Entity.World.GetEntityById(entityId);
            if (entity == null || !(entity is EntityAgent))
            {
                player.SendMessage(GlobalConstants.CurrentChatGroup, "Cannot find entity agent by id : " + entityId, EnumChatType.CommandError);
                return;
            }

            SendEmote((EntityAgent)entity, ArgsToString(args, 1));
        }

        private void CmdLocal(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendLocal(player.Entity, ArgsToString(args));
        }

        private void CmdShout(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendLocal(player.Entity, ArgsToString(args), true);
        }

        public override double ExecuteOrder()
        {
            /// Worldgen:
            /// - GenTerra: 0 
            /// - RockStrata: 0.1
            /// - Deposits: 0.2
            /// - Caves: 0.3
            /// - Blocklayers: 0.4
            /// Asset Loading
            /// - Json Overrides loader: 0.05
            /// - Load hardcoded mantle block: 0.1
            /// - Block and Item Loader: 0.2
            /// - Recipes (Smithing, Knapping, Clayforming, Grid recipes, Alloys) Loader: 1
            /// 
            return 1.1;
        }
    }
}
