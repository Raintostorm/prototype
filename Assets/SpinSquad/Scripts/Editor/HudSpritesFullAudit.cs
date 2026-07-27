#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SpinSquad.UI;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public static class HudSpritesFullAudit
    {
        const string HudRoot = "Assets/SpinSquad/Resources/UI/Hud";
        const string MapPath = "Assets/SpinSquad/Docs/AI_CONTEXT/UI_HUD_ASSET_MAP.md";

        [MenuItem("SpinSquad/UI/Audit All HUD Sprites", false, 2)]
        public static void AuditAll()
        {
            HudUiSprites.ClearCache();
            var rows = CollectRows();
            var ok = 0;
            var missing = 0;
            var wired = 0;
            var gap = 0;

            foreach (var row in rows)
            {
                if (row.LoadOk)
                    ok++;
                else
                    missing++;
                if (row.IsWired)
                    wired++;
                else if (row.IsGap)
                    gap++;
            }

            Debug.Log(
                $"[HudSpritesFullAudit] PNG={rows.Count} loadOk={ok} missing={missing} wired≈{wired} gap≈{gap}. Ghi map: {MapPath}");
            WriteAssetMap(rows, ok, missing, wired, gap);
            AssetDatabase.Refresh();
        }

        static List<AuditRow> CollectRows()
        {
            var list = new List<AuditRow>();
            if (!Directory.Exists(HudRoot))
            {
                Debug.LogError("[HudSpritesFullAudit] Missing folder: " + HudRoot);
                return list;
            }

            var pngs = Directory.GetFiles(HudRoot, "*.PNG", SearchOption.AllDirectories);
            Array.Sort(pngs, StringComparer.OrdinalIgnoreCase);

            foreach (var abs in pngs)
            {
                var rel = abs.Replace('\\', '/');
                var assetName = Path.GetFileNameWithoutExtension(rel);
                var combat = rel.IndexOf("/Combat/", StringComparison.OrdinalIgnoreCase) >= 0;
                var resourcesPath = HudUiWiringRegistry.ResourcesPathFor(assetName, combat);
                HudUiSprites.TryGet(assetName, out var sprite);
                var loadOk = sprite != null;
                HudUiWiringRegistry.RoleByAssetName.TryGetValue(assetName, out var role);
                if (string.IsNullOrEmpty(role))
                    role = "unmapped";
                var isGap = role.StartsWith("gap_", StringComparison.OrdinalIgnoreCase);
                var isWired = loadOk && !isGap;

                list.Add(new AuditRow
                {
                    FileName = Path.GetFileName(rel),
                    AssetName = assetName,
                    ResourcesPath = resourcesPath,
                    LoadOk = loadOk,
                    Role = role,
                    IsGap = isGap,
                    IsWired = isWired,
                    PngSize = ReadPngSize(abs)
                });
            }

            return list;
        }

        static void WriteAssetMap(List<AuditRow> rows, int ok, int missing, int wired, int gap)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# UI HUD Asset Map (machine-readable)");
            sb.AppendLine();
            sb.AppendLine("> Auto-generated / refreshed by **SpinSquad → UI → Audit All HUD Sprites**.");
            sb.AppendLine("> Load convention: `Resources.Load<Sprite>(\"UI/Hud/Meta/Setting\")` — no extension.");
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine($"| Metric | Value |");
            sb.AppendLine($"|--------|-------|");
            sb.AppendLine($"| PNG files | {rows.Count} |");
            sb.AppendLine($"| loadOk | {ok} |");
            sb.AppendLine($"| missing | {missing} |");
            sb.AppendLine($"| wired (role ≠ gap) | {wired} |");
            sb.AppendLine($"| gap (no UI hook) | {gap} |");
            sb.AppendLine();
            sb.AppendLine("## Folders");
            sb.AppendLine();
            sb.AppendLine("- `Resources/UI/Hud/Meta/` — meta/homepage pack (55)");
            sb.AppendLine("- `Resources/UI/Hud/Combat/` — duel/combat pack (13)");
            sb.AppendLine();
            sb.AppendLine("## Master table");
            sb.AppendLine();
            sb.AppendLine("| fileName | resourcesPath | loadOk | wiredInCode | uiRole | notes |");
            sb.AppendLine("|----------|---------------|--------|-------------|--------|-------|");

            foreach (var r in rows)
            {
                var wiredCol = r.IsWired ? "yes" : "no";
                var notes = r.LoadOk ? "" : "Run Reimport HUD Sprites";
                if (!string.IsNullOrEmpty(r.PngSize))
                    notes = string.IsNullOrEmpty(notes) ? r.PngSize : notes + "; " + r.PngSize;
                sb.AppendLine(
                    $"| {r.FileName} | {r.ResourcesPath} | {(r.LoadOk ? "yes" : "no")} | {wiredCol} | {r.Role} | {notes} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Scene wiring (runtime BuildUi)");
            sb.AppendLine();
            sb.AppendLine("| Scene | Elements | Sprites |");
            sb.AppendLine("|-------|----------|---------|");
            sb.AppendLine("| Homepage | CTA Play/Upgrade/Treasure; icon bar (nền = flat MetaHudTheme, không dùng Background PNG) | Battle_button, Green_button, Chest, Setting, Mail_button, Shop_icon |");
            sb.AppendLine("| Homepage | Level overlay close | Back |");
            sb.AppendLine("| SampleScene / Duel | Top bar; bottom CTAs; roll slots; ally menu; settings stub icons | Stop_on/off, Setting, Speed_x2_off, Restart, Home_in_battle, Blue/Orange_button, Selected/Not_selected, Merge/Sell, Volume_*, On/Turn_off |");
            sb.AppendLine("| Upgrade | Back; card portrait fallback | Back; Wood/Fire/Metal/Water/Earth_icon |");
            sb.AppendLine("| UpgradeDetail / Treasure / TreasureDetail | Back CTA | Back |");
            sb.AppendLine();
            sb.AppendLine("## GAP — có file, chưa gắn UI");
            sb.AppendLine();
            sb.AppendLine("Các asset sau cần màn/feature mới (shop thật, tab treasure, resource bar art, v.v.):");
            sb.AppendLine();

            foreach (var r in rows)
            {
                if (!r.IsGap)
                    continue;
                sb.AppendLine($"- **{r.AssetName}** — {r.Role.Replace("gap_", "")}");
            }

            sb.AppendLine();
            sb.AppendLine("## Next steps (tiếng Việt)");
            sb.AppendLine();
            sb.AppendLine("- Chạy **SpinSquad → UI → Reimport HUD Sprites** nếu `loadOk` < 68.");
            sb.AppendLine("- Chạy **Validate All HUD Sprites** sau khi sửa wiring.");
            sb.AppendLine("- Smoke Play: Homepage → Upgrade/Treasure → SampleScene (prep, pause, roll, ally menu).");
            sb.AppendLine("- Khi thêm Shop/Mail/Quest: map sprite tương ứng và xóa dòng khỏi GAP.");
            sb.AppendLine();
            sb.AppendLine("## Related docs");
            sb.AppendLine();
            sb.AppendLine("- [`ASSET_REQUEST.md`](../ASSET_REQUEST.md) — art brief");
            sb.AppendLine("- [`assetrequired.txt`](../assetrequired.txt) — checklist ngắn");
            sb.AppendLine("- [`SYSTEM_MAP.md`](SYSTEM_MAP.md) — logic game");

            var dir = Path.GetDirectoryName(MapPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(MapPath, sb.ToString(), Encoding.UTF8);
            Debug.Log("[HudSpritesFullAudit] Wrote " + MapPath);
        }

        static string ReadPngSize(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var buf = new byte[24];
                if (fs.Read(buf, 0, buf.Length) < 24)
                    return "";
                if (buf[0] != 137 || buf[1] != 80)
                    return "";
                var w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                var h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                return w + "×" + h;
            }
            catch
            {
                return "";
            }
        }

        struct AuditRow
        {
            public string FileName;
            public string AssetName;
            public string ResourcesPath;
            public bool LoadOk;
            public string Role;
            public bool IsGap;
            public bool IsWired;
            public string PngSize;
        }
    }
}
#endif
