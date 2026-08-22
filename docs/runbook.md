# Runbook

## Common operations

- start the service in Docker
- run unit and security tests
- submit a sample alert
- review the recommendation
- approve or reject the workflow
- inspect audit and governance output

## Failure handling

- AI unavailable: use the fallback recommendation and require human review
- invalid AI output: reject the output and block action
- database unavailable: fail safely and do not claim persistence
- action failure: keep the approval record and allow explicit retry
- unexpected behavior: activate the kill switch

## Recovery

- roll back the container image
- revert policy configuration
- disable a bad model or prompt version
- reprocess a failed event if safe
- escalate to a human owner
