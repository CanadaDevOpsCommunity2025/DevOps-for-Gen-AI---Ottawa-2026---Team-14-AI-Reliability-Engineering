import { BedrockRuntimeClient, InvokeModelCommand } from "@aws-sdk/client-bedrock-runtime";

const bedrockClient = new BedrockRuntimeClient({ region: "us-east-1" });

/**
 * Sends model outputs and grounding context to Claude Sonnet 4.6 for SRE Evaluation.
 * @param {string} context - The original source material (truth).
 * @param {string} modelOutput - The AI completion to grade.
 * @returns {Promise<object>} The structured SRE scorecard.
 */
export async function runSonnetJudge(context, modelOutput) {
  const prompt = `
You are an expert AI Site Reliability Engineer (AI SRE) Quality Judge.
Your task is to compare the Model Output against the provided Grounding Context to detect hallucinations, metric flips, or fabrications.

Grounding Context: ${context}
Model Output: ${modelOutput}

Classify the output's reliability using this strict scale:
- Score 1 (Passed): Output is fully grounded and accurate to the context.
- Score 2 (Warning): Output contains minor speculative details or unharmful fabrications not in the context.
- Score 3 (Critical Fail): Output directly contradicts the context, flips numbers/metrics, or invents high-risk claims.

You must return your judgment in RAW JSON format. Do not include conversational preambles, introductory text, or markdown block wrappers (\`\`\`json).
Use this exact schema:
{
    "score": 1, 2, or 3,
    "status": "🟢 PASSED" or "🟡 WARNING" or "🔴 CRITICAL FAIL",
    "hallucinated_facts": "Detailed explanation of any mismatch, or 'None'",
    "severity": "None", "Medium Risk", or "High Risk (Clinical/Financial Safety Violation)"
}
`.trim();

const payload = {
    anthropic_version: "bedrock-2023-05-31",
    max_tokens: 500,
    temperature: 0.0,
    messages: [
      {
        role: "user",
        content: prompt,
      },
    ],
  };

  const command = new InvokeModelCommand({
    modelId: "us.anthropic.claude-sonnet-4-6",
    contentType: "application/json",
    accept: "application/json",
    body: JSON.stringify(payload),
  });

  try {
    const response = await bedrockClient.send(command);

    const decoder = new TextDecoder("utf-8");
    const responseBody = JSON.parse(decoder.decode(response.body));
    const rawText = responseBody.content[0].text.trim();
    
    // 4. Safely clean any potential markdown formatting
    const cleanedText = rawText.replace(/^```json/, "").replace(/```$/, "").trim();
    
    return JSON.parse(cleanedText);
  } catch (error) {
    console.error("AI SRE Judge failed:", error);
    return {
      score: 3,
      status: "🔴 SRE EVALUATOR ERROR",
      hallucinated_facts: `The evaluation API call failed: ${error.message}`,
      severity: "High Risk (Operational Outage)",
    };
  }
}