/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CarouselPerformance;

public class MainViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _status;
    private const int TotalItems = 1000;
    private const int PageSize = 50;
    private int _loadedCount = 0;

    public ObservableCollection<string> Items { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public ICommand LoadAllCommand { get; }
    public ICommand LoadIncrementalCommand { get; }
    public ICommand LoadMoreCommand { get; }

    public MainViewModel()
    {
        LoadAllCommand = new Command(async () => await LoadAllAsync());
        LoadIncrementalCommand = new Command(async () => await StartIncrementalAsync());
        LoadMoreCommand = new Command(async () => await LoadMoreAsync());
        Status = "Ready to test";
    }

    private async Task LoadAllAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Items.Clear();
        Status = "Loading all items...";
        
        var stopwatch = Stopwatch.StartNew();

        // Simulate fetching all data at once
        await Task.Run(async () =>
        {
            var newItems = new List<string>();
            for (int i = 0; i < TotalItems; i++)
            {
                // Simulate some processing per item (e.g. thumbnail generation)
                // await Task.Delay(1); 
                newItems.Add($"Item {i} - Bulk Loaded");
            }

            // UI Update
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }
            });
        });

        stopwatch.Stop();
        Status = $"Bulk Load Complete. {TotalItems} items in {stopwatch.ElapsedMilliseconds}ms";
        IsBusy = false;
    }

    private async Task StartIncrementalAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Items.Clear();
        _loadedCount = 0;
        Status = "Starting incremental load...";

        var stopwatch = Stopwatch.StartNew();

        await LoadMoreBatchAsync();

        stopwatch.Stop();
        Status = $"Initial Page Loaded ({Items.Count} items) in {stopwatch.ElapsedMilliseconds}ms. Scroll for more.";
        IsBusy = false;
    }

    private async Task LoadMoreAsync()
    {
        if (IsBusy || _loadedCount >= TotalItems) return;
        
        IsBusy = true;
        await LoadMoreBatchAsync();
        IsBusy = false;
    }

    private async Task LoadMoreBatchAsync()
    {
        await Task.Run(async () =>
        {
            // Simulate network/db delay
            await Task.Delay(500); 

            var newItems = new List<string>();
            int limit = Math.Min(_loadedCount + PageSize, TotalItems);
            
            for (int i = _loadedCount; i < limit; i++)
            {
                newItems.Add($"Item {i} - Lazy Loaded");
            }
            _loadedCount = limit;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }
            });
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
