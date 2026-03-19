---
name: research
description: Use when conducting research on any topic related to company goals (competitive
  analysis, market research, software engineering best practices, marketing, ERP software
  in Slovenia, accounting, cloud migration, AI integration). Covers research sourcing
  conventions, output formatting, and storage structure under the research/ directory.
---

# Research

## Sourcing conventions
When researching best practices in anything related to our goals (for example software engineering, marketing, ERP software in Slovenia in general, accounting):
- Search for articles from industry leaders
- Use the current date; only cite sources less than 1 month old
- State references and examples with publication dates
- Prioritize concrete evidence over abstract principles

## Output structure
After completing any research, ask the user whether they want to store the results. Only save if the user confirms.
When saving, store under `research/<topic>/`:
- Create a `README.md` in each subfolder explaining: the research goal, key findings summary, sources with dates, and when the research was conducted
- Additional files (data, detailed notes, comparisons) go in the same subfolder
- Use short, descriptive folder names (e.g., `research/cloud-erp-competitors/`, `research/ai-accounting-trends/`)
