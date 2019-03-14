using si.birokrat.next.common.conversion;
using si.birokrat.next.common.logging;
using si.birokrat.next.common.shell;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace si.birokrat.next.common.networking {
    public static class NetworkingUtils {
        public static string GetLocalIPAddress() {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) {
                return null;
            }
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

        public static bool OpenFirewallPort(string name, int port, string dir = "in", string action = "allow", string protocol = "TCP", string profile = "private") {
            string command;
            string result;

            command = $"netsh advfirewall firewall show rule name=\"{name}\"";
            result = CommandPrompt.Execute(command);
            if (!result.Contains("No rules match the specified criteria.")) {
                if (Regex.IsMatch(result, $"LocalPort:.*{port}{Environment.NewLine}")) {
                    Logger.Log("Firewall", $"Port {port} already opened.");
                    return true;
                } else {
                    command = $"netsh advfirewall firewall delete rule name=\"{name}\"";
                    try {
                        result = CommandPrompt.Execute(command, administrator: true);
                    } catch (UnauthorizedAccessException ex) {
                        Logger.Log("Exception", ex.Message);
                        Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                        return false;
                    }
                }
            }

            command = $"netsh advfirewall firewall add rule name=\"{name}\" dir=in action={action} protocol={protocol} profile={profile} localport={port}";
            try {
                result = CommandPrompt.Execute(command, administrator: true);
            } catch (UnauthorizedAccessException ex) {
                Logger.Log("Exception", ex.Message);
                Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                return false;
            }

            bool success = result.Contains("Ok.");
            if (success) {
                Logger.Log("Firewall", $"Opened port {port}.");
            }

            return success;
        }

        public static bool AddAllowedProgram(string name, string program, bool force = false, string profile = "standard") {
            string command;
            string result;

            command = $"netsh advfirewall firewall show rule name=\"{name}\"";
            result = CommandPrompt.Execute(command);
            if (!result.Contains("No rules match the specified criteria.")) {
                if (force ||
                    !(
                        Regex.IsMatch(result, $"Enabled:.*Yes{Environment.NewLine}") &&
                        Regex.IsMatch(result, $"Protocol:.*UDP{Environment.NewLine}") &&
                        Regex.IsMatch(result, $"Protocol:.*TCP{Environment.NewLine}")
                    )
                ) {
                    command = $"netsh advfirewall firewall delete rule name=\"{name}\"";
                    try {
                        result = CommandPrompt.Execute(command, administrator: true);
                    } catch (UnauthorizedAccessException ex) {
                        Logger.Log("Exception", ex.Message);
                        Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                        return false;
                    }
                } else {
                    Logger.Log("Firewall", $"Program '{name}' already allowed.");
                    return true;
                }
            }

            bool success = false;

            if (profile != "any") {
                command = $"netsh firewall add allowedprogram name=\"{name}\" program=\"{program}\" mode=enable profile={profile}";
                try {
                    result = CommandPrompt.Execute(command, administrator: true);
                    success = result.Contains("Ok.");
                } catch (UnauthorizedAccessException ex) {
                    Logger.Log("Exception", ex.Message);
                    Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                    return false;
                }
            } else {
                command = $"netsh firewall add allowedprogram name=\"{name}\" program=\"{program}\" mode=enable profile=standard";
                try {
                    result = CommandPrompt.Execute(command, administrator: true);
                    success = result.Contains("Ok.");
                } catch (UnauthorizedAccessException ex) {
                    Logger.Log("Exception", ex.Message);
                    Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                    return false;
                }

                command = $"netsh advfirewall firewall set rule name=\"{name}\" profile=private new profile=any";
                try {
                    result = CommandPrompt.Execute(command, administrator: true);
                    success &= result.Contains("Ok.");
                } catch (UnauthorizedAccessException ex) {
                    Logger.Log("Exception", ex.Message);
                    Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                    return false;
                }
            }

            if (success) {
                Logger.Log("Firewall", $"Program '{name}' allowed.");
            }

            return success;
        }

        public static bool ReserveUrlForNonAdministratorUsersAndAccounts(int port, string scheme = "http", string host = "+") {
            string command;
            string result;

            command = $"netsh http show urlacl url={scheme}://{host}:{port}/";
            result = CommandPrompt.Execute(command);
            if (result.Contains("Reserved URL")) {
                Logger.Log("Url Reservation", $"Url {scheme}://{host}:{port}/ already has a reservation.");
                return true;
            }

            command = $"netsh http add urlacl {scheme}://{host}:{port}/ user=$env:UserName";
            try {
                result = PowerShell.Execute(command, administrator: true);
            } catch (UnauthorizedAccessException ex) {
                Logger.Log("Exception", ex.Message);
                Logger.Log("Napaka", "Nimate administratorskih pravic.", toLog: false);
                return false;
            }

            bool success = result.Contains("URL reservation successfully added");
            if (success) {
                Logger.Log("Url Reservation", $"Url {scheme}://{host}:{port}/ reserved.");
            }

            return success;
        }

        public static int FindProcessPIDByListeningTCPPort(int port) {
            string command = $"netstat -ano -p TCP | find /I \"listening\" | find /I \"{port}\"";
            string result = CommandPrompt.Execute(command);
            var tokens = result.Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0) {
                return TypeConverter.StringToInteger(tokens.Last());
            }

            return 0;
        }
    }
}
