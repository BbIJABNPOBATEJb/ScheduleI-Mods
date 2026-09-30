# Turkish glossary (tr)

Consult this before translating a chunk; add new recurring terms as you go. It is binding:
the same English term always gets the translation listed here.

## Conventions

- **Address: "sen" everywhere by default.** The game is casual and cheeky, so UI instructions and
  system text use informal 2nd person singular: "Tıkla", "Seç", "Basılı tut", "Emin misin?",
  "Raporunla birlikte...". Never mix "sen" and "siz" inside one string.
- **Dialogue:** NPCs say "sen" to the player and use street slang ("lan", "kanka", "abi" where it fits
  the character). Formal NPCs (police officers, bank/shop clerks, the realtor, hospital) use "siz".
  Keep crude humor; do not sanitize.
- **Buttons / actions:** bare imperative (verb stem), as short as the English: "Satın Al", "Kapat",
  "Geri", "İptal", "Kabul Et", "Ayarları Uygula", "Sepete Ekle", "Müşteri Ata".
- **Capitalization:** mirror the key. Title Case keys → Turkish Title Case (every word capitalized:
  "Aktif Siparişler", "İzin Verilen Kalite"; conjunctions "ve", "ile", "ya da" stay lowercase).
  Sentence-case keys → sentence case ("Filtreyi temizle", "Satılık işletmeler").
- **"(Hold) X" prompts:** "(Basılı Tut) <eylem>", e.g. "(Basılı Tut) Tüket", "(Basılı Tut) Zıpla".
  "Click" → "tıkla"; "Click and hold" → "tıklayıp basılı tut"; "Press <key>" → "<key> tuşuna bas";
  "Hold <key>" → "<key> tuşunu basılı tut".
- **ALL-CAPS keys → ALL-CAPS Turkish with Turkish casing rules:** i → **İ**, ı → **I**
  (dotted/dotless pairs never swap), plus Ç Ğ Ö Ş Ü. Examples: "AKTİF", "İPTAL", "KAPALI",
  "ÜST ARAMASI", "BAHİS ÇARPANI", "BİR DAHAKİ SEFERE...", "ENSELENDİN". Never write "IPTAL" or "AKTIF".
  Beware: a naive `str.upper()` turns i into I — type the caps by hand or map i→İ first.
- **Suffixes on Latin proper names:** attach with an apostrophe, following the English pronunciation for
  vowel harmony / consonant hardening: "Gas-Mart'ta", "Northtown'da", "Downtown'a", "Westville'de",
  "Northville'deki", "Benzies'in", "Docks'ta", "Uptown'dan", "Suburbia'da", "Albert'in".
- **Proper names stay Latin:** people, stores/brands (Gas-Mart, Dan's Hardware, Bleuball's Boutique,
  Bud's Bar, Taco Ticklers, Ray's Realty, Thrifty Threads), places (Hyland Point, Northtown, Westville,
  Downtown, Docks, Suburbia, Uptown, Northville), the Benzies, strains/products (OG Kush, Sour Diesel,
  Granddaddy Purple, Baby Blue, Biker Crank, Glass), brand-name additives (PGR, SpeedGrow), vehicles,
  casino game names (Blackjack, Ride the Bus), SCHEDULE I.
- **Punctuation / formats:** keep "..." as three dots; keep trailing ":" "?" "!"; money stays "$" in
  front ("${0}", "+${0} Bonus"); units stay as in the key (g, oz, mph, °F). Dates: day before month
  ("{0} Ocak'ta geliyor"). Month names lowercase inside sentences except when the key is Title Case.
- Generic map labels ARE translated ("Köprü", "Kanal", "Oto Yıkama", "Basketbol\nSahası");
  named businesses are not ("Bud's\nBar", "Bleuball\nBoutique").

## Ranks (Roman numeral tiers I–V kept: "Tahsildar III")

| English | Turkish |
|---|---|
| Street Rat | Sokak Faresi |
| Hoodlum | Serseri |
| Peddler | Torbacı |
| Hustler | Madrabaz |
| Bagman | Tahsildar |
| Enforcer | Kabadayı |
| Shot Caller | Racon Kesen |
| Block Boss | Mahalle Babası |
| Underlord | Yeraltı Lordu |
| Baron | Baron |
| Kingpin | Kral |
| Rank / Level / Level Up / XP | Rütbe / Seviye / Seviye Atladın! / XP |

## Employees, contacts, people

