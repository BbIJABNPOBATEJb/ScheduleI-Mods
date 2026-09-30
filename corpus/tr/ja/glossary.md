# Japanese glossary (ja)

Read this before you translate a chunk, and follow it strictly. Add new recurring terms as you go. Don't change
existing entries: other chunks already use them.

## Conventions

### Register
- **UI, system text, tooltips, tutorials:** use neutral-polite Japanese, as in typical Japanese PC games.
  - Full sentences use です/ます: "〜が作成されます。", "よろしいですか？".
  - Instructions end in 〜してください: "選択肢を長押しして投票してください".
  - Short hints and descriptions can end without ます or 。 when the English has no period:
    "^必要な材料はここから自動で取り出されます".
- **Labels and titles:** use nouns, with no する and no 。: "依存度", "担当顧客（{0}/{1}）", "進行中の注文".
- **Buttons and actions:** keep them short.
  - Use a Sino-Japanese noun where it reads naturally: 購入, 売却, 追加, 適用, 開始, 承諾, 確定, 完了.
  - Otherwise use the dictionary-form verb: 閉じる, 戻る, 捨てる, 吸う.
  - A verb phrase is fine for object actions: "肥料を与える", "現金を回収", "インターホンを鳴らす".
  - Never write 〜します or 〜しましょう on a button.
- **ALL-CAPS keys:** Japanese has no letter case. Translate them like normal-case keys ("CANCEL" → "キャンセル",
  "BUY" → "購入"), and keep a trailing ！ or ？ if the key has one.
- **Dialogue:** use natural spoken Japanese.
  - NPCs speak casually (タメ口) with slang and rough speech that fits the character: dealers and thugs use
    オレ / お前 / 〜だぜ / 〜じゃねえ, and junkie customers sound jittery.
  - Shopkeepers, clerks, police and officials use polite です/ます. Police can be stiff and formal: "〜しなさい", "止まりなさい！".
  - The player's own choice lines are casual.
  - Avoid あなた in dialogue. Drop "you" where Japanese allows it, or use お前 / あんた / 君 to match the speaker.
  - UI and system text say あなた when "you" is needed at all.
- Keep the humor, crudeness and slang. Don't soften or moralize.

### Punctuation and typography
- Use full-width 。、！？ in sentences, and full-width （） and ： next to Japanese text.
  - "Amount:" → "数量："
  - "Albert Hoover (Weed Supplier)" → "Albert Hoover（大麻サプライヤー）"
- A key made only of proper names and tags is copied unchanged, including its half-width " (Downtown)".
- **Ellipsis:** a key ending in "..." ends in "..." (three ASCII dots): "アイテムを追加...". In dialogue, "..." is
  also used for pauses. Don't use "……".
- Keep the key's slashes "/", list bullets "- ", "• ", leading "^ " / "^" and "+ " prefixes exactly as they are.
- **Spaces:**
  - No spaces between Japanese text and Latin words or placeholders: "Gas-Martで購入", "<NAME>に話しかける",
    "PGRを与える".
  - One space goes before a number slot that follows a label ("添加剤 {0}", "回答 {0}") and before a rank tier ("男爵 III").
- Use 「」 for quoted speech and names of things. Use 『』 for titles of works.
- Keep numbers as Arabic digits. Money stays "$" in front: "${0}", "+${0} ボーナス". Units stay as in the key (g, mph, °F, oz).
- Use counters where Japanese needs them: 個 (items), 人 (people), 件 (entries/orders), 日 (days).
  - "+ {0} Others" → "+ 他{0}件"
  - "Coming January {0}" → "1月{0}日に登場"

### Input prompts and controls
- "(Hold) X" → "（長押し）X", with full-width parentheses and no space: "（長押し）摂取", "（長押し）ジャンプ".
- "Click" → "クリック". "Click and hold" / "hold" → "長押し": "アイテムを長押しして隠してください".
- "Right mouse button" → "マウス右ボタン" and "left mouse button" → "マウス左ボタン".
- Key names: [SPACEBAR] → [スペース], Shift / Ctrl / Tab / Esc stay as they are.
- "Bindings" → "キー割り当て". "Cannot be rebound" → "変更不可".

