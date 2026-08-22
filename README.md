# DevOps-for-GenAI---Ottawa-2026---Team-14-AyushSharma
Team 14
Project Name - N/A
Project Lead: Ayush Sharma
Other Participants: Roland Agodzo, Shervin Naseri
Project Title: AI Reliability Engineering
Team Name: EarlyBird

What it is: A single, lightweight Python/Node.js AWS Lambda function exposed via AWS API Gateway as a REST endpoint
Goal 1: The proxy will monitor Prompt/Response Quality, Latency, Token Costs, and Security Guardrails in real-time.
Goal 2: The AI SRE proxy, will define an AI Reliability Error Budget. For example, the app allows a maximum "Toxicity/PII leak rate" of 1% or an "Average Hallucination Score" of 1.5/5. If the model's performance consumes this budget, the dashboard flags an incident and triggers an alert.
Objective: By sitting in front of the model, the proxy acts as an active guardrail and compliance gate. It checks prompts for injections and completions for PII leaks before they can reach the user, enforcing continuous compliance.


The client app calls the Lambda API instead of calling Bedrock directly.
Request Gate (Shift Left): The Lambda runs quick regex/rule checks on the prompt to detect prompt injections or PII (like credit card numbers). If flagged, it blocks the request immediately with a safe fallback response.
The Forward: If safe, it forwards the prompt to Amazon Bedrock (e.g., calling Anthropic Claude or Meta Llama 3) and measures the exact time to first token and total latency.
Response Evaluation (LLM-as-a-Judge): Before returning the completion to the user, Lambda runs an evaluation. 
Synchronous (Fast): Run a rapid rule-based test for forbidden words or basic safety.
Asynchronous (Deep): Send the response to a background Lambda queue that uses a smaller, cheaper Bedrock model to grade the response quality, hallucination likelihood, and relevance on a scale of 1-5.
The Return: The proxy returns the LLM response to the client application.