| English | Turkish |
|---|---|
| Employee / Hire / Fire | çalışan / İşe Al / Kov |
| Botanist | Botanikçi |
| Chemist | Kimyager |
| Packager / Handler | Paketçi |
| Cleaner | Temizlikçi |
| Dealer (your street seller NPC) | Satıcı |
| Arms Dealer | Silah Satıcısı |
| Supplier | Tedarikçi |
| Weed / Meth / Coke Supplier | Ot / Meth / Kokain Tedarikçisi |
| Customer | Müşteri |
| Contact(s) | Kişi(ler) |
| Assign / Assigned | Ata / Atanan |
| Wage (daily) | günlük ücret |
| Bed (employee) | yatak |
| Host (multiplayer) | host |
| Police / Officer | polis / memur |

## Relationship status (customers/NPCs)

| English | Turkish |
|---|---|
| Hostile | Düşman |
| Unfriendly | Soğuk |
| Neutral | Nötr |
| Friendly | Dost |
| Loyal | Sadık |
| Relationship | İlişki |
| Unlocked / Locked | Kilidi Açık / Kilitli |

## Cartel (Benzies)

| English | Turkish |
|---|---|
| Cartel | kartel |
| Cartel influence / Influence | kartel nüfuzu / nüfuz ("Northtown'da Kartel Nüfuzu") |
| Status: Unknown | Bilinmiyor |
| Status: Truced | Ateşkes |
| Status: Hostile | Düşman |
| Status: Defeated | Yenildi |
| Agreement (with the Benzies) | anlaşma ("anlaşmayı bozmak" = call off) |
| hostile (in a sentence) | düşman ("sana düşman olacaklar") |

## Product, drugs, quality

| English | Turkish |
|---|---|
| Product (drug for sale) | ürün |
| Product (harvest from plants) | hasat / hasat edilen ürün |
| Weed / Meth / Cocaine / Shrooms | ot / meth / kokain / mantar |
| Buds / Leaves | tomurcuk / yaprak |
| Coca leaves / Coca seeds | Koka Yaprakları / Koka Tohumları |
| Seed | tohum ("OG Kush Tohumu") |
| Quality | kalite |
| Quality tiers: Trash / Poor / Standard / Premium / Heavenly | Çöp / Kötü / Standart / Premium / Cennetlik |
| Effect / Property (of a product) | etki |
| Attribute | özellik |
| Addiction | Bağımlılık |
| Addictiveness | Bağımlılık Gücü |
| Listed / Unlisted (product manager) | listede / listede değil; list/unlist → listeye ekle / listeden çıkar |
| Item | eşya |
| Allowed Items / Allowed Quality | İzin Verilen Eşyalar / İzin Verilen Kalite |
| Blacklist / Whitelist | Kara Liste / Beyaz Liste |
| Filter / Clear filter | filtre / Filtreyi temizle |

## Effects (product properties; adjectives, Title Case in UI)

| English | Turkish |
|---|---|
| Calming | Sakinleştirici |
| Munchies | Acıktırıcı |
| Smelly | Kokuşuk |
| Energizing | Enerji Verici |
| Euphoric | Öforik |
| Focused | Odaklayıcı |
| Refreshing | Ferahlatıcı |
| Sneaky | Sinsi |
| Sedating | Uyutucu |
| Paranoia | Paranoya |
| Toxic | Zehirli |
| Laxative | Müshil |
| Athletic | Atletik |
| Balding | Saç Döktürücü |
| Bright-Eyed | Parlak Gözlü |
| Calorie-Dense | Kalorili |
| Disorienting | Sersemletici |
| Electrifying | Elektriklendirici |
| Explosive | Patlayıcı |
| Foggy | Sisli |
| Glowing | Parlayan |
| Long Faced | Uzun Suratlı |
| Schizophrenic | Şizofrenik |
| Seizure-Inducing | Nöbet Getirici |
| Shrinking | Küçültücü |
| Slippery | Kaygan |
| Spicy | Acılı |
| Thought-Provoking | Düşündürücü |
| Zombifying | Zombileştirici |
| Anti-Gravity / Cyclopean / Gingeritis / Jennerising / Tropic Thunder | Yerçekimsiz / Tepegöz / Kızıllaştırıcı / Jennerising / Tropic Thunder |

## Mixing, growing, stations

