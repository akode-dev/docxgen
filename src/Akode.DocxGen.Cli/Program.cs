#pragma warning disable CA2007 // Console applications have no synchronization context.
using System.CommandLine;
using Akode.DocxGen.Cli;
using Akode.DocxGen.Cli.Commands;

using var services = CompositionRoot.Build();
var root = new RootCommand(
    "Akode DocxGen — render polished DOCX documents and extract semantic Markdown.");

root.Subcommands.Add(CommandFactory.Inspect(services));
root.Subcommands.Add(CommandFactory.ScaffoldModel(services));
root.Subcommands.Add(CommandFactory.ValidateModel(services));
root.Subcommands.Add(CommandFactory.Render(services));
root.Subcommands.Add(CommandFactory.Convert(services));
root.Subcommands.Add(CommandFactory.Extract(services));
root.Subcommands.Add(CommandFactory.Validate(services));

return await root.Parse(args).InvokeAsync();
#pragma warning restore CA2007
