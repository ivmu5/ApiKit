using ApiKit.Management.Abstractions;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed partial class WindowsNamedPipeManagementClient :
    IManagementServiceCatalog,
    IManagementOperationClient,
    IManagementResourceClient,
    IManagedServiceLifecycleController,
    IManagedServiceInstaller
{
    private readonly IOptions<WindowsManagementClientOptions> _options;
    private readonly IManagementCredentialProvider _credentialProvider;
    private readonly IManagementChallengeProvider _challengeProvider;
    private readonly IManagementPeerVerifier _peerVerifier;

    public WindowsNamedPipeManagementClient(
        IOptions<WindowsManagementClientOptions> options,
        IManagementCredentialProvider credentialProvider,
        IManagementChallengeProvider challengeProvider,
        IManagementPeerVerifier peerVerifier)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _challengeProvider = challengeProvider ?? throw new ArgumentNullException(nameof(challengeProvider));
        _peerVerifier = peerVerifier ?? throw new ArgumentNullException(nameof(peerVerifier));
    }
}
