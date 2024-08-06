using System;
using System.IO;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.API.Interfaces;
using Player = Exiled.Events.Handlers.Player;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using System.Collections.Generic;
using Exiled.Events.Handlers;
using Exiled.API.Features.DamageHandlers;
using PluginAPI.Roles;

namespace ServerLog
{
    public class PluginMain : Plugin<Config>
    {
        public override string Name => "서버로그";
        public override string Author => "Dack";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(3, 0, 0);

        public static PluginMain Singleton;

        private static EventHandlers eventHandlers;
        private string killLogFilePath;
        private string scpLeftLogFilePath;
        private string connectLogFilePath;

        public override void OnEnabled()
        {
            base.OnEnabled();

            Singleton = this;

            eventHandlers = new EventHandlers();

            Player.Verified += eventHandlers.OnPlayerVerified;
            Player.Dying += eventHandlers.OnPlayerDying;
            Player.Died += eventHandlers.OnPlayerDied;
            Player.Left += eventHandlers.OnPlayerLeft;

            string logDirectory = Path.Combine(Paths.Plugins, "서버로그");
            Directory.CreateDirectory(logDirectory);

            killLogFilePath = Path.Combine(logDirectory, "kill_log.txt");
            scpLeftLogFilePath = Path.Combine(logDirectory, "scp_left_log.txt");
            connectLogFilePath = Path.Combine(logDirectory, "connect_log.txt");

            EnsureFileExists(killLogFilePath);
            EnsureFileExists(scpLeftLogFilePath);
            EnsureFileExists(connectLogFilePath);
        }

        private void EnsureFileExists(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create or check file {filePath}: {ex}");
            }
        }

        public override void OnDisabled()
        {
            Player.Verified -= eventHandlers.OnPlayerVerified;
            Player.Dying -= eventHandlers.OnPlayerDying;
            Player.Died -= eventHandlers.OnPlayerDied;
            Player.Left -= eventHandlers.OnPlayerLeft;

            eventHandlers = null;

            Singleton = null;

            base.OnDisabled();
        }

        public void LogKillToFile(string message)
        {
            LogToFile(killLogFilePath, message);
        }

        public void LogScpLeftToFile(string message)
        {
            LogToFile(scpLeftLogFilePath, message);
        }

        public void LogConnectToFile(string message)
        {
            LogToFile(connectLogFilePath, message);
        }

        private void LogToFile(string filePath, string message)
        {
            try
            {
                File.AppendAllText(filePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write to file {filePath}: {ex}");
            }
        }
    }

    public class EventHandlers
    {
        private readonly Dictionary<string, RoleTypeId> playerRoles = new Dictionary<string, RoleTypeId>();
        private readonly Dictionary<RoleTypeId, string> roleNames = new Dictionary<RoleTypeId, string>
        {
            { RoleTypeId.ClassD, "죄수" },
            { RoleTypeId.Tutorial, "튜토리얼" },
            { RoleTypeId.Scientist, "과학자" },
            { RoleTypeId.FacilityGuard, "경비원" },
            { RoleTypeId.NtfPrivate, "MTF 이등병" },
            { RoleTypeId.NtfSpecialist, "MTF 상등병" },
            { RoleTypeId.NtfSergeant, "MTF 병장" },
            { RoleTypeId.NtfCaptain, "MTF 대위" },
            { RoleTypeId.ChaosRepressor, "혼돈의 반란 약탈자" },
            { RoleTypeId.ChaosMarauder, "혼돈의 반란 압제자" },
            { RoleTypeId.ChaosRifleman, "혼돈의 반란 병사" },
            { RoleTypeId.ChaosConscript, "혼돈의 반란 징집병" },
            { RoleTypeId.Scp049, "SCP-049" },
            { RoleTypeId.Scp0492, "SCP-049-2" },
            { RoleTypeId.Scp079, "SCP-079" },
            { RoleTypeId.Scp096, "SCP-096" },
            { RoleTypeId.Scp106, "SCP-106" },
            { RoleTypeId.Scp173, "SCP-173" },
            { RoleTypeId.Scp939, "SCP-939" },
            { RoleTypeId.Scp3114, "SCP-3114" },
            // Add more roles as needed
        };

