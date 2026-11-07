using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UPlayGround.Combat
{
    public static class CombatLogExportUtility
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string ToCsv(IEnumerable<CombatLogEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("sequence,frame,combatTime,unscaledTime,attacker,victim,animKey,hitPhaseIndex,attackKind,defenseType,defenseOutcome,rawDamage,finalDamage,hpDelta,poiseDelta,breakDelta,reactionType,reactionState,critical,attackerPower,defenseRate,damageTakenMultiplier,criticalMultiplier,hitParticle");

            foreach (CombatLogEntry entry in entries ?? Enumerable.Empty<CombatLogEntry>())
            {
                CombatResult result = entry.Result;
                builder
                    .Append(entry.Sequence).Append(',')
                    .Append(entry.Frame).Append(',')
                    .Append(Format(entry.CombatTime)).Append(',')
                    .Append(Format(entry.UnscaledTime)).Append(',')
                    .Append(EscapeCsv(ActorName(result.Attacker))).Append(',')
                    .Append(EscapeCsv(ActorName(result.Victim))).Append(',')
                .Append(result.Hit.MotionAsset != null ? result.Hit.MotionAsset.name : "-").Append(',')
                    .Append(result.Hit.HitPhaseIndex).Append(',')
                    .Append(result.Hit.AttackKind).Append(',')
                    .Append(result.Hit.DefenseType).Append(',')
                    .Append(result.Defense.Outcome).Append(',')
                    .Append(Format(result.Hit.Damage)).Append(',')
                    .Append(Format(result.Damage.FinalDamage)).Append(',')
                    .Append(Format(result.Resources.HpDelta)).Append(',')
                    .Append(Format(result.Resources.PoiseDelta)).Append(',')
                    .Append(Format(result.Resources.BreakDelta)).Append(',')
                    .Append(result.Hit.ReactionType).Append(',')
                    .Append(result.Reaction.TargetState).Append(',')
                    .Append(result.Damage.IsCritical).Append(',')
                    .Append(Format(result.Damage.AttackerPower)).Append(',')
                    .Append(Format(result.Damage.DefenseRate)).Append(',')
                    .Append(Format(result.Damage.DamageTakenMultiplier)).Append(',')
                    .Append(Format(result.Damage.CriticalMultiplier)).Append(',')
                    .Append(EscapeCsv(result.Hit.HitParticleName))
                    .AppendLine();
            }

            return builder.ToString();
        }

        public static string ToMarkdown(IEnumerable<CombatLogEntry> entries, float expectedDuration = -1f)
        {
            List<CombatLogEntry> snapshot = entries?.ToList() ?? new List<CombatLogEntry>();
            float totalDamage = snapshot.Sum(e => e.Result.Damage.FinalDamage);
            int criticalCount = snapshot.Count(e => e.Result.Damage.IsCritical);
            float duration = snapshot.Count > 1
                ? snapshot[^1].CombatTime - snapshot[0].CombatTime
                : 0f;

            var builder = new StringBuilder();
            builder.AppendLine("# Combat Log Report");
            builder.AppendLine();
            builder.AppendLine($"- Entries: {snapshot.Count}");
            builder.AppendLine($"- Duration: {Format(duration)} sec");
            if (expectedDuration > 0f)
            {
                builder.AppendLine($"- Expected Duration: {Format(expectedDuration)} sec");
                builder.AppendLine($"- Duration Delta: {Format(duration - expectedDuration)} sec");
            }
            builder.AppendLine($"- Total Damage: {Format(totalDamage)}");
            builder.AppendLine($"- Average Damage: {Format(snapshot.Count > 0 ? totalDamage / snapshot.Count : 0f)}");
            builder.AppendLine($"- Critical Hits: {criticalCount}");
            builder.AppendLine();

            AppendTopAnimKeys(builder, snapshot);
            builder.AppendLine();

            builder.AppendLine("| # | Time | Attacker | Victim | Motion | Phase | Defense | Raw | Final | HP | Poise | Break | Reaction |");
            builder.AppendLine("|---|------|----------|--------|---------|-------|---------|-----|-------|----|-------|-------|----------|");
            foreach (CombatLogEntry entry in snapshot)
            {
                CombatResult result = entry.Result;
                builder
                    .Append("| ").Append(entry.Sequence)
                    .Append(" | ").Append(Format(entry.CombatTime))
                    .Append(" | ").Append(EscapeMarkdown(ActorName(result.Attacker)))
                    .Append(" | ").Append(EscapeMarkdown(ActorName(result.Victim)))
                .Append(" | ").Append(result.Hit.MotionAsset != null ? result.Hit.MotionAsset.name : "-")
                    .Append(" | ").Append(result.Hit.HitPhaseIndex)
                    .Append(" | ").Append(result.Defense.Outcome)
                    .Append(" | ").Append(Format(result.Hit.Damage))
                    .Append(" | ").Append(Format(result.Damage.FinalDamage))
                    .Append(" | ").Append(Format(result.Resources.HpDelta))
                    .Append(" | ").Append(Format(result.Resources.PoiseDelta))
                    .Append(" | ").Append(Format(result.Resources.BreakDelta))
                    .Append(" | ").Append(result.Reaction.TargetState)
                    .AppendLine(" |");
            }

            return builder.ToString();
        }

        private static void AppendTopAnimKeys(StringBuilder builder, List<CombatLogEntry> snapshot)
        {
            builder.AppendLine("## Motion Summary");
            builder.AppendLine();
            builder.AppendLine("| Motion | Hits | Total Damage | Average Damage |");
            builder.AppendLine("|---------|------|--------------|----------------|");

            foreach (var group in snapshot
                .GroupBy(e => e.Result.Hit.MotionAsset != null ? e.Result.Hit.MotionAsset.name : "-")
                         .OrderByDescending(g => g.Sum(e => e.Result.Damage.FinalDamage)))
            {
                float totalDamage = group.Sum(e => e.Result.Damage.FinalDamage);
                int count = group.Count();
                builder
                    .Append("| ").Append(group.Key)
                    .Append(" | ").Append(count)
                    .Append(" | ").Append(Format(totalDamage))
                    .Append(" | ").Append(Format(count > 0 ? totalDamage / count : 0f))
                    .AppendLine(" |");
            }
        }

        private static string ActorName(GameActor actor)
        {
            if (actor == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(actor.ActorId))
                return actor.ActorId;

            return actor.name;
        }

        private static string Format(float value) => value.ToString("0.###", Invariant);

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool shouldQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!shouldQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string EscapeMarkdown(string value)
            => string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
