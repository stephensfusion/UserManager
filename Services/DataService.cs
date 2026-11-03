using System;
using UserManager.Models;
using NSCore.Models;

namespace UserManager.Services;

public class DataService
{
    private readonly int _pageSize;
    private readonly Func<int, int, Task<List<UserModel>>> _fetchDataFunc;

    public DataService(int pageSize, Func<int, int, Task<List<UserModel>>> fetchDataFunc)
    {
        _pageSize = pageSize;
        _fetchDataFunc = fetchDataFunc;
    }

    /// <summary>
    /// Loads data asynchronously starting from the specified index.
    /// </summary>
    /// <param name="from">The starting index for loading data.</param>
    /// <returns>A <see cref="LazyLoad"/> object containing the result, a flag indicating if more records are available, and the next starting index.</returns>
    public async Task<LazyLoad<UserModel>> LoadDataAsync(int from)
    {
        var items = await _fetchDataFunc(from, _pageSize);

        return new LazyLoad<UserModel>
        {
            Result = items,
            HasMoreRecords = items.Count >= _pageSize,
            NextFrom = from + items.Count
        };
    }
}
