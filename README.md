# DevOps-for-GenAI---Ottawa-2026---Team-14-AyushSharma

**Team 14**

- **Project Name:** N/A
- **Project Title:** AI Reliability Engineering
- **Team Name:** EarlyBird
- **Project Lead:** Ayush Sharma
- **Other Participants:** Roland Agodzo, Shervin Naseri

## Overview

**What it is:** A single, lightweight Python/Node.js AWS Lambda function exposed via AWS API Gateway as a REST endpoint.

**Goal 1:** The proxy will monitor Prompt/Response Quality, Latency, Token Costs, and Security Guardrails in real-time.

**Goal 2:** The AI SRE proxy will define an AI Reliability Error Budget. For example, the app allows a maximum "Toxicity/PII leak rate" of 1% or an "Average Hallucination Score" of 1.5/5. If the model's performance consumes this budget, the dashboard flags an incident and triggers an alert.

**Objective:** By sitting in front of the model, the proxy acts as an active guardrail and compliance gate. It checks prompts for injections and completions for PII leaks before they can reach the user, enforcing continuous compliance.

## How It Works

The client app calls the Lambda API instead of calling Bedrock directly.

1. **Request Gate (Shift Left):** The Lambda runs quick regex/rule checks on the prompt to detect prompt injections or PII (like credit card numbers). If flagged, it blocks the request immediately with a safe fallback response.
2. **The Forward:** If safe, it forwards the prompt to Amazon Bedrock (e.g., calling Anthropic Claude or Meta Llama 3) and measures the exact time to first token and total latency.
3. **Response Evaluation (LLM-as-a-Judge):** Before returning the completion to the user, Lambda runs an evaluation.
   - **Synchronous (Fast):** Run a rapid rule-based test for forbidden words or basic safety.
   - **Asynchronous (Deep):** Send the response to a background Lambda queue that uses a smaller, cheaper Bedrock model to grade the response quality, hallucination likelihood, and relevance on a scale of 1-5.
4. **The Return:** The proxy returns the LLM response to the client application.

## Current Implementation

Steps 1, 2, and the deep-evaluation half of step 3 are implemented, deployed to AWS, and callable over the internet as a real proxy — not just a local script.

- **[`sreJudge.js`](sreJudge.js)** — `runSonnetJudge(context, modelOutput)` sends a grounding context and a model's completion to **Claude Sonnet 4.6** on Amazon Bedrock, and gets back a structured reliability scorecard:

  ```json
  {
    "score": 1,
    "status": "🟢 PASSED",
    "hallucinated_facts": "None",
    "severity": "None"
  }
  ```

  The judge grades on a 3-point scale — **Passed**, **Warning** (minor unsupported detail), or **Critical Fail** (contradiction, metric flip, or fabricated high-risk claim) — so it can drive an error-budget-style alerting system rather than a simple pass/fail gate.

- **[`handler.js`](handler.js)** — The Lambda proxy itself, deployed behind API Gateway (see [`template.yaml`](template.yaml)). For each request it:
  1. **Request Gate** — runs regex checks for prompt injection phrasing and PII (credit card numbers, SSNs) and blocks the request immediately if flagged, before any Bedrock call.
  2. **Forward** — sends the prompt (plus grounding context, if supplied) to Claude Sonnet 4.6 on Bedrock and times the call.
  3. **Response Evaluation** — grades the completion with `runSonnetJudge`.
  4. **Return** — sends back the completion, latency, request-gate result, and SRE evaluation as one JSON response.

- **[`demo.js`](demo.js)** — Calls `runSonnetJudge` directly (no network hop) across three cases covering all three verdict types: a grounded output, a metric flip, and a minor unsupported detail. Useful for demoing the judge in isolation.

- **[`demo-api.js`](demo-api.js)** — Calls the **live deployed endpoint** over HTTP across four cases: a grounded question, a prompt-injection attempt, PII in the prompt, and a prompt with no grounding context. Demonstrates the whole proxy pipeline, not just the judge.

Still on the roadmap: the synchronous fast rule-check running alongside the async judge (currently the judge call is synchronous, not offloaded to a background queue), the 1-5 relevance/quality scale (currently 1-3), and the error-budget dashboard/alerting from Goal 2.

## Getting Started

### Prerequisites

- Node.js
- An AWS account with:
  - Access to **Claude Sonnet 4.6** enabled in Amazon Bedrock (submit the one-time use-case form in the Bedrock console playground if you haven't already)
  - An IAM identity with permissions for Bedrock, Lambda, API Gateway, CloudFormation, and S3 (needed to deploy the stack — see [Deployment](#deployment) below)
  - Credentials configured locally (`aws configure`, or `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_REGION` environment variables)

### Install

```bash
npm install
```

### Run the local judge demo

```bash
node demo.js
```

Sends three test cases straight to `runSonnetJudge` (no deployment needed) and prints the grounding context, the model output under test, and the judge's verdict for each.

### Use the judge in your own code

```js
import { runSonnetJudge } from "./sreJudge.js";

const result = await runSonnetJudge(
  "Q3 revenue was $4.2M, up 12% year-over-year.",
  "Q3 revenue was $4.2M, down 12% year-over-year."
);

console.log(result);
// { score: 3, status: "🔴 CRITICAL FAIL", severity: "High Risk (...)", hallucinated_facts: "..." }
```

## Deployment

The proxy is packaged with [AWS SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) ([`template.yaml`](template.yaml)) as a Lambda function behind an API Gateway REST API.

### Prerequisites

- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
- Docker (used by `sam build --use-container` to build in an environment matching the Lambda runtime — avoids local npm-version build issues)

### Build and deploy

```bash
sam build --use-container
sam deploy --stack-name ai-sre-proxy --region us-east-1 --resolve-s3 --capabilities CAPABILITY_IAM
```

`sam deploy` prints the API's invoke URL when it finishes (`Outputs > ApiUrl`).

### Call the deployed endpoint

```bash
curl -X POST https://<your-api-id>.execute-api.us-east-1.amazonaws.com/Prod/evaluate \
  -H "Content-Type: application/json" \
  -d '{"prompt": "What was Q3 revenue, and was it up or down year-over-year?", "groundingContext": "Q3 revenue was $4.2M, up 12% year-over-year."}'
```

Response:

```json
{
  "requestGate": { "blocked": false, "reason": null },
  "completion": "Q3 revenue was $4.2M, up 12% year-over-year.",
  "latencyMs": 1922,
  "sreEvaluation": {
    "score": 1,
    "status": "🟢 PASSED",
    "hallucinated_facts": "None",
    "severity": "None"
  }
}
```

If `groundingContext` is omitted, the model answers freely and the judge grades the completion against the prompt itself instead.

A prompt containing injection phrasing (e.g. "ignore all previous instructions") or PII (a credit card number, an SSN) gets blocked by the request gate and never reaches Bedrock:

```json
{ "requestGate": { "blocked": true, "reason": "prompt_injection_detected" }, "completion": null, "sreEvaluation": null }
```

### Run the live API demo

```bash
API_URL=https://<your-api-id>.execute-api.us-east-1.amazonaws.com/Prod/evaluate node demo-api.js
```

(Omit `API_URL` to use the default endpoint baked into the script.) Runs four cases against the deployed proxy: a grounded question, a prompt-injection attempt, PII in the prompt, and a prompt with no grounding context — showing the request gate, the forward-to-Bedrock call with latency, and the judge's verdict for each.