| English | Turkish |
|---|---|
| Mix (noun) / mix (verb) / mixing | karışım / karıştır / karıştırma ("Yeni Karışım") |
| Mixer (mixing ingredient) / Ingredient | karışım malzemesi (short: malzeme) |
| Mixing station | Karıştırma İstasyonu |
| Additive (Fertilizer, PGR, SpeedGrow) | katkı ("Katkı {0}") |
| Apply (additive) | Uygula ("Gübre Uygula", "PGR Uygula") |
| Fertilizer | Gübre |
| Soil | toprak |
| Pot | saksı |
| Water (verb) | sula |
| Harvest | hasat et / hasat |
| Grow tent / Grow light | yetiştirme çadırı / yetiştirme lambası |
| Drying rack / drying | Kurutma Rafı / kurutma |
| Mushroom bed | Mantar Yatağı |
| Mushroom spawn (station) | misel / Mantar Miseli İstasyonu |
| Chemistry station | Kimya İstasyonu |
| Lab oven | Laboratuvar Fırını |
| Cauldron | Kazan |
| Brick press | Kalıp Presi |
| Station | istasyon |
| Output (of a station) | çıktı |
| Destination | hedef |
| Supplies / Materials | malzemeler / gerekli malzemeler |
| Recipe | tarif |

## Packaging

| English | Turkish |
|---|---|
| Packaging station | Paketleme İstasyonu |
| Package / Unpackage (verb) | Paketle / Paketten Çıkar |
| Packaging (material) | ambalaj |
| Baggie | Poşet |
| Jar | Kavanoz |
| Brick | Kalıp |
| Unpackaged | Paketlenmemiş |

## Deals, crime, money

| English | Turkish |
|---|---|
| Deal | anlaşma |
| Contract | sözleşme |
| Order / Active Orders | sipariş / Aktif Siparişler |
| Offer / Counter(-offer) | teklif / karşı teklif |
| Sample (free) | numune ("Numune Ver") |
| Handover | teslim |
| Delivery / Arrived | teslimat / Ulaştı |
| Payment / Bonus | ödeme / Bonus |
| Asking Price / Price | İstenen Fiyat / fiyat |
| Dead drop | zula |
| Stash | saklama yeri |
| Laundering / launder / laundering operation | aklama / akla / aklama operasyonu |
| Business (laundering front) / Business Management | İşletme / İşletme Yönetimi |
| Property (real estate) | mülk (never "etki") |
| Cash / Balance / Deposit / Withdraw | nakit / bakiye / Yatır / Çek |
| Amount (money) | tutar |
| Amount / Quantity (items) | miktar / adet |
| ATM | ATM |
| Cart / Add to Cart | Sepet / Sepete Ekle |
| Body search | Üst Araması |
| BUSTED | ENSELENDİN |
| Arrest / Wanted / Curfew | tutuklama / Aranıyor / sokağa çıkma yasağı |
| Charge (offense) / Penalty / Fine | suçlama / ceza / para cezası |
| Conceal | sakla |

## Casino

| English | Turkish |
|---|---|
| Casino | Kumarhane |
| Blackjack / BLACKJACK! | Blackjack / BLACKJACK! |
| BUST! | PATLADIN! |
| Hit / Stand | Kart Çek / Dur |
| Bet / Bet Amount | bahis / Bahis Miktarı |
| Bet multiplier | Bahis Çarpanı |
| BIG WIN! | BÜYÜK KAZANÇ! |
| Slot machine | slot makinesi |

## Settings and generic UI

| English | Turkish |
|---|---|
| Settings / Apply Settings | Ayarlar / Ayarları Uygula |
| Audio / Ambience | Ses / Ortam Sesleri |
| Display / Active Display | Ekran / Aktif Ekran |
| Anti-aliasing | Kenar Yumuşatma |
| Brightness | Parlaklık |
| Camera Sensitivity / Camera Bobbing | Kamera Hassasiyeti / Kamera Sallanması |
| Bindings / Controls | Tuş Atamaları / Kontroller |
| Forward / Backward / Left / Right | İleri / Geri / Sol / Sağ |
| SPACEBAR | BOŞLUK |
| Save (game file) / overwrite | kayıt / üzerine yaz |
| Backup / Auto Backup Saves | yedek / Kayıtları Otomatik Yedekle |
| Full game | tam sürüm |
| Back / Cancel / Close / Accept / Begin | Geri / İptal / Kapat / Kabul Et / Başla |
| Add / Add New / Clear | Ekle / Yeni Ekle / Temizle |
| Active / Closed | Aktif / Kapalı |
| Are you sure? | Emin misin? |
| Bug report / feedback | hata bildirimi / geri bildirim |
| Character / Body / Clothing | Karakter / Vücut / Giyim |
| Face expression (e.g. Agitated) | adjective: "Gergin" |
| Basic / Advanced (enum) | Temel / Gelişmiş |
| Quest / Objective / Reward | görev / görev hedefi / ödül |
| Confirm / Continue / Done / Next | Onayla / Devam Et / Tamam / Sonraki |
| Decline / Delete / Exit / Leave | Reddet / Sil / Çık / Ayrıl |
| Import / Export | İçe Aktar / Dışa Aktar |
| Journal (phone app) | Günlük |
| Messages / Contacts / Map / Deliveries (phone apps) | Mesajlar / Kişiler / Harita / Teslimatlar |
| Interact | Etkileşim |
| Gamepad / Mouse | Oyun Kumandası / Fare |
| D-Pad Up/Down/Left/Right | D-Pad Yukarı/Aşağı/Sol/Sağ |
| Key names (Enter, Esc, Control, Tab, Shift) | kept as is |

