// ============================================================================
// File:     src/localization/CatalogueCoverage.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-008a, §2.3 F5, ERR-049-005
// Purpose:  Caller-admitted identities the base catalogue must cover at construction.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// The identities a caller or boundary admits: static keys, template ids and citation clauses.
    /// <see cref="Localizer"/> construction fails if the base catalogue lacks any of them
    /// (FR-LC-008a / F5, extended to static keys by ERR-049-005). The core never enumerates a
    /// producer roster itself.
    /// </summary>
    public sealed class CatalogueCoverage
    {
        private readonly LocalizationKey[] _staticKeys;
        private readonly TextTemplateId[] _templateIds;
        private readonly CitationClauseKey[] _clauseKeys;

        /// <summary>Creates a coverage set from defensive copies of the supplied identities.</summary>
        public CatalogueCoverage(LocalizationKey[] staticKeys, TextTemplateId[] templateIds, CitationClauseKey[] clauseKeys)
        {
            _staticKeys = Copy(staticKeys, nameof(staticKeys));
            _templateIds = Copy(templateIds, nameof(templateIds));
            _clauseKeys = Copy(clauseKeys, nameof(clauseKeys));
            for (int i = 0; i < _staticKeys.Length; i++)
            {
                if (!_staticKeys[i].IsValid)
                {
                    throw new ArgumentException("Coverage cannot contain a default static key.", nameof(staticKeys));
                }
            }

            for (int i = 0; i < _templateIds.Length; i++)
            {
                if (!_templateIds[i].IsValid)
                {
                    throw new ArgumentException("Coverage cannot contain a default template id.", nameof(templateIds));
                }
            }

            for (int i = 0; i < _clauseKeys.Length; i++)
            {
                if (!_clauseKeys[i].IsValid)
                {
                    throw new ArgumentException("Coverage cannot contain a default clause key.", nameof(clauseKeys));
                }
            }
        }

        /// <summary>Number of required static keys.</summary>
        public int StaticKeyCount => _staticKeys.Length;

        /// <summary>Number of required template ids.</summary>
        public int TemplateIdCount => _templateIds.Length;

        /// <summary>Number of required clause keys.</summary>
        public int ClauseKeyCount => _clauseKeys.Length;

        internal LocalizationKey StaticKey(int index) => _staticKeys[index];

        internal TextTemplateId TemplateId(int index) => _templateIds[index];

        internal CitationClauseKey ClauseKey(int index) => _clauseKeys[index];

        private static T[] Copy<T>(T[] source, string name)
        {
            if (source == null)
            {
                throw new ArgumentNullException(name);
            }

            var copy = new T[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 caller-admitted coverage set (localization-l2-plan v0.5 §4.1, §5.1). |
#endregion
