# Simplified Chinese glossary (zh-CN)

Read this before you translate a chunk, and follow it strictly. When a new term recurs, add it here.
The hard rules in `tools/corpus/TRANSLATING.md` still apply (placeholders, tags, `\n` count, edge spaces).
The ALL-CAPS rule does not apply to Chinese: translate ALL-CAPS keys the same way as normal keys ("CLOSE" → "关闭").

## Conventions

### Tone and address
- Address the player as "你" everywhere: UI, system text, tutorials and NPC dialogue. Use "您" only for formal
  NPCs such as shopkeepers, bank or realty staff, and police speaking officially.
- Dialogue should sound like natural spoken Chinese. Street NPCs use slang: 货 (product), 条子 (cops), 哥们/兄弟 (bro),
  搞钱 (make money). The crude humor is intentional, so don't soften or moralize it.
- UI text should be concise, the way Chinese PC games word it. Don't pad it.

### Punctuation
- Chinese sentences use full-width punctuation: ，。？！：；、（）“”‘’. Examples: "确定吗？", "数量：", "种植师可被指派到：".
- Ellipsis is "……" (two full-width ellipsis characters), including in input placeholders: "Add item..." → "添加物品……",
  "Amount..." → "金额……". The dash is "——".
- Anything inside placeholders, tags and technical strings stays ASCII and is never changed: `<color=#FFC73D>`, `{0}`, `<NAME>`,
  `%USERPROFILE%\AppData\...`, `ALL ()`.
- Parentheses are full-width （） when the content or its surroundings are Chinese: "Albert Hoover（大麻供货商）",
  "已指派顾客（{0}/{1}）". When the parentheses hold only Latin text and follow a Latin name, leave them as in the key:
  "Andy<color=#A0A0A0FF> (Downtown)</color>" stays unchanged.
- Leading list and marker symbols from the key stay as they are, including the space after them: "- ", "+ ", "^", "•  ".
  So "- Property" → "- 效果", "^ Supplies..." → "^ 将从此处取用物资".
- The slash separator stays ASCII "/" with no spaces around it: "加速/刹车", "花苞/叶子", "上膛/射击".
- Don't add a final "。" to UI labels or short hints that have no period in the English. Keep "。" when the English sentence ends with ".".

### Spacing
- Put one half-width space between Chinese and a Latin-script word (a proper name, brand or acronym):
  "可在 Gas-Mart 购买", "施加 PGR", "Benzies 在 Northville 的势力", "OG Kush 种子".
  Name placeholders that expand to Latin text (`<NAME>` and the like) follow the same rule.
- Put no space between Chinese and numbers, number slots or money: "属性{0}", "答案{0}", "+${0}奖金", "其他{0}项", "1月{0}日".
- Roman numeral rank tiers get a space before them: "枭雄 III", "街区老大 V".

### Prompts, buttons, instructions
- "(Hold) X" → "（按住）X", with full-width parentheses and no space: "（按住）服用", "（按住）跳跃", "（按住）鼻吸".
- Click → 点击. Click and hold → 按住. Press → 按. Hold (a key) → 按住. Right/left mouse button → 鼠标右键/鼠标左键.
  "Click X to ..." → "点击X即可……" or "点击X以……".
- A button is a short verb or verb phrase of 2–4 characters with no punctuation: 购买, 取消, 关闭, 返回, 确认, 接受, 开始,
  开始游戏, 应用设置, 加入购物车.
- Status labels read as states: 进行中, 已结束, 已送达, 已解锁, 未解锁, 已指派.
- Map labels that the key splits with `\n` keep the same number of line breaks. Split the Chinese at a natural point:
  "Brown Apartment\nBlock" → "棕色\n公寓楼", "Body\nShop" → "汽车\n修理厂", "Basketball\nCourt" → "篮球\n场".

