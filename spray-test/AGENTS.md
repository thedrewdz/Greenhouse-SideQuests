# AGENTS

## Purpose

This repository is an implementation project.

Agent policy, shared guardrails, role workflows, and reusable skills are maintained in the Greenhouse documentation repository to keep one source of truth and avoid drift.

Canonical source:

- https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/AGENTS.md

## Required External References

Read and follow these files from the documentation repository before non-trivial work:

1. https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/AGENTS.md
2. https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/CONTEXT.md
3. https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/DOCS-MAP.md
4. https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/.agents/skills/README.md

## Skill Loading Rule

Do not copy or redefine skills in this repository.

When a skill is needed, load it from the documentation repository under:

- https://github.com/thedrewdz/Greenhouse-Documentation/tree/main/.agents/skills

## Artifact Rule For Implementation Repositories

When work is spec-driven, write stage artifacts in this repository under:

- .agent-output/specs/<spec-name>/

Use templates from the documentation repository as source format:

- https://github.com/thedrewdz/Greenhouse-Documentation/tree/main/templates

## Local Scope Addendum

Use this repository for firmware and implementation concerns only.

If local code behavior and documentation policy conflict, pause implementation and raise a documentation feedback item targeting the documentation repository.
