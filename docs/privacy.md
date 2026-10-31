# Privacy

What Zaturnava keeps about the people who play it, why, for how long, and how to have it deleted. Zaturnava is a
hobby project, open source under the MIT licence (https://github.com/nilvon9wo/Zaturnava), with no advertising,
no analytics and nothing sold to anyone.

*Last changed 2026-10-31.*

## Playing without signing in

You need no account to play, and today there is none to make. What is kept:

- **On the server, for each game:** the ruleset; which seats are played by people and which by which bots; and
  every action taken in it, in order. Nothing in a game says who a person is, unless they signed in (below). Finished games are kept, and their
  histories are public (at `api/history`), so that anyone, bots' authors included, can study them.
- **On the server, for each seat a person holds:** a fingerprint (a SHA-256 hash) of the seat's secret token, so the
  server can tell the token when your browser shows it, without keeping the token itself; and the nickname you play
  by, if you gave one, shown to everyone as "Nickname (guest)" and kept with the game.
- **On the server, what is said at a table:** each message, the seat that said it, and when. It is deleted once
  every player has left the table. Only players who signed in may speak; a guest reads.
- **In your browser** (its local storage, never sent anywhere but back to this game's server): the token of each
  seat you hold, so that you can come back to it, the nickname you play by, if you gave one, and your settings: the colour theme, sound, how the pieces are
  drawn, the safety that keeps you from taking your own team's pieces, how the log and the talk are shown, and how
  you last set up a new game.
  Clearing the site's data in your browser deletes all of it.

No cookies are set while you play (only while signing in, below), and nothing tracks you from one game, or one site, to another.

## Signing in

Signing in is optional, and is being built: the game page offers to sign in with GitHub, in a window of its own.
Your browser then keeps the token you were given until you sign out, when the server lets it go too, and it stands
for no one any more. Signing in sets one cookie, only for the few minutes it takes, deleted once you are signed in. It keeps, on the server, your id at the provider, the name it shows you by, and a fingerprint (a SHA-256
hash) of the token your browser is then given, as for a seat. A seat you take while signed in also keeps your
player id, so that the others at the table, and anyone watching or listing the games, see your name there; a seat
taken without signing in shows "guest".

When it is finished, signing in with a provider (itch.io, GitHub, Google, Discord, perhaps later Facebook) will
keep:

- your id at that provider, so the server knows you when you come back;
- the name you are shown by, which you may change;
- which games you played, at which seat, and how each ended, for your rating and record.

Your public profile will show only your name, rating, wins, losses and draws, points, recent scores, and links you
chose to add. Your e-mail address, if a provider offers it, is not asked for. You may link more than one provider to
the same player, and only if you choose to.

## The services the game runs on

- **The server** is hosted by MonsterASP.NET, in Europe. Like any web host, it may log requests (your IP address,
  the time, the address asked for) for its own operation and security, under its own policy.
- **The game page** is published on itch.io, and also at the server's own address. A page on itch.io is under
  itch.io's policy too.
- **No bot sends anything anywhere.** Athena, the bot with AI of its own, runs on the server itself. A bot that asks
  an outside AI service would be sent only the position it is asked to play, nothing about the people at the
  table, and would sit at a table only when someone chose it.

## Having your data deleted

- **Without an account:** clearing this site's data in your browser deletes your tokens and settings. Talk at a table
  is deleted by itself, once every player has left. Games keep nothing that says who you are.
- **With an account:** while signed in, press **Delete account** at the top right of the lobby, and confirm. The server
  then forgets you at once: your name, your id at each provider and every sign-in of yours. No one has to do
  anything by hand, and you need no other account to ask. The games you played stay, so that other players' records
  and ratings stay true, but nothing in them says who you were: they show "deleted player" in your seat. (Your
  player id, a random value that no longer leads to anyone, stays beside the seat.)
