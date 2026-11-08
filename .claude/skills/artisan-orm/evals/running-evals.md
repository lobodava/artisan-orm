# Evaluating skill selection and behavior

Use `evals.json` as the case inventory. Cases describe a project context, a user prompt, whether the skill applies, and observable assertions. These are assistant-behavior evaluations, not a replacement for the library's automated tests or proof of complete API coverage.

## Freeze the revision

Use the current worktree, including intended uncommitted skill changes. Record the Git commit and worktree differences, Claude Code version, actual model, and hashes of `SKILL.md`, every reference, this protocol, the scenario JSON, the runner, and source/package fixtures before launching sessions. Do not edit them during the run. Report infrastructure fixes as a separate revision instead of silently mixing evidence.

Read the scenario list to calculate the schedule: run every case once, then run each case whose `kind` is `selection` twice more in fresh sessions. Thus `planned sessions = case count + 2 * selection case count`. Do not assume that a previous 14-case run covers this expanded suite.

## Prepare independent sessions

- Use a new Claude Code process/conversation and a fresh isolated fixture for each attempt. Make the frozen skill discoverable in every fixture, including cases whose `expected_skill_usage` is false.
- Supply only the case's context and prompt, expressed through fixture files and the user request. Do not reveal expected usage, assertions, prior reports, or these coordinator instructions to the evaluated session.
- Do not invoke the skill through a slash command, force a Skill call, or preload its body/references. Observe normal selection. A context that names a reference file, such as the shared-example case, still does not authorize editing it.
- Preserve case constraints: the older-package case has no matching package/source to inspect; the consumer example has no source checkout; SQL-only and unrelated-library cases have no Artisan dependency. Do not make a hidden source checkout available merely to help a case pass.
- When examples need compilation, use a separate validation copy with the exact local 4.1.0 package or source and record that addition. Do not feed this validation context back into the evaluated session.
- Keep generated application files inside the fixture. Do not change the original project, wiki, skill, global Claude configuration, or databases. Do not commit, push, publish, or deploy as part of these evaluations. If infrastructure prevents completion, record it rather than treating silence as a passing negative case.

## Record activation and behavior separately

Classify each attempt from raw tool events:

| Class | Observed event |
| --- | --- |
| A | The skill loader successfully loads the frozen fixture copy |
| B | A load is attempted but fails or targets a different copy |
| C | The session reads the skill body/references directly without a successful loader event |
| D | Neither a skill-load attempt nor a direct skill read occurs |
| E | Evidence is missing or inconclusive |

For positive cases, distinguish A from direct-reading fallback C. A negative case passes selection only with D and a completed, relevant response; a crashed, denied, or unfinished session is not evidence of correct avoidance. Record the loaded path, file hash, permission denials, out-of-fixture reads/writes, exit status, model, duration, and cost when available.

Grade each assertion independently as `PASS`, `FAIL`, or `NOT_ASSESSED`, with a concise quote or artifact location supporting the result. Do not infer correct APIs from a convincing explanation or count compilation as evidence of runtime SQL behavior. Compilation and local parameter-shape checks may be recorded separately for .NET 8 and .NET 10. Document any imports, wrappers, models, or stubs added only by the validator; do not silently repair the submitted code before grading it.

Validate advertised project files as well as C# examples: restoring a different dependency graph does not validate the submitted configuration. Preserve the original restore/build failure before trying a corrected diagnostic variant. Compile complete derived repository declarations, including their constructors, and resolve types against the actual fixture before adding validator stubs.

Before reporting an implementation defect, follow the relevant helper definitions and surrounding control flow rather than inferring behavior from a method name or one getter call. Corroborate disputed runtime claims with a focused authorized check when possible; otherwise label them as source analysis and state what remains unverified. Keep a grader error separate from an evaluated-session error, and retain evidence for both.

Also review technical claims outside the named assertions, including inline API calls outside code fences. Record unsupported APIs, incorrect contracts, and invented fixture/deployment details separately from false claims of having inspected or executed something. All assertions passing does not make an answer with those inaccuracies fully correct. Classify non-compiling code units as intended executable examples, incomplete fragments, or quoted source excerpts, with evidence; do not dismiss an invalid advertised call as a harmless fragment.

When checking the three API levels, accept the simplest valid repository shortcut for a straightforward operation. Require command/reader control only when the case calls for it, such as a custom projection or mixed result protocol. Keep manual-mapper shape flexibility intact throughout the suite.

## Report the first pass and repeats

Create a new `artifacts/claude-skill-evals/run-<timestamp>/` directory in the coordinator workspace. Save the frozen manifest, runner, fixture descriptions, raw transcripts, generated files, compile logs, `results.json`, `grading.json`, and `report.md` there, outside the distributed skill.

The report must identify the frozen revision and actual model, show all first-pass case outcomes, list every failed/unassessed assertion, and report selection repeats separately. A later pass does not erase an earlier failure. Additional infrastructure retries are separate attempts beyond the planned schedule, with reasons and links to the original logs. Do not retry by changing prompts until they pass.

Reconcile the manifest and machine-readable results with the report: planned attempts, completed responses, interrupted attempts, and infrastructure retries are separate counts. List every retry explicitly in the manifest and retain its original attempt. Preserve reports and frozen inputs from earlier revisions; revised criteria apply to a new run rather than rewriting historical scores.

Finish with the report path, first-pass selection results, assertion totals, repeat consistency, and unresolved findings. Report a stopped or partial run as partial. When comparing whether the skill improves behavior, run matched tasks without it as a separate comparison; a well-formed JSON file or a human review cannot establish automatic selection.
