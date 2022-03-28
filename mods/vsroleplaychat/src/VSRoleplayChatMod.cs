using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace vsroleplaychat.src
{
    public class VSRoleplayChatMod : ModSystem
    {
        Random rand;
        private ICoreServerAPI csapi;
        private int localChatDistance = 700;

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
            api.RegisterCommand("global", "sends a message to ooc", "", CmdOoc, null);
            api.RegisterCommand("local", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("l", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("say", "sends a message to local roleplay chat", "", CmdLocal, null);
            api.RegisterCommand("emote", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("e", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("do", "sends an emote to local roleplay chat", "", CmdEmote, null);
            api.RegisterCommand("roll", "rolls a dice", "", CmdRoll, null);
            this.csapi = api;
            base.StartServerSide(api);
        }

        public override void Start(ICoreAPI api)
        {
            rand = new Random();
            base.Start(api);

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
                player.SendMessage(groupId, $"Invalid number ", EnumChatType.CommandError);
                return;
            }

            String message = "rolls 1d" + maxnumber + ". It's a " + rand.Next(1, maxnumber + 1) + "!";
            SendEmote(player, message, true);
        }

        private void OnPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            consumed.value = true;
            SendOoc(byPlayer, StripDefaultChatPrefix(message));
        }

        private string StripDefaultChatPrefix(string message)
        {
            if (message.Contains("</strong>") && message.StartsWith("<strong>"))
                message = message.Substring(message.IndexOf("</strong>")+9,message.Length-(message.IndexOf("</strong>") + 9));
            return message.Trim();
        }

        private void SendOoc(IServerPlayer sourcePlayer, string message)
        {
            foreach (var player in csapi.Server.Players)
                SendMessage(sourcePlayer, player, EnumRPChannelPrefix.OutOfCharacter, message);
        }

        private void SendEmote(IServerPlayer sourcePlayer, string message, bool prefixNonUserEmote = false)
        {
            foreach (var player in csapi.Server.Players)
            {
                if (player.Entity.ServerPos.SquareDistanceTo(sourcePlayer.Entity.ServerPos) > localChatDistance)
                    continue;

                SendEmoteLocally(sourcePlayer, player, message, prefixNonUserEmote);
            }
        }


        private void SendLocal(IServerPlayer sourcePlayer, string message)
        {
            bool didAnyoneHear = false;
            foreach (var player in csapi.Server.Players)
            {
                if (player.Entity.ServerPos.SquareDistanceTo(sourcePlayer.Entity.ServerPos) > localChatDistance)
                    continue;

                SendMessage(sourcePlayer, player, EnumRPChannelPrefix.NearbyRoleplay, message);
                if (didAnyoneHear == false && !sourcePlayer.PlayerUID.Equals(player.PlayerUID))
                    didAnyoneHear = true;
            }

            if (didAnyoneHear == false)
                sourcePlayer.SendMessage(GlobalConstants.GeneralChatGroup, "* You sense no one is close enough to hear you", EnumChatType.Notification);
        }


        private void SendMessage(IServerPlayer sourcePlayer, IServerPlayer destinationPlayer, EnumRPChannelPrefix rpChannelPrefix, string message)
        {
            var chatType = EnumChatType.OwnMessage;
            if (!sourcePlayer.PlayerUID.Equals(destinationPlayer.PlayerUID))
                chatType = EnumChatType.OthersMessage;

            var prefix = "["+ rpChannelPrefix.ToString()+"] " + PlayerNameUtils.GetFullRoleplayNameAsDisplayFormat(sourcePlayer.Entity) + ": ";
            destinationPlayer.SendMessage(GlobalConstants.GeneralChatGroup, prefix + message, chatType);

            // LOG IT
            if (sourcePlayer.PlayerUID.Equals(destinationPlayer.PlayerUID))
                sourcePlayer.Entity.Api.Logger.Chat(sourcePlayer.PlayerName + "@" + prefix + message);
        }

        private void SendEmoteLocally(IServerPlayer sourcePlayer, IServerPlayer destinationPlayer, string message, bool prefixNonUserEmote = false)
        {
            var chatType = EnumChatType.OwnMessage;
            if (!sourcePlayer.PlayerUID.Equals(destinationPlayer.PlayerUID))
                chatType = EnumChatType.OthersMessage;

            // used to prevent /roll fraud
            var prefix = "* " + PlayerNameUtils.GetFullRoleplayNameAsDisplayFormat(sourcePlayer.Entity) + " ";
            if (prefixNonUserEmote)
                prefix = "[A]" + prefix;

            destinationPlayer.SendMessage(GlobalConstants.GeneralChatGroup, prefix + message, chatType);

            // LOG IT
            if (sourcePlayer.PlayerUID.Equals(destinationPlayer.PlayerUID))
                sourcePlayer.Entity.Api.Logger.Chat(sourcePlayer.PlayerName + "@" + prefix + message);
        }

        private void CmdOoc(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendOoc(player, ArgsToString(args));
        }

        private void CmdEmote(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendEmote(player, ArgsToString(args));
        }

        private string ArgsToString(CmdArgs args)
        {
            var message = "";
            for (int i = 0; i < args.Length; i++)
            {
                message += args[i] + " ";
            }

            return message.TrimEnd();
        }

        private void CmdLocal(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendLocal(player, ArgsToString(args));
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