### Proper names
- Keep proper names in Latin script, as TRANSLATING.md says. This covers people, stores, brands, places, strains,
  vehicles, and Benzies.
  - Attach particles directly: "Uncle Nelsonに会う", "NorthtownにおけるBenziesの影響力".
  - Don't add さん or other honorifics to a Latin name unless the English has "Mr." or similar.
    "Mr." / "Mrs." stay in Latin as part of the name.
- Translate the generic part around a name: "OG Kush Seed" → "OG Kushの種", "Benzies cartel" → "Benziesカルテル",
  "Albert Hoover (Weed Supplier)" → "Albert Hoover（大麻サプライヤー）".
- Generic place labels on the map are translated: "Canal" → "運河", "Bridge" → "橋", "Casino" → "カジノ",
  "Car Wash" → "洗車場". Store and brand names are not ("Bud's Bar", "Bleuball's Boutique").

## Ranks (Roman numeral tiers kept, with a space: "男爵 III")

| # | English | Japanese |
|---|---|---|
| 1 | Street Rat | ドブネズミ |
| 2 | Hoodlum | チンピラ |
| 3 | Peddler | 密売人 |
| 4 | Hustler | やり手 |
| 5 | Bagman | 集金人 |
| 6 | Enforcer | 用心棒 |
| 7 | Shot Caller | 幹部 |
| 8 | Block Boss | 縄張りのボス |
| 9 | Underlord | 黒幕 |
| 10 | Baron | 男爵 |
| 11 | Kingpin | 麻薬王 |
| | Rank | ランク |
| | Level up / Rank up | レベルアップ / ランクアップ |
| | XP | XP |
| | Unlock / Unlocked | 解放 / 解放済み |

## Employees, contacts, people