        private readonly Dictionary<DamageType, string> damageTypeTranslations = new Dictionary<DamageType, string>
        {
            { DamageType.Falldown, "낙사" },
            { DamageType.PocketDimension, "할배 주머니" },
            { DamageType.Scp, "SCP" },
            { DamageType.Asphyxiation, "질식" },
            { DamageType.Bleeding, "출혈" },
            { DamageType.CardiacArrest, "심정지" },
            { DamageType.Custom, "커스텀" },
            { DamageType.Decontamination, "제독" },
            { DamageType.Explosion, "폭발" },
            { DamageType.FriendlyFireDetector, "아군 사격 탐지기" },
            { DamageType.MicroHid, "레일건" },
            { DamageType.Poison, "독" },
            { DamageType.Tesla, "테슬라" },
            { DamageType.Logicer, "로지카" },
            { DamageType.E11Sr, "E11" },
            { DamageType.Jailbird, "제일버드" },
            { DamageType.A7, "A7" },
            { DamageType.Revolver, "리볼버" },
            { DamageType.Crossvec, "벡터" },
            { DamageType.Shotgun, "샷건" },
            { DamageType.Fsp9, "mp7" },
            { DamageType.ParticleDisruptor, "3X 입자 분열기" },
            { DamageType.Scp173, "SCP-173" },
            { DamageType.Scp096, "SCP-096" },
            { DamageType.Scp106, "SCP-106" },
            { DamageType.Scp939, "SCP-939" },
            { DamageType.Scp3114, "SCP-3114" },
        };

        public void OnPlayerVerified(VerifiedEventArgs ev)
        {
            try
            {
                string logMessage = $"{ev.Player.Nickname} (Steam ID: {ev.Player.UserId}, IP: {ev.Player.IPAddress}) 님이 들어오셨습니다.";
                Log.Info(logMessage);
                PluginMain.Singleton.LogConnectToFile(logMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPlayerVerified: {ex}");
            }
        }

        public void OnPlayerDying(DyingEventArgs ev)
        {
            try
            {
                playerRoles[ev.Player.UserId] = ev.Player.Role.Type;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPlayerDying: {ex}");
            }
        }

        public void OnPlayerDied(DiedEventArgs ev)
        {
            try
            {
                if (playerRoles.TryGetValue(ev.Player.UserId, out RoleTypeId role))
                {
                    if (role == RoleTypeId.Spectator)
                        return;

                    if (ev.DamageHandler.Type == DamageType.Unknown)
                        return;

                    string roleName = roleNames.ContainsKey(role) ? roleNames[role] : role.ToString();
                    string damageTypeName = GetDamageTypeName(ev.DamageHandler.Type);

                    string logMessage;

                    if (ev.DamageHandler is AttackerDamageHandler attackerHandler)
                    {
                        var attacker = attackerHandler.Attacker;
                        if (attacker != null)
                        {
                            string killerRoleName = roleNames.ContainsKey(attacker.Role.Type) ? roleNames[attacker.Role.Type] : attacker.Role.Type.ToString();
                            string killerInfo = $"죽인 사람: {attacker.Nickname} (역할: {killerRoleName}, Steam ID: {attacker.UserId}, IP: {attacker.IPAddress})";
                            logMessage = $"{ev.Player.Nickname}님이 {roleName} 역할로 죽으셨습니다. 사망 원인: {damageTypeName} (Steam ID: {ev.Player.UserId}) {killerInfo}";
                        }
                        else
                        {
                            logMessage = $"{ev.Player.Nickname}님이 {roleName} 역할로 죽으셨습니다. 사망 원인: {damageTypeName} (Steam ID: {ev.Player.UserId})";
                        }
                    }
                    else
                    {
                        logMessage = $"{ev.Player.Nickname}님이 {roleName} 역할로 죽으셨습니다. 사망 원인: {damageTypeName} (Steam ID: {ev.Player.UserId})";
                    }

                    Log.Info(logMessage);
                    PluginMain.Singleton.LogKillToFile(logMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPlayerDied: {ex}");
            }
        }

        private string GetDamageTypeName(DamageType damageType)
        {
            try
            {
                return damageTypeTranslations.TryGetValue(damageType, out string translation) ? translation : damageType.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in translating DamageType: {ex}");
                return damageType.ToString();
            }
        }

        public void OnPlayerLeft(LeftEventArgs ev)
        {
            try
            {
                if (ev.Player.IsAlive && ev.Player.Role.Team == Team.SCPs)
                {
                    string roleName = roleNames.ContainsKey(ev.Player.Role.Type) ? roleNames[ev.Player.Role.Type] : ev.Player.Role.Type.ToString();
                    string logMessage = $"{ev.Player.Nickname} (Steam ID: {ev.Player.UserId}) 님이 살아있는 상태로 게임을 나갔습니다. (역할: {roleName})";
                    Log.Info(logMessage);
                    PluginMain.Singleton.LogScpLeftToFile(logMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPlayerLeft: {ex}");
            }
        }
    }

    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
    }
}
수박바 먹고 씹따
