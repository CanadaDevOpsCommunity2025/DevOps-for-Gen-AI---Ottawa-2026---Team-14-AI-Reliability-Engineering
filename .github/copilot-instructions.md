# GitHub Copilot Instructions
# Project: SecureFix AI
# DevOps for GenAI Ottawa 2026 Hackathon
# Theme: Autonomous DevOps

## Project Purpose

Build an AI Security Release Engineer that detects software vulnerabilities, analyzes risk, recommends fixes, creates remediation pull requests, and enforces security, governance, observability, and human approval controls.

The project must demonstrate a credible production path and answer:

1. How would this be deployed?
2. How would it be secured?
3. How would it be governed?
4. How would it be monitored?
5. How would failures be handled?

The objective is NOT to build a chatbot.

The objective is to build a production-minded AI-assisted DevOps workflow.

---

# Core Use Case

When a vulnerability alert is received:

1. Ingest the alert.
2. Normalize the vulnerability data.
3. Assess severity and impact.
4. Generate a remediation recommendation.
5. Generate a pull request proposal.
6. Run validation checks.
7. Produce a governance report.
8. Require human approval.
9. Record all actions in an audit trail.

---

# Guiding Principles

Copilot must prioritize:

1. Security
2. Governance
3. Traceability
4. Observability
5. Human oversight

Over:

1. UI polish
2. Complex AI features
3. Fancy dashboards

Every feature should demonstrate production readiness.

---

# Preferred Technology Stack

Backend:
- Python 3.12
- FastAPI

Testing:
- PyTest

AI:
- Azure OpenAI preferred
- OpenAI supported
- Mock provider supported

Storage:
- SQLite for demo
- PostgreSQL abstraction for production

Messaging:
- In-memory queue for demo
- Service Bus/Kafka abstraction for production

Observability:
- OpenTelemetry
- Structured JSON logs

CI/CD:
- GitHub Actions

Containers:
- Docker

Deployment:
- Azure Container Apps
- AKS compatible

---

# Required Repository Structure

src/
    api/
    agents/
    services/
    repositories/
    models/
    security/
    governance/
    observability/
    workflows/

tests/
docs/
infra/
.github/

---

# Architecture Requirements

Use clean architectural boundaries.

Controllers must not contain business logic.

Business logic belongs in services.

External providers must be abstracted.

Examples:

IVulnerabilityProvider
IAIProvider
IAuditProvider
IApprovalProvider

Use dependency injection.

Avoid tightly coupled code.

---

# Required MVP Features

## Feature 1: Vulnerability Ingestion

Support:

- JSON file ingestion
- Sample Dependabot payload ingestion

Store:

- CVE
- Package
- Version
- Severity
- Description

---

## Feature 2: Risk Assessment

Calculate:

- Critical
- High
- Medium
- Low

Return:

- Severity
- Confidence
- Recommended action

---

## Feature 3: AI Remediation Recommendation

Generate:

- Explanation
- Patch recommendation
- Upgrade recommendation
- Risk summary

All AI responses must contain:

- Confidence score
- Disclaimer
- Model name
- Prompt version

---

## Feature 4: Pull Request Draft Generation

Generate:

- Minimal code change proposal
- Package upgrade recommendation
- Release notes

Do NOT automatically merge.

---

## Feature 5: Governance Report

Generate:

governance-report.json

Include:

- Alert ID
- Severity
- AI recommendation
- Confidence
- Reviewer
- Approval status
- Timestamp

---

## Feature 6: Human Approval Gate

Implement:

ApprovalStatus

Values:

- Pending
- Approved
- Rejected

No remediation action may proceed if status is Pending.

Human approval is mandatory.

---

## Feature 7: Audit Logging

Capture:

- Ingestion
- Analysis
- Recommendation
- Approval
- Rejection
- Error
- Retry

Every event must receive a correlation ID.

---

## Feature 8: Observability

Track:

- Requests processed
- Vulnerabilities analyzed
- AI calls
- Failed AI calls
- Approval count
- Rejection count

Expose:

/health
/metrics

---

# Security Requirements

Implement:

## Identity

Mock identity for demo.

Abstract for:

- Azure Managed Identity
- Workload Identity

---

## Authorization

Roles:

Admin
SecurityReviewer
Developer
Viewer

Enforce role checks.

---

## Secrets

Never hardcode secrets.

Use:

environment variables

Create:

.env.example

Never commit credentials.

---

## Input Validation

Validate:

- Vulnerability payloads
- Severity values
- CVE format

Reject malformed input.

---

## Prompt Injection Protection

Treat all external vulnerability text as untrusted.

Never allow incoming payloads to alter system instructions.

Keep system prompts isolated.

---

# Governance Requirements

All AI decisions must be:

- Explainable
- Logged
- Reviewable

The system must always support:

- Human override
- Audit review
- Rejection workflow

AI recommendations are advisory.

AI is not authorized to approve, merge, deploy, or release.

---

# Failure Handling

Implement graceful failure.

Examples:

## AI unavailable

Fallback:

rules engine

Return:

"HUMAN REVIEW REQUIRED"

---

## Database unavailable

Queue request.

Retry later.

---

## Invalid recommendation

Reject recommendation.

Require reviewer intervention.

---

## Critical exception

Log event.

Keep audit trail.

Fail safely.

---

# Monitoring Requirements

Create dashboards or reports showing:

- Vulnerability volume
- Severity trends
- AI recommendation success
- Approval rates
- Processing times

Store all metrics in structured format.

---

# CI/CD Requirements

Every pull request must execute:

1. Lint
2. Unit tests
3. Security tests
4. Dependency scan
5. Build verification

Fail the build when tests fail.

---

# Required Documentation

Copilot must maintain:

README.md

docs/architecture.md

docs/security-model.md

docs/governance-model.md

docs/runbook.md

docs/threat-model.md

docs/ai-system-card.md

---

# AI System Card Requirements

Document:

Purpose

Scope

Users

Risks

Limitations

Human oversight

Model provider

Prompt version

Fallback strategy

---

# Threat Model Requirements

Cover:

Prompt injection

Vulnerable dependencies

Unauthorized approval

Privilege escalation

Secrets exposure

Data leakage

Denial of service

Supply chain attacks

---

# Hackathon Success Criteria

The completed solution must clearly demonstrate:

✅ Deployment strategy

✅ Security model

✅ Governance model

✅ Monitoring strategy

✅ Failure handling strategy

✅ Human approval process

✅ Auditability

✅ AI transparency

✅ Production readiness

If a feature does not improve one of these areas, do not prioritize it.
