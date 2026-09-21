using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchCraft.App.Models;

public sealed class BatchJob : ObservableObject
{
    private string name = "New step";
    private string command = "echo Hello from BatchCraft";
    private bool continueOnError;
    private string status = "Pending";

    public string Name { get => name; set => SetProperty(ref name, value); }
    public string Command { get => command; set => SetProperty(ref command, value); }
    public bool ContinueOnError { get => continueOnError; set => SetProperty(ref continueOnError, value); }
    public string Status { get => status; set => SetProperty(ref status, value); }
}
