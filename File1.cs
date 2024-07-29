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

            if (!File.Exists(killLogFilePath))
            {
                File.Create(killLogFilePath).Dispose();
            }

            if (!File.Exists(scpLeftLogFilePath))
            {
                File.Create(scpLeftLogFilePath).Dispose();
            }

            if (!File.Exists(connectLogFilePath))
            {
                File.Create(connectLogFilePath).Dispose();
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
            try
            {
                File.AppendAllText(killLogFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write to kill log file: {ex}");
            }
        }

        public void LogScpLeftToFile(string message)
        {
            try
            {
                File.AppendAllText(scpLeftLogFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write to SCP left log file: {ex}");
            }
        }

        public void LogConnectToFile(string message)
        {
            try
            {
                File.AppendAllText(connectLogFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to write to connect log file: {ex}");
            }
        }
    }

    public class EventHandlers
    {
        private readonly Dictionary<string, RoleTypeId> playerRoles = new Dictionary<string, RoleTypeId>();

        public void OnPlayerVerified(VerifiedEventArgs ev)
        {
            try
            {
                string logMessage = $"{ev.Player.Nickname} (Steam ID: {ev.Player.UserId}, IP: {ev.Player.IPAddress}) 님이 들어오셨습니다.";
                Log.Info(logMessage);
                PluginMain.Singleton.LogConnectToFile(logMessage); // 접속 로그를 connect_log.txt에 기록
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
                    // 예외 처리: Spectator 역할일 경우 로그에 기록하지 않음
                    if (role == RoleTypeId.Spectator)
                        return;

                    string logMessage;

                    if (ev.DamageHandler is AttackerDamageHandler attackerHandler)
                    {
                        var attacker = attackerHandler.Attacker;
                        if (attacker != null)
                        {
                            string killerInfo = $"죽인 사람: {attacker.Nickname} (역할: {attacker.Role.Type}, Steam ID: {attacker.UserId}, IP: {attacker.IPAddress})";
                            logMessage = $"{ev.Player.Nickname}님이 {role} 역할로 죽으셨습니다. 사망 원인: {ev.DamageHandler.Type} (Steam ID: {ev.Player.UserId}) {killerInfo}";
                        }
                        else
                        {
                            logMessage = $"{ev.Player.Nickname}님이 {role} 역할로 죽으셨습니다. 사망 원인: {ev.DamageHandler.Type} (Steam ID: {ev.Player.UserId})";
                        }
                    }
                    else
                    {
                        logMessage = $"{ev.Player.Nickname}님이 {role} 역할로 죽으셨습니다. (Steam ID: {ev.Player.UserId})";
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

        public void OnPlayerLeft(LeftEventArgs ev)
        {
            try
            {
                if (ev.Player.IsAlive && ev.Player.Role.Team == Team.SCPs)
                {
                    string logMessage = $"{ev.Player.Nickname} (Steam ID: {ev.Player.UserId}) 님이 살아있는 상태로 게임을 나갔습니다. (역할: {ev.Player.Role.Type})";
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
