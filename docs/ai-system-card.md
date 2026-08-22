# AI System Card

## Purpose

Recommend vulnerability remediation options inside a governed release workflow.

## Scope

AI may explain risk and suggest remediation. It may not approve, merge, deploy, or change policy.

## Users

- developers
- security reviewers
- administrators
- viewers

## Risks

- hallucinated remediation advice
- prompt injection
- over-trust in model output
- false confidence

## Human oversight

A human reviewer must approve or reject each remediation action.

## Fallback

If AI is unavailable or invalid output is returned, the workflow falls back to a rules-based recommendation and requires human review.
