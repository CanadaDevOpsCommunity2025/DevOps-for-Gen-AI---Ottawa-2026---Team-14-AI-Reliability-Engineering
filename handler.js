import { BedrockRuntimeClient, InvokeModelCommand } from "@aws-sdk/client-bedrock-runtime";
import { runSonnetJudge } from "./sreJudge.js";

const bedrockClient = new BedrockRuntimeClient({ region: process.env.AWS_REGION || "us-east-1" });
const GENERATION_MODEL_ID = "us.anthropic.claude-sonnet-4-6";

// Request Gate: quick regex checks for prompt injection and PII, run before any Bedrock call.
const INJECTION_PATTERNS = [
  /ignore (all |any )?(previous|prior|above) instructions/i,
  /disregard (all |any )?(previous|prior|above) instructions/i,
  /you are now/i,
  /reveal (your |the )?(system|initial|hidden) prompt/i,
  /forget (all |any )?(previous|prior) (instructions|context)/i,
];

const PII_PATTERNS = [
  { name: "credit_card", regex: /\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b/ },
  { name: "ssn", regex: /\b\d{3}-\d{2}-\d{4}\b/ },
];

function checkRequestGate(prompt) {
  for (const pattern of INJECTION_PATTERNS) {
    if (pattern.test(prompt)) {
      return { blocked: true, reason: "prompt_injection_detected" };
    }
  }
  for (const { name, regex } of PII_PATTERNS) {
    if (regex.test(prompt)) {
      return { blocked: true, reason: `pii_detected:${name}` };
    }
  }
  return { blocked: false, reason: null };
}

async function generateCompletion(prompt, groundingContext) {
  const userMessage = groundingContext
    ? `Context:\n${groundingContext}\n\nQuestion:\n${prompt}`
    : prompt;

  const payload = {
    anthropic_version: "bedrock-2023-05-31",
    max_tokens: 1024,
    temperature: 0.7,
    messages: [{ role: "user", content: userMessage }],
  };

  const command = new InvokeModelCommand({
    modelId: GENERATION_MODEL_ID,
    contentType: "application/json",
    accept: "application/json",
    body: JSON.stringify(payload),
  });

  const startedAt = Date.now();
  const response = await bedrockClient.send(command);
  const latencyMs = Date.now() - startedAt;

  const decoder = new TextDecoder("utf-8");
  const responseBody = JSON.parse(decoder.decode(response.body));
  const completion = responseBody.content[0].text.trim();

  return { completion, latencyMs };
}

function jsonResponse(statusCode, body) {
  return {
    statusCode,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}

export const lambdaHandler = async (event) => {
  let payload;
  try {
    payload = JSON.parse(event.body || "{}");
  } catch {
    return jsonResponse(400, { error: "Request body must be valid JSON." });
  }

  const { prompt, groundingContext } = payload;
  if (!prompt || typeof prompt !== "string") {
    return jsonResponse(400, { error: "Request body must include a string 'prompt' field." });
  }

  // 1. Request Gate (shift-left): block unsafe prompts before they reach Bedrock.
  const gateResult = checkRequestGate(prompt);
  if (gateResult.blocked) {
    return jsonResponse(200, {
      requestGate: gateResult,
      completion: null,
      sreEvaluation: null,
    });
  }

  try {
    // 2. The Forward: send the prompt (plus grounding context, if provided) to
    //    Bedrock and measure latency.
    const { completion, latencyMs } = await generateCompletion(prompt, groundingContext);

    // 3. Response Evaluation: grade the completion against the grounding context
    //    (or the prompt itself, if no separate grounding context was supplied).
    const sreEvaluation = await runSonnetJudge(groundingContext || prompt, completion);

    // 4. The Return.
    return jsonResponse(200, {
      requestGate: gateResult,
      completion,
      latencyMs,
      sreEvaluation,
    });
  } catch (error) {
    return jsonResponse(500, { error: `Proxy failed: ${error.message}` });
  }
};
