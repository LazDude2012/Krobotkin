using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules {
    public class AssignableRoles : Module {
        private const int MAX_ROLES = 250;

        private Timer removeMessagesTimer = new Timer();
        private List<Message> msgsToRemove = new List<Message>();

        public override void InitiateClient(DiscordClient _client) {
            // set up message checking timer
            removeMessagesTimer.Interval = 15000; // 15 seconds
            removeMessagesTimer.Elapsed += (sender, e) => MessageChecker(sender, e, _client);
            removeMessagesTimer.AutoReset = true;
            removeMessagesTimer.Start();

            // check any outdated self-assignable roles without "group" properties
            int rolesFound = 0;
            foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                if (String.IsNullOrEmpty(r.group)) {
                    r.group = "";
                    rolesFound++;
                }
            }
            if (rolesFound > 0) Config.INSTANCE.Commit();

            // set up commands
            _client.GetService<CommandService>().CreateCommand("roles")
                .Description("Prints out all server roles.")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    // print all server roles
                    String roleString = "```";
                    for (int i = 0; i < e.Server.RoleCount; i++){
                        Role role = e.Server.Roles.ElementAt(i);
                        roleString += role.Name + (i == e.Server.RoleCount-1 ? "" : ", ");
                    }
                    roleString += "```";

                    e.Channel.SendMessage($"Server roles ({e.Server.RoleCount}/{MAX_ROLES}):\n{roleString}");
                });

            
            _client.GetService<CommandService>().CreateCommand("iam")
                .Alias("giverole")
                .Alias("setrole")
                .Description("Gives the user a self-assignable role.")
                .Parameter("role", ParameterType.Multiple)
                .Do(e => {
                    String roleName = String.Join(" ", e.Args);
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    ModerationLog.LogToPublic($"User {e.User.Name} attempted to give themselves role {roleName} in #{e.Channel.Name}", e.Server);

                    // check if role exists
                    if (role == null) {
                        GiveFeedback(e, $"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is self-assignable
                    if (!RoleIsAssignable(role)) {
                        GiveFeedback(e, $"Role `{roleName}` is not self-assignable.");
                        return;
                    }

                    // assign role
                    if (e.User.HasRole(role)){
                        GiveFeedback(e, "You already have this role.");
                    } else {
                        e.User.AddRoles(role);
                        GiveFeedback(e, $"Assigned role `{roleName}`.");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("iamn")
                .Alias("iamnot")
                .Alias("removerole")
                .Description("Removes a self-assignable role from the user.")
                .Parameter("role", ParameterType.Multiple)
                .Do(e => {
                    String roleName = String.Join(" ", e.Args);
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    // check if role exists
                    if (role == null) {
                        GiveFeedback(e, $"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is self-assignable
                    if (!RoleIsAssignable(role)) {
                        GiveFeedback(e, $"Role `{roleName}` is not self-assignable.");
                        return;
                    }

                    // unassign role
                    if (!e.User.HasRole(role)){
                        GiveFeedback(e, "You do not have this role to remove.");
                    } else {
                        e.User.RemoveRoles(role);
                        GiveFeedback(e, $"Unassigned role `{roleName}`.");
                        ModerationLog.LogToPublic($"User {e.User.Name} removed their role {roleName} in #{e.Channel.Name}", e.Server);
                    }
                });

            _client.GetService<CommandService>().CreateGroup("aroles", egp => {
                egp.CreateCommand("add")
                .Description("Makes a role self-assignable, optionally adding it to a group.")
                .Parameter("role")
                .Parameter("group", ParameterType.Optional)
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String roleName = e.GetArg("role");
                    String groupName = e.GetArg("group").ToLower();
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    // check if role exists
                    if (role == null) {
                        e.Channel.SendMessage($"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is already self-assignable
                    if (RoleIsAssignable(role)) {
                        e.Channel.SendMessage($"Role `{roleName}` is already self-assignable.");
                        return;
                    }

                    // set role as self-assignable
                    SelfAssignRole newRole = new SelfAssignRole {
                        server_id = role.Server.Id,
                        role_id = role.Id,
                        group = groupName
                    };
                    String groupText = String.IsNullOrEmpty(groupName) ? "." : $" (added to group \"{groupName}\")";
                    Config.INSTANCE.selfAssignRoles.Add(newRole);
                    e.Channel.SendMessage($"Role `{roleName}` is now self-assignable{groupText}");
                    ModerationLog.LogToPublic($"User {e.User.Name} made role {roleName} self-assignable", e.Server);
                    Config.INSTANCE.Commit();
                });

                egp.CreateCommand("remove")
                .Description("Removes a role from being self-assignable.")
                .Parameter("role", ParameterType.Multiple)
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String roleName = String.Join(" ", e.Args);
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    // check if role exists
                    if (role == null) {
                        e.Channel.SendMessage($"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is self-assignable
                    if (!RoleIsAssignable(role)) {
                        e.Channel.SendMessage($"Role `{roleName}` is not self-assignable.");
                        return;
                    }

                    // remove role from being self-assignable
                    foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                        if (r.server_id == e.Server.Id && r.role_id == role.Id) {
                            Config.INSTANCE.selfAssignRoles.Remove(r);
                            e.Channel.SendMessage($"Role `{roleName}` is no longer self-assignable.");
                            ModerationLog.LogToPublic($"User {e.User.Name} removed role {roleName} from being self-assignable", e.Server);
                            Config.INSTANCE.Commit();
                            break;
                        }
                    }
                });

                egp.CreateCommand("print")
                .Description("Prints all self-assignable roles.")
                .Parameter("group", ParameterType.Optional)
                .Do(e => {
                    // print all self-assignable roles
                    String roleString = "";
                    int saRoleCount = 0;
                    SortedDictionary<String, List<Role>> groups = new SortedDictionary<String, List<Role>>();
                    String groupFilter = e.GetArg("group");

                    // compile all self-assignable roles by group
                    foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                        // only roles on this server
                        if (r.server_id == e.Server.Id) {
                            // get valid role
                            Role role = e.Server.GetRole(r.role_id);
                            if (role == null) continue;

                            // if group filter specified, exclude all roles not belonging to that group
                            if (!String.IsNullOrEmpty(groupFilter) && r.group != groupFilter) {
                                continue;
                            }

                            // create group list if it doesn't exist
                            if (!groups.ContainsKey(r.group)) {
                                groups.Add(r.group, new List<Role>());
                            }

                            // add role to group list
                            groups[r.group].Add(role);
                        }
                    }

                    // print roles
                    foreach (KeyValuePair<String, List<Role>> group in groups) {
                        String groupName = group.Key == "" ? "Ungrouped" : group.Key;
                        roleString += $"```{groupName} ({group.Value.Count}):\n";
                        for (int i = 0; i < group.Value.Count; i++) {
                            saRoleCount++;
                            Role role = group.Value.ElementAt(i);
                            roleString += $"{role.Name}" + (i == group.Value.Count - 1 ? "" : ", ");
                        }
                        roleString += "```";
                    }

                    // print message
                    if (saRoleCount > 0) {
                        roleString = "Self-assignable roles" + (String.IsNullOrEmpty(groupFilter) ? ":\n" : $" in group `{groupFilter}`:\n") + roleString;
                        roleString += "\nThe above roles are self-assignable using the commands:\n";
                        roleString += "`!iam [role]`    -- assigns a role\n";
                        roleString += "`!iamn [role]`   -- removes a role";

                        if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                            // feedback messages to non-mods are temporary
                            GiveFeedback(e, roleString);
                        } else {
                            // feedback messages to mods are regular messages
                            e.Channel.SendMessage(roleString);
                        }
                    } else if (!String.IsNullOrEmpty(groupFilter)) {
                        GiveFeedback(e, $"There are no self-assignable roles in group `{groupFilter}`.");
                    } else {
                        GiveFeedback(e, "There are no self-assignable roles on this server.");
                    }
                });

                egp.CreateCommand("setgroup")
                .Description("Add a self-assignable role to a group for organization.")
                .Parameter("role")
                .Parameter("group")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String roleName = e.GetArg("role");
                    String groupName = e.GetArg("group").ToLower();
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    // check if role exists
                    if (role == null) {
                        e.Channel.SendMessage($"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is self-assignable
                    if (!RoleIsAssignable(role)) {
                        e.Channel.SendMessage($"Role `{roleName}` is not self-assignable.");
                        return;
                    }

                    // set role group
                    foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                        if (r.server_id == e.Server.Id && r.role_id == role.Id) {
                            if (String.IsNullOrEmpty(r.group)) {
                                // add new group to role
                                e.Channel.SendMessage($"Role `{roleName}` added to group `{groupName}`.");
                                ModerationLog.LogToPublic($"User {e.User.Name} added role {roleName} to group {groupName}", e.Server);
                            } else {
                                // move role to other group
                                e.Channel.SendMessage($"Role `{roleName}` moved from group `{r.group}` to `{groupName}`.");
                                ModerationLog.LogToPublic($"User {e.User.Name} moved role {roleName} from group {r.group} to {groupName}", e.Server);
                            }
                            r.group = groupName;
                            Config.INSTANCE.Commit();
                            break;
                        }
                    }
                });

                egp.CreateCommand("removegroup")
                .Alias("unsetgroup")
                .Description("Remove a self-assignable role from a group.")
                .Parameter("role", ParameterType.Multiple)
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String roleName = e.GetArg("role");
                    Role role = e.Server.FindRoles(roleName, true).FirstOrDefault();

                    // check if role exists
                    if (role == null) {
                        e.Channel.SendMessage($"Role `{roleName}` does not exist.");
                        return;
                    }

                    // check if role is self-assignable
                    if (!RoleIsAssignable(role)) {
                        e.Channel.SendMessage($"Role `{roleName}` is not self-assignable.");
                        return;
                    }

                    // remove role group
                    foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                        if (r.server_id == e.Server.Id && r.role_id == role.Id) {
                            if (String.IsNullOrEmpty(r.group)) {
                                e.Channel.SendMessage($"Role `{roleName}` already not in any group.");
                            } else {
                                e.Channel.SendMessage($"Role `{roleName}` removed from group `{r.group}`.");
                                ModerationLog.LogToPublic($"User {e.User.Name} removed role {roleName} to group {r.group}", e.Server);
                                r.group = "";
                                Config.INSTANCE.Commit();
                            }
                            break;
                        }
                    }
                });

                egp.CreateCommand("renamegroup")
                .Description("Rename a group containing self-assignable roles.")
                .Parameter("current")
                .Parameter("new")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String currName = e.GetArg("current").ToLower();
                    String newName = e.GetArg("new").ToLower();

                    // change group property on matching roles
                    int foundRoles = 0;
                    foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                        if (r.group == currName) {
                            r.group = newName;
                            foundRoles++;
                        }
                    }

                    // save changes
                    if (foundRoles > 0) {
                        e.Channel.SendMessage($"Group `{currName}` renamed to `{newName}`.");
                        ModerationLog.LogToPublic($"User {e.User.Name} renamed role group {currName} to {newName}", e.Server);
                        Config.INSTANCE.Commit();
                    } else {
                        e.Channel.SendMessage($"Group `{currName}` does not exist.");
                    }
                });
            });
        }

        // send command feedback message
        private async void GiveFeedback(CommandEventArgs e, String msg){
            Message m = await e.Channel.SendMessage($"{e.User.Mention} - {msg}");
            msgsToRemove.Add(e.Message);
            msgsToRemove.Add(m);
        }

        // delete command feedback messages
        private async void MessageChecker(object sender, ElapsedEventArgs e, DiscordClient client) {
            if (msgsToRemove.Count == 0) return;
            
            List<Message> toRemove = new List<Message>();
            foreach (Message m in msgsToRemove){
                int diff = (DateTime.UtcNow - m.Timestamp.ToUniversalTime()).Seconds;
                if (diff >= 15) {
                    toRemove.Add(m);
                    await m.Delete();
                }
            }
            msgsToRemove.RemoveAll(x => toRemove.Contains(x));
        }

        // check if role is self-assignable
        private bool RoleIsAssignable(Role role) {
            foreach (SelfAssignRole r in Config.INSTANCE.selfAssignRoles) {
                if (role.Server.Id == r.server_id && role.Id == r.role_id) {
                    return true;
                }
            }
            return false;
        }
    }
}