| English | Japanese |
|---|---|
| Employee | 従業員 |
| Botanist | 栽培師 |
| Chemist | 化学者 |
| Packager / Handler | 梱包係 |
| Cleaner | 清掃員 |
| Dealer (hired NPC who sells for you) | 売人 |
| Dealer (casino) | ディーラー |
| Arms Dealer | 武器商人 |
| Supplier | サプライヤー |
| Weed / Meth / Coke / Shroom Supplier | 大麻サプライヤー / 覚醒剤サプライヤー / コカインサプライヤー / キノコサプライヤー |
| Customer | 顧客 (UI) / 客 (dialogue) |
| Contact | 連絡先 |
| Assign / Assigned | 割り当て / 担当の・担当 ("担当の栽培師", "担当顧客") |
| Hire / Fire (employee) | 雇用 / 解雇 ("雇う" / "クビにする" in dialogue) |
| Daily wage / Salary | 日給 |
| Cut (dealer's share) | 取り分 |
| Recruit (a dealer) | 勧誘 |
| Summon | 呼び出す |
| Host (multiplayer) | ホスト |
| Player | プレイヤー |

## Relationship and cartel status (nouns)

| English | Japanese |
|---|---|
| Relationship | 関係 |
| Hostile | 敵対 |
| Unfriendly | 不仲 |
| Neutral | 中立 |
| Friendly | 友好 |
| Loyal | 忠誠 |
| Truced | 休戦 |
| Defeated | 壊滅 |
| hostile (in a sentence) | 敵対する ("あなたと敵対します") |
| Cartel | カルテル |
| Cartel influence | カルテルの影響力 |
| Agreement (with the Benzies) | 協定 |
| Call off the agreement | 協定を破棄する |
| Goon (cartel thug) | 手下 |

## Products and drugs

| English | Japanese |
|---|---|
| Product (drug for sale) | 商品 (UI) / ブツ (dialogue slang) |
| Product (harvest from plants) | 収穫物 |
| Weed / Marijuana | 大麻 (UI) / ハッパ・草 (dialogue) |
| Meth | 覚醒剤 (UI) / シャブ (dialogue) |
| Cocaine / Coke | コカイン (UI) / コーク (dialogue) |
| Shrooms / Mushrooms | マッシュルーム (UI) / キノコ (dialogue) |
| Strain | 品種 |
| Listed / Unlisted (product for sale) | 販売リストに追加 / 販売リストから削除 ("list/unlist" → "販売リストへの追加/削除") |
| Quality | 品質 |
| Trash / Poor / Standard / Premium / Heavenly (quality) | ゴミ / 粗悪 / 標準 / 上質 / 極上 |
| Addiction (customer) | 依存度 |
| Addictiveness (product) | 依存性 |
| Price / Asking Price | 価格 / 希望価格 |
| Sample (free sample) | サンプル ("無料サンプル") |
| Deal (transaction) | 取引 |
| Contract | 契約 |
| Offer | 提示 / 提示額 |
| Counteroffer / counter | 価格交渉 |
| Order | 注文 |
| Delivery | 配達 |
| Recipe | レシピ |

## Mixing

| English | Japanese |
|---|---|
| Mix (noun, a mixed product) | ミックス ("New Mix" → "新しいミックス") |
| Mix / mixing (action, process) | 調合 ("調合を開始") |
| Mixer (mixing ingredient) | 調合素材 |
| Ingredient | 材料 |
| Effect / Property (of a product) | 効果 |
| Attribute | 特性 |
| Mixing station | 調合ステーション |

## Effects (product properties)

| English | Japanese |
|---|---|
| Anti-Gravity | 反重力 |
| Athletic | アスリート |
| Balding | 抜け毛 |
| Bright-Eyed | 輝く瞳 |
| Calming | リラックス |
| Calorie-Dense | 高カロリー |
| Cyclopean | 一つ目 |
| Disorienting | 混乱 |
| Electrifying | 帯電 |
| Energizing | 活力 |
| Euphoric | 多幸感 |
| Explosive | 爆発性 |
| Focused | 集中 |
| Foggy | 霧 |
| Gingeritis | 赤毛病 |
| Glowing | 発光 |
| Jennerising | ジェンナー化 |
| Laxative | 下剤 |
| Long Faced | 面長 |
| Munchies | 食欲増進 |
| Paranoia | パラノイア |
| Refreshing | 爽快 |
| Schizophrenic | 精神錯乱 |
| Sedating | 鎮静 |
| Seizure-Inducing | 発作誘発 |
| Shrinking | 縮小 |
| Slippery | ツルツル |
| Smelly | 悪臭 |
| Sneaky | 隠密 |
| Spicy | 激辛 |
| Thought-Provoking | 思索 |
| Toxic | 有毒 |
| Tropic Thunder | トロピック・サンダー |
| Zombifying | ゾンビ化 |

## Mixer ingredients

| English | Japanese |
|---|---|
| Addy, Cuke, Mega Bean, Viagor (brands) | keep in Latin |
| Banana | バナナ |
| Battery | 電池 |
| Chili | 唐辛子 |
| Donut | ドーナツ |
| Energy Drink | エナジードリンク |
| Flu Medicine | 風邪薬 |
| Gasoline | ガソリン |
| Horse Semen | 馬の精液 |
| Iodine | ヨウ素 |
| Motor Oil | エンジンオイル |
| Mouth Wash | マウスウォッシュ |
| Paracetamol | パラセタモール |

## Growing and production

| English | Japanese |
|---|---|
| Station (generic) | ステーション |
| Packaging station | 梱包ステーション |
| Packaging (verb: package / unpackage) | 梱包 / 開封 |
| Chemistry station | 化学ステーション |
| Lab oven | ラボオーブン |
| Cauldron | 大釜 |
| Brick press | ブリックプレス |
| Pot (plant) | 植木鉢 |
| Grow tent | 栽培テント |
| Grow light | 栽培ライト |
| Drying rack | 乾燥ラック |
| Mushroom bed | キノコ苗床 |
| Mushroom spawn / Mushroom spawn station | 種菌 / 種菌ステーション |
| Soil | 土 |
| Seed(s) | 種 ("Coca Seeds" → "コカの種", "OG Kush Seed" → "OG Kushの種") |
| Buds / leaves | バッズ / 葉 |
| Coca leaves | コカの葉 |
| Additive (Fertilizer, PGR, SpeedGrow) | 添加剤 |
| Fertilizer | 肥料 |
| Apply (an additive) | 与える ("肥料を与える", "PGRを与える") |
| Apply (a setting, color) | 適用 ("設定を適用") |
| Water / Watering can | 水やり / じょうろ |
| Harvest | 収穫 |
| Trimmers | 剪定ばさみ |
| Output (of a station) | 完成品 |
| Destination (where output is moved) | 搬送先 |
| Supplies (for employees) | 資材 |
| Materials | 材料 |
| Item | アイテム |
| Allowed Items / Allowed Quality | 許可アイテム / 許可品質 |
| Blacklist / Whitelist | ブラックリスト / ホワイトリスト |
| Filter | フィルター ("Clear filter" → "フィルターをリセット") |
| Storage rack | 収納ラック |

## Packaging

| English | Japanese |
|---|---|
| Packaging (item category) | パッケージ |
| Baggie | 小袋 (dialogue: パケ) |
| Jar | 瓶 |
| Brick | ブリック |

## Crime, police, money

| English | Japanese |
|---|---|
| Police / Officer | 警察 / 警官 (dialogue: サツ) |
| BUSTED | 逮捕 |
| Arrest | 逮捕 |
| Body search | 身体検査 |
| Charge (offense) | 容疑 |
| Penalty | 罰則 |
| Fine | 罰金 |
| Wanted / Pursuit | 手配 / 追跡 |
| Curfew | 夜間外出禁止令 |
| Dead drop | デッドドロップ |
| Stash | 隠し場所 |
| Laundering / laundering operation | 資金洗浄 |
| Business (laundering front) | 事業 ("Business Management" → "事業管理", "Businesses for sale" → "売り出し中の事業") |
| Property (real estate) | 物件 (never 効果) |
| Cash | 現金 |
| Online balance / Balance | オンライン残高 / 残高 |
| Deposit / Withdraw | 入金 / 引き出し |
| Amount (money) | 金額 |
| Amount / Quantity (items) | 数量 |
| Bonus | ボーナス |
| Payment | 支払い |
| ATM | ATM |
| Cart | カート ("Add to Cart" → "カートに追加") |
| Shop / Store | ショップ / 店 |
| Delivery status "Arrived" | 到着済み |
| Net worth | 純資産 |

## Casino

| English | Japanese |
|---|---|
| Casino | カジノ |
| Blackjack / BLACKJACK! | ブラックジャック / ブラックジャック！ |
| BUST! | バースト！ |
| Bet / Bet Amount | ベット / 賭け金 |
| Bet multiplier | 賭け倍率 |
| BIG WIN! | 大当たり！ |
| Win / Lose / Push (tie) | 勝ち / 負け / 引き分け |

## Settings and generic UI

| English | Japanese |
|---|---|
| Settings / Apply Settings | 設定 / 設定を適用 |
| Display / Graphics / Audio / Controls | ディスプレイ / グラフィック / サウンド / 操作 |
| Ambience | 環境音 |
| Anti-aliasing | アンチエイリアス |
| Brightness | 明るさ |
| Camera Sensitivity | カメラ感度 |
| Camera Bobbing | カメラの揺れ |
| Save (game file) | セーブデータ |
| Backup / Auto Backup Saves | バックアップ / セーブデータの自動バックアップ |
| Full game | 製品版 |
| Demo | 体験版 |
| Back / Cancel / Close | 戻る / キャンセル / 閉じる |
| Accept | 承諾 |
| Begin / Start | 開始 ("Begin Game" → "ゲーム開始") |
| Continue | 続ける |
| Confirm | 確定 |
| Buy / Sell | 購入 / 売却 |
| Add / Add New / Remove | 追加 / 新規追加 / 削除 |
| Clear | クリア |
| Yes / No / OK / Done | はい / いいえ / OK / 完了 |
| Are you sure? | よろしいですか？ |
| All | すべて |
| Basic / Advanced (enum) | 基本 / 上級 |
| Active / Closed (vote status) | 受付中 / 締め切り |
| Bug report / Feedback | バグ報告 / フィードバック |
| Character (creator tab) | キャラクター |
| Body / Clothing / Hair / Face | 体 / 服装 / 髪型 / 顔 |
| Category | カテゴリー |
| Phone | スマホ |
| Messages / Contacts / Map | メッセージ / 連絡先 / マップ |
| Quest / Objective | クエスト / 目標 |
| Day names | 月曜日, 火曜日… (short form: 月, 火…) |
