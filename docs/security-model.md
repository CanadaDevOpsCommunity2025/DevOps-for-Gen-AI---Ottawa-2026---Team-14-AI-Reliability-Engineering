# Security Model

SecureFix AI treats all external vulnerability content as untrusted data.

## Controls

- validate all payloads
- enforce allowed severity values and CVE formats
- isolate system prompts from untrusted text
- validate AI output against a strict schema
- apply role-based authorization
- mask secrets and sensitive headers in logs
- block AI from approving, merging, deploying, or changing policy
- provide a kill switch for action adapters

## Roles

- Admin
- SecurityReviewer
- Developer
- Viewer

## Secrets

Secrets must come from environment variables or managed secret storage in production.
No credentials are committed to source control.
