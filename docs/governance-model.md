# Governance Model

SecureFix AI requires human approval for any remediation action.

## Governance rules

- AI recommendations are advisory only
- approval status is Pending, Approved, or Rejected
- no action proceeds while approval is Pending
- every decision is recorded with correlation ID, reviewer identity, and rationale
- policy is versioned and loaded from configuration

## Governance artifacts

- governance-report.json
- correlated audit trail
- reviewer decision history
- policy version record
