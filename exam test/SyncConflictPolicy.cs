using System;
using System.Collections.Generic;
using System.Linq;

namespace exam_test
{
    public class SyncConflictMergeResult
    {
        public bool ConflictDetected { get; set; }
        public bool DuplicateCreated { get; set; }
        public VaultEntry? Entry { get; set; }
        public string UserMessage { get; set; } = "";
    }

    public static class SyncConflictPolicy
    {
        public static bool HasMeaningfulContentDifference(VaultEntry first, VaultEntry second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return !StringEquals(first.Platform, second.Platform) ||
                   !StringEquals(first.Username, second.Username) ||
                   !StringEquals(first.Secret, second.Secret) ||
                   !StringEquals(first.Website, second.Website) ||
                   !StringEquals(first.Note, second.Note) ||
                   first.IsFavorite != second.IsFavorite;
        }

        public static SyncConflictMergeResult AddOrReplaceMergedEntry(
            List<VaultEntry> mergedEntries,
            VaultEntry incomingEntry,
            string conflictDeviceName,
            DateTime utcNow)
        {
            if (mergedEntries == null)
            {
                throw new ArgumentNullException(nameof(mergedEntries));
            }

            if (incomingEntry == null)
            {
                throw new ArgumentNullException(nameof(incomingEntry));
            }

            if (string.IsNullOrWhiteSpace(incomingEntry.Id))
            {
                incomingEntry.Id = Guid.NewGuid().ToString("N");
            }

            int existingIndex = mergedEntries.FindIndex(existing =>
                string.Equals(existing.Id, incomingEntry.Id, StringComparison.OrdinalIgnoreCase));

            if (existingIndex < 0)
            {
                mergedEntries.Add(incomingEntry);

                return new SyncConflictMergeResult
                {
                    Entry = incomingEntry,
                    UserMessage = "Entry added."
                };
            }

            VaultEntry existingEntry = mergedEntries[existingIndex];

            if (!HasMeaningfulContentDifference(existingEntry, incomingEntry))
            {
                if (incomingEntry.UpdatedAt.ToUniversalTime() > existingEntry.UpdatedAt.ToUniversalTime())
                {
                    mergedEntries[existingIndex] = incomingEntry;
                }

                return new SyncConflictMergeResult
                {
                    Entry = mergedEntries[existingIndex],
                    UserMessage = "Entry already matched or newer timestamp was kept."
                };
            }

            VaultEntry duplicate = CloneAsConflictDuplicate(incomingEntry, conflictDeviceName, utcNow);
            mergedEntries.Add(duplicate);

            return new SyncConflictMergeResult
            {
                ConflictDetected = true,
                DuplicateCreated = true,
                Entry = duplicate,
                UserMessage = "Same entry changed on two devices. QuickForge kept both copies instead of silently overwriting."
            };
        }

        public static VaultEntry CloneAsConflictDuplicate(
            VaultEntry source,
            string conflictDeviceName,
            DateTime utcNow)
        {
            string cleanDeviceName = string.IsNullOrWhiteSpace(conflictDeviceName)
                ? "another device"
                : conflictDeviceName.Trim();

            string platform = string.IsNullOrWhiteSpace(source.Platform)
                ? "Conflict copy"
                : source.Platform.Trim() + " (conflict copy)";

            string notePrefix =
                "Conflict copy created by QuickForge because the same entry changed on two devices. " +
                "Review both copies before deleting either one. Source device: " + cleanDeviceName + ".";

            string note = string.IsNullOrWhiteSpace(source.Note)
                ? notePrefix
                : notePrefix + Environment.NewLine + Environment.NewLine + source.Note;

            return new VaultEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Platform = platform,
                Username = source.Username,
                Secret = source.Secret,
                Website = source.Website,
                Note = note,
                CreatedAt = source.CreatedAt == DateTime.MinValue ? utcNow : source.CreatedAt,
                UpdatedAt = utcNow,
                IsFavorite = source.IsFavorite
            };
        }

        public static bool ShouldApplyTombstone(VaultEntry entry, VaultDeletedEntry tombstone)
        {
            if (entry == null || tombstone == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.Id) ||
                string.IsNullOrWhiteSpace(tombstone.EntryId) ||
                !string.Equals(entry.Id, tombstone.EntryId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            DateTime entryUpdatedAtUtc = entry.UpdatedAt == DateTime.MinValue
                ? DateTime.MinValue
                : entry.UpdatedAt.ToUniversalTime();

            DateTime deletedAtUtc = tombstone.DeletedAtUtc == DateTime.MinValue
                ? DateTime.MinValue
                : tombstone.DeletedAtUtc.ToUniversalTime();

            return deletedAtUtc >= entryUpdatedAtUtc;
        }

        private static bool StringEquals(string first, string second)
        {
            return string.Equals(first ?? "", second ?? "", StringComparison.Ordinal);
        }
    }
}
