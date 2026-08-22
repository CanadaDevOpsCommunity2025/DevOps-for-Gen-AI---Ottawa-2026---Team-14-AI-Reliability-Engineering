import { runSonnetJudge } from "./sreJudge.js";

const cases = [
  {
    label: "Grounded output (should PASS)",
    context: "Q3 revenue was $4.2M, up 12% year-over-year.",
    modelOutput: "Q3 revenue came in at $4.2M, a 12% increase compared to last year.",
  },
  {
    label: "Metric flip (should CRITICAL FAIL)",
    context: "Q3 revenue was $4.2M, up 12% year-over-year.",
    modelOutput: "Q3 revenue was $4.2M, down 12% year-over-year.",
  },
  {
    label: "Minor unsupported detail (should WARNING)",
    context: "The patient was prescribed 500mg of amoxicillin twice daily.",
    modelOutput: "The patient was prescribed 500mg of amoxicillin twice daily, a common antibiotic used since the 1970s.",
  },
];

function printHeader(text) {
  const bar = "=".repeat(text.length + 4);
  console.log(`\n${bar}\n  ${text}\n${bar}`);
}

for (const [i, testCase] of cases.entries()) {
  printHeader(`TEST ${i + 1}: ${testCase.label}`);
  console.log(`Grounding Context:\n  "${testCase.context}"`);
  console.log(`\nModel Output Under Test:\n  "${testCase.modelOutput}"`);

  console.log("\n> Sending to Claude Sonnet 4.6 on AWS Bedrock...");
  const result = await runSonnetJudge(testCase.context, testCase.modelOutput);

  console.log("\nSRE Judge Verdict:");
  console.log(`  Status:      ${result.status}`);
  console.log(`  Score:       ${result.score}`);
  console.log(`  Severity:    ${result.severity}`);
  console.log(`  Explanation: ${result.hallucinated_facts}`);
}

console.log("\nDone.\n");
