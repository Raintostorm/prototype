# Spin Squad UI Button Gap Audit

Updated: 2026-08-17

This audit covers player-facing screens only. Debug and animation-test controls are excluded.

## Ready and reusable

- Battle preparation: Start, Roll, Bag Open, Bag Closed.
- Ally context: Merge, Sell, Info, Close.
- Pause/result flow: Resume, Restart.
- Common navigation: Back, OK.
- Resource and stat icons: Coin, Key, Energy, HP, Attack, Armor, Critical, Healing, Speed.
- Utility icons: Home, Mail, Shop, Settings, Pause, Bag.

## Missing production assets

All interactive assets below need `normal`, `pressed`, and where applicable `selected` or `disabled` states. Text must remain English.

### Homepage

- Bottom navigation tab set: Home, Upgrade, Treasure, Settings (`normal`, `pressed`, `selected`).
- Top utility square buttons: Mail and Shop (`normal`, `pressed`, notification-badge-safe).
- Campaign arrows: Previous and Next (`normal`, `pressed`, `disabled`).
- Homepage Battle CTA matching the final kit (`normal`, `pressed`, `disabled`).

### Battle

- Combat-stat tabs: Damage, Taken, Healing (`normal`, `pressed`, `selected`).
- Speed selector: x1 and x2 (`normal`, `pressed`, `selected`).
- Pause and Settings square-button shells matching the final kit.
- Result actions: Continue, Home, Retry/Restart (`normal`, `pressed`).
- Compact dynamic-number badge for Roll cost. The main Roll caption is already baked into the button art.

### Upgrade

- Upgrade card shell per rarity: Common, Rare, Epic, Legendary (`normal`, `pressed`, `selected`, `locked`).
- Upgrade confirmation CTA (`normal`, `pressed`, `disabled`).
- Horizontal swipe/page indicator matching the kit.

### Treasure

- Treasure Roll/Open CTA (`normal`, `pressed`, `disabled`).
- Rarity tabs: Common, Rare, Epic, Legendary (`normal`, `pressed`, `selected`).
- Treasure card shells per rarity (`normal`, `pressed`, `selected`, `locked`).
- Claim/Equip action (`normal`, `pressed`, `disabled`) if the detail flow keeps those actions.

### Shared overlays

- Modal panel shell and compact dialog action pair: Confirm and Cancel.
- Notification badge in small and medium sizes.
- Locked-state overlay and unavailable-state overlay.

## Layout rules for the next export

- Transparent PNG, no white or black matte.
- Every state in a pair must use the exact same canvas size and pivot.
- Keep text inside wide CTA assets only; icon buttons must contain no baked text.
- Leave a safe center/right area for dynamic values such as costs and inventory capacity.
- Do not combine multiple controls into one PNG unless it is delivered as a clearly documented atlas.
