# Commands

Each command gets one builder and one handler. Builders own
`System.CommandLine` options; handlers translate parsed input into Core
requests. Command handlers must not contain rendering or model logic.

Planned commands:

- `inspect`
- `scaffold-model`
- `validate-model`
- `render`
- `convert`
- `validate`
