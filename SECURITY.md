# Security

Zaturnava is a hobby project, but the live server (<https://zaturnava.runasp.net/>, the game on itch.io) is
played by people, and their seats, sign-ins and talk should be safe.

## Reporting a weakness

Please report it privately, not in a public issue: through this repository's **Security** tab, **Report a
vulnerability**, or by e-mail to the maintainer listed on the GitHub profile of
[nilvon9wo](https://github.com/nilvon9wo). Say what you found, how to reproduce it, and what it lets someone do.
You will have an answer as soon as the maintainer can give one; there is no bounty.

Please do not test against the live server in ways that harm other players or the host (no denial of service,
no taking others' seats or accounts): a server of your own, run from this repository
([running the game](docs/contribute/local-development.md#running-the-game)), is the place for that.

## What the server protects

How seats, sign-ins, cross-origin calls and request limits are protected is described in
[Playing on the live server](docs/bots/live-server.md#what-is-protected-and-how); what is kept about people, in
the [privacy notice](docs/privacy.md).
