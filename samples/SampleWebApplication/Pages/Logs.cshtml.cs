using Azure.Data.Tables;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SampleWebApplication.Models;

using Serilog.Sinks.AzureTableStorage.Extensions;

namespace SampleWebApplication.Pages;

public class LogsModel : PageModel
{
    private readonly TableServiceClient _tableServiceClient;

    public LogsModel(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    [BindProperty(Name = "s", SupportsGet = true)]
    public int PageSize { get; set; } = 100;

    [BindProperty(Name = "l", SupportsGet = true)]
    public string? Level { get; set; } = string.Empty;

    [BindProperty(Name = "d", SupportsGet = true)]
    public DateTime Date { get; set; } = DateTime.Today;

    [BindProperty(Name = "t", SupportsGet = true)]
    public string ContinuationToken { get; set; } = string.Empty;

    public string? NextToken { get; set; }

    public IReadOnlyCollection<LogEventModel> Logs { get; set; } = new List<LogEventModel>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var logTable = _tableServiceClient.GetTableClient("SampleLog");

        var dateTime = Date.Date.ToUniversalTime();
        var upper = dateTime.GeneratePartitionKey();
        var lower = dateTime.AddDays(1).GeneratePartitionKey();

        var filter = $"({nameof(ITableEntity.PartitionKey)} ge '{lower}') and ({nameof(ITableEntity.PartitionKey)} lt '{upper}')";

        if (!string.IsNullOrWhiteSpace(Level))
            filter += $" and ({nameof(LogEventModel.Level)} eq '{Level}')";

        await using var enumerator = logTable
            .QueryAsync<LogEventModel>(
                filter: filter,
                maxPerPage: PageSize,
                cancellationToken: cancellationToken
            )
            .AsPages(
                continuationToken: ContinuationToken,
                pageSizeHint: PageSize
            )
            .GetAsyncEnumerator(cancellationToken);

        await enumerator.MoveNextAsync();

        // only use first page
        Logs = enumerator.Current.Values;
        NextToken = enumerator.Current.ContinuationToken;

        return Page();
    }
}
