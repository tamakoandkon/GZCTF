# Dynamic-instance AI identification probe

This optional feature asks AI agents that read an instance's HTTP response
headers to identify themselves on later requests. A matching `X-LLM-Id` request
header creates a **review signal** in Game → Monitor → Possible AI Use. It does
not prove that a player used AI: a person can send that header, and many agents
never see response headers.

## Requirements

- Kubernetes container provider. The current Docker provider rejects instances
  with AI detection enabled.
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

Use an address reachable from challenge Pods. The callback URL should resolve
to the platform API, not to a challenge. For isolated challenge networks, use
an IP address so GZCTF can create a narrow egress policy for the callback.
The proxy image must be available to every challenge node.

When an enabled instance starts, GZCTF adds a proxy container to its Pod and
maps the public Service to the proxy port. The challenge port is not published.
The proxy adds `X-LLM-Agent-Instruction` to normal HTTP responses and reports
the first 20 distinct model identifiers it sees on that instance. The report
token is unique to the instance and is never exposed in the public response.

The game monitor shows the team, challenge, reported model or agent, and first
observation time. Since a team shares an instance, the signal cannot identify
one specific team member. Existing instances retain their original access path;
restart an instance after switching detection on or off. WebSocket upgrades
are forwarded, but their handshake responses do not carry the instruction.
