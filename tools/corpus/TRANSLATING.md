# Translating Schedule I for Polyglot

Schedule I is a darkly comedic crime game: the player builds a drug empire in the fictional
city of Hyland Point — growing, cooking and mixing products, dealing to customers, hiring
dealers and employees, dodging police and the Benzies cartel. Tone: casual, cheeky, street
slang in dialogue, crude humor is intentional. Keep the tone; don't sanitize or moralize.

## Input / output

Each input line of `corpus/chunks/NN.jsonl`:
`{"id": 17, "key": "English text", "cat": "ui|item|quest|place|dialogue|code|runtime|data|enum", "ctx": "where it appears"}`

Write `corpus/tr/<lang>/NN.jsonl`, one line per input line, same order:
`{"id": 17, "t": "translated text"}` — valid JSON, UTF-8, `\n` for line breaks.

## Hard rules (a validator rejects violations)

1. Keep every placeholder exactly: `<PRICE>`, `<NAME>`, `<ARG0>`, `<QUANTITY>` … (UPPER_CASE in angle
   brackets). You may move them to fit grammar. Never translate or rename them.
2. Keep number slots `{0}`, `{1}` … exactly (they stand for numbers).
3. Keep rich-text / markup tags exactly and around the matching words: `<color=#54E717>…</color>`,
   `<color=red>`, `<b>…</b>`, `<i>`, `<u>`, `<size=..>`, `<h1>…</h>`, `<h2>`, `<h3>`, and input tokens like
   `<Input_Interact>` (they become key names). Same tags, same count.
4. Keep the same number of line breaks (`\n`) and the leading/trailing spaces of the key.
5. ALL-CAPS keys → ALL-CAPS translation. Keys ending with `:` / `...` / `?` keep that punctuation.
6. Never leave `t` empty. If a key must stay as is (a proper name, code, unit), copy it unchanged.

## Style rules

- Proper names stay in the original Latin spelling: people (Uncle Nelson, Benji Coleman), brands and
  stores (Gas-Mart, Dan's Hardware, Taco Ticklers, Ray's Realty, Thrifty Threads), the Benzies cartel,
  places (Hyland Point, Northtown, Westville, Downtown, Docks, Suburbia, Uptown), drug strain and
  product names (OG Kush, Sour Diesel, Granddaddy Purple, Baby Blue, Biker Crank, Glass), vehicle models
  (Shitbox, Veeper, Hounddog…), game title SCHEDULE I. Translate the generic part around them
  ("OG Kush Seed" → seed word translated + "OG Kush").
- Ranks (Street Rat, Hoodlum, Peddler, Hustler, Bagman, Enforcer, Shot Caller, Block Boss, Underlord,
  Baron, Kingpin) ARE translated (they are titles) — pick a punchy, consistent equivalent; keep Roman
  numeral tiers (I–V).
- UI labels (cat ui/enum): short, same register as typical games in that language, imperative for
  buttons ("Continue", "Buy"). Keep them roughly as short as the English.
- Be consistent: the same English term → the same translation everywhere. Maintain
  `corpus/tr/<lang>/glossary.md` (English → translation) for recurring game terms and consult it.
- `ctx` tells where the text is shown; use it for gender/number/case and for button vs sentence.
- Dialogue: natural spoken language; NPCs use slang; the player is addressed informally unless the
  NPC is formal (shopkeepers, police may be formal).
- Measurement and money formats stay as in the key (`$`, `g`, `mph`, `°F`, `oz`).
