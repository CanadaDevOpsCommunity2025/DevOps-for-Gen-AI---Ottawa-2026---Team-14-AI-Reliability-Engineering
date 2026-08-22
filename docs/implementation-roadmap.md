# SecureFix AI Implementation Roadmap

## DevOps for GenAI Ottawa 2026

**Repository:** [devops-for-gen-ai---ottawa-2026---team-X-ai-reliability-engineering](https://github.com/wesmacdonald/devops-for-gen-ai---ottawa-2026---team-X-ai-reliability-engineering/)  
**Primary theme:** Autonomous DevOps  
**Project:** SecureFix AI, an AI-assisted vulnerability release workflow  
**Delivery constraint:** A working, reproducible project that can be completed and demonstrated in one hackathon day

---

## 1. Project Objective

Build a production-minded workflow that receives a software vulnerability alert, assesses its risk, proposes a remediation, validates the proposal, requires human approval, and records a complete audit trail.

The project must demonstrate credible answers to five production-readiness questions:

1. How would the solution be deployed?
2. How would the solution be secured?
3. How would the solution be governed?
4. How would the solution be monitored?
5. How would the solution handle failure?

The project is not a chatbot. AI is an advisory component inside a controlled DevOps workflow.

---

## 2. Hackathon Scope

### Must Have

- Ingest a sample Dependabot-compatible vulnerability payload.
- Normalize and validate the incoming data.
- Calculate a deterministic risk level.
- Generate an AI-assisted remediation recommendation.
- Fall back to a rules-based recommendation when the AI provider is unavailable.
- Generate a draft pull-request proposal or patch plan.
- Require explicit human approval before any simulated remediation action.
- Record structured audit events with correlation IDs.
- Expose health and metrics endpoints.
- Run automated tests and security checks in GitHub Actions.
- Provide a containerized local deployment.
- Include architecture, security, governance, threat-model, AI-system-card, and runbook documentation.

### Should Have

- Role-based authorization for developer, reviewer, administrator, and viewer personas.
- Configurable policy-as-code for approval requirements and risk thresholds.
- OpenTelemetry-compatible tracing.
- Markdown and JSON governance reports.
- A repository adapter that can create a draft pull request when credentials are supplied.
- A simple dashboard or generated operational summary.

### Could Have

- Slack or Microsoft Teams notification adapter.
- Additional vulnerability providers.
- A second AI provider as a fallback.
- Kubernetes or Azure Container Apps deployment manifests.
- Software bill of materials generation.
- Policy exception workflow.

### Explicitly Out of Scope for the One-Day MVP

- Automatic merge to the default branch.
- Automatic production deployment.
- Unrestricted code-writing agents.
- Production-grade multitenancy.
- Full enterprise identity integration.
- A complex front-end application.
- Autonomous approval of AI-generated changes.

---

## 3. Target Architecture

```text
Dependabot-Compatible Event or Sample JSON
                   |
                   v
          FastAPI Ingestion API
                   |
          Validation and Normalization
                   |
                   v
        Deterministic Risk Engine
                   |
                   v
      AI Recommendation Provider
          |                 |
          | failure         | success
          v                 v
    Rules-Based       Remediation Proposal
      Fallback               |
          \__________________/
                   |
                   v
        Validation and Policy Gate
                   |
                   v
         Human Approval Required
          |                 |
       Reject             Approve
          |                 |
          v                 v
       Audit Log      Draft PR proposal or human-reviewed merge path
                   |
                   v
        Metrics, Traces, and Reports
```

### Production Deployment Position

For the hackathon, run the API, worker logic, and SQLite-backed audit store in a Docker container or Docker Compose environment.

For a production path, document deployment as independently scalable API and worker containers using Azure Container Apps or Kubernetes, with managed identity or workload identity, a managed message queue, PostgreSQL, secrets management, private networking, and centralized observability.

The production architecture is a documented target. The hackathon implementation must clearly label components that are simulated, stubbed, or deferred.

---

## 4. Phase Overview

| Phase | Outcome | Release Gate |
|---|---|---|
| Phase 0 | Scope, repository, and evidence skeleton established | Team can explain the problem, architecture, risks, and demo path |
| Phase 1 | End-to-end MVP workflow functions locally | A sample alert reaches a pending human approval decision |
| Phase 2 | Security and safe AI controls are enforced | Injection, malformed input, secrets, and unauthorized actions are blocked |
| Phase 3 | Governance and auditability are demonstrable | Every decision is traceable and no action bypasses approval |
| Phase 4 | Monitoring and failure handling are visible | Health, metrics, fallback, retry, and safe failure are demonstrated |
| Phase 5 | CI/CD, deployment, documentation, and demo are complete | A clean clone can build, test, run, and reproduce the demo |

---

# Phase 0: Foundation and Scope Lock

## Objective

Create the repository structure, define the evidence the judges will see, and prevent scope expansion.

## Tasks

### 0.1 Create the Project Structure

```text
.github/
  workflows/
  copilot-instructions.md
docs/
infra/
samples/
src/
  api/
  governance/
  models/
  observability/
  providers/
  repositories/
  security/
  services/
  workflows/
tests/
```

### 0.2 Add Core Project Files

Create:

- `README.md`
- `pyproject.toml`
- `.env.example`
- `.gitignore`
- `Dockerfile`
- `docker-compose.yml`
- `LICENSE`
- `AI-USAGE.md`
- `docs/architecture.md`
- `docs/security-model.md`
- `docs/governance-model.md`
- `docs/threat-model.md`
- `docs/ai-system-card.md`
- `docs/runbook.md`
- `docs/demo-script.md`

### 0.3 Define the Primary Demo Scenario

Use one synthetic vulnerability such as an outdated dependency with:

- Alert ID
- CVE identifier
- Package name
- Installed version
- Recommended version
- Severity
- Advisory description
- Repository identifier

The sample must contain no real credentials, private source code, personal information, or proprietary data.

### 0.4 Define Success Metrics

Track at least:

- Percentage of valid alerts successfully processed.
- Percentage of required actions blocked until human approval.
- Number of unsafe or malformed requests rejected.
- Time from alert receipt to recommendation.
- AI fallback activation count.

### 0.5 Assign Workstreams

Recommended workstreams for a team of up to five:

1. API and workflow
2. AI provider and fallback
3. Security and testing
4. Governance and observability
5. CI/CD, deployment, documentation, and demo

For smaller teams, combine adjacent workstreams.

## Phase 0 Acceptance Criteria

- [ ] Repository structure exists.
- [ ] Project scope and non-goals are documented.
- [ ] One complete demo scenario is defined.
- [ ] Architecture diagram is committed.
- [ ] Initial threat model lists at least five material threats.
- [ ] AI usage and verification process is documented.
- [ ] Team agrees that automatic merge and deployment are out of scope.

## Stop-the-Line Rule

Do not begin dashboard work, secondary integrations, or cloud deployment until the Phase 1 end-to-end workflow passes.

---

# Phase 1: Core End-to-End MVP

## Objective

Process one vulnerability alert from ingestion through a pending approval decision.

## Tasks

### 1.1 Define Domain Models

Create typed models for:

- `VulnerabilityAlert`
- `RiskAssessment`
- `RemediationRecommendation`
- `PullRequestProposal`
- `ApprovalDecision`
- `AuditEvent`
- `WorkflowResult`

Use explicit enumerations for:

- Severity: `critical`, `high`, `medium`, `low`
- Approval status: `pending`, `approved`, `rejected`
- Workflow status: `received`, `validated`, `assessed`, `recommended`, `pending_approval`, `approved`, `rejected`, `failed`

### 1.2 Build Alert Ingestion

Implement:

- `POST /api/v1/alerts`
- JSON schema validation
- A sample Dependabot-compatible adapter
- Correlation ID creation
- Duplicate-event detection using the external alert ID

Return a structured response containing the alert ID, correlation ID, and workflow status.

### 1.3 Build the Deterministic Risk Engine

The risk engine must work without AI.

Inputs may include:

- Provider severity
- Availability of a fixed version
- Whether the vulnerable package is direct or transitive
- Whether the vulnerability is marked as exploitable in the synthetic input

Outputs must include:

- Normalized severity
- Numeric risk score
- Reasons contributing to the score
- Required approval level

Keep these rules in configuration rather than embedding all thresholds in code.

### 1.4 Define the AI Provider Interface

Create an abstraction such as:

```python
class AIRecommendationProvider(Protocol):
    async def recommend(
        self,
        alert: VulnerabilityAlert,
        assessment: RiskAssessment,
    ) -> RemediationRecommendation:
        ...
```

Implement:

- A configurable external-model provider.
- A deterministic mock provider for local runs and tests.
- A rules-based fallback provider.

### 1.5 Generate a Remediation Recommendation

The structured result must include:

- Recommended action
- Suggested target version, when available in the input
- Explanation
- Assumptions
- Confidence indicator
- Model/provider identifier
- Prompt version
- Human-review requirement

Do not allow the model to invent package versions. Candidate versions must come from trusted structured input or be marked as requiring verification.

### 1.6 Generate a Draft Pull-Request Proposal

For the MVP, generate a proposal rather than changing a repository.

Include:

- Proposed title
- Proposed description
- Files likely to require review
- Candidate dependency change
- Validation commands
- Rollback guidance
- Known limitations

Write the proposal to JSON and Markdown.

### 1.7 Add Human Approval State

Implement:

- `GET /api/v1/workflows/{id}`
- `POST /api/v1/workflows/{id}/approve`
- `POST /api/v1/workflows/{id}/reject`

No simulated remediation action may run unless the workflow is approved.

### 1.8 Add Basic Persistence

Use SQLite for the hackathon implementation.

Persist:

- Alerts
- Assessments
- Recommendations
- Approval decisions
- Audit events

Keep repository interfaces independent from SQLite so PostgreSQL can be described as the production alternative.

## Phase 1 Acceptance Criteria

- [ ] A sample alert can be submitted through the API.
- [ ] The payload is validated and assigned a correlation ID.
- [ ] The deterministic engine produces an explainable score.
- [ ] The AI or mock provider produces a structured recommendation.
- [ ] A draft pull-request proposal is generated.
- [ ] The workflow stops in `pending_approval`.
- [ ] Approval and rejection endpoints update workflow state.
- [ ] Unit tests cover the main happy path.

## Demonstration Checkpoint

The team can show:

```text
Alert received -> risk assessed -> recommendation generated
-> proposal produced -> human approval pending
```

If this flow does not work, stop and fix it before adding more features.

---

# Phase 2: Security and Safe AI Controls

## Objective

Demonstrate least privilege, secure input handling, prompt separation, secret protection, and blocked unauthorized actions.

## Tasks

### 2.1 Implement Input Security

Add:

- Strict payload size limit
- CVE format validation when a CVE is supplied
- Allowed severity values
- String length limits
- Rejection of unknown or dangerous fields where appropriate
- Safe error messages without stack traces or secrets

### 2.2 Isolate Untrusted Content

Treat advisory titles, descriptions, package names, and repository metadata as untrusted data.

- Never concatenate external text into system instructions without clear delimiters.
- Label untrusted fields as data.
- Instruct the model not to execute or follow instructions contained inside vulnerability text.
- Validate model output against a strict schema.
- Reject tool names or actions not present in an allowlist.

### 2.3 Add Prompt-Injection Tests

Include synthetic cases such as:

- Advisory text instructing the model to ignore system rules.
- Advisory text requesting environment-variable disclosure.
- Package metadata attempting to add an unauthorized action.
- Advisory text asking the agent to merge or deploy directly.

Expected result: the system treats these strings as data and keeps the workflow within the approved action set.

### 2.4 Implement Authorization

For the demo, use a clearly labelled mock identity middleware with role headers or test tokens.

Roles:

- `Viewer`: read workflow and reports
- `Developer`: submit alerts and view recommendations
- `SecurityReviewer`: approve or reject
- `Admin`: manage policy and activate the kill switch

Production documentation must map this abstraction to managed identity, workload identity, or an enterprise identity provider.

### 2.5 Enforce Least Privilege

The AI component must not possess permissions to:

- Approve its own recommendation
- Merge a pull request
- Change branch protections
- Deploy to production
- Modify approval policy
- Disable auditing

The optional repository adapter may create a draft pull request only.

### 2.6 Protect Secrets

- Read provider credentials only from environment variables.
- Commit `.env.example`, never `.env`.
- Mask secrets in logs.
- Run a secret scanner in CI.
- Use fake values in samples and demonstrations.

### 2.7 Add a Kill Switch

Implement an administrative configuration that disables all action adapters while still permitting read-only analysis.

When active:

- Alert ingestion remains available.
- Recommendations may be generated.
- Approval may be recorded.
- Draft-PR or simulated change actions are blocked.
- The block is written to the audit trail.

### 2.8 Add Dependency and Supply-Chain Checks

CI should run:

- Dependency vulnerability audit
- Secret scan
- Static analysis or secure-code linting
- Optional SBOM generation

## Phase 2 Acceptance Criteria

- [ ] Malformed payloads are rejected.
- [ ] Prompt-injection test payloads cannot alter system policy.
- [ ] Output schema validation rejects malformed AI output.
- [ ] Only a reviewer role can approve or reject.
- [ ] AI cannot approve, merge, or deploy.
- [ ] No secrets are stored in source or logs.
- [ ] Kill switch blocks action adapters.
- [ ] Security checks run in CI.
- [ ] Threat model is updated with implemented mitigations and residual risks.

## Security Demo

Show three controls live:

1. Submit an injection-style advisory and show that it remains inert data.
2. Attempt approval as a developer and show an authorization failure.
3. Activate the kill switch and show that an approved workflow cannot perform the simulated action.

---

# Phase 3: Governance, Approval, and Auditability

## Objective

Make every AI-assisted decision traceable, reviewable, and subject to explicit policy and human oversight.

## Tasks

### 3.1 Create Policy-as-Code

Add `config/policy.yml` or an equivalent file containing:

- Severity thresholds
- Approval requirements
- Allowed actions
- Confidence handling
- Model/provider allowlist
- Maximum retry count
- AI timeout
- Kill-switch default
- Retention setting for demo records

Example:

```yaml
approval:
  required: true
  reviewer_roles:
    - SecurityReviewer

risk:
  critical_minimum_score: 90
  high_minimum_score: 70
  medium_minimum_score: 40

ai:
  allowed_providers:
    - mock
    - azure-openai
  timeout_seconds: 20
  max_retries: 1

actions:
  allowed:
    - generate_report
    - create_draft_pr
  prohibited:
    - merge
    - deploy
    - change_policy
```

Values are project configuration, not claims about an external standard.

### 3.2 Build the Approval Record

Every decision must capture:

- Workflow ID
- Reviewer identity
- Reviewer role
- Decision
- Reason
- Timestamp
- Policy version
- Recommendation version
- Correlation ID

The AI must never populate the reviewer identity or approval decision.

### 3.3 Build the Audit Trail

Record events for:

- Alert received
- Validation passed or failed
- Risk assessment created
- AI provider called
- Fallback provider activated
- Recommendation created or rejected
- Approval requested
- Approval granted or rejected
- Action attempted
- Action blocked or completed
- Kill switch changed
- Error and retry

Audit records should be append-only through the application interface.

### 3.4 Generate a Governance Report

Produce `governance-report.json` and a Markdown equivalent containing:

- Alert summary
- Risk assessment and rationale
- Recommendation and confidence indicator
- Model/provider and prompt version
- Policy version
- Validation results
- Approval status
- Reviewer decision and rationale
- Action status
- Audit-event summary
- Limitations and required follow-up

### 3.5 Complete the AI System Card

Document:

- Intended purpose
- Intended users
- Non-goals and prohibited uses
- Model/provider
- Inputs and outputs
- Data sensitivity assumptions
- Material risks
- Human oversight
- Monitoring indicators
- Change process
- Incident response
- Known limitations

### 3.6 Create the AI Usage Statement

`AI-USAGE.md` must identify:

- AI tools used by the team
- Artefacts produced with AI assistance
- Human verification performed
- Tests executed
- Corrections made to AI output
- Known unverified assumptions
- Mocked or stubbed components

## Phase 3 Acceptance Criteria

- [ ] Policy is versioned and loaded from configuration.
- [ ] Every action is checked against policy.
- [ ] Approval contains a real demo identity and rationale.
- [ ] Audit events are correlated across the workflow.
- [ ] Governance reports are reproducible.
- [ ] Model/provider, prompt version, and policy version are recorded.
- [ ] AI system card and AI usage statement are complete.
- [ ] Human override and escalation paths are documented.

## Governance Demo

Show one workflow from alert receipt to final reviewer decision, then display its governance report and correlated audit events.

---

# Phase 4: Observability, Reliability, and Failure Handling

## Objective

Make the system's availability, performance, quality, security, and cost visible, and demonstrate safe behaviour when dependencies fail.

## Tasks

### 4.1 Add Structured Logging

Every log event should include, where applicable:

- Timestamp
- Severity
- Event name
- Correlation ID
- Workflow ID
- Component
- Outcome
- Duration

Do not log credentials, access tokens, raw authorization headers, or full sensitive payloads.

### 4.2 Add Health Endpoints

Implement:

- `GET /health/live`
- `GET /health/ready`

Liveness indicates that the process is operating.

Readiness checks critical dependencies required to accept work, such as the database. The design should distinguish an unavailable AI provider from an unavailable application, because the rules-based fallback may allow the application to remain operational.

### 4.3 Add Metrics

Expose or generate metrics for:

#### Availability

- Requests received
- Successful requests
- Failed requests
- Readiness state

#### Performance

- Alert-processing duration
- Risk-assessment duration
- AI-provider duration
- Approval-wait duration, if practical

#### Quality

- Recommendations generated
- Recommendations rejected by validation
- Reviewer approvals
- Reviewer rejections
- Fallback recommendations

#### Security

- Authorization failures
- Malformed payloads
- Injection-test detections
- Blocked actions
- Kill-switch activations

#### Cost

- AI calls
- Input and output token counts when supplied by the provider
- Estimated call cost only when a configured price source exists

Do not fabricate cost. If the provider does not return usage or pricing is not configured, report usage as unavailable.

### 4.4 Add Failure Scenarios

Implement and test:

#### AI Provider Unavailable

- Stop waiting after a configured timeout.
- Record the failure.
- Invoke the rules-based fallback.
- Mark the result as requiring human review.

#### Invalid AI Output

- Reject output that does not match the schema.
- Do not create an action proposal from invalid output.
- Use fallback or route to human review.

#### Database Failure

- Return a safe service error.
- Do not claim that work was saved.
- Document recovery and replay limitations for the MVP.

#### Action Adapter Failure

- Keep the approval record.
- Mark the action as failed.
- Permit an explicit, idempotent retry.
- Never silently repeat a state-changing action.

#### Unsafe Recommendation

- Fail validation.
- Prevent action.
- Require reviewer intervention.

#### Compromised or Unexpected Behaviour

- Activate the kill switch.
- Disable action adapters.
- Preserve read-only investigation and audit access.

### 4.5 Add Retry and Idempotency Controls

- Use alert IDs and action IDs as idempotency keys.
- Limit retries.
- Retry only operations considered safe to retry.
- Record each retry attempt.
- Document how a production queue and dead-letter queue would replace in-process retry.

### 4.6 Document Rollback and Recovery

The runbook must cover:

- Rolling back the application container image.
- Disabling a problematic model or prompt version.
- Reverting policy configuration.
- Closing or reverting a proposed pull request.
- Reprocessing a failed event.
- Activating and releasing the kill switch.
- Escalating to a human owner.

## Phase 4 Acceptance Criteria

- [ ] Health endpoints distinguish live and ready states.
- [ ] Structured logs contain correlation IDs.
- [ ] Metrics cover availability, performance, quality, security, and AI usage.
- [ ] AI timeout triggers fallback.
- [ ] Invalid model output is blocked.
- [ ] Failed actions are visible and safely retryable.
- [ ] Kill-switch behaviour is tested.
- [ ] Runbook documents rollback, recovery, and escalation.
- [ ] At least one failure scenario appears in the live demo.

---

# Phase 5: CI/CD, Deployment, Evidence, and Demo

## Objective

Make the solution reproducible from a clean clone and package the evidence for judges.

## Tasks

### 5.1 Create the GitHub Actions Workflow

On pull request and push to the default branch, run:

1. Dependency installation
2. Formatting check
3. Linting
4. Type checking
5. Unit tests
6. Integration tests
7. Security tests
8. Dependency audit
9. Secret scan
10. Container build
11. Optional SBOM generation
12. Test and evidence artifact upload

The pipeline must fail when required tests or security gates fail.

### 5.2 Containerize the Application

The `Dockerfile` should:

- Use a maintained Python 3.12 base image.
- Run as a non-root user.
- Install only required dependencies.
- Expose the API port.
- Include a health check where practical.
- Avoid embedding credentials or environment-specific configuration.

`docker-compose.yml` should provide the simplest reproducible demo environment.

### 5.3 Document Deployment Options

#### Hackathon Deployment

- Local Docker or Docker Compose.
- Optional Azure Container Apps deployment.
- SQLite for demonstration data.
- Environment variables for provider configuration.

#### Production Path

Document:

- Kubernetes or Azure Container Apps.
- Separate ingestion and worker components.
- Managed message queue.
- PostgreSQL or another managed relational store.
- Managed identity or workload identity.
- Secrets manager.
- Private networking and controlled egress.
- Centralized logs, metrics, traces, and alerts.
- Independent scaling and rollout strategy.

Do not imply that unimplemented production components are complete. Label them as the target architecture.

### 5.4 Complete the Evidence Pack

The repository should make the following easy to locate:

- Project name and selected theme
- Elevator pitch
- Problem statement and target users
- Architecture diagram
- Working demo instructions
- Technology and AI-tool inventory
- AI usage disclosure
- Threat model
- Security and adversarial test results
- Governance model and AI system card
- CI/CD pipeline evidence
- Functional and failure-test results
- Observability evidence
- Dependency inventory or SBOM
- Secret-scan evidence
- Runbook
- Known limitations
- Future roadmap
- Team roster

### 5.5 Prepare the Live Demo

Recommended demo flow:

1. Explain the vulnerability-remediation problem.
2. Show the architecture and control boundaries.
3. Submit a normal synthetic alert.
4. Show risk assessment and remediation recommendation.
5. Show that the workflow stops for human approval.
6. Approve as an authorized reviewer.
7. Show the draft action and governance report.
8. Submit an injection-style payload and show it being contained.
9. Simulate AI-provider failure and show fallback to human review.
10. Activate the kill switch and show the action adapter being blocked.
11. Show metrics and correlated audit events.
12. Close with the production deployment and recovery path.

### 5.6 Perform a Clean-Clone Test

From a clean environment:

- Clone the repository.
- Follow only the README.
- Create configuration from `.env.example`.
- Build the container.
- Run the tests.
- Start the service.
- Submit the sample alert.
- Complete the demo workflow.

Fix every undocumented dependency discovered during this test.

## Phase 5 Acceptance Criteria

- [ ] CI passes from a clean commit.
- [ ] Required security checks are visible.
- [ ] Container builds and runs.
- [ ] README reproduces the demo.
- [ ] Evidence pack is complete and linked from the README.
- [ ] Mocked and deferred components are identified.
- [ ] Demo includes a successful path, a blocked security action, and a failure fallback.
- [ ] Team can answer all five production-readiness questions consistently.

---

# 5. Testing Strategy

## Unit Tests

Cover:

- Payload validation
- Risk scoring
- Policy evaluation
- Approval authorization
- Recommendation schema validation
- Fallback selection
- Kill-switch enforcement
- Audit-event creation

## Integration Tests

Cover:

- Alert to pending approval
- Approval to simulated action
- Rejection path
- AI timeout to fallback
- Invalid AI output to safe block
- Duplicate alert handling

## Security Tests

Cover:

- Prompt injection in advisory text
- Secret-request payload
- Unauthorized approval
- Attempted prohibited action
- Oversized or malformed payload
- Sensitive-field log masking

## Failure Tests

Cover:

- AI provider unavailable
- Database unavailable
- Action adapter failure
- Retry exhaustion
- Kill switch active

## Demo Test Data

All test data must be synthetic and explicitly labelled. Never include active credentials, production repository data, personal information, or proprietary code.

---

# 6. Production-Readiness Answers

## How Would We Deploy This?

The hackathon version runs as a containerized FastAPI service with SQLite and local or mock providers. The documented production path uses separately scalable API and worker containers on Azure Container Apps or Kubernetes, a managed queue, PostgreSQL, managed identity, a secrets manager, private networking, and centralized telemetry.

## How Would We Secure This?

The system validates all inputs, treats advisory content as untrusted data, separates system prompts from external text, validates AI output against schemas, applies role-based authorization, stores secrets outside source code, masks sensitive logs, restricts actions to an allowlist, prevents AI self-approval, and provides a kill switch.

## How Would We Govern This?

Versioned policy-as-code determines allowed actions and approval requirements. AI recommendations remain advisory. A human reviewer approves or rejects every action. Model, prompt, policy, decision, reviewer, and action metadata are captured in an audit trail and governance report.

## How Would We Monitor This?

Health endpoints, structured logs, correlation IDs, metrics, and traces make availability, latency, recommendation quality, security events, approvals, fallback activation, and AI usage visible. Cost is reported only when verifiable provider usage and configured pricing are available.

## How Would We Handle Failure?

AI timeouts or invalid output trigger a deterministic fallback and mandatory human review. State-changing actions are idempotent and use limited retries. Failed actions remain visible and require safe retry. The kill switch disables actions during unexpected behaviour. Runbooks cover container rollback, model or prompt rollback, policy reversion, event replay, and human escalation.

---

# 7. Definition of Done

The project is done when all of the following are true:

- [ ] One selected theme is stated in the README.
- [ ] A real problem and target user are documented.
- [ ] The core workflow runs live or reproducibly.
- [ ] The solution answers all five production-readiness questions.
- [ ] AI recommendations are advisory and require human approval.
- [ ] Threats, mitigations, and adversarial tests are documented.
- [ ] Functional, security, and failure-path tests pass.
- [ ] Logs, metrics, and health signals are visible.
- [ ] CI/CD builds and tests the project.
- [ ] The project runs in a container.
- [ ] Secrets are absent from the repository.
- [ ] Dependencies are inventoried.
- [ ] The governance report and AI system card are complete.
- [ ] The AI usage statement documents human verification.
- [ ] Setup and runbook instructions work from a clean clone.
- [ ] Mocked components and known limitations are disclosed.
- [ ] The demo shows success, attack containment, failure fallback, human approval, and audit evidence.

---

# 8. Post-Hackathon Roadmap

Only begin these items after the MVP and evidence pack are complete:

1. Replace SQLite with PostgreSQL.
2. Introduce a managed queue and dead-letter processing.
3. Integrate enterprise identity and workload identity.
4. Add a GitHub App with narrowly scoped permissions.
5. Create draft pull requests against test repositories.
6. Add sandboxed patch validation.
7. Add multiple AI providers with controlled failover.
8. Add prompt and model regression evaluation.
9. Deploy with infrastructure as code.
10. Add signed artefacts and stronger supply-chain provenance.
11. Add tenant-aware authorization and data isolation.
12. Add operational alerting and incident-management integration.

---

# 9. Instructions for GitHub Copilot

When implementing this roadmap:

- Complete phases in order.
- Do not add out-of-scope features before the current phase acceptance criteria pass.
- Prefer small, testable modules and typed interfaces.
- Keep external providers behind abstractions.
- Generate tests with every implementation change.
- Never invent dependency versions, CVE facts, package-fix versions, model capabilities, or pricing.
- Use synthetic data in samples and tests.
- Mark mocks, stubs, and future production components clearly.
- Never give AI permission to approve, merge, deploy, change policy, or disable auditing.
- Fail safely and route uncertainty to human review.
- Keep documentation synchronized with implemented behaviour.
- Record AI-assisted development and human verification in `AI-USAGE.md`.

The primary optimization target is not the number of features. It is a small, demonstrable, secure, governed, observable, and recoverable end-to-end workflow.
