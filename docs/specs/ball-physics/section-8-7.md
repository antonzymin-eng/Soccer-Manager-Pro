## 8.7 Future Reference Updates

This section briefly notes future reference work required as the specification evolves. **For full implementation details and architectural decisions, see Section 7 (Future Extensions).**

### 8.7.1 Stage 2 Surface Research (Planned)

When per-tile surface properties are implemented (Section 7.2.1), additional research required:

- **Needed:** COR and Î¼_r values for mud, waterlogged grass, artificial turf, hardened ground
- **Potential sources:** CarrÃ©, M.J. papers on synthetic turf; FIFA Quality Programme for artificial turf; field testing for mud/waterlogged conditions
- **Timeline:** Before Stage 2 implementation (Year 3)
- **Full details:** See Section 7.2.1 for complete surface property architecture and implementation approach

### 8.7.2 Stage 3 Multi-Ball Research (Planned)

For training mode multi-ball scenarios (Section 7.3):

- **Needed:** Ball-ball collision physics (COR, momentum transfer between balls)
- **Potential sources:** Billiard ball collision papers, multi-particle simulation literature
- **Timeline:** Before Stage 3 implementation (Year 4)
- **Full details:** See Section 7.3.2 for complete multi-ball architecture and BallManager design

### 8.7.3 Stage 5 Fixed64 Library Evaluation (Planned)

Before Fixed64 migration (Section 7.4):

- **Needed:** Accuracy and performance benchmarking of candidate Fixed64 libraries
- **Acceptance criteria:** <0.01m position error over 10s simulation vs. double-precision reference
- **Timeline:** Early Stage 5 (Year 6)
- **Full details:** See Section 7.4 for complete migration strategy, candidate libraries, risk analysis, and selection criteria

### 8.7.4 Spin Decay Empirical Validation (Planned)

Before or during Stage 0 implementation:

- **Needed:** Empirical spin decay measurements to validate or replace TORQUE_COEFFICIENT, DECAY_VELOCITY_FACTOR, and DECAY_SPIN_FACTOR
- **Potential approach:** High-speed camera analysis of spinning balls in flight, measuring spin rate over time
- **Acceptance criteria:** Simulated spin decay curve matches observed spin persistence within Â±20%
- **Timeline:** During Stage 0 implementation phase
- **Notes:** If empirical data proves difficult to obtain, the current values are acceptable for gameplay provided they produce visually convincing curve trajectories during playtesting

---

**END OF SECTION 8**

---

## Document Status

**Section 8 Completion:**
- âœ… Research papers documented with full citations, DOIs, and access metadata (8.1)
- âœ… Real-world validation sources documented with search terms (8.2)
- âœ… Master Volume cross-references documented with corrected dates (8.3)
- âœ… Software and tool references documented (8.4)
- âœ… Citation audit completed with full coverage of all Section 3.1 components (8.6)
- âœ… Future reference work identified with Section 7 cross-references (8.7)
- âœ… All fabricated data removed â€” specification relies only on peer-reviewed sources
- âœ… Selection rationale added for all coefficient ranges
- âœ… All internal document dates verified against actual project files
- âœ… C_L implementation vs. literature discrepancy documented with rationale (8.6.1)
- âœ… Drag crisis threshold adjustment documented with rationale (8.6.2)
- âœ… Spin decay coefficients explicitly marked as empirically chosen (8.1.6, 8.6.6)
- âœ… State machine thresholds audited (8.6.7)
- âœ… HONG-2012 and SAYERS-1999 citation dates corrected
- âœ… PLANNED status clearly marked on all unfinished validation sources

**Version:** 1.3 (all critical, moderate, and minor issues from v1.1 review addressed)  
**Page Count:** ~12 pages  
**Status:** READY FOR REVIEW

**Quality assurance:**
- No fictional data present
- All academic papers include DOI and access information (or URL for open sources)
- All internal documents dated correctly
- Citation format standardized across all sources
- Field validation marked as planned (to occur during Stage 0 implementation)
- All implemented values that differ from literature are documented with rationale
- Empirically chosen values are explicitly identified, not disguised as sourced

**Next Steps:**
1. Final review and approval
2. Proceed to Appendices A-C
