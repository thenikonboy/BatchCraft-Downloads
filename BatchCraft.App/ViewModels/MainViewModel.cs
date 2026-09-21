using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using BatchCraft.App.Models;
using BatchCraft.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BatchCraft.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IBatchService batchService;
    private CancellationTokenSource? cancellation;
    private BatchJob? selectedJob;
    private string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string statusMessage = "Ready";
    private string logText = "";
    private bool isRunning;

    public MainViewModel(IBatchService batchService)
    {
        this.batchService = batchService;
        RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
        Jobs.Add(new BatchJob { Name = "Example", Command = "echo BatchCraft is ready" });
        SelectedJob = Jobs[0];
    }

    public ObservableCollection<BatchJob> Jobs { get; } = [];
    public BatchJob? SelectedJob { get => selectedJob; set => SetProperty(ref selectedJob, value); }
    public string WorkingDirectory { get => workingDirectory; set => SetProperty(ref workingDirectory, value); }
    public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
    public string LogText { get => logText; private set => SetProperty(ref logText, value); }
    public bool IsRunning { get => isRunning; private set { if (SetProperty(ref isRunning, value)) RunCommand.NotifyCanExecuteChanged(); } }
    public string Title => "BatchCraft";
    public IAsyncRelayCommand RunCommand { get; }

    public void AddJob() { var job = new BatchJob(); Jobs.Add(job); SelectedJob = job; }
    public void RemoveSelected() { if (SelectedJob is not null) Jobs.Remove(SelectedJob); SelectedJob = Jobs.FirstOrDefault(); }
    public void MoveSelected(int offset)
    {
        if (SelectedJob is null) return;
        var oldIndex = Jobs.IndexOf(SelectedJob); var newIndex = oldIndex + offset;
        if (newIndex >= 0 && newIndex < Jobs.Count) Jobs.Move(oldIndex, newIndex);
    }

    public void ClearLog() => LogText = "";
    private void AppendLog(string value) => System.Windows.Application.Current.Dispatcher.Invoke(() => LogText += value + Environment.NewLine);

    private async Task RunAsync()
    {
        if (!Directory.Exists(WorkingDirectory)) { StatusMessage = "Working folder does not exist"; return; }
        IsRunning = true; cancellation = new CancellationTokenSource(); LogText = "";
        try
        {
            foreach (var job in Jobs)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                job.Status = "Running"; StatusMessage = $"Running: {job.Name}"; AppendLog($"> {job.Name}: {job.Command}");
                int exitCode;
                try { exitCode = await batchService.RunAsync(job, WorkingDirectory, AppendLog, cancellation.Token); }
                catch (Exception ex) when (ex is not OperationCanceledException) { AppendLog("ERROR: " + ex.Message); exitCode = -1; }
                job.Status = exitCode == 0 ? "Completed" : $"Failed ({exitCode})";
                AppendLog($"[{job.Status}]{Environment.NewLine}");
                if (exitCode != 0 && !job.ContinueOnError) { StatusMessage = $"Stopped: {job.Name} failed"; return; }
            }
            StatusMessage = Jobs.Count == 0 ? "Nothing to run" : "Batch completed";
        }
        catch (OperationCanceledException) { StatusMessage = "Cancelled"; AppendLog("[Cancelled]"); }
        finally { IsRunning = false; cancellation.Dispose(); cancellation = null; }
    }

    private bool CanRun() => !IsRunning && Jobs.Count > 0;
    public void Cancel() => cancellation?.Cancel();

    public async Task SaveAsync(string path)
    {
        var data = new BatchFile(WorkingDirectory, Jobs.ToList());
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        StatusMessage = "Batch saved";
    }

    public async Task LoadAsync(string path)
    {
        var data = JsonSerializer.Deserialize<BatchFile>(await File.ReadAllTextAsync(path)) ?? throw new InvalidDataException("Invalid batch file.");
        Jobs.Clear(); foreach (var job in data.Jobs ?? []) { job.Status = "Pending"; Jobs.Add(job); }
        WorkingDirectory = string.IsNullOrWhiteSpace(data.WorkingDirectory) ? Environment.CurrentDirectory : data.WorkingDirectory;
        SelectedJob = Jobs.FirstOrDefault(); RunCommand.NotifyCanExecuteChanged(); StatusMessage = "Batch loaded";
    }

    private sealed record BatchFile(string WorkingDirectory, List<BatchJob> Jobs);
}
