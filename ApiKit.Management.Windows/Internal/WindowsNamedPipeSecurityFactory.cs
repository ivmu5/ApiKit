using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using ApiKit.Management.Windows.Options;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeSecurityFactory
{
    public static NamedPipeServerStream CreateServer(
        string pipeName,
        WindowsNamedPipeAccessOptions access)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(access);

        if (access.CurrentUserOnly)
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

        // Серверной identity нужен FullControl, иначе создание следующих экземпляров
        // того же pipe может быть заблокировано собственной DACL.
        using var serverIdentity = WindowsIdentity.GetCurrent();
        var serverSid = serverIdentity.User
            ?? throw new InvalidOperationException("Не удалось определить SID текущей Windows identity.");

        security.AddAccessRule(new PipeAccessRule(
            serverSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        foreach (var accountName in access.AllowedAccounts)
        {
            var account = new NTAccount(accountName);
            var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
            security.AddAccessRule(new PipeAccessRule(
                sid,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
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
}
