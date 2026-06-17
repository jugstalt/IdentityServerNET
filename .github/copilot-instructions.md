# Copilot Instructions

## Project Guidelines
- Tests in this repository should use xUnit and test projects should be placed in the top-level "test" folder.
- Maintain the vendored IdentityServer4 under `src/libs-is4` as an OpenSource fork, ensuring it is kept maintained, OpenID/OAuth2-standards-conformant, and secure. Prefer fixing standards/conformance issues in the vendored lib itself, and keep the IS4 integration/conformance test suite green as a permanent safety net. Prioritize hardening this fork over migrating off IdentityServer4.