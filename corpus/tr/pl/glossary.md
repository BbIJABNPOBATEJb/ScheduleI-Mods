# Polish glossary (pl)

Consult this before translating a chunk; follow it strictly. Add new recurring terms as you go (never change existing ones).

## Conventions

- **Addressing the player: always informal "ty"**, in UI and in dialogue: "Kliknij", "Wybierz", "Czy na pewno?",
  "Natychmiast staną się wobec ciebie wrodzy." Pronouns lowercase ("ty", "ciebie", "twój"), never "Ty/Twój".
- Formal NPCs (police, shopkeepers, bank/real-estate staff) stay polite through "proszę" + infinitive or impersonal
  forms ("Proszę się zatrzymać", "Czym mogę służyć?"). Do NOT use "Pan/Pani" (player gender is unknown).
- Avoid gendered 2nd-person past forms ("zrobiłeś/zrobiłaś") in UI: use impersonal/neutral forms ("Odblokowano",
  "Zdobyto", "Awans!"). In dialogue, if a gendered form is unavoidable, use the masculine.
- **Buttons, actions, input prompts: 2nd-person singular imperative** (Polish PC-UI standard): "Kup", "Zamknij",
  "Anuluj", "Wróć", "Zastosuj ustawienia", "Celuj", "Skocz". Never infinitive ("Kupić") for buttons.
  Settings options, tabs, headers, column titles: **nouns** ("Dźwięk", "Jasność", "Kategoria", "Stawka").
- **"(Hold) X" → "(Przytrzymaj) X"** with X in imperative: "(Przytrzymaj) Spożyj", "(Przytrzymaj) Wciągnij".
  "Click" → "Kliknij"; "Click and hold" → "Kliknij i przytrzymaj"; "Press" → "Naciśnij"; "Hold" → "Przytrzymaj".
  "X to Y" instruction → "Kliknij X, aby Y" (comma before "aby").