### Names, money, units
- Keep proper names in Latin script: people, brands, stores, the Benzies cartel, districts (Hyland Point, Northtown, Westville,
  Downtown, Docks, Suburbia, Uptown, Northville), strains and product names (OG Kush, Sour Diesel, Granddaddy Purple,
  Baby Blue, Biker Crank, Glass), vehicle models, and brand-name items (PGR, SpeedGrow, Addy, Cuke, Mega Bean, Viagor).
  Translate the generic word around them: "OG Kush Seed" → "OG Kush 种子", "Bleuball's Boutique" stays unchanged.
- Money keeps "$" in front: "${0}", "+${0}奖金". Units stay as in the key: g, oz, mph, °F.
- Dates: "January {0}" → "1月{0}日".

## Ranks

Roman numeral tiers I–V are kept with a space before them: "收账人 III".

| English | Chinese |
|---|---|
| Street Rat | 街头鼠辈 |
| Hoodlum | 小混混 |
| Peddler | 小贩 |
| Hustler | 倒爷 |
| Bagman | 收账人 |
| Enforcer | 打手 |
| Shot Caller | 话事人 |
| Block Boss | 街区老大 |
| Underlord | 地下霸主 |
| Baron | 枭雄 |
| Kingpin | 教父 |
| Rank / Rank up | 等级 / 升级 |
| XP | 经验值 |

## Employees, contacts, people

