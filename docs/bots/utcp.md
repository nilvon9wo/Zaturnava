# Playing through UTCP

The [Universal Tool Calling Protocol](https://www.utcp.io) describes tools as calls a client makes itself: the
server publishes a manual, and the client calls each tool's own address directly, with no server of tools in
between. The game server publishes one at `utcp`:

```text
https://zaturnava.runasp.net/utcp
```

(or `http://localhost:5164/utcp`, for [a server of your own](README.md#running-a-server)). A UTCP client takes that
address as an HTTP manual:

```json
{
  "manual_call_templates": [
    {
      "name": "zaturnava",
      "call_template_type": "http",
      "url": "https://zaturnava.runasp.net/utcp",
      "http_method": "GET"
    }
  ]
}
```

## The tools

The manual offers the same tools as [the MCP server](mcp.md), by the same names (`catalog`, `list_games`,
`create_game`, `claim_seat`, `get_game`, `game_history`, `advance_game`, `submit_action`, `read_chat`, `say`), each
one route of [the HTTP API](http-api.md), whose answers they are. A tool's inputs go where its call template says:

| Input | Goes |
|---|---|
| `gameId`, `seat` | into the address, for `{gameId}` and `{seat}` |
| `body` | as the JSON body, for the tools that post one (`body_field`) |
| `Authorization` | as the header of that name (`header_fields`): `Bearer {token}`, the seat's token from `claim_seat`, or, to claim a seat for a player, their AI token |
| anything else | into the query (`after`, for `read_chat`; `game`, `ruleset`, `open` and `page`, for `list_games`) |

A refused call answers as the HTTP API does: its status, and a problem saying which refusal and why
([refusals](http-api.md#refusals)). The manual is UTCP 1.0.1 (*Proposed*); its own version (`manual_version`)
rises when its tools change.

## A bot to start from

[UtcpSampleBot](../../samples/UtcpSampleBot/README.md) reads the manual and plays by it: a complete C# bot, with the
same mechanical mind as the [HTTP](../../samples/HttpApiSampleBot/README.md) and
[MCP](../../samples/McpSampleBot/README.md) samples.
