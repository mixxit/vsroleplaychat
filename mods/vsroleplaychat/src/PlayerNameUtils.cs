using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace vsroleplaychat.src
{
    public class PlayerNameUtils
    {
        public static string CleanupRoleplayName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return playerName;

            playerName = playerName.ToLower();
            if (playerName.Length > 8)
                playerName = playerName.Substring(0, 8);

            Regex rgx = new Regex("[^a-z]");
            playerName = rgx.Replace(playerName, "");

            if (playerName.Length < 1)
                return "unknown";

            return playerName;
        }

        public static string GetFullRoleplayNameAsDisplayFormat(EntityAgent entity, Color? fallbackColor = null, bool colorPlayersRole = false)
        {
            var name = entity.GetName();
            if (entity is EntityPlayer)
            {
                var foreName = FirstCharToUpper(PlayerNameUtils.CleanupRoleplayName(entity.WatchedAttributes.GetString("roleplayForename", "")).TrimEnd());
                var lastName = FirstCharToUpper(PlayerNameUtils.CleanupRoleplayName(entity.WatchedAttributes.GetString("roleplaySurname", "")).TrimEnd());

                name = foreName + lastName;
                if (String.IsNullOrEmpty(foreName + lastName))
                    name = FirstCharToUpper(PlayerNameUtils.CleanupRoleplayName(entity.GetName()).TrimEnd()); ;

                if ((foreName + lastName).Length > 16)
                    name = FirstCharToUpper(PlayerNameUtils.CleanupRoleplayName(entity.GetName()).TrimEnd());
            }

            if (colorPlayersRole && entity is EntityPlayer && ((EntityPlayer)entity)?.Player is IServerPlayer)
            {
                name = HexColor.ColorMessage(((IServerPlayer)((EntityPlayer)entity)?.Player).Role.Color, name);
            }
            else if (fallbackColor != null)
            {
                name = HexColor.ColorMessage((Color)fallbackColor, name);
            }

            return name;
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;

            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }
    }
}
