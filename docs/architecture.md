# Architecture

SecureFix AI uses a controlled workflow:

1. ingest a vulnerability alert
2. validate and normalize the payload
3. assess risk deterministically
4. generate an AI recommendation
5. build a draft PR proposal
6. wait for human approval
7. record audit and governance data

## Target shape

- FastAPI API for ingestion and workflow actions
- service layer for business logic
- repositories for persistence abstraction
- security layer for identity and authorization
- governance layer for approval and reporting
- observability layer for logs, metrics, and traces

## Deployment path

Hackathon demo: containerized app with SQLite and local/mock providers.

Production target: separately scalable containers with managed identity, managed queueing, PostgreSQL, secrets management, and centralized telemetry.
