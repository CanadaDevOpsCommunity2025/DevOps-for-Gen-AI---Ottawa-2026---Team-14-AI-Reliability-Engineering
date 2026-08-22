# SecureFix AI

SecureFix AI is an AI-assisted vulnerability release workflow for the DevOps for GenAI Ottawa 2026 hackathon.

It is designed to demonstrate:

- how a vulnerability alert is ingested and normalized
- how severity and risk are assessed deterministically
- how AI can recommend a remediation without being allowed to approve it
- how a draft pull request can be proposed safely
- how human approval gates the final action
- how the system stays auditable, observable, and fail-safe

## Core workflow

```text
Dependabot or JSON alert
  -> validate and normalize
  -> risk assessment
  -> AI remediation recommendation
  -> draft PR proposal
  -> human approval
  -> optional human-reviewed merge
  -> audit trail and governance report
```

## Production questions

### How would this be deployed?

Hackathon demo: containerized FastAPI app with SQLite and local/mock providers.

Production path: separately scalable API and worker services on Azure Container Apps or AKS, backed by managed messaging, PostgreSQL, managed identity, secrets management, and centralized observability.

### How would this be secured?

The system validates all input, treats advisory content as untrusted data, isolates system prompts from payload text, masks secrets, enforces role-based authorization, and blocks AI from approving, merging, or deploying.

### How would this be governed?

Every recommendation is advisory only. Human approval is mandatory. Policy, reviewer identity, correlation ID, and decision history are recorded in an audit trail and governance report.

### How would this be monitored?

The service exposes health endpoints, structured logs, and metrics for requests, recommendations, approvals, rejections, AI calls, failures, and fallback use.

### How would failures be handled?

AI timeout or invalid output falls back to a rules-based recommendation and requires human review. Unsafe actions are blocked. Kill-switch behavior disables action adapters while preserving read-only analysis and audit access.

## Scope

### Must have

- JSON and sample Dependabot payload ingestion
- payload validation and normalization
- deterministic risk scoring
- AI remediation recommendation with fallback
- draft pull request proposal
- mandatory human approval
- structured audit logging with correlation IDs
- health and metrics endpoints
- CI checks for tests and security scanning

### Out of scope

- automatic merge
- automatic production deployment
- autonomous approval
- full enterprise identity integration
- complex front-end UI

## Repository guide

- [docs/implementation-roadmap.md](docs/implementation-roadmap.md) - delivery plan and phase gates
- [docs/architecture.md](docs/architecture.md) - target architecture
- [docs/security-model.md](docs/security-model.md) - security controls and authorization model
- [docs/governance-model.md](docs/governance-model.md) - approval and audit model
- [docs/threat-model.md](docs/threat-model.md) - threat analysis and mitigations
- [docs/ai-system-card.md](docs/ai-system-card.md) - AI system card
- [docs/runbook.md](docs/runbook.md) - rollback, recovery, and escalation

## Demo expectations

The intended demo should show:

1. a valid alert flowing through validation and risk assessment
2. an AI recommendation that remains advisory
3. a draft PR proposal generated from the recommendation
4. the workflow stopping at pending approval
5. a reviewer approving or rejecting the action
6. audit events and governance output for the full flow
7. a blocked prompt-injection or unauthorized-action attempt
8. a fallback path when AI is unavailable

## Status

This repository is organized around the implementation roadmap and supporting docs for the hackathon MVP.

The preferred delivery approach is a reproducible, production-minded demo that favors security, governance, traceability, and human oversight over UI polish.