## Mixing ingredients (item names; brand names kept)

| English | Turkish |
|---|---|
| Cuke / Viagor / Addy | Cuke / Viagor / Addy |
| Banana | Muz |
| Paracetamol | Parasetamol |
| Donut | Donut |
| Mouth Wash | Gargara |
| Flu Medicine | Grip İlacı |
| Gasoline | Benzin |
| Energy Drink | Enerji İçeceği |
| Motor Oil | Motor Yağı |
| Mega Bean / Mega Beans | Mega Fasulye |
| Chili | Acı Biber |
| Battery | Pil |
| Iodine | İyot |
| Horse Semen | At Spermi |

## Money / misc (added in chunks 01+)

| English | Turkish |
|---|---|
| Online balance | online bakiye ("Online Bakiye") |
| Clean Cash | Temiz Para |
| Fair price | Makul fiyat |
| Market Value | Piyasa Değeri |
| Net worth | Net servet |
| Heroin / MDMA / Liquid Meth | Eroin / MDMA / Sıvı Meth |
| Dealer Management / Dealer Earnings | Satıcı Yönetimi / Satıcı Kazançları |
| Launder (button) / Laundering operation | Akla / Aklama Operasyonu |
| Loading Dock | Yükleme Rampası |
| Hardware Store / Gas Station | Nalbur / Benzin İstasyonu |
| Curfew | sokağa çıkma yasağı ("SOKAĞA ÇIKMA YASAĞI") |
| Deal window: Morning / Afternoon / Night / Late Night | Sabah / Öğleden Sonra / Gece / Gece Yarısı |
| Skateboard / Cruiser / Lightweight Board / Golden Skateboard | Kaykay / Cruiser / Hafif Kaykay / Altın Kaykay |

## Dialogue & story terms (added in chunks 11+)

