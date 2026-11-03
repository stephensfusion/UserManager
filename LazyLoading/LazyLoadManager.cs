using System;
using UserManager.Models;
using UserManager.Services;
using NSCore.Models;

namespace UserManager.LazyLoading;

public class LazyLoadManager
{
    private readonly DataService _dataService;
    private LazyLoad<UserModel> _currentState;

    public LazyLoadManager(DataService dataService)
    {
        _dataService = dataService;

        _currentState = new LazyLoad<UserModel>
        {
            HasMoreRecords = true,
            NextFrom = 0,
            Result = new List<UserModel>()
        };
    }

    public IEnumerable<UserModel> CurrentItems => _currentState.Result ?? new List<UserModel>();
    public bool HasMoreItems => _currentState.HasMoreRecords;

    /// <summary>
    /// Loads the next batch of users asynchronously.
    /// </summary>
    /// <returns>A list of users from the next batch.</returns>
    public async Task<IEnumerable<UserModel>> LoadNextBatchAsync()
    {
        if (!_currentState.HasMoreRecords)
        {
            return CurrentItems;
        }
        _currentState = await _dataService.LoadDataAsync(_currentState.NextFrom);
        return _currentState.Result ?? new List<UserModel>();
    }
}