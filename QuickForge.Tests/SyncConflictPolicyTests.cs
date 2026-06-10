using System;
using System.Collections.Generic;
using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class SyncConflictPolicyTests
    {
        [Fact]
        public void AddOrReplaceMergedEntry_CreatesDuplicate_WhenSameEntryChangedDifferently()
        {
            var merged = new List<VaultEntry>
            {
                new VaultEntry
                {
                    Id = "same-entry",
                    Platform = "GitHub",
                    Username = "user@example.com",
                    Secret = "cloud-secret",
                    Website = "https://github.com",
                    Note = "cloud",
                    CreatedAt = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 6, 10, 10, 5, 0, DateTimeKind.Utc)
                }
            };

            var local = new VaultEntry
            {
                Id = "same-entry",
                Platform = "GitHub",
                Username = "user@example.com",
                Secret = "local-secret",
                Website = "https://github.com",
                Note = "local",
                CreatedAt = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 6, 10, 10, 6, 0, DateTimeKind.Utc)
            };

            var result = SyncConflictPolicy.AddOrReplaceMergedEntry(
                merged,
                local,
                "Laptop",
                new DateTime(2026, 6, 10, 10, 7, 0, DateTimeKind.Utc));

            Assert.True(result.ConflictDetected);
            Assert.True(result.DuplicateCreated);
            Assert.Equal(2, merged.Count);
            Assert.Equal("cloud-secret", merged[0].Secret);
            Assert.Equal("local-secret", merged[1].Secret);
            Assert.NotEqual("same-entry", merged[1].Id);
            Assert.Contains("conflict copy", merged[1].Platform);
        }

        [Fact]
        public void AddOrReplaceMergedEntry_DoesNotDuplicate_WhenContentMatches()
        {
            var merged = new List<VaultEntry>
            {
                new VaultEntry
                {
                    Id = "same-entry",
                    Platform = "GitHub",
                    Username = "user@example.com",
                    Secret = "same-secret",
                    Website = "https://github.com",
                    Note = "same",
                    UpdatedAt = new DateTime(2026, 6, 10, 10, 5, 0, DateTimeKind.Utc)
                }
            };

            var incoming = new VaultEntry
            {
                Id = "same-entry",
                Platform = "GitHub",
                Username = "user@example.com",
                Secret = "same-secret",
                Website = "https://github.com",
                Note = "same",
                UpdatedAt = new DateTime(2026, 6, 10, 10, 6, 0, DateTimeKind.Utc)
            };

            var result = SyncConflictPolicy.AddOrReplaceMergedEntry(
                merged,
                incoming,
                "Laptop",
                new DateTime(2026, 6, 10, 10, 7, 0, DateTimeKind.Utc));

            Assert.False(result.ConflictDetected);
            Assert.Single(merged);
            Assert.Equal(new DateTime(2026, 6, 10, 10, 6, 0, DateTimeKind.Utc), merged[0].UpdatedAt);
        }

        [Fact]
        public void ShouldApplyTombstone_DeletesOnlyWhenTombstoneIsNewerOrSame()
        {
            var entry = new VaultEntry
            {
                Id = "entry-1",
                UpdatedAt = new DateTime(2026, 6, 10, 10, 5, 0, DateTimeKind.Utc)
            };

            var olderTombstone = new VaultDeletedEntry
            {
                EntryId = "entry-1",
                DeletedAtUtc = new DateTime(2026, 6, 10, 10, 4, 0, DateTimeKind.Utc)
            };

            var newerTombstone = new VaultDeletedEntry
            {
                EntryId = "entry-1",
                DeletedAtUtc = new DateTime(2026, 6, 10, 10, 6, 0, DateTimeKind.Utc)
            };

            Assert.False(SyncConflictPolicy.ShouldApplyTombstone(entry, olderTombstone));
            Assert.True(SyncConflictPolicy.ShouldApplyTombstone(entry, newerTombstone));
        }

        [Fact]
        public void HasMeaningfulContentDifference_IgnoresTimestampOnlyChanges()
        {
            var first = new VaultEntry
            {
                Id = "entry-1",
                Platform = "GitHub",
                Username = "user@example.com",
                Secret = "same",
                Website = "https://github.com",
                Note = "note",
                UpdatedAt = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc)
            };

            var second = new VaultEntry
            {
                Id = "entry-1",
                Platform = "GitHub",
                Username = "user@example.com",
                Secret = "same",
                Website = "https://github.com",
                Note = "note",
                UpdatedAt = new DateTime(2026, 6, 10, 10, 5, 0, DateTimeKind.Utc)
            };

            Assert.False(SyncConflictPolicy.HasMeaningfulContentDifference(first, second));
        }
    }
}