| English | Turkish |
|---|---|
| boss (employees/dealers → player) | patron |
| bro / dawg / dog / homie (Bro customers) | kanka; man / dude / buddy → dostum; brody / mulatto → kardeşim / birader; partner (Stan, Mick, Dan) → ortak |
| Thomas Benzies / cartel boss → player; realtor, car salesman, motel clerk, police, Albert Hoover | formal "siz" (generic SupplierModule lines shared by all suppliers stay "sen") |
| Pseudo (meth precursor) | psödo ("Psödo") |
| RDX | RDX |
| Sewer / sewer key | kanalizasyon / kanalizasyon anahtarı |
| Stash box (supplier's) | saklama kutusu |
| Tab (supplier credit, "put it on my tab") | veresiye hesabı / hesap ("hesabıma yaz") |
| RV | karavan |
| Motel / motel room / motel office | motel / motel odası / motel ofisi |
| Checkpoint | kontrol noktası |
| Locker (employee) | dolap |
| Mushroom spawn / shroom spawn / substrate | misel / mantar miseli / substrat ("Mantar Substratı") |
| Barn (property) | Ahır |
| Pawn / pawn shop | rehin bırak / rehinci |
| Product manager app / deliveries app / contacts app | ürün yöneticisi uygulaması / Teslimatlar uygulaması / Kişiler uygulaması |
| Weekly deposit limit | haftalık yatırma limiti |
| Free sample | bedava numune |
| "meet you <LOCATION> between <WINDOW_START> and <WINDOW_END>" | "<WINDOW_START> ile <WINDOW_END> arasında <LOCATION> buluşalım" (LOCATION = locative phrase, e.g. "motel ofisinin arkasında") |
| formal_address (runtime token, Herbert) | keep literally: formal_address |
| Vehicle choice "The Bruiser (<PRICE>)" | drop "The": "Bruiser (<PRICE>)" |
| Fixer (hires employees) | işbitirici |
| Body shop (Marco) | kaportacı |
| Management clipboard | yönetim panosu (short: pano) |
| Loan sharks | tefeciler |
| Warehouse (dark market) | depo |
| Docks Warehouse / Laundromat / Post Office / Storage Unit / Car Wash (properties) | Docks Deposu / Çamaşırhane / Postane / depo ünitesi / Oto Yıkama |
| <PROPERTY_NAME> (Owned) / (Unowned); Owned Vehicle | (Senin) / (Senin Değil); Senin Aracın (see "(Owned)" row below) |
| "<NAME>'s Inventory / Stash / Briefcase" (unknown name, no suffix) | "<FULL_NAME> - Envanter" / "- Saklama Yeri" / "- Evrak Çantası" |
| Supplies stash / supplies source (botanist) | malzeme saklama yeri / malzeme kaynağı |
| Low / moderate / high-severity drug | hafif / orta / ağır sınıf uyuşturucu |
| Lethal (effect) | Ölümcül |
| Ride the Bus: Higher / Lower / Inside / Outside; forfeit | Daha Yüksek / Daha Düşük / Arasında / Dışında; pes et |
| Card suits Hearts / Diamonds / Clubs / Spades; Blackjack dealer | Kupa / Karo / Sinek / Maça; krupiye |
| AC unit / spray bottle / watering can / trimmer | klima / Sprey Şişe (item; in sentences "sprey şişesiyle") / sulama kabı / Budama Makası |
| Ma'am / Sir | Hanımefendi / Beyefendi |
| Payphone | ankesörlü telefon |
| Consume verbs: Smoke / Snort / Consume | Tüttür / Burna Çek / Tüket |
| Employee whiteboard bullets | 3rd person present: "• Tohum eker", "• Bitkileri sular" |
| Split world-sign words (one word per sign object) | Town/Hall → Belediye/Binası; Community/Center → Toplum/Merkezi; Basketball/Court → Basketbol/Sahası; Liquor/Store → İçki/Dükkanı; Chinese/Restaurant → Çin/Restoranı; Post/Office → Posta/Ofisi; Medical → Tıp; BANK → BANKASI; Employee of/the month → Ayın/Elemanı; brand words kept |
| Internal identifiers (CamelCase variables, asset paths, anim triggers, debug labels) | copy unchanged |

## Items, places, locations (added in chunks 05–10)

| English | Turkish |
|---|---|
| Drug properties: Addictive / Highly Addictive / Cerebral / Dissociative / Hallucinogenic / Mild / Overwhelming / Physical / Potent / Psychedelic / Stimulating / Uplifting | Bağımlılık Yapıcı / Yüksek Bağımlılık Yapıcı / Zihinsel / Dissosiyatif / Halüsinojen / Hafif / Ezici / Fiziksel / Güçlü / Psikedelik / Uyarıcı / Moral Verici |
| Marijuana / Methamphetamine / Magic Mushroom | Marihuana / Metamfetamin / Sihirli Mantar |
| Pseudo (Low-/High-Quality) / pseudoephedrine | Psödo (Düşük/Yüksek Kaliteli) / psödoefedrin |
| Acid / Phosphorus / Cocaine Base | Asit / Fosfor / Kokain Bazı |
| Grain bag / Spore syringe / Mushroom substrate | Tahıl Torbası / Spor Şırıngası / Mantar Substratı |
| Plant trimmers / Watering can / Spray bottle | Budama Makası / Sulama Kabı / Sprey Şişe |
| Suspension rack / Storage rack / Storage closet | Askı Rafı / Depo Rafı / Depolama Dolabı |
| AC unit / Trash can / Trash bag / Dumpster | Klima / Çöp Kutusu / Çöp Torbası / Çöp Konteyneri |
| Management clipboard | Yönetim Panosu |
| Supplier's Stash ("Albert Hoover's Stash") | saklama yeri ("Albert Hoover'ın Saklama Yeri") |
| Hardware store / Pawn shop / Dealership / Real estate agent / Fixer | nalbur / rehinci / galeri / emlakçı / işbitirici (Manny) |
| Payphone | ankesörlü telefon |
| Location NAME labels ("Behind bank") | noun phrase: "Banka arkası", "Motelin önü" |
| Location DESCRIPTIONS (lowercase, inserted as <LOCATION>/<DEAD_DROP_DESCRIPTION>) | locative phrase: "bankanın arkasında", "mezarlıkta", "batı köprüsünün altında" |
| "(Owned)" on map POIs | "(Senin)": "Karavan (Senin)" |
| Quality tiers as ColorFont names (Trash/Poor/Standard/Premium/Heavenly) | Çöp / Kötü / Standart / Premium / Cennetlik |
| Albert Hoover (supplier) → player | formal "siz", "efendim" |
