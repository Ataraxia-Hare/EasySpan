# Contributing to EasySpan

Thank you for your interest in contributing to this project!

This document describes how to contribute code, report issues, and submit changes.

## 1. Ways to Contribute

You can contribute by:

- Reporting bugs and unexpected behavior.
- Suggesting new features or improvements.
- Improving documentation.
- Submitting code changes through pull requests.
- Adding or improving tests.

For significant changes, please open an issue first to discuss the proposed solution.

## 2. Development Workflow

1. Fork this repository if you do not have write access.
2. Clone your fork to your local machine.
3. Create a feature branch from `main`.
4. Implement your changes and test them.
5. Commit your changes using the required commit message convention.
6. Push your branch to your fork.
7. Open a pull request targeting the original repository's `main` branch.

Use descriptive branch names, such as:

- `feat/level-generation`
- `fix/grid-selection`
- `docs/setup-guide`

Do not commit directly to `main`.

## 3. Commit Message Convention

This project follows [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).

### 3.1 Format

Every commit message must follow this format:

`<type>(<scope>): <description>`

The scope is optional. Breaking changes may be indicated by `!`.

Examples:

- `feat(level): add random level generation`
- `fix(input): correct grid intersection selection`
- `docs: update installation instructions`
- `refactor(pool): simplify object reuse logic`
- `perf(ui): reduce canvas rebuilds`
- `feat(api)!: remove legacy interface`

### 3.2 Allowed Types

- `feat`: Introduce a new feature.
- `fix`: Fix a bug.
- `docs`: Update documentation.
- `style`: Make code style or formatting changes without changing behavior.
- `refactor`: Refactor code without changing external behavior.
- `perf`: Improve performance.
- `test`: Add or update tests.
- `build`: Change build configuration or dependencies.
- `ci`: Change CI/CD configuration.
- `chore`: Perform maintenance tasks.
- `revert`: Revert a previous commit.

### 3.3 Commit Message Rules

- Use one of the allowed lowercase types.
- Include a colon followed by a space after the type and optional scope.
- Write a clear, concise description of the change.
- Use an optional scope to identify the affected module.
- Keep each commit focused on one logical change.
- Write commit messages in English.
- Use the commit body to explain the motivation or implementation details when necessary.
- Reference related issues when appropriate.
- Document breaking changes using `!` and/or a `BREAKING CHANGE:` footer.

The CI workflow checks commit message headers. Other requirements in this section are contributor guidelines unless separately automated.

## 4. Pull Request Guidelines

Before submitting a pull request:

- Ensure the project builds successfully.
- Run relevant tests.
- Review your changes for unrelated modifications.
- Update documentation when necessary.
- Explain the purpose and impact of your changes.
- Link related issues when applicable.

Keep pull requests focused on a single feature, bug fix, or maintenance task whenever practical.

All required CI checks must pass before a pull request can be merged.

## 5. Reporting Bugs

When reporting a bug, include as much of the following information as possible:

- A clear and descriptive title.
- Steps to reproduce the issue.
- Expected behavior.
- Actual behavior.
- Relevant logs, screenshots, or error messages.
- Environment information, such as the Unity version and target platform.

Do not include passwords, access tokens, or other sensitive information.

## 6. Suggesting Features

When suggesting a feature, explain:

- The problem or use case.
- The proposed behavior.
- Why the feature would be useful.
- Any relevant examples or references.

## 7. Code Quality

- Follow the existing project structure and coding conventions.
- Prefer readable, maintainable implementations.
- Avoid unrelated changes in the same pull request.
- Do not commit generated files or local configuration files unless they are required by the project.
- Do not introduce dependencies without considering their impact on the project.

## 8. License

By submitting a contribution, you agree that your contribution will be distributed under the license applicable to this repository.

If you are unsure whether a contribution is appropriate, open an issue before submitting it.

Thank you for helping improve this project!
