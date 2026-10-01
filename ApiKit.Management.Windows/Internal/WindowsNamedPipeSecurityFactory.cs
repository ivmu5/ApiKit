using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using ApiKit.Management.Windows.Options;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeSecurityFactory
{
    public static NamedPipeServerStream CreateServer(
        string pipeName,
        WindowsNamedPipeAccessOptions access,
        IEnumerable<string>? additionalAllowedSids = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(access);

        var dynamicSids = additionalAllowedSids?
            .Where(static sid => !string.IsNullOrWhiteSpace(sid))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        if (access.CurrentUserOnly && dynamicSids.Length == 0)
        {
            return new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        }

        var security = new PipeSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var serverSid = WindowsIsolationIdentity.GetCurrentIsolationSid();

        security.AddAccessRule(new PipeAccessRule(
            serverSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        foreach (var accountName in access.AllowedAccounts)
        {
            var account = new NTAccount(accountName);
            var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
            AddReadWriteRule(security, sid);
        }

        foreach (var sidValue in access.AllowedSids.Concat(dynamicSids))
        {
            var sid = new SecurityIdentifier(sidValue);
            AddReadWriteRule(security, sid);
        }

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            security,
            HandleInheritability.None,
            additionalAccessRights: (PipeAccessRights)0);
    }

    private static void AddReadWriteRule(PipeSecurity security, SecurityIdentifier sid)
    {
        security.AddAccessRule(new PipeAccessRule(
            sid,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));
    }
}