- Capitalization: English Title Case → Polish sentence case ("Assigned Packaging Stations" → "Przypisane stacje
  pakowania"). ALL-CAPS keys stay ALL-CAPS (with Polish diacritics: "ZAMKNIĘTE", "WIĘCEJ").
- Enum/status values that must fit any NPC gender are **nouns** (relationship statuses). Adjective labels agree with
  their implied noun: product effects → masculine ("efekt"), face expressions → masculine ("wyraz twarzy"),
  quality tiers → feminine ("jakość"), vote status → neuter ("głosowanie": "AKTYWNE", "ZAMKNIĘTE").
- Numbers with nouns: avoid Polish numeral agreement traps with `{0}` by using forms that work for any number:
  "+ jeszcze {0}", "Ilość: {0}", "{0}x", "Klienci: {0}". Money stays "$" in front: "${0}", "+${0} premii".
- Rank placeholders (`<REQUIRED_RANK>`) must stay nominative: "Dostępne od rangi: <REQUIRED_RANK>".
- Ellipsis stays three dots "..." (not "…"). Keep "x" multipliers ("{0}x").
- Proper names, brands, stores, places, strains stay in Latin/original: Benzies, Gas-Mart, Northtown, Northville,
  Downtown, Westville, Docks, Suburbia, Uptown, Hyland Point, Bleuball's Boutique, Bud's Bar, Albert Hoover,
  OG Kush, Sour Diesel, Granddaddy Purple, Green Crack, Baby Blue, Biker Crank, Glass, SpeedGrow, PGR, Cuke,
  Addy, Viagor, Mega Bean, Tropic Thunder, Shitbox, Veeper, Hounddog, Bungalow. Generic parts get translated:
  "OG Kush Seed" → "Nasiona OG Kush", "Bleuball\nBoutique" stays as is. Use "w Gas-Mart" (no inflection of brands).
- Map labels split by "\n": keep the same number of line breaks, split where Polish reads naturally
  ("Basketball\nCourt" → "Boisko do\nkoszykówki").
- Crude humor stays crude; slang welcome in dialogue (trawka, zioło, towar, hajs, psy/gliny for police in NPC mouths).

## Ranks (Roman numeral tiers I–V kept: "Inkasent III"; always nominative, capitalized)

| English | Polish |
|---|---|
| Street Rat | Uliczny szczur |
| Hoodlum | Dresiarz |
| Peddler | Handlarz |
| Hustler | Kombinator |
| Bagman | Inkasent |
| Enforcer | Egzekutor |
| Shot Caller | Szycha |
| Block Boss | Boss osiedla |
| Underlord | Władca podziemia |
| Baron | Baron |
| Kingpin | Ojciec chrzestny |
| Rank / rank up | ranga / awans ("Awans!", "Awansuj na wyższą rangę") |
| XP | XP |

## Employees, contacts

| English | Polish |
|---|---|
| Employee / Employees | pracownik / Pracownicy |
| Botanist | botanik (pl. botanicy) |
| Chemist | chemik (pl. chemicy) |
| Packager / Handler | pakowacz (pl. pakowacze) |
| Cleaner | sprzątacz (pl. sprzątacze) |
| Driver | kierowca |
| Fixer (Manny) | załatwiacz |
| Dealer (player's street dealer; also the "Dealer" UI label) | diler (pl. dilerzy) |
| Arms Dealer | handlarz bronią |
| Supplier | dostawca ("Weed Supplier" → "dostawca trawki") |
| Customer | klient (pl. klienci) |
| Contact / Contacts (app) | kontakt / Kontakty |
| Assign / Assigned | przypisz / przypisany ("Przypisz klienta", "Przypisane stacje") |
| Hire / Fire | zatrudnij / zwolnij |
| Wage / Daily wage | pensja / dniówka |
| Locker (employee) | szafka |
| Bed (employee) | łóżko |
| Host (multiplayer) | host |
| Summon | wezwij / wezwać |

## Relationship / cartel status (nouns, gender-neutral; one translation per key)

| English | Polish |
|---|---|
| Hostile / HOSTILE | Wrogość / WROGOŚĆ |
| Unfriendly | Niechęć |
| Neutral (relationship) | Neutralność |
| Friendly | Przyjaźń |
| Loyal | Lojalność |
| Truced | Rozejm |
| Defeated (cartel) | Pokonani |
| Unknown / UNKNOWN | Nieznany / NIEZNANY |
| hostile (in a sentence) | wrogi / wrodzy / wrogo nastawieni |
| Relationship | relacje |
| Cartel / the Benzies | kartel / Benzies (plural: "Benzies są...", "z Benzies") |
| Cartel influence | wpływy kartelu ("Wpływy kartelu w Northtown") |
| Agreement (with the Benzies) | układ ("Zerwanie układu z Benzies") |
| Region (Northtown etc.) | dzielnica |

## Product, mixing, packaging

| English | Polish |
|---|---|
| Product (drug for sale) | towar (mass noun; "Twój towar", "Wystaw towar") |
| Product (harvest from plants) | plony ("zebrane plony") |
| Output (station slot label) | Wynik; in sentences "gotowe przedmioty" |
| Destination / Source | miejsce docelowe / źródło |
| Mix (noun, a created product) | mieszanka ("Nowa mieszanka", "Stwórz mieszankę") |
| Mixing (process) / Mix (verb) | mieszanie / zmieszaj |
| Mixer (ingredient for mixing) | składnik ("Włóż składnik") |
| Ingredient | składnik |
| Recipe | przepis |
| Mixing station | stacja mieszania |
| Packaging station | stacja pakowania |
| Chemistry station | stacja chemiczna |
| Lab oven | piec laboratoryjny |
| Cauldron | kocioł (pl. kotły) |
| Brick press | prasa do cegieł |
| Pot (plant) | doniczka |
| Grow tent | namiot do uprawy |
| Drying rack | suszarka |
| Drying | suszenie |
| Mushroom bed | grządka grzybowa |
| Mushroom spawn / spawn station | grzybnia / stacja grzybni |
| Additive (Fertilizer, PGR, SpeedGrow) | dodatek |
| Fertilizer | nawóz |
| Apply (additive) | dodaj ("Dodaj nawóz", "Dodaj PGR") |
| Apply (settings/color) | zastosuj ("Zastosuj ustawienia") |
| Soil | ziemia |
| Seeds | nasiona ("Nasiona koki", "Nasiona OG Kush") |
| Buds / leaves | szczyty / liście |
| Coca / coca leaves | koka / liście koki |
| Weed / Meth / Cocaine / Shrooms | trawka / meta / kokaina / grzybki |
| Pseudo (pseudoephedrine) | pseudo / pseudoefedryna |
| Watering can / Trimmers | konewka / sekator |
| Supplies | zapasy |
| Materials | materiały |
| Item | przedmiot |
| Quality | jakość |
| Property (effect of a product) | efekt ("- Efekt") |
| Property (real estate) | nieruchomość |
| Effects / Favourite Effects | Efekty / Ulubione efekty |
| Attribute | cecha |
| Addiction / Addictiveness | uzależnienie / uzależnialność |
| Highly addictive | silnie uzależniający |
| Packaging | opakowanie |
| Package / Unpackage (verb) | zapakuj / rozpakuj |
| Baggie | woreczek |
| Jar | słoik |
| Brick | cegła |
| List / unlist (product for sale) | wystaw / wycofaj |
| Allowed Items / Allowed Quality | Dozwolone przedmioty / Dozwolona jakość |
| Blacklist / Whitelist | czarna lista / biała lista |
| Filter | filtr ("Wyczyść filtr") |
| Route | trasa |
| Storage / Stash / Shelf | schowek / schowek / półka |
| Trash / Trash bag / Trash can | śmieci / worek na śmieci / kosz na śmieci |

## Quality tiers (feminine, agree with "jakość")

| English | Polish |
|---|---|
| Trash (quality tier) | Śmieciowa |
| Poor | Słaba |
| Standard | Standardowa |
| Premium | Premium |
| Heavenly | Niebiańska |

Customer standards ("Standards" → "Wymagania"; plural adjectives): Very Low / Low / Moderate / High / Very High →
Bardzo niskie / Niskie / Umiarkowane / Wysokie / Bardzo wysokie.

## Effects (product properties; masculine adjectives, nouns where English is a noun)

| English | Polish |
|---|---|
| Anti-gravity | Antygrawitacja |
| Athletic | Atletyczny |
| Balding | Łysienie |
| Bright-Eyed | Błyszczące oczy |
| Calming | Uspokajający |
| Calorie-Dense | Kaloryczny |
| Cyclopean | Cyklopi |
| Disorienting | Dezorientujący |
| Electrifying | Elektryzujący |
| Energizing | Energetyzujący |
| Euphoric | Euforyczny |
| Explosive | Wybuchowy |
| Focused | Skupiający |
| Foggy | Zamglony |
| Gingeritis | Rudzica |
| Glowing | Świecący |
| Jennerising | Jenneryzujący |
| Laxative | Przeczyszczający |
| Lethal | Śmiertelny |
| Long faced | Długolicy |
| Munchies | Gastrofaza |
| Paranoia | Paranoja |
| Refreshing | Orzeźwiający |
| Schizophrenic | Schizofreniczny |
| Sedating | Usypiający |
| Seizure-Inducing | Drgawkowy |
| Shrinking | Kurczący |
| Slippery | Śliski |
| Smelly | Śmierdzący |
| Sneaky | Skryty |
| Spicy | Pikantny |
| Thought-Provoking | Refleksyjny |
| Toxic | Toksyczny |
| Tropic Thunder | Tropic Thunder |
| Zombifying | Zombifikujący |

## Mixer ingredients (item names)

| English | Polish |
|---|---|
| Addy / Cuke / Mega Bean / Viagor | unchanged (brands) |
| Banana | Banan |
| Battery | Bateria |
| Chili | Chili |
| Donut | Pączek |
| Energy Drink | Energetyk |
| Flu Medicine | Lek na grypę |
| Gasoline | Benzyna |
| Horse Semen | Końskie nasienie |
| Iodine | Jod |
| Motor Oil | Olej silnikowy |
| Mouth Wash | Płyn do płukania ust |
| Paracetamol | Paracetamol |

## Deals, crime, cartel, money

| English | Polish |
|---|---|
| Deal (sale to a customer) | transakcja ("Sfinalizuj transakcję", "Transakcja zakończona") |
| Deal (dialogue, slang) | interes / dil |
| Contract | zlecenie |
| Offer / Counteroffer / Counter | oferta / kontroferta / złóż kontrofertę |
| Sample / free sample | próbka / darmowa próbka |
| Handover | przekazanie |
| Order / Active Orders | zamówienie / Aktywne zamówienia |
| Delivery / Arrived (status) | dostawa / Dostarczono |
| Dead drop | skrytka |
| Laundering / laundering operation | pranie pieniędzy / operacja prania |
| Launder (button) | Wypierz |
| Business (laundering front) / Business Management | firma / Zarządzanie firmą |
| Cash | gotówka |
| Online balance | saldo online |
| Deposit / Withdraw | wpłać / wypłać |
| Amount (money) | kwota |
| Amount / Quantity (items) | ilość |
| Price / Asking Price | cena / Żądana cena |
| Bonus | premia ("+${0} premii") |
| Earnings | zarobki |
| ATM | bankomat |
| Cart / Add to Cart | koszyk / Do koszyka |
| Pawn shop | lombard |
| Police / officer | policja / policjant (slang in dialogue: psy, gliny) |
| Curfew | godzina policyjna |
| Body search | rewizja ("BODY SEARCH" → "REWIZJA") |
| BUSTED | WPADKA |
| Arrest / arrested | aresztowanie / aresztowany |
| Charge (offense) | zarzut |
| Penalty / Fine | kara / grzywna |
| Wanted | poszukiwany |
| Conceal | ukryj |

## Casino

| English | Polish |
|---|---|
| Casino | kasyno |
| Blackjack / BLACKJACK! | Blackjack / BLACKJACK! |
| BUST! (over 21) | FURA! |
| Bet / Bet Amount | stawka |
| Bet multiplier | mnożnik stawki |
| BIG WIN! | WIELKA WYGRANA! |
| Better luck next time | Więcej szczęścia następnym razem |
| Ride the Bus | Ride the Bus (game name, unchanged) |

## Settings and generic UI

| English | Polish |
|---|---|
| Settings / Apply Settings | Ustawienia / Zastosuj ustawienia |
| Audio / Ambience | Dźwięk / Dźwięki otoczenia |
| Display / Active Display | Obraz / Aktywny monitor |
| Anti-aliasing | Wygładzanie krawędzi |
| Brightness | Jasność |
| Camera Sensitivity / Camera Bobbing | Czułość kamery / Kołysanie kamery |
| Bindings / Controls | Przypisanie klawiszy / Sterowanie |
| Forward / Backward | Do przodu / Do tyłu |
| Save (game file) / Save (verb) | zapis / Zapisz |
| Load | Wczytaj |
| Backup | kopia zapasowa ("Automatyczne kopie zapisów") |
| Full game | pełna wersja |
| Back / Cancel / Close | Wróć / Anuluj / Zamknij |
| Accept / Confirm | Przyjmij / Potwierdź |
| Begin / Begin Game / Continue | Zacznij / Rozpocznij grę / Kontynuuj |
| Clear | Wyczyść |
| Add / Add New | Dodaj / Dodaj nową (trasę) |
| Answer (phone) | Odbierz |
| Bug / Bug Report/Feedback | Błąd / Zgłoś błąd/opinię |
| Character (creator) | Postać |
| Body / Clothing / Hair | Ciało / Ubrania / Włosy |
| Basic / Advanced (enum) | Podstawowy / Zaawansowany |
| Active / Closed (vote) | AKTYWNE / ZAMKNIĘTE |
| Quest / Journal | zadanie / Dziennik |
| Phone / Messages / Map | telefon / Wiadomości / Mapa |
| SPACEBAR | SPACJA |
| Left/right mouse button | lewy/prawy przycisk myszy |

## Added while translating chunks 11–19

| English | Polish |
|---|---|
| RV (the player's starting trailer) | kamper ("w kamperze") |
| Motel / Motel Room | motel / pokój w motelu |
| Product Manager (app) | Menedżer towaru ("w aplikacji Menedżer towaru") |
| Deliveries (app) | Dostawy ("w aplikacji Dostawy") |
| Deal window: Morning / Afternoon / Night / Late Night | Poranek / Popołudnie / Noc / Późna noc |
| Suspension Rack | stelaż |
| Grow Light | lampa do uprawy |
| AC Unit | klimatyzator |
| Spray Bottle | spryskiwacz |
| Checkpoint (police) | punkt kontrolny |
| Sewer / sewer (access) key | kanały / klucz do kanałów |
| Stash box (supplier's payment box) | schowek |
| Tab (debt, "put it on my tab") | rachunek |
| Trash for Cash (machine) | Trash for Cash (unchanged) |
| Singleplayer / Multiplayer | tryb jednoosobowy / tryb wieloosobowy |
| Dialogue slang for bro/dawg/homie/mulatto | stary / ziom / ziomek / mordeczko (no literal "mulat") |
| Properties / businesses | Bungalow, Hyland Manor (unchanged); Barn → Stodoła; Car Wash → Myjnia; Laundromat → Pralnia; Post Office → Poczta; Sweatshop → Szwalnia; Storage Unit → Boks magazynowy; Docks Warehouse → Magazyn w Docks ("The Barn (<PRICE>)" → "Stodoła (<PRICE>)") |
| Vehicle models | unchanged, drop "The": Bruiser, Cheetah, Hotbox, Hounddog, Shitbox, Veeper |
| Merchant (warehouse / dark market NPC) | kupiec (NOT "handlarz" — that is the Peddler rank) |
| Mushroom substrate / Grain bag / Spores | podłoże (grzybowe) / worek z ziarnem / zarodniki |
| `formal_address` (runtime token in Herbert's lines) | keep unchanged: "Dziękuję, formal_address" |
| `<LOCATION>` in customer lines ("I'll meet you <LOCATION>") | location descriptions are prepositional phrases ("za Handy Hank's"), so "spotkajmy się <LOCATION> między <WINDOW_START> a <WINDOW_END>" |
| `<BUSINESS>`/`<PROPERTY>`/`<VEHICLE>` names | keep nominative via constructions like "Czyli interesuje cię <PROPERTY>?" |
| Crimes (charges) | Assault → Napaść; Assault with a deadly weapon → Napaść z bronią w ręku; Drug trafficking → Handel narkotykami; Brandishing a weapon → Wymachiwanie bronią; Discharge of a firearm in a public place → Oddanie strzału w miejscu publicznym; Attempting to sell illicit items → Próba sprzedaży nielegalnych przedmiotów |
| Blackjack dealer (casino) | krupier ("Ruch krupiera...") — NOT "diler" |
| Card suits | Clubs / Diamonds / Hearts / Spades → Trefl / Karo / Kier / Pik |
| Loan sharks | lichwiarze |
| Time abbreviations with numbers | "{0} godz.", "{0} min", "<NUM> dni" (avoid numeral agreement) |
| Body shop | warsztat |
