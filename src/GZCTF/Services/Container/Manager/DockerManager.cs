// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt

using System.Collections.Concurrent;
using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class DockerManager : IContainerManager
{
    private const int MaxCreateAttempts = 3;
    private const int MaxStartAttempts = 3;
    private const int AiProbePort = 18080;
    private static readonly TimeSpan StartRetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly DockerClient _client;
    private readonly ILogger<DockerManager> _logger;
    private readonly DockerMetadata _meta;
    private readonly ConcurrentDictionary<string, ImagePull> _imagePulls = new();

    public DockerManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerMode)],
            TaskStatus.Success, LogLevel.Debug);
    }


    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        if (container.AiDetectionToken.HasValue)
        {
            try
            {
                await _client.Containers.RemoveContainerAsync(GetAiProbeName(container.ContainerId),
                    new() { Force = true }, token);
            }
            catch (DockerContainerNotFoundException)
            {
                // The proxy may already have been removed.
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // The proxy may already have been removed.
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to remove AI detection proxy for {ContainerId}", container.ContainerId);
                return;
            }
        }

        try
        {
            await _client.Containers.RemoveContainerAsync(container.ContainerId,
                new() { Force = true }, token);
        }
        catch (DockerContainerNotFoundException)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                    container.LogId],
                TaskStatus.Success, LogLevel.Debug);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                        container.LogId],
                    TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                _logger.LogDeletionFailedWithHttpContext(container.LogId, e.StatusCode, e.ResponseBody ?? string.Empty);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    container.LogId]);
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(GZCTF.Models.Internal.ContainerConfig config,
        CancellationToken token = default)
    {
        if (config.EnableAiDetection &&
            (!Uri.TryCreate(_meta.AiDetectionCallbackUrl, UriKind.Absolute, out var callback) ||
             (callback.Scheme != Uri.UriSchemeHttp && callback.Scheme != Uri.UriSchemeHttps)))
        {
            _logger.LogWarning("AI detection requires ContainerProvider:AiDetectionCallbackUrl");
            return null;
        }

        if (config.EnableAiDetection && config.NetworkMode == NetworkMode.Isolated &&
            !IPAddress.TryParse(new Uri(_meta.AiDetectionCallbackUrl).Host, out _))
        {
            _logger.LogWarning("Isolated AI-detected instances require an IP-based callback URL");
            return null;
        }

        var probePort = config.ExposedPort == AiProbePort ? AiProbePort + 1 : AiProbePort;
        var probeToken = config.EnableAiDetection ? Guid.NewGuid() : (Guid?)null;
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();

        if (string.IsNullOrWhiteSpace(imageName))
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_UnresolvedImageName), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        // Docker has no pull policy API, so the policy is applied on the client side.
        if (!await EnsureImageAsync(config.Image, token))
            return null;

        if (config.EnableAiDetection && !await EnsureImageAsync(_meta.AiDetectionProxyImage, token))
            return null;

        var parameters = GetCreateContainerParameters(config);
        var containerName = parameters.Name ?? DockerMetadata.GetName(config);
        parameters.Name = containerName;

        if (_meta.ExposePort)
        {
            parameters.HostConfig ??= new();
            var publishedPort = config.EnableAiDetection ? probePort : config.ExposedPort;
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct> { [publishedPort.ToString()] = new() };
            parameters.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                // let docker choose a random port, do not use "PublishAllPorts" option
                // reference: https://github.com/moby/moby/blob/master/daemon/libnetwork/portallocator/portallocator.go#L135
                // function: RequestPortsInRange
                // comment:
                //     If portStart and portEnd are 0 it returns
                //     the first free port in the default ephemeral range.
                [publishedPort.ToString()] = [new PortBinding { HostPort = "0" }]
            };
        }

        var containerRes = await CreateContainerWithRetryAsync(parameters, token);

        if (containerRes is null)
            return null;

        var container = new Models.Data.Container
        {
            ContainerId = containerRes.ID,
            Image = config.Image,
            AiDetectionToken = probeToken
        };

        if (!await StartContainerWithRetryAsync(container, imageName, token))
        {
            await DestroyContainerAsync(container, token);
            return null;
        }

        var info = await _client.Containers.InspectContainerAsync(container.ContainerId, token);
        var state = info.State;

        if (state is null)
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    imageName, string.Empty],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        container.Status = state.Dead || state.OOMKilled || state.Restarting
            ? ContainerStatus.Destroyed
            : state.Running
                ? ContainerStatus.Running
                : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    imageName, state.Error],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(state.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        var networkSettings = info.NetworkSettings;
        container.IP = networkSettings?.Networks?.FirstOrDefault().Value?.IPAddress ?? string.Empty;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;

        if (config.EnableAiDetection)
        {
            if (string.IsNullOrEmpty(container.IP) ||
                !await StartAiProbeAsync(container, config, probePort, probeToken!.Value, token))
            {
                await DestroyContainerAsync(container, token);
                return null;
            }

            container.Port = probePort;
        }

        if (!_meta.ExposePort)
            return container;

        var bindings = GetPublishedPortBindings(networkSettings?.Ports, container.Port);

        if (bindings is [])
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                    imageName],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        var port = bindings.First().HostPort;

        if (int.TryParse(port, out var numPort))
            container.PublicPort = numPort;
        else
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_PortParsingFailed), port],
                TaskStatus.Failed,
                LogLevel.Warning);

        if (!string.IsNullOrEmpty(_meta.PublicEntry))
            container.PublicIP = _meta.PublicEntry;

        return container;
    }

    private static string GetAiProbeName(string containerId) => $"gzctf-ai-{containerId[..12]}";

    private async Task<bool> StartAiProbeAsync(Models.Data.Container container, GZCTF.Models.Internal.ContainerConfig config,
        int probePort, Guid probeToken, CancellationToken token)
    {
        var parameters = new CreateContainerParameters
        {
            Name = GetAiProbeName(container.ContainerId),
            Image = _meta.AiDetectionProxyImage,
            Entrypoint = ["node"],
            Cmd = ["-e", AiProbeScript.Source],
            Env =
            [
                $"AI_PROBE_PORT={probePort}",
                $"AI_CHALLENGE_PORT={config.ExposedPort}",
                $"AI_CALLBACK_URL={_meta.AiDetectionCallbackUrl}",
                $"AI_PROBE_TOKEN={probeToken}"
            ],
            Labels = new Dictionary<string, string> { ["gzctf.ai-detection-for"] = container.ContainerId },
            HostConfig = new HostConfig
            {
                NetworkMode = $"container:{container.ContainerId}",
                Memory = 64L * 1024 * 1024,
                CPUPercent = 10
            }
        };

        var proxy = await CreateContainerWithRetryAsync(parameters, token);
        if (proxy is null)
            return false;

        var proxyContainer = new Models.Data.Container
        {
            ContainerId = proxy.ID,
            Image = _meta.AiDetectionProxyImage
        };

        if (!await StartContainerWithRetryAsync(proxyContainer, "ai-detection-proxy", token))
            return false;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(
                    $"http://{container.IP}:{probePort}/__gzctf_ai_probe_health", token);
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return true;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), token);
        }

        _logger.LogWarning("AI detection proxy did not become healthy for {ContainerId}", container.ContainerId);
        return false;
    }

    internal static IList<PortBinding> GetPublishedPortBindings(
        IDictionary<string, IList<PortBinding>>? ports, int exposedPort)
    {
        if (ports is not { Count: > 0 })
            return [];

        var port = exposedPort.ToString();
        var portPrefix = $"{port}/";
        var matchedPorts = ports
            .Where(kv => kv.Value is { Count: > 0 }
                && kv.Key.StartsWith(portPrefix, StringComparison.Ordinal))
            .ToArray();

        return matchedPorts switch
        {
            [] => [],
            [{ Value: var bindings }] => bindings,
            _ => matchedPorts.FirstOrDefault(kv =>
                     kv.Key.EndsWith("/tcp", StringComparison.OrdinalIgnoreCase))
                 .Value
                 ?? matchedPorts[0].Value
        };
    }

    private async Task<bool> EnsureImageAsync(string image, CancellationToken token)
    {
        var pullPolicy = _meta.Config.ImagePullPolicy;

        if (pullPolicy == ImagePullPolicy.Always)
            return await PullImageAsync(image, token);

        bool exists;
        try
        {
            exists = await ImageExistsAsync(image, token);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ImageInspectFailed), image]);
            return false;
        }

        if (exists)
            return true;

        if (pullPolicy == ImagePullPolicy.Never)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ImageNotFound), image],
                TaskStatus.Failed, LogLevel.Warning);
            return false;
        }

        return await PullImageAsync(image, token);
    }

    private async Task<bool> ImageExistsAsync(string image, CancellationToken token)
    {
        try
        {
            await _client.Images.InspectImageAsync(image, token);
            return true;
        }
        catch (DockerImageNotFoundException)
        {
            return false;
        }
    }

    private async Task<CreateContainerResponse?> CreateContainerWithRetryAsync(
        CreateContainerParameters parameters, CancellationToken token)
    {
        var containerName = parameters.Name ?? string.Empty;
        var image = parameters.Image ?? string.Empty;

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            try
            {
                return await _client.Containers.CreateContainerAsync(parameters, token);
            }
            catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted), containerName],
                    TaskStatus.Duplicate, LogLevel.Warning);

                if (!await RemoveContainerAsync(containerName, token))
                    return null;
            }
            catch (DockerImageNotFoundException)
            {
                // The image was removed after EnsureImageAsync; do not pull again.
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ImageMissing), image],
                    TaskStatus.Failed, LogLevel.Warning);
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DockerApiException e)
            {
                _logger.LogCreationFailedWithHttpContext(containerName, e.StatusCode,
                    e.ResponseBody ?? string.Empty);
                return null;
            }
            catch (Exception e)
            {
                _logger.LogErrorMessage(e,
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                        containerName]);
                return null;
            }
        }

        _logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), containerName],
            TaskStatus.Failed, LogLevel.Information);
        return null;
    }

    private async Task<bool> RemoveContainerAsync(string containerName, CancellationToken token)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(containerName, new() { Force = true }, token);
            return true;
        }
        catch (DockerContainerNotFoundException)
        {
            return true;
        }
        catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    containerName]);
            return false;
        }
    }

    private async Task<bool> StartContainerWithRetryAsync(Models.Data.Container container, string imageName,
        CancellationToken token)
    {
        for (var attempt = 1; attempt <= MaxStartAttempts; attempt++)
        {
            if (await _client.Containers.StartContainerAsync(container.ContainerId, new(), token))
                return true;

            if (attempt < MaxStartAttempts)
                await Task.Delay(StartRetryDelay, token);
        }

        _logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerInstanceStartFailed),
                container.LogId, imageName],
            TaskStatus.Failed, LogLevel.Warning);
        return false;
    }

    // The shared pull is not bound to one caller's token: a single cancelled waiter
    // does not abort it, but the last waiter leaving cancels the shared pull.
    private async Task<bool> PullImageAsync(string image, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var pull = AcquireImagePull(image, token);

        try
        {
            return await pull.Task.Value.WaitAsync(token);
        }
        finally
        {
            pull.RemoveWaiter();
        }
    }

    private ImagePull AcquireImagePull(string image, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();

            var pull = _imagePulls.GetOrAdd(image, CreateImagePull);

            if (pull.TryAddWaiter())
                return pull;

            _imagePulls.TryRemove(new KeyValuePair<string, ImagePull>(image, pull));
        }
    }

    private ImagePull CreateImagePull(string image)
    {
        var pull = new ImagePull();

        pull.Task = new Lazy<Task<bool>>(async () =>
        {
            try
            {
                return await PullImageCoreAsync(image, pull.Cancellation.Token);
            }
            finally
            {
                // Value-based removal never drops a newer in-flight pull.
                _imagePulls.TryRemove(new KeyValuePair<string, ImagePull>(image, pull));
            }
        });

        return pull;
    }

    private async Task<bool> PullImageCoreAsync(string image, CancellationToken token)
    {
        _logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerManager_PullContainerImage), image],
            TaskStatus.Pending, LogLevel.Information);

        var auth = _meta.AuthConfigs.GetForImage(image) ?? new AuthConfig();
        var progress = new ImagePullProgress(_logger, image);

        try
        {
            await _client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = image }, auth,
                progress, token);

            return !progress.Failed;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ImagePullFailed), image]);
            return false;
        }
    }

    internal sealed class ImagePull
    {
        private readonly Lock _gate = new();
        private int _waiters;
        private bool _closed;

        public CancellationTokenSource Cancellation { get; } = new();

        public Lazy<Task<bool>> Task { get; set; } = null!;

        public bool TryAddWaiter()
        {
            lock (_gate)
            {
                if (_closed)
                    return false;

                _waiters++;
                return true;
            }
        }

        public void RemoveWaiter()
        {
            bool cancel;

            lock (_gate)
            {
                cancel = --_waiters == 0;

                if (cancel)
                    _closed = true;
            }

            if (cancel)
                Cancellation.Cancel();
        }
    }

    // Reports synchronously so a pull error is captured before CreateImageAsync returns.
    private sealed class ImagePullProgress(ILogger logger, string image) : IProgress<JSONMessage>
    {
        private volatile bool _failed;

        public bool Failed => _failed;

        public void Report(JSONMessage message)
        {
            if (message.Error is not null)
            {
                _failed = true;
                logger.LogWarning("Failed to pull image {Image}: {Error}", image, message.Error.Message);
                return;
            }

            if (!string.IsNullOrEmpty(message.Status))
                logger.LogDebug("Pulling image {Image}: {Status}", image, message.Status);
        }
    }

    private CreateContainerParameters GetCreateContainerParameters(GZCTF.Models.Internal.ContainerConfig config) =>
        new()
        {
            Image = config.Image,
            Labels =
                new Dictionary<string, string>
                {
                    ["TeamId"] = config.TeamId,
                    ["UserId"] = config.UserId.ToString(),
                    ["ChallengeId"] = config.ChallengeId.ToString()
                },
            Name = DockerMetadata.GetName(config),

            // GZCTF_FLAG is Per-team dynamic flag issued & audited by the platform.
            //
            // Compliance & Abuse Notice:
            //
            // These env vars are integral to anti-abuse, audit trails and license compliance under
            // the Restricted License (LicenseRef-GZCTF-Restricted). Unauthorized removal, renaming
            // or semantic alteration can indicate an attempt to bypass license terms or weaken
            // challenge isolation guarantees. Downstream extensions MUST preserve their semantics.
            // Modification without a valid authorization may be treated as misuse.
            //
            // References: NOTICE, LICENSE_ADDENDUM.txt, licenses/LicenseRef-GZCTF-Restricted.txt
            Env = BuildContainerEnv(config),
            HostConfig = new()
            {
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                NetworkMode = _meta.NetworkNames[config.NetworkMode]
            }
        };

    private static IList<string> BuildContainerEnv(GZCTF.Models.Internal.ContainerConfig config)
    {
        var env = new List<string>(5)
        {
            $"GZCTF_TEAM_ID={config.TeamId}",
            $"GZCTF_USER_ID={config.UserId}",
            $"GZCTF_CHALLENGE_ID={config.ChallengeId}"
        };

        if (config.GameId is int gameId)
            env.Add($"GZCTF_GAME_ID={gameId}");

        if (!string.IsNullOrWhiteSpace(config.Flag))
            env.Add($"GZCTF_FLAG={config.Flag}");

        return env;
    }
}
