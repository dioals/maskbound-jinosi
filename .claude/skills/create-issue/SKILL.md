---
name: create-issue
description: >
  Draft and file a GitHub issue on this repo (dioals/maskbound-jinosi), auto-added to the
  linked GitHub Project "Maskbound Task List" (owner dioals, project #2). Issue body is
  auto-filled with Context, Description, Requirements, Related, and Out of Scope sections
  built from the current conversation/task. Use when user says "create an issue", "file a
  github issue", "make a task for this", "/create-issue", or asks to track work in the
  linked project.
---

Draft GitHub issue, get user confirm, file it linked to project. Never skip confirm step — filing issue is visible action.

## Flow

1. **Gather context.** Pull from current conversation, code just discussed, or ask user directly if topic unclear. Don't invent requirements not stated or implied by conversation/code.
2. **Draft body** using template below. Fill every section — write "None identified" or "N/A" if genuinely empty, don't omit section.
3. **Show draft to user, ask confirm** before creating (title + full body). Wait for explicit go-ahead.
4. **File it:**
   ```
   gh issue create --repo dioals/maskbound-jinosi --title "<title>" --body "<body>" --project "Maskbound Task List"
   ```
5. Report back: issue URL + number.

## Body template

```markdown
## Context
<why this exists — triggering conversation, bug report, feature ask, prior decision. Link commit/file/line if relevant.>

## Description
<what needs doing, in plain terms — 2-5 sentences.>

## Requirements
- <concrete, testable requirement>
- <concrete, testable requirement>

## Related
- <linked issues/PRs: #12>
- <relevant files: path/to/file.cs:42>
- <external refs/docs if any>

## Out of Scope
- <adjacent work explicitly NOT covered by this issue>
```

## Title rules

`<area>: <imperative summary>` — e.g. `combat: fix mask swap not resetting cooldown`. No trailing period. ≤70 chars.

## Project/repo discovery

Repo and project are hardcoded above (confirmed via `gh api graphql` lookup — this repo's only linked Projects v2 board is "Maskbound Task List" #2, owner dioals). If repo remote changes or a second project gets linked, re-run:
```
gh api graphql -f query='query{repository(owner:"dioals",name:"maskbound-jinosi"){projectsV2(first:10){nodes{id title number}}}}'
```
and update this file.

## Labels / fields

Project has Status (Backlog/Ready/In progress/In review/Done), Priority (P0/P1/P2), Size (XS/S/M/L/XL) fields. `gh issue create --project` only adds item to board with defaults — it does NOT set these fields. If user wants Priority/Size set, follow up with:
```
gh project item-edit --project-id PVT_kwHOAIcBy84BcI0H --id <ITEM_ID> --field-id <FIELD_ID> --single-select-option-id <OPTION_ID>
```
(get item id from `gh project item-list 2 --owner dioals`). Don't set these unless user asks — default Status is fine.

## Boundaries

Skill only drafts + files issue. Does not close/edit/comment on existing issues, does not set project fields unless asked, does not create the issue without user confirming draft first.
