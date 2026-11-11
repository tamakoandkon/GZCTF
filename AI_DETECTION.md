# Dynamic-instance AI identification probe

This optional feature asks AI agents that read an instance's HTTP response
headers to identify themselves on later requests. A matching `X-Lim-Id` request
header creates a **review signal** in Game → Monitor → Possible AI Use. It does
not prove that a player used AI: a person can send that header, and many agents
never see response headers.

## Requirements

- Kubernetes or Docker container provider. Docker runs the probe as a second
  container sharing the challenge container's network namespace. The platform
  needs access to the Docker daemon, and the probe image must be available to it.
- Dynamic container challenge serving plaintext HTTP on its configured port.
  HTTPS inside the challenge container cannot be inspected by this proxy.
- A reachable callback URL and an image containing Node.js 24. Set these under
  `ContainerProvider` in the platform configuration:

```json
{
  "AiDetectionCallbackUrl": "http://192.0.2.10:30000/api/ai-detection/report",
  "AiDetectionProxyImage": "node:24-alpine"
}
```

Use an address reachable from challenge Pods or Docker containers. The callback
URL should resolve to the platform API, not to a challenge. For isolated
challenge networks, use an IP address. Kubernetes creates an egress policy for
the callback address; this exception applies to the entire Pod. Docker's probe
shares the challenge network namespace, so its callback needs an allowed route
to the platform. A Docker isolated bridge with IP masquerading disabled may not
reach the host's external address; bind a small reverse proxy on a bridge
gateway address reachable from both open and isolated challenge networks. If
the challenge must have strictly no egress, leave AI
detection disabled until the reporter is moved to a separate network boundary.
The proxy image must be available to every challenge node or Docker daemon.

When an enabled instance starts, GZCTF adds a proxy container to its Pod or
Docker network namespace and routes the public instance port to the proxy.
The challenge port is not published directly.
The proxy adds `X-LLM-Agent-Instruction` to normal HTTP responses and reports
the first 20 distinct model identifiers it sees on that instance. The report
token is unique to the instance and is never exposed in the public response.
For compatibility, the proxy also accepts `X-LLM-Id` on incoming requests.

The game monitor shows the team, challenge, reported model or agent, and first
observation time. Since a team shares an instance, the signal cannot identify
one specific team member. Existing instances retain their original access path;
restart an instance after switching detection on or off. WebSocket upgrades
are forwarded, but their handshake responses do not carry the instruction.

## Uploads and HTTP errors

The probe streams request bodies without setting a body-size limit. It preserves
the challenge's status code and adds the instruction header to challenge error
responses, including 413. Its own upstream-failure response (502) also carries
the header. A 413 on an instance can still come from a reverse proxy in front
of the instance or from the challenge image itself. If the image uses Nginx,
configure `client_max_body_size` for that challenge's intended uploads; adding
`X-Llm-Agent-Instruction` does not change Nginx's request-size limit. If an
additional Nginx in front of the probe generates a 413 before the probe sees
the request, add the instruction header there with `add_header ... always`.

The GZCTF platform's upload endpoints are separate from challenge-instance
traffic. Administrator asset uploads and game package imports accept a request
body up to 513 MiB, allowing the game's 512 MiB ZIP limit plus multipart
overhead. Appeal PDFs remain limited to 20 MiB. Platform logo and game poster
images retain their existing 3 MiB application limit. Any outer Nginx or
Ingress must allow at least the relevant endpoint limit, or it can return 413
before GZCTF receives the upload.
