using System.CommandLine;

var root = new RootCommand(
    "Akode.DocxGen repository scaffold. Rendering commands are not implemented yet.");

root.Subcommands.Add(new Command("inspect", "Inspect a DOCX template contract."));
root.Subcommands.Add(new Command("scaffold-model", "Create a model skeleton from a template."));
root.Subcommands.Add(new Command("validate-model", "Validate model data before rendering."));
root.Subcommands.Add(new Command("render", "Render a DOCX from a template and model."));
root.Subcommands.Add(new Command("convert", "Convert Markdown using a reference style document."));
root.Subcommands.Add(new Command("validate", "Validate a generated DOCX package."));

return root.Parse(args).Invoke();
