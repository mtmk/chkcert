// See https://aka.ms/new-console-template for more information

using System.Reflection.Metadata.Ecma335;
using chktls;

int Usage(int exit)
{
    Console.Error.WriteLine($@"
  Usage:
    chkcert [-vvv] <url|host>
");
    return exit;
}

try
{
    string url;
    int verbosity = 0;
    
    switch (args.Length)
    {
        case 0:
            return Usage(2);
        case 1 when args[0].StartsWith("-"):
            return Usage(2);
        case 1:
            url = args[0];
            break;
        case 2 when !args[0].StartsWith("-"):
            return Usage(2);
        case 2:
            foreach (var v in args[0].TrimStart('-'))
            {
                if (v != 'v') return Usage(2);
                verbosity++;
            }
            url = args[1];
            break;
        default:
            return Usage(2);
    }

    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        url = "https://" + url;
    
    await new Checker(verbosity).Go(url);
}
catch (HttpRequestException e)
{
    await Console.Error.WriteLineAsync($"HTTP request error: {e.Message}");
    await Console.Error.WriteLineAsync(e.ToString());
    return 2;
}
catch (Exception e)
{
    await Console.Error.WriteLineAsync($"Error: {e.Message}");
    await Console.Error.WriteLineAsync(e.ToString());
    return 2;
}

return 0;
