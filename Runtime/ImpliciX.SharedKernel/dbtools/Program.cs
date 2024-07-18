// See https://aka.ms/new-console-template for more information

var command = new RootCommand("HotDb debug tools.")
{
    JsonExport.CreateCommand(),
};

return await command.InvokeAsync(args);