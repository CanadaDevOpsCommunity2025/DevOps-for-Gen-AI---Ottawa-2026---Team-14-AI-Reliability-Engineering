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

The LLM-as-a-Judge component (step 3, deep evaluation) is implemented and working end-to-end against AWS Bedrock.

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

- **[`demo.js`](demo.js)** — Runs three real Bedrock calls through the judge, covering all three verdict types: a grounded output, a metric flip (e.g. "up 12%" flipped to "down 12%"), and a minor unsupported detail. Useful for demoing the judge live.

The request-gate (prompt injection / PII regex checks), Lambda/API Gateway wrapper, latency instrumentation, and error-budget dashboard described above are part of the project's roadmap and not yet implemented.

## Getting Started

### Prerequisites

- Node.js
- An AWS account with:
  - Access to **Claude Sonnet 4.6** enabled in Amazon Bedrock (submit the one-time use-case form in the Bedrock console playground if you haven't already)
  - An IAM identity with `bedrock:InvokeModel` permission (e.g. the `AmazonBedrockFullAccess` managed policy)
  - Credentials configured locally (`aws configure`, or `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_REGION` environment variables)

### Install

```bash
npm install
```

### Run the demo

```bash
node demo.js
```

This sends three test cases to the judge and prints the grounding context, the model output under test, and the judge's verdict for each.

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
