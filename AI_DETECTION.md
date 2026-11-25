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
  Raw TCP services, including typical Pwn challenges reached with `nc`, do not
  support HTTP response headers. Keep the probe disabled for these challenges;
  placing the HTTP proxy in front of them would break their protocol.
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

## Decoy flags

A request that reports an identifier can also be handed a **decoy flag**. When
that request's response is about to carry the instance's real flag, the proxy
replaces the value in that one response with a random string of the same length
and character classes, then reports the pair to the platform. The challenge
container keeps the real flag: the file on disk is never modified, a request
without the header still receives the real value, and a teammate who never sends
the header is unaffected.

Only responses that can be rewritten safely are touched:

- `text/*`, JSON, JavaScript or XML, without `content-encoding`;
- `204`, `206`, `304` and `HEAD` are skipped;
- `text/event-stream` is never buffered;
- a declared length above 2 MiB, or a body that stops sending for 250 ms, falls
  back to plain streaming and is forwarded unchanged.

`Content-Length` is preserved because the replacement has the same byte length,
so clients and chunked encodings see no difference. Challenge uploads are not
affected: request bodies are still piped without buffering or size limits.

The platform stores team, challenge, reported identifier, issue time and the
value itself. Submitting that exact value is shown in **Monitor → Submissions**
as a `MF` badge, and **Monitor → Possible AI Use** marks the row as submitted.
The submitter still receives an ordinary wrong-answer result; no new status is
exposed to players or to the realtime feed.

Limits worth keeping in mind:

- the flag has to appear in an HTTP text body; Pwn/TCP services, static flags and
  flags embedded in images or archives cannot be replaced;
- an identifier header can be typed by a person, and a decoy value can be copied
  by hand, so a hit is evidence to review, not a verdict;
- decoys issued before a restart keep their rows, but an instance only receives
  them after it is recreated with detection enabled;
- the proxy sidecar now needs 256 MiB of memory because it may buffer a response
  body.