| English | Chinese |
|---|---|
| Employee | 员工 |
| Botanist | 种植师 |
| Chemist | 化学师 |
| Packager / Handler | 包装工 |
| Cleaner | 清洁工 |
| Dealer (hired, or rival street dealer) | 毒贩 |
| Arms Dealer | 军火商 |
| Supplier | 供货商 |
| Weed / Meth / Coke Supplier | 大麻供货商 / 冰毒供货商 / 可卡因供货商 |
| Customer | 顾客 |
| Contact (phone) | 联系人 |
| Assign / Assigned | 指派 / 已指派 |
| Hire / Fire (employee) | 雇佣 / 解雇 |
| Wage / Daily wage | 工资 / 日薪 |
| Cut (dealer's share) | 分成 |
| Bed (employee bed) | 床 |
| Summon | 叫来 |
| Host (multiplayer) | 房主 |
| Player | 玩家 |

## Relationship and cartel status

| English | Chinese |
|---|---|
| Relationship | 关系 |
| Hostile | 敌对 |
| Unfriendly | 不友好 |
| Neutral | 中立 |
| Friendly | 友好 |
| Loyal | 忠诚 |
| Truced | 休战 |
| Defeated | 已击败 |
| become hostile (in a sentence) | 与你为敌 |
| Unlocked / Locked | 已解锁 / 未解锁 |
| Recommend / Recommendation | 推荐 |
| Standards (customer) | 要求 |
| Very Low / Low / Moderate / High / Very High | 极低 / 低 / 中等 / 高 / 极高 |

## Products, production, stations

| English | Chinese |
|---|---|
| Product (drug for sale) | 产品 (dialogue slang: 货) |
| Product (harvest from plants) | 收成 |
| Output (of a station) | 产出 |
| Destination | 目的地 |
| Weed / Meth / Cocaine / Shrooms | 大麻 / 冰毒 / 可卡因 / 迷幻蘑菇 |
| Strain | 品种 |
| Listed / List / Unlist (product) | 已上架 / 上架 / 下架 |
| Mix (verb, process) / Mixing | 混合 |
| Mix (noun, a mixed product) / New Mix | 混合品 / 新混合品 |
| Mixer (ingredient for mixing) | 配料 |
| Ingredients (generic station input) | 原料 |
| Recipe | 配方 |
| Effect / Property (of a product) | 效果 |
| Attribute | 属性 |
| Quality | 品质 |
| Addiction (customer level) | 成瘾度 |
| Addictiveness (product stat) | 成瘾性 |
| Mixing station | 混合台 |
| Packaging station | 包装台 |
| Chemistry station | 化学台 |
| Lab oven | 实验烤炉 |
| Cauldron | 大锅 |
| Brick press | 压砖机 |
| Pot | 花盆 |
| Grow tent | 种植帐篷 |
| Drying rack | 晾干架 |
| Drying (process) | 晾干 |
| Mushroom bed | 蘑菇床 |
| Mushroom spawn station | 菌种培育台 |
| Spawn (mushroom) | 菌种 |
| Storage rack | 储物架 |
| Station (generic) | 工作台 |
| Additive (Fertilizer, PGR, SpeedGrow) | 添加剂 |
| Fertilizer | 肥料 |
| Apply (additive) | 施加 ("施加肥料", "施加 PGR") |
| Soil | 土壤 |
| Seed / Seeds | 种子 ("古柯种子", "OG Kush 种子") |
| Buds / Leaves | 花苞 / 叶子 |
| Coca leaves | 古柯叶 |
| Pseudo (pseudoephedrine) | 伪麻黄碱 |
| Watering can | 浇水壶 |
| Trimmers | 修枝剪 |
| Harvest | 收获 |
| Supplies | 物资 |
| Materials | 材料 |
| Item | 物品 |
| Allowed Items / Allowed Quality | 允许的物品 / 允许的品质 |
| Blacklist / Whitelist | 黑名单 / 白名单 |
| Filter / Clear filter | 筛选 / 清除筛选 |

### Quality tiers

| English | Chinese |
|---|---|
| Trash | 垃圾 |
| Poor | 劣质 |
| Standard | 普通 |
| Premium | 优质 |
| Heavenly | 极品 |

### Packaging

| English | Chinese |
|---|---|
| Packaging | 包装 |
| Package / Unpackage | 包装 / 拆包 |
| Unpackaged | 未包装 |
| Baggie | 小袋 |
| Jar | 玻璃罐 |
| Brick | 砖 |

### Mixer ingredients

| English | Chinese |
|---|---|
| Addy / Cuke / Mega Bean / Viagor | unchanged (brands) |
| Banana | 香蕉 |
| Battery | 电池 |
| Chili | 辣椒 |
| Donut | 甜甜圈 |
| Energy Drink | 能量饮料 |
| Flu Medicine | 感冒药 |
| Gasoline | 汽油 |
| Horse Semen | 马精液 |
| Iodine | 碘酒 |
| Motor Oil | 机油 |
| Mouth Wash | 漱口水 |
| Paracetamol | 扑热息痛 |

### Effects (product properties)

| English | Chinese |
|---|---|
| Anti-Gravity | 反重力 |
| Athletic | 健步如飞 |
| Balding | 秃顶 |
| Bright-Eyed | 目光炯炯 |
| Calming | 镇静 |
| Calorie-Dense | 高热量 |
| Cyclopean | 独眼 |
| Disorienting | 晕头转向 |
| Electrifying | 电流 |
| Energizing | 活力 |
| Euphoric | 欣快 |
| Explosive | 爆炸 |
| Focused | 专注 |
| Foggy | 迷雾 |
| Gingeritis | 红毛症 |
| Glowing | 发光 |
| Jennerising | 变性 |
| Laxative | 腹泻 |
| Long Faced | 长脸 |
| Munchies | 嘴馋 |
| Paranoia | 偏执 |
| Refreshing | 清爽 |
| Schizophrenic | 精神分裂 |
| Sedating | 催眠 |
| Seizure-Inducing | 抽搐 |
| Shrinking | 缩小 |
| Slippery | 脚滑 |
| Smelly | 恶臭 |
| Sneaky | 鬼祟 |
| Spicy | 辛辣 |
| Thought-Provoking | 发人深省 |
| Toxic | 有毒 |
| Tropic Thunder | 热带雷霆 |
| Zombifying | 丧尸化 |

## Dealing, deals, money

| English | Chinese |
|---|---|
| Deal (with a customer) | 交易 |
| Contract | 合约 |
| Offer / Counter (offer) | 报价 / 还价 |
| Asking Price | 要价 |
| Sample / Free sample | 样品 / 免费样品 |
| Handover (giving product) | 交货 |
| Order | 订单 |
| Active Orders | 进行中的订单 |
| Delivery | 配送 |
| Delivery status "Arrived" | 已送达 |
| Dead drop | 藏货点 |
| Stash | 藏货 |
| Laundering | 洗钱 |
| Laundering operation | 洗钱业务 |
| Business (laundering front) | 产业 |
| Business Management | 产业管理 |
| Property (real estate) | 房产 |
| Cash | 现金 |
| Online balance | 账户余额 |
| Deposit / Withdraw | 存款 / 取款 |
| Net worth | 净资产 |
| Amount (money) | 金额 |
| Amount / Quantity (items) | 数量 |
| Price | 价格 |
| Bonus | 奖金 |
| ATM | ATM |
| Cart / Add to Cart | 购物车 / 加入购物车 |
| Buy / Sell | 购买 / 出售 |
| Shop / Store | 商店 |
| Pawn shop | 当铺 |
| Region / Area | 区域 |

## Crime, police, cartel

| English | Chinese |
|---|---|
| Police / Officer | 警察 / 警官 |
| Cops (NPC slang) | 条子 |
| Wanted / Pursuit | 通缉 / 追捕 |
| Arrest / Arrested | 逮捕 / 被捕 |
| BUSTED | 落网 |
| Charge (offense) | 罪名 |
| Penalty | 处罚 |
| Fine | 罚款 |
| Body search | 搜身 |
| Conceal | 藏起 |
| Checkpoint | 检查站 |
| Curfew | 宵禁 |
| Investigating | 调查中 |
| Cartel | 卡特尔 |
| Benzies (the cartel) | Benzies |
| Cartel influence | 卡特尔势力 ("卡特尔在 Northtown 的势力") |
| Agreement (with the Benzies) | 协议 |
| Call off (agreement) | 解除 |
| Ambush | 伏击 |
| Goon (cartel thug) | 喽啰 |
| Graffiti / Spray Paint | 涂鸦 / 喷漆 |
| Hospital bill | 医疗账单 |

## Casino

| English | Chinese |
|---|---|
| Casino | 赌场 |
| Blackjack (game) | 二十一点 |
| BLACKJACK! (outcome) | 黑杰克！ |
| BUST! | 爆牌！ |
| Hit / Stand | 要牌 / 停牌 |
| Bet / Bet Amount | 下注 / 下注金额 |
| Bet multiplier | 下注倍率 |
| BIG WIN! | 大奖！ |
| BETTER LUCK NEXT TIME... | 下次好运…… |
| Slot machine | 老虎机 |
| Ride the Bus (card game) | Ride the Bus |

## Input prompts and actions

| English | Chinese |
|---|---|
| (Hold) Consume | （按住）服用 |
| (Hold) Smoke | （按住）抽一口 |
| (Hold) Snort | （按住）鼻吸 |
| (Hold) Jump | （按住）跳跃 |
| Mount (skateboard) | 骑乘 |
| Aim | 瞄准 |
| Cock/Fire | 上膛/射击 |
| Accelerate/Brake | 加速/刹车 |
| Interact / Use | 互动 / 使用 |
| Pick up / Drop | 拿起 / 丢下 |
| Equip | 装备 |
| Bag Trash | 垃圾装袋 |
| Answer (phone) | 接听 |
| Change Amount / Change Quantity | 更改数量 |

## Settings and generic UI

| English | Chinese |
|---|---|
| Settings / Apply Settings | 设置 / 应用设置 |
| Display / Graphics | 显示 / 图形 |
| Audio | 音频 |
| Ambience | 环境音 |
| Anti-aliasing | 抗锯齿 |
| Brightness | 亮度 |
| Camera Sensitivity | 镜头灵敏度 |
| Camera Bobbing | 镜头晃动 |
| Controls / Bindings | 操作 / 按键绑定 |
| Save (game file) | 存档 |
| Load | 载入 |
| Backup / Auto Backup Saves | 备份 / 自动备份存档 |
| Full game | 完整版 |
| BETA | 测试版 |
| Continue / Resume | 继续 / 继续游戏 |
| Main Menu / Quit | 主菜单 / 退出 |
| Back / Cancel / Close | 返回 / 取消 / 关闭 |
| Accept / Confirm | 接受 / 确认 |
| Begin | 开始 |
| Add / Add New | 添加 / 新增 |
| Clear | 清除 |
| Are you sure? | 确定吗？ |
| All | 全部 |
| Category | 类别 |
| Bug report / feedback | Bug 报告/反馈 |
| Character (creator) | 角色 |
| Body / Clothing / Hair / Face | 身体 / 服装 / 发型 / 面部 |
| Color / Choose Color / Apply Color | 颜色 / 选择颜色 / 应用颜色 |
| Basic / Advanced (enum) | 基础 / 高级 |
| SPACEBAR | 空格键 |
| Phone apps: Messages / Contacts / Map / Deliveries / Products | 短信 / 联系人 / 地图 / 配送 / 产品 |

## Places (generic map labels are translated; names stay Latin)

| English | Chinese |
|---|---|
| Canal / Bridge | 运河 / 大桥 |
| Car Wash | 洗车店 |
| Laundromat | 洗衣店 |
| Post Office | 邮局 |
| Body Shop (car repair) | 汽车修理厂 |
| Motel Room | 汽车旅馆房间 |
| Sweatshop | 血汗工厂 |
| Bungalow | 平房 |
| Barn | 谷仓 |
| Docks Warehouse | Docks 仓库 |
| Manor | 庄园 |
| Businesses for sale | 待售产业 |
| Hyland Manor | Hyland 庄园 |
| RV | 房车 |
| Storage unit (Stash & Dash) | 储物间 |
| Warehouse | 仓库 |
| Motel | 汽车旅馆 |
| Hardware store | 五金店 |

## Dialogue and system text (added with chunks 11+)

| English | Chinese |
|---|---|
| Locker (employee) | 储物柜 |
| Suspension rack | 悬挂架 |
| Grow light | 种植灯 |
| Substrate (mushroom) | 基质 |
| Spray bottle | 喷壶 |
| AC unit | 空调 |
| Management clipboard | 管理写字板 |
| Journal | 日志 |
| Quest | 任务 |
| Fixer (hires employees) | 中间人 |
| Stash box (supplier's) | 藏货箱 |
| Tab (supplier credit) | 账 ("记在账上") |
| Drop (supplier dead-drop order) | 货 / 藏货点送货 ("请求藏货点送货") |
| Deal is off (customer) | 交易取消 |
| Product manager app / Dealer management app / Deliveries app | 产品管理应用 / 毒贩管理应用 / 配送应用 |

Placeholder spacing in dialogue:
- `<PRODUCT>`, `<NAME>`, `<VEHICLE>`, `<REGION>` expand to Latin text → one space on each side next to Chinese: "我要 <PRODUCT>！", "去找 <NAME> 聊聊".
- Money/number placeholders (`<PRICE>`, `<AMOUNT>`, `<DEBT>`, `<PAYMENT>`, `<SIGN_FEE>`, `<DAILY_WAGE>`) → no space: "我付<PRICE>".
- `<PROPERTY>`, `<BUSINESS>`, `<LOCATION>` expand to translated Chinese → no space. `<LOCATION>` may or may not contain "在", so build sentences
  that work either way: "<WINDOW_START>到<WINDOW_END>之间，<LOCATION>见。", "取货地点：<LOCATION>".
- Input tokens (`<Input_TogglePhone>` etc.) become key names → one space on each side: "按 <Input_TogglePhone> 打开".
- "they" for a single unknown customer → "这人" ("我觉得这人会喜欢你的货").
- Price choices "Name (<PRICE>)": Latin names keep ASCII "The Shitbox (<PRICE>)"; translated names use full-width "谷仓（<PRICE>）".
