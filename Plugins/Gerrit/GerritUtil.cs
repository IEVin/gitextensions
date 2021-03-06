﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GitCommands;
using GitUI;
using GitUIPluginInterfaces;
using JetBrains.Annotations;

namespace Gerrit
{
    internal static class GerritUtil
    {
        private static readonly ISshPathLocator SshPathLocatorInstance = new SshPathLocator();

        public static string RunGerritCommand([NotNull] IWin32Window owner, [NotNull] IGitModule module, [NotNull] string command, [NotNull] string remote, byte[] stdIn)
        {
            var fetchUrl = GetFetchUrl(module, remote);

            return RunGerritCommand(owner, module, command, fetchUrl, remote, stdIn);
        }

        public static Uri GetFetchUrl(IGitModule module, string remote)
        {
            string remotes = module.RunGitCmd("remote show -n \"" + remote + "\"");

            string fetchUrlLine = remotes.Split('\n').Select(p => p.Trim()).First(p => p.StartsWith("Push"));

            return new Uri(fetchUrlLine.Split(new[] { ':' }, 2)[1].Trim());
        }

        public static string RunGerritCommand([NotNull] IWin32Window owner, [NotNull] IGitModule module, [NotNull] string command, [NotNull] Uri fetchUrl, [NotNull] string remote, byte[] stdIn)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (fetchUrl == null)
            {
                throw new ArgumentNullException(nameof(fetchUrl));
            }

            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            StartAgent(owner, module, remote);

            var sshCmd = SshPathLocatorInstance.Find(AppSettings.GitBinDir);
            if (GitCommandHelpers.Plink())
            {
                sshCmd = AppSettings.Plink;
            }

            if (string.IsNullOrEmpty(sshCmd))
            {
                sshCmd = "ssh.exe";
            }

            string hostname = fetchUrl.Host;
            string username = fetchUrl.UserInfo;
            string portFlag = GitCommandHelpers.Plink() ? " -P " : " -p ";
            int port = fetchUrl.Port;

            if (port == -1 && fetchUrl.Scheme == "ssh")
            {
                port = 22;
            }

            var sb = new StringBuilder();

            sb.Append('"');

            if (!string.IsNullOrEmpty(username))
            {
                sb.Append(username);
                sb.Append('@');
            }

            sb.Append(hostname);
            sb.Append('"');
            sb.Append(portFlag);
            sb.Append(port);

            sb.Append(" \"");
            sb.Append(command);
            sb.Append("\"");

            return module.RunCmd(
                sshCmd,
                sb.ToString(),
                null,
                stdIn);
        }

        public static void StartAgent([NotNull] IWin32Window owner, [NotNull] IGitModule module, [NotNull] string remote)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (GitCommandHelpers.Plink())
            {
                if (!File.Exists(AppSettings.Pageant))
                {
                    MessageBoxes.PAgentNotFound(owner);
                }
                else
                {
                    module.StartPageantForRemote(remote);
                }
            }
        }
    }
}
